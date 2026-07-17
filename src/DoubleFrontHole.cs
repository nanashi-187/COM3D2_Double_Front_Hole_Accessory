using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.DoubleFrontHole
{
    [BepInPlugin("local.com3d2.doublefronthole", "COM3D2 Double Front Whole Accessory", "0.3.1")]
    public sealed class DoubleFrontHolePlugin : BaseUnityPlugin
    {
        private static bool _routeNextAccXxxToSecondary;
        private static readonly byte[] AccXxxUpper = { (byte)'a', (byte)'c', (byte)'c', (byte)'X', (byte)'X', (byte)'X' };
        private static readonly byte[] AccVagUpper = { (byte)'a', (byte)'c', (byte)'c', (byte)'V', (byte)'a', (byte)'g' };
        private static readonly byte[] AccXxxLower = { (byte)'a', (byte)'c', (byte)'c', (byte)'x', (byte)'x', (byte)'x' };
        private static readonly byte[] AccVagLower = { (byte)'a', (byte)'c', (byte)'c', (byte)'v', (byte)'a', (byte)'g' };

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(DoubleFrontHolePlugin));
            Logger.LogInfo("Loaded. Normal click equips primary accessory; hold Ctrl while clicking front-hole accessories to equip the secondary slot.");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.SetProp), typeof(MPN), typeof(string), typeof(int), typeof(bool), typeof(bool))]
        private static void RouteMpnSetProp(ref MPN idx)
        {
            RouteMpn(ref idx);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.SetProp), typeof(MPN), typeof(int), typeof(bool))]
        private static void RouteMpnValueSetProp(ref MPN idx)
        {
            RouteMpn(ref idx);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.DelProp), typeof(MPN), typeof(bool))]
        private static void RouteMpnDelProp(ref MPN idx)
        {
            RouteMpn(ref idx);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.ResetProp), typeof(MPN), typeof(bool))]
        private static void RouteMpnResetProp(ref MPN idx)
        {
            RouteMpn(ref idx);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.SetProp), typeof(string), typeof(string), typeof(int), typeof(bool), typeof(bool))]
        private static void RouteStringSetProp(ref string tag)
        {
            RouteTag(ref tag);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.SetProp), typeof(string), typeof(int), typeof(bool))]
        private static void RouteStringValueSetProp(ref string tag)
        {
            RouteTag(ref tag);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Maid), nameof(Maid.ResetProp), typeof(string), typeof(bool))]
        private static void RouteStringResetProp(ref string mpn)
        {
            RouteTag(ref mpn);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Menu), "ProcScriptBin", typeof(Maid), typeof(byte[]), typeof(MaidProp), typeof(bool), typeof(SubProp))]
        private static void RewriteSecondaryMaidPropMenu(ref byte[] cd, MaidProp mp)
        {
            if (mp != null && mp.name != null && mp.name.Equals("accvag", StringComparison.OrdinalIgnoreCase))
            {
                cd = RewriteAccXxxToAccVag(cd);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Menu), "ProcScriptBin", typeof(Maid), typeof(byte[]), typeof(string), typeof(bool), typeof(SubProp))]
        private static void RewriteSecondaryFilenameMenu(ref byte[] cd)
        {
            if (_routeNextAccXxxToSecondary)
            {
                cd = RewriteAccXxxToAccVag(cd);
                _routeNextAccXxxToSecondary = false;
            }

        }

        private static bool CtrlHeld()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private static void RouteMpn(ref MPN idx)
        {
            if (idx == MPN.accxxx && CtrlHeld())
            {
                idx = MPN.accvag;
                _routeNextAccXxxToSecondary = true;
            }
        }

        private static void RouteTag(ref string tag)
        {
            if (tag != null && tag.Equals("accxxx", StringComparison.OrdinalIgnoreCase) && CtrlHeld())
            {
                tag = char.IsUpper(tag[3]) ? "accVag" : "accvag";
                _routeNextAccXxxToSecondary = true;
            }
        }

        private static byte[] RewriteAccXxxToAccVag(byte[] source)
        {
            if (source == null || source.Length == 0)
            {
                return source;
            }

            var copy = (byte[])source.Clone();
            ReplaceTokenAll(copy, AccXxxUpper, AccVagUpper);
            ReplaceTokenAll(copy, AccXxxLower, AccVagLower);
            return copy;
        }

        private static void ReplaceTokenAll(byte[] bytes, byte[] from, byte[] to)
        {
            for (var i = 0; i <= bytes.Length - from.Length; i++)
            {
                var match = true;
                for (var j = 0; j < from.Length; j++)
                {
                    if (bytes[i + j] != from[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    var prev = i > 0 ? bytes[i - 1] : (byte)0;
                    var next = i + from.Length < bytes.Length ? bytes[i + from.Length] : (byte)0;
                    if (IsTokenPart(prev) || IsTokenPart(next))
                    {
                        continue;
                    }

                    Buffer.BlockCopy(to, 0, bytes, i, to.Length);
                    i += from.Length - 1;
                }
            }
        }

        private static bool IsTokenPart(byte value)
        {
            return (value >= (byte)'0' && value <= (byte)'9')
                   || (value >= (byte)'A' && value <= (byte)'Z')
                   || (value >= (byte)'a' && value <= (byte)'z')
                   || value == (byte)'_';
        }
    }
}
