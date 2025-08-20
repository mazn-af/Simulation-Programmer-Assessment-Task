using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum IncidentState { Idle, AlertRaised, AwaitingAccept, Active, Resolved }

public class IncidentManager : MonoBehaviour
{
    [Header("Refs")]
    public FireManager fireManager;
    [SerializeField] Transform player;
    [SerializeField] WaypointIndicator waypoint;
    [SerializeField] GameObject inCallHud;
    [SerializeField] TextMeshProUGUI hudState;
    [SerializeField] TextMeshProUGUI hudTimer;
    [SerializeField] TextMeshProUGUI hudProgress;
    [SerializeField] GameObject resultsPanel;
    [SerializeField] TextMeshProUGUI resultsText;

    [Header("Settings")]
    [SerializeField] int incidentIndex = 0;
    [SerializeField] float winExtinguishRatio = 0.9f;
    [SerializeField] KeyCode acceptKey = KeyCode.Y;

    IncidentState state = IncidentState.Idle;

    float tAlert;
    float tAccepted;
    float tFirstSpray = -1f;
    float tResolved;

    public IncidentState CurrentState => state;
    public Transform CurrentTarget { get; private set; }
    public float ElapsedSinceAccept => (state == IncidentState.Active) ? (Time.time - tAccepted) : 0f;

    void Start()
    {
        if (resultsPanel) resultsPanel.SetActive(false);
        EventBus.OnSprayStarted += OnSprayStart;
        inCallHud.SetActive(false);
        RaiseAlert();
    }

    void OnDestroy()
    {
        EventBus.OnSprayStarted -= OnSprayStart;
    }

    void Update()
    {
        if (hudState) hudState.text = $"State: {state}";

        if (state == IncidentState.AwaitingAccept && Input.GetKeyDown(acceptKey))
            AcceptAlert();

        if (state == IncidentState.Active)
        {
            if (hudTimer) hudTimer.text = $"Time: {ElapsedSinceAccept:0.0}s";

            if (state == IncidentState.Active)
            {
                if (hudTimer) hudTimer.text = $"Time: {ElapsedSinceAccept:0.0}s";

                float ratio = fireManager ? fireManager.ExtinguishedRatio : 0f;
                if (hudProgress) hudProgress.text = $"Extinguished: {(ratio * 100f):0}%";

                bool firesOK = ratio >= winExtinguishRatio;
                bool injuriesOK =Injury.AllStable();

                if (firesOK && injuriesOK)
                    ResolveSuccess();
            }

        }
    }

    public Transform GetIncidentCenter(int index)
    {
        if (!fireManager) return null;
        return fireManager.GetIncidentCenter(index);
    }

    void RaiseAlert()
    {
        state = IncidentState.AlertRaised;
        tAlert = Time.time;
        if (hudTimer) hudTimer.text = "Time: 0.0s";

        CurrentTarget = GetIncidentCenter(incidentIndex);
        if (waypoint) waypoint.SetTarget(CurrentTarget);

        state = IncidentState.AwaitingAccept;
        EventBus.OnAlertRaised?.Invoke();
    }

    public void AcceptAlert()
    {
        waypoint.transform.gameObject.SetActive(true);
        tAccepted = Time.time;
        inCallHud.SetActive(true);
        var center = fireManager ? fireManager.ActivateIncident(incidentIndex) : null;
        CurrentTarget = center ? center : GetIncidentCenter(incidentIndex);

        if (waypoint) waypoint.SetTarget(CurrentTarget);

        state = IncidentState.Active;
        EventBus.OnAlertAccepted?.Invoke();
    }

    void ResolveSuccess()
    {
        if (state != IncidentState.Active) return;
        waypoint.transform.gameObject.SetActive(false);

        state = IncidentState.Resolved;
        tResolved = Time.time;

        EventBus.OnMissionSucceeded?.Invoke();

        float acceptDelay = tAccepted - tAlert;
        float firstSprayDelay = (tFirstSpray > 0) ? (tFirstSpray - tAccepted) : -1f;
        float totalExec = tResolved - tAccepted;

        string report = "Mission Success!\n\n" +
                        $"- Accept delay: {acceptDelay:0.0}s\n" +
                        (firstSprayDelay >= 0 ? $"- First spray after accept: {firstSprayDelay:0.0}s\n" : "- No spray recorded\n") +
                        $"- Execution time (accept To resolve): {totalExec:0.0}s\n";

        if (acceptDelay > 8f) report += "Tip: Accept faster to reduce dispatch latency.\n";
        if (firstSprayDelay > 6f) report += "Tip: Engage the fire sooner after arrival.\n";
        if (totalExec > 90f) report += "Tip: Prioritize the most intense fires first.\n";

        if (resultsPanel) resultsPanel.SetActive(true);
        if (resultsText) resultsText.text = report;
    }

    void OnSprayStart()
    {
        if (state == IncidentState.Active && tFirstSpray < 0f)
            tFirstSpray = Time.time;
    }
}
