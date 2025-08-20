using System.Collections.Generic;
using UnityEngine;

public class AICommander : MonoBehaviour
{
    public static AICommander Instance { get; private set; }

    readonly HashSet<FireNode> activeFires = new();
    readonly Dictionary<FireNode, AIResponder> assigned = new();

    void Awake()
    {
        Instance = this;
        EventBus.OnFireIgnited += OnFireIgnited;
        EventBus.OnFireExtinguished += OnFireExtinguished;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        EventBus.OnFireIgnited -= OnFireIgnited;
        EventBus.OnFireExtinguished -= OnFireExtinguished;
    }

    void OnFireIgnited(FireNode n) { if (n) activeFires.Add(n); }
    void OnFireExtinguished(FireNode n)
    {
        activeFires.Remove(n);
        if (n && assigned.ContainsKey(n)) assigned.Remove(n);
    }

    public FireNode RequestNearestUnassigned(Vector3 fromPos)
    {
        FireNode best = null; float bestSqr = float.MaxValue;
        foreach (var f in activeFires)
        {
            if (!f || !f.IsActive) continue;
            if (assigned.ContainsKey(f)) continue;
            float d = (f.transform.position - fromPos).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = f; }
        }
        if (best) assigned[best] = null;
        return best;
    }

    public void ConfirmAssignment(FireNode f, AIResponder who)
    {
        if (!f) return;
        assigned[f] = who;
    }

    public void Release(FireNode f, AIResponder who)
    {
        if (!f) return;
        if (assigned.TryGetValue(f, out var cur) && (cur == who || cur == null))
            assigned.Remove(f);
    }

    public IEnumerable<FireNode> ActiveFires => activeFires;
}
