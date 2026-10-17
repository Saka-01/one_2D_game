using UnityEngine;

/// <summary>
/// 玩家站立状态。
/// </summary>
public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 进入 Idle 时只清除水平速度。
        //
        // 不直接把垂直速度设为 0，
        // 避免以后在斜坡、移动平台或特殊落地场景中产生问题。
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    public override void Update()
    {
        // 先执行 GroundedState 的公共逻辑：
        // 读取输入、检查离地和检查跳跃。
        base.Update();

        // GroundedState 有可能已经把玩家切换到了
        // AirState 或 JumpState。
        //
        // 如果当前状态已经不是这个 IdleState，
        // 就不能继续执行下面的 Idle 逻辑。
        if (!IsCurrentState)
        {
            return;
        }

        // 只要玩家有水平输入，就进入 MoveState。
        //
        // 即使玩家面前有墙，也依然进入 MoveState。
        // 实际位移会由墙壁的 Collider2D 阻挡，
        // 但玩家仍然播放奔跑动画。
        if (xInput != 0)
        {
            stateMachine.ChangeState(player.moveState);
        }
    }
}