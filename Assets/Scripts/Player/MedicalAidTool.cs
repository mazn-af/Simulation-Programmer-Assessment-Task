using UnityEngine;
using UnityEngine.AI;

public class MedicalAidTool : MonoBehaviour, IUsableTool
{
    [SerializeField] float healPerSecond = 20f;
    [SerializeField] float healRange = 2.5f;
    [SerializeField] LayerMask injuryMask;
    [SerializeField] ParticleSystem healFX;

    bool isUsing;
    Injury current;
    NavMeshAgent currentAgent;

    void Update()
    {
        if (!isUsing)
        {
            if (healFX && healFX.isPlaying) healFX.Stop();
            StopTreat();
            return;
        }

        if (healFX && !healFX.isPlaying) healFX.Play();

        if (current == null || current.IsStable ||
            Vector3.Distance(transform.position, current.transform.position) > healRange)
        {
            StopTreat();
            current = FindNearestInRange();
            if (current != null) StartTreat(current);
        }

        if (current != null)
            current.Stabilize(healPerSecond * Time.deltaTime);
    }

    Injury FindNearestInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, healRange, injuryMask);
        float best = float.MaxValue; Injury pick = null;
        foreach (var h in hits)
        {
            var inj = h.GetComponentInParent<Injury>() ?? h.GetComponent<Injury>();
            if (inj != null && !inj.IsStable)
            {
                float d = (inj.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; pick = inj; }
            }
        }
        return pick;
    }

    void StartTreat(Injury inj)
    {
        current = inj;
        current.BeginTreatment();
        currentAgent = inj.GetComponentInParent<NavMeshAgent>();
        if (currentAgent) currentAgent.isStopped = true;
    }

    void StopTreat()
    {
        if (current != null)
        {
            current.EndTreatment();
            if (currentAgent) { currentAgent.isStopped = false; currentAgent = null; }
            current = null;
        }
    }

    public void Use(bool isUsingNow) { isUsing = isUsingNow; if (!isUsing) StopTreat(); }
    public void TriggerOnce() { }
}
