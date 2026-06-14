using UnityEngine;

public class OceanWave : MonoBehaviour
{
    public float waveHeight = 0.3f;
    public float waveSpeed = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * waveSpeed) * waveHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}