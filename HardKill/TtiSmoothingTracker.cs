using NOCS.Config;
using NOCS.Core;
using UnityEngine;

namespace NOCS.HardKill
{
    internal static class TtiSmoothingTracker
    {
        private const int MaxSlots = 16;

        private struct TrackSlot
        {
            internal PersistentID MissileId;
            internal float SmoothedTti;
            internal bool Active;
            internal bool HasState;
            internal int LastFilterFrame;
        }

        private static readonly TrackSlot[] Slots = new TrackSlot[MaxSlots];
        private static int _lastPruneFrame = -1;

        internal static void Reset()
        {
            for (int i = 0; i < MaxSlots; i++)
                Slots[i].Active = false;
        }

        internal static void EnsurePruned()
        {
            int frame = Time.frameCount;
            if (frame == _lastPruneFrame)
                return;

            _lastPruneFrame = frame;
            PruneInvalid();
        }

        internal static bool TryFilter(
            PersistentID missileId,
            float rawTti,
            float dt,
            out float smoothedTti,
            out bool impact)
        {
            smoothedTti = 0f;
            impact = false;
            if (!missileId.IsValid)
                return false;

            if (rawTti < 0f || float.IsNaN(rawTti) || float.IsInfinity(rawTti))
                return false;

            float alpha = Mathf.Clamp(NocsConfigCache.TtiSmoothingFactor, 0.01f, 1f);
            float step = dt > 0f ? dt : Time.deltaTime;
            if (step <= 0f)
                step = 0.016f;

            int slotIndex = FindSlot(missileId);
            if (slotIndex < 0)
                slotIndex = AllocSlot(missileId);

            if (slotIndex < 0)
            {
                smoothedTti = Mathf.Max(0f, rawTti);
                impact = smoothedTti <= 0f;
                return true;
            }

            ref TrackSlot slot = ref Slots[slotIndex];
            int frame = Time.frameCount;
            if (slot.HasState && slot.LastFilterFrame == frame)
            {
                smoothedTti = Mathf.Max(0f, slot.SmoothedTti);
                impact = smoothedTti <= 0f;
                return true;
            }

            if (!slot.HasState)
            {
                slot.SmoothedTti = rawTti;
                slot.HasState = true;
            }
            else
            {
                float predicted = slot.SmoothedTti - step;
                slot.SmoothedTti = Mathf.Lerp(predicted, rawTti, alpha);
            }

            slot.LastFilterFrame = frame;
            smoothedTti = Mathf.Max(0f, slot.SmoothedTti);
            impact = smoothedTti <= 0f;
            return true;
        }

        private static void PruneInvalid()
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!Slots[i].Active)
                    continue;

                PersistentID id = Slots[i].MissileId;
                if (!id.IsValid)
                {
                    Slots[i].Active = false;
                    continue;
                }

                if (!NocsUnitLookup.TryGetLiveUnit(id, out Unit unit))
                {
                    Slots[i].Active = false;
                }
            }
        }

        private static int FindSlot(PersistentID missileId)
        {
            for (int i = 0; i < MaxSlots; i++)
            {
                if (Slots[i].Active && Slots[i].MissileId == missileId)
                    return i;
            }

            return -1;
        }

        private static int AllocSlot(PersistentID missileId)
        {
            int free = -1;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (!Slots[i].Active)
                {
                    free = i;
                    break;
                }
            }

            if (free < 0)
            {
                free = 0;
                Slots[free].Active = false;
            }

            Slots[free].MissileId = missileId;
            Slots[free].SmoothedTti = 0f;
            Slots[free].HasState = false;
            Slots[free].LastFilterFrame = -1;
            Slots[free].Active = true;
            return free;
        }
    }
}
