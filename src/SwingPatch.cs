using HarmonyLib;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// After the game simulates and poses a slot's legacy (TBoneHair) chains, exaggerate the swing of
    /// chains in either front-hole slot (accVag or accXXX), so it works for both accessories.
    /// </summary>
    [HarmonyPatch(typeof(TBoneHair_), "Update")]
    internal static class SwingPatch
    {
        private static void Postfix(TBoneHair_ __instance)
        {
            var slot = __instance.bodyskin;
            if (slot == null || !DoubleFrontHolePlugin.SwingEnabled
                || (slot.SlotId != TBody.SlotID.accVag && slot.SlotId != TBody.SlotID.accXXX))
            {
                return;
            }

            SwingAmplifier.Apply(__instance, DoubleFrontHolePlugin.SwingStrength, DoubleFrontHolePlugin.SwingTaper);
        }
    }
}
