using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace COM3D2.DoubleFrontHole
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class DoubleFrontHolePlugin : BaseUnityPlugin
    {
        // GUID and DLL name stay fixed so existing installs upgrade in place.
        public const string Guid = "local.com3d2.doublefronthole";
        public const string Name = "COM3D2 Double Front Whole Accessory";
        public const string Version = "0.5.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(MpnSlotPatch));
            harmony.PatchAll(typeof(TagSlotPatch));
            harmony.PatchAll(typeof(MenuScriptPatch));

            Log.LogInfo("Loaded. Normal click equips primary accessory; hold Ctrl while clicking front-hole accessories to equip the secondary slot.");
        }
    }
}
