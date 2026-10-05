using UnityEngine;

public class PatrolRoute : MonoBehaviour
{
    public Transform[] Waypoints;
    public Color RouteColor = Color.cyan;
    public float WaypointRadius = 0.5f;

    public int Count => Waypoints == null ? 0 : Waypoints.Length;

    public Vector3 GetPosition(int index)
    {
        return Waypoints[index % Waypoints.Length].position;
    }

    public Vector3 GetPointOnRoute(float t, float laneOffset, out int nextWaypointIndex, out Vector3 forwardDirection)
    {
        if (Waypoints == null || Waypoints.Length == 0)
        {
            nextWaypointIndex = 0;
            forwardDirection = Vector3.forward;
            return Vector3.zero;
        }

        float totalSegments = Waypoints.Length;
        float progress = (t % 1f) * totalSegments;
        int index = Mathf.FloorToInt(progress);
        float segmentT = progress - index;

        Vector3 start = Waypoints[index].position;
        Vector3 end = Waypoints[(index + 1) % Waypoints.Length].position;

        nextWaypointIndex = (index + 1) % Waypoints.Length;
        forwardDirection = (end - start).normalized;
        if (forwardDirection.sqrMagnitude < 0.001f) forwardDirection = Vector3.forward;

        Vector3 normal = Vector3.Cross(Vector3.up, forwardDirection);
        return Vector3.Lerp(start, end, segmentT) + normal * laneOffset;
    }

    private void OnDrawGizmos()
    {
        if (Count == 0) return;

        Gizmos.color = RouteColor;
        for (int i = 0; i < Waypoints.Length; i++)
        {
            Vector3 current = Waypoints[i].position;
            Vector3 next = Waypoints[(i + 1) % Waypoints.Length].position;
            Gizmos.DrawSphere(current, WaypointRadius);
            Gizmos.DrawLine(current, next);
        }
    }
}
