using UnityEngine;

/// <summary>
/// 所有平台型实体的基础类。
///
/// Entity 只负责“所有实体都可能需要”的基础能力：
/// - Rigidbody2D、Collider2D 和 Animator 缓存；
/// - 水平朝向与翻转；
/// - 速度设置；
/// - 地面和墙面检测；
/// - Scene 视图中的检测 Gizmos。
///
///
/// Player 和未来的 Enemy 可以在此基础上扩展各自的专属逻辑。
/// </summary>
public class Entity : MonoBehaviour
{
    [Header("碰撞检测")]
    [Tooltip("脚底检测的起点。通常放在实体脚底中心附近。")]
    [SerializeField] protected Transform groundCheck;

    [Tooltip("脚底射线向下检测的距离。")]
    [SerializeField] protected float groundCheckDistance;

    [Tooltip("墙面检测的起点。通常放在实体身体侧面附近。")]
    [SerializeField] protected Transform wallCheck;

    [Tooltip("墙面射线沿实体朝向检测的距离。")]
    [SerializeField] protected float wallCheckDistance;

    [Tooltip("哪些 Layer 被视为实体可以站立或贴靠的地形。")]
    [SerializeField] protected LayerMask whatIsGround;

    [Tooltip("脚底检测射线数量。左、中、右至少需要 2 条。")]
    [SerializeField, Range(2, 5)] protected int groundRayCount = 3;

    [Tooltip("脚底左右射线向碰撞体内部缩进的距离。")]
    [SerializeField] protected float groundRayInset = 0.05f;

    [Tooltip("墙面检测射线数量。默认 1 条可以保持旧版行为；需要更稳定时可改成 3。")]
    [SerializeField, Range(1, 5)] protected int wallRayCount = 1;

    [Tooltip("墙面上下射线向碰撞体内部缩进的距离。")]
    [SerializeField] protected float wallRayInset = 0.05f;

    #region Components

    /// <summary>
    /// 实体使用的 Animator。
    /// 通常 Animator 位于当前物体或其子物体上。
    /// </summary>
    public Animator anim { get; private set; }

    /// <summary>
    /// 实体使用的 2D 刚体。
    /// </summary>
    public Rigidbody2D rb { get; private set; }

    /// <summary>
    /// 实体根物体上的碰撞体。
    ///
    /// 派生类通常不需要直接访问它，而是调用
    /// IsGroundDetected() 和 IsWallDetected()。
    /// </summary>
    protected Collider2D entityCollider { get; private set; }

    #endregion

    #region Facing

    /// <summary>
    /// 实体当前面朝的方向。
    /// 1 表示右，-1 表示左。
    /// </summary>
    public int facingDir { get; private set; } = 1;

    /// <summary>
    /// 是否面朝右侧。
    /// 派生类通常只需要读取 facingDir；
    /// facingRight 主要由 Entity 内部维护。
    /// </summary>
    protected bool facingRight = true;

    #endregion

