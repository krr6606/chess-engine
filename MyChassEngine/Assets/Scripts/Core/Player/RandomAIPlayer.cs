using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using Debug = UnityEngine.Debug;

public class RandomAIPlayer : AIPlayer
{
    [SerializeField] private float minThinkTime = 0.5f;
    [SerializeField] private float maxThinkTime = 2.0f;
    [SerializeField] private int difficultyLevel = 1; // 1-3: 초급, 중급, 고급
    
    private System.Random randomSeed = new System.Random(); // 스레드 안전한 랜덤 생성기
    
    public override void Initialize(BoardManager manager)
    {
        base.Initialize(manager);
        // 난이도에 따른 사고 시간 조정
        thinkTime = UnityEngine.Mathf.Lerp(maxThinkTime, minThinkTime, difficultyLevel / 3.0f);
        playerName = $"랜덤 AI (난이도 {difficultyLevel})";
        
        // 랜덤 시드 초기화 (시간 기반)
        randomSeed = new System.Random(Environment.TickCount);
    }
    
    // 비동기 이동 계산 메서드 구현
    protected override void CalculateMove(CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested) return;
        
        ChessGameState state = GetEvaluationState();
        var legalMoves = stateManager.GenerateLegalMoves(state);
        
        if (cancelToken.IsCancellationRequested) return;
        
        if (legalMoves.Count > 0)
        {
            // 난이도에 따라 선택 전략 변경
            if (difficultyLevel == 1) // 초급: 완전 랜덤
            {
                int randomIndex = randomSeed.Next(0, legalMoves.Count);
                selectedMove = legalMoves[randomIndex];
            }
            else if (difficultyLevel == 2) // 중급: 캡처 우선
            {
                // 캡처 이동을 우선적으로 선택
                List<Move> captureMoves = legalMoves.FindAll(m => m.IsCapture);
                
                if (captureMoves.Count > 0)
                {
                    int randomIndex = randomSeed.Next(0, captureMoves.Count);
                    selectedMove = captureMoves[randomIndex];
                }
                else
                {
                    int randomIndex = randomSeed.Next(0, legalMoves.Count);
                    selectedMove = legalMoves[randomIndex];
                }
            }
            else // 고급: 체크 또는 캡처 우선
            {
                if (cancelToken.IsCancellationRequested) return;
                
                // 체크 이동을 우선적으로 선택
                List<Move> checkMoves = new List<Move>();
                
                foreach (var move in legalMoves)
                {
                    if (cancelToken.IsCancellationRequested) return;
                    
                    ChessGameState newState = stateManager.ApplyMove(state, move);
                    
                    // 상대가 체크 상태가 되는지 확인
                    if (newState.IsInCheck(!newState.IsWhiteTurn))
                    {
                        checkMoves.Add(move);
                    }
                }
                
                if (checkMoves.Count > 0)
                {
                    int randomIndex = randomSeed.Next(0, checkMoves.Count);
                    selectedMove = checkMoves[randomIndex];
                }
                else
                {
                    // 체크 이동이 없으면 캡처 이동 시도
                    List<Move> captureMoves = legalMoves.FindAll(m => m.IsCapture);
                    
                    if (captureMoves.Count > 0)
                    {
                        int randomIndex = randomSeed.Next(0, captureMoves.Count);
                        selectedMove = captureMoves[randomIndex];
                    }
                    else
                    {
                        int randomIndex = randomSeed.Next(0, legalMoves.Count);
                        selectedMove = legalMoves[randomIndex];
                    }
                }
            }
        }
    }
    
    // 난이도 설정
    public override void SetDifficulty(int level)
    {
        difficultyLevel = Mathf.Clamp(level, 1, 3);
        thinkTime = UnityEngine.Mathf.Lerp(maxThinkTime, minThinkTime, difficultyLevel / 3.0f);
        playerName = $"랜덤 AI (난이도 {difficultyLevel})";
    }
} 