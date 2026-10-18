using System.Collections;
using UnityEngine;

public class FlyingEnemy : Enemy
{
    protected enum State { Patrol, Hunt }

    [Header("Hovering")]
    [SerializeField] private float hoverHeight = 3f; 
    [SerializeField] private float minHeight = 1.5f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float groundSearchDistance = 12f;

    [Header("Patrol")]
    [SerializeField] private float patrolDistance = 4f;

    [Header("Hunting")]
    [SerializeField] private float huntRange = 8f;
    [SerializeField] private float loseInterestRange = 12f; 
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float preferredDistance = 5f; 
    [SerializeField] private float distanceTolerance = 0.75f; 
    [SerializeField] private float acceleration = 20f;
    [SerializeField] private float turnDeadZone = 0.5f;  
    [Header("Shooting")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;   
    [SerializeField] private float shootRange = 7f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float launchDelay = 0.4f;

    protected State state = State.Patrol;
    private float startX;
    private float nextShootTime;
    private Collider2D col;
    private Animator anim;

    protected override void Start()
    {
        base.Start();
        col = GetComponent<Collider2D>(); 
        startX = transform.position.x;

        anim = GetComponent<Animator>();

        if (rb != null) rb.gravityScale = 0f;
    }

    protected override void Update()
    {
        base.Update();
        if (isDead || isRecoiling) return;

        float dist = DistanceToPlayer();

        if (state == State.Patrol)
        {
            if (dist <= huntRange)
            {
                state = State.Hunt;
                nextShootTime = Time.time + shootCooldown;
            }
            else PatrolTurnCheck();
        }
        else // Only 2 states, faster code
        {
            if (dist > loseInterestRange) state = State.Patrol;
            else
            {
                FacePlayer();
                TryShoot(dist);
            }
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead || isRecoiling) return;

        Vector2 desired = state == State.Hunt ? HuntVelocity() : PatrolVelocity();

        // Blend toward the target velocity, which also smooths the end of knockback
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);
        if (anim != null)
        {
            anim.SetBool("Walking", Mathf.Abs(rb.linearVelocity.x) > 0.1f); // Almost always moving but compensating so small movements
        }
    }

    protected override void PatrolCheck() { } // To block the base.PatrolCheck()

    private Vector2 PatrolVelocity()
    {
        float dir = facingRight ? 1f : -1f;
        return new Vector2(dir * speed, HoverVelocityY());
    }

    private Vector2 HuntVelocity()
    {
        if (player == null) return Vector2.zero;

        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;
        Vector2 dir = toPlayer / Mathf.Max(dist, 0.001f);

        // Correct position of the enemy to stand a bit away from the player
        float error = dist - preferredDistance;
        float excess = Mathf.Abs(error) - distanceTolerance;

        Vector2 v = Vector2.zero;
        if (excess > 0f)
            v = dir * Mathf.Sign(error) * Mathf.Clamp(excess * 2f, 0f, chaseSpeed); // eases in and out

        // Never dive into the floor
        if (GroundDistance() < minHeight) v.y = Mathf.Max(v.y, hoverSpeed);

        return v;
    }

    private float HoverVelocityY()
    {
        float g = GroundDistance();
        if (float.IsInfinity(g)) return 0f;

        float target = hoverHeight;
        return Mathf.Clamp((target - g) * 3f, -hoverSpeed, hoverSpeed);
    }

    private float GroundDistance()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundSearchDistance, whatIsGround);
        return hit.collider != null ? hit.distance : float.PositiveInfinity;
    }

    private void PatrolTurnCheck()
    {
        float offset = transform.position.x - startX;
        bool movingAway = (offset > 0f) == facingRight;
        bool tooFar = Mathf.Abs(offset) > patrolDistance && movingAway;

        if (tooFar || WallAhead()) Face(!facingRight);
    }

    private bool WallAhead()
    {
        float dirX = facingRight ? 1f : -1f;
        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x + dirX * (b.extents.x + 0.02f), b.center.y); 
        return Physics2D.Raycast(origin, new Vector2(dirX, 0f), checkDistance, whatIsGround).collider != null;
    }

    private void FacePlayer()
    {
        if (player == null) return;

        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) < turnDeadZone) return;
        Face(dx > 0f);
    }

    private void Face(bool right) // Flip() isn't adapted for the flyer
    {
        if (facingRight == right) return;

        facingRight = right;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
        transform.localScale = s;
    }

    private void TryShoot(float dist)
    {
        if (projectilePrefab == null || player == null) return;
        if (Time.time < nextShootTime || dist > shootRange) return;

        Vector2 from = (Vector2)firePoint.position;
        Vector2 to = player.transform.position;

        if (Physics2D.Linecast(from, to, whatIsGround).collider != null) return; // Make sure it cannot shoot through walls

        nextShootTime = Time.time + shootCooldown;
        if (anim != null) anim.SetTrigger("Attack");

        StartCoroutine(LaunchAfterDelay());
    }

    private IEnumerator LaunchAfterDelay()
    {
        yield return new WaitForSeconds(launchDelay);

        if (isDead || player == null) yield break;

        Vector2 from = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        EnemyProjectile p = Instantiate(projectilePrefab, from, Quaternion.identity);
        p.Launch(player.transform.position, damage);
    }

    private float DistanceToPlayer()
    {
        return player != null
            ? Vector2.Distance(transform.position, player.transform.position)
            : float.PositiveInfinity;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, huntRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}