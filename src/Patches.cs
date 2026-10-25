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
            if (ExtraSlot.Loading)
            {
                cd = MenuSlotRewriter.PrimaryToExtra(cd, ExtraSlot.SlotName);
            }
            else if (mp != null && SlotRouter.IsSecondaryTag(mp.name))
            {
                cd = MenuSlotRewriter.PrimaryToSecondary(cd);
            }
        }
    }

    /// <summary>
    /// Third-slot key (default Alt) + click on a front-hole item: equip or remove it in the third
    /// slot instead of changing the primary accxxx property.
    /// </summary>
    [HarmonyPatch(typeof(Maid), "SetProp", new[] { typeof(MPN), typeof(string), typeof(int), typeof(bool), typeof(bool) })]
    internal static class ThirdSlotMpnPatch
    {
        private static bool Prefix(Maid __instance, MPN __0, string __1, bool __3)
        {
            return !(__0 == MPN.accxxx && ThirdSlotSelect.Handle(__instance, __1, __3));
        }
    }

    [HarmonyPatch(typeof(Maid), "SetProp", new[] { typeof(string), typeof(string), typeof(int), typeof(bool), typeof(bool) })]
    internal static class ThirdSlotTagPatch
    {
        private static bool Prefix(Maid __instance, string __0, string __1, bool __3)
        {
            return !(SlotRouter.IsPrimaryTag(__0) && ThirdSlotSelect.Handle(__instance, __1, __3));
        }
    }

    internal static class ThirdSlotSelect
    {
        /// <summary>Returns true when the call was taken over and the original must be skipped.</summary>
        public static bool Handle(Maid maid, string filename, bool temp)
        {
            if (!SlotRouter.ThirdRequested || string.IsNullOrEmpty(filename))
            {
                return false;
            }

            // Temporary (preview) changes are dropped rather than applied to the primary slot.
            if (!temp)
            {
                ExtraSlot.OnSelected(maid, filename);
            }

            return true;
        }
    }

    /// <summary>
    /// Body calls that name a slot directly, or fall back to the menu category
    /// (accxxx) when the menu names none. Only active while the third slot loads.
    /// </summary>
    [HarmonyPatch]
    internal static class ExtraSlotBodyPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TBody), "AddItem", new[] { typeof(MPN), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool), typeof(int) });
            yield return AccessTools.Method(typeof(TBody), "DelItem", new[] { typeof(MPN), typeof(string) });
        }

        private static void Prefix(ref string __1)
        {
            ExtraSlot.RedirectSlotName(ref __1);
        }
    }

    /// <summary>The node hide/show menu commands use the menu category as the slot.</summary>
    [HarmonyPatch]
    internal static class ExtraSlotNodePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(TBody), "SetVisibleNodeSlot", new[] { typeof(string), typeof(bool), typeof(string) });
            yield return AccessTools.Method(typeof(TBody), "SetVisibleNodeSlotParts", new[] { typeof(string), typeof(string), typeof(bool), typeof(string) });
        }

        private static void Prefix(ref string __0)
        {
            ExtraSlot.RedirectSlotName(ref __0);
        }
    }
}
