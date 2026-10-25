using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using CM3D2.ExternalSaveData.Managed;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// Per-maid menu file of the extra slot, saved through ExternalSaveData
    /// (an .exsave.xml next to the game save). Without ExternalSaveData the
    /// choice is kept for the current session only.
    /// </summary>
    internal static class ExtraSlotStore
    {
        private const string PluginKey = "COM3D2.DoubleFrontHole";
        private const string PropName = "frontHole3";

        private static readonly Dictionary<Maid, string> SessionOnly = new Dictionary<Maid, string>();
        private static bool available = true;

        public static string Get(Maid maid)
        {
            if (available)
            {
                try
                {
                    return GetSaved(maid);
                }
                catch (Exception ex)
                {
                    Disable(ex);
                }
            }

            string value;
            return SessionOnly.TryGetValue(maid, out value) ? value : null;
        }

        public static void Set(Maid maid, string filename)
        {
            if (available)
            {
                try
                {
                    SetSaved(maid, filename);
                    return;
                }
                catch (Exception ex)
                {
                    Disable(ex);
                }
            }

            SessionOnly[maid] = filename;
        }

        public static void Remove(Maid maid)
        {
            if (available)
            {
                try
                {
                    RemoveSaved(maid);
                    return;
                }
                catch (Exception ex)
                {
                    Disable(ex);
                }
            }

            SessionOnly.Remove(maid);
        }

        private static void Disable(Exception ex)
        {
            available = false;
            DoubleFrontHolePlugin.Log.LogWarning("ExternalSaveData is not usable, the third front-hole accessory will not be saved: " + ex.Message);
        }

        // ExternalSaveData is referenced only from these methods so a missing
        // assembly fails when they are compiled, inside the callers' try blocks.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetSaved(Maid maid)
        {
            return ExSaveData.Get(maid, PluginKey, PropName, null);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetSaved(Maid maid, string filename)
        {
            ExSaveData.Set(maid, PluginKey, PropName, filename, true);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void RemoveSaved(Maid maid)
        {
            ExSaveData.Remove(maid, PluginKey, PropName);
        }
    }
}
