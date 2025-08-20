using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class FireNode : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public bool IsActive => currentHealth > 0.5f;

 

    [Header("Optional explicit lists (else auto-collect)")]
    public ParticleSystem[] particleSystems;
    public Light[] lights;
    public AudioSource fireLoop;

    class PSBaseline
    {
        public ParticleSystem ps;
        public float rateMul0 = 1f;
        public float sizeMul0 = 1f;
        public float lifeMul0 = 1f;
    }
    readonly List<PSBaseline> psList = new();
    readonly List<(Light l, float intensity0)> lightList = new();
    float audioVol0 = 1f;

    void Awake()
    {
       
            if (particleSystems == null || particleSystems.Length == 0)
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if ((lights == null || lights.Length == 0))
                lights = GetComponentsInChildren<Light>(true);
            if (!fireLoop)
                fireLoop = GetComponentInChildren<AudioSource>(true);
        

        psList.Clear();
        foreach (var ps in particleSystems)
        {
            if (!ps) continue;
            var main = ps.main;
            var em = ps.emission;

            psList.Add(new PSBaseline
            {
                ps = ps,
                rateMul0 = em.rateOverTimeMultiplier,
                sizeMul0 = main.startSizeMultiplier,
                lifeMul0 = main.startLifetimeMultiplier
            });
        }

        lightList.Clear();
        foreach (var l in lights)
        {
            if (!l) continue;
            lightList.Add((l, l.intensity));
        }

        if (fireLoop) audioVol0 = fireLoop.volume;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        ApplyByHealth(forcePlay: true);
    }

    void OnEnable()
    {
        if (currentHealth <= 0f) currentHealth = maxHealth;
        ApplyByHealth(forcePlay: true);
        EventBus.OnFireIgnited?.Invoke(this);
    }

    public void Reduce(float amountPerSecond)
    {
        if (!IsActive) return;
        currentHealth = Mathf.Max(0f, currentHealth - amountPerSecond);
        ApplyByHealth(forcePlay: false);

        if (!IsActive)
        {
            StopAllParticles(clear: true);
            if (fireLoop && fireLoop.isPlaying) fireLoop.Stop();
            foreach (var (l, _) in lightList) if (l) l.enabled = false;

            EventBus.OnFireExtinguished?.Invoke(this);
            gameObject.SetActive(false); 
        }
    }

    public void Ignite(float health = -1f)
    {
        currentHealth = (health > 0f) ? Mathf.Min(health, maxHealth) : maxHealth;
        gameObject.SetActive(true);
        ApplyByHealth(forcePlay: true);
        EventBus.OnFireIgnited?.Invoke(this);
    }

    void ApplyByHealth(bool forcePlay)
    {
        float t = Mathf.Clamp01(currentHealth / maxHealth);

        float rateMul = Mathf.Lerp(0.0f, 1.0f, t);          
        float sizeMul = Mathf.Lerp(0.2f, 1.0f, t);          
        float lifeMul = Mathf.Lerp(0.2f, 1.0f, Mathf.Sqrt(t));

        foreach (var b in psList)
        {
            if (b.ps == null) continue;
            var main = b.ps.main;
            var em = b.ps.emission;

            em.rateOverTimeMultiplier = b.rateMul0 * rateMul;
            main.startSizeMultiplier = b.sizeMul0 * sizeMul;
            main.startLifetimeMultiplier = b.lifeMul0 * lifeMul;

            if (forcePlay && t > 0f && !b.ps.isPlaying) b.ps.Play();
            if (t <= 0f && b.ps.isPlaying)
                b.ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        foreach (var (l, i0) in lightList)
        {
            if (!l) continue;
            l.intensity = i0 * t;
            l.enabled = t > 0.02f;
        }

        if (fireLoop)
        {
            fireLoop.volume = audioVol0 * t;
            if (forcePlay && t > 0f && !fireLoop.isPlaying) fireLoop.Play();
            if (t <= 0f && fireLoop.isPlaying) fireLoop.Stop();
        }
    }

    void StopAllParticles(bool clear)
    {
        foreach (var b in psList)
        {
            if (!b.ps) continue;
            b.ps.Stop(true, clear ? ParticleSystemStopBehavior.StopEmittingAndClear
                                  : ParticleSystemStopBehavior.StopEmitting);
        }
    }
}