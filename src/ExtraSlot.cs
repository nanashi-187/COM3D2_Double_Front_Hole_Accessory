using System;
using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// The plugin-managed third front-hole slot. It is a new body slot appended
    /// to the game's slot table, not a MaidProp, so the game save never sees it.
    /// The chosen menu is kept per maid in ExternalSaveData and loaded again
    /// whenever the maid's body is rebuilt.
    /// </summary>
    internal static class ExtraSlot
    {
        public const string SlotName = "accXXX3";

        private const float PollInterval = 0.5f;

        // Set while this plugin loads a menu into the extra slot; the menu and
        // TBody patches use it to redirect the primary slot name.
        [ThreadStatic] private static bool loading;

        private static readonly Dictionary<Maid, Applied> AppliedByMaid = new Dictionary<Maid, Applied>();
        private static float nextPoll;

        public static bool Installed { get; private set; }

        public static bool Loading
        {
            get { return loading; }
        }

        private sealed class Applied
        {
            public TBody Body;
            public string File;
            public bool Loaded;
        }

        /// <summary>
        /// Inserts the slot triplet before "end" in TBody.m_strDefSlotName.
        /// TBody.Init copies that table into each new body, so this must run
        /// before any maid is created.
        /// </summary>
        public static void Install()
        {
            var field = typeof(TBody).GetField("m_strDefSlotName");
            var table = (string[])field.GetValue(null);
            if (Array.IndexOf(table, SlotName) >= 0)
            {
                Installed = true;
                return;
            }

            var end = Array.IndexOf(table, "end");
            if (end < 0 || end % 3 != 0)
            {
                DoubleFrontHolePlugin.Log.LogError("TBody slot table has an unexpected layout; third slot disabled.");
                return;
            }

            var extended = new string[table.Length + 3];
            Array.Copy(table, extended, end);
            extended[end] = SlotName;
            extended[end + 1] = "_ROOT_";
            extended[end + 2] = "Uwagi";
            Array.Copy(table, end, extended, end + 3, table.Length - end);
            field.SetValue(null, extended);

            Installed = Array.IndexOf((string[])field.GetValue(null), SlotName) >= 0;
            if (!Installed)
            {
                DoubleFrontHolePlugin.Log.LogError("Could not extend the TBody slot table; third slot disabled.");
                return;
            }

            if (TBody.hashSlotName != null)
            {
                DoubleFrontHolePlugin.Log.LogWarning("A maid body already exists; the third slot works only for bodies created from now on.");
            }

            DoubleFrontHolePlugin.Log.LogInfo("Added body slot " + SlotName + " (slot no. " + end / 3 + ").");
        }

        /// <summary>Handles a third-slot click (ThirdSlotKey held) on a front-hole item.</summary>
        public static void OnSelected(Maid maid, string filename)
        {
            if (filename.IndexOf("_del", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ExtraSlotStore.Remove(maid);
                Unload(maid);
                AppliedByMaid.Remove(maid);
                DoubleFrontHolePlugin.Log.LogInfo("Removed the third front-hole accessory.");
                return;
            }

            ExtraSlotStore.Set(maid, filename);
            Apply(maid, filename);
        }

        /// <summary>
        /// Called every frame; every half second loads the stored item into any
        /// active maid whose body lost it (new body, outfit rebuild, save load)
        /// and removes it from maids that no longer have one stored.
        /// </summary>
        public static void Poll()
        {
            if (!Installed || Time.realtimeSinceStartup < nextPoll)
            {
                return;
            }

            nextPoll = Time.realtimeSinceStartup + PollInterval;

            var game = GameMain.Instance;
            var characters = game != null ? game.CharacterMgr : null;
            if (characters == null)
            {
                return;
            }

            for (var i = 0; i < characters.GetMaidCount(); i++)
            {
                var maid = characters.GetMaid(i);
                if (maid != null)
                {
                    Sync(maid);
                }
            }
        }

        private static void Sync(Maid maid)
        {
            var body = maid.body0;
            if (body == null || !body.isLoadedBody || maid.IsBusy || !body.IsSlotNo(SlotName))
            {
                return;
            }

            var stored = ExtraSlotStore.Get(maid);
            var loaded = body.GetSlot(SlotName).obj != null;
            Applied applied;
            AppliedByMaid.TryGetValue(maid, out applied);

            if (string.IsNullOrEmpty(stored))
            {
                // Only this plugin ever fills the slot, so anything left there is stale.
                if (loaded)
                {
                    Unload(maid);
                }

                AppliedByMaid.Remove(maid);
                return;
            }

            // A menu that loads no model would never count as loaded, so only
            // reload when the item was there and has gone, not on every poll.
            if (applied == null || applied.Body != body || applied.File != stored || (applied.Loaded && !loaded))
            {
                Apply(maid, stored);
            }
        }

        private static void Apply(Maid maid, string filename)
        {
            var body = maid.body0;
            if (body == null || !body.IsSlotNo(SlotName))
            {
                return;
            }

            body.DelItem(MPN.accxxx, SlotName);

            var ok = false;
            loading = true;
            try
            {
                ok = Menu.ProcScript(maid, filename, false, null);
            }
            catch (Exception ex)
            {
                DoubleFrontHolePlugin.Log.LogError("Loading " + filename + " into " + SlotName + " failed: " + ex);
            }
            finally
            {
                loading = false;
            }

            body.MulTexProc();
            body.FixMaskFlag();
            body.FixVisibleFlag(false);

            var loaded = ok && body.GetSlot(SlotName).obj != null;
            AppliedByMaid[maid] = new Applied { Body = body, File = filename, Loaded = loaded };
            if (ok)
            {
                DoubleFrontHolePlugin.Log.LogInfo("Equipped " + filename + " in the third front-hole slot.");
            }
            else
            {
                DoubleFrontHolePlugin.Log.LogWarning("Could not open " + filename + " for the third front-hole slot.");
            }
        }

        private static void Unload(Maid maid)
        {
            var body = maid.body0;
            if (body == null || !body.IsSlotNo(SlotName))
            {
                return;
            }

            body.DelItem(MPN.accxxx, SlotName);
            body.FixMaskFlag();
            body.FixVisibleFlag(false);
        }

        /// <summary>
        /// While a menu is loading into the extra slot, maps the primary or
        /// secondary slot name to the extra one. Menus that name no slot use their category
        /// ("accxxx"), which also lands here.
        /// </summary>
        public static void RedirectSlotName(ref string slotname)
        {
            if (loading && SlotRouter.IsFrontHoleTag(slotname))
            {
                slotname = SlotName;
            }
        }
    }
}
