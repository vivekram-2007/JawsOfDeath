using UnityEngine;

public class WindFollow : MonoBehaviour
{
    public Transform target;
    public float heightOffset = 1f;
    public float windYaw = 45f;

    void Start()
    {
        if (target == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 p = target.position;
        transform.position = new Vector3(p.x, p.y + heightOffset, p.z);
        transform.rotation = Quaternion.Euler(0f, windYaw, 0f);
    }
}