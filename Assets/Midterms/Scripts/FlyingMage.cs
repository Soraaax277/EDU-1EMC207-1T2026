using UnityEngine;

namespace Midterms
{
    public class FlyingMage : MonoBehaviour
    {
        [Header("Waypoints")]
        [SerializeField] private Vector3[] PatrolWaypoints;
        [SerializeField] private float CruiseSpeed = 3.8f;
        [SerializeField] private float BaseAltitude = 3.2f;
        [SerializeField] private float BuildingClearance = 2.0f;
        [SerializeField] private float WaypointTolerance = 1.5f;
        [SerializeField] private float TurnSpeed = 4.0f;

        [Header("Hover Dynamics")]
        [SerializeField] private float BobAmplitude = 0.25f;
        [SerializeField] private float BobFrequency = 2.0f;
        [SerializeField] private float AltitudeSmoothSpeed = 4.0f;

        [Header("Detection")]
        [SerializeField] private PlayerMovement Player;
        [SerializeField] private float SearchlightRadius = 3.2f;
        [SerializeField] private float CatchDistance = 1.8f;
        [SerializeField] private Light Spotlight;
        [SerializeField] private float GracePeriodDuration = 3.0f;

        private int CurrentWaypointIndex;
        private bool IsEliminated;
        private float TargetAltitude;
        private float CurrentAltitude;
        private float GraceTimer;
        private Transform GroundShadow;

        private void Start()
        {
            GraceTimer = GracePeriodDuration;

            if (Player == null)
            {
                Player = FindFirstObjectByType<PlayerMovement>();
            }

            if (PatrolWaypoints == null || PatrolWaypoints.Length == 0)
            {
                PatrolWaypoints = new Vector3[]
                {
                    new Vector3(-9.0f, BaseAltitude, 7.0f),
                    new Vector3(-5.0f, BaseAltitude, 12.0f),
                    new Vector3(2.5f, BaseAltitude, 8.5f),
                    new Vector3(-5.0f, BaseAltitude, 7.0f)
                };
            }

            TargetAltitude = BaseAltitude;
            CurrentAltitude = BaseAltitude;

            EnsureSpotlight();
            EnsureGroundShadow();
        }

        private void Update()
        {
            if (IsEliminated) return;

            PatrolAlongWaypoints();
            AdjustAltitudeOverObstacles();
            CheckSearchlightDetection();
            UpdateGroundShadow();
        }

        private void PatrolAlongWaypoints()
        {
            if (PatrolWaypoints == null || PatrolWaypoints.Length == 0) return;

            Vector3 targetWaypoint = PatrolWaypoints[CurrentWaypointIndex];
            Vector3 flatTarget = new Vector3(targetWaypoint.x, transform.position.y, targetWaypoint.z);
            Vector3 moveDirection = (flatTarget - transform.position);

            if (moveDirection.sqrMagnitude <= WaypointTolerance * WaypointTolerance)
            {
                CurrentWaypointIndex = (CurrentWaypointIndex + 1) % PatrolWaypoints.Length;
                targetWaypoint = PatrolWaypoints[CurrentWaypointIndex];
                flatTarget = new Vector3(targetWaypoint.x, transform.position.y, targetWaypoint.z);
                moveDirection = (flatTarget - transform.position);
            }

            if (moveDirection.sqrMagnitude > 0.05f)
            {
                Vector3 flatDir = moveDirection.normalized;
                flatDir.y = 0f;
                if (flatDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(flatDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, TurnSpeed * Time.deltaTime);
                }

                transform.position += flatDir * CruiseSpeed * Time.deltaTime;
            }
        }

        private void AdjustAltitudeOverObstacles()
        {
            Vector3 rayOrigin = new Vector3(transform.position.x, 30f, transform.position.z);
            float targetGroundY = 0f;

            int mask = ~LayerMask.GetMask("Ignore Raycast");
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 50f, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.transform.root != transform.root)
                {
                    if (hits[i].point.y > targetGroundY)
                    {
                        targetGroundY = hits[i].point.y;
                    }
                }
            }

            float desiredAltitude = targetGroundY + BaseAltitude;
            if (targetGroundY > 0.5f)
            {
                desiredAltitude += BuildingClearance;
            }

            desiredAltitude = Mathf.Clamp(desiredAltitude, BaseAltitude, 6.5f);

            TargetAltitude = Mathf.Lerp(TargetAltitude, desiredAltitude, AltitudeSmoothSpeed * Time.deltaTime);
            float bobOffset = Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
            CurrentAltitude = TargetAltitude + bobOffset;

