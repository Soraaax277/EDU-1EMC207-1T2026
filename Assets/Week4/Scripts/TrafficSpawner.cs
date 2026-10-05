using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TrafficSpawner : MonoBehaviour
{
    public GameObject[] CarPrefabs;
    public GameObject FlyingAgentPrefab;
    public PatrolRoute GroundRoute;
    public PatrolRoute AirRoute;
    public int CarCount = 100;

    private void Start()
    {
        SpawnFlyingAgent();
        SpawnAllCars();
    }

    private void SpawnFlyingAgent()
    {
        Vector3 spawnPosition = AirRoute.GetPosition(0) + Vector3.up * FlyingAgentPrefab.GetComponent<FlyingAgent>().CruiseHeight;
        FlyingAgent flyingAgent = Instantiate(FlyingAgentPrefab, spawnPosition, Quaternion.identity).GetComponent<FlyingAgent>();
        flyingAgent.AirRoute = AirRoute;
        flyingAgent.GroundRoute = GroundRoute;
        flyingAgent.AlternateRoutes = true;
    }

    private void SpawnAllCars()
    {
        float[] laneOffsets = { -1.1f, 0f, 1.1f };
        for (int i = 0; i < CarCount; i++)
        {
            float t = (float)i / CarCount;
            float laneOffset = laneOffsets[i % laneOffsets.Length];
            float speed = 4.5f + (i % 5) * 0.25f;

            Vector3 targetPosition = GroundRoute.GetPointOnRoute(t, laneOffset, out int nextIndex, out Vector3 forwardDirection);

            if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                if (hit.position.y < 0.5f)
                {
                    targetPosition = hit.position;
                }
                else
                {
                    targetPosition = GroundRoute.GetPointOnRoute(t, 0f, out nextIndex, out forwardDirection);
                    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit groundHit, 3f, NavMesh.AllAreas))
                    {
                        targetPosition = groundHit.position;
                    }
                }
            }

            if (Physics.CheckSphere(targetPosition + Vector3.up * 0.5f, 0.7f, 1 << 0))
            {
                targetPosition = GroundRoute.GetPointOnRoute(t, 0f, out nextIndex, out forwardDirection);
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit groundHit, 3f, NavMesh.AllAreas))
                {
                    targetPosition = groundHit.position;
                }
            }

            GameObject prefab = CarPrefabs[i % CarPrefabs.Length];
            GameObject car = Instantiate(prefab, targetPosition, Quaternion.LookRotation(forwardDirection), transform);

            NavMeshAgent navAgent = car.GetComponent<NavMeshAgent>();
            navAgent.avoidancePriority = i % 100;

            GroundAgent groundAgent = car.GetComponent<GroundAgent>();
            groundAgent.Initialize(GroundRoute, nextIndex, laneOffset, speed);
        }
    }
}
