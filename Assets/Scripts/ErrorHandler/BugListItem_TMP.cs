using UnityEngine;
using UnityEngine.UI;
using TMPro;

// BugListItem_TMP: UI element backed by TextMeshProUGUI and a progress bar Image.
// Prefab should include:
// - TMP Text: nameText (TextMeshProUGUI)
// - TMP Text: targetText (TextMeshProUGUI)
// - TMP Text: timerText (TextMeshProUGUI)
// - Image: progressBar (Image with type = Filled, Fill Method = Horizontal)
// - Buttons: assignButton, resolveButton
[RequireComponent(typeof(RectTransform))]
public class BugListItem_TMP : MonoBehaviour
{
    [Header("UI References (TextMeshPro)")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI targetText;
    public TextMeshProUGUI timerText;

    [Header("Progress & Visuals")]
    public Image progressBar; // fillAmount will represent progress 0..1
    public Color colorFull = new Color(0.2f, 0.9f, 0.3f);   // green
    public Color colorMid = new Color(1f, 0.85f, 0.2f);     // yellow
    public Color colorLow = new Color(1f, 0.25f, 0.25f);    // red

    [Header("Buttons")]
    public Button assignButton;
    public Button resolveButton;

    // Animation parameters
    [Header("Countdown Animation")]
    [Tooltip("When time <= warningThreshold*baseDuration, start pulsing.")]
    [Range(0f,1f)]
    public float warningThreshold = 0.25f;
    public float pulseFrequency = 3f;
    public float pulseAmplitude = 0.06f; // fraction of scale

    [Header("Low-Time Warning FX")]
    public LowTimeWarningFX lowTimeWarningFX;
    public bool useWarningFX = true;   // toggle from inspector

    // Internals
    private BugInstance instance;
    private UIManager_uGUI_TMP uiManager;
    private Vector3 baseScale;

    public void Initialize(BugInstance b, UIManager_uGUI_TMP ui)
    {
        instance = b;
        uiManager = ui;

        if (nameText != null) nameText.text = b.Definition.bugName;
        if (targetText != null) targetText.text = b.target ? b.target.name : "<global>";
        if (assignButton != null) assignButton.onClick.AddListener(OnAssignClicked);
        if (resolveButton != null) resolveButton.onClick.AddListener(OnResolveClicked);
        baseScale = transform.localScale;
        RefreshTimer();
    }

    public void RefreshTimer()
    {
        if (instance == null) return;
        float time = Mathf.Max(0f, instance.TimeRemaining);
        float total = Mathf.Max(0.01f, instance.Definition.baseDuration);
        float progress = Mathf.Clamp01(1f - (time / total)); // 0 at spawn -> 1 when expired

        // Update progress bar fill (animated by lerp to make it smooth)
        if (progressBar != null)
        {
            // smooth-ish fill (lerp from current to target)
            progressBar.fillAmount = Mathf.Lerp(progressBar.fillAmount, progress, Time.deltaTime * 6f);
            // Update progress bar color based on progress
            float t = progress; // 0..1 (0 = full time, 1 = expired)
            Color barColor;
            if (t < 0.6f) barColor = Color.Lerp(colorFull, colorMid, Mathf.InverseLerp(0f, 0.6f, t));
            else barColor = Color.Lerp(colorMid, colorLow, Mathf.InverseLerp(0.6f, 1f, t));
            progressBar.color = barColor;
        }

        // Timer text
        if (timerText != null) timerText.text = $"{time:F1}s";

        // Determine whether we are in "warning" zone
        bool inWarning = time <= total * warningThreshold;

        // Warning FX: pulse scale + border + audio (delegated to LowTimeWarningFX)
        if (useWarningFX && lowTimeWarningFX != null)
        {
            if (inWarning)
                lowTimeWarningFX.StartWarning();
            else
                lowTimeWarningFX.StopWarning();
        }
        else
        {
            // fallback scale pulse (legacy behavior)
            if (time <= total * warningThreshold)
            {
                float pulse = 1f + Mathf.PingPong(Time.time * pulseFrequency, pulseAmplitude) - (pulseAmplitude * 0.5f);
                transform.localScale = baseScale * pulse;
            }
            else
            {
                transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 8f);
            }
        }

        // Optionally change text color as time runs low
        if (timerText != null)
        {
            if (time <= total * warningThreshold) timerText.color = colorLow;
            else if (time <= total * 0.6f) timerText.color = colorMid;
            else timerText.color = Color.white;
        }
    }

    void OnAssignClicked()
    {
        if (uiManager != null && instance != null)
            uiManager.AssignBugToPlayer(instance.Id, "Player2");
    }

    void OnResolveClicked()
    {
        if (uiManager != null && instance != null)
            uiManager.AttemptResolveFromUI(instance.Id);
    }

    public void MarkResolved()
    {
        if (nameText != null) nameText.color = Color.gray;
        if (timerText != null) timerText.text = "Resolved";
        if (progressBar != null) progressBar.fillAmount = 1f;
        transform.localScale = baseScale;
        // stop warning if active
        if (lowTimeWarningFX != null) lowTimeWarningFX.StopWarning();
    }

    public void MarkExpired()
    {
        if (nameText != null) nameText.color = Color.magenta;
        if (timerText != null) timerText.text = "Expired";
        if (progressBar != null) progressBar.fillAmount = 1f;
        // stop warning if active
        if (lowTimeWarningFX != null) lowTimeWarningFX.StopWarning();
    }
}
