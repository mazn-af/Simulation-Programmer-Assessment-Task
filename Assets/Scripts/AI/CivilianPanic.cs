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
    bool hasTarget;

    Transform mySafePoint;
    Vector3 myOffset;       
    int myId;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!injury) injury = GetComponent<Injury>();
        myId = GetInstanceID();
    }

    void OnDisable()
    {
        SafePointManager.I?.ReleaseFor(myId);
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

            if (!mySafePoint) 
            {
                mySafePoint = SafePointManager.I ?
                    SafePointManager.I.RequestSafePoint(transform.position, threat.transform.position, myId) : null;

                float r = Random.Range(0.6f, 1.4f);
                float a = Random.Range(0f, Mathf.PI * 2f);
                myOffset = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
            }

            Vector3 target = transform.position;
            if (mySafePoint)
                target = mySafePoint.position + myOffset;
            else
                target = transform.position + (transform.position - threat.transform.position).normalized * panicRadius;

            if (NavMesh.SamplePosition(target, out var hit, 2.5f, NavMesh.AllAreas))
                SetTarget(hit.position);
        }
        else
        {
            agent.speed = calmSpeed;
            ReleaseSafe();
            StopMove();
        }
    }

    void ReleaseSafe()
    {
        if (mySafePoint)
        {
            SafePointManager.I?.ReleaseFor(myId);
            mySafePoint = null;
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
        if (hasTarget && (p - agent.destination).sqrMagnitude < 0.25f) return;
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
