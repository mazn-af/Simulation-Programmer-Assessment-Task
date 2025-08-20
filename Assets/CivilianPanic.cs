using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CivilianPanic : MonoBehaviour
{
    [Header("Threat & Flee")]
    [SerializeField] float panicRadius = 15f;
    [SerializeField] float repathInterval = 0.8f;

    [Header("Speeds")]
    [SerializeField] float panicSpeed = 4.2f;
    [SerializeField] float calmSpeed = 2.2f;

    [Header("Health Link")]
    [SerializeField] Injury injury;

    NavMeshAgent agent;
    float tNext;
    Vector3 currentTarget;
    bool hasTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!injury) injury = GetComponent<Injury>();
    }

    void Update()
    {
        if (injury && (injury.IsBeingTreated || injury.Current <= injury.ImmobilizeBelow))
        {
            StopMove();
            return;
        }

        if (Time.time < tNext) return;
        tNext = Time.time + repathInterval;

        var threat = GetNearestActiveFire();
        if (threat && Vector3.Distance(transform.position, threat.transform.position) <= panicRadius)
        {
            agent.speed = panicSpeed;
            var spm = SafePointManager.I;
            Transform safe = spm ? spm.GetBestSafePoint(transform.position, threat.transform.position) : null;

            if (safe)
            {
                if (NavMesh.SamplePosition(safe.position, out var hit, 3f, NavMesh.AllAreas))
                    SetTarget(hit.position);
            }
            else
            {
                Vector3 away = (transform.position - threat.transform.position).normalized;
                Vector3 fallback = transform.position + away * panicRadius;
                if (NavMesh.SamplePosition(fallback, out var hit2, 3f, NavMesh.AllAreas))
                    SetTarget(hit2.position);
            }
        }
        else
        {
            agent.speed = calmSpeed;
            StopMove();
        }
    }

    FireNode GetNearestActiveFire()
    {
        var cmd = AICommander.Instance;
        if (cmd == null) return null;
        FireNode best = null; float bestSqr = float.MaxValue;
        foreach (var f in cmd.ActiveFires)
        {
            if (!f || !f.IsActive) continue;
            float d = (f.transform.position - transform.position).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = f; }
        }
        return best;
    }

    void SetTarget(Vector3 p)
    {
        if (hasTarget && (p - currentTarget).sqrMagnitude < 0.5f) return;
        currentTarget = p;
        hasTarget = true;
        agent.isStopped = false;
        agent.SetDestination(p);
    }

    void StopMove()
    {
        if (!hasTarget) return;
        hasTarget = false;
        agent.isStopped = true;
        agent.ResetPath();
    }
}
