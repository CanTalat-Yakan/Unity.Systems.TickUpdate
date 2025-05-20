using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace UnityEssentials
{
    public static partial class TickUpdate
    {
        public static event Action<float> OnTick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize() =>
            PlayerLoopHook.Add<Update>(Tick);

        private static void Tick()
        {
            OnTick?.Invoke(Time.deltaTime);

            if (!Application.isPlaying)
                Clear();
        }

        public static void Clear()
        {
            PlayerLoopHook.Remove<Update>(Tick);

            OnTick = null;

            s_tickGroups.Clear();
            s_groupsToRemove.Clear();
        }
    }

    public static partial class TickUpdate
    {
        private class TickGroup
        {
            public readonly int TicksPerSecond;
            public readonly float SecondsPerTick;
            public readonly List<Action> Actions = new();
            public float AccumulatedTime;
            public int CurrentActionIndex;

            public TickGroup(int ticksPerSecond)
            {
                TicksPerSecond = ticksPerSecond;
                SecondsPerTick = 1f / ticksPerSecond;
            }
        }

        private static readonly Dictionary<int, TickGroup> s_tickGroups = new();
        private static readonly List<int> s_groupsToRemove = new();

        public static void Register(int ticksPerSecond, Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (ticksPerSecond <= 0) throw new ArgumentException("Ticks per second must be positive", nameof(ticksPerSecond));

            if (!s_tickGroups.TryGetValue(ticksPerSecond, out var tickGroup))
            {
                tickGroup = new TickGroup(ticksPerSecond);
                s_tickGroups.Add(ticksPerSecond, tickGroup);
            }

            if (!tickGroup.Actions.Contains(action))
                tickGroup.Actions.Add(action);
        }

        public static void Unregister(int ticksPerSecond, Action action)
        {
            if (s_tickGroups.TryGetValue(ticksPerSecond, out var tickGroup))
            {
                tickGroup.Actions.Remove(action);

                // Mark empty groups for removal
                if (tickGroup.Actions.Count == 0)
                    s_groupsToRemove.Add(ticksPerSecond);
            }
        }

        public static void Update(float deltaTime)
        {
            // First clean up empty groups
            foreach (var ticksPerSecond in s_groupsToRemove)
                s_tickGroups.Remove(ticksPerSecond);
            s_groupsToRemove.Clear();

            foreach (var group in s_tickGroups)
            {
                var tickGroup = group.Value;
                tickGroup.AccumulatedTime += deltaTime;

                // Determine how many ticks should have occurred
                int ticksToProcess = (int)(tickGroup.AccumulatedTime / tickGroup.SecondsPerTick);
                if (ticksToProcess <= 0) continue;

                // Calculate how much time we're accounting for with these ticks
                float accountedTime = ticksToProcess * tickGroup.SecondsPerTick;
                tickGroup.AccumulatedTime -= accountedTime;

                // Spread the actions across the ticks
                if (tickGroup.Actions.Count > 0)
                {
                    int actionsPerTick = Math.Max(1, tickGroup.Actions.Count / ticksToProcess);

                    for (int i = 0; i < ticksToProcess; i++)
                    {
                        int actionsThisTick = Math.Min(actionsPerTick, tickGroup.Actions.Count - tickGroup.CurrentActionIndex);

                        for (int j = 0; j < actionsThisTick; j++)
                        {
                            try { tickGroup.Actions[tickGroup.CurrentActionIndex]?.Invoke(); }
                            catch (Exception ex) { Debug.Log($"Error executing tick action: {ex}"); }

                            tickGroup.CurrentActionIndex++;
                            if (tickGroup.CurrentActionIndex >= tickGroup.Actions.Count)
                                tickGroup.CurrentActionIndex = 0;
                        }
                    }
                }
            }
        }
    }
}