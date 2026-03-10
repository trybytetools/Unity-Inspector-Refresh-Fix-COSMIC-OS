// CosmicInspectorFix.cs  (v4 — final)
// Place in Assets/Editor/CosmicInspectorFix.cs
//
// ROOT CAUSE (confirmed via debug logging on Unity 6000.3.10f1 / COSMIC):
//   COSMIC does not send the window-activation event that triggers Unity's
//   managed selectionChanged callback. Selection.activeInstanceID updates
//   correctly on the C++ side, but InspectorWindow never receives the
//   notification to redraw.
//
// FIX (confirmed working via CosmicLockProbe.cs candidate testing):
//   Poll Selection.activeInstanceID each editor tick. On a silent change:
//     1. ActiveEditorTracker.sharedTracker.ForceRebuild()
//        Tells Unity's tracker to throw away its cached Editor objects and
//        rebuild them against the current selection. Public API, no reflection.
//     2. InspectorWindow.RepaintAllInspectors()  (via cached reflection)
//        Signals every open InspectorWindow to redraw immediately.
//
// Candidates tested and confirmed working: ForceRebuild, RefreshInspectors,
//   RepaintAllInspectors, rapid lock-toggle.
// Candidate confirmed NOT working: RebuildContentsContainers alone.
// Chosen combination is the least invasive — no lock state touched, no
//   instance methods, no deprecated APIs.
//
// Tested against Unity 6000.3.10f1 on Pop!_OS 24.04 with COSMIC desktop.

#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CosmicInspectorFix
{
    private static int        _lastKnownInstanceID;
    private static MethodInfo _repaintAllInspectors;

    static CosmicInspectorFix()
    {
        // Cache the static RepaintAllInspectors method once at load time.
        var inspectorType = typeof(Editor).Assembly
            .GetType("UnityEditor.InspectorWindow");

        if (inspectorType != null)
        {
            _repaintAllInspectors = inspectorType.GetMethod(
                "RepaintAllInspectors",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (_repaintAllInspectors == null)
            Debug.LogWarning("[CosmicInspectorFix] Could not find RepaintAllInspectors — will use ForceRebuild only.");

#pragma warning disable CS0618 // reading activeInstanceID is fine; we never write it
        _lastKnownInstanceID = Selection.activeInstanceID;
#pragma warning restore CS0618

        EditorApplication.update += PollAndSync;

        Debug.Log("[CosmicInspectorFix] v4 active.");
    }

    private static void PollAndSync()
    {
#pragma warning disable CS0618
        int currentID = Selection.activeInstanceID;
#pragma warning restore CS0618

        if (currentID == _lastKnownInstanceID) return;

        _lastKnownInstanceID = currentID;

        // Step 1: rebuild the tracker against the new selection.
        ActiveEditorTracker.sharedTracker.ForceRebuild();

        // Step 2: repaint all inspector windows.
        if (_repaintAllInspectors != null)
            _repaintAllInspectors.Invoke(null, null);
    }
}
#endif
