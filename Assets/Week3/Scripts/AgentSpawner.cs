using System.Collections;
using UnityEngine;

public class AgentSpawner : MonoBehaviour
{
    public static IslandTarget[] Islands;

    public GameObject AgentPrefab;
    public int AgentCount = 150;
    public IslandTarget[] IslandTargets;
    public Material[] AgentMaterials;

    void Awake()
    {
        if (IslandTargets == null || IslandTargets.Length == 0)
        {
            IslandTargets = FindObjectsByType<IslandTarget>(FindObjectsSortMode.None);
        }
        Islands = IslandTargets;
    }

    IEnumerator Start()
    {
        yield return null;
        SpawnAgents();
    }

    void SpawnAgents()
    {
        if (AgentPrefab == null || Islands == null || Islands.Length == 0) return;

        for (int i = 0; i < AgentCount; i++)
        {
            IslandTarget startIsland = Islands[i % Islands.Length];
            Vector3 pos = startIsland.GetRandomPoint();

            GameObject agent = Instantiate(AgentPrefab, pos, Quaternion.identity, transform);

            if (AgentMaterials != null && AgentMaterials.Length > 0)
            {
                MeshRenderer rend = agent.GetComponentInChildren<MeshRenderer>();
                if (rend != null)
                {
                    rend.material = AgentMaterials[i % AgentMaterials.Length];
                }
            }
        }
    }
}
