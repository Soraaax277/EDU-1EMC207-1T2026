using UnityEngine;
using UnityEngine.AI;

namespace Midterms
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CharacterAnimatorController : MonoBehaviour
    {
        [SerializeField] private Animator TargetAnimator;

        private NavMeshAgent Agent;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            LocateAnimator();
        }

        private void Update()
        {
            if (TargetAnimator == null)
            {
                LocateAnimator();
                if (TargetAnimator == null) return;
            }

            float currentSpeed = (Agent != null && Agent.isOnNavMesh) ? Agent.velocity.magnitude : 0f;
            TargetAnimator.SetFloat("Speed", currentSpeed);
        }

        private void LocateAnimator()
        {
            Animator[] animators = GetComponentsInChildren<Animator>();
            foreach (var anim in animators)
            {
                if (anim != null && anim.gameObject != gameObject)
                {
                    TargetAnimator = anim;
                    return;
                }
            }

            if (TargetAnimator == null)
            {
                TargetAnimator = GetComponent<Animator>();
            }
        }
    }
}
