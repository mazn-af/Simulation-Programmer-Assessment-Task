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
    [SerializeField] AIToolView toolView;

    [Header("Distances")]
    [SerializeField] float followDistance = 3.5f;
    [SerializeField] float detectFireRadius = 30f;
    [SerializeField] float fireActionDistance = 4.5f;
    [SerializeField] float detectInjuryRadius = 18f;
    [SerializeField] float healActionDistance = 2.2f;

    [Header("Follow Offsets")]
    [SerializeField] float followBackOffset = 3.5f;
    [SerializeField] float followSideOffset = 1.2f;
    [SerializeField] float followDeceleration = 0.4f;
    Vector3 lastFollowTarget;

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

        agent.autoBraking = true;
        agent.stoppingDistance = Mathf.Max(followDistance, 0.6f);
        agent.avoidancePriority = 50;


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
        agent.isStopped = false;

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
                    Vector3 back = -leader.forward * followBackOffset;
                    Vector3 side = leader.right * followSideOffset;
                    Vector3 target = leader.position + back + side;

                    if (NavMesh.SamplePosition(target, out var hit, 2.0f, NavMesh.AllAreas))
                        target = hit.position;

                    float dist = Vector3.Distance(transform.position, target);
                    bool farEnough = dist > agent.stoppingDistance + followDeceleration;
                    bool targetMoved = (lastFollowTarget - target).sqrMagnitude > 0.25f;

                    if (farEnough || targetMoved)
                    {
                        GoTo(target, Mathf.Max(followDistance, 0.6f));
                        lastFollowTarget = target;
                    }
                    else if (dist <= agent.stoppingDistance)
                    {
                        agent.ResetPath();
                        agent.isStopped = true;
                        Vector3 f = leader.forward; f.y = 0f;
                        if (f.sqrMagnitude > 0.001f)
                            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(f, Vector3.up), Time.deltaTime * faceTurnSpeed);
                    }

                    UseTools(false, false);
                }
                break;



            case Mode.FightFire:
                if (!fireTarget || !fireTarget.IsActive)
                {
                    AICommander.Instance?.Release(fireTarget, this);
                    fireTarget = null; mode = Mode.FollowLeader; UseTools(false, false);
                    agent.isStopped = false;
                    break;
                }
                {
                    Vector3 p = fireTarget.transform.position;
                    float df = Vector3.Distance(transform.position, p);
                    if (df > fireActionDistance)
                    {
                        GoTo(p, fireActionDistance * 0.8f);
                        UseTools(false, false);
                    }
                    else
                    {
                        agent.ResetPath();
                        agent.isStopped = true;
                        Face(p);
                        UseTools(true, false);
                    }
                }
                break;


            case Mode.Heal:
                if (!healTarget || healTarget.IsStable)
                {
                    healTarget = null; mode = Mode.FollowLeader; UseTools(false, false);
                    agent.isStopped = false;
                    break;
                }
                {
                    Vector3 hpos = healTarget.transform.position;
                    float dh = Vector3.Distance(transform.position, hpos);
                    if (dh > healActionDistance)
                    {
                        GoTo(hpos, healActionDistance * 0.8f);
                        UseTools(false, false);
                    }
                    else
                    {
                        agent.ResetPath();
                        agent.isStopped = true;
                        Face(hpos);
                        UseTools(false, true);
                    }
                }
                break;

        }
    }

    void UseTools(bool useExt, bool useMed)
    {
        if (extinguisher) extinguisher.Use(useExt);
        if (medic) medic.Use(useMed);

        if (toolView)
        {
            if (useExt) toolView.ShowExtinguisher();
            else if (useMed) toolView.ShowMedic();
            else toolView.ShowNone();
        }
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

    void GoTo(Vector3 p, float stopDist)
    {
        if (!agent.enabled) return;
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var snap, 2.5f, NavMesh.AllAreas))
                agent.Warp(snap.position);
            else return;
        }
        agent.stoppingDistance = Mathf.Max(0.1f, stopDist);
        agent.isStopped = false;
        agent.SetDestination(p);
    }
    
}
