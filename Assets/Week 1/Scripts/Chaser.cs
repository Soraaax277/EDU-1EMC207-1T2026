using UnityEngine;
using UnityEngine.AI;

public class Chaser : MonoBehaviour
{
    public Transform sora;
    private NavMeshAgent aros;

    void Start()
    {
        aros = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (sora == null) return;

        if (transform.position.z * sora.position.z < 0)
        {
            float side = transform.position.z > 0 ? 1.6f : -1.6f;
            aros.SetDestination(new Vector3(-6f, transform.position.y, side));
        }
        else
        {
            aros.SetDestination(sora.position);
        }
    }
}
