using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace UnityEssentials
{
    /// <summary>
    /// Provides functionality for managing and invoking tick updates during the Unity game loop.
    /// </summary>
    /// <remarks>The <see cref="TickUpdate"/> class allows developers to hook into the Unity game loop by
    /// subscribing to the <see cref="OnTick"/> event. This event is triggered every frame during the <see
    /// cref="UnityEngine.PlayerLoop.Update"/> phase, passing the time elapsed since the last frame.</remarks>
    public static partial class TickUpdate
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize() =>
            PlayerLoopHook.Add<Update>(Tick);

        private static void Tick()
        {
            Update(Time.deltaTime);

            if (!Application.isPlaying)
                Clear();
        }

        public static void Clear()
        {
            PlayerLoopHook.Remove<Update>(Tick);

            s_tickGroups.Clear();
            s_groupsToRemove.Clear();
        }
    }

    public static partial class TickUpdate
    {
        /// <summary>
        /// Represents a group of actions that are executed at a specified tick rate,
        /// distributing their execution over multiple frames to help balance workload and avoid frame time spikes.
        /// </summary>
        /// <remarks>
        /// This class manages and executes a collection of actions at a consistent interval,
        /// determined by the specified number of ticks per second. It tracks accumulated time and the current action index,
        /// ensuring that actions are scheduled in a way that spreads processing across frames.
        /// This helps prevent situations where too many actions are executed in a single frame,
        /// promoting smoother and more consistent frame times.
        /// </remarks>
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

        /// <summary>
        /// Registers an action to be executed at a specified frequency, defined in ticks per second.
        /// </summary>
        /// <remarks>If the specified frequency (ticks per second) does not already exist, a new tick
        /// group is created. The action will only be added to the tick group if it is not already registered.</remarks>
        /// <param name="ticksPerSecond">The number of ticks per second at which the action should be executed. Must be a positive integer.</param>
        /// <param name="action">The action to be executed. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="action"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="ticksPerSecond"/> is less than or equal to zero.</exception>
        public static void Register(int ticksPerSecond, Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (ticksPerSecond <= 0)
                throw new ArgumentException("Ticks per second must be positive", nameof(ticksPerSecond));

            if (!s_tickGroups.TryGetValue(ticksPerSecond, out var tickGroup))
            {
                tickGroup = new TickGroup(ticksPerSecond);
                s_tickGroups.Add(ticksPerSecond, tickGroup);
            }

            if (!tickGroup.Actions.Contains(action))
                tickGroup.Actions.Add(action);
        }

        /// <summary>
        /// Unregisters an action from being executed at the specified tick rate.
        /// </summary>
        /// <remarks>If the specified action is not found in the group associated with the given tick
        /// rate, this method has no effect. If the group of actions for the specified tick rate becomes empty after the
        /// action is removed, the group is marked for removal.</remarks>
        /// <param name="ticksPerSecond">The frequency, in ticks per second, at which the action was registered to execute.</param>
        /// <param name="action">The action to unregister. Must not be <see langword="null"/>.</param>
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
        /// <summary>
        /// Updates the state of all registered tick groups based on the elapsed time since the last update,
        /// distributing the execution of actions across multiple frames to avoid overloading any single frame.
        /// </summary>
        /// <remarks>
        /// This method processes tick groups by determining how many ticks should occur based on
        /// the elapsed time and the tick frequency of each group. For each tick, it executes a subset of the actions
        /// registered with the group, spreading the workload over time to maintain consistent frame times and
        /// prevent spikes caused by executing too many actions in a single frame.
        /// Tick groups that are marked for removal are cleaned up at the start of the method.
        /// </remarks>
        /// <param name="deltaTime">The time, in seconds, that has elapsed since the last update.</param>
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