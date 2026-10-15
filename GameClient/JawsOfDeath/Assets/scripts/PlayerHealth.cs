using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Vector3 lastDeathPosition;
    public Font deathFont; // optional: drag a .ttf here to override the default serif

    public GameObject deathFirePrefab; // drag Fire001 prefab here
    public float deathFireScale = 1f;

    [Header("Damage Screen Effect")]
    public float fadeInSpeed = 6f;
    public float fadeOutSpeed = 1.2f;
    [Range(0f, 1f)] public float darkness = 0.45f;
    [Range(0f, 1f)] public float bloodAmount = 0.9f;

    private GameObject deathFire;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private CharacterController controller;
    private RectTransform barFill;
    private float barWidth = 255f;
    private Image overlay;
    private Text deathText;
    private Image darkImg;
    private Image bloodImg;
    private float intensity;
    private float hurtTimer;

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        startRotation = transform.rotation;
        controller = GetComponent<CharacterController>();
        BuildUI();
    }

    void Update()
    {
        if (darkImg == null || bloodImg == null) return;

        hurtTimer -= Time.deltaTime;
        float target = hurtTimer > 0f ? 1f : 0f;
        float speed = target > intensity ? fadeInSpeed : fadeOutSpeed;
        intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime * speed);

        float pulse = target > 0f ? 1f + Mathf.Sin(Time.time * 6f) * 0.1f : 1f;
        darkImg.color = new Color(0f, 0f, 0f, intensity * darkness);
        bloodImg.color = new Color(1f, 1f, 1f, Mathf.Clamp01(intensity * bloodAmount * pulse));
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;
        currentHealth -= amount;
        UpdateBar();

        // screen effect: stays on while damage keeps coming, big hits flash instantly
        hurtTimer = 0.25f;
        intensity = Mathf.Max(intensity, Mathf.Clamp01(amount / maxHealth * 4f));

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            lastDeathPosition = transform.position;
            if (OrbPickup.hasFire)
            {
                PlaceDeathFire();
                OrbPickup.LoseFire();
            }
            StartCoroutine(DeathSequence());
        }
    }

    void PlaceDeathFire()
    {
        if (deathFire != null) Destroy(deathFire);
        if (deathFirePrefab == null) return;
        deathFire = Instantiate(deathFirePrefab, lastDeathPosition, Quaternion.identity);
        deathFire.transform.localScale = Vector3.one * deathFireScale;
    }

    void UpdateBar()
    {
        barFill.sizeDelta = new Vector2(barWidth * (currentHealth / maxHealth), barFill.sizeDelta.y);
    }

    IEnumerator DeathSequence()
    {
        yield return Fade(0f, 1f, 1.5f);
        yield return new WaitForSeconds(2f);

        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        if (controller != null) controller.enabled = true;

        currentHealth = maxHealth;
        UpdateBar();
        hurtTimer = 0f;
        intensity = 0f;
        yield return Fade(1f, 0f, 1f);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            overlay.color = new Color(0, 0, 0, a * 0.7f);
            deathText.color = new Color(0.6f, 0.05f, 0.05f, a);
            yield return null;
        }
    }

    Image MakeFullscreen(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return img;
    }

    Sprite MakeVignette()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x / (size - 1f) * 2f - 1f;
                float dy = y / (size - 1f) * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0.3f, 1.3f, d);
                a = 0.15f + 0.85f * a;
                tex.SetPixel(x, y, new Color(0.55f, 0f, 0f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("HUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Damage effect (drawn first so the health bar stays on top)
        darkImg = MakeFullscreen("DamageDark", canvasObj.transform);
        darkImg.color = new Color(0f, 0f, 0f, 0f);
        bloodImg = MakeFullscreen("DamageBlood", canvasObj.transform);
        bloodImg.sprite = MakeVignette();
        bloodImg.color = new Color(1f, 1f, 1f, 0f);

        // Bar background (bottom-left)
        GameObject bg = new GameObject("BarBG");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = bgRect.pivot = new Vector2(0, 0);
        bgRect.anchoredPosition = new Vector2(60, 50);
        bgRect.sizeDelta = new Vector2(barWidth, 8);

        // Blood red fill
        GameObject fill = new GameObject("BarFill");
        fill.transform.SetParent(bg.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.54f, 0.01f, 0.02f, 1f);
        barFill = fill.GetComponent<RectTransform>();
        barFill.anchorMin = barFill.anchorMax = barFill.pivot = new Vector2(0, 0.5f);
        barFill.anchoredPosition = Vector2.zero;
        barFill.sizeDelta = new Vector2(barWidth, 8);

        // Death overlay
        GameObject ov = new GameObject("DeathOverlay");
        ov.transform.SetParent(canvasObj.transform, false);
        overlay = ov.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0);
        overlay.raycastTarget = false;
        RectTransform ovRect = ov.GetComponent<RectTransform>();
        ovRect.anchorMin = Vector2.zero;
        ovRect.anchorMax = Vector2.one;
        ovRect.offsetMin = ovRect.offsetMax = Vector2.zero;

        // YOU DIED text
        GameObject tx = new GameObject("DeathText");
        tx.transform.SetParent(canvasObj.transform, false);
        deathText = tx.AddComponent<Text>();
        deathText.font = deathFont != null
            ? deathFont
            : Font.CreateDynamicFontFromOSFont(
                new string[] { "Garamond", "Palatino Linotype", "Georgia", "Times New Roman" }, 110);
        deathText.text = "YOU DIED";
        deathText.fontSize = 120;
        deathText.alignment = TextAnchor.MiddleCenter;
        deathText.color = new Color(0.6f, 0.05f, 0.05f, 0);
        deathText.raycastTarget = false;
        Shadow shadow = tx.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(4, -4);
        RectTransform txRect = tx.GetComponent<RectTransform>();
        txRect.anchorMin = Vector2.zero;
        txRect.anchorMax = Vector2.one;
        txRect.offsetMin = txRect.offsetMax = Vector2.zero;
    }
}