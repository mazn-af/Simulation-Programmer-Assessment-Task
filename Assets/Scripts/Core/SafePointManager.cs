using UnityEngine;
using System.Collections.Generic;

public class SafePointManager : MonoBehaviour
{
    public static SafePointManager I;

    [SerializeField] Transform[] safePoints;
    [SerializeField] int maxPerPoint = 3;
    [SerializeField] float crowdWeight = 8f;
    [SerializeField] float awayWeight = 12f;  

    readonly Dictionary<Transform, HashSet<int>> occ = new();

    void Awake()
    {
        I = this;
        if (safePoints == null) safePoints = new Transform[0];
        foreach (var sp in safePoints)
            if (sp && !occ.ContainsKey(sp)) occ[sp] = new HashSet<int>();
    }

    public Transform RequestSafePoint(Vector3 from, Vector3 threatPos, int requesterId)
    {
        Transform best = null;
        float bestScore = float.PositiveInfinity;

        Vector3 fromDir = (from - threatPos).normalized;

        for (int i = 0; i < safePoints.Length; i++)
        {
            var sp = safePoints[i];
            if (!sp) continue;

            if (!occ.TryGetValue(sp, out var set)) { set = new HashSet<int>(); occ[sp] = set; }
            int load = set.Count;
            if (load >= maxPerPoint && !set.Contains(requesterId)) continue;

            float dist = Vector3.Distance(from, sp.position);
            Vector3 spDir = (sp.position - threatPos).normalized;
            float away = Mathf.Clamp01(Vector3.Dot(spDir, fromDir)); 

            float score = dist + crowdWeight * (load / (float)maxPerPoint) - awayWeight * away;
            if (score < bestScore)
            {
                bestScore = score;
                best = sp;
            }
        }

        if (best)
        {
            occ[best].Add(requesterId);
        }
        return best;
    }

    public void ReleaseFor(int requesterId)
    {
        foreach (var kv in occ)
        {
            if (kv.Value.Remove(requesterId)) break;
        }
    }

    public Transform GetBestSafePoint(Vector3 from, Vector3 threatPos, int requesterId)
        => RequestSafePoint(from, threatPos, requesterId);
}
