using UnityEngine;
using System.Collections;
using System.Diagnostics;

// 定义敌人的所有状态
public enum EnemyState
{
    Patrol,     // 巡逻
    Chase,      // 追击
    Attack,     // 攻击
    Hurt,       // 受击
    Dead        // 死亡
}

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Enemy2DController : MonoBehaviour
{
    [Header("basic")]
    public float maxHealth = 100f;          // 最大生命值
    public float currentHealth;             // 当前生命值
    public float moveSpeed = 2f;            // 移动速度
    public float patrolRange = 5f;          // 巡逻范围

    [Header("visio")]
    public float viewDistance = 8f;         // 视野距离
    public float viewAngle = 90f;           // 视野角度
    public Transform eyePoint;              // 视野检测点（可选）
    public LayerMask targetLayer;           // 目标图层（玩家）
    public LayerMask obstacleLayer;         // 障碍物图层

    [Header("attack")]
    public float attackRange = 1.5f;        // 攻击范围
    public float attackDamage = 20f;        // 攻击伤害
    public float attackCooldown = 1f;       // 攻击冷却
    private float lastAttackTime;           // 上次攻击时间

    [Header("be attacked")]
    public float hurtForce = 5f;            // 受击击退力
    public float hurtDuration = 0.5f;       // 受击状态持续时间

    [Header("die")]
    public float deathDestroyDelay = 2f;    // 死亡后销毁延迟
    public GameObject deathEffect;          // 死亡特效（可选）

    // 状态和移动相关
    public EnemyState currentState;
    private Rigidbody2D rb;
    private Vector2 patrolStartPos;
    private int patrolDirection = 1;        // 巡逻方向：1向右，-1向左
    private Transform target;               // 检测到的目标（玩家）

    private void Awake()
    {
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        if (eyePoint == null) eyePoint = transform;

        // 初始化
        currentHealth = maxHealth;
        patrolStartPos = transform.position;
        lastAttackTime = -attackCooldown;
    }

    private void Update()
    {
        // 死亡状态下不执行任何逻辑
        if (currentState == EnemyState.Dead) return;

        // 根据当前状态执行对应行为
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
            case EnemyState.Hurt:
                // 受击状态由协程处理，Update中无需额外逻辑
                break;
        }

        // 状态切换的核心检测（优先级：死亡 > 受击 > 攻击 > 追击 > 巡逻）
        CheckStateTransitions();
    }

    private void FixedUpdate()
    {
        // 物理相关的移动逻辑放在FixedUpdate中
        if (currentState == EnemyState.Dead || currentState == EnemyState.Hurt) return;

        if (currentState == EnemyState.Patrol)
        {
            PatrolMovement();
        }
        else if (currentState == EnemyState.Chase)
        {
            ChaseMovement();
        }
    }

    #region 状态行为逻辑
    // 巡逻行为
    private void PatrolBehaviour()
    {
        // 检测是否超出巡逻范围，超出则反转方向
        float distanceFromStart = Mathf.Abs(transform.position.x - patrolStartPos.x);
        if (distanceFromStart >= patrolRange)
        {
            patrolDirection *= -1;
            // 修正位置，防止超出范围
            transform.position = new Vector2(
                patrolStartPos.x + patrolRange * Mathf.Sign(patrolDirection),
                transform.position.y
            );
        }
    }

    // 巡逻移动
    private void PatrolMovement()
    {
        rb.velocity = new Vector2(patrolDirection * moveSpeed, rb.velocity.y);
        // 翻转敌人朝向
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * patrolDirection, transform.localScale.y, transform.localScale.z);
    }

    // 追击行为
    private void ChaseBehaviour()
    {
        // 空检测
        if (target == null) return;
    }

    // 追击移动
    private void ChaseMovement()
    {
        if (target == null) return;

        // 计算朝向目标的方向
        float direction = Mathf.Sign(target.position.x - transform.position.x);
        rb.velocity = new Vector2(direction * moveSpeed * 1.5f, rb.velocity.y); // 追击速度比巡逻快50%
        // 翻转朝向
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * direction, transform.localScale.y, transform.localScale.z);
    }

    // 攻击行为
    private void AttackBehaviour()
    {
        if (target == null) return;

        // 冷却时间到则执行攻击
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            AttackTarget();
            lastAttackTime = Time.time;
        }
    }

    // 执行攻击
    private void AttackTarget()
    {
        // 检测攻击范围内的目标
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange, targetLayer);
        foreach (var hitCollider in hitColliders)
        {
            // 假设玩家有Health组件，调用受击方法
            Health playerHealth = hitCollider.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                // 可以在这里添加攻击动画、音效等
                //Debug.Log("敌人攻击了玩家，造成" + attackDamage + "点伤害");
            }
        }
    }

    // 受击处理
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (currentState == EnemyState.Dead) return;

        // 扣血
        currentHealth -= damage;
        //Debug.Log("敌人受击，剩余生命值：" + currentHealth);

        // 血量小于等于0则死亡
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // 进入受击状态
        currentState = EnemyState.Hurt;
        // 施加击退力
        rb.velocity = Vector2.zero;
        rb.AddForce(hitDirection.normalized * hurtForce, ForceMode2D.Impulse);
        // 受击结束后恢复之前的状态
        StartCoroutine(ExitHurtState());
    }

    // 退出受击状态
    private IEnumerator ExitHurtState()
    {
        yield return new WaitForSeconds(hurtDuration);
        // 受击结束后回到巡逻或追击状态
        if (currentState != EnemyState.Dead)
        {
            currentState = IsTargetInView() ? EnemyState.Chase : EnemyState.Patrol;
        }
    }

    // 死亡处理
    private void Die()
    {
        currentState = EnemyState.Dead;
        rb.velocity = Vector2.zero;
        // 禁用碰撞体和刚体（可选）
        GetComponent<Collider2D>().enabled = false;
        rb.isKinematic = true;

        // 播放死亡特效（如果有）
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 延迟销毁
        Destroy(gameObject, deathDestroyDelay);
        //Debug.Log("badpeople die");
    }
    #endregion

    #region 状态切换检测
    private void CheckStateTransitions()
    {
        // 1. 检测是否死亡（优先级最高）
        if (currentHealth <= 0 && currentState != EnemyState.Dead)
        {
            Die();
            return;
        }

        // 2. 受击状态下不切换其他状态
        if (currentState == EnemyState.Hurt) return;

        // 3. 检测是否有目标在视野内
        bool hasTargetInView = IsTargetInView();

        // 4. 检测目标是否在攻击范围内
        bool isTargetInAttackRange = hasTargetInView && Vector2.Distance(transform.position, target.position) <= attackRange;

        // 状态切换逻辑
        if (isTargetInAttackRange)
        {
            // 目标在攻击范围内，切换到攻击状态
            currentState = EnemyState.Attack;
        }
        else if (hasTargetInView)
        {
            // 目标在视野内但不在攻击范围，切换到追击状态
            currentState = EnemyState.Chase;
        }
        else
        {
            // 无目标，回到巡逻状态
            currentState = EnemyState.Patrol;
        }
    }

    // 检测视野内是否有目标
    private bool IsTargetInView()
    {
        // 检测视野范围内的所有目标
        Collider2D[] collidersInView = Physics2D.OverlapCircleAll(eyePoint.position, viewDistance, targetLayer);

        foreach (var collider in collidersInView)
        {
            Transform potentialTarget = collider.transform;
            // 计算目标方向
            Vector2 directionToTarget = (potentialTarget.position - eyePoint.position).normalized;

            // 检测角度是否在视野范围内
            float angle = Vector2.Angle(eyePoint.right, directionToTarget);
            if (angle <= viewAngle / 2)
            {
                // 检测是否有障碍物阻挡
                RaycastHit2D hit = Physics2D.Raycast(
                    eyePoint.position,
                    directionToTarget,
                    viewDistance,
                    obstacleLayer
                );

                // 无障碍物则检测到目标
                if (!hit)
                {
                    target = potentialTarget;
                    return true;
                }
            }
        }

        // 未检测到目标
        target = null;
        return false;
    }
    #endregion

    #region Gizmos可视化（调试用）
    private void OnDrawGizmos()
    {
        // 绘制巡逻范围
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(patrolStartPos, new Vector2(patrolStartPos.x + patrolRange, patrolStartPos.y));
        Gizmos.DrawLine(patrolStartPos, new Vector2(patrolStartPos.x - patrolRange, patrolStartPos.y));

        // 绘制视野范围
        if (eyePoint != null)
        {
            Gizmos.color = IsTargetInView() ? Color.red : Color.yellow;
            // 绘制视野距离
            Gizmos.DrawWireSphere(eyePoint.position, viewDistance);
            // 绘制视野角度
            Vector2 viewAngleA = DirectionFromAngle(-viewAngle / 2, false);
            Vector2 viewAngleB = DirectionFromAngle(viewAngle / 2, false);
            Gizmos.DrawLine(eyePoint.position, (Vector2)eyePoint.position + viewAngleA * viewDistance);
            Gizmos.DrawLine(eyePoint.position, (Vector2)eyePoint.position + viewAngleB * viewDistance);
        }

        // 绘制攻击范围
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    // 计算视野角度的方向向量
    private Vector2 DirectionFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.z;
        }
        return new Vector2(Mathf.Cos(angleInDegrees * Mathf.Deg2Rad), Mathf.Sin(angleInDegrees * Mathf.Deg2Rad));
    }
    #endregion
}