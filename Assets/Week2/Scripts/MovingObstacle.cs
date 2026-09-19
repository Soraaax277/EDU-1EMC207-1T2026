using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MovingObstacle : MonoBehaviour
{
    public Vector3 moveOffset = new Vector3(0f, 0f, 5f);
    public float speed = 2.5f;
    public float waitTime = 0.2f;

    private Vector3 posA;
    private Vector3 posB;
    private NavMeshObstacle obstacle;

    void Start()
    {
        posA = transform.position;
        posB = transform.position + transform.TransformDirection(moveOffset);

        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null)
        {
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }

        StartCoroutine(MoveRoutine());
    }

    IEnumerator MoveRoutine()
    {
        while (true)
        {
            yield return MoveToPoint(posB);
            yield return new WaitForSeconds(waitTime);

            yield return MoveToPoint(posA);
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator MoveToPoint(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }
}
