using UnityEngine;

public class CodeFragment : MonoBehaviour
{
    [SerializeField] private bool debugLogs = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLogs)
            Debug.Log($"[CodeFragment] Trigger entered by '{other.gameObject.name}' (active={other.gameObject.activeInHierarchy})");

        // Be robust to collider nesting:
        // - human/slime hitboxes can be on child objects
        // - Rigidbody2D might live on the player root
        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            // Prefer attachedRigidbody because it survives collider swaps more reliably.
            Rigidbody2D attachedRb = other.attachedRigidbody;
            if (attachedRb != null)
            {
                player = attachedRb.GetComponent<PlayerMovement>();
                if (player == null)
                    player = attachedRb.GetComponentInParent<PlayerMovement>(true);
            }
        }
        if (player == null)
            player = other.GetComponentInParent<PlayerMovement>(true);

        if (player != null)
        {
            Debug.Log("Code Fragment collected!");
            player.EnableHumanTransformation();

            // Show the keybind tutorial panel (finds it even if hidden in the Hierarchy)
            CodeFragmentPanel panel = FindObjectOfType<CodeFragmentPanel>(true);
            if (panel != null)
                panel.ShowPanel();
            else
                Debug.LogWarning("CodeFragment: No CodeFragmentPanel found in the scene.");

            Destroy(gameObject);
        }
        else
        {
            if (debugLogs)
                Debug.LogWarning("[CodeFragment] Triggered but could not find PlayerMovement on collider/parents.");
        }
    }
}