            transform.position = new Vector3(transform.position.x, CurrentAltitude, transform.position.z);
        }

        private void CheckSearchlightDetection()
        {
            if (GraceTimer > 0f)
            {
                GraceTimer -= Time.deltaTime;
                return;
            }

            if (Player == null)
            {
                Player = FindFirstObjectByType<PlayerMovement>();
                if (Player == null) return;
            }

            Vector3 searchlightOrigin = transform.position + Vector3.down * 0.5f;
            Vector3 playerTarget = Player.transform.position + Vector3.up * 0.8f;
            Vector3 toPlayer = playerTarget - searchlightOrigin;

            Vector3 flatMage = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 flatPlayer = new Vector3(Player.transform.position.x, 0f, Player.transform.position.z);
            float flatDistanceSqr = (flatMage - flatPlayer).sqrMagnitude;

            bool inProximity = flatDistanceSqr <= CatchDistance * CatchDistance;
            bool inSearchlight = flatDistanceSqr <= SearchlightRadius * SearchlightRadius;

            if (inProximity)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Caught)
                {
                    GameManager.Instance.OnPlayerCaught();
                }
                return;
            }

            if (inSearchlight)
            {
                if (Physics.Raycast(searchlightOrigin, toPlayer.normalized, out RaycastHit hit, toPlayer.magnitude + 0.5f))
                {
                    if (hit.collider.transform.root == Player.transform.root || hit.collider.CompareTag("Player"))
                    {
                        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Caught)
                        {
                            GameManager.Instance.OnPlayerCaught();
                        }
                    }
                }
            }
        }

        public void Eliminate()
        {
            if (IsEliminated) return;
            IsEliminated = true;

            if (Spotlight != null)
            {
                Spotlight.enabled = false;
            }

            if (GroundShadow != null)
            {
                GroundShadow.gameObject.SetActive(false);
            }

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            StartCoroutine(GroundMageRoutine());
        }

        private System.Collections.IEnumerator GroundMageRoutine()
        {
            Vector3 startPos = transform.position;
            Vector3 groundPos = new Vector3(startPos.x, 0.1f, startPos.z);
            Quaternion startRot = transform.rotation;
            Quaternion tumbleRot = startRot * Quaternion.Euler(60f, 30f, 0f);

            float duration = 1.0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeIn = t * t;
                transform.position = Vector3.Lerp(startPos, groundPos, easeIn);
                transform.rotation = Quaternion.Slerp(startRot, tumbleRot, easeIn);
                yield return null;
            }

            yield return new WaitForSeconds(1.0f);
            gameObject.SetActive(false);
        }

        private void EnsureSpotlight()
        {
            if (Spotlight == null)
            {
                Spotlight = GetComponentInChildren<Light>();
            }

            if (Spotlight == null)
            {
                GameObject lightObj = new GameObject("Mage_Searchlight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = new Vector3(0f, -0.2f, 0.4f);
                lightObj.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

                Spotlight = lightObj.AddComponent<Light>();
                Spotlight.type = LightType.Spot;
                Spotlight.spotAngle = 60f;
                Spotlight.innerSpotAngle = 35f;
                Spotlight.range = 15f;
                Spotlight.color = new Color(0.8f, 0.2f, 1f, 1f);
                Spotlight.intensity = 3.5f;
            }
        }

        private void EnsureGroundShadow()
        {
            Transform existing = transform.Find("GroundShadow");
            if (existing != null)
            {
                GroundShadow = existing;
                return;
            }

            GameObject shadowObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadowObj.name = "GroundShadow";
            shadowObj.transform.SetParent(transform, false);
            shadowObj.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);

            Collider col = shadowObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer rend = shadowObj.GetComponent<MeshRenderer>();
            if (rend != null)
            {
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
#if UNITY_EDITOR
                Material shadowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Midterms/Materials/ShadowMat.mat");
                if (shadowMat != null)
                {
                    rend.material = shadowMat;
                }
#endif
            }

            GroundShadow = shadowObj.transform;
            UpdateGroundShadow();
        }

        private void UpdateGroundShadow()
        {
            if (GroundShadow == null) return;

            RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
            float highestGroundY = 0.02f;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.transform.root != transform.root)
                {
                    if (hits[i].point.y > highestGroundY)
                    {
                        highestGroundY = hits[i].point.y + 0.03f;
                    }
                }
            }

            GroundShadow.position = new Vector3(transform.position.x, highestGroundY, transform.position.z);
            GroundShadow.rotation = Quaternion.identity;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.8f, 0.2f, 1f, 0.35f);
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, 0f, transform.position.z), SearchlightRadius);
        }
    }
}
