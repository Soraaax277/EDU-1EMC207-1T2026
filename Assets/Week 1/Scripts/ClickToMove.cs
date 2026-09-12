using UnityEngine;
using UnityEngine.AI;

public class ClickToMove : MonoBehaviour
{
    private NavMeshAgent sora;

    void Start()
    {
        sora = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Floor") || hit.collider.gameObject.name == "Floor")
                {
                    Vector3 target = hit.point;
                    Vector3 origin = transform.position;
                    Vector3 dir = target - origin;

                    if (Physics.Raycast(origin, dir.normalized, out RaycastHit wallHit, dir.magnitude))
                    {
                        if (wallHit.collider.gameObject.name != "Floor")
                        {
                            target = wallHit.point - dir.normalized * 0.3f;
                        }
                    }

                    sora.SetDestination(target);
                }
            }
        }
    }
}

