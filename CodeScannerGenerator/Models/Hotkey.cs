using CodeScannerGenerator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeScannerGenerator.Models
{
    public class Hotkey
    {
        public System.Windows.Forms.Keys Key { get; }

        public ModifierKeys Modifiers { get; }

        public Hotkey()
        {
            Key = System.Windows.Forms.Keys.None;
            Modifiers = ModifierKeys.None;
        }

        public Hotkey(System.Windows.Forms.Keys key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public Hotkey(string str)
        {
            Key = System.Windows.Forms.Keys.None;
            Modifiers = ModifierKeys.None;
            if (String.IsNullOrEmpty(str) || str == "< None >")
            {
                Key = System.Windows.Forms.Keys.None;
                Modifiers = ModifierKeys.None;
            }
            else
            {
                var keys = str.Split('+');
                foreach (string key in keys)
                {
                    string keyTxt = key.Trim();
                    if (keyTxt == "Ctrl")
                        Modifiers |= ModifierKeys.Control;
                    else if (keyTxt == "Shift")
                        Modifiers |= ModifierKeys.Shift;
                    else if (keyTxt == "Alt")
                        Modifiers |= ModifierKeys.Alt;
                    else if (keyTxt == "Win")
                        Modifiers |= ModifierKeys.Win;
                    else
                    {
                        System.Windows.Forms.KeysConverter kc = new System.Windows.Forms.KeysConverter();
                        Key = (System.Windows.Forms.Keys)kc.ConvertFromString(keyTxt);
                    }
                }
            }
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
