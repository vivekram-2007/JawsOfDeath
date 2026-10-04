using UnityEngine;

public class KeepLevel : MonoBehaviour
{
    public Transform lightSource; // found automatically, leave empty

    private Vector3 worldOffset;
    private bool ready = false;

    void LateUpdate()
    {
        if (lightSource == null && transform.parent != null)
            lightSource = transform.parent.Find("Lantern_01_glass/Point Light");

        if (lightSource == null) return;

        // keep the disc flat
        transform.rotation = Quaternion.identity;

        // remember the standing-still offset once, after the first animation frame
        if (!ready)
        {
            worldOffset = transform.position - lightSource.position;
            ready = true;
        }

        // keep that same offset from the light while walking
        transform.position = lightSource.position + worldOffset;
    }
}