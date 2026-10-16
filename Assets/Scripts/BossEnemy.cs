using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BossEnemy : Enemy
{
    private enum BossState { Choosing, AcidRainDash, Lunge, SpawnMinion, Slam, Stunned }

    [Header("General Boss Settings")]
    [SerializeField] private float timeBetweenAttacks = 1.5f;
    [SerializeField] private BossState currentBossState = BossState.Choosing;
    [SerializeField] private float initialDelay = 2f;
    [SerializeField] private float bossWidth = 1f;
    [Space(5)]

    [Header("Flight & Movement")]
    [SerializeField] private float flightSpeed = 16f;
    [SerializeField] private float hoverHeight = 10f;
    [SerializeField] private float roomLeftBoundary = -25f;
    [SerializeField] private float roomRightBoundary = 25;
    [SerializeField] private float initialYSpeed = 3f;
    [SerializeField] private float fightYSpeed = 6f;
    private float ySpeed;
    [Space(5)]

    [Header("Attack 1: Acid Rain Projectiles and Dash")]
    [SerializeField] private EnemyProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float dropInterval = 0.25f;
    [SerializeField] private float dashSpeed = 24f;
    [Space(5)]

    [Header("Attack 2: Lunge & Wall Stun")]
    [SerializeField] private float lungeSpeed = 32f;
    [SerializeField] private float groundLevelY = 1f;
    [SerializeField] private float stunDuration = 2.5f;
    [SerializeField] private float lungeDelay = 0.6f;
    [Space(5)]

    [Header("Attack 3: Minion Spawn")]
    [SerializeField] private GameObject[] bossMinionPrefabs;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnDelay = 0.5f;
    [Space(5)]

    [Header("Attack 4: Tracking & Slam")]
    [SerializeField] private float trackingDuration = 2f;
    [SerializeField] private float slamSpeed = 40f;
    [SerializeField] private float slamTrackSpeed = 10f;
    [SerializeField] private float slamAdditionalHeight = 6f;
    [SerializeField] private float slamDelay = 1f;
    [Space(5)]

    [Header("Damage")]
    [SerializeField] private float bossContactDamage = 1f;
    [SerializeField] private float acidDamage = 1f;
    [SerializeField] private float lungeDamage = 2f;
    [SerializeField] private float slamDamage = 2f;

    private Animator anim;
    private int lastAttackIndex = -1;

    protected override void Start()
    {
        base.Start();
        anim = GetComponent<Animator>();
        if (rb != null) rb.gravityScale = 0f;
        StartCoroutine(BossLoop());
    }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;
        if (currentBossState == BossState.Choosing && player != null)
        {
            FacePlayer();
        }
    }

    private IEnumerator BossLoop()
    {
        ySpeed = initialYSpeed;
        // Initial delay before fight starts
        yield return new WaitForSeconds(initialDelay);
        ySpeed = fightYSpeed;

        while (!isDead)
        {
            currentBossState = BossState.Choosing;
            damage = bossContactDamage;
            yield return new WaitForSeconds(timeBetweenAttacks);

            int attackChoice = GetRandomAttackIndex();
            lastAttackIndex = attackChoice;

            switch (attackChoice)
            {
                case 0:
                    yield return StartCoroutine(AcidRainDash());
                    break;
                case 1:
                    yield return StartCoroutine(Lunge());
                    break;
                case 2:
                    yield return StartCoroutine(SpawnMinion());
                    break;
                case 3:
                    yield return StartCoroutine(Slam());
                    break;
            }
        }
    }

    private int GetRandomAttackIndex()
    {
        int choice = Random.Range(0, 4);
        while (choice == lastAttackIndex)
        {
            choice = Random.Range(0, 4);
        }
        return choice;
    }

    private IEnumerator AcidRainDash()
    {
        currentBossState = BossState.AcidRainDash;

        // 1. Go to the nearest edge
        bool isOnRightSide = rb.position.x > (roomLeftBoundary + roomRightBoundary) / 2f;
        float startEdgeX = isOnRightSide ? roomRightBoundary - bossWidth : roomLeftBoundary + bossWidth;
        float endEdgeX = isOnRightSide ? roomLeftBoundary + bossWidth : roomRightBoundary - bossWidth;
        float dashDir = isOnRightSide ? -1f : 1f;

        Face(isOnRightSide);
        yield return MoveTo(new Vector2(startEdgeX, rb.position.y), flightSpeed);
        if (isDead) yield break;

        // 2. Dash and drop acid
        if (anim != null) anim.SetTrigger("AcidRainDash");
        
        float targetX = isOnRightSide ? roomLeftBoundary + bossWidth : roomRightBoundary - bossWidth;
        Face(dashDir > 0f);

        float nextDropTime = 0f;
        float timeout = 6f;

        while (!isDead && timeout > 0f && Mathf.Abs(rb.position.x - endEdgeX) > 1f)
        {
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            timeout -= Time.fixedDeltaTime;

            if (Time.time >= nextDropTime && projectilePrefab != null)
            {
                nextDropTime = Time.time + dropInterval;
                Vector2 dropPos = firePoint != null ? (Vector2)firePoint.position : rb.position;
                EnemyProjectile p = Instantiate(projectilePrefab, dropPos, Quaternion.identity);
                p.Drop(acidDamage); // Different method for different physics
            }

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
    }

    private IEnumerator Lunge()
    {
        currentBossState = BossState.Lunge;

        // 1. Go to the nearest edge
        bool isOnRightSide = rb.position.x > (roomLeftBoundary + roomRightBoundary) / 2f;
        float startEdgeX = isOnRightSide ? roomRightBoundary - bossWidth : roomLeftBoundary + bossWidth;
        float lungeDir = isOnRightSide ? -1f : 1f;

        Face(isOnRightSide);
        yield return MoveTo(new Vector2(startEdgeX, rb.position.y), flightSpeed);
        if (isDead) yield break;

        // 2. Lower
        yield return MoveTo(new Vector2(rb.position.x, groundLevelY), flightSpeed, 2f);
        if (isDead) yield break;

        // 3. Warning
        Face(lungeDir > 0f);
        if (anim != null) anim.SetTrigger("Warning");
        yield return new WaitForSeconds(lungeDelay);
        if (isDead) yield break;

        // 4. Lunge
        if (anim != null) anim.SetTrigger("Lunge");
        damage = lungeDamage;

        float moveTime = 0.15f;
        float timeout = 3f;

        while (!isDead && timeout > 0f)
        {
            rb.linearVelocity = new Vector2(lungeDir * lungeSpeed, 0f);
            timeout -= Time.fixedDeltaTime;
            moveTime -= Time.fixedDeltaTime;

            bool pastBoundary = rb.position.x <= roomLeftBoundary || rb.position.x >= roomRightBoundary;
            bool blocked = moveTime <= 0f && Mathf.Abs(rb.linearVelocity.x) < 1f; 

            if (pastBoundary || blocked) break;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;

        // 5. Stun
        currentBossState = BossState.Stunned;
        damage = 0f;
        if (anim != null) anim.SetTrigger("Stunned");
        yield return new WaitForSeconds(stunDuration);
    }

    private IEnumerator SpawnMinion()
    {
        currentBossState = BossState.SpawnMinion;
        if (anim != null) anim.SetTrigger("Spawn");

        yield return new WaitForSeconds(spawnDelay);

        if (bossMinionPrefabs != null && bossMinionPrefabs.Length > 0)
        {
            int randomIndex = Random.Range(0, bossMinionPrefabs.Length);
            GameObject chosenPrefab = bossMinionPrefabs[randomIndex];
            Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + Vector3.down;

            Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.8f);
    }

    private IEnumerator Slam()
    {
        currentBossState = BossState.Slam;

        if (anim != null) anim.SetTrigger("TrackStart");

        // 1. Track Player from above
        float trackTimer = 0f;
        while (!isDead && trackTimer < trackingDuration)
        {
            trackTimer += Time.fixedDeltaTime;

            if (player != null)
            {
                float targetX = Mathf.Clamp(player.transform.position.x, roomLeftBoundary, roomRightBoundary);
                Vector2 targetPos = new Vector2(targetX, hoverHeight + slamAdditionalHeight);

                Vector2 desired = (targetPos - rb.position) * 5f;
                rb.linearVelocity = Vector2.ClampMagnitude(desired, slamTrackSpeed);
            }

            yield return new WaitForFixedUpdate();
        }

        // 2. Pause
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetTrigger("Warning");
        yield return new WaitForSeconds(slamDelay);
        if (isDead) yield break;

        // 3. Slam 
        if (anim != null) anim.SetTrigger("Slam");
        damage = slamDamage;

        float grace = 0.1f;
        float timeout = 2f;

        while (!isDead && timeout > 0f)
        {
            rb.linearVelocity = new Vector2(0f, -slamSpeed);
            timeout -= Time.fixedDeltaTime;
            grace -= Time.fixedDeltaTime;

            bool landed = grace <= 0f && rb.linearVelocity.y > -1f;
            if (landed || GroundDistance() <= 0.5f) break;

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;

        // 4. Stun
        currentBossState = BossState.Stunned;
        damage = 0f;
        if (anim != null) anim.SetTrigger("Stunned");
        yield return new WaitForSeconds(1.5f);
    }


    private float GroundDistance()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 15f, whatIsGround);
        return hit.collider != null ? hit.distance : float.PositiveInfinity;
    }

    private void FacePlayer()
    {
        if (player == null) return;
        float dx = player.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.5f)
        {
            Face(dx > 0f);
        }
    }

    private void Face(bool right)
    {
        if (facingRight == right) return;
        facingRight = right;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (right ? 1f : -1f);
        transform.localScale = s;
    }
    private IEnumerator MoveTo(Vector2 target, float speed, float timeout = 8f)
    {
        while (Vector2.Distance(rb.position, target) > 0.3f && timeout > 0f  && !isDead)
        {
            rb.linearVelocity = (target - rb.position).normalized * speed;
            timeout -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = Vector2.zero;
    }

    protected override void FixedUpdate()
    {
        if (isDead || isRecoiling) return;

        if (currentBossState == BossState.Choosing)
        {
            float g = GroundDistance();
            float vy = float.IsInfinity(g) ? 0f : Mathf.Clamp((hoverHeight - g) * ySpeed, -ySpeed, ySpeed);
            rb.linearVelocity = new Vector2(0f, vy);
        }
    }

    protected override void PatrolCheck() { } // Block base.PatrolCheck()

    protected override void Die()
    {
        StopAllCoroutines();
        base.Die();
    }

    public override void EnemyHit(float _damageDone, Vector2 _hitDirection, float _hitForce) // No recoil
    {
        if (isDead) return;
        health -= _damageDone;
    }
}