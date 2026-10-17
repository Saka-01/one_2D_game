using UnityEngine;

/// <summary>
/// 管理玩家当前所处的状态。
///
/// 状态机本身不负责移动、跳跃等具体行为，
/// 它只负责：
/// 1. 记录当前状态；
/// 2. 退出旧状态；
/// 3. 进入新状态。
/// </summary>
public class PlayerStateMachine
{
    /// <summary>
    /// 玩家当前正在运行的状态。
    ///
    /// public：外部可以读取当前状态；
    /// private set：只有状态机自己能够修改当前状态。
    /// </summary>
    public PlayerState currentState { get; private set; }

    /// <summary>
    /// 初始化状态机。
    /// 游戏开始时通常只调用一次。
    /// </summary>
    /// <param name="_startState">玩家开始时所处的状态</param>
    public void Initialize(PlayerState _startState)
    {
        // 防止传入一个不存在的状态。
        // 如果没有这个检查，下一行调用 Enter() 时会产生空引用错误。
        if (_startState == null)
        {
            Debug.LogError("PlayerStateMachine 初始化失败：起始状态不能为空。");
            return;
        }

        currentState = _startState;
        currentState.Enter();
    }

    /// <summary>
    /// 从当前状态切换到新的状态。
    /// </summary>
    /// <param name="_newState">即将进入的新状态</param>
    public void ChangeState(PlayerState _newState)
    {
        // 不允许切换到空状态。
        if (_newState == null)
        {
            Debug.LogError("PlayerStateMachine 切换失败：目标状态不能为空。");
            return;
        }

        // 如果目标状态就是当前状态，则不重复切换。
        if (currentState == _newState)
        {
            return;
        }

        // 退出旧状态。
        // 问号表示：只有 currentState 不为空时才调用 Exit。
        currentState?.Exit();

        // 保存新状态。
        currentState = _newState;

        // 进入新状态。
        currentState.Enter();
    }
}