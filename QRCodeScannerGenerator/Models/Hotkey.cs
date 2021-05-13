using QRCodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QRCodeScannerGenerator.Models
{
    public class Hotkey
    {
        public System.Windows.Forms.Keys Key { get; }

        public ModifierKeys Modifiers { get; }

        public Hotkey(System.Windows.Forms.Keys key = System.Windows.Forms.Keys.None, ModifierKeys modifiers = ModifierKeys.None)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public override string ToString()
        {
            if (Key == System.Windows.Forms.Keys.None && Modifiers == ModifierKeys.None)
                return "< None >";

            var str = new StringBuilder();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                str.Append("Ctrl + ");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                str.Append("Shift + ");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                str.Append("Alt + ");
            if (Modifiers.HasFlag(ModifierKeys.Win))
                str.Append("Win + ");

            str.Append(Key);

            return str.ToString();
        }
    }
}
