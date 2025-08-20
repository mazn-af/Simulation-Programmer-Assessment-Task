using UnityEngine;

public class Injury : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;
    [SerializeField] float current = 60f;
    [SerializeField] float decayPerSec = 2f;
    [SerializeField] float immobilizeBelow = 30f;

    public float MaxHealth => maxHealth;
    public float Current => current;
    public float ImmobilizeBelow => immobilizeBelow;
    public bool IsStable => current >= maxHealth - 0.01f;
    public bool IsBeingTreated { get; private set; }

    public void BeginTreatment() { IsBeingTreated = true; }
    public void EndTreatment() { IsBeingTreated = false; }

    public void Stabilize(float amt)
    {
        current = Mathf.Min(maxHealth, current + amt);
        if (IsStable) EndTreatment();
    }

    void Update()
    {
        if (!IsStable && !IsBeingTreated)
            current = Mathf.Max(0f, current - decayPerSec * Time.deltaTime);
    }
}
