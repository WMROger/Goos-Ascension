using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Attach to the CinemachineCamera GameObject.
/// Smoothly follows at normal speed when the player is rising/idle,
/// and snaps faster when the player is falling so the camera never lags behind.
/// </summary>
public class CameraFollowController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player's Rigidbody2D used to read vertical velocity.")]
    [SerializeField] private Rigidbody2D playerRb;

    [Header("Y Damping")]
    [Tooltip("Damping when the player is idle or moving up (higher = slower/smoother follow).")]
    [SerializeField] private float normalDamping = 10f;
    [Tooltip("Damping when the player is falling (lower = faster follow).")]
    [SerializeField] private float fallingDamping = 1f;
    [Tooltip("How fast the damping value itself transitions between normal and falling.")]
    [SerializeField] private float dampingTransitionSpeed = 8f;
    [Tooltip("Vertical velocity below this value is considered falling.")]
    [SerializeField] private float fallThreshold = -0.5f;

    private CinemachinePositionComposer positionComposer;
    private float currentDamping;

    private void Awake()
    {
        positionComposer = GetComponent<CinemachinePositionComposer>();

        if (positionComposer == null)
            Debug.LogError("[CameraFollowController] CinemachinePositionComposer not found on this GameObject.");

        currentDamping = normalDamping;
    }

    private void Update()
    {
        if (playerRb == null || positionComposer == null) return;

        float targetDamping = playerRb.linearVelocity.y < fallThreshold
            ? fallingDamping
            : normalDamping;

        // Smoothly blend between damping values
        currentDamping = Mathf.Lerp(currentDamping, targetDamping, dampingTransitionSpeed * Time.deltaTime);

        var damping = positionComposer.Damping;
        damping.y = currentDamping;
        positionComposer.Damping = damping;
    }
}
