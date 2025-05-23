using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// 체스 상태 관리 및 이동 생성을 전담하는 경량화된 관리자 클래스
/// AI가 깊은 탐색을 할 때 효율적으로 상태를 조작하고 이동을 생성할 수 있도록 최적화됨
/// </summary>
public class ChessStateCalculator
{
    private MoveGenerator moveGenerator;
    
    // 이동 정렬 기능 추가
    public void SortMovesByMVVLVA(List<Move> moves, ChessGameState state)
    {
        if (moves == null || moves.Count <= 1)
            return;
            
        try
        {
            moveGenerator.SortMovesByMVVLVA(moves, state);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"이동 정렬 중 오류 발생: {ex.Message}. 정렬을 건너뜁니다.");
            // 오류 발생 시 정렬 건너뛰기 (기본 순서 유지)
        }
    }

    
    public ChessStateCalculator()
    {
        moveGenerator = new MoveGenerator();
    }
    
    /// <summary>
    /// 주어진 체스 상태에 대한 모든 합법적인 이동을 생성합니다.
    /// </summary>
    /// <param name="state">체스 상태</param>
    /// <param name="includeQuietMoves">비공격 이동을 포함할지 여부</param>
    /// <returns>합법적인 이동 목록</returns>
    public List<Move> GenerateLegalMoves(ChessGameState state, bool includeQuietMoves = true)
    {
        // 먼저 체크 상태 정보 업데이트 확인
        state.UpdateAttackMaps();
        state.UpdatePinInformation();
        state.UpdateCheckInformation();
        
        // 이제 이동 생성
        return moveGenerator.GenerateLegalMoves(state, includeQuietMoves);
    }
    
    /// <summary>
    /// 이동을 적용한 새로운 체스 상태를 반환합니다.
    /// 원본 상태는 변경되지 않습니다.
    /// </summary>
    /// <param name="state">원본 체스 상태</param>
    /// <param name="move">적용할 이동</param>
    /// <returns>새로운 체스 상태</returns>
    public ChessGameState ApplyMove(ChessGameState state, Move move)
    {
        // 상태 복제
        ChessGameState newState = state.Clone();
        
        // 이동 적용
        ApplyMoveToState(newState, move);
        
        return newState;
    }
    
    /// <summary>
    /// 체스 상태에 이동을 직접 적용합니다.
    /// 원본 상태가 변경됩니다.
    /// </summary>
    /// <param name="state">체스 상태</param>
    /// <param name="move">적용할 이동</param>
    public void ApplyMoveToState(ChessGameState state, Move move)
    {
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;
        int flag = move.Flag;
        
        // 기본 이동 실행
        state.MovePiece(fromSquare, toSquare);
        
        // 특수 이동 처리
        HandleSpecialMove(state, move);
        
        // 캐슬링 권한 업데이트
        UpdateCastlingRights(state, fromSquare, toSquare);
        
        // 차례 변경
        state.SwitchTurn();
        
        // 앙파상 타겟 초기화 (폰 두 칸 이동이 아닌 경우)
        if (!move.IsPawnTwoUp)
        {
            state.EnPassantTargetSquare = -1;
        }
        
        // 공격 맵과 체크 정보 업데이트
        UpdateGameStateInformation(state);
        
        // 50수 규칙 카운터 업데이트
        UpdateFiftyMoveCounter(state, toSquare);
        
        // 수 카운터 업데이트
        UpdateMoveCounter(state);
        
        // 게임 상태 업데이트 (체크, 체크메이트, 스테일메이트 등)
        UpdateGameState(state);
    }
    
    /// <summary>
    /// 이동이 합법적인지 확인합니다.
    /// </summary>
    /// <param name="state">체스 상태</param>
    /// <param name="move">확인할 이동</param>
    /// <returns>합법적인 이동인지 여부</returns>
    public bool IsValidMove(ChessGameState state, Move move)
    {
        // 가능한 모든 이동 목록 생성
        List<Move> legalMoves = GenerateLegalMoves(state);
        
        // 목록에서 일치하는 이동 찾기
        foreach (var legalMove in legalMoves)
        {
            if (legalMove.FromSquare == move.FromSquare && legalMove.ToSquare == move.ToSquare)
            {
                // 프로모션 확인
                if (legalMove.IsPromotion && move.IsPromotion)
                {
                    return legalMove.Flag == move.Flag;
                }
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 게임이 종료되었는지 확인합니다.
    /// </summary>
    /// <param name="state">체스 상태</param>
    /// <returns>게임 종료 여부</returns>
    public bool IsGameOver(ChessGameState state)
    {
        return state.CurrentGameState == ChessGameState.GameState.Checkmate || 
               state.CurrentGameState == ChessGameState.GameState.Stalemate || 
               state.CurrentGameState == ChessGameState.GameState.DrawByFiftyMoveRule ||
               state.CurrentGameState == ChessGameState.GameState.DrawByRepetition ||
               state.CurrentGameState == ChessGameState.GameState.DrawByInsufficientMaterial;
    }
    
    /// <summary>
    /// 이동을 적용하고 되돌릴 수 있는 정보를 저장합니다.
    /// 미니맥스 탐색에 최적화된 방식입니다.
    /// </summary>
    public MoveUndoInfo ApplyMoveWithUndo(ChessGameState state, Move move)
    {
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;
        
        // 되돌리기 위한 정보 저장
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
        
        // 되돌리기 위한 정보 반환
        return undoInfo;
    }
    
    /// <summary>
    /// 마지막 이동을 되돌립니다. 최적화된 상태 관리를 위해 사용됩니다.
    /// </summary>
    public void UndoLastMove(ChessGameState state, MoveUndoInfo undoInfo)
    {
        Move move = undoInfo.Move;
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;
        
        // 특수 이동 처리 (캐슬링, 앙파상, 프로모션)
        if (move.HasFlag(Move.KingSideCastleFlag) || move.HasFlag(Move.QueenSideCastleFlag))
        {
            UndoCastling(state, move, undoInfo.WasWhiteTurn);
        }
        else if (move.HasFlag(Move.EnPassantCaptureFlag))
        {
            UndoEnPassant(state, move, undoInfo.WasWhiteTurn);
        }
        else if (move.IsPromotion)
        {
            UndoPromotion(state, move, undoInfo.WasWhiteTurn, undoInfo.CapturedPiece);
        }
        else
        {
            // 일반 이동 되돌리기
            int movedPiece = state.GetPieceAt(toSquare);
            state.PlacePiece(fromSquare, movedPiece);
            state.PlacePiece(toSquare, undoInfo.CapturedPiece);
        }
        
        // 상태 복원
        state.IsWhiteTurn = undoInfo.WasWhiteTurn;
        state.EnPassantTargetSquare = undoInfo.EnPassantTarget;
        state.WhiteKingSideCastleRight = undoInfo.WhiteKingSideCastle;
        state.WhiteQueenSideCastleRight = undoInfo.WhiteQueenSideCastle;
        state.BlackKingSideCastleRight = undoInfo.BlackKingSideCastle;
        state.BlackQueenSideCastleRight = undoInfo.BlackQueenSideCastle;
        state.FiftyMoveCounter = undoInfo.FiftyMoveCounter;
        state.CurrentGameState = undoInfo.PreviousGameState;
        
        // 공격 맵 업데이트
        state.UpdateAttackMaps();
        state.UpdatePinInformation();
        state.UpdateCheckInformation();
        
        // 디버그 로그 추가
        //Debug.Log($"이동 되돌림: 현재 턴은 {(state.IsWhiteTurn ? "백색" : "흑색")}입니다.");
    }
    
    /// <summary>
    /// 캐슬링 이동 되돌리기
    /// </summary>
    private void UndoCastling(ChessGameState state, Move move, bool wasWhiteTurn)
    {
        // 킹 위치 복원
        int kingFrom = move.FromSquare;
        int kingTo = move.ToSquare;
        int kingPiece = state.GetPieceAt(kingTo);
        state.PlacePiece(kingFrom, kingPiece);
        state.ClearSquare(kingTo);
        
        // 룩 위치 복원
        if (move.HasFlag(Move.KingSideCastleFlag))
        {
            int rookFrom = wasWhiteTurn ? 7 : 63;
            int rookTo = wasWhiteTurn ? 5 : 61;
            int rookPiece = state.GetPieceAt(rookTo);
            state.PlacePiece(rookFrom, rookPiece);
            state.ClearSquare(rookTo);
        }
        else if (move.HasFlag(Move.QueenSideCastleFlag))
        {
            int rookFrom = wasWhiteTurn ? 0 : 56;
            int rookTo = wasWhiteTurn ? 3 : 59;
            int rookPiece = state.GetPieceAt(rookTo);
            state.PlacePiece(rookFrom, rookPiece);
            state.ClearSquare(rookTo);
        }
    }
    
    /// <summary>
    /// 앙파상 이동 되돌리기
    /// </summary>
    private void UndoEnPassant(ChessGameState state, Move move, bool wasWhiteTurn)
    {
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;
        int pawnPiece = state.GetPieceAt(toSquare);
        
        // 이동한 폰 원위치
        state.PlacePiece(fromSquare, pawnPiece);
        state.ClearSquare(toSquare);
        
        // 잡힌 폰 복원
        int capturedPawnSquare = wasWhiteTurn ? toSquare - 8 : toSquare + 8;
        int capturedPawn = wasWhiteTurn ? pieceNum.black | pieceNum.pwan : pieceNum.white | pieceNum.pwan;
        state.PlacePiece(capturedPawnSquare, capturedPawn);
    }
    
    /// <summary>
    /// 프로모션 이동 되돌리기
    /// </summary>
    private void UndoPromotion(ChessGameState state, Move move, bool wasWhiteTurn, int capturedPiece)
    {
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;
        
        // 프로모션된 기물 제거
        state.ClearSquare(toSquare);
        
        // 원래 폰 복원
        int pawnPiece = wasWhiteTurn ? pieceNum.white | pieceNum.pwan : pieceNum.black | pieceNum.pwan;
        state.PlacePiece(fromSquare, pawnPiece);
        
        // 잡힌 기물이 있었다면 복원
        if (capturedPiece != 0)
        {
            state.PlacePiece(toSquare, capturedPiece);
        }
    }
    
    /// <summary>
    /// 특수 이동(앙파상, 캐슬링, 프로모션 등)을 처리합니다.
    /// </summary>
    private void HandleSpecialMove(ChessGameState state, Move move)
    {
        int toSquare = move.ToSquare;
        
        // 앙파상 처리
        if (move.IsEnPassant)
        {
            int capturedPawnSquare = state.IsWhiteTurn ? toSquare - 8 : toSquare + 8;
            state.ClearSquare(capturedPawnSquare);
        }
        // 킹 사이드 캐슬링 처리
        else if (move.HasFlag(Move.KingSideCastleFlag))
        {
            int rookFromSquare = state.IsWhiteTurn ? 7 : 63;
            int rookToSquare = state.IsWhiteTurn ? 5 : 61;
            state.MovePiece(rookFromSquare, rookToSquare);
        }
        // 퀸 사이드 캐슬링 처리
        else if (move.HasFlag(Move.QueenSideCastleFlag))
        {
            int rookFromSquare = state.IsWhiteTurn ? 0 : 56;
            int rookToSquare = state.IsWhiteTurn ? 3 : 59;
            state.MovePiece(rookFromSquare, rookToSquare);
        }
        // 프로모션 처리
        else if (move.IsPromotion)
        {
            HandlePromotion(state, move, toSquare);
        }
        // 폰 두 칸 이동 처리
        else if (move.IsPawnTwoUp)
        {
            int epSquare = state.IsWhiteTurn ? toSquare - 8 : toSquare + 8;
            state.EnPassantTargetSquare = epSquare;
        }
    }
    
    /// <summary>
    /// 프로모션 처리를 수행합니다.
    /// </summary>
    private void HandlePromotion(ChessGameState state, Move move, int toSquare)
    {
        int promotionPiece = 0;
        
        if (move.IsQueenPromotion)
        {
            promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.queen : pieceNum.black | pieceNum.queen; // 올바른 pieceNum 상수 사용
        }
        else if (move.IsRookPromotion)
        {
            promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.rook : pieceNum.black | pieceNum.rook; // 올바른 pieceNum 상수 사용
        }
        else if (move.IsBishopPromotion)
        {
            promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.bishop : pieceNum.black | pieceNum.bishop; // 올바른 pieceNum 상수 사용
        }
        else if (move.IsKnightPromotion)
        {
            promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.knight : pieceNum.black | pieceNum.knight; // 올바른 pieceNum 상수 사용
        }
        
        // 디버그 로그 추가
        Debug.Log($"프로모션: {move.FromSquare}에서 {toSquare}로, 기물 값: {promotionPiece}");
        
        // 폰을 제거하고 새 기물 배치
        state.ClearSquare(toSquare);
        state.PlacePiece(toSquare, promotionPiece);
    }
    
    /// <summary>
    /// 게임 상태 정보를 업데이트합니다.
    /// </summary>
    private void UpdateGameStateInformation(ChessGameState state)
    {
        state.UpdateAttackMaps();
        state.UpdatePinInformation();
        state.UpdateCheckInformation();
    }
    
    /// <summary>
    /// 50수 규칙 카운터를 업데이트합니다.
    /// </summary>
    private void UpdateFiftyMoveCounter(ChessGameState state, int toSquare)
    {
        // 캡처 또는 폰 이동 확인
        bool wasPieceCaptured = false;
        bool wasPawnMoved = false;
        
        int pieceAtTarget = state.GetPieceAt(toSquare);
        
        // 폰 이동 확인 (비트 마스크로 폰 확인)
        wasPawnMoved = (pieceAtTarget & 1) != 0; // 폰은 1 비트가 설정됨
        
        if (wasPieceCaptured || wasPawnMoved)
        {
            state.FiftyMoveCounter = 0;
        }
        else
        {
            state.FiftyMoveCounter++;
        }
    }
    
    /// <summary>
    /// 수 카운터를 업데이트합니다.
    /// </summary>
    private void UpdateMoveCounter(ChessGameState state)
    {
        if (!state.IsWhiteTurn)
        {
            state.FullMoveCounter++;
        }
    }
    
    /// <summary>
    /// 캐슬링 권한을 업데이트합니다.
    /// </summary>
    private void UpdateCastlingRights(ChessGameState state, int fromSquare, int toSquare)
    {
        // 킹 이동
        if (fromSquare == 4) // 백색 킹
        {
            state.WhiteKingSideCastleRight = false;
            state.WhiteQueenSideCastleRight = false;
        }
        else if (fromSquare == 60) // 흑색 킹
        {
            state.BlackKingSideCastleRight = false;
            state.BlackQueenSideCastleRight = false;
        }
        
        // 룩 이동
        else if (fromSquare == 0) // 백색 퀸사이드 룩
            state.WhiteQueenSideCastleRight = false;
        else if (fromSquare == 7) // 백색 킹사이드 룩
            state.WhiteKingSideCastleRight = false;
        else if (fromSquare == 56) // 흑색 퀸사이드 룩
            state.BlackQueenSideCastleRight = false;
        else if (fromSquare == 63) // 흑색 킹사이드 룩
            state.BlackKingSideCastleRight = false;
        
        // 룩 캡처
        if (toSquare == 0) // 백색 퀸사이드 룩
            state.WhiteQueenSideCastleRight = false;
        else if (toSquare == 7) // 백색 킹사이드 룩
            state.WhiteKingSideCastleRight = false;
        else if (toSquare == 56) // 흑색 퀸사이드 룩
            state.BlackQueenSideCastleRight = false;
        else if (toSquare == 63) // 흑색 킹사이드 룩
            state.BlackKingSideCastleRight = false;
    }
    
    /// <summary>
    /// 게임 상태를 업데이트합니다.
    /// </summary>
    private void UpdateGameState(ChessGameState state)
    {
        // 공격 맵 업데이트
        state.UpdateAttackMaps();
        state.UpdatePinInformation();
        state.UpdateCheckInformation();
        
        // 체크 여부 확인
        bool isInCheck = state.IsInCheck(state.IsWhiteTurn);
        
        // 합법적 이동 생성
        List<Move> legalMoves = moveGenerator.GenerateLegalMoves(state);
        
        // 가능한 이동이 있는지 확인
        bool hasLegalMoves = legalMoves.Count > 0;
        
        // 게임 상태 결정
        if (isInCheck)
        {
            if (!hasLegalMoves)
            {
                // 체크메이트: 체크 상태이고 합법적인 이동이 없음
                state.CurrentGameState = ChessGameState.GameState.Checkmate;
                
            }
            else
            {
                // 체크: 체크 상태이지만 합법적인 이동이 있음
                state.CurrentGameState = ChessGameState.GameState.Check;
            }
        }
        else
        {
            if (!hasLegalMoves)
            {
                // 스테일메이트: 체크 상태가 아니지만 합법적인 이동이 없음
                state.CurrentGameState = ChessGameState.GameState.Stalemate;
                
            }
            else
            {
                // 일반 플레이: 체크 상태가 아니고 합법적인 이동이 있음
                state.CurrentGameState = ChessGameState.GameState.Playing;
            }
        }
        
        // 50수 규칙 확인
        if (state.FiftyMoveCounter >= 100) // 50수 = 100 반수
        {
            state.CurrentGameState = ChessGameState.GameState.DrawByFiftyMoveRule;
        }
    }
} 