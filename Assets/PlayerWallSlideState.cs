using UnityEngine;

/// <summary>
/// 玩家贴墙滑行状态。
///
/// 进入这个状态的前提是：
/// 1. 玩家处于空中；
/// 2. 玩家正在下落；
/// 3. 玩家面朝的方向检测到了墙壁。
///
/// 在这个状态中：
/// - 玩家会被限制在墙边；
/// - 玩家可以按 K 进行蹬墙跳；
/// - 玩家可以按反方向离开墙壁；
/// - 如果墙壁消失，也会回到 AirState。
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

        // 进入贴墙状态时，立即清除水平速度。
        //
        // 这样可以防止玩家继续向墙壁内部移动，
        // 也可以避免保留之前的空中水平速度。
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        // 读取输入、更新计时器和 Animator 的 yVelocity。
        base.Update();

        // 为了安全起见，如果父类逻辑未来发生了状态切换，
        // 这里立即停止旧状态的后续代码。
        if (!IsCurrentState)
        {
            return;
        }

        // 如果脚下已经接触地面，
        // 就不应该继续保持 WallSlide。
        if (player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 如果墙壁已经消失，说明玩家已经离开墙面。
        //
        // 这是原脚本缺少的重要检查。
        // 没有它，角色离开墙后仍可能停留在 WallSlide，
        // 甚至可以在没有墙的地方继续按 K 蹬墙。
        if (!player.IsWallDetected())
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        // 按下 K 时进行蹬墙跳。
        //
        // 这里要在“反向输入判断”之前处理，
        // 这样玩家可以同时按方向键和 K，
        // 仍然能够优先完成蹬墙。
        if (Input.GetKeyDown(KeyCode.K))
        {
            stateMachine.ChangeState(player.wallJump);
            return;
        }

        // 如果玩家正在输入远离墙壁的方向，
        // 就离开 WallSlide，进入普通空中状态。
        //
        // 例如：
        // facingDir = 1，说明玩家面向右侧墙壁；
        // xInput = -1，说明玩家按左键远离墙壁。
        //
        // 这里不能进入 Idle，因为玩家仍然处于空中。
        if (xInput != 0 && Mathf.Sign(xInput) != player.facingDir)
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        // 贴墙时不允许保留水平速度。
        // Collider 会阻挡玩家，但这里主动设为 0
        // 能让贴墙状态更加稳定。
        float verticalVelocity;

        if (yInput < 0)
        {
            // 按下方向键时允许更快下落。
            verticalVelocity = rb.velocity.y;
        }
        else
        {
            // 没有按下方向键时限制最大下滑速度。
            //
            // Mathf.Max 的效果：
            // - 如果当前速度还没有超过最大下滑速度，就保持当前值；
            // - 如果下落太快，就限制到 -wallSlideSpeed。
            verticalVelocity = Mathf.Max(
                rb.velocity.y,
                -player.wallSlideSpeed
            );
        }

        rb.velocity = new Vector2(0, verticalVelocity);
    }
}