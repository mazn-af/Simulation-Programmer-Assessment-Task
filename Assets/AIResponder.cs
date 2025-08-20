using UnityEngine;
using UnityEngine.AI;
using System.Linq;

[RequireComponent(typeof(NavMeshAgent))]
public class AIResponder : MonoBehaviour
{
    public enum Mode { FollowLeader, FightFire, Heal }

    [Header("Refs")]
    [SerializeField] Transform leader;
    [SerializeField] ExtinguisherTool extinguisher;
    [SerializeField] MedicalAidTool medic;
    [SerializeField] LayerMask injuryMask;

    [Header("Distances")]
    [SerializeField] float followDistance = 3.5f;
    [SerializeField] float detectFireRadius = 30f;
    [SerializeField] float fireActionDistance = 4.5f;
    [SerializeField] float detectInjuryRadius = 18f;
    [SerializeField] float healActionDistance = 2.2f;

    [Header("Timings")]
    [SerializeField] float retargetInterval = 0.6f;
    [SerializeField] float faceTurnSpeed = 12f;

    NavMeshAgent agent;
    Mode mode = Mode.FollowLeader;
    FireNode fireTarget;
    Injury healTarget;
    float tNextThink;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!leader && GameObject.FindWithTag("Player"))
            leader = GameObject.FindWithTag("Player").transform;

        EventBus.OnFireExtinguished += OnFireDone;
    }

    void OnDestroy()
    {
        EventBus.OnFireExtinguished -= OnFireDone;
        if (fireTarget) AICommander.Instance?.Release(fireTarget, this);
    }

    void Update()
    {
        if (Time.time >= tNextThink) { Think(); tNextThink = Time.time + retargetInterval; }
        Act();
    }

    void Think()
    {
        if (TryPickFire()) return;
        if (TryPickInjury()) return;
        mode = Mode.FollowLeader;
        fireTarget = null;
        healTarget = null;
    }

    bool TryPickFire()
    {
        if (!extinguisher) return false;

        if (fireTarget && fireTarget.IsActive)
        {
            mode = Mode.FightFire;
            return true;
        }

        var cmd = AICommander.Instance;
        if (cmd == null) return false;

        var candidate = cmd.RequestNearestUnassigned(transform.position);
        if (candidate)
        {
            fireTarget = candidate;
            cmd.ConfirmAssignment(candidate, this);
            mode = Mode.FightFire;
            return true;
        }

        var any = cmd.ActiveFires.FirstOrDefault(f => f && f.IsActive &&
                     (f.transform.position - transform.position).sqrMagnitude <= detectFireRadius * detectFireRadius);
        if (any)
        {
            fireTarget = any;
            mode = Mode.FightFire;
            return true;
        }

        return false;
    }

    bool TryPickInjury()
    {
        if (!medic) return false;
        Collider[] hits = Physics.OverlapSphere(transform.position, detectInjuryRadius, injuryMask);
        float best = float.MaxValue; Injury bestInj = null;
        foreach (var h in hits)
        {
            var inj = h.GetComponentInParent<Injury>() ?? h.GetComponent<Injury>();
            if (inj == null || inj.IsStable) continue;
            float d = (inj.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; bestInj = inj; }
        }
        if (bestInj)
        {
            healTarget = bestInj;
            mode = Mode.Heal;
            return true;
        }
        return false;
    }

    void Act()
    {
        switch (mode)
        {
            case Mode.FollowLeader:
                if (leader)
                {
                    float d = Vector3.Distance(transform.position, leader.position);
                    if (d > followDistance) agent.SetDestination(leader.position);
                    UseTools(false, false);
                }
                break;

            case Mode.FightFire:
                if (!fireTarget || !fireTarget.IsActive) { AICommander.Instance?.Release(fireTarget, this); fireTarget = null; mode = Mode.FollowLeader; UseTools(false, false); return; }
                Vector3 p = fireTarget.transform.position;
                float df = Vector3.Distance(transform.position, p);
                if (df > fireActionDistance) agent.SetDestination(p);
                else
                {
                    agent.ResetPath();
                    Face(p);
                    UseTools(true, false);
                }
                break;

            case Mode.Heal:
                if (!healTarget || healTarget.IsStable) { healTarget = null; mode = Mode.FollowLeader; UseTools(false, false); return; }
                Vector3 hpos = healTarget.transform.position;
                float dh = Vector3.Distance(transform.position, hpos);
                if (dh > healActionDistance) agent.SetDestination(hpos);
                else
                {
                    agent.ResetPath();
                    Face(hpos);
                    UseTools(false, true);
                }
                break;
        }
    }

    void UseTools(bool useExt, bool useMed)
    {
        if (extinguisher) extinguisher.Use(useExt);
        if (medic) medic.Use(useMed);
    }

    void Face(Vector3 worldPos)
    {
        Vector3 dir = (worldPos - transform.position); dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion q = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, q, Time.deltaTime * faceTurnSpeed);
    }

    void OnFireDone(FireNode n)
    {
        if (n == fireTarget)
        {
            AICommander.Instance?.Release(fireTarget, this);
            fireTarget = null;
            mode = Mode.FollowLeader;
        }
    }
}
