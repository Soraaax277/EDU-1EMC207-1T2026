using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class BridgeManager : MonoBehaviour
{
    [System.Serializable]
    public class Blocker
    {
        public GameObject BlockerObject;
        public Vector3 OpenPosition;
        public Vector3 ClosedPosition;
        public bool IsBlocked;
    }

    public NavMeshSurface NavMeshSurface;
    public Blocker[] Blockers;
    public float Interval = 3f;
    public float BlockDuration = 3f;
    public float MoveSpeed = 6f;

    void Start()
    {
        if (NavMeshSurface == null)
        {
            NavMeshSurface = FindFirstObjectByType<NavMeshSurface>();
        }

        StartCoroutine(BlockerLoop());
    }

    IEnumerator BlockerLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Interval);

            if (Blockers != null && Blockers.Length > 0)
            {
                int index = Random.Range(0, Blockers.Length);
                Blocker blocker = Blockers[index];

                blocker.IsBlocked = true;
                yield return StartCoroutine(MoveBlocker(blocker));
                UpdateSurface();

                yield return new WaitForSeconds(BlockDuration);

                blocker.IsBlocked = false;
                yield return StartCoroutine(MoveBlocker(blocker));
                UpdateSurface();
            }
        }
    }

    void UpdateSurface()
    {
        if (NavMeshSurface != null && NavMeshSurface.navMeshData != null)
        {
            NavMeshSurface.UpdateNavMesh(NavMeshSurface.navMeshData);
        }
    }

    IEnumerator MoveBlocker(Blocker blocker)
    {
        if (blocker.BlockerObject == null) yield break;

        Vector3 target = blocker.IsBlocked ? blocker.ClosedPosition : blocker.OpenPosition;
        Transform blockerTransform = blocker.BlockerObject.transform;

        while (Vector3.Distance(blockerTransform.position, target) > 0.05f)
        {
            blockerTransform.position = Vector3.MoveTowards(blockerTransform.position, target, MoveSpeed * Time.deltaTime);
            yield return null;
        }

        blockerTransform.position = target;
    }
}
