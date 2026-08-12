using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class WaypointArrivedEvent : UnityEvent<GameObject>
{
}

public class LabeledWaypoint : MonoBehaviour
{
    [SerializeField] private string label = "Point";
    [SerializeField] private float arriveRadius = 0.35f;
    [SerializeField] private UnityEvent onArrived = new UnityEvent();
    [SerializeField] private WaypointArrivedEvent onCharacterArrived = new WaypointArrivedEvent();

    public string Label => label;
    public float ArriveRadius => arriveRadius;
    public Vector3 Position => transform.position;

    public void SetLabel(string value)
    {
        label = value;
        gameObject.name = string.IsNullOrWhiteSpace(value) ? "Waypoint" : value;
    }

    public bool IsInside(Vector3 worldPosition)
    {
        return Vector3.Distance(transform.position, worldPosition) <= arriveRadius;
    }

    public void Arrive(GameObject character)
    {
        onArrived.Invoke();
        onCharacterArrived.Invoke(character);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.1f, 0.75f, 1f, 0.85f);
        Gizmos.DrawSphere(transform.position, 0.12f);
        Gizmos.DrawWireSphere(transform.position, arriveRadius);

#if UNITY_EDITOR
        Handles.Label(transform.position + Vector3.up * 0.25f, label);
#endif
    }
}
