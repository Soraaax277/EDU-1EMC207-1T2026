using UnityEngine;
using UnityEngine.AI;

public enum EnemyState
{
    Patrol,
    Alert,
    Chase
}

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public EnemyState CurrentState { get; private set; }

    [SerializeField] private PlayerMovement player;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float viewRadius = 5.5f;
    [SerializeField] private float viewAngle = 90.0f;
    [SerializeField] private float chaseViewRadius = 7.0f;
    [SerializeField] private float chaseViewAngle = 110.0f;
    [SerializeField] private float hearingRadius = 6.0f;

    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float alertSpeed = 1.4f;
    [SerializeField] private float chaseSpeed = 2.4f;
    [SerializeField] private float waypointTolerance = 0.5f;
    [SerializeField] private float rotationSpeed = 8.0f;

    [SerializeField] private float lookAtSoundDuration = 0.8f;
    [SerializeField] private float alertInvestigateDuration = 2.0f;
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float repathDistanceThreshold = 0.25f;

    [SerializeField] private int visionMeshResolution = 32;
    [SerializeField] private Material visionMaterial;
    [SerializeField] private Color patrolVisionColor = new Color(1f, 0.92f, 0.016f, 0.35f);
    [SerializeField] private Color alertVisionColor = new Color(1f, 0.5f, 0f, 0.45f);
    [SerializeField] private Color chaseVisionColor = new Color(1f, 0f, 0f, 0.55f);

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private Vector3 targetAlertPosition;
    private float alertTimer = 0f;
    private bool isHeadingToSound = false;

    private float repathTimer = 0f;
    private Vector3 lastAssignedDestination;

    private Mesh visionMesh;
    private MeshFilter visionMeshFilter;
    private MeshRenderer visionMeshRenderer;

    private Vector3[] meshVertices;
    private int[] meshTriangles;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.speed = patrolSpeed;

        InitializeVisionCone();
    }

    private void Start()
    {
        LocatePlayer();
        LocateWaypoints();
        SetState(EnemyState.Patrol);
    }

    private void Update()
    {
        if (player == null)
        {
            LocatePlayer();
        }

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyState.Alert:
                UpdateAlert();
                break;
            case EnemyState.Chase:
                UpdateChase();
                break;
        }

        UpdateRotation();
    }

    private void LateUpdate()
    {
        RenderVisionCone();
    }

    public void SetState(EnemyState newState)
    {
        CurrentState = newState;
        alertTimer = 0f;
        repathTimer = 0f;

        switch (CurrentState)
        {
            case EnemyState.Patrol:
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                isHeadingToSound = false;
                UpdateVisionColor(patrolVisionColor);
                MoveToCurrentWaypoint();
                break;

            case EnemyState.Alert:
                agent.speed = alertSpeed;
                agent.isStopped = true;
                isHeadingToSound = false;
                UpdateVisionColor(alertVisionColor);
                break;

            case EnemyState.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                isHeadingToSound = false;
                UpdateVisionColor(chaseVisionColor);
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (CanSeePlayer(viewRadius, viewAngle))
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (CanHearPlayer())
        {
            targetAlertPosition = player.transform.position;
            SetState(EnemyState.Alert);
            return;
        }

        if (waypoints == null || waypoints.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            MoveToCurrentWaypoint();
        }
    }

    private void UpdateAlert()
    {
        if (CanSeePlayer(viewRadius, viewAngle))
        {
            SetState(EnemyState.Chase);
            return;
        }

        if (CanHearPlayer())
        {
            Vector3 soundPosition = player.transform.position;
            if (Vector3.SqrMagnitude(soundPosition - targetAlertPosition) > 0.5f)
            {
                targetAlertPosition = soundPosition;
                alertTimer = 0f;
                isHeadingToSound = false;
                agent.isStopped = true;
            }
        }

        if (!isHeadingToSound)
        {
            LookToward(targetAlertPosition);
            alertTimer += Time.deltaTime;

            if (alertTimer >= lookAtSoundDuration)
            {
                isHeadingToSound = true;
                alertTimer = 0f;
                agent.isStopped = false;
                SetAgentDestination(targetAlertPosition, true);
            }
        }
        else
        {
            if (!agent.pathPending && (agent.remainingDistance <= waypointTolerance || !agent.hasPath))
            {
                alertTimer += Time.deltaTime;
                if (alertTimer >= alertInvestigateDuration)
                {
                    SetState(EnemyState.Patrol);
                }
            }
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            SetState(EnemyState.Patrol);
            return;
        }

        if (CanSeePlayer(chaseViewRadius, chaseViewAngle))
        {
            targetAlertPosition = player.transform.position;
            agent.isStopped = false;

            repathTimer += Time.deltaTime;
            if (repathTimer >= repathInterval)
            {
                repathTimer = 0f;
                SetAgentDestination(player.transform.position, false);
            }
        }
        else
        {
            SetState(EnemyState.Alert);
            isHeadingToSound = true;
            agent.isStopped = false;
            SetAgentDestination(targetAlertPosition, true);
        }
    }

    private void SetAgentDestination(Vector3 destination, bool forceUpdate)
    {
        if (forceUpdate || Vector3.SqrMagnitude(destination - lastAssignedDestination) >= (repathDistanceThreshold * repathDistanceThreshold))
        {
            lastAssignedDestination = destination;
            agent.SetDestination(destination);
        }
    }

    private bool CanSeePlayer(float maxDistance, float maxAngle)
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.transform.position - transform.position;
        directionToPlayer.y = 0f;
        float distance = directionToPlayer.magnitude;

        if (distance > maxDistance || distance < 0.001f) return false;

        Vector3 forwardXZ = transform.forward;
        forwardXZ.y = 0f;

        float angle = Vector3.Angle(forwardXZ, directionToPlayer);
        if (angle > maxAngle * 0.5f) return false;

        Vector3 eyeOrigin = transform.position + Vector3.up * 0.5f;
        Vector3 targetEye = player.transform.position + Vector3.up * 0.5f;

        if (Physics.Linecast(eyeOrigin, targetEye, out RaycastHit hit))
        {
            if (hit.transform != player.transform)
            {
                return false;
            }
        }

        return !NavMesh.Raycast(transform.position, player.transform.position, out _, NavMesh.AllAreas);
    }

    private bool CanHearPlayer()
    {
        if (player == null || !player.IsMakingSound) return false;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        float effectiveRadius = Mathf.Min(hearingRadius, player.SoundRadius);

        return distance <= effectiveRadius;
    }

    private void LookToward(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void UpdateRotation()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;

        if (velocity.sqrMagnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void MoveToCurrentWaypoint()
    {
        if (waypoints != null && waypoints.Length > 0 && waypoints[currentWaypointIndex] != null)
        {
            agent.isStopped = false;
            SetAgentDestination(waypoints[currentWaypointIndex].position, true);
        }
    }

    private void LocatePlayer()
    {
        player = FindFirstObjectByType<PlayerMovement>();
    }

    private void LocateWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            GameObject waypointsRoot = GameObject.Find("Waypoints");
            if (waypointsRoot != null && waypointsRoot.transform.childCount > 0)
            {
                waypoints = new Transform[waypointsRoot.transform.childCount];
                for (int i = 0; i < waypointsRoot.transform.childCount; i++)
                {
                    waypoints[i] = waypointsRoot.transform.GetChild(i);
                }
            }
        }
    }

    private void InitializeVisionCone()
    {
        GameObject coneObject = new GameObject("VisionCone");
        coneObject.transform.SetParent(transform, false);
        coneObject.transform.localPosition = new Vector3(0f, 0.05f, 0f);

        visionMeshFilter = coneObject.AddComponent<MeshFilter>();
        visionMeshRenderer = coneObject.AddComponent<MeshRenderer>();

        int stepCount = Mathf.Max(2, visionMeshResolution);
        meshVertices = new Vector3[stepCount + 2];
        meshTriangles = new int[stepCount * 3];

        for (int i = 0; i < stepCount; i++)
        {
            meshTriangles[i * 3] = 0;
            meshTriangles[i * 3 + 1] = i + 1;
            meshTriangles[i * 3 + 2] = i + 2;
        }

        visionMesh = new Mesh { name = "VisionConeMesh" };
        visionMesh.MarkDynamic();
        visionMesh.vertices = meshVertices;
        visionMesh.triangles = meshTriangles;
        visionMeshFilter.sharedMesh = visionMesh;

        if (visionMaterial == null)
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader == null) defaultShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            if (defaultShader == null) defaultShader = Shader.Find("Standard");

            visionMaterial = new Material(defaultShader);
        }

        visionMeshRenderer.material = new Material(visionMaterial);
        UpdateVisionColor(patrolVisionColor);
    }

    private void UpdateVisionColor(Color color)
    {
        if (visionMeshRenderer != null && visionMeshRenderer.material != null)
        {
            visionMeshRenderer.material.color = color;
        }
    }

    private void RenderVisionCone()
    {
        if (visionMesh == null) return;

        int stepCount = Mathf.Max(2, visionMeshResolution);
        float currentAngleSpan = CurrentState == EnemyState.Chase ? chaseViewAngle : viewAngle;
        float currentMaxDistance = CurrentState == EnemyState.Chase ? chaseViewRadius : viewRadius;
        float stepAngleSize = currentAngleSpan / stepCount;

        meshVertices[0] = Vector3.zero;

        Vector3 worldOrigin = transform.position + Vector3.up * 0.2f;
        Quaternion baseRotation = transform.rotation;

        for (int i = 0; i <= stepCount; i++)
        {
            float currentAngle = -currentAngleSpan * 0.5f + stepAngleSize * i;
            Vector3 localDirection = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;
            Vector3 worldDirection = baseRotation * localDirection;

            float distance = currentMaxDistance;
            if (Physics.Raycast(worldOrigin, worldDirection, out RaycastHit hit, currentMaxDistance))
            {
                if (hit.transform != player?.transform)
                {
                    distance = hit.distance;
                }
            }

            meshVertices[i + 1] = localDirection * distance;
        }

        visionMesh.vertices = meshVertices;
        visionMesh.RecalculateBounds();
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Vector3 current = waypoints[i].position;
                Vector3 next = waypoints[(i + 1) % waypoints.Length] != null ? waypoints[(i + 1) % waypoints.Length].position : current;
                Gizmos.DrawSphere(current, 0.3f);
                Gizmos.DrawLine(current, next);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);

        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, leftBoundary * viewRadius);
        Gizmos.DrawRay(transform.position, rightBoundary * viewRadius);

        if (CurrentState == EnemyState.Alert && targetAlertPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(targetAlertPosition, 0.25f);
            Gizmos.DrawLine(transform.position, targetAlertPosition);
        }
    }
}
