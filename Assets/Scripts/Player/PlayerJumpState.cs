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
        // 先执行父类进入逻辑，获取 Rigidbody2D 并开启 Jump 动画。
        base.Enter();

        // 给玩家一次向上的初始速度。
        //
        // 保留当前水平速度，保证玩家可以在奔跑过程中起跳。
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
        // 执行 PlayerAirState 的公共逻辑：
        // 1. 空中水平移动；
        // 2. 短按低跳、长按高跳；
        // 3. 落地检测；
        // 4. 墙面检测。
        base.Update();

        // PlayerAirState.Update 可能已经将玩家切换到
        // IdleState 或 WallSlideState。
        // 如果当前状态已经不是这个 JumpState，
        // 就不能继续执行下面的逻辑。
        if (!IsCurrentState)
        {
            return;
        }

        // 当纵向速度小于或等于 0 时，
        // 说明玩家已经到达最高点或开始下落。
        // 此时从 JumpState 进入普通 AirState。
        if (rb.velocity.y <= 0)
        {
            stateMachine.ChangeState(player.airState);
        }
    }
}
