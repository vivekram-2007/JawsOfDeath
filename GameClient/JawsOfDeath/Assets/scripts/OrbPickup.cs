using UnityEngine;

public class OrbPickup : MonoBehaviour
{
    public static bool hasFire = false;

    public GameObject fireEffectPrefab; // drag Fire001 prefab here
    public Vector3 fireOffset = new Vector3(0f, 1f, 0f);
    public float fireScale = 0.6f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        hasFire = true;

        if (fireEffectPrefab != null)
        {
            GameObject fire = Instantiate(fireEffectPrefab, other.transform);
            fire.transform.localPosition = fireOffset;
            fire.transform.localScale = Vector3.one * fireScale;
        }

        Destroy(gameObject);
    }
}