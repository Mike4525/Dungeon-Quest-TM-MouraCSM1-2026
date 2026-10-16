using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health & Combat")]
    [SerializeField] protected float health;
    [SerializeField] protected float damage;
    [SerializeField] protected bool contactDamage = true;

    [Header("Recoil Settings")]
    [SerializeField] protected float recoilLength = 0.08f;
    [SerializeField] protected float recoilFactor = 1f;
    [SerializeField] protected bool isRecoiling = false;

    [Header("Movement & Patrol Settings")]
    [SerializeField] protected float speed = 2f;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] protected Transform pitCheck;
    [SerializeField] protected float checkDistance = 0.4f;
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected bool facingRight = true;

    protected float recoilTimer;
    protected Rigidbody2D rb;
    protected PlayerController player;

    public event Action<Enemy> Died;
    [SerializeField] protected float deathDuration = 1f;
    protected bool isDead;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        player = PlayerController.Instance;

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (health <= 0)
        {
            Die();
            return;
        }

        HandleRecoil();
        if (!isRecoiling) PatrolCheck();
    }

    protected virtual void FixedUpdate()
    {
        if (!isRecoiling && health > 0)
        {
            Move();
        }
    }

    protected virtual void Move()
    {
        float moveDirection = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDirection * speed, rb.linearVelocityY);
    }

    protected virtual void PatrolCheck()
    {
        Vector2 moveVector = facingRight ? Vector2.right : Vector2.left;

        if (wallCheck != null)
        {
            RaycastHit2D hitWall = Physics2D.Raycast(wallCheck.position, moveVector, checkDistance, whatIsGround);

            if (hitWall.collider != null && !hitWall.collider.transform.IsChildOf(transform))
            {
                Flip();
                return;
            }
        }

        if (pitCheck != null)
        {
            RaycastHit2D hitGround = Physics2D.Raycast(pitCheck.position, Vector2.down, checkDistance, whatIsGround);

            if (hitGround.collider == null)
            {
                Flip();
            }
        }
    }

    protected virtual void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocityY);

            float stepOutDirection = facingRight ? 0.05f : -0.05f;
            rb.position = new Vector2(rb.position.x + stepOutDirection, rb.position.y);
        }
    }

    protected virtual void HandleRecoil()
    {
        if (isRecoiling)
        {
            if (recoilTimer < recoilLength)
            {
                recoilTimer += Time.deltaTime;
            }
            else
            {
                isRecoiling = false;
                recoilTimer = 0f;
            }
        }
    }

    protected virtual void Die()
    {
        isDead = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // no more collisions, so it can't hurt the player or take hits
        }

        if (TryGetComponent(out Animator anim))
        {
            anim.SetTrigger("Dying");
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDuration);
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    public virtual void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce)
    {
        if (isDead) return;

        health -= _damageDone;

        if (isRecoiling) return;

        isRecoiling = true;
        recoilTimer = 0f;

        rb.linearVelocity = -_hitDirection.normalized * (_hitForce * recoilFactor);
    }

    public void NotifyPlayerContact() 
    {
        if (isDead) return;
        OnPlayerContact();
    }

    protected virtual void OnPlayerContact()
    {
        if (player != null && !player.pState.invincible)
        {
            Attack();
            player.HitStopTime(0.05f, 5, .2f);
        }
    }

    protected virtual void Attack()
    {
        if (player != null)
        {
            player.TakeDamage(damage);
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector2 moveVector = facingRight ? Vector2.right : Vector2.left;

        if (wallCheck != null)
        {
            Gizmos.DrawRay(wallCheck.position, moveVector * checkDistance);
        }

        if (pitCheck != null)
        {
            Gizmos.DrawRay(pitCheck.position, Vector2.down * checkDistance);
        }
    }
}