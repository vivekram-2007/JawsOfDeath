using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    public BossHealth boss;                 // auto-filled if left empty (same object)
    public float barWidth = 800f;
    public float barHeight = 18f;
    public float bottomOffset = 90f;        // pixels above the bottom of the screen
    public float trailDelay = 0.6f;         // seconds before the yellow trail drains
    public float trailSpeed = 0.5f;         // fraction of the bar per second

    public Color fillColor = new Color(0.65f, 0.08f, 0.08f, 1f);
    public Color trailColor = new Color(0.9f, 0.65f, 0.15f, 1f);
    public Color backColor = new Color(0f, 0f, 0f, 0.75f);
    public Color borderColor = new Color(0.45f, 0.38f, 0.2f, 1f);

    private GameObject canvasObj;
    private RectTransform fillRect;
    private RectTransform trailRect;
    private float trailFraction = 1f;
    private float lastFraction = 1f;
    private float lastHitTime;

    void Start()
    {
        if (boss == null) boss = GetComponent<BossHealth>();
        BuildUI();
    }

    void Update()
    {
        if (boss == null || fillRect == null) return;

        float f = Mathf.Clamp01(boss.CurrentHealth / boss.maxHealth);
        fillRect.anchorMax = new Vector2(f, 1f);

        if (f < lastFraction) lastHitTime = Time.time;     // took damage
        if (f > trailFraction) trailFraction = f;          // healing pushes the trail up
        if (Time.time - lastHitTime > trailDelay)
            trailFraction = Mathf.MoveTowards(trailFraction, f, trailSpeed * Time.deltaTime);

        trailRect.anchorMax = new Vector2(trailFraction, 1f);
        lastFraction = f;
    }

    void OnDestroy()
    {
        if (canvasObj != null) Destroy(canvasObj);
    }

    Image MakeImage(string objName, Transform parent, Color c)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        return img;
    }

    void Stretch(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    void BuildUI()
    {
        canvasObj = new GameObject("BossHealthHUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // border (outer frame)
        Image border = MakeImage("Border", canvasObj.transform, borderColor);
        RectTransform bRect = border.rectTransform;
        bRect.anchorMin = bRect.anchorMax = bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = new Vector2(0f, bottomOffset);
        bRect.sizeDelta = new Vector2(barWidth + 6f, barHeight + 6f);

        // dark background
        Image back = MakeImage("Back", border.transform, backColor);
        Stretch(back.rectTransform, 2f);

        // yellow trail
        Image trail = MakeImage("Trail", back.transform, trailColor);
        trailRect = trail.rectTransform;
        Stretch(trailRect, 0f);

        // red fill
        Image fill = MakeImage("Fill", back.transform, fillColor);
        fillRect = fill.rectTransform;
        Stretch(fillRect, 0f);

        // boss name
        GameObject tx = new GameObject("BossName");
        tx.transform.SetParent(canvasObj.transform, false);
        Text nameText = tx.AddComponent<Text>();
        nameText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Georgia", "Times New Roman", "Arial" }, 30);
        nameText.fontSize = 30;
        nameText.alignment = TextAnchor.LowerLeft;
        nameText.color = new Color(0.9f, 0.85f, 0.7f, 1f);
        nameText.raycastTarget = false;
        nameText.text = boss != null ? boss.bossName : "BOSS";
        Shadow sh = tx.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        sh.effectDistance = new Vector2(2f, -2f);
        RectTransform tRect = tx.GetComponent<RectTransform>();
        tRect.anchorMin = tRect.anchorMax = tRect.pivot = new Vector2(0.5f, 0f);
        tRect.anchoredPosition = new Vector2(0f, bottomOffset + barHeight + 8f);
        tRect.sizeDelta = new Vector2(barWidth, 44f);
    }
}