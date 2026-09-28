using UnityEngine;
using UnityEngine.AI;

public class IslandAgent : MonoBehaviour
{
    public float NormalSpeed = 4f;
    public float WaterSpeed = 2f;
    public float ArrivalDistance = 1.5f;

    private NavMeshAgent agent;
    private int currentIsland = -1;
    private int waterMask;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = NormalSpeed;
        
        int waterAreaIndex = NavMesh.GetAreaFromName("Water");
        waterMask = waterAreaIndex >= 0 ? (1 << waterAreaIndex) : (1 << 3);

        PickRandomDestination();
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        CheckSurface();

        if (!agent.pathPending && (agent.remainingDistance <= ArrivalDistance || !agent.hasPath))
        {
            PickRandomDestination();
        }
    }

    void CheckSurface()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
        {
            if ((hit.mask & waterMask) != 0)
            {
                agent.speed = WaterSpeed;
            }
            else
            {
                agent.speed = NormalSpeed;
            }
        }
    }

    public void PickRandomDestination()
    {
        if (AgentSpawner.Islands == null || AgentSpawner.Islands.Length == 0) return;

        int nextIsland = currentIsland;
        int attempts = 0;
        while (nextIsland == currentIsland && attempts < 10)
        {
            nextIsland = Random.Range(0, AgentSpawner.Islands.Length);
            attempts++;
        }

        currentIsland = nextIsland;
        Vector3 targetPos = AgentSpawner.Islands[currentIsland].GetRandomPoint();
        agent.SetDestination(targetPos);
    }
}
