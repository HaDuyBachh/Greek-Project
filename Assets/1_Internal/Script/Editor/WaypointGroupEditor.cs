#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WaypointGroup))]
public class WaypointGroupEditor : Editor
{
    private WaypointGroup group;

    private void OnEnable()
    {
        group = (WaypointGroup)target;
        SceneView.duringSceneGui += HandleSceneGui;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= HandleSceneGui;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Generate Waypoints"))
        {
            Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Generate Waypoints");
            group.GenerateWaypoints();
            EditorUtility.SetDirty(group);
        }

        if (GUILayout.Button("Create Waypoint At Group Position"))
        {
            CreateWaypoint(group.transform.position, true);
        }

        bool nextPlacementMode = GUILayout.Toggle(
            group.ScenePlacementMode,
            "Scene Placement: Ctrl + Left Click",
            "Button");

        if (nextPlacementMode != group.ScenePlacementMode)
        {
            Undo.RecordObject(group, "Toggle Waypoint Scene Placement");
            group.ScenePlacementMode = nextPlacementMode;
            EditorUtility.SetDirty(group);
            SceneView.RepaintAll();
        }

        if (group.ScenePlacementMode)
        {
            EditorGUILayout.HelpBox(
                "Move the mouse in Scene View, then Ctrl + Left Click to create a waypoint at the raycast hit point.",
                MessageType.Info);
        }
    }

    private void HandleSceneGui(SceneView sceneView)
    {
        if (group == null || !group.ScenePlacementMode || Selection.activeGameObject != group.gameObject)
        {
            return;
        }

        Event current = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);

        if (TryGetPlacementPoint(ray, out Vector3 point))
        {
            Handles.color = new Color(0.1f, 0.75f, 1f, 0.9f);
            Handles.DrawWireDisc(point, Vector3.up, 0.35f);
            Handles.SphereHandleCap(0, point, Quaternion.identity, 0.12f, EventType.Repaint);
        }

        if (current.type != EventType.MouseDown || current.button != 0 || !current.control)
        {
            return;
        }

        if (TryGetPlacementPoint(ray, out Vector3 hitPoint))
        {
            CreateWaypoint(hitPoint, false);
            current.Use();
        }
    }

    private bool TryGetPlacementPoint(Ray ray, out Vector3 point)
    {
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            point = hit.point;
            return true;
        }

        Plane groupHeightPlane = new Plane(Vector3.up, group.transform.position);
        if (groupHeightPlane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private void CreateWaypoint(Vector3 position, bool selectCreated)
    {
        Undo.RegisterFullObjectHierarchyUndo(group.gameObject, "Create Waypoint");
        LabeledWaypoint waypoint = group.CreateWaypoint(position);
        Undo.RegisterCreatedObjectUndo(waypoint.gameObject, "Create Waypoint");
        Selection.activeGameObject = selectCreated ? waypoint.gameObject : group.gameObject;
        EditorUtility.SetDirty(group);
    }
}
#endif
