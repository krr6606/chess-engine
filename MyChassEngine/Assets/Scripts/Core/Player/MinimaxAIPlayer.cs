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
    [SerializeField] private int searchDepth = 2;
    [SerializeField] private int maxSearchDepth = 6; // 반복 심화 탐색 최대 깊이
    [SerializeField] private bool useIterativeDeepening = true; // 반복 심화 탐색 사용 여부
    [SerializeField] private bool debugMode = false; // 디버그 모드
    [SerializeField] public float maxThinkingTime = 3f; // 최대 생각 시간 (초)
    
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
    public Move? currentBestMove; // null 가능하도록 변경
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
        if (!isMyTurn) return;
        base.Update();

        // 생각 시간이 지났는지 확인
        if ((Time.time + thinkTime) - (lastMoveTime + thinkTime) >= thinkTime || moveReady)
        {  
            // 이동이 준비되었으면 실행
            if (selectedMove.FromSquare != 0 && selectedMove.ToSquare != 0)
            {
                LogSafe("시작 무브: " + selectedMove.FromSquare + "도착 무브: " + selectedMove.ToSquare);

                // 이동 유효성 최종 검사
                ForceMoveSelection();
                if (debugMode)
                {
                    LogSafe($"[미니맥스 AI] 최종 선택된 이동: {selectedMove}");
                    LogSafe($"[미니맥스 AI] 성능 정보: 평가된 노드 = {nodesEvaluated:N0}, 트랜스포지션 히트 = {transpositionHits:N0}, 소요 시간 = {stopwatch.ElapsedMilliseconds}ms");

                }
            }


        }


    }
    
    // 이동 결정 후 호출되는 메서드
    public override void OnMoveExecuted(Move move)
    {

        // 주기적인 트랜스포지션 테이블 관리
        moveCount++;
        if (moveCount % RESET_TRANSPOSITION_EVERY_N_MOVES == 0)
        {
            transpositionTable.Clear();
            if (debugMode) LogSafe($"[미니맥스 AI] 트랜스포지션 테이블 정기 초기화 수행됨 (턴 {moveCount})");
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
            

            

            
            // 현재 플레이어가 맞는지 확인 (AI가 흑색인데 상태가 백색 턴이면 문제)
            if (threadSafeState.IsWhiteTurn != chessManager.IsWhiteTurn())
            {
                threadSafeState.IsWhiteTurn = chessManager.IsWhiteTurn();
            }
            
            // 합법적인 이동 확인
            var possibleMoves = stateManager.GenerateLegalMoves(threadSafeState);
            if (possibleMoves.Count == 0)
            {
                return;
            }
            
            // 트랜스포지션 테이블 크기 관리
            if (transpositionTable.Count > MAX_TRANSPOSITION_SIZE)
            {
                transpositionTable.Clear();
            }
            
            if (useIterativeDeepening)
            {
                // 반복 심화 탐색 수행
                for (int currDepth = 1; currDepth <= maxSearchDepth; currDepth++)
                {
                    if (cancelToken.IsCancellationRequested) 
                    {
                        break;
                    }
                    
                    currentMaxDepth = currDepth;
                    var newBestMove = FindBestMove(threadSafeState, currDepth, cancelToken);
                LogSafe("새로운 움직임 찾음: " + newBestMove);


                    // 유효한 이동을 찾은 경우에만 설정
                    if (newBestMove.HasValue)
                    {
                        currentBestMove = newBestMove;
                    }
                    

                    
                    // 시간 제한 확인 - 총 thinkTime의 90%에 도달하면 중지
                    if (stopwatch.ElapsedMilliseconds > (thinkTime * 1000 * 0.9))
                    {
                        
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
            selectedMove = (Move)currentBestMove;
            LogSafe($"{playerName}의 이동 완료");

            return;
        }

                // 최선의 이동을 전혀 찾지 못한 경우만 랜덤 이동 선택
              
                List<Move> legalMoves = stateManager.GenerateLegalMoves(threadSafeState);
                if (legalMoves.Count > 0)
                {
                    selectedMove = legalMoves[random.Next(0, legalMoves.Count)];

                }
        return;
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

        
        var legalMoves = stateManager.GenerateLegalMoves(state);
        
        // MVV-LVA로 이동 정렬
        stateManager.SortMovesByMVVLVA(legalMoves, state);

        if (legalMoves.Count == 0) { return null; }
        
        Move bestMove = legalMoves[0]; // 초기 최선의 이동
        int bestScore = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        

        foreach (var move in legalMoves)
        {
            if (cancelToken.IsCancellationRequested) break;
            
            // 상태 복제 대신 Undo 사용
            var undoInfo = stateManager.ApplyMoveWithUndo(state, move);
            
            int score = -Minimax(state, depth - 1, alpha, beta, false, cancelToken);
            
            // 이동 되돌리기
            stateManager.UndoLastMove(state, undoInfo);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                
            }
            
            // 알파 업데이트
            alpha = Math.Max(alpha, bestScore);
        }
        
        return bestMove;
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

    private int Minimax(ChessGameState state, int depth, int alpha, int beta, bool isMaximizingPlayer, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested) return 0;

        // 노드 평가 카운터 증가
        ++nodesEvaluated;

        // 해시 키 계산
        ulong stateHash = ComputeStateHash(state);

        // 트랜스포지션 테이블 조회
        if (transpositionTable.TryGetValue(stateHash, out TranspositionEntry entry) && entry.Depth >= depth)
        {
            ++transpositionHits;
            return isMaximizingPlayer ? entry.Score : -entry.Score;
        }

        // 종료 조건: 최대 깊이 도달 또는 게임 종료
        if (depth == 0 || stateManager.IsGameOver(state))
        {
            // 최대 깊이에 도달했다면 Quiescence Search 수행
            if (depth == 0 && !stateManager.IsGameOver(state))
            {
                return QuiescenceSearch(state, alpha, beta, isMaximizingPlayer, cancelToken);
            }

            // 평가 함수는 항상 백색 관점에서 점수 계산
            int score = EvaluatePosition(state);

            // 흑색 차례면 관점 반전
            if (!state.IsWhiteTurn)
                score = -score;

            // 현재 플레이어의 관점으로 변환
            int finalScore = isMaximizingPlayer ? score : -score;

            // 트랜스포지션 테이블에 결과 저장
            if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
            {
                transpositionTable[stateHash] = new TranspositionEntry
                {
                    Depth = depth,
                    Score = isMaximizingPlayer ? finalScore : -finalScore, // 항상 Max 플레이어 관점으로 저장
                    BestMove = null
                };
            }

            return finalScore;
        }

        List<Move> legalMoves = stateManager.GenerateLegalMoves(state);

        // MVV-LVA로 이동 정렬
        if (legalMoves.Count > 0)
        {
            stateManager.SortMovesByMVVLVA(legalMoves, state);
        }

        // 합법적인 이동이 없으면 평가
        if (legalMoves.Count == 0)
        {
            int score = EvaluatePosition(state);

            // 흑색 차례면 관점 반전
            if (!state.IsWhiteTurn)
                score = -score;

            // 현재 플레이어의 관점으로 변환
            int finalScore = isMaximizingPlayer ? score : -score;

            if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
            {
                transpositionTable[stateHash] = new TranspositionEntry
                {
                    Depth = depth,
                    Score = isMaximizingPlayer ? finalScore : -finalScore,
                    BestMove = null
                };
            }

            return finalScore;
        }

        Move? bestMove = null;
        int bestScore = int.MinValue;

        foreach (var move in legalMoves)
        {
            if (cancelToken.IsCancellationRequested) break;

            var undoInfo = stateManager.ApplyMoveWithUndo(state, move);

            // 부호를 뒤집고 알파와 베타를 뒤집어 재귀 호출 (Negamax 패턴)
            int score = -Minimax(state, depth - 1, -beta, -alpha, !isMaximizingPlayer, cancelToken);

            stateManager.UndoLastMove(state, undoInfo);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, bestScore);

            // 알파-베타 가지치기
            if (alpha >= beta)
                break;
        }

        // 트랜스포지션 테이블에 결과 저장
        if (transpositionTable.Count < MAX_TRANSPOSITION_SIZE)
        {
            transpositionTable[stateHash] = new TranspositionEntry
            {
                Depth = depth,
                Score = bestScore,
                BestMove = bestMove
            };
        }

        return bestScore;
    }

    // Quiescence Search (정적 탐색) - 오직 캡처 이동만 고려
    private int QuiescenceSearch(ChessGameState state, int alpha, int beta, bool isMaximizingPlayer, CancellationToken cancelToken)
    {
        if (cancelToken.IsCancellationRequested) return 0;
        LogSafe("퀴에센스 탐색 시작: 알파 = " + alpha + ", 베타 = " + beta);
        nodesEvaluated++;

        // 현재 상태 평가
        int standPat = EvaluatePosition(state);

        // Beta 컷 확인
        if (standPat >= beta)
            return beta;

        // Alpha 값 업데이트
        if (standPat > alpha)
            alpha = standPat;

        // 캡처 이동만 생성 (새 리스트에 복사)
        List<Move> captureMoves = new List<Move>(stateManager.GenerateLegalMoves(state, false));

        if (captureMoves.Count > 0)
        {
            stateManager.SortMovesByMVVLVA(captureMoves, state);
        }

        foreach (var move in captureMoves)
        {
            if (cancelToken.IsCancellationRequested)
                break;

            var undoInfo = stateManager.ApplyMoveWithUndo(state, move);
            int score;


                score = -QuiescenceSearch(state, -beta, -alpha, !isMaximizingPlayer, cancelToken);

                stateManager.UndoLastMove(state, undoInfo);



            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
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
            return -10000; 
        }

        if (state.CurrentGameState == ChessGameState.GameState.Stalemate ||
            state.CurrentGameState == ChessGameState.GameState.DrawByFiftyMoveRule)
        {
            return 0; // 무승부는 0점
        }



        int score = 0;

        // 기물 가치 설정
        int pawnValue = 100;
        int knightValue = 320;
        int bishopValue = 330;
        int rookValue = 500;
        int queenValue = 900;

        // 백색 기물 점수 계산
        int whiteMaterial =
            BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN]) * pawnValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT]) * knightValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP]) * bishopValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_ROOK]) * rookValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_QUEEN]) * queenValue;

        // 흑색 기물 점수 계산
        int blackMaterial =
            BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN]) * pawnValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT]) * knightValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP]) * bishopValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_ROOK]) * rookValue +
            BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_QUEEN]) * queenValue;

        // 절대적인 백색 입장에서 평가 
        score += whiteMaterial - blackMaterial;

        // 기물 위치 보너스 점수 (센터 지배, 발전 등)
        score +=  CalculatePositionalBonus(state);

        // 공격/방어 점수 (체크, 핀 등)
        score += CalculateAttackDefenseBonus(state);

        // 킹 안전 점수
        score += CalculateKingSafetyBonus(state);

        return score;
    }

    // 기물 위치에 따른 보너스 점수 계산
    private int CalculatePositionalBonus(ChessGameState state)
    {
        int bonus = 0;

        // 확장된 센터 영역 (중앙 16칸)
        ulong extendedCenterSquares = 0x00003C3C3C3C0000UL;


        // 센터 지배 보너스
        if (state.FullMoveCounter < 8)
        {
            // 초반 단계에서는 센터 지배가 중요
            bonus += 5 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN] & extendedCenterSquares);
            bonus -= 5 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN] & extendedCenterSquares);

            bonus += 10 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT] & extendedCenterSquares);
            bonus -= 10 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT] & extendedCenterSquares);

            bonus += 8 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP] & extendedCenterSquares);
            bonus -= 8 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP] & extendedCenterSquares);

            // 나이트에 대한 추가 보너스: 외곽에 있으면 패널티
            ulong edgeSquares = 0xFF818181818181FFUL;
            bonus -= 14 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT] & edgeSquares);
            bonus += 14 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT] & edgeSquares);
        }
        else
        {
            // 중반 이후에는 센터 지배가 덜 중요
            bonus += 3 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN] & extendedCenterSquares);
            bonus -= 3 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN] & extendedCenterSquares);

            bonus += 6 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT] & extendedCenterSquares);
            bonus -= 6 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT] & extendedCenterSquares);

            bonus += 4 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP] & extendedCenterSquares);
            bonus -= 4 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP] & extendedCenterSquares);

            // 나이트에 대한 추가 보너스: 외곽에 있으면 패널티
            ulong edgeSquares = 0xFF818181818181FFUL;
            bonus -= 10 * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_KNIGHT] & edgeSquares);
            bonus += 10 * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_KNIGHT] & edgeSquares);
        }


        // 백색 폰 전진 보너스 (더 앞쪽에 있을수록 가치 증가)
        for (int rank = 1; rank < 7; rank++)
        {
            ulong rankMask = 0xFFUL << (rank * 8);
            int rankBonus = rank * 2; // 높은 랭크일수록 더 큰 보너스
            bonus += rankBonus * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN] & rankMask);
            if(BitHelper.CountBits(state.whiteAttackMap & state.BitBoards[ChessGameState.WHITE_PAWN]) < BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN]) && state.FullMoveCounter >3)
            {
                bonus -= rankBonus * BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_PAWN] & rankMask) + 2; // 공격 가능한 폰은 보너스 감소
            }
        }

        // 흑색 폰 전진 보너스
        for (int rank = 6; rank > 0; rank--)
        {
            ulong rankMask = 0xFFUL << (rank * 8);
            int rankBonus = (7 - rank) * 2; // 낮은 랭크일수록 더 큰 보너스
            bonus -= rankBonus * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN] & rankMask);
            if (BitHelper.CountBits(state.blackAttackMap & state.BitBoards[ChessGameState.BLACK_PAWN]) < BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN]) && state.FullMoveCounter > 3)
            {
                bonus += rankBonus * BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_PAWN] & rankMask) + 2; // 공격 가능한 폰은 보너스 감소
            }
        }

        // 비숍 페어 보너스
        if (BitHelper.CountBits(state.BitBoards[ChessGameState.WHITE_BISHOP]) >= 2)
        {
            bonus += 12;
        }
        if (BitHelper.CountBits(state.BitBoards[ChessGameState.BLACK_BISHOP]) >= 2)
        {
            bonus -= 12;
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
            //오프닝 제외
            if(state.FullMoveCounter < 8)
            {
                bonus = 1;
            }
            else
            {
                bonus += 31; // 상대를 체크 상태로 만들면 보너스

            }
        }
        if (state.IsInCheck(state.IsWhiteTurn))
        {
            bonus -= 16; // 자신이 체크 상태면 패널티
        }

        // 핀된 기물 보너스/패널티
        int whitePinnedCount = BitHelper.CountBits(state.PinnedPieces & state.WhitePieces);
        int blackPinnedCount = BitHelper.CountBits(state.PinnedPieces & state.BlackPieces);
        bonus -= whitePinnedCount * 12;
        bonus += blackPinnedCount * 12;

        return bonus;
    }

    // 킹 안전 관련 보너스 계산
    private int CalculateKingSafetyBonus(ChessGameState state)
    {
        int bonus = 0;

        // 백색 킹 주변의 아군 기물 수

        int whiteKingProtection = BitHelper.CountBits(state.whiteKingAttackMap & state.WhitePieces);

        // 흑색 킹 주변의 아군 기물 수
        int blackKingProtection = BitHelper.CountBits(state.blackKingAttackMap & state.BlackPieces);

        // 킹 보호 보너스
        bonus += whiteKingProtection * 4;
        bonus -= blackKingProtection * 4;

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


    
    // 강제 이동 선택 메서드 개선
    protected override void ForceMoveSelection()
    {
        CancelThinking();

        // 스톱워치 있다면 정지
        if (stopwatch.IsRunning)
        {
            stopwatch.Stop();
        }
        
        // 현재까지의 최선의 이동 선택
        if (currentBestMove.HasValue)
        {

            selectedMove = currentBestMove.Value;
            ValidateAndExecuteMove();
        }
        else
        {
               LogSafe("[미니맥스 AI] 현재까지의 최선의 이동이 존재하지 않습니다");
        }
    }
    public override void SetDebugMode(bool enabled)
    {
        debugMode = enabled;
        

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
            LogSafe($"[미니맥스 AI] OnDisable 중 예외 발생: {ex.Message}");
        }
    }
    

    
    // 게임 상태 로깅 함수
    private void LogGameState(ChessGameState state)
    {
        LogSafe("===== 현재 게임 상태 =====");
        
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
            LogSafe($"BitBoard[{i}] {pieceName}: {state.BitBoards[i]}");
        }
        
        // 현재 턴 정보
        LogSafe($"현재 턴: {(state.IsWhiteTurn ? "백색" : "흑색")}");
        LogSafe($"게임 상태: {state.CurrentGameState}");
        
        // 캐슬링 권한
        LogSafe($"캐슬링 권한: 백킹측({state.WhiteKingSideCastleRight}), 백퀸측({state.WhiteQueenSideCastleRight}), 흑킹측({state.BlackKingSideCastleRight}), 흑퀸측({state.BlackQueenSideCastleRight})");
        
        // 앙파상 정보
        LogSafe($"앙파상 타겟: {state.EnPassantTargetSquare}");
        LogSafe($"50수 카운터: {state.FiftyMoveCounter}, 전체 수 카운터: {state.FullMoveCounter}");
        
        // 체스판 상태 출력
        LogSafe("체스판 상태:");
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
        LogSafe(boardVisualization);
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