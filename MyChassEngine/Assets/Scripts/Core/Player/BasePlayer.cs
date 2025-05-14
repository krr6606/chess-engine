using UnityEngine;
using System;

public abstract class BasePlayer : MonoBehaviour, IPlayer
{
    protected BoardManager chessManager;
    protected bool isMyTurn = false;
    protected string playerName = "Base Player";

    public virtual void Initialize(BoardManager manager)
    {
        chessManager = manager;
    }

    public virtual void OnTurnStarted()
    {
        isMyTurn = true;
    }

    public virtual void OnMoveExecuted(Move move)
    {
        isMyTurn = false;
        StopAllCoroutines();
    }

    public virtual void OnGameEnded()
    {
        isMyTurn = false;
    }

    public virtual void Update()
    {
        if (!isMyTurn) return;
    }

    public virtual bool IsHumanPlayer => false;
    public virtual string PlayerName => playerName;
} 