using System;
using UnityEngine;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// Decides whether a front-hole accessory operation targets the primary
    /// slot (MPN.accxxx) or the secondary slot (MPN.accvag).
    /// </summary>
    internal static class SlotRouter
    {
        private const string PrimaryTag = "accxxx";
        private const string SecondaryTag = "accvag";

        private static bool SecondaryRequested
        {
            get { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl); }
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
    }
}
