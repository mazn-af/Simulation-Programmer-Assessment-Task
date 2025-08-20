using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResponderHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerResponder player;
    [SerializeField] IncidentManager incident;

    [Header("Panel (Toast)")]
    [SerializeField] GameObject popUpPanel;
    [SerializeField] TextMeshProUGUI popUpText;
    [SerializeField] float popUpSeconds = 3f;

    [Header("ESC Panel")]
    [SerializeField] GameObject escPanel;
    [SerializeField] TextMeshProUGUI txtIncidentTitle;
    [SerializeField] TextMeshProUGUI txtDistance;
    [SerializeField] Button btnAccept;

    [Header("Tools Bar")]
    [SerializeField] Image[] toolSlots;
    [SerializeField] Sprite[] toolIcons;
    [SerializeField] Color normalColor = new Color(1, 1, 1, 0.6f);
    [SerializeField] Color highlightColor = Color.white;
    [SerializeField] Color inactiveColor = new Color(1, 1, 1, 0.15f);

    Transform currentTarget;
    float toastHideAt = -1f;
    bool escOpen = false;

    void OnEnable()
    {
        EventBus.OnAlertRaised += OnAlertRaised;
        EventBus.OnAlertAccepted += OnAlertAccepted;
        if (player) player.OnToolChanged += OnToolChanged;
    }

    void OnDisable()
    {
        EventBus.OnAlertRaised -= OnAlertRaised;
        EventBus.OnAlertAccepted -= OnAlertAccepted;
        if (player) player.OnToolChanged -= OnToolChanged;
    }

    void Start()
    {
        if (!player) player = FindObjectOfType<PlayerResponder>();
        if (!incident) incident = FindObjectOfType<IncidentManager>();

        if (popUpPanel) popUpPanel.SetActive(false);
        if (escPanel) escPanel.SetActive(false);

        SetupToolBar();
        RefreshToolHighlight(player ? player.CurrentToolIndex : 0);
        CacheIncidentTarget();
        SyncInitialUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleEscPanel();

        if (popUpPanel && popUpPanel.activeSelf && Time.time >= toastHideAt && toastHideAt > 0f)
            popUpPanel.SetActive(false);

        if (escOpen) UpdateEscPanel();
    }

    void ToggleEscPanel(bool? force = null)
    {
        escOpen = force ?? !escOpen;
        if (escPanel) escPanel.SetActive(escOpen);

        if (player) player.SetCursor(escOpen);
        else
        {
            Cursor.visible = escOpen;
            Cursor.lockState = escOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        if (escOpen) CacheIncidentTarget();
    }

    void OnAlertRaised()
    {
        CacheIncidentTarget();
        ShowAlert("Emergency alert received! Press [Y] to accept or [Esc] for details");
        if (escOpen) UpdateEscPanel();
    }

    void OnAlertAccepted()
    {
        ShowAlert("Dispatch accepted. Proceed to incident.");
        CacheIncidentTarget();
        if (escOpen) UpdateEscPanel();
        ToggleEscPanel(false);
    }

    void ShowAlert(string msg)
    {
        if (!popUpPanel || !popUpText) return;
        popUpText.text = msg;
        popUpPanel.SetActive(true);
        toastHideAt = Time.time + popUpSeconds;
    }

    void CacheIncidentTarget()
    {
        if (!incident) { currentTarget = null; return; }
        currentTarget = incident.CurrentTarget ? incident.CurrentTarget : null;
    }

    void UpdateEscPanel()
    {
        if (!escPanel) return;

        string title = incident ? incident.CurrentState.ToString() : "Incident";
        if (txtIncidentTitle) txtIncidentTitle.text = $"Status: {title}";

        float dist = 0f;
        if (player && currentTarget)
            dist = Vector3.Distance(player.transform.position, currentTarget.position);
        if (txtDistance) txtDistance.text = currentTarget ? $"Distance: {dist:0.0} m" : "Distance: —";

        if (btnAccept)
        {
            bool showAccept = incident && incident.CurrentState == IncidentState.AwaitingAccept;
            btnAccept.gameObject.SetActive(showAccept);
            btnAccept.onClick.RemoveAllListeners();
            if (showAccept)
            {
                btnAccept.onClick.AddListener(() =>
                {
                    incident.AcceptAlert();
                });
            }
        }
    }

    void SetupToolBar()
    {
        if (toolSlots == null || toolSlots.Length == 0) return;
        for (int i = 0; i < toolSlots.Length; i++)
        {
            if (!toolSlots[i]) continue;
            Sprite s = (toolIcons != null && i < toolIcons.Length) ? toolIcons[i] : null;
            toolSlots[i].sprite = s;
            toolSlots[i].color = (player && i < player.ToolCount) ? normalColor : inactiveColor;
        }
    }

    void OnToolChanged(int idx) => RefreshToolHighlight(idx);

    void RefreshToolHighlight(int currentIndex)
    {
        if (toolSlots == null) return;
        for (int i = 0; i < toolSlots.Length; i++)
        {
            if (!toolSlots[i]) continue;
            bool active = (player && i < player.ToolCount);
            if (!active) { toolSlots[i].color = inactiveColor; continue; }
            toolSlots[i].color = (i == currentIndex) ? highlightColor : normalColor;
        }
    }

    void SyncInitialUI()
    {
        if (!incident) return;
        if (incident.CurrentState == IncidentState.AwaitingAccept)
            ShowAlert("Emergency alert received! Press [Y] to accept or [Esc] for details");
        else if (incident.CurrentState == IncidentState.Active)
            ShowAlert("Dispatch accepted. Proceed to incident.");
    }
}
