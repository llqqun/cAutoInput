using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace cAutoInput
{
    public class ScriptExecutor
    {
        private CancellationTokenSource _cts;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        public bool IsRunning { get; private set; } = false;
        public bool IsPaused => !_pauseEvent.IsSet;

        public event Action<string> OnStatus; // status messages

        public async Task StartAsync(Script script, int runCount, int totalDurationMs)
        {
            if (IsRunning) return;
            IsRunning = true;
            _cts = new CancellationTokenSource();
            _pauseEvent.Set();
            var token = _cts.Token;

            OnStatus?.Invoke("开始运行");

            try
            {
                if (totalDurationMs > 0)
                {
                    var sw = Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < totalDurationMs && !token.IsCancellationRequested)
                    {
                        _pauseEvent.Wait(token);
                        await ExecuteOnce(script, token);
                    }
                }
                else
                {
                    for (int i = 0; i < runCount && !token.IsCancellationRequested; i++)
                    {
                        _pauseEvent.Wait(token);
                        await ExecuteOnce(script, token);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsRunning = false;
                OnStatus?.Invoke("已停止");
            }
        }

        private async Task ExecuteOnce(Script script, CancellationToken token)
        {
            foreach (var act in script.Actions)
            {
                token.ThrowIfCancellationRequested();
                _pauseEvent.Wait(token);

                switch (act.Type)
                {
                    case ActionType.DelayMs:
                        await Task.Delay(Math.Max(0, act.DurationMs), token);
                        break;
                    case ActionType.KeyPress:
                        InputSimulator.KeyPress((ushort)act.KeyCode);
                        break;
                    case ActionType.KeyDown:
                        InputSimulator.KeyDown((ushort)act.KeyCode);
                        break;
                    case ActionType.KeyUp:
                        InputSimulator.KeyUp((ushort)act.KeyCode);
                        break;
                    case ActionType.MouseClick:
                        InputSimulator.ClickLeft(act.X, act.Y);
                        break;
                    case ActionType.MouseLeftDown:
                        InputSimulator.ClickLeft(act.X, act.Y); // 简化：用 click 表示
                        break;
                    case ActionType.MouseRightDown:
                        InputSimulator.ClickRight(act.X, act.Y);
                        break;
                    case ActionType.MouseLongPress:
                        InputSimulator.ClickLeft(act.X, act.Y);
                        await Task.Delay(act.DurationMs, token);
                        break;
                }
            }
        }

        public void Pause() { _pauseEvent.Reset(); OnStatus?.Invoke("已暂停"); }
        public void Resume() { _pauseEvent.Set(); OnStatus?.Invoke("继续运行"); }
        public void Stop() { _cts?.Cancel(); OnStatus?.Invoke("停止中"); }
    }
}
