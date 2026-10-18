using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BossGate : MonoBehaviour
{
    public GameObject boss;            // drag zombie boss here
    public float promptRange = 6f;     // how close to the glass the prompt appears
    public float sealDepth = 8f;       // how far inside you must walk before the glass seals

    private MeshCollider glass;
    private BossAI ai;
    private BossHealthBar bar;
    private Transform player;
    private Text prompt;
    private bool opening;
    private bool fightStarted;

    public bool FightStarted { get { return fightStarted; } }

    void Awake()
    {
        glass = GetComponent<MeshCollider>();
        if (boss != null)
        {
            ai = boss.GetComponent<BossAI>();
            bar = boss.GetComponent<BossHealthBar>();
            if (ai != null) ai.enabled = false;     // boss sleeps
            if (bar != null) bar.enabled = false;   // no health bar yet
        }
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        BuildUI();
    }

    void Update()
    {
        if (player == null || prompt == null) return;

        if (fightStarted || opening || !OrbPickup.hasFire)
        {
            prompt.enabled = false;
            return;
        }

        float r = transform.lossyScale.x * 0.5f;
        Vector3 d = player.position - transform.position;
        d.y = 0f;
        float dist = d.magnitude;

        bool near = dist > r && dist - r < promptRange;
        prompt.enabled = near;

        if (near && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            StartCoroutine(Enter(r));
    }

    IEnumerator Enter(float r)
    {
        opening = true;
        glass.enabled = false;      // glass opens

        float timeout = 15f;
        while (timeout > 0f)
        {
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            if (d.magnitude < r - sealDepth) break;   // you are inside
            timeout -= Time.deltaTime;
            yield return null;
        }

        glass.enabled = true;       // glass seals behind you
        if (timeout <= 0f)          // never walked in: reset the gate
        {
            opening = false;
            yield break;
        }

        fightStarted = true;
        if (ai != null) ai.enabled = true;
        if (bar != null) bar.enabled = true;
    }

    void BuildUI()
    {
        GameObject canvasObj = new GameObject("GateHUD");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject tx = new GameObject("GatePrompt");
        tx.transform.SetParent(canvasObj.transform, false);
        prompt = tx.AddComponent<Text>();
        prompt.font = Font.CreateDynamicFontFromOSFont(new string[] { "Georgia", "Times New Roman", "Arial" }, 36);
        prompt.fontSize = 36;
        prompt.alignment = TextAnchor.MiddleCenter;
        prompt.color = new Color(0.95f, 0.9f, 0.75f, 1f);
        prompt.raycastTarget = false;
        prompt.text = "Press E to enter the boss arena";
        prompt.enabled = false;
        Shadow sh = tx.AddComponent<Shadow>();
        sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
        sh.effectDistance = new Vector2(2f, -2f);
        RectTransform rt = tx.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, -200f);
        rt.sizeDelta = new Vector2(900f, 60f);
    }
}