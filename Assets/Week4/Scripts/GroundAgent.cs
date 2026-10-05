using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GroundAgent : MonoBehaviour
{
    public PatrolRoute Route;
    public float MovementSpeed = 5f;
    public float MinimumSpeed = 0f;
    public float WaypointTolerance = 2.5f;
    public float FollowDistance = 3.5f;
    public float StopDistance = 1.6f;
    public float SensorRadius = 0.5f;
    public float AgentRadius = 0.65f;
    public float RotationSmoothSpeed = 8f;
    public float LaneOffset = 0f;
    public LayerMask VehicleMask = 1 << 2;

    private NavMeshAgent agent;
    private int waypointIndex;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.radius = AgentRadius;
        agent.autoBraking = false;
        agent.autoRepath = false;
        agent.updateRotation = false;
    }

    public void Initialize(PatrolRoute route, int startIndex, float laneOffset, float speed)
    {
        Route = route;
        waypointIndex = startIndex;
        LaneOffset = laneOffset;
        MovementSpeed = speed;
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.radius = AgentRadius;
        agent.autoBraking = false;
        agent.autoRepath = false;
        agent.updateRotation = false;
        agent.speed = speed;
        MoveToCurrentWaypoint();
    }

    private void Start()
    {
        if (!agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        MoveToCurrentWaypoint();
    }

    private void Update()
    {
        if (!agent.isOnNavMesh || Route == null || Route.Count == 0) return;

        AdjustSpeedForTraffic();
        UpdateSmoothRotation();

        if (!agent.pathPending && agent.remainingDistance <= WaypointTolerance)
        {
            waypointIndex = (waypointIndex + 1) % Route.Count;
            MoveToCurrentWaypoint();
        }
    }

    private void UpdateSmoothRotation()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSmoothSpeed * Time.deltaTime);
        }
    }

    public void SetRoute(PatrolRoute route)
    {
        Route = route;
        waypointIndex = 0;
        MoveToCurrentWaypoint();
    }

    private void MoveToCurrentWaypoint()
    {
        if (agent.isOnNavMesh && Route != null && Route.Count > 0)
        {
            Vector3 currentWaypoint = Route.GetPosition(waypointIndex);
            Vector3 nextWaypoint = Route.GetPosition((waypointIndex + 1) % Route.Count);
            Vector3 direction = (nextWaypoint - currentWaypoint).normalized;
            Vector3 normal = Vector3.Cross(Vector3.up, direction);
            Vector3 target = currentWaypoint + normal * LaneOffset;

            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2.5f, NavMesh.AllAreas) && hit.position.y < 0.5f)
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(currentWaypoint);
            }
        }
    }

    private void AdjustSpeedForTraffic()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit[] hits = Physics.SphereCastAll(origin, SensorRadius, transform.forward, FollowDistance, VehicleMask, QueryTriggerInteraction.Ignore);
        float closestDistance = FollowDistance;
        bool hasCarInFront = false;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.root != transform.root && hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                hasCarInFront = true;
            }
        }

        float targetSpeed = MovementSpeed;
        if (hasCarInFront)
        {
            float spacing = Mathf.InverseLerp(StopDistance, FollowDistance, closestDistance);
            targetSpeed = Mathf.Lerp(MinimumSpeed, MovementSpeed, spacing);
        }

        agent.speed = targetSpeed;
    }
}
