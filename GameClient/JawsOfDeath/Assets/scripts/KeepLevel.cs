using UnityEngine;

public class KeepLevel : MonoBehaviour
{
    [Tooltip("Max tilt in degrees. Lower = less chopping, higher = follows the lantern more")]
    public float MaxTilt = 25f;

    private Transform lantern;
    private Transform lightSource;
    private Vector3 localOffset;
    private bool ready = false;

    void Start()
    {
        lantern = transform.parent;
        if (lantern == null) return;
        lightSource = lantern.Find("Lantern_01_glass/Point Light");
        if (lightSource == null) return;

        // remember where the disc sits relative to the light, in the lantern's own space
        localOffset = lantern.InverseTransformVector(transform.position - lightSource.position);
        ready = true;
    }

    void LateUpdate()
    {
        if (!ready) return;

        // follow the lantern's turn fully, cap only the tilt
        Quaternion yaw = Quaternion.Euler(0f, lantern.eulerAngles.y, 0f);
        Quaternion tilt = Quaternion.Inverse(yaw) * lantern.rotation;
        tilt = Quaternion.RotateTowards(Quaternion.identity, tilt, MaxTilt);
        Quaternion rot = yaw * tilt;

        // keep the same distance from the light at all times
        transform.rotation = rot;
        transform.position = lightSource.position + rot * localOffset;
    }
}