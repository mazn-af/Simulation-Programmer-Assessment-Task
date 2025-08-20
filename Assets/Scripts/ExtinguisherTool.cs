using UnityEngine;

public class ExtinguisherTool : MonoBehaviour, IUsableTool
{
    [Header("Spray")]
    [SerializeField] float range = 6f;
    [SerializeField] float coneAngle = 25f;
    [SerializeField] float dousePerSecond = 35f;
    [SerializeField] LayerMask fireMask;

    [Header("FX")]
    [SerializeField] ParticleSystem sprayFX;
    [SerializeField] AudioSource sprayAudio;

    bool lastUsing = false;

    public void Use(bool isUsing)
    {
        if (sprayFX)
        {
            if (isUsing && !sprayFX.isPlaying) sprayFX.Play();
            else if (!isUsing && sprayFX.isPlaying) sprayFX.Stop();
        }
        if (sprayAudio)
        {
            if (isUsing && !sprayAudio.isPlaying) sprayAudio.Play();
            else if (!isUsing && sprayAudio.isPlaying) sprayAudio.Stop();
        }

        if (isUsing && !lastUsing) EventBus.OnSprayStarted?.Invoke();
        if (!isUsing && lastUsing) EventBus.OnSprayStopped?.Invoke();
        lastUsing = isUsing;

        if (!isUsing) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, range, fireMask);
        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            Vector3 dir = (t.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) <= coneAngle)
            {
                var fire = t.GetComponent<FireNode>();
                if (fire != null && fire.IsActive)
                    fire.Reduce(dousePerSecond * Time.deltaTime);
            }
        }
    }

    public void TriggerOnce() { }
}
