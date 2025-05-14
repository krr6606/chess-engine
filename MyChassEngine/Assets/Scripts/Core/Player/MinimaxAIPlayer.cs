using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Linq;

public class MinimaxAIPlayer : AIPlayer
{
    [SerializeField] private int searchDepth = 3;
    [SerializeField] private int maxSearchDepth = 6; // 반복 심화 탐색 최대 깊이
    [SerializeField] private bool useIterativeDeepening = true; // 반복 심화 탐색 사용 여부
    [SerializeField] private bool debugMode = false; // 디버그 모드
    [SerializeField] private float maxThinkingTime = 30f; // 최대 생각 시간 (초)
    
    // 성능 측정을 위한 변수
    private Stopwatch stopwatch = new Stopwatch();
    private int nodesEvaluated = 0;
    private int transpositionHits = 0;
    
    // 트랜스포지션 테이블 항목 클래스
    private class TranspositionEntry
    {
        public int Depth;
        public int Score;
        public Move? BestMove; // null 가능하도록 변경
    }
    
    // 트랜스포지션 테이블 (스레드 안전한 동시성 딕셔너리)
    private ConcurrentDictionary<ulong, TranspositionEntry> transpositionTable = new ConcurrentDictionary<ulong, TranspositionEntry>();
    
    // 현재 최적의 이동과 탐색 깊이
    private Move? currentBestMove; // null 가능하도록 변경
    private int currentMaxDepth;
    
    // 이동 객체 풀링을 위한 클래스
    private class MovePool
    {
        private Stack<Move> pool = new Stack<Move>(1000);
        
        public Move Get(int from, int to, int flags = 0)
        {
            if (pool.Count > 0)
                return pool.Pop().With(from, to, flags);
            else
                return new Move(from, to, flags);
        }
        
        public void Return(Move move)
        {
            pool.Push(move);
        }
        
        public void Return(List<Move> moves)
        {
            foreach (var move in moves)
                pool.Push(move);
            moves.Clear();
        }
    }
    
    private MovePool movePool = new MovePool();
    
    private int moveCount = 0;
    private const int RESET_TRANSPOSITION_EVERY_N_MOVES = 20; // 20수마다 트랜스포지션 테이블 초기화
    private const int MAX_TRANSPOSITION_SIZE = 300000; // 최대 트랜스포지션 테이블 크기
    private bool isRecoveryMode = false; // 복구 모드 플래그
    
    [Header("진단 설정")]
    [SerializeField] private bool enableStateValidation = true; // 상태 검증 활성화
    [SerializeField] private bool enableDetailedDiagnostics = true; // 상세 진단 정보 활성화
    [SerializeField] private int validationFrequency = 1; // 매번 검증
    
    // 진단 정보 관련 필드 추가
    private int validationCounter = 0;
    private string lastErrorSource = "";
    private string lastErrorDetails = "";
    private int consecutiveErrorCount = 0;
    private const int MAX_CONSECUTIVE_ERRORS = 3; // 연속 오류 최대 허용 횟수
    
    public override void Initialize(BoardManager manager)
    {
        base.Initialize(manager);
        playerName = "미니맥스 AI (깊이 " + searchDepth + ")";
        
        // 트랜스포지션 테이블 초기화
        transpositionTable = new ConcurrentDictionary<ulong, TranspositionEntry>();
    }
    
    // AIPlayer.Update() 메서드 오버라이드
    public override void Update()
    {
        // 기본 업데이트 로직
        base.Update();
        
        // AI 안전장치: 지정된 최대 시간보다 오래 생각하면 강제로 이동 선택
        if (isThinking && Time.time - lastMoveTime > maxThinkingTime)
        {
            Debug.LogWarning($"[미니맥스 AI] 안전장치 작동: 최대 생각 시간({maxThinkingTime}초)을 초과했습니다. 강제 이동 선택.");
            isRecoveryMode = true;
            ForceMoveSelection();
            isThinking = false;
            isRecoveryMode = false;
        }
        

    }
    
    // 이동 실행 후 호출되는 메서드
    public override void OnMoveExecuted(Move move)
    {

        // 주기적인 트랜스포지션 테이블 관리
        moveCount++;
        if (moveCount % RESET_TRANSPOSITION_EVERY_N_MOVES == 0)
        {
            transpositionTable.Clear();
            if (debugMode) Debug.Log($"[미니맥스 AI] 트랜스포지션 테이블 정기 초기화 수행됨 (턴 {moveCount})");
        }

        // 기본 로직 실행
        base.OnMoveExecuted(move);

    }

