using UnityEngine;
using UnityEngine.AI;

namespace Midterms
{
    [RequireComponent(typeof(Collider))]
    public class TerrainHazard : MonoBehaviour
    {
        [SerializeField] private float SpeedMultiplier = 0.5f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<NavMeshAgent>(out var agent))
            {
                agent.speed *= SpeedMultiplier;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<NavMeshAgent>(out var agent))
            {
                agent.speed /= SpeedMultiplier;
            }
        }
    }
}
