using UnityEngine;

public class CrystalGlow : MonoBehaviour
{
    public float pulseSpeed = 1.5f;
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    public Light glowLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;

    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
        float s = Mathf.Lerp(minScale, maxScale, pulse);

        // Only pulse Y axis (up and down)
        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y * s,
            originalScale.z
        );

        if (glowLight != null)
            glowLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
    }
}