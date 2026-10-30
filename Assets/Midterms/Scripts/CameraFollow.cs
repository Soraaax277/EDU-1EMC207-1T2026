using UnityEngine;

namespace Midterms
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform Target;
        [SerializeField] private Vector3 Offset = new Vector3(0f, 10.0f, -6.5f);
        [SerializeField] private float SmoothSpeed = 8.0f;

        private void Start()
        {
            FindTarget();

            if (Target != null)
            {
                transform.position = Target.position + Offset;
                transform.rotation = Quaternion.LookRotation(Target.position - transform.position);
            }
        }

        private void LateUpdate()
        {
            if (Target == null)
            {
                FindTarget();
                if (Target == null) return;
            }

            Vector3 targetPosition = Target.position + Offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, SmoothSpeed * Time.deltaTime);
        }

        private void FindTarget()
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                Target = player.transform;
            }
        }
    }
}
