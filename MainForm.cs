using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cAutoInput
{
    public class MainForm : Form
    {
        private ListBox lstActions;
        private Button btnAddKeyPress, btnAddDelay, btnSave, btnLoad, btnRun, btnStop, btnRecord;
        private Script currentScript = new Script();
        private HookRecorder recorder;
        private ScriptExecutor executor = new ScriptExecutor();
        private HotkeyManager hotkeys;
        private CheckBox chkUseDuration;
        private NumericUpDown numDurationMs, numRunCount;
        private Label lblStatus;

        // Hotkey IDs
        private const int HK_F9 = 0x1001;
        private const int HK_F10 = 0x1002;
        private const int HK_F11 = 0x1003;

        public MainForm()
        {
            Text = "AutoInput - 模拟鼠标键盘工具 (Admin required)";
            Width = 800; Height = 600;

            lstActions = new ListBox { Left = 10, Top = 10, Width = 560, Height = 400 };
            Controls.Add(lstActions);

            btnAddKeyPress = new Button { Left = 580, Top = 10, Width = 180, Text = "添加按键 (A)" };
            btnAddKeyPress.Click += (s, e) => { var act = new ActionItem { Type = ActionType.KeyPress, KeyCode = (int)Keys.A }; currentScript.Actions.Add(act); RefreshList(); };
            Controls.Add(btnAddKeyPress);

            btnAddDelay = new Button { Left = 580, Top = 50, Width = 180, Text = "添加延迟 500ms" };
            btnAddDelay.Click += (s, e) => { var act = new ActionItem { Type = ActionType.DelayMs, DurationMs = 500 }; currentScript.Actions.Add(act); RefreshList(); };
            Controls.Add(btnAddDelay);

            btnSave = new Button { Left = 580, Top = 100, Width = 85, Text = "保存" };
            btnLoad = new Button { Left = 675, Top = 100, Width = 85, Text = "导入" };
            Controls.Add(btnSave); Controls.Add(btnLoad);
            btnSave.Click += BtnSave_Click;
            btnLoad.Click += BtnLoad_Click;

            btnRun = new Button { Left = 580, Top = 150, Width = 85, Text = "运行 (F9)" };
            btnStop = new Button { Left = 675, Top = 150, Width = 85, Text = "停止 (F10)" };
            Controls.Add(btnRun); Controls.Add(btnStop);
            btnRun.Click += BtnRun_Click;
            btnStop.Click += (s, e) => executor.Stop();

            btnRecord = new Button { Left = 580, Top = 200, Width = 180, Text = "录制 (F11)" };
            btnRecord.Click += BtnRecord_Click;
            Controls.Add(btnRecord);

            chkUseDuration = new CheckBox { Left = 580, Top = 250, Width = 200, Text = "使用：运行总时长(ms)" };
            numDurationMs = new NumericUpDown { Left = 580, Top = 275, Width = 180, Maximum = 3600000, Value = 10000 };
            numRunCount = new NumericUpDown { Left = 580, Top = 310, Width = 180, Maximum = 10000, Value = 1 };
            Controls.Add(chkUseDuration); Controls.Add(numDurationMs); Controls.Add(numRunCount);

            chkUseDuration.CheckedChanged += (s, e) => { numDurationMs.Enabled = chkUseDuration.Checked; numRunCount.Enabled = !chkUseDuration.Checked; };

            lblStatus = new Label { Left = 10, Top = 420, Width = 760, Height = 40, Text = "状态: 就绪" };
            Controls.Add(lblStatus);

            // Setup components
            executor.OnStatus += s => Invoke(() => lblStatus.Text = $"状态: {s}");
            recorder = new HookRecorder();
            recorder.OnActionRecorded += item => { currentScript.Actions.Add(item); Invoke(() => RefreshList()); };

            // hotkeys
            hotkeys = new HotkeyManager(this.Handle);
            // register F9, F10, F11 without modifiers (fsModifiers = 0)
            if (!hotkeys.RegisterHotkey(Keys.F9, 0, HK_F9)) MessageBox.Show("注册 F9 失败（可能被占用）");
            if (!hotkeys.RegisterHotkey(Keys.F10, 0, HK_F10)) MessageBox.Show("注册 F10 失败（可能被占用）");
            if (!hotkeys.RegisterHotkey(Keys.F11, 0, HK_F11)) MessageBox.Show("注册 F11 失败（可能被占用）");

            // message filter to receive WM_HOTKEY
            this.HandleCreated += (s,e)=> { /* no-op */ };
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HK_F9) TogglePauseResume();
                if (id == HK_F10) executor.Stop();
                if (id == HK_F11) ToggleRecording();
            }
            base.WndProc(ref m);
        }

        private void RefreshList()
        {
            lstActions.Items.Clear();
            foreach (var a in currentScript.Actions) lstActions.Items.Add(a.ToString());
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "JSON 脚本|*.json" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var txt = JsonSerializer.Serialize(currentScript, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(sfd.FileName, txt);
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "JSON 脚本|*.json" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var txt = File.ReadAllText(ofd.FileName);
                currentScript = JsonSerializer.Deserialize<Script>(txt) ?? new Script();
                RefreshList();
            }
        }

        private async void BtnRun_Click(object sender, EventArgs e)
        {
            if (executor.IsRunning)
            {
                // Toggle pause/resume
                if (executor.IsPaused) executor.Resume(); else executor.Pause();
                return;
            }

            int runCount = (int)numRunCount.Value;
            int totalMs = chkUseDuration.Checked ? (int)numDurationMs.Value : 0;
            lblStatus.Text = "状态: 开始任务";
            await executor.StartAsync(currentScript, runCount, totalMs);
        }

        private bool isRecording = false;
        private void BtnRecord_Click(object sender, EventArgs e) => ToggleRecording();

        private void ToggleRecording()
        {
            if (!isRecording)
            {
                currentScript = new Script { Name = $"Script_{DateTime.Now:yyyyMMdd_HHmmss}" };
                recorder.Start();
                isRecording = true;
                btnRecord.Text = "停止录制 (F11)";
                lblStatus.Text = "状态: 正在录制...";
            }
            else
            {
                recorder.Stop();
                isRecording = false;
                btnRecord.Text = "录制 (F11)";
                lblStatus.Text = "状态: 录制停止";
                RefreshList();
            }
        }

        private void TogglePauseResume()
        {
            if (!executor.IsRunning) // start if not running
            {
                BtnRun_Click(null, null);
                return;
            }
            if (executor.IsPaused) executor.Resume(); else executor.Pause();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            executor.Stop();
            recorder.Dispose();
            base.OnFormClosing(e);
        }
    }
}
