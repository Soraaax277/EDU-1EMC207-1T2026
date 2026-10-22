using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum BotState
{
    None,
    Patrol,
    Alert,
    Chase
}

[RequireComponent(typeof(NavMeshAgent))]
public class BasicStateMachine : MonoBehaviour
{
    public BotState CurrentState { get; private set; }

    [SerializeField] private Transform player;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float detectionRadius = 3.0f;
    [SerializeField] private float chaseLostRadius = 4.5f;

    [SerializeField] private float alertToChaseDuration = 2.0f;
    [SerializeField] private float alertToPatrolDuration = 1.8f;

    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private float chaseSpeed = 1.8f;
    [SerializeField] private float waypointTolerance = 0.5f;
    [SerializeField] private float rotationSmoothSpeed = 8.0f;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private Vector3 lastKnownPosition;
    private float alertTimer = 0f;
    private float lostTimer = 0f;
    private bool isInvestigatingLastKnownPos = false;

    private void OnValidate()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = patrolSpeed;
        }
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = patrolSpeed;
            agent.acceleration = 8f;
        }
    }

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.speed = patrolSpeed;
            agent.acceleration = 8f;
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                ClickToMove clickMove = FindFirstObjectByType<ClickToMove>();
                if (clickMove != null) player = clickMove.transform;
            }
        }

        if (waypoints == null || waypoints.Length == 0)
        {
            GameObject wpRoot = GameObject.Find("Waypoints");
            if (wpRoot != null && wpRoot.transform.childCount > 0)
            {
                waypoints = new Transform[wpRoot.transform.childCount];
                for (int i = 0; i < wpRoot.transform.childCount; i++)
                {
                    waypoints[i] = wpRoot.transform.GetChild(i);
                }
            }
        }

        SetState(BotState.Patrol);
    }

    void Update()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }

        switch (CurrentState)
        {
            case BotState.Patrol:
                UpdatePatrol();
                break;
            case BotState.Alert:
                UpdateAlert();
                break;
            case BotState.Chase:
                UpdateChase();
                break;
            default:
                break;
        }

        UpdateSmoothRotation();
    }

    public void SetState(BotState newState)
    {
        CurrentState = newState;
        alertTimer = 0f;
        lostTimer = 0f;

        switch (CurrentState)
        {
            case BotState.Patrol:
                agent.speed = patrolSpeed;
                agent.isStopped = false;
                isInvestigatingLastKnownPos = false;
                MoveToCurrentWaypoint();
                break;

            case BotState.Alert:
                agent.speed = patrolSpeed;
                alertTimer = 0f;
                lostTimer = 0f;
                if (isInvestigatingLastKnownPos)
                {
                    agent.isStopped = false;
                    agent.SetDestination(lastKnownPosition);
                }
                else
                {
                    agent.isStopped = true;
                }
                break;

            case BotState.Chase:
                agent.speed = chaseSpeed;
                agent.isStopped = false;
                isInvestigatingLastKnownPos = false;
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (CanSeePlayer(detectionRadius))
        {
            lastKnownPosition = player.position;
            isInvestigatingLastKnownPos = false;
            SetState(BotState.Alert);
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
        bool playerInSight = CanSeePlayer(detectionRadius);

        if (isInvestigatingLastKnownPos)
        {
            if (!agent.pathPending && agent.remainingDistance <= waypointTolerance)
            {
                isInvestigatingLastKnownPos = false;
                agent.isStopped = true;
            }
        }
        else
        {
            Vector3 lookTarget = playerInSight ? player.position : lastKnownPosition;
            Vector3 lookDir = lookTarget - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothSpeed * Time.deltaTime);
            }
        }

        if (playerInSight)
        {
            lastKnownPosition = player.position;
            lostTimer = 0f;
            alertTimer += Time.deltaTime;

            if (alertTimer >= alertToChaseDuration)
            {
                SetState(BotState.Chase);
            }
        }
        else
        {
            alertTimer = 0f;
            lostTimer += Time.deltaTime;

            if (lostTimer >= alertToPatrolDuration)
            {
                SetState(BotState.Patrol);
            }
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            SetState(BotState.Patrol);
            return;
        }

        bool playerInSight = CanSeePlayer(chaseLostRadius);

        if (playerInSight)
        {
            lastKnownPosition = player.position;
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            isInvestigatingLastKnownPos = true;
            SetState(BotState.Alert);
        }
    }

    private bool CanSeePlayer(float maxDistance)
    {
        if (player == null) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > maxDistance) return false;

        bool blockedByBuilding = NavMesh.Raycast(transform.position, player.position, out NavMeshHit hit, NavMesh.AllAreas);
        return !blockedByBuilding;
    }

    private void MoveToCurrentWaypoint()
    {
        if (waypoints != null && waypoints.Length > 0 && waypoints[currentWaypointIndex] != null)
        {
            agent.isStopped = false;
            agent.SetDestination(waypoints[currentWaypointIndex].position);
        }
    }

    private void UpdateSmoothRotation()
    {
        Vector3 velocity = agent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude > 0.05f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = new Color(0.15f, 0.85f, 1f, 0.9f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Vector3 current = waypoints[i].position;
                Vector3 next = waypoints[(i + 1) % waypoints.Length] != null ? waypoints[(i + 1) % waypoints.Length].position : current;
                Gizmos.DrawSphere(current, 0.2f);
                Gizmos.DrawLine(current, next);
            }
        }

        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, chaseLostRadius);

        if (player != null)
        {
            bool visible = CanSeePlayer(detectionRadius);
            Gizmos.color = visible ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.4f, player.position + Vector3.up * 0.4f);
        }

        if (lastKnownPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPosition, 0.25f);
            Gizmos.DrawLine(transform.position, lastKnownPosition);
        }
    }
}
