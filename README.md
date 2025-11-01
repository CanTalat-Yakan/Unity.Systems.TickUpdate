# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Tick Update

> Quick overview: Frame-synchronized tick scheduler. Register actions at N ticks/second; work is distributed across frames via round‑robin to avoid spikes. Hooks itself into PlayerLoop.Update and cleans up automatically in the Editor when exiting Play Mode.

A tiny runtime scheduler for periodic work. You register actions at a frequency (ticks per second). The system groups actions by frequency, accumulates deltaTime, and executes the right number of ticks each frame. Within each group, actions are advanced in a round‑robin manner and spread across the ticks to smooth frame time.

![screenshot](Documentation/Screenshot.png)

## Features
- Register actions by frequency
  - `TickUpdate.Register(ticksPerSecond, Action)` and `Unregister(...)`
  - Duplicate registrations are ignored
- Automatic PlayerLoop integration
  - Hooks into `UnityEngine.PlayerLoop.Update` on load
  - Clears itself when not playing in the Editor
- Workload smoothing
  - Accumulates `deltaTime`, computes how many ticks to process, and spreads actions across those ticks
  - Round‑robin iteration per group; at least one action per tick
- Safe execution and maintenance
  - Exceptions in actions are logged (don’t break the loop)
  - Empty groups are removed automatically
- Manual drive option
  - Call `TickUpdate.Update(deltaTime)` yourself for tests or custom loops

## Requirements
- Unity 6000.0+
- Runtime module; no external dependencies

## Usage
1) Register and Unregister in a MonoBehaviour
```csharp
using UnityEngine;
using UnityEssentials;

public class SpawnSystem : MonoBehaviour
{
    void OnEnable()
    {
        // 10 ticks/second ≈ every 0.1s
        TickUpdate.Register(10, DoSpawn);
    }

    void OnDisable()
    {
        TickUpdate.Unregister(10, DoSpawn);
    }

    void DoSpawn()
    {
        // Your periodic work
        Debug.Log("Spawn tick");
    }
}
```

2) Multiple rates in the same scene
```csharp
TickUpdate.Register(1,  () => Debug.Log("Once per second"));
TickUpdate.Register(2,  () => Debug.Log("Twice per second"));
TickUpdate.Register(10, () => Debug.Log("Ten times per second"));
```

3) Manually drive in tests or custom loops
```csharp
// Advance time: process however many ticks fit into this delta
TickUpdate.Update(Time.deltaTime);
```

Notes for usage
- Always `Unregister` when your object is disabled/destroyed
- One action can be registered to multiple frequencies if desired (register separately)
- Duplicate registers for the same `(ticksPerSecond, action)` are ignored

## How It Works
- Initialization
  - `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` adds a `Tick` callback to `PlayerLoop.Update` using an internal `PlayerLoopHook`
- Per‑frame processing
  - `Tick()` calls `Update(Time.deltaTime)`; in the Editor, if `Application.isPlaying` is false, the system `Clear()`s itself
- Scheduling
  - Actions are grouped by `ticksPerSecond`
  - Each group accumulates `deltaTime` and computes how many ticks occurred (`ticks = floor(accumulated / secondsPerTick)`)
  - For each tick, a slice of actions is executed using round‑robin iteration (`CurrentActionIndex` advances and wraps)
  - At least one action runs per tick; exceptions are caught and logged via `Debug.Log`
- Cleanup
  - Groups that become empty are removed; `Clear()` removes all state and the PlayerLoop hook

## Notes and Limitations
- Threading: not thread‑safe. Register/unregister and execute on the main thread
- Ordering: per‑group order is round‑robin; global ordering across groups/frequencies is unspecified
- Backlog: a long frame can cause multiple ticks to process next frame; the scheduler still caps to at least one action per tick and distributes work
- Validation: `Register` throws on `null` action or non‑positive `ticksPerSecond`
- Exceptions: user action exceptions are logged; they do not stop the scheduler
- Editor lifecycle: the system clears itself when the Editor is not in Play Mode; you can call `TickUpdate.Clear()` to reset manually

## Files in This Package
- `Runtime/TickUpdate.cs` – Core scheduler, PlayerLoop hook, register/unregister/update/clear
- `Runtime/UnityEssentials.TickUpdate.asmdef` – Runtime assembly definition
- `Tests/TickUpdateTests.cs` – Unit tests for registration, execution, and cleanup

## Tags
unity, tick, update, scheduler, throttle, rate-limit, playerloop, runtime, systems
