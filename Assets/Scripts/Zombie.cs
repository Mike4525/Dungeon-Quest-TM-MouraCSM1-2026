using UnityEngine;

public class Zombie : Enemy
{
    protected enum State { Patrol, Idle, Chase, Windup, Recovery }

    [Header("Patrol")]
    [SerializeField] private float walkTimeMin = 2f;
    [SerializeField] private float walkTimeMax = 4f;
    [SerializeField] private float idleTimeMin = 1f;
    [SerializeField] private float idleTimeMax = 2.5f;

    [Header("Hunting")]
    [SerializeField] private float huntRange = 0f; // 0 = no hunting, just a patrolling enemy
    [SerializeField] private float loseInterestRange = 9f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float turnDeadZone = 0.6f;
    [SerializeField] private float flipCooldown = 0.3f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackHeight = 1f;
    [SerializeField] private float attackWindup = 0.6f;
    [SerializeField] private float attackRecovery = 0.7f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private bool attackFromBehind = true;
    [SerializeField] private bool canBeInterrupted = true;

    protected State state = State.Patrol;
    private float stateTimer;
    private float nextAttackTime;
    private float nextFlipTime;
    private Animator anim;

    protected override void Start()
    {
        base.Start();
        anim = GetComponent<Animator>();
        EnterPatrol();
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        if (isRecoiling)
        {
            if (state == State.Windup && canBeInterrupted) CancelAttack(); // Interrupts attack
            return;
        }

        stateTimer -= Time.deltaTime;
        bool playerNear = PlayerWithin(huntRange);

        switch (state)
        {
            case State.Patrol:
                if (playerNear) state = State.Chase;
                else if (stateTimer <= 0f) EnterIdle();
                break;

            case State.Idle:
                if (playerNear) state = State.Chase;
                else if (stateTimer <= 0f) EnterPatrol();
                break;

            case State.Chase:
                FacePlayer();
                if (!PlayerWithin(loseInterestRange)) EnterPatrol();
                break;

            case State.Windup:
                if (stateTimer <= 0f)
                {
                    Strike();
                    EnterRecovery();
                }
                break;

            case State.Recovery:
                if (stateTimer <= 0f) EndAttack();
                break;
        }
    }

    protected override void FixedUpdate()
    {
        if (isDead || isRecoiling) return;

        float dir = facingRight ? 1f : -1f;
        float vx = 0f; // Idle, Windup and Recovery while standing still

        if (state == State.Patrol) vx = dir * speed;
        else if (state == State.Chase && !PathBlocked() && HorizontalGap() > turnDeadZone) vx = dir * chaseSpeed;

        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

        if (anim != null)
        {
            anim.SetBool("Walking", vx != 0f);
            anim.SetBool("Attack", state == State.Windup || state == State.Recovery);
        }
    }

    protected override void PatrolCheck()
    {
        // Only patrol turns around on its own, so chasing can't cause endless flipping
        if (state == State.Patrol && PathBlocked()) Flip();
    }

    protected override void OnPlayerContact()
    {
        if (isDead || isRecoiling) return;
        if (state == State.Windup || state == State.Recovery) return;
        if (Time.time < nextAttackTime) return;
        if (player == null) return;

        if (!attackFromBehind)
        {
            float dx = player.transform.position.x - transform.position.x;
            bool playerInFront = facingRight ? dx > 0f : dx < 0f;
            if (!playerInFront && state != State.Chase) return;
        }

        FacePlayer();
        EnterWindup();
    }

    private void EnterPatrol()
    {
        state = State.Patrol;
        stateTimer = Random.Range(walkTimeMin, walkTimeMax);
    }

    private void EnterIdle()
    {
        state = State.Idle;
        stateTimer = Random.Range(idleTimeMin, idleTimeMax);
    }

    private void EnterWindup()
    {
        state = State.Windup;
        stateTimer = attackWindup;
        if (anim != null) anim.SetBool("Attack", true);
    }

    private void EnterRecovery()
    {
        state = State.Recovery;
        stateTimer = attackRecovery;
    }

    private void EndAttack()
    {
        if (anim != null) anim.SetBool("Attack", false);
        nextAttackTime = Time.time + attackCooldown;
        if (huntRange > 0f && PlayerWithin(loseInterestRange)) state = State.Chase;
        else EnterPatrol();
    }

    private void CancelAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        EnterPatrol();
        if (anim != null) anim.SetBool("Attack", false);
    }

    private void Strike()
    {
        float dir = facingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + new Vector2(dir * attackRange * 0.5f, 0f);
        Vector2 size = new Vector2(attackRange, attackHeight * 2f);

        if (player == null) return;

        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            if (hit.GetComponentInParent<PlayerController>() != player) continue;

            if (!player.pState.invincible)
            {
                player.TakeDamage(damage);
                player.HitStopTime(0.05f, 5, .2f);
            }
            break;
        }
    }

    private bool PlayerWithin(float range)
    {
        return range > 0f && player != null && Vector2.Distance(transform.position, player.transform.position) <= range;
    }

    private void FacePlayer()
    {
        if (player == null) return;

        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) < turnDeadZone) return; // directly above or below: keep facing the same way
        if (Time.time < nextFlipTime) return;

        if ((dx > 0f && !facingRight) || (dx < 0f && facingRight))
        {
            Flip();
            nextFlipTime = Time.time + flipCooldown;
        }
    }

    private float HorizontalGap()
    {
        return player != null ? Mathf.Abs(player.transform.position.x - transform.position.x) : float.MaxValue;
    }

    private bool PathBlocked()
    {
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D wall = wallCheck != null
            ? Physics2D.Raycast(wallCheck.position, dir, checkDistance, whatIsGround) : default;
        bool wallAhead = wall.collider != null && !wall.collider.transform.IsChildOf(transform);

        bool noGround = pitCheck != null && !Physics2D.Raycast(pitCheck.position, Vector2.down, checkDistance, whatIsGround);

        return wallAhead || noGround;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        float dir = facingRight ? 1f : -1f;
        Vector2 center = (Vector2)transform.position + new Vector2(dir * attackRange * 0.5f, 0f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, new Vector3(attackRange, attackHeight * 2f, 0f));
    }
}