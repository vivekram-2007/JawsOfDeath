using UnityEngine;

[DefaultExecutionOrder(10)]
public class ShadowBars : MonoBehaviour
{
    public int RayCount = 4;
    public float RayWidth = 0.12f;
    [Range(0f, 0.8f)] public float Variation = 0f;

    [Header("Ray Shape")]
    [Tooltip("ON = parallel-sided (box) shadow rays. OFF = plain bars (cone-shaped shadow).")]
    public bool BoxShapedRays = true;
    [Tooltip("How close to a point the bar tip gets. Smaller = rays run further before ending.")]
    [Range(0.005f, 0.2f)] public float TipFraction = 0.02f;
    [Tooltip("Rotates all rays around the light, in degrees. Use this to move rays off the straight-ahead line.")]
    public float RayAngleOffset = 45f;

    [Header("Cage")]
    [Tooltip("Cap height above the light (also bar height when Box Shaped Rays is OFF).")]
    public float ExtendAbove = 0.5f;
    public bool TopCap = true;
    public float CapOverhang = 1.05f;

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

    private Vector3 discCenter;
    private Transform[] bars;
    private Vector3[] baseRadial;
    private float[] baseWidth;
    private float[] barMidY;
    private float[] barHeight;
    private Transform cap;
    private float baseCapDiameter;
    private float capY;
    private float capHalfThickness;
    private Mesh wedgeMesh;

    // unit wedge: x = radial thickness, y = height, z = tangential width
    // bottom width 1, top width = topWidth (1 = plain box)
    static Mesh BuildWedge(float topWidth)
    {
        float h = 0.5f;
        float ht = 0.5f * topWidth;

        Vector3[] v = new Vector3[8];
        v[0] = new Vector3(-0.5f, -0.5f, -h);
        v[1] = new Vector3(0.5f, -0.5f, -h);
        v[2] = new Vector3(0.5f, -0.5f, h);
        v[3] = new Vector3(-0.5f, -0.5f, h);
        v[4] = new Vector3(-0.5f, 0.5f, -ht);
        v[5] = new Vector3(0.5f, 0.5f, -ht);
        v[6] = new Vector3(0.5f, 0.5f, ht);
        v[7] = new Vector3(-0.5f, 0.5f, ht);

        int[] t =
        {
            0,1,2, 0,2,3,   // bottom
            4,6,5, 4,7,6,   // top
            0,4,5, 0,5,1,   // -z side
            3,2,6, 3,6,7,   // +z side
            0,3,7, 0,7,4,   // -x side
            1,6,2, 1,5,6    // +x side
        };

        Mesh m = new Mesh();
        m.vertices = v;
        m.triangles = t;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

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
        discCenter = new Vector3(toDisc.x, 0f, toDisc.z);
        float radius = transform.lossyScale.x * 0.5f;
        float thickness = transform.lossyScale.y;
        float bottomDepth = depth + thickness;

        // bar top: just below the light when box-shaped (tip ends far away), above the light otherwise
        float topY;
        float topWidth;
        if (BoxShapedRays)
        {
            topWidth = Mathf.Clamp(TipFraction, 0.005f, 1f);
            topY = -bottomDepth * topWidth;
        }
        else
        {
            topWidth = 1f;
            topY = depth * ExtendAbove;
        }

        wedgeMesh = BuildWedge(topWidth);

        root = new GameObject("ShadowBarsRoot").transform;

        bars = new Transform[RayCount];
        baseRadial = new Vector3[RayCount];
        baseWidth = new float[RayCount];
        barMidY = new float[RayCount];
        barHeight = new float[RayCount];

        Random.InitState(7);
        for (int i = 0; i < RayCount; i++)
        {
            float a = (i + Random.Range(-0.25f, 0.25f)) / RayCount * Mathf.PI * 2f + RayAngleOffset * Mathf.Deg2Rad;
            float v = 1f + Random.Range(-Variation, Variation);

            Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            baseRadial[i] = radial * (radius * 0.97f);
            baseWidth[i] = radius * RayWidth * v;
            barHeight[i] = topY + bottomDepth;
            barMidY[i] = (topY - bottomDepth) * 0.5f;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bar.GetComponent<Collider>());
            bar.GetComponent<MeshFilter>().sharedMesh = wedgeMesh;
            bar.transform.SetParent(root, false);
            bar.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            bar.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            bars[i] = bar.transform;
        }

        if (TopCap)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(c.GetComponent<Collider>());
            c.transform.SetParent(root, false);
            c.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            cap = c.transform;
            baseCapDiameter = radius * 2f * CapOverhang;
            capHalfThickness = Mathf.Max(thickness, 0.005f) * 0.5f;
            capY = depth * ExtendAbove + capHalfThickness;
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

            // closer ground = smaller shadow, so grow the cage
            k = Mathf.Clamp(ReferenceDistance / smoothedDistance, MinScale, MaxScale);
        }

        // grow the disc sideways only, never its thickness
        transform.localScale = new Vector3(baseDiscScale.x * k, baseDiscScale.y, baseDiscScale.z * k);

        root.position = lightSource.position;
        root.rotation = transform.rotation;

        // bars and cap scale around the SAME center as the disc
        for (int i = 0; i < bars.Length; i++)
        {
            bars[i].localPosition = new Vector3(
                discCenter.x + baseRadial[i].x * k,
                barMidY[i],
                discCenter.z + baseRadial[i].z * k);
            bars[i].localScale = new Vector3(baseWidth[i] * k, barHeight[i], baseWidth[i] * k);
        }

        if (cap != null)
        {
            cap.localPosition = new Vector3(discCenter.x, capY, discCenter.z);
            cap.localScale = new Vector3(baseCapDiameter * k, capHalfThickness, baseCapDiameter * k);
        }
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
        if (wedgeMesh != null) Destroy(wedgeMesh);
    }
}