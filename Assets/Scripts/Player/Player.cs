using UnityEngine;

/// <summary>
/// 玩家专属控制器。
///
/// Player 继承 Entity，直接复用 Entity 提供的：
/// - Rigidbody2D / Animator / Collider2D 缓存；
/// - SetVelocity、SetVelocityRaw、ZeroVelocity；
/// - 朝向翻转；
/// - 地面和墙面检测。
///
/// Player 自己只负责玩家专属内容：
/// - 输入；
/// - 移动、跳跃和冲刺；
/// - 攻击和连击；
/// - 跳跃/攻击预输入；
/// - PlayerStateMachine 和玩家状态实例。
/// </summary>
public class Player : Entity
{
    [Header("移动")]
    public float moveSpeed = 12.0f;
    public float jumpForce;

    [Header("攻击位移")]
    public Vector2[] attackMovement;

    [Header("攻击输入")]
    [Tooltip("攻击输入预输入的有效时间。单位：秒。")]
    [Min(0f)]
    public float attackBufferDuration = 0.12f;

    [Header("墙面移动")]
    [Tooltip("贴墙时允许的最大下滑速度。")]
    public float wallSlideSpeed = 5f;

    [Tooltip("蹬墙跳时的水平弹出力度。")]
    public float wallJumpHorizontalForce = 10f;

    [Tooltip("蹬墙跳时的垂直弹出力度。")]
    public float wallJumpVerticalForce = 12f;

    [Tooltip("蹬墙后暂时锁定水平控制的时间。")]
    public float wallJumpControlLockDuration = 0.12f;

    [Header("冲刺")]
    [SerializeField] private float dashCooldown;
    [SerializeField] private float dashUsageTimer;
    public float dashSpeed;
    public float dashDuration;
    public float dashDir { get; private set; }

    [Header("跳跃")]
    [Tooltip("松开跳跃键时保留的向上速度比例。")]
    [Range(0.1f, 1f)]
    public float jumpCutMultiplier = 0.45f;

    [Tooltip("跳跃预输入的有效时间。单位：秒。")]
    [Min(0f)]
    public float jumpBufferDuration = 0.12f;

    [Tooltip("离开地面后仍然允许起跳的时间。单位：秒。")]
    [Min(0f)]
    public float coyoteDuration = 0.10f;

    // 当前还剩多少跳跃预输入时间。
    private float jumpBufferTimer;

    // 当前还剩多少攻击预输入时间。
    private float attackBufferTimer;

    // 当前还剩多少土狼时间。
    private float coyoteTimer;

    #region States

    public PlayerStateMachine stateMachine { get; private set; }

    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerWallSlideState wallSlide { get; private set; }
    public PlayerDashState dashState { get; private set; }
    public PlayerWallJumpState wallJump { get; private set; }
    public PlayerPrimaryAttackState primaryAttack { get; private set; }

    #endregion

    /// <summary>
    /// 初始化 Entity 的基础组件后，创建玩家状态机和所有玩家状态。
    /// </summary>
    protected override void Awake()
    {
        // 必须先调用 Entity.Awake，
        // 让 rb、Collider2D 等公共组件先完成初始化。
        base.Awake();

        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        wallSlide = new PlayerWallSlideState(this, stateMachine, "WallSlide");
        wallJump = new PlayerWallJumpState(this, stateMachine, "Jump");
        primaryAttack = new PlayerPrimaryAttackState(this, stateMachine, "Attack");
    }

    /// <summary>
    /// Entity.Start 会先获取 Animator，
    /// 然后才能安全地进入 IdleState 并设置动画参数。
    /// </summary>
    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    /// <summary>
    /// 先执行 Entity 的公共更新入口，
    /// 再处理玩家输入缓冲、土狼时间、当前状态和冲刺。
    /// </summary>
    protected override void Update()
    {
        base.Update();

        // 必须在当前状态 Update 之前记录输入和土狼时间。
        UpdateJumpBuffer();
        UpdateAttackBuffer();
        UpdateCoyoteTime();

        stateMachine.currentState.Update();
        CheckForDashInput();
    }

    /// <summary>
    /// 由攻击动画末尾的 Animation Event 调用。
    ///
    /// 只有当前状态确实是攻击状态时，
    /// 才允许把动画结束信号交给状态机。
    /// </summary>
    public void AnimationTrigger()
    {
        if (stateMachine.currentState == primaryAttack)
        {
            stateMachine.currentState.AnimationFinishTrigger();
        }
    }

    /// <summary>
    /// 更新跳跃预输入计时器。
    /// </summary>
    private void UpdateJumpBuffer()
    {
        jumpBufferTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.K))
        {
            jumpBufferTimer = jumpBufferDuration;
        }
    }

    /// <summary>
    /// 更新攻击预输入计时器。
    /// </summary>
    private void UpdateAttackBuffer()
    {
        attackBufferTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.J))
        {
            attackBufferTimer = attackBufferDuration;
        }
    }

    /// <summary>
    /// 当前是否存在有效的攻击预输入。
    /// </summary>
    public bool HasAttackBuffered => attackBufferTimer > 0f;

    /// <summary>
    /// 消费一次攻击预输入。
    /// </summary>
    public void ConsumeAttackBuffer()
    {
        attackBufferTimer = 0f;
    }

    /// <summary>
    /// 更新土狼时间。
    ///
    /// 只要玩家接触地面，就刷新土狼计时器；
    /// 离开地面后，计时器开始倒计时。
    /// </summary>
    private void UpdateCoyoteTime()
    {
        if (IsGroundDetected())
        {
            coyoteTimer = coyoteDuration;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 当前是否存在有效的跳跃预输入。
    /// </summary>
    public bool HasJumpBuffered => jumpBufferTimer > 0f;

    /// <summary>
    /// 当前是否仍处于土狼时间内。
    /// </summary>
    public bool HasCoyoteTime => coyoteTimer > 0f;

    /// <summary>
    /// 消费一次跳跃预输入。
    /// </summary>
    public void ConsumeJumpBuffer()
    {
        jumpBufferTimer = 0f;
    }

    /// <summary>
    /// 消费一次土狼时间，防止一次起跳被重复识别。
    /// </summary>
    public void ConsumeCoyoteTime()
    {
        coyoteTimer = 0f;
    }

    /// <summary>
    /// 处理玩家的冲刺输入。
    /// 冲刺方向为当前输入方向；没有输入时使用当前朝向。
    /// </summary>
    private void CheckForDashInput()
    {
        dashUsageTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.L) && dashUsageTimer <= 0f)
        {
            dashUsageTimer = dashCooldown;
            dashDir = Input.GetAxisRaw("Horizontal");

            if (dashDir == 0f)
            {
                dashDir = facingDir;
            }

            stateMachine.ChangeState(dashState);
        }
    }
}
