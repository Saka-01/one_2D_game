using UnityEngine;

/// <summary>
/// 玩家的公共空中状态。
///
/// PlayerJumpState 继承这个状态，因此跳跃上升阶段
/// 和普通下落阶段都可以复用空中移动、可变跳跃、
/// 落地检测和墙面检测逻辑。
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
        // 读取输入、更新计时器和 Animator 的 yVelocity。
        base.Update();

        // 空中水平移动。
        //
        // 由于 PlayerJumpState 继承 PlayerAirState，
        // 所以角色在跳跃上升阶段也可以控制左右方向。
        player.SetVelocity(
            xInput * player.moveSpeed,
            rb.velocity.y
        );

        // 处理短按低跳、长按高跳。
        HandleVariableJump();

        // 只有角色正在下落时才检测落地。
        // 这样可以避免刚起跳时脚底检测仍接触平台，
        // 导致 JumpState 立即被切回 IdleState。
        if (rb.velocity.y <= 0 && player.IsGroundDetected())
        {
            // 如果玩家在落地前按了 K，
            // 就在落地的这一刻直接开始下一次跳跃。
            if (player.HasJumpBuffered)
            {
                player.ConsumeJumpBuffer();
                stateMachine.ChangeState(player.jumpState);
            }
            else
            {
                stateMachine.ChangeState(player.idleState);
            }

            return;
        }

        // 只有下落时接触墙壁，才进入贴墙状态。
        // 上升时擦到墙壁不会打断主动跳跃。
        if (rb.velocity.y <= 0 && player.IsWallDetected())
        {
            stateMachine.ChangeState(player.wallSlide);
            return;
        }
    }

    /// <summary>
    /// 实现“短按低跳、长按高跳”。
    ///
    /// 起跳时始终给予完整的 jumpForce。
    /// 如果玩家在上升阶段提前松开 K，
    /// 就削弱当前剩余的向上速度。
    /// </summary>
    protected void HandleVariableJump()
    {
        // GetKeyUp 只在 K 被松开的这一帧返回 true。
        if (!Input.GetKeyUp(KeyCode.K))
        {
            return;
        }

        // 只有仍在上升时，提前松开才会影响跳跃高度。
        if (rb.velocity.y <= 0)
        {
            return;
        }

        // 将剩余向上速度乘以一个小于 1 的系数。
        rb.velocity = new Vector2(
            rb.velocity.x,
            rb.velocity.y * player.jumpCutMultiplier
        );
    }
}
