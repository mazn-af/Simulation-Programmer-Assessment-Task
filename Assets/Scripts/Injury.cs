using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Injury : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float current = 60f;
    [SerializeField] float decayPerSec = 2f;
    [SerializeField] float immobilizeBelow = 30f;

    [Header("World-Space UI (optional)")]
    [SerializeField] bool useWorldspaceBar = true;
    [SerializeField] Slider healthSlider;         
    [SerializeField] Transform uiRoot;            
    [SerializeField] Camera lookAtCamera;

    public float MaxHealth => maxHealth;
    public float Current => current;
    public float ImmobilizeBelow => immobilizeBelow;
    public bool IsStable => current >= maxHealth - 0.01f;
    public bool IsBeingTreated { get; private set; }

    static readonly HashSet<Injury> s_All = new HashSet<Injury>();

    public static bool AllStable()
    {
        foreach (var i in s_All) { if (i && !i.IsStable) return false; }
        return true;
    }
    public static int TotalCount()
    {
        int c = 0; foreach (var i in s_All) if (i) c++; return c;
    }
    public static int UnstableCount()
    {
        int c = 0; foreach (var i in s_All) if (i && !i.IsStable) c++; return c;
    }

    public void BeginTreatment() { IsBeingTreated = true; }
    public void EndTreatment() { IsBeingTreated = false; }

    public void Stabilize(float amt)
    {
        current = Mathf.Min(maxHealth, current + amt);
        if (IsStable) EndTreatment();
    }

    void OnEnable()
    {
        s_All.Add(this);
        if (useWorldspaceBar)
        {
            if (!healthSlider) healthSlider = GetComponentInChildren<Slider>(true);
            if (!lookAtCamera) lookAtCamera = Camera.main;
            if (!uiRoot && healthSlider) uiRoot = healthSlider.transform;
            if (healthSlider) healthSlider.maxValue = maxHealth;
        }
    }

    void OnDisable()
    {
        s_All.Remove(this);
    }

    void Update()
    {
        if (!IsStable && !IsBeingTreated)
            current = Mathf.Max(0f, current - decayPerSec * Time.deltaTime);

        if (useWorldspaceBar && healthSlider)
        {
            healthSlider.value = current;

            if (uiRoot && lookAtCamera)
            {
                var f = lookAtCamera.transform.rotation * Vector3.forward;
                var u = lookAtCamera.transform.rotation * Vector3.up;
                uiRoot.transform.LookAt(uiRoot.position + f, u);
            }
        }
    }
}
