using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 2f;

    private float direction = 1f;
    private float damage = 10f;
    private bool hasProcessedHit;
    private int groundLayer = -1;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.NameToLayer("Ground");
    }

    public void SetDirection(float dir)
    {
        direction = Mathf.Sign(dir);
        
        // Flip sprite based on direction
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * direction;
        transform.localScale = scale;

        ApplyVelocity();
    }

    public void SetDamage(float dmg)
    {
        damage = Mathf.Max(0f, dmg);
    }

    private void Start()
    {
        ApplyVelocity();
        Destroy(gameObject, lifeTime);
    }

    private void ApplyVelocity()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector2.right * (speed * direction);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        HandleHit(other.collider);
    }

    private void HandleHit(Collider2D other)
    {
        if (hasProcessedHit || other == null)
            return;

        if (IsEnvironmentBlocker(other.gameObject))
        {
            hasProcessedHit = true;
            Destroy(gameObject);
            return;
        }

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null)
            return;

        hasProcessedHit = true;
        target.TakeDamage(damage);
        Destroy(gameObject);
    }

    private bool IsEnvironmentBlocker(GameObject obj)
    {
        if (obj == null)
            return false;

        if (obj.CompareTag("Ground"))
            return true;

        return groundLayer != -1 && obj.layer == groundLayer;
    }
}
