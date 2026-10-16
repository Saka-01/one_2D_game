using UnityEngine;

/// <summary>
/// 玩家主动起跳后的上升状态。
///
/// 该状态继承 PlayerAirState，因此会自动获得：
/// 1. 空中水平移动；
/// 2. 短按和长按跳跃；
/// 3. 落地检测；
/// 4. 墙面检测。
/// </summary>
public class PlayerJumpState : PlayerAirState
{
    public PlayerJumpState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        // 先执行 PlayerAirState 和 PlayerState 的进入逻辑，
        // 包括获取 Rigidbody2D 和开启动画参数。
        base.Enter();

        // 给玩家一个初始向上的速度。
        //
        // 水平速度保持原值，
        // 因此奔跑过程中起跳不会立即失去原本的水平速度。
        rb.velocity = new Vector2(
            rb.velocity.x,
            player.jumpForce
        );
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        // 执行 PlayerAirState 的公共空中逻辑：
        // 1. 空中横移；
        // 2. 可变跳高；
        // 3. 落地检查；
        // 4. 墙壁检查。
        base.Update();

        // PlayerAirState.Update 可能已经将玩家切换到：
        // IdleState 或 WallSlideState。
        //
        // 如果当前状态已经不是这个 JumpState，
        // 就不能继续执行下面的最高点判断。
        if (!IsCurrentState)
        {
            return;
        }

        // 当纵向速度小于或等于 0 时，
        // 说明玩家已经到达最高点或开始下落。
        //
        // JumpState 主要表示主动跳跃的上升阶段，
        // AirState 表示普通下落阶段。
        if (rb.velocity.y <= 0)
        {
            stateMachine.ChangeState(player.airState);
        }
    }
}