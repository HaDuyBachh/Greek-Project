using UnityEngine;

public class WaypointArrivalDetector : MonoBehaviour
{
    [SerializeField] private WaypointGroup waypointGroup;
    [SerializeField] private bool triggerOnlyOncePerWaypoint = true;

    private LabeledWaypoint currentWaypoint;

    private void Reset()
    {
        waypointGroup = FindObjectOfType<WaypointGroup>();
    }

    private void Update()
    {
        if (waypointGroup == null)
        {
            return;
        }

        LabeledWaypoint arrivedWaypoint = FindArrivedWaypoint();

        if (arrivedWaypoint == null)
        {
            currentWaypoint = null;
            return;
        }

        if (triggerOnlyOncePerWaypoint && arrivedWaypoint == currentWaypoint)
        {
            return;
        }

        currentWaypoint = arrivedWaypoint;
        arrivedWaypoint.Arrive(gameObject);
    }

    private LabeledWaypoint FindArrivedWaypoint()
    {
        foreach (LabeledWaypoint waypoint in waypointGroup.Waypoints)
        {
            if (waypoint != null && waypoint.IsInside(transform.position))
            {
                return waypoint;
            }
        }

        return null;
    }
}
