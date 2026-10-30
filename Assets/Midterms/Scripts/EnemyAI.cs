using UnityEngine;
using UnityEngine.AI;

namespace Midterms
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        public enum State
        {
            Patrol,
            Alert,
            Chase,
            Eliminated
        }

        public State CurrentState { get; private set; } = State.Patrol;

        [Header("Target & Patrol")]
        [SerializeField] private PlayerMovement Player;
        [SerializeField] private Transform[] Waypoints;

        [Header("Vision & Detection")]
        [SerializeField] private float ViewRadius = 6.0f;
        [SerializeField] private float ViewAngle = 85.0f;
        [SerializeField] private float ChaseViewRadius = 8.0f;
        [SerializeField] private float ChaseViewAngle = 105.0f;
        [SerializeField] private float HearingRadius = 6.5f;
        [SerializeField] private float CatchDistance = 1.15f;
        [SerializeField] private LayerMask ObstacleMask = ~0;

        [Header("Movement Speeds & Tolerances")]
        [SerializeField] private float PatrolSpeed = 1.5f;
        [SerializeField] private float AlertSpeed = 1.6f;
        [SerializeField] private float ChaseSpeed = 2.6f;
        [SerializeField] private float WaypointTolerance = 0.5f;
        [SerializeField] private float RotationSpeed = 8.0f;

        [Header("Timers & Intervals")]
        [SerializeField] private float LookAtSoundDuration = 0.8f;
        [SerializeField] private float AlertInvestigateDuration = 2.0f;
        [SerializeField] private float RepathInterval = 0.15f;

        [Header("Vision Mesh Visualization")]
        [SerializeField] private int VisionMeshResolution = 32;
        [SerializeField] private Material VisionMaterial;
        [SerializeField] private Color PatrolVisionColor = new Color(1f, 0.92f, 0.016f, 0.35f);
        [SerializeField] private Color AlertVisionColor = new Color(1f, 0.5f, 0f, 0.45f);
        [SerializeField] private Color ChaseVisionColor = new Color(1f, 0f, 0f, 0.55f);

        private NavMeshAgent Agent;
        private int CurrentWaypointIndex;
        private Vector3 TargetAlertPosition;
        private float AlertTimer;
        private bool IsHeadingToSound;
        private float RepathTimer;

        private Mesh VisionMesh;
        private MeshFilter VisionFilter;
        private MeshRenderer VisionRenderer;
        private Vector3[] Vertices;
        private int[] Triangles;

        private int WaterAreaMask;
        private bool IsInWater;
        private bool IsInWaterTrigger;
        private float TerrainCheckTimer;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Agent.updateRotation = false;
            Agent.speed = PatrolSpeed;

            int waterIndex = NavMesh.GetAreaFromName("Water");
            WaterAreaMask = waterIndex >= 0 ? (1 << waterIndex) : (1 << 3);

            InitializeVisionMesh();
        }

        private void Start()
        {
            LocatePlayer();
            LocateWaypoints();
            EnsureOnNavMesh();
            SetState(State.Patrol);
        }

        private void Update()
        {
            if (CurrentState == State.Eliminated) return;

            if (Agent == null || !Agent.isOnNavMesh)
            {
                EnsureOnNavMesh();
                if (!Agent.isOnNavMesh) return;
            }

            if (Player == null)
            {
                LocatePlayer();
                if (Player == null) return;
            }

            CheckSurfaceWater();

            switch (CurrentState)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Alert:
                    UpdateAlert();
                    break;
                case State.Chase:
                    UpdateChase();
                    break;
            }

            UpdateRotation();
            CheckCatchProximity();
        }

        private void LateUpdate()
        {
            if (CurrentState != State.Eliminated)
            {
                RenderVision();
            }
        }

        private void CheckSurfaceWater()
        {
            if (IsInWaterTrigger)
            {
                IsInWater = true;
            }
            else
            {
                TerrainCheckTimer -= Time.deltaTime;
                if (TerrainCheckTimer <= 0f)
                {
                    TerrainCheckTimer = 0.15f;
                    if (Agent != null && Agent.enabled && Agent.isOnNavMesh && Agent.velocity.sqrMagnitude >= 0.05f)
                    {
                        if (Agent.SamplePathPosition(NavMesh.AllAreas, 0.0f, out NavMeshHit hit))
                        {
                            IsInWater = (hit.mask & WaterAreaMask) != 0;
                        }
                        else if (NavMesh.SamplePosition(transform.position, out NavMeshHit posHit, 0.4f, NavMesh.AllAreas))
                        {
                            IsInWater = (posHit.mask & WaterAreaMask) != 0;
                        }
                        else
                        {
                            IsInWater = false;
                        }
                    }
                    else if (Agent != null && Agent.velocity.sqrMagnitude < 0.05f)
                    {
                    }
                    else
                    {
                        IsInWater = false;
                    }
                }
            }

            float targetSpeed = PatrolSpeed;
            if (CurrentState == State.Alert) targetSpeed = AlertSpeed;
            else if (CurrentState == State.Chase) targetSpeed = ChaseSpeed;

            if (IsInWater) targetSpeed *= 0.5f;

            if (Agent != null && Agent.isOnNavMesh && Agent.enabled)
            {
                Agent.speed = targetSpeed;
            }
        }

        public void InvestigateSound(Vector3 soundPosition)
        {
            if (CurrentState == State.Eliminated) return;

            TargetAlertPosition = soundPosition;
            SetState(State.Alert);
            AlertTimer = 0f;
            IsHeadingToSound = false;
        }

        public void Eliminate()
        {
            if (CurrentState == State.Eliminated) return;

            SetState(State.Eliminated);
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = true;
            }
            Agent.enabled = false;

            if (VisionRenderer != null)
            {
                VisionRenderer.enabled = false;
            }

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.runtimeAnimatorController != null)
            {
                anim.SetTrigger("Die");
            }

            StartCoroutine(DisableAfterDeathRoutine());
        }

        private System.Collections.IEnumerator DisableAfterDeathRoutine()
        {
            yield return new WaitForSeconds(2.0f);
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<TerrainHazard>() != null || other.name.Contains("Water_Slowdown_Hazard"))
            {
                IsInWaterTrigger = true;
                IsInWater = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<TerrainHazard>() != null || other.name.Contains("Water_Slowdown_Hazard"))
            {
                IsInWaterTrigger = false;
                IsInWater = false;
            }
        }

        private void SetState(State newState)
        {
            CurrentState = newState;

            switch (newState)
            {
                case State.Patrol:
                    if (Agent.isOnNavMesh)
                    {
                        Agent.speed = PatrolSpeed;
                        Agent.isStopped = false;
                    }
                    SetVisionColor(PatrolVisionColor);
                    if (Waypoints != null && Waypoints.Length > 0 && Waypoints[CurrentWaypointIndex] != null)
                    {
                        if (Agent.isOnNavMesh) Agent.SetDestination(Waypoints[CurrentWaypointIndex].position);
                    }
                    break;

                case State.Alert:
                    if (Agent.isOnNavMesh)
                    {
                        Agent.speed = AlertSpeed;
                        Agent.ResetPath();
                    }
                    SetVisionColor(AlertVisionColor);
                    AlertTimer = 0f;
                    IsHeadingToSound = false;
                    break;

                case State.Chase:
                    if (Agent.isOnNavMesh)
                    {
                        Agent.speed = ChaseSpeed;
                        Agent.isStopped = false;
                    }
                    SetVisionColor(ChaseVisionColor);
                    RepathTimer = RepathInterval;
                    break;

                case State.Eliminated:
                    break;
            }
        }

        private void UpdatePatrol()
        {
            if (CanSeePlayer())
            {
                SetState(State.Chase);
                return;
            }

            if (CanHearPlayer())
            {
                TargetAlertPosition = Player.transform.position;
                SetState(State.Alert);
                return;
            }

            if (Waypoints == null || Waypoints.Length == 0) return;
            if (!Agent.isOnNavMesh) return;

            if (!Agent.pathPending && Agent.hasPath && Agent.remainingDistance <= WaypointTolerance)
            {
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % Waypoints.Length;
                if (Waypoints[CurrentWaypointIndex] != null)
                {
                    Agent.SetDestination(Waypoints[CurrentWaypointIndex].position);
                }
            }
            else if (!Agent.hasPath)
            {
                if (Waypoints[CurrentWaypointIndex] != null)
                {
                    Agent.SetDestination(Waypoints[CurrentWaypointIndex].position);
                }
            }
        }

        private void UpdateAlert()
        {
            if (CanSeePlayer())
            {
                SetState(State.Chase);
                return;
            }

            if (CanHearPlayer())
            {
                TargetAlertPosition = Player.transform.position;
                AlertTimer = 0f;
                IsHeadingToSound = false;
                if (Agent.isOnNavMesh) Agent.ResetPath();
            }

            AlertTimer += Time.deltaTime;

            if (!IsHeadingToSound)
            {
                Vector3 directionToSound = (TargetAlertPosition - transform.position);
                directionToSound.y = 0f;

                if (directionToSound.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToSound.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
                }

                if (AlertTimer >= LookAtSoundDuration)
                {
                    IsHeadingToSound = true;
                    if (Agent.isOnNavMesh) Agent.SetDestination(TargetAlertPosition);
                }
            }
            else
            {
                if (Agent.isOnNavMesh && !Agent.pathPending && Agent.hasPath && Agent.remainingDistance <= WaypointTolerance)
                {
                    if (AlertTimer >= LookAtSoundDuration + AlertInvestigateDuration)
                    {
                        SetState(State.Patrol);
                    }
                }
            }
        }

        private void UpdateChase()
        {
            if (!CanSeePlayer() && !CanHearPlayer())
            {
                TargetAlertPosition = Player.transform.position;
                SetState(State.Alert);
                return;
            }

            RepathTimer += Time.deltaTime;
            if (RepathTimer >= RepathInterval)
            {
                RepathTimer = 0f;
                if (Agent.isOnNavMesh && Player != null)
                {
                    Agent.SetDestination(Player.transform.position);
                }
            }
        }

        private void CheckCatchProximity()
        {
            if (Player == null || GameManager.Instance == null) return;

            float distance = (transform.position - Player.transform.position).sqrMagnitude;
            if (distance <= CatchDistance * CatchDistance)
            {
                GameManager.Instance.OnPlayerCaught();
            }
        }

        private bool CanSeePlayer()
        {
            if (Player == null) return false;

            float radius = CurrentState == State.Chase ? ChaseViewRadius : ViewRadius;
            float angle = CurrentState == State.Chase ? ChaseViewAngle : ViewAngle;

            Vector3 eyePos = transform.position + Vector3.up * 0.5f;
            Vector3 playerPos = Player.transform.position + Vector3.up * 0.5f;
            Vector3 dirToPlayer = playerPos - eyePos;

            if (dirToPlayer.sqrMagnitude > radius * radius) return false;

            Vector3 forwardFlat = transform.forward;
            forwardFlat.y = 0f;
            Vector3 dirFlat = dirToPlayer;
            dirFlat.y = 0f;

            if (Vector3.Angle(forwardFlat, dirFlat) > angle * 0.5f) return false;

            if (Physics.Raycast(eyePos, dirToPlayer.normalized, out RaycastHit hit, radius, ObstacleMask))
            {
                return hit.transform.GetComponent<PlayerMovement>() != null || hit.transform.IsChildOf(Player.transform);
            }

            return false;
        }

        private bool CanHearPlayer()
        {
            if (Player == null || !Player.IsMakingSound) return false;

            float distance = (transform.position - Player.transform.position).sqrMagnitude;
            float effectiveRadius = HearingRadius + Player.CurrentSoundRadius;
            return distance <= effectiveRadius * effectiveRadius;
        }

        private void UpdateRotation()
        {
            Vector3 velocity = Agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            }
        }

        private void EnsureOnNavMesh()
        {
            if (Agent == null) return;

            if (!Agent.isOnNavMesh)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
                {
                    Agent.Warp(hit.position);
                }
            }
        }

        private void LocatePlayer()
        {
            Player = FindFirstObjectByType<PlayerMovement>();
        }

        private void LocateWaypoints()
        {
            if (Waypoints != null && Waypoints.Length > 0 && Waypoints[0] != null) return;

            Transform root = GameObject.Find("Waypoints")?.transform;
            if (root != null)
            {
                Waypoints = new Transform[root.childCount];
                for (int i = 0; i < root.childCount; i++)
                {
                    Waypoints[i] = root.GetChild(i);
                }
            }
        }

        private void InitializeVisionMesh()
        {
            GameObject coneObj = new GameObject("VisionCone");
            coneObj.transform.SetParent(transform, false);
            coneObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);

            VisionFilter = coneObj.AddComponent<MeshFilter>();
            VisionRenderer = coneObj.AddComponent<MeshRenderer>();

            if (VisionMaterial != null)
            {
                VisionRenderer.material = new Material(VisionMaterial);
            }
            else
            {
                Shader shader = Shader.Find("Sprites/Default");
                VisionRenderer.material = new Material(shader);
            }

            VisionMesh = new Mesh { name = "VisionMesh" };
            VisionFilter.mesh = VisionMesh;

            int count = VisionMeshResolution + 2;
            Vertices = new Vector3[count];
            Triangles = new int[VisionMeshResolution * 3];

            for (int i = 0; i < VisionMeshResolution; i++)
            {
                Triangles[i * 3] = 0;
                Triangles[i * 3 + 1] = i + 1;
                Triangles[i * 3 + 2] = i + 2;
            }
        }

        private void SetVisionColor(Color color)
        {
            if (VisionRenderer != null && VisionRenderer.material != null)
            {
                VisionRenderer.material.color = color;
            }
        }

        private void RenderVision()
        {
            if (VisionMesh == null) return;

            float radius = CurrentState == State.Chase ? ChaseViewRadius : ViewRadius;
            float angle = CurrentState == State.Chase ? ChaseViewAngle : ViewAngle;

            Vertices[0] = Vector3.zero;
            float step = angle / VisionMeshResolution;
            float start = -angle * 0.5f;

            Vector3 rayStart = transform.position + Vector3.up * 0.2f;

            for (int i = 0; i <= VisionMeshResolution; i++)
            {
                float currentAngle = start + (step * i);
                Vector3 localDirection = Quaternion.Euler(0f, currentAngle, 0f) * Vector3.forward;
                Vector3 worldDirection = transform.TransformDirection(localDirection);

                float distance = radius;
                if (Physics.Raycast(rayStart, worldDirection, out RaycastHit hit, radius, ObstacleMask))
                {
                    distance = hit.distance;
                }

                Vertices[i + 1] = localDirection * distance;
            }

            VisionMesh.Clear();
            VisionMesh.vertices = Vertices;
            VisionMesh.triangles = Triangles;
            VisionMesh.RecalculateBounds();
        }
    }
}
