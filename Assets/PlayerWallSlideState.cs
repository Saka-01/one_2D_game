using UnityEngine;

/// <summary>
/// 玩家贴墙滑行状态。
///
/// 玩家下落并面向墙壁时进入这个状态。
/// 该状态负责限制下滑速度、离墙检测和蹬墙跳输入。
/// </summary>
public class PlayerWallSlideState : PlayerState
{
    public PlayerWallSlideState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 进入贴墙状态时清除水平速度，
        // 避免保留进入墙面前的空中移动速度。
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        // 读取输入、更新计时器和 Animator 参数。
        base.Update();

        if (!IsCurrentState)
        {
            return;
        }

        // 接触地面后结束贴墙状态。
        if (player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 墙壁消失或玩家离开墙面后，回到普通空中状态。
        if (!player.IsWallDetected())
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        // 使用跳跃预输入，而不是只检查当前帧是否按下 K。
        //
        // 玩家可以在即将接触墙面前提前按 K；
        // 如果进入 WallSlide 时请求仍在有效时间内，
        // 就会立刻执行蹬墙跳。
        if (player.HasJumpBuffered)
        {
            player.ConsumeJumpBuffer();
            stateMachine.ChangeState(player.wallJump);
            return;
        }

        // 输入方向与朝向相反，表示玩家主动远离墙壁。
        // 此时玩家仍在空中，所以进入 Air 而不是 Idle。
        if (xInput != 0 && Mathf.Sign(xInput) != player.facingDir)
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        float verticalVelocity;

        if (yInput < 0)
        {
            // 按住下方向时允许正常快速下落。
            verticalVelocity = rb.velocity.y;
        }
        else
        {
            // 否则把下落速度限制在 wallSlideSpeed 以内。
            verticalVelocity = Mathf.Max(
                rb.velocity.y,
                -player.wallSlideSpeed
            );
        }

        // 贴墙期间水平速度保持为 0。
        rb.velocity = new Vector2(0, verticalVelocity);
    }
}
