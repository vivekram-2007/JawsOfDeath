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

    private GameObject deathFire;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private CharacterController controller;
    private RectTransform barFill;
    private float barWidth = 255f;
    private Image overlay;
    private Text deathText;

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        startRotation = transform.rotation;
        controller = GetComponent<CharacterController>();
        BuildUI();
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;
        currentHealth -= amount;
        UpdateBar();
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            lastDeathPosition = transform.position;
            PlaceDeathFire();
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

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("HUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

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