using UnityEngine;

public class LanternFlicker : MonoBehaviour
{
    public float baseIntensity = 6f;
    public float flickerAmount = 0.5f;
    public float flickerSpeed = 3f;

    private Light lanternLight;
    private float seed;

    void Start()
    {
        lanternLight = GetComponent<Light>();
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        lanternLight.intensity = baseIntensity + (n - 0.5f) * 2f * flickerAmount;
    }
}