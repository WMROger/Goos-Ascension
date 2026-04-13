using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [Tooltip("Drag your Player object here")]
    public Transform target;

    [Tooltip("Adjust this if you want the minimap centered slightly higher or lower than the player")]
    public float yOffset = 0f;

    private void LateUpdate()
    {
        if (target != null)
        {
            // Follow the player's X and Y, but keep the camera's original Z position
            transform.position = new Vector3(target.position.x, target.position.y + yOffset, transform.position.z);
        }
    }
}