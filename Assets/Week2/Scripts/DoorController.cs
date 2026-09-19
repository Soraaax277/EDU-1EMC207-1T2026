using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class DoorController : MonoBehaviour
{
    public Vector3 closedAngle = Vector3.zero;
    public Vector3 openAngle = new Vector3(0f, 80f, 0f);
    public float doorSpeed = 2.5f;
    public float openTime = 3f;
    public float closedTime = 3f;

    private NavMeshObstacle obstacle;

    void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle != null)
        {
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }

        StartCoroutine(DoorLoop());
    }

    IEnumerator DoorLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(closedTime);
            yield return RotateTo(openAngle);
            yield return new WaitForSeconds(openTime);
            yield return RotateTo(closedAngle);
        }
    }

    IEnumerator RotateTo(Vector3 targetEuler)
    {
        Quaternion targetRot = Quaternion.Euler(targetEuler);
        while (Quaternion.Angle(transform.localRotation, targetRot) > 0.5f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, doorSpeed * Time.deltaTime);
            yield return null;
        }
        transform.localRotation = targetRot;
    }
}
