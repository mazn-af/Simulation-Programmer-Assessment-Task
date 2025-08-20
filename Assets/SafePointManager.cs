using UnityEngine;
using System.Collections.Generic;

public class SafePointManager : MonoBehaviour
{
    public static SafePointManager I { get; private set; }
    public List<Transform> safePoints = new List<Transform>();
    void Awake() { I = this; }

    public Transform GetBestSafePoint(Vector3 from, Vector3 threatPos)
    {
        Transform best = null; float bestScore = float.NegativeInfinity;
        foreach (var sp in safePoints)
        {
            if (!sp) continue;
            float away = Vector3.Distance(sp.position, threatPos);
            float dist = Vector3.Distance(sp.position, from);
            float score = away - 0.4f * dist; 
            if (score > bestScore) { bestScore = score; best = sp; }
        }
        return best;
    }
}