    /// <summary>
    /// Unity 生命周期：初始化实体共有的物理组件。
    ///
    /// Player 等派生类重写 Awake 时，必须先调用 base.Awake()。
    /// </summary>
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        entityCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError(
                $"{GetType().Name} 缺少 Rigidbody2D，实体移动功能无法正常工作。",
                this
            );
        }

        if (entityCollider == null)
        {
            Debug.LogError(
                $"{GetType().Name} 缺少 Collider2D，地面和墙面检测将无法正常工作。",
                this
            );
        }
    }

    /// <summary>
    /// Unity 生命周期：获取当前物体或子物体上的 Animator。
    ///
    /// Player 会在自己的 Start 中调用 base.Start()，
    /// 然后再初始化第一个玩家状态，保证状态 Enter 时 Animator 已存在。
    /// </summary>
    protected virtual void Start()
    {
        anim = GetComponentInChildren<Animator>();

        if (anim == null)
        {
            Debug.LogError(
                $"{GetType().Name} 没有找到 Animator，动画状态无法正常工作。",
                this
            );
        }
    }

    /// <summary>
    /// 基类暂时没有每帧逻辑。
    /// 保留这个生命周期入口，方便未来扩展。
    /// </summary>
    protected virtual void Update()
    {
    }

    /// <summary>
    /// 清空实体速度，但不改变实体朝向。
    /// </summary>
    public void ZeroVelocity()
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = Vector2.zero;
    }

    /// <summary>
    /// 设置速度，并根据水平速度自动翻转实体。
    ///
    /// 适合玩家主动移动、攻击位移和冲刺等行为。
    /// 当 x 为 0 时，不会改变当前朝向。
    /// </summary>
    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }

    /// <summary>
    /// 设置速度，但不改变实体朝向。
    ///
    /// 适合只想修正速度的特殊逻辑，例如：
    /// - 贴墙时清除水平速度；
    /// - 墙跳锁定期间保持弹出速度；
    /// - 修改跳跃的垂直速度。
    /// </summary>
    public void SetVelocityRaw(float _xVelocity, float _yVelocity)
    {
        if (rb == null)
        {
            return;
        }

        rb.velocity = new Vector2(_xVelocity, _yVelocity);
    }

    /// <summary>
    /// 使用多条脚底射线检测地面。
    ///
    /// 只要左、中、右任意一条射线命中 whatIsGround，
    /// 就认为实体仍然站在地面上。
    /// </summary>
    public bool IsGroundDetected()
    {
        if (groundCheck == null || entityCollider == null)
        {
            return false;
        }

        Bounds bounds = entityCollider.bounds;

        // 防止 Inspector 中的缩进值大于碰撞体宽度的一半，
        // 导致左右检测范围反向。
        float halfWidth = bounds.extents.x;
        float safeInset = Mathf.Clamp(
            groundRayInset,
            0f,
            Mathf.Max(0f, halfWidth - 0.001f)
        );

        float leftX = bounds.min.x + safeInset;
        float rightX = bounds.max.x - safeInset;
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

    /// <summary>
    /// 使用墙面射线检测实体当前面朝方向是否有墙。
    ///
    /// wallRayCount 为 1 时使用 wallCheck 的 y 坐标，
    /// 可以保持原来单射线墙面检测的行为。
    /// wallRayCount 大于 1 时，会沿碰撞体高度分布多条射线。
    /// </summary>
    public bool IsWallDetected()
    {
        if (wallCheck == null)
        {
            return false;
        }

        int rayCount = Mathf.Max(1, wallRayCount);

        // 默认单射线模式：完全沿用 wallCheck 的位置。
        if (rayCount == 1 || entityCollider == null)
        {
            return Physics2D.Raycast(
                wallCheck.position,
                Vector2.right * facingDir,
                wallCheckDistance,
                whatIsGround
            );
        }

        Bounds bounds = entityCollider.bounds;
        float halfHeight = bounds.extents.y;
        float safeInset = Mathf.Clamp(
            wallRayInset,
            0f,
            Mathf.Max(0f, halfHeight - 0.001f)
        );

        float bottomY = bounds.min.y + safeInset;
        float topY = bounds.max.y - safeInset;
        float rayX = wallCheck.position.x;

        for (int i = 0; i < rayCount; i++)
        {
            float normalizedPosition = i / (float)(rayCount - 1);
            float rayY = Mathf.Lerp(bottomY, topY, normalizedPosition);
            Vector2 rayOrigin = new Vector2(rayX, rayY);

            if (Physics2D.Raycast(
                    rayOrigin,
                    Vector2.right * facingDir,
                    wallCheckDistance,
                    whatIsGround))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 绘制与运行时检测相同的地面和墙面射线。
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
        Collider2D colliderForGizmos = entityCollider;

        // Awake 在编辑器非运行状态下可能还没有执行，
        // 因此使用 GetComponent 作为回退。
        if (colliderForGizmos == null)
        {
            colliderForGizmos = GetComponent<Collider2D>();
        }

        if (groundCheck != null && colliderForGizmos != null)
        {
            Bounds bounds = colliderForGizmos.bounds;
            float halfWidth = bounds.extents.x;
            float safeInset = Mathf.Clamp(
                groundRayInset,
                0f,
                Mathf.Max(0f, halfWidth - 0.001f)
            );

            float leftX = bounds.min.x + safeInset;
            float rightX = bounds.max.x - safeInset;
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
            int rayCount = Mathf.Max(1, wallRayCount);

            if (rayCount == 1 || colliderForGizmos == null)
            {
                Vector3 rayStart = wallCheck.position;
                Vector3 rayEnd = rayStart
                    + Vector3.right * facingDir * wallCheckDistance;
                Gizmos.DrawLine(rayStart, rayEnd);
                return;
            }

            Bounds bounds = colliderForGizmos.bounds;
            float halfHeight = bounds.extents.y;
            float safeInset = Mathf.Clamp(
                wallRayInset,
                0f,
                Mathf.Max(0f, halfHeight - 0.001f)
            );

            float bottomY = bounds.min.y + safeInset;
            float topY = bounds.max.y - safeInset;
            float rayX = wallCheck.position.x;

            for (int i = 0; i < rayCount; i++)
            {
                float normalizedPosition = i / (float)(rayCount - 1);
                float rayY = Mathf.Lerp(bottomY, topY, normalizedPosition);
                Vector3 rayStart = new Vector3(rayX, rayY, 0f);
                Vector3 rayEnd = rayStart
                    + Vector3.right * facingDir * wallCheckDistance;
                Gizmos.DrawLine(rayStart, rayEnd);
            }
        }
    }

    /// <summary>
    /// 翻转实体朝向。
    /// </summary>
    public void Flip()
    {
        facingDir *= -1;
        facingRight = !facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    /// <summary>
    /// 保留旧拼写，避免已有脚本或动画事件引用失效。
    /// 新代码请使用 Flip()。
    /// </summary>
    public void Filp()
    {
        Flip();
    }

    /// <summary>
    /// 根据水平速度自动调整实体朝向。
    /// </summary>
    public void FlipController(float _xVelocity)
    {
        if (_xVelocity > 0f && !facingRight)
        {
            Flip();
        }
        else if (_xVelocity < 0f && facingRight)
        {
            Flip();
        }
    }
}
