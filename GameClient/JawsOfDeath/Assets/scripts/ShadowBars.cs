using UnityEngine;

[DefaultExecutionOrder(10)]
public class ShadowBars : MonoBehaviour
{
    public int RayCount = 10;
    [Range(0.1f, 0.99f)] public float RayReach = 0.95f;
    public float RayWidth = 0.12f;
    [Range(0f, 0.8f)] public float Variation = 0.2f;

    [Header("Ground Compensation")]
    public bool Compensate = true;
    [Tooltip("0 = measured automatically at start (spawn on flat ground)")]
    public float ReferenceDistance = 0f;
    public float MinScale = 0.85f;
    public float MaxScale = 3f;
    public float MaxCheckDistance = 6f;
    public float Smoothing = 10f;

    private Transform lightSource;
    private Transform root;
    private Vector3 baseDiscScale;
    private float smoothedDistance = -1f;
    private Vector3[] localDirs;

    void Start()
    {
        if (transform.parent == null) return;
        lightSource = transform.parent.Find("Lantern_01_glass/Point Light");
        if (lightSource == null) return;

        baseDiscScale = transform.localScale;

        // five check directions: straight down plus four tilted outward
        localDirs = new Vector3[5];
        localDirs[0] = Vector3.down;
        for (int i = 0; i < 4; i++)
            localDirs[i + 1] = Quaternion.Euler(0f, i * 90f, 0f) * (Quaternion.Euler(30f, 0f, 0f) * Vector3.down);

        // where the disc sits relative to the light, in the disc's own frame
        Vector3 toDisc = Quaternion.Inverse(transform.rotation) * (transform.position - lightSource.position);
        float depth = Mathf.Abs(toDisc.y);
        Vector3 center = new Vector3(toDisc.x, 0f, toDisc.z);
        float radius = transform.lossyScale.x * 0.5f;
        float bottomDepth = depth + transform.lossyScale.y;

        root = new GameObject("ShadowBarsRoot").transform;

        Random.InitState(7);
        for (int i = 0; i < RayCount; i++)
        {
            float a = (i + Random.Range(-0.25f, 0.25f)) / RayCount * Mathf.PI * 2f;
            float v = 1f + Random.Range(-Variation, Variation);

            float reach = Mathf.Clamp(RayReach * v, 0.1f, 0.99f);
            float topDepth = depth * (1f - reach);
            float height = bottomDepth - topDepth;
            float midY = -(bottomDepth + topDepth) * 0.5f;
            float width = radius * RayWidth * v;

            Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Vector3 pos = center + radial * (radius * 0.97f);
            pos.y = midY;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(root, false);
            bar.transform.localPosition = pos;
            bar.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            bar.transform.localScale = new Vector3(width, height, width);
            bar.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    // average ground distance below the light, ignoring the player's own colliders
    float MeasureGround()
    {
        Quaternion yaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        float sum = 0f;
        for (int i = 0; i < localDirs.Length; i++)
        {
            Vector3 dir = yaw * localDirs[i];
            float best = MaxCheckDistance;
            RaycastHit[] hits = Physics.RaycastAll(lightSource.position, dir, MaxCheckDistance,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int h = 0; h < hits.Length; h++)
            {
                if (hits[h].collider.transform.root == transform.root) continue;
                if (hits[h].distance < best) best = hits[h].distance;
            }
            sum += best;
        }
        return sum / localDirs.Length;
    }

    void LateUpdate()
    {
        if (root == null) return;

        float k = 1f;
        if (Compensate)
        {
            float d = MeasureGround();
            if (smoothedDistance < 0f) smoothedDistance = d;
            smoothedDistance = Mathf.Lerp(smoothedDistance, d, 1f - Mathf.Exp(-Smoothing * Time.deltaTime));

            if (ReferenceDistance <= 0f) ReferenceDistance = d; // auto-calibrate once

            k = Mathf.Clamp(ReferenceDistance / smoothedDistance, MinScale, MaxScale);
        }

        // grow the disc sideways only, never its thickness
        transform.localScale = new Vector3(baseDiscScale.x * k, baseDiscScale.y, baseDiscScale.z * k);

        root.position = lightSource.position;
        root.rotation = transform.rotation;
        root.localScale = new Vector3(k, 1f, k);
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
    }
}