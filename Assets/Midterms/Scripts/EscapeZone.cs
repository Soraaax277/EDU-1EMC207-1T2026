using UnityEngine;

namespace Midterms
{
    public class EscapeZone : MonoBehaviour
    {
        [SerializeField] private float TriggerRadius = 2.5f;

        private Collider TriggerCollider;
        private Transform PlayerTransform;
        private Renderer[] AllRenderers;

        private void Awake()
        {
            TriggerCollider = GetComponent<Collider>();
            if (TriggerCollider != null)
            {
                TriggerCollider.isTrigger = true;
            }

            AllRenderers = GetComponentsInChildren<Renderer>(true);
            SetZoneActive(false);
        }

        private void Start()
        {
            FindPlayer();

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ObjectiveSecured)
            {
                SetZoneActive(true);
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.ObjectiveSecured) return;

            if (PlayerTransform == null)
            {
                FindPlayer();
                if (PlayerTransform == null) return;
            }

            Vector3 diff = PlayerTransform.position - transform.position;
            diff.y = 0f;

            if (diff.sqrMagnitude <= TriggerRadius * TriggerRadius)
            {
                GameManager.Instance.OnPlayerEscaped();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            SetZoneActive(state == GameState.ObjectiveSecured);
        }

        private void SetZoneActive(bool isActive)
        {
            if (TriggerCollider != null)
            {
                TriggerCollider.enabled = isActive;
            }

            if (AllRenderers != null)
            {
                for (int i = 0; i < AllRenderers.Length; i++)
                {
                    if (AllRenderers[i] != null)
                    {
                        AllRenderers[i].enabled = isActive;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.ObjectiveSecured) return;

            if (other.GetComponentInParent<PlayerMovement>() != null || other.CompareTag("Player"))
            {
                GameManager.Instance.OnPlayerEscaped();
            }
        }

        private void FindPlayer()
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                PlayerTransform = player.transform;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, TriggerRadius);
        }
    }
}
