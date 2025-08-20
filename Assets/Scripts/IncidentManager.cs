using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum IncidentState { Idle, AlertRaised, AwaitingAccept, Active, Resolved }

public class IncidentManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] FireManager fireManager;
    [SerializeField] Transform player;
    [SerializeField] WaypointIndicator waypoint;
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
    float tFirstSpray = -1; 
    float tResolved;        

    void Start()
    {
        resultsPanel?.SetActive(false);

        EventBus.OnSprayStarted += OnSprayStart;

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
            float elapsed = Time.time - tAccepted;
            if (hudTimer) hudTimer.text = $"Time: {elapsed:0.0}s";

            float ratio = fireManager.ExtinguishedRatio;
            if (hudProgress) hudProgress.text = $"Extinguished: {(ratio * 100f):0}%";

            if (ratio >= winExtinguishRatio)
                ResolveSuccess();
        }
    }

    void RaiseAlert()
    {
        state = IncidentState.AlertRaised;
        tAlert = Time.time;
        if (hudTimer) hudTimer.text = "Time: 0.0s";

        var target = fireManager.GetIncidentCenter(incidentIndex); 
        waypoint?.SetTarget(target);

        state = IncidentState.AwaitingAccept;
        EventBus.OnAlertRaised?.Invoke();
    }


    void AcceptAlert()
    {
        tAccepted = Time.time;
        var target = fireManager.ActivateIncident(incidentIndex); 
        waypoint?.SetTarget(target);
        state = IncidentState.Active;
        EventBus.OnAlertAccepted?.Invoke();
    }


    void ResolveSuccess()
    {
        if (state != IncidentState.Active) return;
        state = IncidentState.Resolved;
        tResolved = Time.time;

        EventBus.OnMissionSucceeded?.Invoke();

        float acceptDelay = tAccepted - tAlert;
        float firstSprayDelay = (tFirstSpray > 0) ? (tFirstSpray - tAccepted) : -1f;
        float totalExec = tResolved - tAccepted;

        string report = "Mission Success!\n\n" +
                        $"- Accept delay: {acceptDelay:0.0}s\n" +
                        (firstSprayDelay >= 0 ? $"- First spray after accept: {firstSprayDelay:0.0}s\n" : "- No spray recorded\n") +
                        $"- Execution time (accept→resolve): {totalExec:0.0}s\n";

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
