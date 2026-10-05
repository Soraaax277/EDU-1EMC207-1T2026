using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FlyingAgent : MonoBehaviour
{
    public PatrolRoute AirRoute;
    public PatrolRoute GroundRoute;
    public bool AlternateRoutes = true;

    [Header("Air Movement")]
    public float CruiseSpeed = 6f;
    public float ClimbingSpeed = 2.5f;
    public float DirectionSmoothSpeed = 3.5f;
    public float MaxBankAngle = 14f;
    public float WaypointTolerance = 3f;

    [Header("Altitude")]
    public float CruiseHeight = 2.4f;
    public float ObstacleClearance = 1.8f;
    public float LookAheadDistance = 1.2f;
    public float FootprintWidth = 0.5f;
    public int LookAheadSamples = 1;
    public float ClimbSmoothTime = 0.22f;
    public float DescendSmoothTime = 0.35f;
    public float MaxClimbRate = 8f;
    public float MaxDescentRate = 6f;
    public float SampleHeight = 60f;
    public LayerMask TerrainMask = Physics.DefaultRaycastLayers;

    [Header("Hover")]
    public float BobAmplitude = 0.08f;
    public float BobFrequency = 1.8f;

    [Header("Rotors")]
    public Transform MainRotor;
    public Transform TailRotor;
    public float RotorSpeed = 1500f;

    private NavMeshAgent agent;
    private PatrolRoute currentRoute;
    private int waypointIndex;
    private int completedWaypoints;
    private float altitude;
    private float verticalVelocity;
    private Vector3 currentFlightDirection = Vector3.forward;
    private float currentBankAngle;
    private float yawVelocity;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
    }

    private void Start()
    {
        currentRoute = AirRoute != null ? AirRoute : GroundRoute;
        altitude = transform.position.y;
        currentFlightDirection = transform.forward;
        currentFlightDirection.y = 0f;
        if (currentFlightDirection.sqrMagnitude < 0.001f)
        {
            currentFlightDirection = Vector3.forward;
        }
        else
        {
            currentFlightDirection.Normalize();
        }

        if (currentRoute != null)
        {
            waypointIndex = FindNextWaypointIndex(transform.position, currentRoute, currentFlightDirection);
        }
    }

    private void Update()
    {
        SpinRotors();
        UpdateFlight();
    }

    private void UpdateFlight()
    {
        if (currentRoute == null)
        {
            currentRoute = AirRoute != null ? AirRoute : GroundRoute;
            if (currentRoute == null || currentRoute.Count == 0) return;
        }

        Vector3 target = currentRoute.GetPosition(waypointIndex);
        Vector3 position = transform.position;
        Vector3 toTarget = new Vector3(target.x - position.x, 0f, target.z - position.z);
        float distance = toTarget.magnitude;

        if (toTarget.sqrMagnitude > 0.01f)
        {
            Vector3 desiredDir = toTarget.normalized;
            currentFlightDirection = Vector3.Slerp(currentFlightDirection, desiredDir, DirectionSmoothSpeed * Time.deltaTime);
        }

        float targetAltitude = GetSafeAltitude(position, currentFlightDirection);
        bool isClimbing = targetAltitude > altitude;
        float smoothTime = isClimbing ? ClimbSmoothTime : DescendSmoothTime;
        float maxRate = isClimbing ? MaxClimbRate : MaxDescentRate;

        altitude = Mathf.SmoothDamp(altitude, targetAltitude, ref verticalVelocity, smoothTime, maxRate);

        float climbRatio = Mathf.Clamp01(Mathf.Abs(verticalVelocity) / MaxClimbRate);
        float speed = Mathf.Lerp(CruiseSpeed, ClimbingSpeed, climbRatio);

        Vector3 movement = currentFlightDirection * (speed * Time.deltaTime);
        float bobFade = Mathf.Clamp01(1f - Mathf.Abs(verticalVelocity) / 1.5f);
        float bob = Mathf.Sin(Time.time * BobFrequency) * (BobAmplitude * bobFade);
        transform.position = new Vector3(position.x + movement.x, altitude + bob, position.z + movement.z);

        UpdateRotation();

        float dot = Vector3.Dot(currentFlightDirection, toTarget);
        if (distance <= WaypointTolerance || (distance < 4f && dot < 0f))
        {
            waypointIndex = (waypointIndex + 1) % currentRoute.Count;
            completedWaypoints++;

            if (completedWaypoints >= currentRoute.Count)
            {
                completedWaypoints = 0;
                if (AlternateRoutes && GroundRoute != null && AirRoute != null)
                {
                    currentRoute = (currentRoute == AirRoute) ? GroundRoute : AirRoute;
                    waypointIndex = FindNextWaypointIndex(position, currentRoute, currentFlightDirection);
                }
            }
        }
    }

    private int FindNextWaypointIndex(Vector3 position, PatrolRoute route, Vector3 forwardDir)
    {
        if (route == null || route.Count == 0) return 0;

        int closestIndex = 0;
        float minDistanceSqr = float.MaxValue;

        for (int i = 0; i < route.Count; i++)
        {
            Vector3 wp = route.GetPosition(i);
            float d2 = (wp.x - position.x) * (wp.x - position.x) + (wp.z - position.z) * (wp.z - position.z);
            if (d2 < minDistanceSqr)
            {
                minDistanceSqr = d2;
                closestIndex = i;
            }
        }

        Vector3 toClosest = route.GetPosition(closestIndex) - position;
        toClosest.y = 0f;

        if (Vector3.Dot(forwardDir, toClosest) <= 0.2f || toClosest.magnitude < WaypointTolerance)
        {
            return (closestIndex + 1) % route.Count;
        }

        return closestIndex;
    }

    private void UpdateRotation()
    {
        if (currentFlightDirection.sqrMagnitude < 0.0001f) return;

        float targetYaw = Mathf.Atan2(currentFlightDirection.x, currentFlightDirection.z) * Mathf.Rad2Deg;
        float currentYaw = transform.eulerAngles.y;
        float newYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, 0.3f);

        float targetBank = Mathf.Clamp(-yawVelocity * 0.2f, -MaxBankAngle, MaxBankAngle);
        currentBankAngle = Mathf.Lerp(currentBankAngle, targetBank, 4f * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, newYaw, currentBankAngle);
    }

    private float GetSafeAltitude(Vector3 position, Vector3 direction)
    {
        Vector3 normal = Vector3.Cross(Vector3.up, direction).normalized;
        float highestSurface = Mathf.Max(
            SampleSurfaceHeight(position),
            Mathf.Max(SampleSurfaceHeight(position + normal * FootprintWidth), SampleSurfaceHeight(position - normal * FootprintWidth))
        );

        for (int i = 1; i <= LookAheadSamples; i++)
        {
            float distance = (LookAheadDistance / LookAheadSamples) * i;
            Vector3 center = position + direction * distance;
            highestSurface = Mathf.Max(highestSurface, SampleSurfaceHeight(center));
            highestSurface = Mathf.Max(highestSurface, SampleSurfaceHeight(center + normal * FootprintWidth));
            highestSurface = Mathf.Max(highestSurface, SampleSurfaceHeight(center - normal * FootprintWidth));
        }

        return Mathf.Max(CruiseHeight, highestSurface + ObstacleClearance);
    }

    private float SampleSurfaceHeight(Vector3 point)
    {
        Vector3 origin = new Vector3(point.x, SampleHeight, point.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, SampleHeight * 2f, TerrainMask, QueryTriggerInteraction.Ignore);
        float highest = 0f;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.root != transform.root && hits[i].point.y > highest)
            {
                highest = hits[i].point.y;
            }
        }
        return highest;
    }

    private void SpinRotors()
    {
        float angle = RotorSpeed * Time.deltaTime;
        if (MainRotor != null) MainRotor.Rotate(0f, angle, 0f, Space.Self);
        if (TailRotor != null) TailRotor.Rotate(0f, angle, 0f, Space.Self);
    }
}
