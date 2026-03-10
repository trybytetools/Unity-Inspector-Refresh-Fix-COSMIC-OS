# CosmicInspectorFix

A one-file Unity Editor script that fixes the Inspector panel not updating when selecting objects in the Hierarchy, when running Unity on the **COSMIC desktop environment** (Pop!_OS, CachyOS, etc).

---

## The Problem

On COSMIC, the Inspector freezes on whatever object was last selected. Clicking a different object in the Hierarchy does nothing — the only workarounds are pressing the lock/unlock button or hitting Ctrl+S.

The root cause is that COSMIC doesn't send the window-activation event Unity relies on to fire its internal `selectionChanged` callback. Unity's C++ side registers the click correctly, but the C# layer — and therefore the InspectorWindow — never gets notified.

## The Fix

The script polls `Selection.activeInstanceID` every editor tick. When it detects a silent selection change it calls `ActiveEditorTracker.sharedTracker.ForceRebuild()` and `InspectorWindow.RepaintAllInspectors()`, which is exactly what the lock button triggers internally.

## Installation

1. In your Unity project, create an `Editor` folder inside `Assets` if one doesn't already exist:
   ```
   Assets/Editor/
   ```

2. Drop `CosmicInspectorFix.cs` into it:
   ```
   Assets/Editor/CosmicInspectorFix.cs
   ```

3. Unity will recompile automatically. You'll see this in the Console when it's active:
   ```
   [CosmicInspectorFix] v4 active.
   ```

That's it. No packages, no settings, no editor restart.

> **Note:** This is an Editor-only script. It has zero effect on your builds.

## Compatibility

| Unity Version | Status |
|---|---|
| 6000.3.10f1 | ✅ Confirmed working |

Tested on Pop!_OS 24.04 with COSMIC desktop. If it works on your setup, feel free to open an issue or PR to update the table.

## Related

- [COSMIC upstream bug report](https://github.com/pop-os/cosmic-epoch/issues/2311)
