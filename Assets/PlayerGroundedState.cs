using UnityEngine;

/// <summary>
/// 玩家处于地面时的公共状态。
///
/// IdleState 和 MoveState 都继承这个状态，
/// 因此地面检测和跳跃输入只需要在这里处理一次。
/// </summary>
public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(
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
        // 读取输入、更新计时器和 Animator 参数。
        base.Update();

        // 如果当前状态已经在父类或其他逻辑中被切换，
        // 就不要继续执行原状态的地面逻辑。
        if (!IsCurrentState)
        {
            return;
        }

        // 玩家离开地面后进入普通空中状态。
        if (!player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.airState);
            return;
        }

        // 跳跃预输入在地面上生效。
        if (player.HasJumpBuffered)
        {
            // 一个预输入只能触发一次跳跃。
            player.ConsumeJumpBuffer();

            // 清除土狼时间，避免起跳后短时间内再次触发空中跳跃。
            player.ConsumeCoyoteTime();

            stateMachine.ChangeState(player.jumpState);
            return;
        }
    }
}
