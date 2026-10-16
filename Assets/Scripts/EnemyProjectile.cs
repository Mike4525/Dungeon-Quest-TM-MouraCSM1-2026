using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Arc")]
    [SerializeField] private float gravityScale = 2f;  
    [SerializeField] private float horizontalSpeed = 6f;   
    [SerializeField] private float flightTimeMin = 0.8f;
    [SerializeField] private float flightTimeMax = 1.5f;
    [SerializeField] private bool faceVelocity = true;

    [Header("General")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private LayerMask whatIsGround;

    private Rigidbody2D rb;
    private float damage;

    public void Launch(Vector2 targetPosition, float damageAmount)
    {
        damage = damageAmount;

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Vector2 delta = targetPosition - (Vector2)transform.position;

        float t = Mathf.Clamp(Mathf.Abs(delta.x) / horizontalSpeed, flightTimeMin, flightTimeMax); // Farther targets get a longer flight

        float g = Physics2D.gravity.y * gravityScale; // negative
        rb.linearVelocity = new Vector2(delta.x / t, delta.y / t - 0.5f * g * t);

        Destroy(gameObject, lifetime);
    }

    public void Drop(float damageAmount) // For boss enemy
    {
        damage = damageAmount;
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = Vector2.zero;
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Rotate the sprite along its path
        if (faceVelocity && rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
            transform.right = rb.linearVelocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Spell") || other.transform.root.CompareTag("Spell")) return;

        var player = other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            if (!player.pState.invincible)
            {
                player.TakeDamage(damage);
                player.HitStopTime(0.05f, 5, .2f);
            }
            Destroy(gameObject);
            return;
        }

        if (IsInMask(other.gameObject, whatIsGround))
            Destroy(gameObject);
    }

    private bool IsInMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}