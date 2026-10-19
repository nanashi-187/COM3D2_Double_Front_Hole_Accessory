using BepInEx;
using BepInEx.Configuration;
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
        public const string Name = "COM3D2 Double Front Whole Accessory";
        public const string Version = "0.4.0";

        internal static ManualLogSource Log;

        private static ConfigEntry<bool> _swingEnabled;
        private static ConfigEntry<float> _swingStrength;
        private static ConfigEntry<float> _swingTaper;

        // Read every frame, so changes made in-game (e.g. through ConfigurationManager) apply at once.
        internal static bool SwingEnabled
        {
            get { return _swingEnabled != null && _swingEnabled.Value; }
        }

        internal static float SwingStrength
        {
            get { return _swingStrength != null ? _swingStrength.Value : 1f; }
        }

        internal static float SwingTaper
        {
            get { return _swingTaper != null ? _swingTaper.Value : 0.5f; }
        }

        private void Awake()
        {
            Log = Logger;

            _swingEnabled = Config.Bind("Physics", "Swing Enabled", true,
                "Apply Swing Strength and Swing Taper to chains of front-hole accessories (both slots).");
            _swingStrength = Config.Bind("Physics", "Swing Strength", 1f, new ConfigDescription(
                "Experimental. Multiplies how far the chains of front-hole accessories swing away from their "
                + "settled pose. 1 = the game's own motion, below 1 dampens it. Only legacy (TBoneHair) rigs, "
                + "i.e. bones named _yure_soft_ / _yure_hard_ / _yure_, are affected.",
                new AcceptableValueRange<float>(0.5f, 4f)));
            _swingTaper = Config.Bind("Physics", "Swing Taper", 0.5f, new ConfigDescription(
                "How the extra swing is spread along a chain. The total stays the same; higher values move it "
                + "from the top of the chain (under the most tension from a pendant) to the lowest joints. "
                + "0 = equal at every joint, 0.5 = linear ramp, 1 = quadratic, 3 = almost all at the last joint.",
                new AcceptableValueRange<float>(0f, SwingAmplifier.MaxTaper)));

            var harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(MpnSlotPatch));
            harmony.PatchAll(typeof(TagSlotPatch));
            harmony.PatchAll(typeof(MenuScriptPatch));
            harmony.PatchAll(typeof(SwingPatch));

            Log.LogInfo("Loaded. Normal click equips primary accessory; hold Ctrl while clicking front-hole accessories to equip the secondary slot.");
        }

        private void Update()
        {
            if (Time.frameCount % 300 == 0)
            {
                SwingAmplifier.PruneStale();
            }
        }
    }
}
