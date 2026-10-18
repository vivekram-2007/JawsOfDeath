using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class BossGate : MonoBehaviour
{
    public GameObject boss;            // drag zombie boss here
    public float promptRange = 6f;     // how close to the glass the prompt appears
    public float enterDepth = 10f;     // how many metres inside the glass you are placed
    public float keepMargin = 1.5f;    // how far from the glass edge you get stopped
    private BossAI ai;
    private BossHealthBar bar;
    private Transform player;
    private Text prompt;
    private bool fightStarted;

    public bool FightStarted { get { return fightStarted; } }

    void Awake()
    {
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

        if (fightStarted || !OrbPickup.hasFire)
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
            EnterArena(r, d);
    }
    void LateUpdate()
    {
        if (!fightStarted || player == null) return;

        float limit = transform.lossyScale.x * 0.5f - keepMargin;
        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.magnitude <= limit) return;

        Vector3 p = transform.position + d.normalized * limit;
        p.y = player.position.y;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = p;
        if (cc != null) cc.enabled = true;
    }
    void EnterArena(float r, Vector3 flatDir)
    {
        if (flatDir.sqrMagnitude < 0.01f) flatDir = Vector3.forward;
        flatDir = flatDir.normalized;

        // place the player inside the glass, on the ground
        Vector3 target = transform.position + flatDir * (r - enterDepth);
        target = SnapToGround(target, r);

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.position = target;
        if (cc != null) cc.enabled = true;

        // seal and start the fight instantly
        fightStarted = true;
        prompt.enabled = false;
        if (ai != null) ai.enabled = true;
        if (bar != null) bar.enabled = true;
    }

    Vector3 SnapToGround(Vector3 pos, float r)
    {
        Vector3 start = new Vector3(pos.x, transform.position.y + r + 20f, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, r * 3f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<BossGate>() != null) continue;   // skip the glass
            if (h.transform == player || h.transform.IsChildOf(player)) continue;
            if (boss != null && h.transform.IsChildOf(boss.transform)) continue;
            return new Vector3(pos.x, h.point.y + 0.1f, pos.z);
        }
        return pos;
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