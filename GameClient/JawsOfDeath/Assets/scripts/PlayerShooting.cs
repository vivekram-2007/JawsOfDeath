using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using StarterAssets;

public class PlayerShooting : MonoBehaviour
{
    public Camera playerCamera;          // leave empty to use Main Camera
    public float range = 60f;
    public int damage = 20;
    public float fireRate = 0.35f;       // seconds between shots
    public float fireMultiplier = 2f;    // damage multiplier while holding fire

    public int magazineSize = 12;
    public float reloadTime = 1.5f;      // press F to reload

    [Header("Crosshair")]
    public float crosshairSize = 24f;    // circle size in pixels
    public float bloomPerShot = 5f;      // pixels added per shot
    public float maxBloom = 18f;         // max extra pixels
    public float bloomRecover = 30f;     // pixels per second it shrinks back
    public float sizeSmoothTime = 0.06f; // lower = snappier, higher = smoother
    public float colorSmoothSpeed = 14f; // how fast the red fade happens
    public Color normalColor = new Color(1f, 1f, 1f, 0.9f);
    public Color enemyColor = new Color(1f, 0.1f, 0.1f, 1f);

    private float nextFire;
    private int ammo;
    private bool reloading;
    private float reloadEnd;
    private float bloom;
    private float displaySize;
    private float sizeVelocity;

    private PlayerHealth health;
    private Text ammoText;
    private Image crosshair;
    private RectTransform crosshairRect;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        health = GetComponent<PlayerHealth>();
        ammo = magazineSize;
        displaySize = crosshairSize;
        BuildUI();
        UpdateUI();
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        if (health != null && health.currentHealth <= 0)
        {
            reloading = false;
            bloom = 0f;
            crosshair.enabled = false;
            return;
        }

        if (reloading && Time.time >= reloadEnd)
        {
            ammo = magazineSize;
            reloading = false;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame) StartReload();

        if (Mouse.current.leftButton.wasPressedThisFrame && Time.time >= nextFire && !reloading)
        {
            if (ammo > 0)
            {
                nextFire = Time.time + fireRate;
                ammo--;
                bloom = Mathf.Min(bloom + bloomPerShot, maxBloom);
                Shoot();
            }
            else
            {
                StartReload();
            }
        }

        bloom = Mathf.MoveTowards(bloom, 0f, bloomRecover * Time.deltaTime);

        UpdateUI();
    }

    void StartReload()
    {
        if (reloading || ammo >= magazineSize) return;
        reloading = true;
        reloadEnd = Time.time + reloadTime;
    }

    void Shoot()
    {
        Transform cam = playerCamera.transform;
        int finalDamage = Mathf.RoundToInt(damage * (OrbPickup.hasFire ? fireMultiplier : 1f));

        RaycastHit[] hits = Physics.RaycastAll(cam.position, cam.forward, range, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            ZombieHealth z = hit.collider.GetComponentInParent<ZombieHealth>();
            if (z != null) z.TakeDamage(finalDamage);
            break; // first solid thing stops the bullet
        }
    }

    ZombieHealth ZombieUnderCrosshair()
    {
        Transform cam = playerCamera.transform;
        RaycastHit[] hits = Physics.RaycastAll(cam.position, cam.forward, range, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            return hit.collider.GetComponentInParent<ZombieHealth>();
        }
        return null;
    }

    void UpdateUI()
    {
        ammoText.text = reloading ? "RELOADING" : ammo + " / " + magazineSize;

        bool aiming = ThirdPersonController.IsAiming;
        crosshair.enabled = aiming;
        if (!aiming)
        {
            displaySize = crosshairSize;
            sizeVelocity = 0f;
            return;
        }

        // smooth size
        displaySize = Mathf.SmoothDamp(displaySize, crosshairSize + bloom, ref sizeVelocity, sizeSmoothTime);
        crosshairRect.sizeDelta = new Vector2(displaySize, displaySize);

        // smooth color
        Color target = ZombieUnderCrosshair() != null ? enemyColor : normalColor;
        crosshair.color = Color.Lerp(crosshair.color, target, 1f - Mathf.Exp(-colorSmoothSpeed * Time.deltaTime));
    }

    Sprite MakeRingSprite()
    {
        int s = 256;
        Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, true);
        float c = (s - 1) * 0.5f;
        float outer = s * 0.5f - 3f;
        float inner = outer - 12f;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                // 2px soft edge on both sides for smooth anti-aliasing
                float a = Mathf.SmoothStep(0f, 1f, (outer - d) / 2f) * Mathf.SmoothStep(0f, 1f, (d - inner) / 2f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        tex.Apply(true);
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("GunHUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Crosshair circle (screen center)
        GameObject dot = new GameObject("Crosshair");
        dot.transform.SetParent(canvasObj.transform, false);
        crosshair = dot.AddComponent<Image>();
        crosshair.sprite = MakeRingSprite();
        crosshair.color = normalColor;
        crosshair.raycastTarget = false;
        crosshairRect = dot.GetComponent<RectTransform>();
        crosshairRect.anchorMin = crosshairRect.anchorMax = crosshairRect.pivot = new Vector2(0.5f, 0.5f);
        crosshairRect.anchoredPosition = Vector2.zero;
        crosshairRect.sizeDelta = new Vector2(crosshairSize, crosshairSize);
        crosshair.enabled = false;

        // Ammo counter (top right)
        GameObject tx = new GameObject("AmmoText");
        tx.transform.SetParent(canvasObj.transform, false);
        ammoText = tx.AddComponent<Text>();
        ammoText.font = Font.CreateDynamicFontFromOSFont(new string[] { "Consolas", "Arial" }, 40);
        ammoText.fontSize = 40;
        ammoText.alignment = TextAnchor.UpperRight;
        ammoText.color = Color.white;
        ammoText.raycastTarget = false;
        Shadow shadow = tx.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
        RectTransform txRect = tx.GetComponent<RectTransform>();
        txRect.anchorMin = txRect.anchorMax = txRect.pivot = new Vector2(1f, 1f);
        txRect.anchoredPosition = new Vector2(-60f, -50f);
        txRect.sizeDelta = new Vector2(400f, 60f);
    }
}