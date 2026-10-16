using UnityEngine;

/// <summary>
/// 玩家的公共空中状态。
///
/// 这个状态负责所有空中行为都会使用的逻辑：
/// 1. 空中水平移动；
/// 2. 短按和长按跳跃；
/// 3. 落地检测；
/// 4. 墙面检测。
///
/// PlayerJumpState 继承自这个类，
/// 因此跳跃上升阶段也可以使用这些功能。
/// </summary>
public class PlayerAirState : PlayerState
{
    public PlayerAirState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        // PlayerState.Update 负责：
        // 1. 读取 xInput 和 yInput；
        // 2. 更新 stateTimer；
        // 3. 更新 Animator 的 yVelocity。
        base.Update();

        // 空中水平移动。
        //
        // 原来 PlayerJumpState 没有执行这段逻辑，
        // 因此角色在跳跃上升时不能改变方向。
        //
        // 现在 JumpState 继承 AirState，
        // 上升和下落阶段都会执行空中水平控制。
        player.SetVelocity(
            xInput * player.moveSpeed,
            rb.velocity.y
        );

        // 检查玩家是否提前松开跳跃键。
        //
        // 提前松开：降低当前向上速度，形成短跳。
        // 继续按住：保留完整速度，形成高跳。
        HandleVariableJump();

        // 只有角色开始下落时才进行落地检测。
        //
        // 这里不能只检查 IsGroundDetected()，
        // 因为角色刚刚起跳时，脚下的射线可能仍然接触地面。
        // 如果不检查纵向速度，JumpState 可能刚进入就被切回 Idle。
        if (rb.velocity.y <= 0 && player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);

            // 切换状态后立即结束旧状态的 Update。
            // 避免后面的墙壁检测再次切换状态。
            return;
        }

        // 第一版只允许玩家在下落阶段进入滑墙状态。
        //
        // 这样角色向上跳并擦到墙壁时，
        // 不会立刻中断上升并进入 WallSlide。
        if (rb.velocity.y <= 0 && player.IsWallDetected())
        {
            stateMachine.ChangeState(player.wallSlide);
            return;
        }

        // 只有角色开始下落时才进行落地检测。
        //
        // 这里不能只检查 IsGroundDetected()，
        // 因为角色刚刚起跳时，脚下的射线可能仍然接触地面。
        // 如果不检查纵向速度，JumpState 可能刚进入就被切回 Idle。
        if (rb.velocity.y <= 0 && player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);

            // 切换状态后立即结束旧状态的 Update。
            // 避免后面的墙壁检测再次切换状态。
            return;
        }

        // 第一版只允许玩家在下落阶段进入滑墙状态。
        //
        // 这样角色向上跳并擦到墙壁时，
        // 不会立刻中断上升并进入 WallSlide。
        if (rb.velocity.y <= 0 && player.IsWallDetected())
        {
            stateMachine.ChangeState(player.wallSlide);
            return;
        }
    }

    /// <summary>
    /// 实现“短按低跳、长按高跳”。
    ///
    /// 工作方式：
    /// 起跳时始终给予完整的 jumpForce。
    /// 如果玩家在上升阶段提前松开 K，
    /// 就削弱当前剩余的向上速度。
    /// </summary>
    protected void HandleVariableJump()
    {
        // GetKeyUp 只在 K 键被松开的这一帧返回 true。
        //
        // 如果玩家仍然按着 K，
        // 说明玩家想进行完整高度的跳跃，不执行截断。
        if (!Input.GetKeyUp(KeyCode.K))
        {
            return;
        }

        // 只有角色仍在上升时，松开按键才有意义。
        //
        // velocity.y > 0：正在上升；
        // velocity.y = 0：接近最高点；
        // velocity.y < 0：正在下落。
        if (rb.velocity.y <= 0)
        {
            return;
        }

        // 将当前向上速度乘以一个小于 1 的系数。
        //
        // 假设当前向上速度是 10：
        // jumpCutMultiplier = 0.45
        // 处理后速度变为 4.5。
        //
        // 角色仍会继续上升一点，但会更早到达最高点。
        rb.velocity = new Vector2(
            rb.velocity.x,
            rb.velocity.y * player.jumpCutMultiplier
        );
    }
}