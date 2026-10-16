using UnityEngine;

/// <summary>
/// 玩家蹬墙跳状态。
///
/// 进入该状态时，玩家会：
/// 1. 朝墙壁相反方向弹出；
/// 2. 获得垂直向上的速度；
/// 3. 在短时间内保持弹出速度；
/// 4. 然后进入普通 AirState。
///
/// 进入 AirState 后，玩家可以控制方向回到原来的墙壁，
/// 重新进入 WallSlide，再次进行蹬墙跳。
/// </summary>
public class PlayerWallJumpState : PlayerState
{
    // 蹬墙跳开始时的水平速度。
    //
    // 使用字段保存，而不是每帧重新根据 facingDir 计算，
    // 避免角色翻面后方向发生错误。
    private float wallJumpVelocityX;

    public PlayerWallJumpState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 使用 Inspector 中设置的控制锁定时间。
        //
        // 例如 0.12 秒：
        // - 这段时间内保持向墙外弹出；
        // - 避免玩家立刻按回墙方向而被墙吸住。
        stateTimer = player.wallJumpControlLockDuration;

        // 记录蹬墙的水平方向。
        //
        // 假设：
        // facingDir = 1，玩家面向右墙；
        // 那么墙外方向就是 -1，也就是向左。
        wallJumpVelocityX = -player.facingDir * player.wallJumpHorizontalForce;

        // 设置蹬墙初始速度。
        //
        // Player.SetVelocity 会根据水平速度自动翻转玩家朝向，
        // 让角色面向跳出去的方向。
        player.SetVelocity(
            wallJumpVelocityX,
            player.wallJumpVerticalForce
        );
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        // 读取输入并递减 stateTimer。
        base.Update();

        if (!IsCurrentState)
        {
            return;
        }

        // 如果在蹬墙过程中落地，
        // 直接回到 Idle。
        if (rb.velocity.y <= 0 && player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 在控制锁定时间内，持续保持向墙外的水平速度。
        //
        // 不能调用 SetVelocity，
        // 因为 SetVelocity 会根据速度方向重新处理角色翻面。
        // 这里直接设置 Rigidbody2D 的速度更安全。
        if (stateTimer > 0)
        {
            rb.velocity = new Vector2(
                wallJumpVelocityX,
                rb.velocity.y
            );

            return;
        }

        // 控制锁定结束后进入普通空中状态。
        //
        // 下一帧 AirState 会根据玩家输入控制方向：
        // - 按墙的方向：回到墙边；
        // - 不按方向：继续保持空中；
        // - 按远离墙的方向：继续向外移动。
        stateMachine.ChangeState(player.airState);
    }
}
