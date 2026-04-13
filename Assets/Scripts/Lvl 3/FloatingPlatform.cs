using UnityEngine;

public class FloatingPlatform : MonoBehaviour
{
    public float floatSpeed = 2f;
    public float floatHeight = 1f;

    private Vector3 startPos;
    private float randomOffset;

    void Start()
    {
        startPos = transform.position;
        // This ensures they don't all move up and down in sync
        randomOffset = Random.Range(0f, 10f); 
    }

    void Update()
    {
        // Simple sine wave math to make it bob up and down smoothly!
        float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatHeight);
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }
}