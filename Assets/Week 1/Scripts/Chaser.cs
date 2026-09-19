using UnityEngine;
using UnityEngine.AI;

public class Chaser : MonoBehaviour
{
    public Transform target;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (target == null) return;

        if (transform.position.z * target.position.z < 0)
        {
            float side = transform.position.z > 0 ? 1.6f : -1.6f;
            agent.SetDestination(new Vector3(-6f, transform.position.y, side));
        }
        else
        {
            agent.SetDestination(target.position);
        }
    }
}
