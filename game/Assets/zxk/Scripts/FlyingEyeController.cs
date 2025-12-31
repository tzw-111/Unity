using UnityEngine;
using System.Collections;

// 删除 EnemyState 定义，直接使用 Enemy2DController 中的枚举

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class FlyingEyeController : MonoBehaviour
{
    [Header("基础属性")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 2f;

    [Header("二维巡逻设置")]
    public Vector2 patrolAreaSize = new Vector2(8f, 6f);
    private Vector2 currentPatrolTarget;
    public float minPatrolWaitTime = 2f;
    public float maxPatrolWaitTime = 4f;
    private float patrolWaitTimer;
    private bool isWaitingAtPatrolPoint = false;
    private Vector2 patrolStartPos;

    [Header("圆形侦测设置")]
    public float detectRadius = 8f;
    public Transform detectCenter;
    public LayerMask targetLayer;
    public LayerMask obstacleLayer;

    [Header("攻击设置")]
    public float attackRange = 1.5f;
    public float attackDamage = 20f;
    public float attackCooldown = 1f;
    private float lastAttackTime;
    private int nextAttackIndex = 0;

    [Header("受击设置")]
    public float hurtForce = 5f;
    public float hurtDuration = 0.5f;

    [Header("死亡设置")]
    public float deathDestroyDelay = 2f;
    public GameObject deathEffect;

    public EnemyState currentState; // 使用原枚举
    private Rigidbody2D rb;
    private Animator animator;
    private Transform target;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (detectCenter == null) detectCenter = transform;

        currentHealth = maxHealth;
        patrolStartPos = transform.position;
        lastAttackTime = -attackCooldown;
        patrolWaitTimer = 0f;
        currentPatrolTarget = GetRandomPatrolPoint();
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead) return;

        UpdateAnimationParameters();

        switch (currentState)
        {
            case EnemyState.Patrol:
                PatrolBehaviour();
                break;
            case EnemyState.Chase:
                ChaseBehaviour();
                break;
            case EnemyState.Attack:
                AttackBehaviour();
                break;
        }

        CheckStateTransitions();
    }

    private void FixedUpdate()
    {
        if (currentState == EnemyState.Dead || currentState == EnemyState.Hurt) return;

        if (currentState == EnemyState.Patrol)
            PatrolMovement();
        else if (currentState == EnemyState.Chase)
            ChaseMovement();
    }

    #region 动画参数更新
    private void UpdateAnimationParameters()
    {
        animator.SetBool("isPatrolling", currentState == EnemyState.Patrol);
        animator.SetBool("isChasing", currentState == EnemyState.Chase);
        animator.SetBool("isAttacking", currentState == EnemyState.Attack);
    }
    #endregion

    #region 巡逻逻辑
    private void PatrolBehaviour()
    {
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= Random.Range(minPatrolWaitTime, maxPatrolWaitTime))
            {
                currentPatrolTarget = GetRandomPatrolPoint();
                isWaitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
            }
            return;
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);
        if (distanceToTarget <= 0.1f)
        {
            isWaitingAtPatrolPoint = true;
            rb.velocity = Vector2.zero;
        }
    }

    private void PatrolMovement()
    {
        if (isWaitingAtPatrolPoint) return;

        Vector2 moveDirection = (currentPatrolTarget - (Vector2)transform.position).normalized;
        rb.velocity = moveDirection * moveSpeed;

        if (moveDirection.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * Mathf.Sign(moveDirection.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    private Vector2 GetRandomPatrolPoint()
    {
        Vector2 randomPoint;
        float minDistance = 2f;
        do
        {
            float randomX = patrolStartPos.x + Random.Range(-patrolAreaSize.x / 2, patrolAreaSize.x / 2);
            float randomY = patrolStartPos.y + Random.Range(-patrolAreaSize.y / 2, patrolAreaSize.y / 2);
            randomPoint = new Vector2(randomX, randomY);
        } while (Vector2.Distance(transform.position, randomPoint) < minDistance);

        return randomPoint;
    }
    #endregion

    #region 追击逻辑
    private void ChaseBehaviour()
    {
        if (target == null) return;
    }

    private void ChaseMovement()
    {
        if (target == null) return;

        Vector2 moveDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
        rb.velocity = moveDirection * moveSpeed * 1.5f;

        if (moveDirection.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * Mathf.Sign(moveDirection.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }
    #endregion

    #region 攻击逻辑
    private void AttackBehaviour()
    {
        if (target == null) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            nextAttackIndex = Random.Range(0, 2);
            animator.SetInteger("attackIndex", nextAttackIndex);

            AttackTarget();
            lastAttackTime = Time.time;
        }

        rb.velocity = Vector2.zero;
    }

    private void AttackTarget()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, targetLayer);
        foreach (var hitCollider in hitColliders)
        {
            // 使用原有的 Health 组件
            Health playerHealth = hitCollider.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("敌人攻击了玩家，造成" + attackDamage + "点伤害");
            }
        }
    }
    #endregion

    #region 受击与死亡
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (currentState == EnemyState.Dead) return;

        currentHealth -= damage;
        Debug.Log("敌人受击，剩余生命值：" + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        animator.SetTrigger("hurtTrigger");
        currentState = EnemyState.Hurt;
        rb.velocity = Vector2.zero;
        rb.AddForce(hitDirection.normalized * hurtForce, ForceMode2D.Impulse);

        StartCoroutine(ExitHurtState());
    }

    private IEnumerator ExitHurtState()
    {
        yield return new WaitForSeconds(hurtDuration);
        if (currentState != EnemyState.Dead)
        {
            currentState = IsPlayerInDetectRange() ?
                (IsPlayerInAttackRange() ? EnemyState.Attack : EnemyState.Chase) :
                EnemyState.Patrol;
        }
    }

    private void Die()
    {
        currentState = EnemyState.Dead;
        rb.velocity = Vector2.zero;
        animator.SetTrigger("dieTrigger");

        GetComponent<Collider2D>().enabled = false;
        rb.isKinematic = true;

        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject, deathDestroyDelay);
        Debug.Log("敌人死亡");
    }
    #endregion

    #region 状态检测
    private void CheckStateTransitions()
    {
        if (currentHealth <= 0 && currentState != EnemyState.Dead)
        {
            Die();
            return;
        }

        if (currentState == EnemyState.Hurt) return;

        bool isPlayerInDetect = IsPlayerInDetectRange();
        bool isPlayerInAttack = isPlayerInDetect && IsPlayerInAttackRange();

        if (isPlayerInAttack)
        {
            currentState = EnemyState.Attack;
        }
        else if (isPlayerInDetect)
        {
            currentState = EnemyState.Chase;
        }
        else
        {
            target = null;
            currentState = EnemyState.Patrol;
        }
    }

    private bool IsPlayerInDetectRange()
    {
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(detectCenter.position, detectRadius, targetLayer);
        foreach (var collider in collidersInRange)
        {
            RaycastHit2D hit = Physics2D.Linecast(detectCenter.position, collider.transform.position, obstacleLayer);
            if (!hit)
            {
                target = collider.transform;
                return true;
            }
        }
        target = null;
        return false;
    }

    private bool IsPlayerInAttackRange()
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) <= attackRange;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(patrolStartPos, patrolAreaSize);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentPatrolTarget, 0.2f);

        if (detectCenter != null)
        {
            Gizmos.color = IsPlayerInDetectRange() ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(detectCenter.position, detectRadius);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion
}