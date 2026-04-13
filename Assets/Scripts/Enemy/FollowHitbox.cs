using UnityEngine;

public class FollowHitbox : MonoBehaviour
{
    public Transform hitbox;

    void LateUpdate()
    {
        if (hitbox != null)
            transform.position = hitbox.position;
    }
}
