using UnityEngine;

/// <summary>
/// 所有玩家状态的基础类。
///
/// Idle、Move、Jump、Air、Dash、WallSlide 和 WallJump
/// 都会直接或间接继承这个类。
///
/// 这里保存所有状态都会使用的公共数据和公共行为。
/// </summary>
public class PlayerState
{
    // 管理当前状态以及状态切换。
    protected PlayerStateMachine stateMachine;

    // 玩家主体对象。
    // 通过它访问移动参数、检测方法、Animator 和 Rigidbody2D。
    protected Player player;

    // 玩家使用的 2D 刚体。
    protected Rigidbody2D rb;

    // 当前帧的水平输入。
    // 通常为 -1、0 或 1。
    protected float xInput;

    // 当前帧的垂直输入。
    // 通常为 -1、0 或 1。
    protected float yInput;

    // 该状态对应的 Animator Bool 参数名称。
    //
    // 例如：
    // IdleState 对应 "Idle"
    // MoveState 对应 "Move"
    // JumpState 对应 "Jump"
    private readonly string animBoolName;

    // 通用状态计时器。
    //
    // 当前主要由 DashState 和 WallJumpState 使用。
    // 每帧会减少 Time.deltaTime。
    protected float stateTimer;

    /// <summary>
    /// 当前对象是否仍然是状态机正在运行的状态。
    ///
    /// 这个属性后续非常重要：
    /// 父类 Update 中可能已经发生状态切换，
    /// 子类在继续执行之前需要检查当前状态是否还是自己。
    /// </summary>
    protected bool IsCurrentState
    {
        get
        {
            return stateMachine.currentState == this;
        }
    }

    /// <summary>
    /// 创建一个玩家状态。
    /// </summary>
    /// <param name="_player">玩家主体</param>
    /// <param name="_stateMachine">玩家状态机</param>
    /// <param name="_animBoolName">该状态对应的动画 Bool 名称</param>
    public PlayerState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
    {
        player = _player;
        stateMachine = _stateMachine;
        animBoolName = _animBoolName;
    }

    /// <summary>
    /// 每次进入该状态时调用一次。
    /// </summary>
    public virtual void Enter()
    {
        // 获取玩家刚体。
        //
        // Player.Awake 中会先取得 Rigidbody2D，
        // 然后才创建各状态，因此这里可以安全获取。
        rb = player.rb;

        // 开启当前状态对应的动画参数。
        //
        // 例如进入 MoveState：
        // Animator 中的 Move Bool 会变为 true。
        player.anim.SetBool(animBoolName, true);
    }

    /// <summary>
    /// 只要该状态仍是当前状态，每帧调用一次。
    /// </summary>
    public virtual void Update()
    {
        // 通用计时器每帧递减。
        stateTimer -= Time.deltaTime;

        // 读取水平和垂直输入。
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        // 将当前垂直速度传递给 Animator。
        //
        // 后面可以用 yVelocity 区分：
        // yVelocity > 0：角色正在上升；
        // yVelocity < 0：角色正在下落。
        player.anim.SetFloat("yVelocity", rb.velocity.y);
    }

    /// <summary>
    /// 每次离开该状态时调用一次。
    /// </summary>
    public virtual void Exit()
    {
        // 关闭当前状态对应的动画参数。
        player.anim.SetBool(animBoolName, false);
    }
}