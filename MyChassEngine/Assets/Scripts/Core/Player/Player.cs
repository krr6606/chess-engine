using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IPlayer
{
	void Initialize(BoardManager manager);
	void OnTurnStarted();
	void OnMoveExecuted(Move move);
	void OnGameEnded();
	void Update();
	bool IsHumanPlayer { get; }
	string PlayerName { get; }
}
