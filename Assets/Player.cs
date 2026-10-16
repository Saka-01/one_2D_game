using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("移动")]
    public float moveSpeed = 12.0f;
    public float jumpForce;

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

    [Header("碰撞检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private LayerMask whatIsGround;

    [Tooltip("脚底检测射线数量。建议使用 3 条：左、中、右。")]
    [SerializeField, Range(2, 5)] private int groundRayCount = 3;

    [Tooltip("脚底左右两侧向碰撞体内部缩进的距离。")]
    [SerializeField] private float groundRayInset = 0.05f;

    [Header("跳跃")]
    [Tooltip("松开跳跃键时保留的向上速度比例。")]
    [Range(0.1f, 1f)]
    public float jumpCutMultiplier = 0.45f;

    [Tooltip("跳跃预输入的有效时间。单位：秒。")]
    [Min(0f)]
    public float jumpBufferDuration = 0.12f;

    // 当前还剩多少预输入时间。
    private float jumpBufferTimer;

    public int facingDir { get; private set; } = 1;
    private bool facingRight = true;

    #region Components

    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }

    // 玩家根物体上的碰撞体。
    // 通过它的 bounds 获取角色实际宽度，
    // 让脚底检测不依赖固定的 Transform 位置。
    private Collider2D playerCollider;

    #endregion

    #region States

    public PlayerStateMachine stateMachine { get; private set; }

    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerWallSlideState wallSlide { get; private set; }
    public PlayerDashState dashState { get; private set; }
    public PlayerWallJumpState wallJump { get; private set; }

    #endregion

    private void Awake()
    {
        stateMachine = new PlayerStateMachine();
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();

        if (playerCollider == null)
        {
            Debug.LogError("Player 缺少 Collider2D，无法进行脚底检测。", this);
        }

        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        moveState = new PlayerMoveState(this, stateMachine, "Move");
        jumpState = new PlayerJumpState(this, stateMachine, "Jump");
        airState = new PlayerAirState(this, stateMachine, "Jump");
        dashState = new PlayerDashState(this, stateMachine, "Dash");
        wallSlide = new PlayerWallSlideState(this, stateMachine, "WallSlide");
        wallJump = new PlayerWallJumpState(this, stateMachine, "Jump");
    }

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        stateMachine.Initialize(idleState);
    }

    private void Update()
    {
        // 必须在当前状态 Update 之前记录预输入。
        // 这样 GroundedState、AirState 和 WallSlideState
        // 在同一帧都可以读取到这个跳跃请求。
        UpdateJumpBuffer();

        stateMachine.currentState.Update();
        CheckForDashInput();
    }

    /// <summary>
    /// 更新跳跃预输入计时器。
    ///
    /// 玩家按下 K 后，保存一个短暂的跳跃请求。
    /// 即使按下时玩家还没有落地，之后落地时仍然可以自动跳跃。
    /// </summary>
    private void UpdateJumpBuffer()
    {
        // 每帧先减少剩余时间。
        jumpBufferTimer -= Time.deltaTime;

        // 当前帧按下 K，就重新开始计时。
        if (Input.GetKeyDown(KeyCode.K))
        {
            jumpBufferTimer = jumpBufferDuration;
        }
    }

    /// <summary>
    /// 当前是否存在有效的跳跃预输入。
    /// </summary>
    public bool HasJumpBuffered => jumpBufferTimer > 0f;

    /// <summary>
    /// 消费掉一次跳跃预输入。
    ///
    /// 一次跳跃只能消费一次，避免同一个按键请求
    /// 同时触发普通跳和其他跳跃。
    /// </summary>
    public void ConsumeJumpBuffer()
    {
        jumpBufferTimer = 0f;
    }

    private void CheckForDashInput()
    {
        dashUsageTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.L) && dashUsageTimer <= 0)
        {
            dashUsageTimer = dashCooldown;
            dashDir = Input.GetAxisRaw("Horizontal");

            if (dashDir == 0)
            {
                dashDir = facingDir;
            }

            stateMachine.ChangeState(dashState);
        }
    }

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }

    /// <summary>
    /// 使用多条脚底射线检测地面。
    ///
    /// 只要左、中、右任意一条射线碰到地面，
    /// 就认为玩家仍然站在地面上。
    ///
    /// 这样角色即使有一部分身体伸出平台边缘，
    /// 只要仍有脚底区域接触平台，就不会误判为空中。
    /// </summary>
    public bool IsGroundDetected()
    {
        if (groundCheck == null)
        {
            return false;
        }

        // 如果碰撞体丢失，使用原来的单射线作为保护性回退。
        if (playerCollider == null)
        {
            return Physics2D.Raycast(
                groundCheck.position,
                Vector2.down,
                groundCheckDistance,
                whatIsGround
            );
        }

        Bounds bounds = playerCollider.bounds;

        // 左右检测点位于碰撞体底部附近，
        // 并向内缩进一点，避免刚好贴着边界时产生不稳定结果。
        float leftX = bounds.min.x + groundRayInset;
        float rightX = bounds.max.x - groundRayInset;
        float rayY = groundCheck.position.y;

        int rayCount = Mathf.Max(2, groundRayCount);

        for (int i = 0; i < rayCount; i++)
        {
            float normalizedPosition = i / (float)(rayCount - 1);
            float rayX = Mathf.Lerp(leftX, rightX, normalizedPosition);

            Vector2 rayOrigin = new Vector2(rayX, rayY);

            if (Physics2D.Raycast(
                    rayOrigin,
                    Vector2.down,
                    groundCheckDistance,
                    whatIsGround))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsWallDetected()
    {
        if (wallCheck == null)
        {
            return false;
        }

        return Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * facingDir,
            wallCheckDistance,
            whatIsGround
        );
    }

    private void OnDrawGizmos()
    {
        // 编辑器模式下 Awake 可能还没有执行，
        // 所以这里不能只依赖 playerCollider 字段。
        Collider2D colliderForGizmos = playerCollider;

        if (colliderForGizmos == null)
        {
            colliderForGizmos = GetComponent<Collider2D>();
        }

        if (groundCheck != null && colliderForGizmos != null)
        {
            Bounds bounds = colliderForGizmos.bounds;
            float leftX = bounds.min.x + groundRayInset;
            float rightX = bounds.max.x - groundRayInset;
            float rayY = groundCheck.position.y;
            int rayCount = Mathf.Max(2, groundRayCount);

            Gizmos.color = Color.green;

            for (int i = 0; i < rayCount; i++)
            {
                float normalizedPosition = i / (float)(rayCount - 1);
                float rayX = Mathf.Lerp(leftX, rightX, normalizedPosition);
                Vector3 rayStart = new Vector3(rayX, rayY, 0f);
                Vector3 rayEnd = rayStart + Vector3.down * groundCheckDistance;

                Gizmos.DrawLine(rayStart, rayEnd);
            }
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.red;

            Vector3 wallEnd = wallCheck.position
                + Vector3.right * facingDir * wallCheckDistance;

            Gizmos.DrawLine(wallCheck.position, wallEnd);
        }
    }

    public void Filp()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        transform.Rotate(0, 180, 0);
    }

    public void FlipController(float _x)
    {
        if (_x > 0 && !facingRight)
        {
            Filp();
        }
        else if (_x < 0 && facingRight)
        {
            Filp();
        }
    }
}
