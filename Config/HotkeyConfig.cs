using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Newtonsoft.Json;

namespace ArcaeaCoverMaker.Config
{
    [Serializable]
    internal struct HotkeyItem
    {
        public HotkeyItem(ModifierKeys modifier, Key key)
        {
            Modifier = modifier;
            Key = key;
        }
        public HotkeyItem(Key key)
        {
            Key = key;
        }

        public ModifierKeys Modifier = ModifierKeys.None;
        public Key Key = Key.None;

        /// <summary>
        /// Check if the input key(s) correspond to the keys in this HoykeyItem.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>The input key(s) correspond to the keys in this HoykeyItem.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckHotkey(KeyEventArgs e)
        {
            if (Modifier == ModifierKeys.None && Key == Key.None) return false;
            return Keyboard.Modifiers == Modifier && Keyboard.GetKeyStates(Key) == e.KeyStates;
        }

        public override string ToString()
        {
            return $"[{Modifier}, {Key}]";
        }
    }

    [Serializable]
    internal class HotkeyConfig
    {
        public Dictionary<string, HotkeyItem> Hotkeys = new()
        {
            ["Reload"] = new(ModifierKeys.Control, Key.R),
            ["Capture"] = new(ModifierKeys.Control, Key.S),
            ["RatioBili"] = new(ModifierKeys.Alt, Key.B),
            ["RatioYtb"] = new(ModifierKeys.Alt, Key.Y),
            ["Ratio4:3"] = new(ModifierKeys.Alt, Key.P),
            ["SwitchSecurityZone"] = new(ModifierKeys.Alt, Key.X),
        };

        /// <summary>
        /// Get a HotkeyItem object with the specified key from Hotkey dictionary.
        /// </summary>
        /// <param name="dictKey"></param>
        /// <returns>
        /// The HotkeyItem object from Hotkey dictionary 
        /// if the dictionary contains an element with the specified key.
        /// <para>Overwise return default KeyItem object.</para>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HotkeyItem GetHotkeyItem(string dictKey)
        {
            Hotkeys.TryGetValue(dictKey, out var result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CheckHotkey(string dictKey, KeyEventArgs e)
        {
            return GetHotkeyItem(dictKey).CheckHotkey(e);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckHotkey(Action action, KeyEventArgs e)
        {
            if (action != null && GetHotkeyItem(action.Method.Name).CheckHotkey(e))
            {
                Trace.WriteLine(action.Method.Name);
                action.Invoke();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CheckHotkey(Action action, string dictKey, KeyEventArgs e)
        {
            if (GetHotkeyItem(dictKey).CheckHotkey(e) && action != null)
            {
                action.Invoke();
            }
        }
    }
}
