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

    private float nextFire;
    private int ammo;
    private bool reloading;
    private float reloadEnd;

    private PlayerHealth health;
    private Text ammoText;
    private Image crosshair;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        health = GetComponent<PlayerHealth>();
        ammo = magazineSize;
        BuildUI();
        UpdateUI();
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null) return;

        if (health != null && health.currentHealth <= 0)
        {
            reloading = false;
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
                Shoot();
            }
            else
            {
                StartReload();
            }
        }

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

    void UpdateUI()
    {
        ammoText.text = reloading ? "RELOADING" : ammo + " / " + magazineSize;
        crosshair.enabled = ThirdPersonController.IsAiming;
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("GunHUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Crosshair dot (screen center)
        GameObject dot = new GameObject("Crosshair");
        dot.transform.SetParent(canvasObj.transform, false);
        crosshair = dot.AddComponent<Image>();
        crosshair.color = new Color(1f, 1f, 1f, 0.95f);
        crosshair.raycastTarget = false;
        dot.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        RectTransform dotRect = dot.GetComponent<RectTransform>();
        dotRect.anchorMin = dotRect.anchorMax = dotRect.pivot = new Vector2(0.5f, 0.5f);
        dotRect.anchoredPosition = Vector2.zero;
        dotRect.sizeDelta = new Vector2(6f, 6f);
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