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

            // 同一帧同时按下攻击和跳跃时，跳跃优先，
            // 丢弃攻击请求，避免落地后意外追加一次攻击。
            player.ConsumeAttackBuffer();

            stateMachine.ChangeState(player.jumpState);
            return;
        }

        // 当前帧按下 J，或者攻击请求仍在有效缓冲时间内，
        // 都可以进入攻击状态。
        //
        // 第一次进入攻击时消费的是当前这次 J；
        // 攻击过程中再次按 J，则会由 PlayerPrimaryAttackState
        // 在当前攻击结束时消费，形成连击。
        if (player.HasAttackBuffered)
        {
            player.ConsumeAttackBuffer();
            stateMachine.ChangeState(player.primaryAttack);
            return;
        }
    }
}
