using UnityEngine;

/// <summary>
/// 玩家移动与状态管理核心脚本
/// 功能：四向移动、待机、喝药（最高优先级）、三连击普攻（鼠标左键触发，可转向、窗口期续连）
/// 优先级：喝药 > 普攻 > 移动
/// 修复1：连击数到3后禁止持续触发普攻3
/// 修复2：普攻触发时错误切入向左普攻（优先同步朝向参数，再激活普攻状态）
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    #region 配置参数（Inspector可调）
    [Header("移动配置")]
    [Tooltip("角色移动速度")]
    public float moveSpeed = 5f;

    [Header("喝药配置")]
    [Tooltip("喝药触发按键（单击触发）")]
    public KeyCode drinkKey = KeyCode.R;
    [Tooltip("喝药动画时长（需与实际动画长度一致，单位：秒）")]
    public float drinkAnimationDuration = 0.375f;

    [Header("三连击普攻配置")]
    [Tooltip("普攻触发按键（鼠标左键）")]
    public KeyCode attackKey = KeyCode.Mouse0;
    [Tooltip("单段普攻动画时长（单位：秒）")]
    public float attackAnimationDuration = 0.5f;
    [Tooltip("连击间隔超时时间（窗口期，超过则重置连击，单位：秒）")]
    public float comboTimeoutDuration = 0.5f;
    #endregion

    #region 组件与状态变量
    private Rigidbody2D _rb;
    private Animator _anim;

    // 输入缓存
    private float _horizontalInput;
    private float _verticalInput;

    // 状态锁定（核心：确保朝向永不丢失，_lockedHorizontalDir本身是正确的）
    private float _lockedHorizontalDir = 1f; // 1:右  -1:左
    private bool _isFirstFrame = true;       // 初始化帧标记

    // 基础状态标记
    private bool _isMoving = false;
    private bool _isDrinking = false;        // 喝药状态（最高优先级）
    private float _drinkTimer = 0f;          // 喝药计时器

    // 三连击普攻状态标记
    private bool _isAttacking = false;       // 普攻状态（中优先级）
    private int _attackComboCount = 0;       // 当前连击数（0=无连击，1/2/3=对应三连击）
    private float _attackTimer = 0f;         // 单段普攻计时器
    private float _comboTimeoutTimer = 0f;   // 连击窗口期计时器
    #endregion

    #region 初始化
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        InitializeInitialState();
    }

    /// <summary>
    /// 初始化角色初始状态（强制锁定右向，避免初始偏移）
    /// </summary>
    private void InitializeInitialState()
    {
        // 强制设置Animator参数，优先于状态机默认值
        _anim.SetFloat("Horizontal", _lockedHorizontalDir);
        _anim.SetBool("IsMoving", false);
        _anim.SetFloat("Vertical", 0);
        _anim.SetBool("IsDrinking", false);
        _anim.SetBool("IsAttacking", false);
        _anim.SetFloat("AttackComboCount", 0f);

        // 强制刷新动画状态，避免初始帧参数漂移
        _anim.Update(0);

        // 物理初始化
        _rb.velocity = Vector2.zero;
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
    }
    #endregion

    #region 每帧逻辑（优先同步朝向，再处理普攻，解决错误切入左普攻问题）
    private void Update()
    {
        if (_isFirstFrame)
        {
            _isFirstFrame = false;
            return;
        }

        // 1. 实时获取输入（优先更新朝向，确保普攻触发前朝向已最新）
        HandleInput();

        // 强制同步朝向（每帧先同步朝向参数，再处理所有状态，核心修复）
        ForceSyncHorizontalDir();

        // 2. 最高优先级：喝药逻辑（同步朝向，不丢失右向）
        HandleDrinkTrigger();
        HandleDrinkCountdown();
        if (_isDrinking)
        {
            ResetAttackState();
            _isMoving = false;
            _anim.SetBool("IsMoving", false);
            return;
        }

        // 3. 中优先级：普攻逻辑（此时朝向已同步，避免错误切入左普攻）
        HandleAttackTrigger();
        HandleAttackCountdown();
        HandleComboTimeout();
        if (_isAttacking)
        {
            _isMoving = false;
            _anim.SetBool("IsMoving", false);
            _anim.SetFloat("AttackComboCount", (float)_attackComboCount);
            return;
        }

        // 4. 最低优先级：移动/待机逻辑
        UpdateMovementState();
        SyncAnimatorParams();
    }

    /// <summary>
    /// 核心：强制同步朝向参数（独立方法，确保任何状态下优先同步）
    /// 解决：普攻触发前朝向参数未同步，导致错误切入左普攻
    /// </summary>
    private void ForceSyncHorizontalDir()
    {
        // 强制将锁定的朝向同步给Animator，优先级高于所有状态切换
        _anim.SetFloat("Horizontal", _lockedHorizontalDir);
        // 强制刷新Animator状态，避免参数延迟生效（关键：让状态机立即识别朝向）
        _anim.Update(0);
    }

    /// <summary>
    /// 输入处理（原有逻辑不变，确保朝向只在有明确输入时更新）
    /// </summary>
    private void HandleInput()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
        _verticalInput = Input.GetAxisRaw("Vertical");

        // 仅当有明确左右输入时，才更新锁定朝向，否则保留原有朝向
        if (_horizontalInput != 0)
        {
            _lockedHorizontalDir = _horizontalInput;
        }
    }

    /// <summary>
    /// 更新移动状态（原有逻辑不变）
    /// </summary>
    private void UpdateMovementState()
    {
        _isMoving = (_horizontalInput != 0 || _verticalInput != 0);
    }

    /// <summary>
    /// 同步动画参数（原有逻辑，朝向已提前通过ForceSyncHorizontalDir同步）
    /// </summary>
    private void SyncAnimatorParams()
    {
        _anim.SetFloat("Vertical", _isMoving ? _verticalInput : 0);
        _anim.SetBool("IsMoving", _isMoving);
        _anim.SetBool("IsDrinking", _isDrinking);
        _anim.SetBool("IsAttacking", _isAttacking);
        _anim.SetFloat("AttackComboCount", (float)_attackComboCount);
    }
    #endregion

    #region 物理移动（原有逻辑不变）
    private void FixedUpdate()
    {
        if (_isDrinking || _isAttacking || _isFirstFrame)
        {
            _rb.velocity = Vector2.zero;
            return;
        }

        Vector2 moveDir = new Vector2(_horizontalInput, _verticalInput).normalized;
        _rb.velocity = moveDir * moveSpeed;
    }
    #endregion

    #region 喝药状态核心逻辑（原有逻辑不变，同步朝向）
    private void HandleDrinkTrigger()
    {
        if (Input.GetKeyDown(drinkKey) && !_isDrinking)
        {
            EnterDrinkState();
        }
    }

    private void HandleDrinkCountdown()
    {
        if (_isDrinking)
        {
            _drinkTimer += Time.deltaTime;
            if (_drinkTimer >= drinkAnimationDuration)
            {
                ExitDrinkState();
            }
        }
    }

    private void EnterDrinkState()
    {
        _isDrinking = true;
        _anim.SetBool("IsDrinking", true);
        _rb.velocity = Vector2.zero;
        // 进入喝药前同步朝向
        ForceSyncHorizontalDir();
        _drinkTimer = 0f;
    }

    private void ExitDrinkState()
    {
        _isDrinking = false;
        _anim.SetBool("IsDrinking", false);
        _drinkTimer = 0f;

        _isMoving = (_horizontalInput != 0 || _verticalInput != 0);
        _anim.SetBool("IsMoving", _isMoving);
        // 退出喝药后同步朝向
        ForceSyncHorizontalDir();
    }
    #endregion

    #region 三连击普攻核心逻辑（重点修复：普攻触发前先同步朝向，避免错误切入左普攻）
    private void HandleAttackTrigger()
    {
        // 仅当非喝药、连击数未满3时，允许触发普攻
        if (Input.GetKeyDown(attackKey) && !_isDrinking && _attackComboCount < 3)
        {
            // 核心修复1：触发普攻前，先强制同步最新朝向（确保状态机识别正确朝向）
            ForceSyncHorizontalDir();

            // 更新连击数
            if (!_isAttacking)
            {
                _attackComboCount = 1;
            }
            else if (_attackComboCount < 3)
            {
                _attackComboCount++;
            }

            // 进入普攻状态（此时朝向已同步，不会错误切入左普攻）
            EnterAttackState();
        }
    }

    private void HandleAttackCountdown()
    {
        if (_isAttacking)
        {
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= attackAnimationDuration)
            {
                ExitSingleAttackState();
            }
        }
    }

    private void HandleComboTimeout()
    {
        if (_attackComboCount > 0 && !_isAttacking)
        {
            _comboTimeoutTimer += Time.deltaTime;
            if (_comboTimeoutTimer >= comboTimeoutDuration)
            {
                ResetAttackState();
            }
        }
        else
        {
            _comboTimeoutTimer = 0f;
        }
    }

    /// <summary>
    /// 进入普攻状态（核心修复2：先同步朝向，再激活普攻状态）
    /// </summary>
    private void EnterAttackState()
    {
        // 第一步：优先同步朝向（双重保障，确保朝向绝对正确）
        ForceSyncHorizontalDir();

        // 第二步：再激活普攻状态，设置连击数
        _isAttacking = true;
        _anim.SetBool("IsAttacking", true);
        _anim.SetFloat("AttackComboCount", (float)_attackComboCount);
        _rb.velocity = Vector2.zero;
        _attackTimer = 0f;
        _comboTimeoutTimer = 0f;
    }

    /// <summary>
    /// 退出单段普攻状态（原有逻辑不变，保留朝向同步）
    /// </summary>
    private void ExitSingleAttackState()
    {
        _isAttacking = false;
        _attackTimer = 0f;

        // 退出普攻前，先强制同步朝向
        ForceSyncHorizontalDir();
        _anim.SetBool("IsAttacking", false);

        // 完成三连击，重置普攻状态
        if (_attackComboCount >= 3)
        {
            ResetAttackState();
            // 重置后再次同步朝向，确保待机状态正确
            ForceSyncHorizontalDir();
            _isMoving = (_horizontalInput != 0 || _verticalInput != 0);
            _anim.SetBool("IsMoving", _isMoving);
        }
    }

    /// <summary>
    /// 重置普攻状态（原有逻辑不变，保留朝向同步）
    /// </summary>
    private void ResetAttackState()
    {
        _isAttacking = false;
        _attackComboCount = 0;
        _comboTimeoutTimer = 0f;
        _attackTimer = 0f;

        // 重置后立即同步朝向
        ForceSyncHorizontalDir();
        _anim.SetBool("IsAttacking", false);
        _anim.SetFloat("AttackComboCount", 0f);
    }
    #endregion
}