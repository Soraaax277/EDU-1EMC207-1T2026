using UnityEngine;

namespace Midterms
{
    public class ObjectiveItem : MonoBehaviour
    {
        [SerializeField] private float BobFrequency = 2.5f;
        [SerializeField] private float BobAmplitude = 0.15f;
        [SerializeField] private float RotateSpeed = 80.0f;
        [SerializeField] private float PickupRadius = 1.6f;

        private Vector3 InitialPosition;
        private bool IsCollected;
        private Transform PlayerTransform;

        private void Awake()
        {
            InitialPosition = transform.position;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            FindPlayer();
        }

        private void Update()
        {
            if (IsCollected) return;

            float verticalOffset = Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
            transform.position = InitialPosition + (Vector3.up * verticalOffset);
            transform.Rotate(Vector3.up, RotateSpeed * Time.deltaTime, Space.World);

            CheckProximity();
        }

        private void CheckProximity()
        {
            if (PlayerTransform == null)
            {
                FindPlayer();
                if (PlayerTransform == null) return;
            }

            Vector3 diff = PlayerTransform.position - transform.position;
            diff.y = 0f;

            if (diff.sqrMagnitude <= PickupRadius * PickupRadius)
            {
                Collect();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsCollected) return;

            if (other.GetComponentInParent<PlayerMovement>() != null || other.CompareTag("Player"))
            {
                Collect();
            }
        }

        private void Collect()
        {
            if (IsCollected) return;
            IsCollected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterObjectiveCollected();
            }

            gameObject.SetActive(false);
        }

        private void FindPlayer()
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                PlayerTransform = player.transform;
            }
        }
    }
}
