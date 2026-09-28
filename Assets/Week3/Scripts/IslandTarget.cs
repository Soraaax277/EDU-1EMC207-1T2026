using UnityEngine;
using UnityEngine.AI;

public class IslandTarget : MonoBehaviour
{
    public int IslandIndex;
    public float Radius = 4f;

    public Vector3 GetRandomPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * Radius;
        Vector3 point = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(point, out NavMeshHit hit, Radius + 2f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return transform.position;
    }
}
