using System;
using System.Collections.Generic;
using UnityEngine;

public class WaypointGroup : MonoBehaviour
{
    public enum GenerateMode
    {
        Line,
        Circle
    }

    [Header("Generated Points")]
    [SerializeField] private GenerateMode generateMode = GenerateMode.Line;
    [SerializeField] private int amount = 5;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private float radius = 3f;
    [SerializeField] private string labelPrefix = "Point";
    [SerializeField] private bool scenePlacementMode = false;
    [SerializeField] private List<LabeledWaypoint> waypoints = new List<LabeledWaypoint>();

    public IReadOnlyList<LabeledWaypoint> Waypoints => waypoints;
    public bool ScenePlacementMode
    {
        get => scenePlacementMode;
        set => scenePlacementMode = value;
    }

    private void Awake()
    {
        RefreshList();
    }

    public LabeledWaypoint GetByLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return null;
        }

        RefreshList();

        foreach (LabeledWaypoint waypoint in waypoints)
        {
            if (waypoint != null && string.Equals(waypoint.Label, label, StringComparison.OrdinalIgnoreCase))
            {
                return waypoint;
            }
        }

        return null;
    }

    public bool TryGetByLabel(string label, out LabeledWaypoint waypoint)
    {
        waypoint = GetByLabel(label);
        return waypoint != null;
    }

    public LabeledWaypoint CreateWaypoint(Vector3 worldPosition)
    {
        RefreshList();

        string label = GetNextLabel();
        GameObject point = new GameObject(label);
        point.transform.SetParent(transform);
        point.transform.position = worldPosition;
        point.transform.localRotation = Quaternion.identity;

        LabeledWaypoint waypoint = point.AddComponent<LabeledWaypoint>();
        waypoint.SetLabel(label);

        waypoints.Add(waypoint);
        return waypoint;
    }

    public LabeledWaypoint GetNearest(Vector3 worldPosition)
    {
        RefreshList();

        LabeledWaypoint nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (LabeledWaypoint waypoint in waypoints)
        {
            if (waypoint == null)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(waypoint.Position - worldPosition);
            if (distance < nearestDistance)
            {
                nearest = waypoint;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    [ContextMenu("Refresh Waypoint List")]
    public void RefreshList()
    {
        waypoints.Clear();
        GetComponentsInChildren(true, waypoints);
        waypoints.RemoveAll(waypoint => waypoint == null || waypoint.transform == transform);
    }

    [ContextMenu("Generate Waypoints")]
    public void GenerateWaypoints()
    {
        amount = Mathf.Max(1, amount);

        RemoveGeneratedWaypoints();

        for (int i = 0; i < amount; i++)
        {
            GameObject point = new GameObject($"{labelPrefix}_{i + 1:00}");
            point.transform.SetParent(transform);
            point.transform.localPosition = GetLocalPosition(i);
            point.transform.localRotation = Quaternion.identity;

            LabeledWaypoint waypoint = point.AddComponent<LabeledWaypoint>();
            waypoint.SetLabel($"{labelPrefix}_{i + 1:00}");
        }

        RefreshList();
    }

    private Vector3 GetLocalPosition(int index)
    {
        if (generateMode == GenerateMode.Circle)
        {
            float angle = (Mathf.PI * 2f / amount) * index;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        float startOffset = (amount - 1) * spacing * -0.5f;
        return new Vector3(startOffset + index * spacing, 0f, 0f);
    }

    private string GetNextLabel()
    {
        int nextIndex = waypoints.Count + 1;
        string label;

        do
        {
            label = $"{labelPrefix}_{nextIndex:00}";
            nextIndex++;
        }
        while (GetByLabelWithoutRefresh(label) != null);

        return label;
    }

    private LabeledWaypoint GetByLabelWithoutRefresh(string label)
    {
        foreach (LabeledWaypoint waypoint in waypoints)
        {
            if (waypoint != null && string.Equals(waypoint.Label, label, StringComparison.OrdinalIgnoreCase))
            {
                return waypoint;
            }
        }

        return null;
    }

    private void RemoveGeneratedWaypoints()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<LabeledWaypoint>() == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
        spacing = Mathf.Max(0.1f, spacing);
        radius = Mathf.Max(0.1f, radius);
    }
}
