using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.DoubleFrontHole
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class DoubleFrontHolePlugin : BaseUnityPlugin
    {
        // GUID and DLL name stay fixed so existing installs upgrade in place.
        public const string Guid = "local.com3d2.doublefronthole";
        public const string Name = "COM3D2 Double Front Hole Accessory";
        public const string Version = "0.6.0";

        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;

            var enableThirdSlot = Config.Bind("General", "EnableThirdSlot", true,
                "Add a third front-hole slot. Takes effect after a game restart.").Value;
            SlotRouter.ThirdSlotKey = Config.Bind("General", "ThirdSlotKey", KeyCode.LeftAlt,
                "Key to hold while clicking a front-hole item to use the third slot (left and right count the same). "
                + "Avoid Shift and Ctrl+Shift if ShiftClickExplorer is installed: it opens the menu folder/file on those clicks.").Value;

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(MpnSlotPatch));
            harmony.PatchAll(typeof(TagSlotPatch));
            harmony.PatchAll(typeof(MenuScriptPatch));

            if (enableThirdSlot)
            {
                ExtraSlot.Install();
                if (ExtraSlot.Installed)
                {
                    harmony.PatchAll(typeof(ThirdSlotMpnPatch));
                    harmony.PatchAll(typeof(ThirdSlotTagPatch));
                    harmony.PatchAll(typeof(ExtraSlotBodyPatch));
                    harmony.PatchAll(typeof(ExtraSlotNodePatch));
                }
            }

            Log.LogInfo("Loaded. Normal click equips primary accessory; hold Ctrl while clicking front-hole accessories to equip the secondary slot"
                + (ExtraSlot.Installed ? ", " + SlotRouter.ThirdSlotKey + " for the third slot." : "."));
        }

        private void Update()
        {
            ExtraSlot.Poll();
        }
    }
}
