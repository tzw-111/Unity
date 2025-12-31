using UnityEngine;
using System.Collections;
using System.Security.Cryptography.X509Certificates;

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
    [Header("动画设置")]
    public Animator enemyAnimator; // 敌人动画控制器

    [Header("基础属性")]
    public float maxHealth = 100f;          // 最大生命值
    public float currentHealth;             // 当前生命值
    public float moveSpeed = 2f;            // 移动速度

    [Header("二维巡逻设置")]
    public Vector2 patrolAreaSize = new Vector2(8f, 6f); // 巡逻区域大小（X宽，Y高）
    private Vector2 currentPatrolTarget;    // 当前要移动的巡逻目标点
    public float minPatrolWaitTime = 2f;    // 到达目标点后最小等待时间（延长避免闪烁）
    public float maxPatrolWaitTime = 4f;    // 到达目标点后最大等待时间
    private float patrolWaitTimer;          // 到达目标点后的等待计时器
    private bool isWaitingAtPatrolPoint = false; // 是否在目标点等待
    private Vector2 patrolStartPos;         // 巡逻区域中心点（初始位置）

    [Header("圆形侦测设置")]
    public float detectRadius = 8f;         // 圆形侦测半径（替代原视野距离）
    public Transform detectCenter;          // 侦测中心点（可选，默认自身位置）
    public LayerMask targetLayer;           // 目标图层（玩家）
    public LayerMask obstacleLayer;         // 障碍物图层

    [Header("攻击设置")]
    public float attackRange = 1.5f;        // 攻击范围
    public float attackDamage = 20f;        // 攻击伤害
    public float attackCooldown = 1f;       // 攻击冷却
    private float lastAttackTime;           // 上次攻击时间

    [Header("受击设置")]
    public float hurtForce = 5f;            // 受击击退力
    public float hurtDuration = 0.5f;       // 受击状态持续时间

    [Header("死亡设置")]
    public float deathDestroyDelay = 2f;    // 死亡后销毁延迟
    public GameObject deathEffect;          // 死亡特效（可选）

    // 状态和移动相关
    public EnemyState currentState;
    private Rigidbody2D rb;
    private Transform target;               // 检测到的目标（玩家）

    private void Awake()
    {
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        if (detectCenter == null) detectCenter = transform; // 侦测中心默认自身

        // 初始化
        currentHealth = maxHealth;
        patrolStartPos = transform.position;
        lastAttackTime = -attackCooldown;
        patrolWaitTimer = 0f;

        // 初始化第一个巡逻目标点（添加最小距离限制）
        currentPatrolTarget = GetRandomPatrolPoint();

        // 获取Animator组件（如果未手动赋值，自动获取）
        if (enemyAnimator == null)
        {
            enemyAnimator = GetComponent<Animator>();
        }
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

        /************************************************************测试区*/

                Vector2 hD = Vector2.right;
                TakeDamage(1, hD);//受击测试
        /**************************************************************测试区*/

        // 状态切换的核心检测（优先级：死亡 > 受击 > 攻击 > 追击 > 巡逻）
        CheckStateTransitions();    
    }

    private void FixedUpdate()
    {
        // 核心修改：死亡/受击状态下，直接返回，不执行任何移动
        if (currentState == EnemyState.Dead || currentState == EnemyState.Hurt)
        {
            // 双重保险：强制停移
            rb.velocity = Vector2.zero;
            return;
        }

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
    // 二维巡逻行为（决策层）
    private void PatrolBehaviour()
    {
        // 如果正在目标点等待，执行等待逻辑
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer += Time.deltaTime;
            // 等待时间足够，生成新目标点并结束等待
            if (patrolWaitTimer >= UnityEngine.Random.Range(minPatrolWaitTime, maxPatrolWaitTime))
            {
                currentPatrolTarget = GetRandomPatrolPoint();
                isWaitingAtPatrolPoint = false;
                patrolWaitTimer = 0f;
            }
            return;
        }

        // 计算与当前巡逻目标点的距离
        float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);
        // 到达目标点（距离小于0.1，避免精度问题）
        if (distanceToTarget <= 0.1f)
        {
            isWaitingAtPatrolPoint = true; // 开始等待
            rb.velocity = Vector2.zero;    // 停止移动
        }
    }

    // 二维巡逻移动（执行层，修复旋转/闪烁问题）
    private void PatrolMovement()
    {
        // 等待时不移动
        if (isWaitingAtPatrolPoint) return;

        // 计算朝向目标点的方向（归一化，保证移动速度一致）
        Vector2 moveDirection = (currentPatrolTarget - (Vector2)transform.position).normalized;

        // 控制刚体移动（二维方向，速度由moveSpeed决定）
        rb.velocity = moveDirection * moveSpeed;

        // 横版游戏适配：仅左右翻转Sprite，不旋转
        if (moveDirection.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * Mathf.Sign(moveDirection.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    // 生成随机巡逻点（添加最小距离限制，避免闪烁）
    private Vector2 GetRandomPatrolPoint()
    {
        Vector2 randomPoint;
        float minDistance = 2f; // 目标点与当前位置的最小距离
        do
        {
            // 计算随机X坐标：patrolStartPos.x ± patrolAreaSize.x/2
            float randomX = patrolStartPos.x + UnityEngine.Random.Range(-patrolAreaSize.x / 2, patrolAreaSize.x / 2);
            // 计算随机Y坐标：patrolStartPos.y ± patrolAreaSize.y/2
            float randomY = patrolStartPos.y + UnityEngine.Random.Range(-patrolAreaSize.y / 2, patrolAreaSize.y / 2);
            randomPoint = new Vector2(randomX, randomY);
        } while (Vector2.Distance(transform.position, randomPoint) < minDistance);

        return randomPoint;
    }

    // 追击行为
    private void ChaseBehaviour()
    {
        // 空检测
        if (target == null) return;
    }

    // 追击移动（二维方向，持续追击玩家）
    private void ChaseMovement()
    {
        if (target == null) return;

        // 计算朝向目标的方向（归一化）
        Vector2 moveDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
        // 追击速度比巡逻快50%
        rb.velocity = moveDirection * moveSpeed * 1.5f;

        // 追击时仅左右翻转Sprite
        if (moveDirection.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(transform.localScale.x) * Mathf.Sign(moveDirection.x),
                transform.localScale.y,
                transform.localScale.z
            );
        }
    }

    // 攻击行为（持续攻击，直到玩家脱离范围）
    private void AttackBehaviour()
    {
        if (target == null) return;

        // 冷却时间到则执行攻击
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            AttackTarget();
            lastAttackTime = Time.time;
        }

        // 攻击时停止移动，避免贴脸抖动
        rb.velocity = Vector2.zero;
    }

    // 执行攻击
    private void AttackTarget()
    {
        // 触发攻击动画
        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger("AttackTrigger");
        }

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
                UnityEngine.Debug.Log("敌人攻击了玩家，造成" + attackDamage + "点伤害");
            }
        }
    }

    // 受击处理
    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (currentState == EnemyState.Dead) return;

        // 1. 受击瞬间强制停止所有移动（核心修改）
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f; // 额外防止旋转（如果有）

        // 扣血
        currentHealth -= damage;
        UnityEngine.Debug.Log("敌人受击，剩余生命值：" + currentHealth);

        // 触发受击动画
        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger("HurtTrigger");
        }

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
        // 受击结束后回到追击/攻击（如果玩家还在范围）或巡逻
        if (currentState != EnemyState.Dead)
        {
            rb.velocity = Vector2.zero; // 防止击退力残留
            currentState = IsPlayerInDetectRange() ? (IsPlayerInAttackRange() ? EnemyState.Attack : EnemyState.Chase) : EnemyState.Patrol;
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

        // 触发死亡动画
        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger("DeadTrigger");
        }

        // 播放死亡特效（如果有）
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // 延迟销毁
        Destroy(gameObject, deathDestroyDelay);
        UnityEngine.Debug.Log("敌人死亡");
    }
    #endregion

    #region 状态切换检测（核心修改：圆形侦测+持续攻击）
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

        // 3. 检测玩家是否在圆形侦测范围内
        bool isPlayerInDetect = IsPlayerInDetectRange();

        // 4. 检测玩家是否在攻击范围内
        bool isPlayerInAttack = isPlayerInDetect && IsPlayerInAttackRange();

        // 设置巡逻/追击动画参数
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool("IsPatrol", currentState == EnemyState.Patrol);
            enemyAnimator.SetBool("IsChase", currentState == EnemyState.Chase);
        }


        // 状态切换逻辑：只要玩家在侦测范围，就持续追击/攻击
        if (isPlayerInAttack)
        {
            // 玩家在攻击范围，持续攻击
            currentState = EnemyState.Attack;
        }
        else if (isPlayerInDetect)
        {
            // 玩家在侦测范围但不在攻击范围，持续追击
            currentState = EnemyState.Chase;
        }
        else
        {
            // 玩家脱离侦测范围，回到巡逻
            target = null;
            currentState = EnemyState.Patrol;
        }
    }

    // 检测玩家是否在圆形侦测范围内（无角度限制，纯圆形）
    private bool IsPlayerInDetectRange()
    {
        // 检测圆形范围内的所有玩家
        Collider2D[] collidersInRange = Physics2D.OverlapCircleAll(detectCenter.position, detectRadius, targetLayer);

        foreach (var collider in collidersInRange)
        {
            Transform player = collider.transform;
            // 检测是否有障碍物阻挡
            RaycastHit2D hit = Physics2D.Linecast(
                detectCenter.position,
                player.position,
                obstacleLayer
            );

            // 无障碍物则检测到玩家，持续锁定
            if (!hit)
            {
                target = player;
                return true;
            }
        }

        // 未检测到玩家
        target = null;
        return false;
    }

    // 检测玩家是否在攻击范围内
    private bool IsPlayerInAttackRange()
    {
        if (target == null) return false;
        return Vector2.Distance(transform.position, target.position) <= attackRange;
    }
    #endregion

    #region Gizmos可视化（调试用：圆形侦测范围）
    private void OnDrawGizmos()
    {
        // 绘制二维巡逻区域（矩形）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(patrolStartPos, patrolAreaSize);

        // 绘制当前巡逻目标点
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(currentPatrolTarget, 0.2f);

        // 绘制圆形侦测范围（核心修改）
        if (detectCenter != null)
        {
            Gizmos.color = IsPlayerInDetectRange() ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(detectCenter.position, detectRadius);
        }

        // 绘制攻击范围
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion
}