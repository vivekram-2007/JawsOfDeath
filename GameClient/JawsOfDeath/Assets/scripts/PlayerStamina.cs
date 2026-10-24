using UnityEngine;
using UnityEngine.UI;
using StarterAssets;

public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [Tooltip("Seconds of sprinting from a full bar")]
    public float sprintSeconds = 8f;

    [Tooltip("Seconds to refill from empty to full")]
    public float refillSeconds = 5f;

    [Tooltip("Pause after using stamina before it starts refilling")]
    public float regenDelay = 1.5f;

    [Tooltip("Fraction of the bar one roll costs (0.2 = 20%)")]
    [Range(0f, 1f)] public float rollCost = 0.2f;

    [Tooltip("After running dry, sprint and roll stay locked until the bar reaches this")]
    [Range(0f, 1f)] public float recoverThreshold = 0.3f;

    [Header("Bar")]
    public Color barColor = new Color(0.53f, 0.81f, 0.92f, 1f); // sky blue
    public float fadeInSpeed = 6f;
    public float fadeOutSpeed = 1.5f;

    // same size and left position as the health bar, sitting just above it
    private float barWidth = 255f;
    private float barHeight = 8f;
    private Vector2 barPosition = new Vector2(60f, 62f);

    private float stamina = 1f;
    private float regenTimer;
    private bool exhausted;

    private CanvasGroup group;
    private RectTransform fill;

    void Start()
    {
        BuildUI();
    }

    void OnEnable()
    {
        ThirdPersonController.Rolled += OnRolled;
    }

    void OnDisable()
    {
        ThirdPersonController.Rolled -= OnRolled;
        ThirdPersonController.SprintAllowed = true;
        ThirdPersonController.RollAllowed = true;
    }

    void OnRolled()
    {
        stamina = Mathf.Max(0f, stamina - rollCost);
        regenTimer = regenDelay;
        if (stamina <= 0f) exhausted = true;
    }

    void Update()
    {
        bool sprinting = ThirdPersonController.IsSprinting;

        if (sprinting)
        {
            stamina -= Time.deltaTime / Mathf.Max(0.1f, sprintSeconds);
            regenTimer = regenDelay;
        }
        else if (regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
        }
        else
        {
            stamina += Time.deltaTime / Mathf.Max(0.1f, refillSeconds);
        }

        stamina = Mathf.Clamp01(stamina);

        if (!exhausted && stamina <= 0f)
        {
            exhausted = true;
            regenTimer = regenDelay;
        }
        if (exhausted && stamina >= recoverThreshold)
        {
            exhausted = false;
        }

        ThirdPersonController.SprintAllowed = !exhausted;
        ThirdPersonController.RollAllowed = !exhausted;

        // bar: hidden when full, fades in while sprinting, rolling or recovering
        bool show = stamina < 0.999f || exhausted || sprinting;
        float targetAlpha = show ? 1f : 0f;
        float speed = show ? fadeInSpeed : fadeOutSpeed;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, speed * Time.deltaTime);

        fill.sizeDelta = new Vector2(barWidth * stamina, barHeight);
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("StaminaHUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        group = canvasObj.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // Bar background
        GameObject bg = new GameObject("StaminaBG");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        bgImg.raycastTarget = false;
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = new Vector2(0, 0);
        bgRect.anchoredPosition = barPosition;
        bgRect.sizeDelta = new Vector2(barWidth, barHeight);

        // Sky blue fill
        GameObject fillObj = new GameObject("StaminaFill");
        fillObj.transform.SetParent(bg.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = barColor;
        fillImg.raycastTarget = false;
        fill = fillObj.GetComponent<RectTransform>();
        fill.anchorMin = fill.anchorMax = fill.pivot = new Vector2(0, 0.5f);
        fill.anchoredPosition = Vector2.zero;
        fill.sizeDelta = new Vector2(barWidth, barHeight);
    }
}