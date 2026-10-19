using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.DoubleFrontHole
{
    // Exaggerates the swing of legacy (TBoneHair_) chains on front-hole accessories.
    //
    // COM3D2's chain constants are fixed in game code, so short accessories swing very little. After
    // the game has posed a chain for the frame, each chain bone's rotation away from its recent average
    // (its settled pose) is multiplied by the configured strength. Only the swing is amplified: the
    // settled hang still comes from the physics, and the simulation itself is never modified, since
    // THair1.Proc re-poses every chain bone from the simulated points on each physics update.
    internal static class SwingAmplifier
    {
        private const float SettleSeconds = 0.4f;
        private const float MaxSwingDegrees = 120f;
        private const int StaleFrames = 600;
        private const int MaxCollisionRetries = 4;
        private const float DepthTolerance = 0.0005f;

        // Pendants hang wider than the chain's centre line (the heart is ~1.8 cm either side), so chain
        // points are tested as small spheres rather than as points.
        private const float AccessoryRadius = 0.01f;

        private static readonly List<Transform> ChainBones = new List<Transform>();
        private static readonly List<float> Shares = new List<float>();

        public const float MaxTaper = 3f;

        // Plain reflection rather than AccessTools.FieldRef, which needs C# 7 (this repo stays C# 5).
        private static readonly FieldInfo ChainsField = AccessTools.Field(typeof(TBoneHair_), "hair1list");
        private static readonly FieldInfo EnabledField = AccessTools.Field(typeof(TBoneHair_), "m_bEnable");

        private static List<THair1> Chains(TBoneHair_ hair)
        {
            return (List<THair1>)ChainsField.GetValue(hair);
        }

        private static bool Enabled(TBoneHair_ hair)
        {
            return (bool)EnabledField.GetValue(hair);
        }

        private sealed class BoneState
        {
            public Transform Bone;
            public Quaternion Settled;
            public int LastFrame;
        }

        private static readonly Dictionary<int, BoneState> Bones = new Dictionary<int, BoneState>();
        private static readonly List<int> StaleIds = new List<int>();

        // Called right after TBoneHair_.Update, i.e. just after THair1.Proc posed the chain bones.
        public static void Apply(TBoneHair_ hair, float strength, float taper)
        {
            if (Mathf.Approximately(strength, 1f) || !SimulatesThisFrame(hair))
            {
                return;
            }

            var chains = Chains(hair);
            if (chains == null)
            {
                return;
            }

            var spheres = hair.bodyhit.spherelist;
            for (var i = 0; i < chains.Count; i++)
            {
                AmplifyChain(chains[i], spheres, strength, taper);
            }
        }

        public static void PruneStale()
        {
            if (Bones.Count == 0)
            {
                return;
            }

            StaleIds.Clear();
            foreach (var pair in Bones)
            {
                if (pair.Value.Bone == null || Time.frameCount - pair.Value.LastFrame > StaleFrames)
                {
                    StaleIds.Add(pair.Key);
                }
            }

            for (var i = 0; i < StaleIds.Count; i++)
            {
                Bones.Remove(StaleIds[i]);
            }
        }

        // Same early-outs as TBoneHair_.Update: when it returned without simulating, the bones still
        // hold last frame's amplified pose and must not be amplified again.
        private static bool SimulatesThisFrame(TBoneHair_ hair)
        {
            return hair != null && hair.bodyhit != null && Enabled(hair);
        }

        // THair1.Proc poses hplist.Count / HairDiv bones, starting at root and following child 0.
        private static void AmplifyChain(THair1 chain, List<THitSphere> spheres, float strength, float taper)
        {
            if (chain == null || chain.boDBL || chain.root == null || chain.HairDiv <= 0 || chain.hplist == null)
            {
                return;
            }

            // The posed bones plus the end marker below the last one; every point below a bone moves
            // when that bone's swing is amplified.
            ChainBones.Clear();
            var count = chain.hplist.Count / chain.HairDiv;
            var bone = chain.root;
            for (var i = 0; i <= count && bone != null; i++)
            {
                ChainBones.Add(bone);
                bone = bone.childCount > 0 ? bone.GetChild(0) : null;
            }

            var settle = 1f - Mathf.Exp(-Time.deltaTime / SettleSeconds);
            var posed = Mathf.Min(count, ChainBones.Count);
            ComputeShares(posed, taper);
            for (var i = 0; i < posed; i++)
            {
                Amplify(i, settle, 1f + (strength - 1f) * Shares[i], spheres);
            }
        }

        // A chain carrying a pendant is under the most tension at the top, so the extra swing is moved
        // from the root towards the last joint. Shares follow ((i + 1) / n)^(2 * taper) and are scaled
        // to average 1, so the taper redistributes the extra swing without reducing it overall.
        private static void ComputeShares(int posed, float taper)
        {
            Shares.Clear();
            var exponent = 2f * Mathf.Clamp(taper, 0f, MaxTaper);
            var sum = 0f;
            for (var i = 0; i < posed; i++)
            {
                var share = Mathf.Pow((i + 1) / (float)posed, exponent);
                Shares.Add(share);
                sum += share;
            }

            var scale = sum > 0f ? posed / sum : 1f;
            for (var i = 0; i < posed; i++)
            {
                Shares[i] *= scale;
            }
        }

        private static void Amplify(int index, float settle, float jointStrength, List<THitSphere> spheres)
        {
            var bone = ChainBones[index];
            var current = bone.localRotation;
            BoneState state;
            if (!Bones.TryGetValue(bone.GetInstanceID(), out state) || state.Bone != bone)
            {
                state = new BoneState { Bone = bone, Settled = current, LastFrame = -1 };
                Bones[bone.GetInstanceID()] = state;
            }

            // The game can update a body more than once per frame; advance the average only once.
            if (state.LastFrame != Time.frameCount)
            {
                state.Settled = Quaternion.Slerp(state.Settled, current, settle);
                state.LastFrame = Time.frameCount;
            }

            float angle;
            Vector3 axis;
            (Quaternion.Inverse(state.Settled) * current).ToAngleAxis(out angle, out axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }

            if (Mathf.Abs(angle) < 0.01f || float.IsNaN(axis.x))
            {
                return;
            }

            // Never push the chain deeper into the body-collision spheres than the physics pose is:
            // halve the extra swing until it stops making contact worse, or fall back to the physics pose.
            var physicsDepth = Depth(index, spheres);
            var strength = jointStrength;
            for (var attempt = 0; attempt <= MaxCollisionRetries; attempt++)
            {
                var amplified = Mathf.Clamp(angle * strength, -MaxSwingDegrees, MaxSwingDegrees);
                bone.localRotation = state.Settled * Quaternion.AngleAxis(amplified, axis);
                if (Depth(index, spheres) <= physicsDepth + DepthTolerance)
                {
                    return;
                }

                strength = 1f + (strength - 1f) * 0.5f;
            }

            bone.localRotation = current;
        }

        // Total depth inside the collision spheres of every chain point below the given bone. Radii are
        // scaled like TBodyHit.SphereMove_hair2 and padded by the accessory's own thickness.
        private static float Depth(int index, List<THitSphere> spheres)
        {
            if (spheres == null || spheres.Count == 0)
            {
                return 0f;
            }

            var scale = 1f + _TS.TestVal2 * 2f;
            var total = 0f;
            for (var p = index + 1; p < ChainBones.Count; p++)
            {
                var point = ChainBones[p].position;
                var deepest = 0f;
                for (var s = 0; s < spheres.Count; s++)
                {
                    var sphere = spheres[s];
                    var depth = sphere.len * scale + AccessoryRadius - (point - sphere.wv).magnitude;
                    if (depth > deepest)
                    {
                        deepest = depth;
                    }
                }

                total += deepest;
            }

            return total;
        }
    }
}
