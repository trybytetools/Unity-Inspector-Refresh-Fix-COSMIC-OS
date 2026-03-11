// CosmicInspectorFix.cs
// Place in Assets/Editor/CosmicInspectorFix.cs
//
// Fixes Inspector and Hierarchy windows not updating under the COSMIC desktop.
// COSMIC drops the window-activation event Unity relies on for both
// selectionChanged and hierarchyChanged — so we poll instead.
//
// Tested: Unity 6000.3.10f1 / Pop!_OS 24.04 / COSMIC
// https://github.com/pop-os/cosmic-epoch/issues/2311

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CosmicInspectorFix
{
    // Inspector
    private static int        _lastInstanceID;
    private static MethodInfo _repaintAllInspectors;

    // Hierarchy
    private static int              _lastOrderHash = 0;
    private static double           _lastHierarchyPoll;
    private const  double           HierarchyPollInterval = 0.1;
    private static System.Type      _hierarchyWindowType;
    private static FieldInfo        _sceneHierarchyField;
    private static MethodInfo       _sceneHierarchyReload;
    private static List<GameObject> _roots = new List<GameObject>();

    static CosmicInspectorFix()
    {
        var asm = typeof(Editor).Assembly;

        // Inspector: RepaintAllInspectors
        var inspectorType = asm.GetType("UnityEditor.InspectorWindow");
        if (inspectorType != null)
            _repaintAllInspectors = inspectorType.GetMethod("RepaintAllInspectors",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (_repaintAllInspectors == null)
            Debug.LogWarning("[CosmicInspectorFix] RepaintAllInspectors not found.");

        // Hierarchy: find SceneHierarchy.Reload() via the window's field
        _hierarchyWindowType    = asm.GetType("UnityEditor.SceneHierarchyWindow");
        var sceneHierarchyType  = asm.GetType("UnityEditor.SceneHierarchy");

        if (_hierarchyWindowType != null && sceneHierarchyType != null)
        {
            foreach (var f in _hierarchyWindowType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType != sceneHierarchyType) continue;
                _sceneHierarchyField = f;
                break;
            }

            // Prefer ReloadData, fall back to Reload
            _sceneHierarchyReload =
                sceneHierarchyType.GetMethod("ReloadData",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? sceneHierarchyType.GetMethod("Reload",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        if (_sceneHierarchyField == null || _sceneHierarchyReload == null)
            Debug.LogWarning("[CosmicInspectorFix] SceneHierarchy reload not found — hierarchy fix inactive.");

#pragma warning disable CS0618
        _lastInstanceID = Selection.activeInstanceID;
#pragma warning restore CS0618

        EditorApplication.update += PollInspector;
        EditorApplication.update += PollHierarchy;

        Debug.Log("[CosmicInspectorFix] active.");
    }

    private static void PollInspector()
    {
#pragma warning disable CS0618
        int currentID = Selection.activeInstanceID;
#pragma warning restore CS0618
        if (currentID == _lastInstanceID) return;
        _lastInstanceID = currentID;

        ActiveEditorTracker.sharedTracker.ForceRebuild();
        _repaintAllInspectors?.Invoke(null, null);
    }

    private static void PollHierarchy()
    {
        if (_sceneHierarchyField == null || _sceneHierarchyReload == null) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - _lastHierarchyPoll < HierarchyPollInterval) return;
        _lastHierarchyPoll = now;

        var scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded) return;

        scene.GetRootGameObjects(_roots);
        int hash = ComputeOrderHash(_roots);
        if (hash == _lastOrderHash) return;
        _lastOrderHash = hash;

        foreach (var w in Resources.FindObjectsOfTypeAll(_hierarchyWindowType))
        {
            var instance = _sceneHierarchyField.GetValue(w);
            if (instance != null)
                _sceneHierarchyReload.Invoke(instance, null);
        }
        EditorApplication.RepaintHierarchyWindow();
    }

    private static int ComputeOrderHash(List<GameObject> roots)
    {
        unchecked
        {
            int hash = 17;
            foreach (var go in roots)
            {
                if (go == null) continue;
                hash = hash * 31 + go.GetInstanceID();
                hash = hash * 31 + go.transform.GetSiblingIndex();
                hash = hash * 31 + go.transform.childCount;
                hash = hash * 31 + go.name.GetHashCode();
            }
            return hash;
        }
    }
}
#endif
