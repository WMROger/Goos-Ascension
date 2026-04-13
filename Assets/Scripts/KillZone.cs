using UnityEngine;

/// <summary>
/// Place this on a trigger collider at the bottom of the level.
/// Any player that falls into it instantly dies.
/// </summary>
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.Die();
    }
}
