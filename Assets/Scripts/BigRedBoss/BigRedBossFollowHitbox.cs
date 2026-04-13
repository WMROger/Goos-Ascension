using UnityEngine;

public class BigRedBossFollowHitbox : MonoBehaviour
{
    public Transform hitbox;

    void LateUpdate()
    {
        if (hitbox != null)
            transform.position = hitbox.position;
    }
}
