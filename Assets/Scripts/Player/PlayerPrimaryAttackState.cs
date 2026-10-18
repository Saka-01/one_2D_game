using UnityEngine;

/// <summary>
/// 玩家基础近战攻击状态。
///
/// 这个状态本身只负责：
/// 1. 播放当前连击段的攻击动画；
/// 2. 应用当前攻击段的攻击位移；
/// 3. 等待动画事件通知攻击结束；
/// 4. 维护三段连击的索引；
/// 5. 在攻击结束后回到地面状态。
///
/// 攻击判定 Hitbox 暂时不放在这里处理，
/// 后续会通过动画事件单独启用和关闭。
/// </summary>
public class PlayerPrimaryAttackState : PlayerState
{
    // 当前连击段索引：0、1、2。
    private int comboCounter;

    // 上一次完整结束的攻击时间。
    // 超过连击窗口后，下一次攻击从第一段开始。
    private float lastTimeAttacked;

    // 两段攻击之间允许继续连击的时间。
    private const float ComboWindow = 1f;

    public PlayerPrimaryAttackState(
        Player _player,
        PlayerStateMachine _stateMachine,
        string _animBoolName)
        : base(_player, _stateMachine, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // attackMovement 数组需要和动画连击段一一对应。
        // 没有配置时仍然允许播放动画，但不应用攻击位移，
        // 避免因为数组为空导致 IndexOutOfRangeException。
        if (player.attackMovement == null || player.attackMovement.Length == 0)
        {
            Debug.LogError(
                "Player 的 attackMovement 没有配置，攻击位移将使用 Vector2.zero。",
                player
            );
        }

        // 如果距离上一次完整攻击的时间超过连击窗口，
        // 或者当前索引超过已配置的攻击段数量，
        // 就从第一段攻击开始。
        if (Time.time >= lastTimeAttacked + ComboWindow)
        {
            comboCounter = 0;
        }

        if (player.attackMovement != null &&
            player.attackMovement.Length > 0 &&
            comboCounter >= player.attackMovement.Length)
        {
            comboCounter = 0;
        }

        // 将当前攻击段索引传给 Animator。
        // Animator 应该根据 ComboCounter 播放 Attack_1、Attack_2、Attack_3。
        player.anim.SetInteger("ComboCounter", comboCounter);

        // 应用当前攻击段的位移。
        if (player.attackMovement != null && player.attackMovement.Length > 0)
        {
            Vector2 movement = player.attackMovement[comboCounter];

            player.SetVelocity(
                movement.x * player.facingDir,
                movement.y
            );
        }
        else
        {
            player.SetZeroVelocity();
        }

        // 攻击开始后一小段时间后清除攻击位移。
        // 具体数值可以根据动画的前摇和打击帧调整。
        stateTimer = 0.1f;
    }

    public override void Exit()
    {
        base.Exit();

        // 只有动画正常播放到结尾并触发 Animation Event，
        // 才推进连击索引。
        //
        // 如果攻击过程中被冲刺或其他状态打断，
        // 不推进 comboCounter，避免下一次攻击跳段。
        if (triggerCalled)
        {
            comboCounter++;
            lastTimeAttacked = Time.time;
        }
    }

    public override void Update()
    {
        // 读取输入、更新计时器和 Animator 参数。
        base.Update();

        // 如果攻击状态已经被其他输入切走，
        // 不再执行旧攻击状态的逻辑。
        if (!IsCurrentState)
        {
            return;
        }

        // 攻击前摇/位移阶段结束后停止水平位移。
        // 只在攻击动画尚未结束时处理，
        // 避免覆盖后续状态的速度。
        if (stateTimer < 0 && !triggerCalled)
        {
            player.SetZeroVelocity();
        }

        // Animation Event 会通过 Player.AnimationTrigger() 设置 triggerCalled。
        if (!triggerCalled)
        {
            return;
        }

        // 这里不直接消费攻击缓冲。
        // 先回到 IdleState，让 GroundedState 在下一帧消费缓冲并重新进入
        // 同一个攻击状态对象。这样可以让状态机保持“不同状态之间切换”的规则，
        // 不需要强行对当前状态执行二次 Enter。
        stateMachine.ChangeState(player.idleState);
    }
}
