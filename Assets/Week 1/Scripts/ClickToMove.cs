using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ClickToMove : MonoBehaviour
{
    private NavMeshAgent agent;
    private Camera cam;
    private bool isTraversingLink = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.autoTraverseOffMeshLink = false;
        }
        cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        if (agent == null) return;

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
        {
            if (cam == null)
            {
                cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }

            if (cam != null)
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    agent.SetDestination(hit.point);
                }
            }
        }

        if (agent.isOnOffMeshLink && !isTraversingLink)
        {
            StartCoroutine(TraverseLink());
        }
    }

    IEnumerator TraverseLink()
    {
        isTraversingLink = true;
        agent.isStopped = true;
        agent.updatePosition = false;
        agent.updateRotation = false;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 startPos = transform.position;
        Vector3 endPos = data.endPos;

        float heightDiff = Mathf.Abs(startPos.y - endPos.y);
        float distance = Vector3.Distance(startPos, endPos);

        if (heightDiff > 3f)
        {
            while (Vector3.Distance(transform.position, endPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, endPos, 4f * Time.deltaTime);
                yield return null;
            }
        }
        else if (distance > 3.5f && (agent.areaMask & (1 << 2)) == 0)
        {
            transform.position = endPos;
            yield return null;
        }
        else
        {
            float duration = 0.4f;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float progress = Mathf.Clamp01(time / duration);
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, progress);
                currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;
                transform.position = currentPos;
                yield return null;
            }
        }

        transform.position = endPos;

        if (agent != null)
        {
            if (agent.isOnOffMeshLink)
            {
                agent.CompleteOffMeshLink();
            }
            agent.updatePosition = true;
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        isTraversingLink = false;
    }
}
