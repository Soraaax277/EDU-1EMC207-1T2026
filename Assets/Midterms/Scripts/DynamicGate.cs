using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Midterms
{
    [RequireComponent(typeof(NavMeshObstacle))]
    public class DynamicGate : MonoBehaviour
    {
        [SerializeField] private Vector3 OpenOffset = new Vector3(0f, 3.5f, 0f);
        [SerializeField] private float TransitionSpeed = 3.0f;
        [SerializeField] private float OpenDuration = 3.5f;
        [SerializeField] private float ClosedDuration = 3.5f;
        [SerializeField] private bool AutoCycle = true;

        private NavMeshObstacle Obstacle;
        private Vector3 ClosedPosition;
        private Vector3 OpenPosition;
        private bool IsOpen;

        private void Awake()
        {
            Obstacle = GetComponent<NavMeshObstacle>();
            if (Obstacle != null)
            {
                Obstacle.carving = true;
                Obstacle.carveOnlyStationary = false;
            }

            ClosedPosition = transform.position;
            OpenPosition = ClosedPosition + OpenOffset;
        }

        private void Start()
        {
            if (AutoCycle)
            {
                StartCoroutine(CycleRoutine());
            }
        }

        private void Update()
        {
            Vector3 target = IsOpen ? OpenPosition : ClosedPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, TransitionSpeed * Time.deltaTime);
        }

        private IEnumerator CycleRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(ClosedDuration);
                SetGate(true);
                yield return new WaitForSeconds(OpenDuration);
                SetGate(false);
            }
        }

        public void ToggleGate()
        {
            IsOpen = !IsOpen;
        }

        public void SetGate(bool open)
        {
            IsOpen = open;
        }
    }
}
