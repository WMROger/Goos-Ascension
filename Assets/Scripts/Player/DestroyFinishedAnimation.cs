using UnityEngine;

public class DestroyFinishedAnimation : MonoBehaviour
{
    [SerializeField] private float delay = 0.8f; // Match this to your explosion animation length

    void Start()
    {
        Destroy(gameObject, delay);
    }
}