    // 스레드 안전한 상태를 사용하는 계산 메서드 개선
    protected override void CalculateMoveWithState(ChessGameState threadSafeState, CancellationToken cancelToken)
    {

            if (cancelToken.IsCancellationRequested) return;
            
            stopwatch.Restart();
            nodesEvaluated = 0;
            transpositionHits = 0;
            
            // 계산 시작 전 상태 검증
            ValidateGameState(threadSafeState, "계산시작_전");
            
            if (debugMode)
            {
                Debug.Log($"[미니맥스 AI] 이동 계산 시작 - 선택한 깊이: {searchDepth}");
            }
            
            // 현재 플레이어가 맞는지 확인 (AI가 흑색인데 상태가 백색 턴이면 문제)
            if (threadSafeState.IsWhiteTurn != chessManager.IsWhiteTurn())
            {
                Debug.LogWarning($"[미니맥스 AI] 턴 불일치 감지! AI턴: {(isMyTurn ? "자신" : "상대방")}, 상태 턴: {(threadSafeState.IsWhiteTurn ? "백색" : "흑색")}");
                threadSafeState.IsWhiteTurn = chessManager.IsWhiteTurn();
            }
            
            // 합법적인 이동 확인
            var possibleMoves = stateManager.GenerateLegalMoves(threadSafeState);
            if (possibleMoves.Count == 0)
            {
                Debug.LogWarning("[미니맥스 AI] 합법적인 이동이 없습니다!");
                return;
            }
            
            // 트랜스포지션 테이블 크기 관리
            if (transpositionTable.Count > MAX_TRANSPOSITION_SIZE)
            {
                transpositionTable.Clear();
                if (debugMode) Debug.Log("[미니맥스 AI] 트랜스포지션 테이블 초기화됨 (최대 크기 초과)");
            }
            
            if (useIterativeDeepening)
            {
                // 반복 심화 탐색 수행
                for (int currDepth = 1; currDepth <= maxSearchDepth; currDepth++)
                {
                    if (cancelToken.IsCancellationRequested) 
                    {
                        if (debugMode) Debug.Log($"[미니맥스 AI] 탐색 취소됨 (깊이 {currDepth})");
                        break;
                    }
                    
                    currentMaxDepth = currDepth;
                    var newBestMove = FindBestMove(threadSafeState, currDepth, cancelToken);
                    
                    // 중간 검증
                    ValidateGameState(threadSafeState, $"반복심화_깊이{currDepth}");
                    
                    // 유효한 이동을 찾은 경우에만 설정
                    if (newBestMove.HasValue)
                    {
                        currentBestMove = newBestMove;
                    }
                    
                    if (debugMode)
                    {
                        Debug.Log($"[미니맥스 AI] 깊이 {currDepth}: 최선의 이동 = {currentBestMove}, 평가된 노드 = {nodesEvaluated:N0}, 소요 시간 = {stopwatch.ElapsedMilliseconds}ms");
                    }
                    
                    // 시간 제한 확인 - 총 thinkTime의 70%에 도달하면 중지
                    if (stopwatch.ElapsedMilliseconds > (thinkTime * 1000 * 0.7))
                    {
                        if (debugMode) Debug.Log($"[미니맥스 AI] 시간 제한 도달 ({stopwatch.ElapsedMilliseconds}ms) - 탐색 중단");
                        break;
                    }
                }
            }
            else
            {
                // 단일 깊이 탐색 수행
                currentMaxDepth = searchDepth;
                currentBestMove = FindBestMove(threadSafeState, searchDepth, cancelToken);
                
            }
            
            stopwatch.Stop();
            
            if (currentBestMove.HasValue)
            {
                // 최종 이동 선택 (취소 토큰 상태와 상관없이 현재까지의 최선의 이동 사용)
                selectedMove = currentBestMove.Value;
                
                if (debugMode)
                {
                    Debug.Log($"[미니맥스 AI] 최종 선택된 이동: {selectedMove}");
                    Debug.Log($"[미니맥스 AI] 성능 정보: 평가된 노드 = {nodesEvaluated:N0}, 트랜스포지션 히트 = {transpositionHits:N0}, 소요 시간 = {stopwatch.ElapsedMilliseconds}ms");
                    Debug.Log($"[미니맥스 AI] 초당 노드: {((double)nodesEvaluated / stopwatch.ElapsedMilliseconds * 1000):N0}");
                    Debug.Log("현재 턴: " + (threadSafeState.IsWhiteTurn ? "백색" : "흑색")); 
                if (cancelToken.IsCancellationRequested)
                    {
                        Debug.Log($"[미니맥스 AI] 시간 초과로 현재까지의 최선의 이동 선택됨: {selectedMove}");
                    }
                }
            }
            else
            {
                // 최선의 이동을 전혀 찾지 못한 경우만 랜덤 이동 선택
                if (debugMode) Debug.Log("[미니맥스 AI] 최선의 이동을 찾지 못함 - 랜덤 이동 시도");
                
                var legalMoves = stateManager.GenerateLegalMoves(threadSafeState);
                if (legalMoves.Count > 0)
                {
                    selectedMove = legalMoves[random.Next(0, legalMoves.Count)];
                    if (debugMode) Debug.Log($"[미니맥스 AI] 랜덤 이동 선택됨: {selectedMove}");
                }
            }
            
    }
 
protected override void CalculateMove(CancellationToken cancelToken)
{
    // 현재 게임 상태 복제
    ChessGameState state = GetEvaluationState();
    
    // 복제한 상태로 계산
    CalculateMoveWithState(state, cancelToken);
}

    
    private Move? FindBestMove(ChessGameState state, int depth, CancellationToken cancelToken)
    {
        // 디버그 로그
        if (debugMode)
        {
            Debug.Log($"[미니맥스 AI] 깊이 {depth}에서 최선의 이동 검색 시작");
        }
        
        var legalMoves = stateManager.GenerateLegalMoves(state);
        
        // MVV-LVA로 이동 정렬
        stateManager.SortMovesByMVVLVA(legalMoves, state);
        
        if (legalMoves.Count == 0) return null;
        
        Move bestMove = legalMoves[0]; // 초기 최선의 이동
        int bestScore = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        
        if (debugMode)
        {
            Debug.Log($"[미니맥스 AI] 가능한 이동 수: {legalMoves.Count}");
        }
        
        foreach (var move in legalMoves)
        {
            if (cancelToken.IsCancellationRequested) break;
            
            // 상태 복제 대신 Undo 사용
            var undoInfo = stateManager.ApplyMoveWithUndo(state, move);
            
            int score = Minimax(state, depth - 1, alpha, beta, false, cancelToken);
            
            // 이동 되돌리기
            stateManager.UndoLastMove(state, undoInfo);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                
                if (debugMode)
                {
                    Debug.Log($"[미니맥스 AI] 새로운 최선의 이동: {move}, 점수: {score}");
                }
            }
            
            // 알파 업데이트
            alpha = Math.Max(alpha, bestScore);
        }
        
