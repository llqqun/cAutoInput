using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace cAutoInput
{
    public enum ActionType
    {
        KeyDown,
        KeyUp,
        KeyPress,    // 简化：按下并松开
        MouseLeftDown,
        MouseLeftUp,
        MouseRightDown,
        MouseRightUp,
        MouseClick,  // 简化的单击
        MouseLongPress, // hold for duration
        DelayMs
    }

    public class ActionItem
    {
        public ActionType Type { get; set; }
        public int KeyCode { get; set; } // Virtual Key for keyboard actions
        public int X { get; set; } // mouse coordinate if needed
        public int Y { get; set; }
        public int DurationMs { get; set; } // for delays or long press
        public override string ToString()
        {
            return Type + (Type == ActionType.DelayMs ? $" {DurationMs}ms" : (Type == ActionType.KeyPress ? $" VK:{KeyCode}" : ""));
        }
    }

    public class Script
    {
        public string Name { get; set; } = "New Script";
        public List<ActionItem> Actions { get; set; } = new List<ActionItem>();
    }
}
