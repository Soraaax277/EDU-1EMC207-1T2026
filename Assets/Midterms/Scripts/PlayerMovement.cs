using UnityEngine;
using UnityEngine.AI;

namespace Midterms
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Speeds")]
        [SerializeField] private float NormalSpeed = 3.5f;
        [SerializeField] private float StealthSpeed = 1.6f;
        [SerializeField] private float RotationSpeed = 12.0f;
        [SerializeField] private float WaterSlowdownMultiplier = 0.5f;

        [Header("Stealth & Sound")]
        [SerializeField] private float SoundRadius = 6.0f;
        [SerializeField] private float AttackSoundRadius = 10.0f;

        [Header("Combat Ranges")]
        [SerializeField] private float MeleeTakedownDistance = 2.8f;
        [SerializeField] private float ThrowTakedownDistance = 14.0f;

        [Header("Layers & Detection")]
        [SerializeField] private LayerMask GroundMask = ~0;

        private static readonly RaycastHit[] CachedSphereHits = new RaycastHit[16];

        private NavMeshAgent Agent;
        private Camera MainCamera;
        private int WaterAreaMask;
        private int JumpAreaMask;
        private bool IsInWater;
        private bool IsInWaterTrigger;
        private float NoiseTimer;
        private float TerrainCheckTimer;
        private bool HasEquippedSword;
        private bool HasEquippedSmokeBomb;
        private bool IsTraversingLink;

        public bool IsStealthMode { get; private set; }
        public bool IsMakingSound { get; private set; }
        public float CurrentSoundRadius => NoiseTimer > 0f ? AttackSoundRadius : SoundRadius;

        public EnemyAI HoveredEnemy { get; private set; }
        public FlyingMage HoveredMage { get; private set; }
        public bool IsHoveredEnemyInRange { get; private set; }

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Agent.updateRotation = false;
            Agent.autoTraverseOffMeshLink = false;
            Agent.speed = NormalSpeed;
            MainCamera = Camera.main;

            int waterIndex = NavMesh.GetAreaFromName("Water");
            WaterAreaMask = waterIndex >= 0 ? (1 << waterIndex) : (1 << 3);

            int jumpIndex = NavMesh.GetAreaFromName("Jump");
            JumpAreaMask = jumpIndex >= 0 ? (1 << jumpIndex) : (1 << 2);
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.ObjectiveSecured)
            {
                EquipArsenal();
            }
        }

        private void Update()
        {
            if (MainCamera == null)
            {
                MainCamera = Camera.main;
            }

            CheckOffMeshLinkTraversal();
            CheckSurfaceWater();
            CheckSecuredWeaponState();
            HandleInput();
            HandleSpeed();
            HandleRotation();
            EvaluateSound();
        }

        private void CheckOffMeshLinkTraversal()
        {
            if (Agent != null && Agent.isOnOffMeshLink && !IsTraversingLink)
            {
                StartCoroutine(TraverseLinkRoutine());
            }
        }

        private System.Collections.IEnumerator TraverseLinkRoutine()
        {
            IsTraversingLink = true;

            try
            {
                if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh || !Agent.isOnOffMeshLink)
                {
                    yield break;
                }

                OffMeshLinkData data = Agent.currentOffMeshLinkData;
                Vector3 startPos = Agent.transform.position;
                Vector3 endPos = data.endPos + Vector3.up * Agent.baseOffset;

                Agent.updatePosition = false;
                Agent.updateRotation = false;

                PlayTriggerAnimation("Jump");
                SetBoolAnimation("IsJumping", true);

                float jumpDistance = Vector3.Distance(startPos, endPos);
                float jumpDuration = Mathf.Clamp(jumpDistance / (NormalSpeed * 1.35f), 0.45f, 1.2f);
                float elapsed = 0f;

                float arcHeight = Mathf.Max(0.8f, Mathf.Abs(endPos.y - startPos.y) * 0.35f + 0.5f);

                while (elapsed < jumpDuration)
                {
                    if (this == null || Agent == null || !Agent.enabled)
                    {
                        yield break;
                    }

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / jumpDuration);

                    Vector3 currentXZ = Vector3.Lerp(startPos, endPos, t);
                    float arcY = Mathf.Sin(t * Mathf.PI) * arcHeight;
                    currentXZ.y = Mathf.Lerp(startPos.y, endPos.y, t) + arcY;

                    transform.position = currentXZ;

                    Vector3 forward = (endPos - startPos);
                    forward.y = 0f;
                    if (forward.sqrMagnitude > 0.05f)
                    {
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(forward.normalized), RotationSpeed * Time.deltaTime);
                    }

                    yield return null;
                }

                if (this != null && Agent != null && Agent.enabled)
                {
                    transform.position = endPos;
                    Agent.Warp(endPos);
                    Agent.updatePosition = true;

                    SetBoolAnimation("IsJumping", false);
                    PlayTriggerAnimation("Land");

                    if (Agent.isActiveAndEnabled && Agent.isOnNavMesh && Agent.isOnOffMeshLink)
                    {
                        Agent.CompleteOffMeshLink();
                    }
                }
            }
            finally
            {
                IsTraversingLink = false;
            }
        }

        private void SetBoolAnimation(string paramName, bool val)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(false);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator anim = animators[i];
                if (anim != null && anim.enabled && anim.runtimeAnimatorController != null)
                {
                    anim.SetBool(paramName, val);
                }
            }
        }

        private void CheckSurfaceWater()
        {
            if (IsInWaterTrigger)
            {
                IsInWater = true;
                return;
            }

            TerrainCheckTimer -= Time.deltaTime;
            if (TerrainCheckTimer > 0f) return;
            TerrainCheckTimer = 0.15f;

            if (Agent == null || !Agent.enabled || !Agent.isOnNavMesh)
            {
                IsInWater = false;
                return;
            }

            if (Agent.velocity.sqrMagnitude < 0.05f)
            {
                return;
            }

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

        private void CheckSecuredWeaponState()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ObjectiveSecured)
            {
                if (!HasEquippedSword || (GameManager.Instance.IsFinalLevel && !HasEquippedSmokeBomb))
                {
                    EquipArsenal();
                }
            }
        }

        private void HandleInput()
        {
            HoveredEnemy = null;
            HoveredMage = null;
            IsHoveredEnemyInRange = false;

            if (MainCamera != null)
            {
                Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);

                if (GameManager.Instance != null && GameManager.Instance.CanEliminateEnemies)
                {
                    bool foundTarget = false;
                    if (Physics.Raycast(ray, out RaycastHit directHit, 100f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        var directEnemy = directHit.collider.GetComponentInParent<EnemyAI>();
                        if (directEnemy != null && directEnemy.CurrentState != EnemyAI.State.Eliminated)
                        {
                            HoveredEnemy = directEnemy;
                            HoveredMage = null;
                            float distSqr = (transform.position - directEnemy.transform.position).sqrMagnitude;
                            IsHoveredEnemyInRange = distSqr <= MeleeTakedownDistance * MeleeTakedownDistance;
                            foundTarget = true;
                        }
                        else
                        {
                            var directMage = directHit.collider.GetComponentInParent<FlyingMage>();
                            if (directMage != null)
                            {
                                HoveredMage = directMage;
                                HoveredEnemy = null;
                                float distSqr = (transform.position - directMage.transform.position).sqrMagnitude;
                                IsHoveredEnemyInRange = distSqr <= ThrowTakedownDistance * ThrowTakedownDistance;
                                foundTarget = true;
                            }
                        }
                    }

                    if (!foundTarget)
                    {
                        int hitCount = Physics.SphereCastNonAlloc(ray, 1.2f, CachedSphereHits, 100f, ~0, QueryTriggerInteraction.Ignore);
                        float closestHitDist = float.MaxValue;

                        for (int i = 0; i < hitCount; i++)
                        {
                            var hitCol = CachedSphereHits[i].collider;
                            if (hitCol == null) continue;

                            var enemy = hitCol.GetComponentInParent<EnemyAI>();
                            if (enemy != null && enemy.CurrentState != EnemyAI.State.Eliminated)
                            {
                                if (CachedSphereHits[i].distance < closestHitDist)
                                {
                                    closestHitDist = CachedSphereHits[i].distance;
                                    HoveredEnemy = enemy;
                                    HoveredMage = null;
                                    float distSqr = (transform.position - enemy.transform.position).sqrMagnitude;
                                    IsHoveredEnemyInRange = distSqr <= MeleeTakedownDistance * MeleeTakedownDistance;
                                }
                            }

                            var mage = hitCol.GetComponentInParent<FlyingMage>();
                            if (mage != null)
                            {
                                if (CachedSphereHits[i].distance < closestHitDist)
                                {
                                    closestHitDist = CachedSphereHits[i].distance;
                                    HoveredMage = mage;
                                    HoveredEnemy = null;
                                    float distSqr = (transform.position - mage.transform.position).sqrMagnitude;
                                    IsHoveredEnemyInRange = distSqr <= ThrowTakedownDistance * ThrowTakedownDistance;
                                }
                            }
                        }
                    }
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (GameManager.Instance != null && GameManager.Instance.CanEliminateEnemies)
                    {
                        if (HoveredEnemy != null)
                        {
                            float distanceSqr = (transform.position - HoveredEnemy.transform.position).sqrMagnitude;
                            if (distanceSqr <= MeleeTakedownDistance * MeleeTakedownDistance)
                            {
                                PlayTriggerAnimation("Attack");
                                HoveredEnemy.Eliminate();
                                TriggerAttackNoise(HoveredEnemy.transform.position);
                                HoveredEnemy = null;
                                return;
                            }
                        }

                        if (HoveredMage != null)
                        {
                            float distSqr = (transform.position - HoveredMage.transform.position).sqrMagnitude;
                            if (distSqr <= ThrowTakedownDistance * ThrowTakedownDistance)
                            {
                                PlayTriggerAnimation("Throw");
                                Vector3 magePos = HoveredMage.transform.position;
                                HoveredMage.Eliminate();
                                TriggerAttackNoise(magePos);
                                HoveredMage = null;
                                return;
                            }
                        }
                    }

                    if (!IsTraversingLink && Physics.Raycast(ray, out RaycastHit hit, 100f, GroundMask))
                    {
                        if (hit.normal.y < 0.5f)
                        {
                            return;
                        }

                        Vector3 targetPosition = hit.point;
                        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit navHit, 1.5f, NavMesh.AllAreas))
                        {
                            targetPosition = navHit.position;
                        }

                        bool playerOnGround = transform.position.y < 1.0f;
                        bool targetOnGround = targetPosition.y < 1.0f;

                        if (playerOnGround && targetOnGround)
                        {
                            Agent.areaMask = NavMesh.AllAreas & ~JumpAreaMask;
                        }
                        else
                        {
                            Agent.areaMask = NavMesh.AllAreas;
                        }

                        NavMeshPath path = new NavMeshPath();
                        if (Agent.CalculatePath(targetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
                        {
                            Agent.SetPath(path);
                        }
                    }
                }
            }

            IsStealthMode = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        private void HandleSpeed()
        {
            float baseSpeed = IsStealthMode ? StealthSpeed : NormalSpeed;
            if (IsInWater)
            {
                baseSpeed *= WaterSlowdownMultiplier;
            }

            Agent.speed = baseSpeed;
        }

        private void HandleRotation()
        {
            Vector3 velocity = Agent.velocity;
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
            }
        }

        private void EvaluateSound()
        {
            if (NoiseTimer > 0f)
            {
                NoiseTimer -= Time.deltaTime;
                IsMakingSound = true;
                return;
            }

            bool isMoving = Agent.velocity.sqrMagnitude > 0.1f;
            IsMakingSound = isMoving && !IsStealthMode;
        }

        private void PlayTriggerAnimation(string triggerName)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(false);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator anim = animators[i];
                if (anim != null && anim.enabled && anim.runtimeAnimatorController != null && anim.gameObject != gameObject)
                {
                    anim.SetTrigger(triggerName);
                    return;
                }
            }

            for (int i = 0; i < animators.Length; i++)
            {
                Animator anim = animators[i];
                if (anim != null && anim.enabled && anim.runtimeAnimatorController != null)
                {
                    anim.SetTrigger(triggerName);
                    return;
                }
            }
        }

        private void TriggerAttackNoise(Vector3 soundOrigin)
        {
            NoiseTimer = 1.0f;
            IsMakingSound = true;

            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                {
                    float distSqr = (enemies[i].transform.position - soundOrigin).sqrMagnitude;
                    if (distSqr <= AttackSoundRadius * AttackSoundRadius)
                    {
                        enemies[i].InvestigateSound(soundOrigin);
                    }
                }
            }
        }

        public void EquipArsenal()
        {
            AttachSwordToHand();

            if (GameManager.Instance != null && GameManager.Instance.IsFinalLevel)
            {
                AttachSmokeBombToHand();
            }
        }

        public void AttachSwordToHand()
        {
            if (HasEquippedSword) return;

            Transform rightHandSlot = FindHandSlot(isRightHand: true);
            if (rightHandSlot == null) return;

            GameObject swordPrefab = null;
#if UNITY_EDITOR
            swordPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Midterms/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE/Assets/fbx/sword_1handed.fbx");
            if (swordPrefab == null)
            {
                swordPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Midterms/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE/Assets/fbx/sword_2handed_color.fbx");
            }
#endif
            if (swordPrefab != null)
            {
                HasEquippedSword = true;
                GameObject swordInstance = Instantiate(swordPrefab, rightHandSlot);
                swordInstance.name = "Equipped_Sword";

                if (rightHandSlot.name.Equals("RightHandSocket", System.StringComparison.OrdinalIgnoreCase))
                {
                    swordInstance.transform.localPosition = Vector3.zero;
                    swordInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    swordInstance.transform.localScale = Vector3.one;
                }
                else if (rightHandSlot.name.IndexOf("Arm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    swordInstance.transform.localPosition = new Vector3(0.08f, 0.45f, 0.25f);
                    swordInstance.transform.localRotation = Quaternion.Euler(15f, 90f, 0f);
                    swordInstance.transform.localScale = Vector3.one;
                }
                else
                {
                    swordInstance.transform.localPosition = Vector3.zero;
                    swordInstance.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    swordInstance.transform.localScale = Vector3.one;
                }

#if UNITY_EDITOR
                Material rogueMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Midterms/Materials/RogueMaterial.mat");
                if (rogueMat != null)
                {
                    foreach (var rend in swordInstance.GetComponentsInChildren<Renderer>())
                    {
                        rend.material = rogueMat;
                    }
                }
#endif
            }
        }

        public void AttachSmokeBombToHand()
        {
            if (HasEquippedSmokeBomb) return;

            Transform leftHandSlot = FindHandSlot(isRightHand: false);
            if (leftHandSlot == null) return;

            GameObject bombPrefab = null;
#if UNITY_EDITOR
            bombPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Midterms/KayKit_Adventurers_2.0_FREE/KayKit_Adventurers_2.0_FREE/Assets/fbx/smokebomb.fbx");
#endif
            if (bombPrefab != null)
            {
                HasEquippedSmokeBomb = true;
                GameObject bombInstance = Instantiate(bombPrefab, leftHandSlot);
                bombInstance.name = "Equipped_SmokeBomb";

                if (leftHandSlot.name.Equals("LeftHandSocket", System.StringComparison.OrdinalIgnoreCase))
                {
                    bombInstance.transform.localPosition = Vector3.zero;
                    bombInstance.transform.localRotation = Quaternion.identity;
                    bombInstance.transform.localScale = Vector3.one;
                }
                else if (leftHandSlot.name.IndexOf("Arm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bombInstance.transform.localPosition = new Vector3(-0.08f, 0.45f, 0.25f);
                    bombInstance.transform.localRotation = Quaternion.identity;
                    bombInstance.transform.localScale = Vector3.one;
                }
                else
                {
                    bombInstance.transform.localPosition = Vector3.zero;
                    bombInstance.transform.localRotation = Quaternion.identity;
                    bombInstance.transform.localScale = Vector3.one;
                }

#if UNITY_EDITOR
                Material rogueMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Midterms/Materials/RogueMaterial.mat");
                if (rogueMat != null)
                {
                    foreach (var rend in bombInstance.GetComponentsInChildren<Renderer>())
                    {
                        rend.material = rogueMat;
                    }
                }
#endif
            }
        }

        private Transform FindHandSlot(bool isRightHand)
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i].isHuman)
                {
                    Transform bone = animators[i].GetBoneTransform(isRightHand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
                    if (bone != null) return bone;
                }
            }

            string[] searchTokens = isRightHand
                ? new string[] { "handslot.r", "hand.r", "wrist.r", "arm.r", "RogueHooded_ArmRight" }
                : new string[] { "handslot.l", "hand.l", "wrist.l", "arm.l", "RogueHooded_ArmLeft" };

            Transform visualRoot = transform.Find("VisualModel");
            if (visualRoot == null) visualRoot = transform;

            Transform[] allBones = visualRoot.GetComponentsInChildren<Transform>(true);
            for (int t = 0; t < searchTokens.Length; t++)
            {
                string token = searchTokens[t];
                for (int i = 0; i < allBones.Length; i++)
                {
                    if (allBones[i].name.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return allBones[i];
                    }
                }
            }

            string socketName = isRightHand ? "RightHandSocket" : "LeftHandSocket";
            Transform existingSocket = visualRoot.Find(socketName);
            if (existingSocket != null)
            {
                return existingSocket;
            }

            GameObject socket = new GameObject(socketName);
            socket.transform.SetParent(visualRoot, false);
            socket.transform.localPosition = isRightHand ? new Vector3(0.28f, 0.45f, 0.2f) : new Vector3(-0.28f, 0.45f, 0.2f);
            socket.transform.localRotation = Quaternion.identity;
            socket.transform.localScale = Vector3.one;

            return socket.transform;
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
    }
}
