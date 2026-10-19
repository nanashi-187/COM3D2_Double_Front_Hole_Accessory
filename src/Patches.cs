using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>Maid property calls whose first argument is an MPN.</summary>
    [HarmonyPatch]
    internal static class MpnSlotPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Maid), "SetProp", new[] { typeof(MPN), typeof(string), typeof(int), typeof(bool), typeof(bool) });
            yield return AccessTools.Method(typeof(Maid), "SetProp", new[] { typeof(MPN), typeof(int), typeof(bool) });
            yield return AccessTools.Method(typeof(Maid), "DelProp", new[] { typeof(MPN), typeof(bool) });
            yield return AccessTools.Method(typeof(Maid), "ResetProp", new[] { typeof(MPN), typeof(bool) });
        }

        private static void Prefix(ref MPN __0)
        {
            SlotRouter.Route(ref __0);
        }
    }

    /// <summary>Maid property calls whose first argument is a slot tag such as "accxxx".</summary>
    [HarmonyPatch]
    internal static class TagSlotPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Maid), "SetProp", new[] { typeof(string), typeof(string), typeof(int), typeof(bool), typeof(bool) });
            yield return AccessTools.Method(typeof(Maid), "SetProp", new[] { typeof(string), typeof(int), typeof(bool) });
            yield return AccessTools.Method(typeof(Maid), "ResetProp", new[] { typeof(string), typeof(bool) });
        }

        private static void Prefix(ref string __0)
        {
            SlotRouter.Route(ref __0);
        }
    }

    /// <summary>
    /// Menu scripts can name their own slot. When a menu is being loaded into
    /// the secondary slot, rewrite that embedded name so it stays there.
    /// </summary>
    [HarmonyPatch(typeof(Menu), "ProcScriptBin", new[] { typeof(Maid), typeof(byte[]), typeof(MaidProp), typeof(bool), typeof(SubProp) })]
    internal static class MenuScriptPatch
    {
        private static void Prefix(ref byte[] cd, MaidProp mp)
        {
            if (mp != null && SlotRouter.IsSecondaryTag(mp.name))
            {
                cd = MenuSlotRewriter.PrimaryToSecondary(cd);
            }
        }
    }
}
