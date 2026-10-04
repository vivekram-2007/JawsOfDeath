using UnityEngine;

[DefaultExecutionOrder(10)]
public class ShadowBars : MonoBehaviour
{
    public int BarCount = 10;
    public float BarRadius = 0.14f;   // distance from the light (keep above 0.12)
    public float BarDrop = 0.06f;     // how far below the light the bars sit
    public float BarHeight = 0.08f;   // taller = longer rays
    public float BarWidth = 0.025f;   // wider = thicker rays
    [Range(0f, 1f)] public float Variation = 0.35f; // random length/thickness per ray

    private Transform lightSource;
    private Transform root;
    private float s = 1f;

    void Start()
    {
        if (transform.parent == null) return;
        lightSource = transform.parent.Find("Lantern_01_glass/Point Light");
        if (lightSource == null) return;

        s = transform.parent.lossyScale.x;
        root = new GameObject("ShadowBarsRoot").transform;

        Random.InitState(7);
        for (int i = 0; i < BarCount; i++)
        {
            float a = (i + Random.Range(-0.25f, 0.25f)) / BarCount * Mathf.PI * 2f;
            float v = 1f + Random.Range(-Variation, Variation);

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bar.GetComponent<Collider>());
            bar.transform.SetParent(root, false);

            float drop = BarDrop * (1f + Random.Range(-Variation, Variation) * 0.5f);
            bar.transform.localPosition = new Vector3(Mathf.Cos(a) * BarRadius, -drop, Mathf.Sin(a) * BarRadius) * s;
            bar.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
            bar.transform.localScale = new Vector3(BarWidth * v, BarHeight, BarWidth * v) * s;

            var r = bar.GetComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    void LateUpdate()
    {
        if (root == null) return;
        root.position = lightSource.position;
        root.rotation = transform.rotation;
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root.gameObject);
    }
}