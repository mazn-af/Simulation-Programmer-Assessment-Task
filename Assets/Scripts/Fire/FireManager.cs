using UnityEngine;
using System.Collections.Generic;

public class FireManager : MonoBehaviour
{
    [SerializeField] FirePool pool;
    [SerializeField] Transform[] incidentPoints;
    [SerializeField] int firesPerIncident = 3;    
    [SerializeField] float spawnRadius = 0f;       

    readonly List<FireNode> activeFires = new();

    int totalFires = 0;
    int extinguishedFires = 0;

    void OnEnable() { EventBus.OnFireExtinguished += HandleExtinguished; }
    void OnDisable() { EventBus.OnFireExtinguished -= HandleExtinguished; }

    public float ExtinguishedRatio
    {
        get
        {
            if (totalFires <= 0) return 1f; 
            return (float)extinguishedFires / totalFires;
        }
    }

    public Transform GetIncidentCenter(int index)
    {
        if (incidentPoints == null || incidentPoints.Length == 0) return null;
        index = Mathf.Clamp(index, 0, incidentPoints.Length - 1);
        return incidentPoints[index];
    }

    public Transform ActivateIncident(int index)
    {
        ClearAll();

        if (incidentPoints == null || incidentPoints.Length == 0)
        {
          #if UNITY_EDITOR
            Debug.LogWarning("FireManager: No incidentPoints assigned.");
          #endif
            return null;
        }

        totalFires = 0;
        extinguishedFires = 0;

        int count = Mathf.Min(firesPerIncident, incidentPoints.Length);
        float r = Mathf.Max(spawnRadius, 0f);

        for (int i = 0; i < count; i++)
        {
            var center = incidentPoints[i];
            Vector3 pos = center.position;

            if (r > 0f)
            {
                Vector2 d = Random.insideUnitCircle * r;
                pos += new Vector3(d.x, 0f, d.y);
            }

            var node = pool.Get(pos, Quaternion.identity);
            activeFires.Add(node);
            totalFires++; 
        }

        return incidentPoints[0];
    }

    void HandleExtinguished(FireNode node)
    {
        extinguishedFires = Mathf.Min(extinguishedFires + 1, totalFires);

        if (node) StartCoroutine(ReturnAfterFrame(node));
    }

    System.Collections.IEnumerator ReturnAfterFrame(FireNode node)
    {
        yield return null; yield return null;
        if (node) pool.Return(node);
        activeFires.Remove(node);
    }

    public void ClearAll()
    {
        for (int i = 0; i < activeFires.Count; i++)
        {
            var n = activeFires[i];
            if (n) pool.Return(n);
        }
        activeFires.Clear();

        totalFires = 0;
        extinguishedFires = 0;
    }
}