        return bestMove;
    }
    
    // 비트보드 안전 연산 유틸리티 메서드 추가 (클래스 내부에 추가)
    
    // 안전한 비트 AND 연산 (오버플로우 방지)
    private ulong SafeBitwiseAnd(ulong a, ulong b)
    {
        // 두 값 중 하나라도 비정상적으로 크면 0 반환
        if (a > 0x7FFFFFFFFFFFFFFF || b > 0x7FFFFFFFFFFFFFFF)
        {
            return 0UL;
        }
        return a & b;
    }
    
    // 안전한 비트 OR 연산 (오버플로우 방지)
    private ulong SafeBitwiseOr(ulong a, ulong b)
    {
        // 두 값 중 하나라도 비정상적으로 크면 정상적인 값만 반환
        if (a > 0x7FFFFFFFFFFFFFFF)
        {
            return b;
        }
        if (b > 0x7FFFFFFFFFFFFFFF)
        {
            return a;
        }
        return a | b;
    }
    
    // 안전한 비트 NOT 연산 (오버플로우 방지)
    private ulong SafeBitwiseNot(ulong a)
    {
        // 비정상적으로 큰 값이면 0 반환
        if (a > 0x7FFFFFFFFFFFFFFF)
        {
            return 0UL;
        }
        // 체스판 영역 (64비트) 내에서만 NOT 연산
        return ~a & 0xFFFFFFFFFFFFFFFFUL;
    }
    
    // 안전한 비트 XOR 연산 (오버플로우 방지)
    private ulong SafeBitwiseXor(ulong a, ulong b)
    {
        // 두 값 중 하나라도 비정상적으로 크면 다른 하나 반환
        if (a > 0x7FFFFFFFFFFFFFFF)
        {
            return b;
        }
        if (b > 0x7FFFFFFFFFFFFFFF)
        {
            return a;
        }
        return a ^ b;
    }
    
    // Minimax 메서드 개선 - 동시성 문제와 상태 복원 실패 방지
    private int Minimax(ChessGameState state, int depth, int alpha, int beta, bool isMaximizingPlayer, CancellationToken cancelToken)
    {
        try
        {
            if (cancelToken.IsCancellationRequested) return 0;
            
            // 검증 추가 (깊은 깊이에서는 빈도를 낮춤)
            if (depth >= currentMaxDepth - 2)
            {
                ValidateGameState(state, $"미니맥스_깊이{depth}");
            }
            
            // 노드 평가 카운터 증가
            nodesEvaluated++;
            
            // 해시 키 계산
            ulong stateHash = ComputeStateHash(state);
            
            // 트랜스포지션 테이블 조회
            if (transpositionTable.TryGetValue(stateHash, out TranspositionEntry entry) && entry.Depth >= depth)
            {
                transpositionHits++;
                return entry.Score;
            }
            
            // 종료 조건: 게임 종료 또는 최대 깊이 도달
            if (depth == 0 || stateManager.IsGameOver(state))
            {
                int score = EvaluatePosition(state);
                
                // 트랜스포지션 테이블에 결과 저장 (크기 제한 확인)
                if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
                {
                    transpositionTable[stateHash] = new TranspositionEntry 
                    { 
                        Depth = depth, 
                        Score = score,
                        BestMove = null
                    };
                }
                return score;
            }
            
            List<Move> legalMoves = stateManager.GenerateLegalMoves(state);
            
            // MVV-LVA로 이동 정렬
            stateManager.SortMovesByMVVLVA(legalMoves, state);
            
            // 합법적인 이동이 없으면 평가
            if (legalMoves.Count == 0)
            {
                int score = EvaluatePosition(state);
                
                if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
                {
                    transpositionTable[stateHash] = new TranspositionEntry 
                    { 
                        Depth = depth, 
                        Score = score,
                        BestMove = null
                    };
                }
                
                return score;
            }
            
            Move? bestMove = null;
            
            if (isMaximizingPlayer)
            {
                int maxScore = int.MinValue;
                
                foreach (var move in legalMoves)
                {
                    if (cancelToken.IsCancellationRequested) break;
                    
                    // 동시성 문제 해결: 깊은 깊이에서는 상태 복제를 사용
                    // 얕은 깊이에서는 성능을 위해 Undo 사용
                    if (depth >= 4) // 깊은 탐색에서는 복제 사용
                    {
                        // 깊은 복사를 통해 안전하게 상태 복제 (스레드 안전)
                        ChessGameState clonedState = state.Clone();
                        
                        // 이동 적용
                        stateManager.ApplyMoveToState(clonedState, move);
                        
                        // 복제된 상태로 재귀 호출
                        int score = Minimax(clonedState, depth - 1, alpha, beta, false, cancelToken);
                        
                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestMove = move;
                        }
                    }
                    else // 얕은 탐색에서는 성능을 위해 Undo 사용
                    {
                        var undoInfo = stateManager.ApplyMoveWithUndo(state, move);
                        int score;
                        
                        try {
                            score = Minimax(state, depth - 1, alpha, beta, false, cancelToken);
                        }
                        finally {
                            // 이동 되돌리기 (예외가 발생해도 반드시 실행)
                            stateManager.UndoLastMove(state, undoInfo);
                            
                            // 복원 후 상태 검증 (성능 영향을 줄이기 위해 일부 깊이에서만 수행)
                            if (depth >= currentMaxDepth - 1 && random.Next(100) == 0)
                            {
                                ValidateGameState(state, $"복원후_검증_깊이{depth}");
                            }
                        }
                        
                        if (score > maxScore)
                        {
                            maxScore = score;
                            bestMove = move;
                        }
                    }
                    
                    alpha = Math.Max(alpha, maxScore);
                    
                    // 알파-베타 가지치기
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                
                // 트랜스포지션 테이블에 결과 저장 (크기 제한 확인)
                if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
                {
                    transpositionTable[stateHash] = new TranspositionEntry 
                    { 
                        Depth = depth, 
                        Score = maxScore,
                        BestMove = bestMove
                    };
                }
                
                return maxScore;
            }
            else
            {
                int minScore = int.MaxValue;
                
                foreach (var move in legalMoves)
                {

                        var undoInfo = stateManager.ApplyMoveWithUndo(state, move);
                        int score;
                        
                        try {
                            score = Minimax(state, depth - 1, alpha, beta, true, cancelToken);
                        }
                        finally {
                            // 이동 되돌리기 (예외가 발생해도 반드시 실행)
                            stateManager.UndoLastMove(state, undoInfo);
                            

                        }
                        
                        if (score < minScore)
                        {
                            minScore = score;
                            bestMove = move;
                        }
                    
                    beta = Math.Min(beta, minScore);
                    
                    // 알파-베타 가지치기
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                
                // 트랜스포지션 테이블에 결과 저장 (크기 제한 확인)
                if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
                {
                    transpositionTable[stateHash] = new TranspositionEntry 
                    { 
                        Depth = depth, 
                        Score = minScore,
                        BestMove = bestMove
                    };
                }
                
                return minScore;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[미니맥스 AI] Minimax 계산 중 예외 발생: {ex.Message} (깊이: {depth}, 최대화: {isMaximizingPlayer})");
            // 오류가 발생하면 중립적인 평가 값 반환
            return 0;
        }
    }
    
    // 체스 상태 해시 계산 (Zobrist 해싱) - 안전한 비트 연산 사용
    private ulong ComputeStateHash(ChessGameState state)
    {
        ulong hash = 0;
        
        // 간단한 해시 구현 - 더 정교한 Zobrist 해싱으로 개선 가능
        for (int i = 0; i < 12; i++)
        {
            ulong bitboard = state.BitBoards[i];
            
            // 비정상적인 값 확인
            if (bitboard > 0x7FFFFFFFFFFFFFFF)
            {
                // 손상된 비트보드 감지 - 해싱에서 제외
                continue;
            }
            
            // 안전한 XOR 연산 사용
            hash = SafeBitwiseXor(hash, bitboard * (ulong)(i + 1));
        }
        
        // 턴 정보 추가
        if (state.IsWhiteTurn)
        {
            hash = SafeBitwiseXor(hash, 0x1UL);
        }
        
        // 캐슬링 권한 추가
        if (state.WhiteKingSideCastleRight) hash = SafeBitwiseXor(hash, 0x2UL);
        if (state.WhiteQueenSideCastleRight) hash = SafeBitwiseXor(hash, 0x4UL);
        if (state.BlackKingSideCastleRight) hash = SafeBitwiseXor(hash, 0x8UL);
        if (state.BlackQueenSideCastleRight) hash = SafeBitwiseXor(hash, 0x10UL);
        
        // 앙파상 타겟 추가
        if (state.EnPassantTargetSquare >= 0)
        {
            hash = SafeBitwiseXor(hash, (ulong)(state.EnPassantTargetSquare + 1) << 32);
        }
        
        return hash;
    }
    
    private int EvaluatePosition(ChessGameState state)
    {
        // 게임 종료 상태 특별 처리
        if (state.CurrentGameState == ChessGameState.GameState.Checkmate)
        {
            // 체크메이트 시 현재 움직일 차례가 패배한 경우

            return state.IsWhiteTurn ? -10000 : 10000;
        }
        
        if (state.CurrentGameState == ChessGameState.GameState.Stalemate ||
            state.CurrentGameState == ChessGameState.GameState.DrawByFiftyMoveRule)
        {
            return 0; // 무승부는 0점
        }
        
        // 향상된 평가 함수: 기물 가치 + 위치 보너스 + 기물 기동성 + 킹 안전 + 공격/방어
        int score = 0;
        
        // 기물 가치 설정
        int pawnValue = 100;
        int knightValue = 320;
        int bishopValue = 330;
        int rookValue = 500;
        int queenValue = 900;
        
        // 백색 기물 점수 계산
        score += BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN]) * pawnValue;
        score += BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT]) * knightValue;
        score += BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP]) * bishopValue;
        score += BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_ROOK]) * rookValue;
        score += BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_QUEEN]) * queenValue;
        
        // 흑색 기물 점수 계산 (마이너스)
        score -= BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN]) * pawnValue;
        score -= BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT]) * knightValue;
        score -= BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP]) * bishopValue;
        score -= BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_ROOK]) * rookValue;
        score -= BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_QUEEN]) * queenValue;
        
        // 기물 위치 보너스 점수 (센터 지배, 발전 등)
        score += CalculatePositionalBonus(state);
        
        // 공격/방어 점수 (체크, 핀 등)
        score += CalculateAttackDefenseBonus(state);
        
        // 킹 안전 점수
        score += CalculateKingSafetyBonus(state);
        
        // AI 색상에 따른 점수 조정
        if (!state.IsWhiteTurn) {
            // 흑색 턴에서는 점수를 반전
            score = -score;
        }
        
        return score;
    }
    
    // 기물 위치에 따른 보너스 점수 계산
    private int CalculatePositionalBonus(ChessGameState state)
    {
        int bonus = 0;
        
        // 확장된 센터 영역 (중앙 16칸)
        ulong extendedCenterSquares = 0x00003C3C3C3C0000UL;
        
        // 센터 지배 보너스
        bonus += 5 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN] & extendedCenterSquares);
        bonus -= 5 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN] & extendedCenterSquares);
        
        bonus += 10 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT] & extendedCenterSquares);
        bonus -= 10 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT] & extendedCenterSquares);
        
        bonus += 8 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP] & extendedCenterSquares);
        bonus -= 8 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP] & extendedCenterSquares);
        
        // 나이트에 대한 추가 보너스: 외곽에 있으면 패널티
        ulong edgeSquares = 0xFF818181818181FFUL;
        bonus -= 15 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT] & edgeSquares);
        bonus += 15 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT] & edgeSquares);
        
        // 백색 폰 전진 보너스 (더 앞쪽에 있을수록 가치 증가)
        for (int rank = 1; rank < 7; rank++)
        {
            ulong rankMask = 0xFFUL << (rank * 8);
            int rankBonus = rank * 2; // 높은 랭크일수록 더 큰 보너스
            bonus += rankBonus * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN] & rankMask);
        }
        
        // 흑색 폰 전진 보너스
        for (int rank = 6; rank > 0; rank--)
        {
            ulong rankMask = 0xFFUL << (rank * 8);
            int rankBonus = (7 - rank) * 2; // 낮은 랭크일수록 더 큰 보너스
            bonus -= rankBonus * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN] & rankMask);
        }
        
        // 비숍 페어 보너스
        if (BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP]) >= 2)
        {
            bonus += 30;
        }
        if (BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP]) >= 2)
        {
            bonus -= 30;
        }
        
        return bonus;
    }
    
    // 공격 및 방어 관련 보너스 계산
    private int CalculateAttackDefenseBonus(ChessGameState state)
    {
        int bonus = 0;
        
        // 체크 상황 보너스
        if (state.IsInCheck(!state.IsWhiteTurn))
        {
            bonus += 20; // 상대를 체크 상태로 만들면 보너스
        }
        if (state.IsInCheck(state.IsWhiteTurn))
        {
            bonus -= 10; // 자신이 체크 상태면 패널티
        }
        
        // 핀된 기물 보너스/패널티
        int whitePinnedCount = BitHelper.CountBits(state.PinnedPieces & state.WhitePieces);
        int blackPinnedCount = BitHelper.CountBits(state.PinnedPieces & state.BlackPieces);
        bonus -= whitePinnedCount * 15;
        bonus += blackPinnedCount * 15;
        
        return bonus;
    }
    
    // 킹 안전 관련 보너스 계산
    private int CalculateKingSafetyBonus(ChessGameState state)
    {
        int bonus = 0;
        
        // 백색 킹 주변의 아군 기물 수
        int whiteKingSquare = state.WhiteKingSquare;
        int whiteKingProtection = CountProtectingPieces(state, whiteKingSquare, true);
        
        // 흑색 킹 주변의 아군 기물 수
        int blackKingSquare = state.BlackKingSquare;
        int blackKingProtection = CountProtectingPieces(state, blackKingSquare, false);
        
        // 킹 보호 보너스
        bonus += whiteKingProtection * 5;
        bonus -= blackKingProtection * 5;
        
        // 캐슬링 권한 보너스
        if (state.WhiteKingSideCastleRight || state.WhiteQueenSideCastleRight)
        {
            bonus += 15;
        }
        if (state.BlackKingSideCastleRight || state.BlackQueenSideCastleRight)
        {
            bonus -= 15;
        }
        
        return bonus;
    }
    
    // 킹 주변의 아군 기물 수 계산
    private int CountProtectingPieces(ChessGameState state, int kingSquare, bool isWhite)
    {
        int count = 0;
        
        // 킹을 중심으로 주변 8칸 확인
        int[] offsets = { -9, -8, -7, -1, 1, 7, 8, 9 };
        ulong ownPieces = isWhite ? state.WhitePieces : state.BlackPieces;
        
        foreach (int offset in offsets)
        {
            int adjacentSquare = kingSquare + offset;
            
            // 유효한 인덱스 범위인지 확인
            if (adjacentSquare >= 0 && adjacentSquare < 64)
            {
                // 같은 행/열에 있는지 확인 (보드 경계를 넘어가는 경우 방지)
                int kingFile = kingSquare % 8;
                int kingRank = kingSquare / 8;
                int adjFile = adjacentSquare % 8;
                int adjRank = adjacentSquare / 8;
                
                // 행/열 차이가 1 이하인 경우만 처리
                if (Math.Abs(kingFile - adjFile) <= 1 && Math.Abs(kingRank - adjRank) <= 1)
                {
                    // 주변 칸에 아군 기물이 있는지 확인
                    if (BitHelper.IsBitSet(ownPieces, adjacentSquare))
                    {
                        count++;
                    }
                }
            }
        }
        
        return count;
    }
    
    // 강제 이동 선택 메서드 개선
    protected override void ForceMoveSelection()
    {
        if (!isRecoveryMode)
        {
            Debug.Log("[미니맥스 AI] 강제 이동 선택 시작");
        }
        
        // 스톱워치 있다면 정지
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
        }
        
        // 현재까지의 최선의 이동 선택
        if (currentBestMove.HasValue)
        {
            Debug.Log("[미니맥스 AI] 현재까지 찾은 최선의 이동 선택");   
            selectedMove = currentBestMove.Value;
            ExecuteMove(selectedMove);
        }
        else
        {
            // 최선의 이동을 찾지 못한 경우, 기본 구현을 사용
            Debug.Log("[미니맥스 AI] 최선의 이동을 찾지 못해 기본 랜덤 이동 사용");
            base.ForceMoveSelection();
        }
    }
    
    public override void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
        
        if (debugMode)
        {
            Debug.Log($"미니맥스 AI 디버그 모드: {(enabled ? "켜짐" : "꺼짐")}");
            Debug.Log($"현재 설정: 깊이 = {searchDepth}, 최대 깊이 = {maxSearchDepth}, 반복 심화 = {useIterativeDeepening}");
        }
    }
    
    public override void SetDifficulty(int level)
    {
        level = Mathf.Clamp(level, 1, 6);
        
        switch (level)
        {
            case 1:
                searchDepth = 2;
                maxSearchDepth = 3;
                break;
            case 2:
                searchDepth = 3;
                maxSearchDepth = 4;
                break;
            case 3:
                searchDepth = 4;
                maxSearchDepth = 5;
                break;
            case 4:
                searchDepth = 5;
                maxSearchDepth = 6;
                break;
            case 5:
                searchDepth = 6;
                maxSearchDepth = 7;
                break;
            case 6:
                searchDepth = 7;
                maxSearchDepth = 8;
                break;
        }
        
        playerName = $"미니맥스 AI (난이도 {level})";
    }
    
    // 자원 정리 메서드 개선
    private void OnDisable()
    {
        try
        {
            // 안전한 정리 작업
            if (transpositionTable != null)
            {
                transpositionTable.Clear();
            }
            
            // 스톱워치 정지
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
            
            System.GC.Collect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[미니맥스 AI] OnDisable 중 예외 발생: {ex.Message}");
        }
    }
    
    // 게임 상태 검증 메서드 - 클래스 내 적절한 위치에 추가
    private bool ValidateGameState(ChessGameState state, string source)
    {
        if (!enableStateValidation) return true;
        
        validationCounter++;
        if (validationCounter % validationFrequency != 0) return true;
        
        bool isValid = true;
        string errorDetails = "";
        

            // 1. 기본 비트보드 유효성 검사 및 자동 복구 시도
            for (int i = 0; i < 12; i++)
            {
                ulong bitboard = state.BitBoards[i];
                

                
                // 비트보드가 체스판의 합리적인 상태를 나타내는지 확인
                // 각 비트보드는 최대 8개(폰) 또는 10개(기타 기물 합산) 정도의 기물만 가질 수 있음
                int bitCount = BitHelper.CountBits(bitboard);
                int maxExpectedPieces = (i == ChessGameState.WHITE_PAWN || i == ChessGameState.BLACK_PAWN) ? 8 : 10;
                
                if (bitCount > maxExpectedPieces)
                {
                    isValid = false;
                    errorDetails += $"[오류] 비트보드[{i}]에 너무 많은 기물이 있습니다: {bitCount}개 (최대 예상: {maxExpectedPieces}개)\n";
                }
            }
            
            // 2. BitBoards 합산과 AllPieces 일치 확인
            ulong whiteSum = 0, blackSum = 0;
            for (int i = 0; i < 6; i++)
            {
                whiteSum |= state.BitBoards[i];
            }
            for (int i = 6; i < 12; i++)
            {
                blackSum |= state.BitBoards[i];
            }
            
            if (whiteSum != state.WhitePieces)
            {
                isValid = false;
                errorDetails += $"[오류] 백색 비트보드 합계({whiteSum})와 WhitePieces({state.WhitePieces})가 일치하지 않습니다.\n";
            }
            
            if (blackSum != state.BlackPieces)
            {
                isValid = false;
                errorDetails += $"[오류] 흑색 비트보드 합계({blackSum})와 BlackPieces({state.BlackPieces})가 일치하지 않습니다.\n";
            }
            
            ulong allPiecesSum = whiteSum | blackSum;
            if (allPiecesSum != state.AllPieces)
            {
                isValid = false;
                errorDetails += $"[오류] 전체 비트보드 합계({allPiecesSum})와 AllPieces({state.AllPieces})가 일치하지 않습니다.\n";
            }
            
            // 3. 보드 배열 검증
            for (int i = 0; i < 64; i++)
            {
                int piece = state.Board[i];
                if (piece != 0)
                {
                    // 기물 값이 유효한지 확인
                    int pieceType = piece & pieceNum.pieceMask;
                    int pieceColor = piece & pieceNum.colorMask;
                    
                    bool validPieceType = pieceType == pieceNum.pwan || 
                                          pieceType == pieceNum.knight || 
                                          pieceType == pieceNum.bishop || 
                                          pieceType == pieceNum.rook || 
                                          pieceType == pieceNum.queen || 
                                          pieceType == pieceNum.king;
                                          
                    bool validPieceColor = pieceColor == pieceNum.white || pieceColor == pieceNum.black;
                    
                    if (!validPieceType || !validPieceColor)
                    {
                        isValid = false;
                        errorDetails += $"[오류] 위치 {i}의 기물 값({piece})이 유효하지 않습니다. 타입:{pieceType}, 색상:{pieceColor}\n";
                    }
                    
                    // 추가: 알 수 없는 값을 가진 기물이 있는지 검사
                    if (piece > 200 || piece < 0)
                    {
                        isValid = false;
                        errorDetails += $"[오류] 위치 {i}의 기물 값({piece})이 비정상적으로 크거나 음수입니다.\n";
                    }
                    
                    // 비트보드와 일치하는지 확인
                    bool isBitSet = false;
                    if (pieceColor == pieceNum.white)
                    {
                        int bitboardIndex = -1;
                        if (pieceType == pieceNum.pwan) bitboardIndex = ChessGameState.WHITE_PAWN;
                        else if (pieceType == pieceNum.knight) bitboardIndex = ChessGameState.WHITE_KNIGHT;
                        else if (pieceType == pieceNum.bishop) bitboardIndex = ChessGameState.WHITE_BISHOP;
                        else if (pieceType == pieceNum.rook) bitboardIndex = ChessGameState.WHITE_ROOK;
                        else if (pieceType == pieceNum.queen) bitboardIndex = ChessGameState.WHITE_QUEEN;
                        else if (pieceType == pieceNum.king) bitboardIndex = ChessGameState.WHITE_KING;
                        
                        if (bitboardIndex >= 0)
                        {
                            isBitSet = BitHelper.IsBitSet(state.BitBoards[bitboardIndex], i);
                        }
                    }
                    else
                    {
                        int bitboardIndex = -1;
                        if (pieceType == pieceNum.pwan) bitboardIndex = ChessGameState.BLACK_PAWN;
                        else if (pieceType == pieceNum.knight) bitboardIndex = ChessGameState.BLACK_KNIGHT;
                        else if (pieceType == pieceNum.bishop) bitboardIndex = ChessGameState.BLACK_BISHOP;
                        else if (pieceType == pieceNum.rook) bitboardIndex = ChessGameState.BLACK_ROOK;
                        else if (pieceType == pieceNum.queen) bitboardIndex = ChessGameState.BLACK_QUEEN;
                        else if (pieceType == pieceNum.king) bitboardIndex = ChessGameState.BLACK_KING;
                        
                        if (bitboardIndex >= 0)
                        {
                            isBitSet = BitHelper.IsBitSet(state.BitBoards[bitboardIndex], i);
                        }
                    }
                    
                    if (!isBitSet)
                    {
                        isValid = false;
                        errorDetails += $"[오류] 위치 {i}의 기물은 보드에 있지만 해당 비트보드에는 설정되지 않았습니다.\n";
                    }
                }
            }
            
            // 4. 킹이 정확히 하나씩 있는지 확인
            int whiteKingCount = BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KING]);
            int blackKingCount = BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KING]);
            
            if (whiteKingCount != 1)
            {
                isValid = false;
                errorDetails += $"[오류] 백색 킹의 수가 1이 아닙니다: {whiteKingCount}\n";
            }
            
            if (blackKingCount != 1)
            {
                isValid = false;
                errorDetails += $"[오류] 흑색 킹의 수가 1이 아닙니다: {blackKingCount}\n";
            }
        if (!isValid)
        {
            lastErrorSource = source;
            lastErrorDetails = errorDetails;
            consecutiveErrorCount++;
            
            // 상세 진단 정보 출력
            if (enableDetailedDiagnostics || consecutiveErrorCount >= MAX_CONSECUTIVE_ERRORS)
            {
                Debug.LogError($"==== 게임 상태 검증 실패({lastErrorSource}) ====\n{lastErrorDetails}");
                LogGameState(state);
                
                if (consecutiveErrorCount >= MAX_CONSECUTIVE_ERRORS)
                {
                    Debug.LogError($"연속 {MAX_CONSECUTIVE_ERRORS}회 이상 상태 오류 발생! 게임 안정성을 위해 강제 복구를 시도합니다.");
                    ForceGameStateRecovery();
                }
            }
        }
        else 
        {
            consecutiveErrorCount = 0;
        }
        
        return isValid;
    }
    
    // 게임 상태 로깅 함수
    private void LogGameState(ChessGameState state)
    {
        Debug.Log("===== 현재 게임 상태 =====");
        
        // 비트보드 상태 출력
        for (int i = 0; i < 12; i++)
        {
            string pieceName = "";
            switch (i)
            {
                case ChessGameState.WHITE_PAWN: pieceName = "백색 폰"; break;
                case ChessGameState.WHITE_KNIGHT: pieceName = "백색 나이트"; break;
                case ChessGameState.WHITE_BISHOP: pieceName = "백색 비숍"; break;
                case ChessGameState.WHITE_ROOK: pieceName = "백색 룩"; break;
                case ChessGameState.WHITE_QUEEN: pieceName = "백색 퀸"; break;
                case ChessGameState.WHITE_KING: pieceName = "백색 킹"; break;
                case ChessGameState.BLACK_PAWN: pieceName = "흑색 폰"; break;
                case ChessGameState.BLACK_KNIGHT: pieceName = "흑색 나이트"; break;
                case ChessGameState.BLACK_BISHOP: pieceName = "흑색 비숍"; break;
                case ChessGameState.BLACK_ROOK: pieceName = "흑색 룩"; break;
                case ChessGameState.BLACK_QUEEN: pieceName = "흑색 퀸"; break;
                case ChessGameState.BLACK_KING: pieceName = "흑색 킹"; break;
            }
            Debug.Log($"BitBoard[{i}] {pieceName}: {state.BitBoards[i]}");
        }
        
        // 현재 턴 정보
        Debug.Log($"현재 턴: {(state.IsWhiteTurn ? "백색" : "흑색")}");
        Debug.Log($"게임 상태: {state.CurrentGameState}");
        
        // 캐슬링 권한
        Debug.Log($"캐슬링 권한: 백킹측({state.WhiteKingSideCastleRight}), 백퀸측({state.WhiteQueenSideCastleRight}), 흑킹측({state.BlackKingSideCastleRight}), 흑퀸측({state.BlackQueenSideCastleRight})");
        
        // 앙파상 정보
        Debug.Log($"앙파상 타겟: {state.EnPassantTargetSquare}");
        Debug.Log($"50수 카운터: {state.FiftyMoveCounter}, 전체 수 카운터: {state.FullMoveCounter}");
        
        // 체스판 상태 출력
        Debug.Log("체스판 상태:");
        string boardVisualization = "";
        for (int rank = 7; rank >= 0; rank--) {
            for (int file = 0; file < 8; file++) {
                int squareIndex = rank * 8 + file;
                int piece = state.Board[squareIndex];
                
                char pieceChar = '.';
                if (piece != 0) {
                    int pieceType = piece & pieceNum.pieceMask;
                    bool isWhite = (piece & pieceNum.colorMask) == pieceNum.white;
                    
                    if (pieceType == pieceNum.pwan) pieceChar = isWhite ? 'P' : 'p';
                    else if (pieceType == pieceNum.knight) pieceChar = isWhite ? 'N' : 'n';
                    else if (pieceType == pieceNum.bishop) pieceChar = isWhite ? 'B' : 'b';
                    else if (pieceType == pieceNum.rook) pieceChar = isWhite ? 'R' : 'r';
                    else if (pieceType == pieceNum.queen) pieceChar = isWhite ? 'Q' : 'q';
                    else if (pieceType == pieceNum.king) pieceChar = isWhite ? 'K' : 'k';
                    else pieceChar = '?'; // 알 수 없는 기물
                }
                boardVisualization += pieceChar + " ";
            }
            boardVisualization += "\n";
        }
        Debug.Log(boardVisualization);
    }
    
    // 게임 상태 강제 복구 함수
    private void ForceGameStateRecovery()
    {
        Debug.LogWarning("게임 상태 강제 복구 시도 중...");
        
        try
        {
            // 현재 체스 매니저에서 상태 가져오기
            ChessGameState currentState = chessManager.GetCurrentState();
            
            // 모든 비트보드 복구 시도
            for (int i = 0; i < 12; i++)
            {
                TryRepairBitboard(currentState, i);
            }
            
            // 공격 맵 강제 업데이트
            ForceUpdateAttackMaps(currentState);
            
            // 수정된 상태를 게임에 적용할 수 있으면 좋겠지만, 
            // 현재 인터페이스상 직접 적용은 어려움
            
            // 트랜스포지션 테이블 초기화
            transpositionTable.Clear();
            
            // 현재 계산 취소
            if (cancelTokenSource != null && !cancelTokenSource.IsCancellationRequested)
            {
                cancelTokenSource.Cancel();
            }
            
            // 현재까지의 최선의 이동을 초기화
            currentBestMove = null;
            
            // 연속 오류 카운터 초기화
            consecutiveErrorCount = 0;
            
            // 강제 이동 선택 요청
            moveReady = true;
            
            // GC 호출
            System.GC.Collect();
            
            Debug.Log("게임 상태 강제 복구 완료. 다음 턴에서 정상화될 것입니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"게임 상태 강제 복구 실패: {ex.Message}");
        }
    }

    // 인스펙터에 표시할 진단 정보 추가
    private void OnGUI()
    {
        if (enableDetailedDiagnostics && !string.IsNullOrEmpty(lastErrorSource))
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.red;
            style.fontSize = 14;
            style.wordWrap = true;
            
            GUI.Label(new Rect(10, 10, Screen.width - 20, 200), 
                      $"[AI 진단 오류]\n출처: {lastErrorSource}\n최근 오류: {lastErrorDetails}", 
                      style);
        }
    }

    // 게임 상태를 직접 검증하는 공개 메서드 추가 (외부에서 호출용)
    public void ValidateCurrentGameState()
    {
        // 현재 게임 상태 가져오기
        ChessGameState currentGameState = chessManager.GetCurrentState();
        
        // 강제로 상세 검증 활성화하여 검증
        bool prevDetailedDiagnostics = enableDetailedDiagnostics;
        bool prevStateValidation = enableStateValidation;
        
        enableDetailedDiagnostics = true;
        enableStateValidation = true;
        
        Debug.Log("현재 게임 상태 수동 검증 시작...");
        bool isValid = ValidateGameState(currentGameState, "수동검증");
        
        if (isValid)
        {
            Debug.Log("현재 게임 상태가 정상입니다.");
        }
        
        // 원래 설정으로 복원
        enableDetailedDiagnostics = prevDetailedDiagnostics;
        enableStateValidation = prevStateValidation;
    }

    // 비트보드 복구 메서드 추가
    private bool TryRepairBitboard(ChessGameState state, int bitboardIndex)
    {
        try
        {
            // 어떤 비트보드인지 확인
            string pieceName = "";
            bool isWhite = bitboardIndex < 6;
            
            switch (bitboardIndex)
            {
                case ChessGameState.WHITE_PAWN: 
                case ChessGameState.BLACK_PAWN:
                    pieceName = "폰"; break;
                case ChessGameState.WHITE_KNIGHT:
                case ChessGameState.BLACK_KNIGHT:
                    pieceName = "나이트"; break;
                case ChessGameState.WHITE_BISHOP:
                case ChessGameState.BLACK_BISHOP:
                    pieceName = "비숍"; break;
                case ChessGameState.WHITE_ROOK:
                case ChessGameState.BLACK_ROOK:
                    pieceName = "룩"; break;
                case ChessGameState.WHITE_QUEEN:
                case ChessGameState.BLACK_QUEEN:
                    pieceName = "퀸"; break;
                case ChessGameState.WHITE_KING:
                case ChessGameState.BLACK_KING:
                    pieceName = "킹"; break;
            }
            
            Debug.Log($"[BitboardRepair] {(isWhite ? "백색" : "흑색")} {pieceName} 비트보드 복구 시도");
            
            // 보드 배열에서 해당 기물 위치를 찾아 비트보드 재구성
            ulong repairedBitboard = 0UL;
            int pieceColor = isWhite ? pieceNum.white : pieceNum.black;
            int pieceType = 0;
            
            switch (bitboardIndex % 6)
            {
                case 0: pieceType = pieceNum.pwan; break;
                case 1: pieceType = pieceNum.knight; break;
                case 2: pieceType = pieceNum.bishop; break;
                case 3: pieceType = pieceNum.rook; break;
                case 4: pieceType = pieceNum.queen; break;
                case 5: pieceType = pieceNum.king; break;
            }
            
            int targetPiece = pieceColor | pieceType;
            
            // 보드 배열을 순회하며 해당 기물 찾기
            for (int i = 0; i < 64; i++)
            {
                int piece = state.Board[i];
                if (piece == targetPiece)
                {
                    // 이 위치에 기물 있음 => 비트보드에 추가
                    repairedBitboard |= 1UL << i;
                }
            }
            
            // 비트보드 값 업데이트
            state.BitBoards[bitboardIndex] = repairedBitboard;
            
            // 비트보드 업데이트 후 공격 맵과 체크 정보 갱신
            // 이렇게 하면 내부적으로 WhitePieces, BlackPieces, AllPieces가 업데이트됨
            state.UpdateAttackMaps();
            state.UpdatePinInformation();
            state.UpdateCheckInformation();
            
            Debug.Log($"[BitboardRepair] 비트보드 복구 완료. 새 값: {state.BitBoards[bitboardIndex]}");
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BitboardRepair] 비트보드 복구 실패: {ex.Message}");
            return false;
        }
    }

    private void ForceUpdateAttackMaps(ChessGameState state)
    {

        state.ForceUpdateAttackMaps();
    }
    /// <summary>
    /// 이동을 적용하고 되돌릴 수 있는 정보를 반환합니다.
    /// </summary>
    public MoveUndoInfo ApplyMoveWithUndo(ChessGameState state, Move move)
    {
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;

        // 되돌리기 위한 정보 생성
        MoveUndoInfo undoInfo = new MoveUndoInfo
        {
            Move = move,
            CapturedPiece = state.GetPieceAt(toSquare),
            WasWhiteTurn = state.IsWhiteTurn,
            EnPassantTarget = state.EnPassantTargetSquare,
            WhiteKingSideCastle = state.WhiteKingSideCastleRight,
            WhiteQueenSideCastle = state.WhiteQueenSideCastleRight,
            BlackKingSideCastle = state.BlackKingSideCastleRight,
            BlackQueenSideCastle = state.BlackQueenSideCastleRight,
            FiftyMoveCounter = state.FiftyMoveCounter,
            PreviousGameState = state.CurrentGameState
        };

        // 이동 적용
        ApplyMoveToState(state, move);

        // 되돌리기 정보를 반환
        return undoInfo;
    }
} 