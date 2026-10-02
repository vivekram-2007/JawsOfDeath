using UnityEngine;

public class OrbPickup : MonoBehaviour
{
    public static bool hasFire = false;
    private static GameObject bodyFire;

    public GameObject fireEffectPrefab; // drag Fire001 prefab here
    public Vector3 fireOffset = new Vector3(0f, 1f, 0f);
    public float fireScale = 0.6f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (hasFire) return;

        hasFire = true;

        if (fireEffectPrefab != null)
        {
            bodyFire = Instantiate(fireEffectPrefab, other.transform);
            bodyFire.transform.localPosition = fireOffset;
            bodyFire.transform.localScale = Vector3.one * fireScale;
        }
    }

    public static void LoseFire()
    {
        if (bodyFire != null) Destroy(bodyFire);
        bodyFire = null;
        hasFire = false;
    }
}