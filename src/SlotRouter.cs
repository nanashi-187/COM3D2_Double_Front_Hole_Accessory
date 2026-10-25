using System;
using UnityEngine;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// Decides whether a front-hole accessory operation targets the primary
    /// slot (MPN.accxxx), the secondary slot (MPN.accvag) or the plugin-managed
    /// third slot (see <see cref="ExtraSlot"/>).
    /// </summary>
    internal static class SlotRouter
    {
        private const string PrimaryTag = "accxxx";
        private const string SecondaryTag = "accvag";

        private static bool CtrlHeld
        {
            get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); }
        }

        /// <summary>Key held to select the third slot (config ThirdSlotKey, default Alt).</summary>
        public static KeyCode ThirdSlotKey = KeyCode.LeftAlt;

        private static bool SecondaryRequested
        {
            get { return CtrlHeld && !ThirdRequested; }
        }

        public static bool ThirdRequested
        {
            get { return ThirdSlotKey != KeyCode.None && (Input.GetKey(ThirdSlotKey) || Input.GetKey(Counterpart(ThirdSlotKey))); }
        }

        // Left and right modifier keys count as the same key.
        private static KeyCode Counterpart(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftAlt: return KeyCode.RightAlt;
                case KeyCode.RightAlt: return KeyCode.LeftAlt;
                case KeyCode.LeftShift: return KeyCode.RightShift;
                case KeyCode.RightShift: return KeyCode.LeftShift;
                case KeyCode.LeftControl: return KeyCode.RightControl;
                case KeyCode.RightControl: return KeyCode.LeftControl;
                default: return key;
            }
        }

        public static void Route(ref MPN mpn)
        {
            if (mpn == MPN.accxxx && SecondaryRequested)
            {
                mpn = MPN.accvag;
                DoubleFrontHolePlugin.Log.LogDebug("Routed MPN.accxxx to MPN.accvag.");
            }
        }

        public static void Route(ref string tag)
        {
            if (IsPrimaryTag(tag) && SecondaryRequested)
            {
                // Keep the caller's casing style: accXXX -> accVag, accxxx -> accvag.
                var routed = char.IsUpper(tag[3]) ? "accVag" : SecondaryTag;
                DoubleFrontHolePlugin.Log.LogDebug("Routed tag '" + tag + "' to '" + routed + "'.");
                tag = routed;
            }
        }

        public static bool IsPrimaryTag(string tag)
        {
            return string.Equals(tag, PrimaryTag, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSecondaryTag(string tag)
        {
            return string.Equals(tag, SecondaryTag, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Primary or secondary slot name; menus made for either can go to the third slot.</summary>
        public static bool IsFrontHoleTag(string tag)
        {
            return IsPrimaryTag(tag) || IsSecondaryTag(tag);
        }
    }
}
