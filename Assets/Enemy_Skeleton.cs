using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Skeleton : Enemy
{

    #region ״̬

    public SkeletonIdleState IdleState {  get; private set; }
    public SkeletonMoveState moveState { get; private set; }

    public SkeletonBattleState battleState { get; private set; }

    #endregion


    protected override void Awake()
    {
        base.Awake();

        IdleState = new SkeletonIdleState(this, stateMachine, "Idle", this);
        moveState = new SkeletonMoveState(this, stateMachine,"Move",this);
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(IdleState);
    }

    protected override void Update()
    {
        base.Update();
    }
}
