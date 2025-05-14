using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[Serializable]
public class ChessGameState
{
    // 비트보드 기반 보드 상태
    // 각 기물 타입과 색상에 대한 비트보드
    public ulong[] BitBoards = new ulong[12]; // 0-5: 백색 (폰, 나이트, 비숍, 룩, 퀸, 킹), 6-11: 흑색 (폰, 나이트, 비숍, 룩, 퀸, 킹)
    
    // 색상별 비트보드 (흰색/검은색 기물 전체)
    public ulong WhitePieces => BitBoards[0] | BitBoards[1] | BitBoards[2] | BitBoards[3] | BitBoards[4] | BitBoards[5];
    public ulong BlackPieces => BitBoards[6] | BitBoards[7] | BitBoards[8] | BitBoards[9] | BitBoards[10] | BitBoards[11];
    
    // 모든 기물의 비트보드
    public ulong AllPieces => WhitePieces | BlackPieces;

    // 기물 인덱스 상수
    public const int WHITE_PAWN = 0;
    public const int WHITE_KNIGHT = 1;
    public const int WHITE_BISHOP = 2;
    public const int WHITE_ROOK = 3;
    public const int WHITE_QUEEN = 4;
    public const int WHITE_KING = 5;
    public const int BLACK_PAWN = 6;
    public const int BLACK_KNIGHT = 7;
    public const int BLACK_BISHOP = 8;
    public const int BLACK_ROOK = 9;
    public const int BLACK_QUEEN = 10;
    public const int BLACK_KING = 11;
    
    // 비트보드와 호환성을 위한 배열 형태의 보드
    public int[] Board = new int[64];
    
    // 게임 상태
    public bool IsWhiteTurn = false;
    
    // 게임 상태 열거형
    public enum GameState
    {
        Playing,
        Check,
        Checkmate,
        Stalemate,
        DrawByRepetition,
        DrawByFiftyMoveRule,
        DrawByInsufficientMaterial
    }
    
    public GameState CurrentGameState = GameState.Playing;
    
    // 캐슬링 가능 여부
    public bool WhiteKingSideCastleRight = true;
    public bool WhiteQueenSideCastleRight = true;
    public bool BlackKingSideCastleRight = true;
    public bool BlackQueenSideCastleRight = true;
    
    // 앙파상 가능한 폰의 위치 (-1이면 불가능)
    public int EnPassantTargetSquare = -1;
    
    // 50수 규칙 카운터 (폰 이동이나 말 잡기가 없을 때 증가)
    public int FiftyMoveCounter = 0;
    
    // 전체 무브 카운터
    public int FullMoveCounter = 1;
    
    // 프로모션 관련
    public bool IsPromotionPending = false;
    public int PromotionSquare = -1;
    
    // 이동 기록 (기보 작성 및 무승부 판정용)
    // 비트 0-3은 흰색과 검은색의 킹사이드/퀸사이드 캐슬링 가능 여부 저장
    // 비트 4-7은 앙파상 가능한 폰의 파일 위치 저장 (1부터 시작, 0은 앙파상 불가능)
    // 비트 8-13은 잡힌 기물 정보
    // 비트 14-...는 50수 규칙 카운터
    public Stack<uint> MoveHistory = new Stack<uint>();

    // 킹 위치 캐시 (체크/체크메이트 빠른 확인용)
    private int whiteKingSquare = 4;
    private int blackKingSquare = 60;

    // 캐시된 킹 위치 접근자
    public int WhiteKingSquare => whiteKingSquare;
    public int BlackKingSquare => blackKingSquare;

    // 공격 비트보드 (각 색상의 모든 기물이 공격하는 위치)
    private ulong WhiteAttackMap = 0UL; // 흰색 기물의 공격 비트보드
    public ulong whiteAttackMap { get { return WhiteAttackMap; } set { WhiteAttackMap = value; } }

    private ulong BlackAttackMap = 0UL; // 검은색 기물의 공격 비트보드
    public ulong blackAttackMap { get { return BlackAttackMap; } set { BlackAttackMap = value; } }

    //기물별 공격 비트보드
    private ulong WhitePawnAttackMap = 0UL;
    public ulong whitePawnAttackMap { get { return WhitePawnAttackMap; } set { WhitePawnAttackMap = value; } }
    private ulong BlackPawnAttackMap = 0UL;
    public ulong blackPawnAttackMap { get { return BlackPawnAttackMap; } set { BlackPawnAttackMap = value; } }
    private ulong WhiteKnightAttackMap = 0UL;
    public ulong whiteKnightAttackMap { get { return WhiteKnightAttackMap; } set { WhiteKnightAttackMap = value; } }
    private ulong BlackKnightAttackMap = 0UL;
    public ulong blackKnightAttackMap { get { return BlackKnightAttackMap; } set { BlackKnightAttackMap = value; } }
    private ulong WhiteBishopAttackMap = 0UL;
    public ulong whiteBishopAttackMap { get { return WhiteBishopAttackMap; } set { WhiteBishopAttackMap = value; } }
    private ulong BlackBishopAttackMap = 0UL;
    public ulong blackBishopAttackMap { get { return BlackBishopAttackMap; } set { BlackBishopAttackMap = value; } }
    private ulong WhiteRookAttackMap = 0UL;
    public ulong whiteRookAttackMap { get { return WhiteRookAttackMap; } set { WhiteRookAttackMap = value; } }
    private ulong BlackRookAttackMap = 0UL;
    public ulong blackRookAttackMap { get { return BlackRookAttackMap; } set { BlackRookAttackMap = value; } }
    private ulong WhiteQueenAttackMap = 0UL;
    public ulong whiteQueenAttackMap { get { return WhiteQueenAttackMap; } set { WhiteQueenAttackMap = value; } }
    private ulong BlackQueenAttackMap = 0UL;
    public ulong blackQueenAttackMap { get { return BlackQueenAttackMap; } set { BlackQueenAttackMap = value; } }
    private ulong WhiteKingAttackMap = 0UL;
    public ulong whiteKingAttackMap { get { return WhiteKingAttackMap; } set { WhiteKingAttackMap = value; } }
    private ulong BlackKingAttackMap = 0UL;
    public ulong blackKingAttackMap { get { return BlackKingAttackMap; } set { BlackKingAttackMap = value; } }

    // 핀 관련 비트보드
    public ulong PinnedPieces = 0UL;        // 핀된 모든 기물
    public ulong PinnedMovesMask = 0UL;     // 핀된 기물이 이동할 수 있는 방향 마스크

    // 체크 관련 비트보드
    private ulong CheckingPieces  = 0UL;      // 체크를 가하는 기물
    public ulong checkingPieces => CheckingPieces; // 체크를 가하는 기물이 이동할 수 있는 방향 마스크
    private ulong CheckBlockingMask =0UL;   // 체크를 방어할 수 있는 위치 마스크

    public void SwitchTurn(){
        IsWhiteTurn = !IsWhiteTurn;
    }
    // 기물 검색 유틸리티
    public int GetPieceAt(int square)
    {
        if (square < 0 || square >= 64) return 0;
        return Board[square];
    }
    
    // 특정 위치에 비트가 설정되어 있는지 확인
    public bool IsBitSet(int bitboardIndex, int square)
    {
        if (bitboardIndex < 0 || bitboardIndex >= 12 || square < 0 || square >= 64) return false;
        return BitHelper.IsBitSet(BitBoards[bitboardIndex], square);
    }
    
    // 특정 위치에 특정 색상의 기물이 있는지 확인
    public bool IsWhitePieceAt(int square)
    {
        ulong bit = BitHelper.SetBit(square);
        return (WhitePieces & bit) != 0;
    }
    
    public bool IsBlackPieceAt(int square)
    {
        ulong bit = BitHelper.SetBit(square);
        return (BlackPieces & bit) != 0;
    }
    
    // 특정 위치에 기물이 있는지 확인
    public bool IsSquareOccupied(int square)
    {
        ulong bit = BitHelper.SetBit(square);
        return (AllPieces & bit) != 0;
    }
    
    // 특정 위치에 어떤 비트보드 인덱스의 기물이 있는지 찾기
    public int FindPieceBitboardIndex(int square)
    {
        if (square < 0 || square >= 64) return -1;
        
        ulong bit = BitHelper.SetBit(square);
        for (int i = 0; i < 12; i++)
        {
            if ((BitBoards[i] & bit) != 0)
            {
                return i;
            }
        }
        
        return -1; // 기물 없음
    }
    
    // 비트보드 인덱스를 pieceNum 값으로 변환
    public int BitboardIndexToPieceNum(int bitboardIndex)
    {
        switch (bitboardIndex)
        {
            case WHITE_PAWN: return pieceNum.whitePawn;
            case WHITE_KNIGHT: return pieceNum.whiteKnight;
            case WHITE_BISHOP: return pieceNum.whiteBishop;
            case WHITE_ROOK: return pieceNum.whiteRook;
            case WHITE_QUEEN: return pieceNum.whiteQueen;
            case WHITE_KING: return pieceNum.whiteKing;
            case BLACK_PAWN: return pieceNum.blackPawn;
            case BLACK_KNIGHT: return pieceNum.blackKnight;
            case BLACK_BISHOP: return pieceNum.blackBishop;
            case BLACK_ROOK: return pieceNum.blackRook;
            case BLACK_QUEEN: return pieceNum.blackQueen;
            case BLACK_KING: return pieceNum.blackKing;
            default: return pieceNum.empty;
        }
    }
    
    // pieceNum 값을 비트보드 인덱스로 변환
    public int PieceNumToBitboardIndex(int pieceNumber)
    {
        if (pieceNumber == 0) return -1;
        
        bool isWhite = (pieceNumber & pieceNum.colorMask) == pieceNum.white;
        int pieceType = pieceNumber & pieceNum.pieceMask;
        
        int offset = isWhite ? 0 : 6;
        
        if ((pieceType & pieceNum.pwan) != 0) return offset + 0;
        if ((pieceType & pieceNum.knight) != 0) return offset + 1;
        if ((pieceType & pieceNum.bishop) != 0) return offset + 2;
        if ((pieceType & pieceNum.rook) != 0) return offset + 3;
        if ((pieceType & pieceNum.queen) != 0) return offset + 4;
        if ((pieceType & pieceNum.king) != 0) return offset + 5;
        
        return -1;
    }
    
    // 같은 종류의 다른 기물 인스턴스 생성
    public ChessGameState Clone()
    {
        ChessGameState clone = new ChessGameState();
        
        // 비트보드 복사
        for (int i = 0; i < 12; i++)
        {
            clone.BitBoards[i] = BitBoards[i];
        }
        
        // 보드 복사
        Array.Copy(Board, clone.Board, 64);
        
        // 상태 복사
        clone.IsWhiteTurn = IsWhiteTurn;
        clone.CurrentGameState = CurrentGameState;
        clone.WhiteKingSideCastleRight = WhiteKingSideCastleRight;
        clone.WhiteQueenSideCastleRight = WhiteQueenSideCastleRight;
        clone.BlackKingSideCastleRight = BlackKingSideCastleRight;
        clone.BlackQueenSideCastleRight = BlackQueenSideCastleRight;
        clone.EnPassantTargetSquare = EnPassantTargetSquare;
        clone.FiftyMoveCounter = FiftyMoveCounter;
        clone.FullMoveCounter = FullMoveCounter;
        clone.IsPromotionPending = IsPromotionPending;
        clone.PromotionSquare = PromotionSquare;
        clone.whiteKingSquare = whiteKingSquare;
        clone.blackKingSquare = blackKingSquare;
        
        // 이동 기록 복사
        foreach (var move in MoveHistory)
        {
            clone.MoveHistory.Push(move);
        }
        
        return clone;
    }
    
    // 기본 시작 상태로 초기화
    public void Reset()
    {
        // FEN 파서를 사용하여 시작 위치로 초기화
        LoadFromFEN(FENParser.StartPositionFEN);
        MoveHistory.Clear();
        CurrentGameState = GameState.Playing;
    }
    
    // FEN 문자열로 초기화
    public void LoadFromFEN(string fen)
    {
        // 비트보드 초기화
        for (int i = 0; i < 12; i++)
        {
            BitBoards[i] = 0UL;
        }
        
        // FEN 파싱 결과 얻기
        ChessGameState loadedState = FENParser.ParseGameState(fen);
        
        // FEN 파싱 결과에서 보드 상태를 비트보드로 변환
        UpdateBitboardsFromBoard(loadedState.Board);
        
        // 보드 배열 복사
        Array.Copy(loadedState.Board, Board, 64);
        
        // 상태 복사
        IsWhiteTurn = loadedState.IsWhiteTurn;
        WhiteKingSideCastleRight = loadedState.WhiteKingSideCastleRight;
        WhiteQueenSideCastleRight = loadedState.WhiteQueenSideCastleRight;
        BlackKingSideCastleRight = loadedState.BlackKingSideCastleRight;
        BlackQueenSideCastleRight = loadedState.BlackQueenSideCastleRight;
        EnPassantTargetSquare = loadedState.EnPassantTargetSquare;
        FiftyMoveCounter = loadedState.FiftyMoveCounter;
        FullMoveCounter = loadedState.FullMoveCounter;
        
        // 킹 위치 찾기
        UpdateKingPositions();
        
        // 게임 상태 리셋
        CurrentGameState = GameState.Playing;
        IsPromotionPending = false;
        PromotionSquare = -1;
    }
    
    // 보드 배열에서 비트보드 업데이트
    public void UpdateBitboardsFromBoard(int[] boardArray)
    {
        // 비트보드 초기화
        for (int i = 0; i < 12; i++)
        {
            BitBoards[i] = 0UL;
        }
        
        // 보드 배열을 순회하면서 비트보드 업데이트
        for (int square = 0; square < 64; square++)
        {
            int pieceValue = boardArray[square];
            if (pieceValue == 0) continue; // 빈 칸
            
            int pieceNumValue;
            
            // pieceNum 형식인지 확인
            if (pieceValue >= 64 || pieceValue <= -64) // pieceNum 형식 (비트 표현)
            {
                pieceNumValue = pieceValue;
            }
            else // 표준 형식 (+/- 1~6)
            {
                pieceNumValue = pieceNum.FromStandardValue(pieceValue);
            }
            
            int bitboardIndex = PieceNumToBitboardIndex(pieceNumValue);
            if (bitboardIndex >= 0)
            {
                BitBoards[bitboardIndex] |= BitHelper.SetBit(square);
            }
        }
    }
    public void UpdateBitboardsFromBitboards(ulong[] bitboards)
    {
        for (int i = 0; i < 12; i++)
        {
            BitBoards[i] = bitboards[i];
        }

    }
    

    // 비트보드에서 보드 배열 업데이트
    private void UpdateBoardFromBitboards()
    {
        // 보드 초기화
        for (int i = 0; i < 64; i++)
        {
            Board[i] = 0;
        }
        
        // 각 비트보드 순회
        for (int bitboardIndex = 0; bitboardIndex < 12; bitboardIndex++)
        {
            ulong bitboard = BitBoards[bitboardIndex];
            int pieceNumValue = BitboardIndexToPieceNum(bitboardIndex);
            
            // 비트보드에서 설정된 각 비트(기물 위치)에 대해
            while (bitboard != 0)
            {
                int square = BitHelper.GetLSBIndex(bitboard);
                Board[square] = pieceNumValue;
                bitboard = BitHelper.ClearBit(bitboard, square);
            }
        }
    }
    
    // 킹 위치 업데이트
    private void UpdateKingPositions()
    {
        if (BitBoards[WHITE_KING] != 0)
        {
            whiteKingSquare = BitHelper.GetLSBIndex(BitBoards[WHITE_KING]);
        }
        else
        {
            whiteKingSquare = -1;
        }
        
        if (BitBoards[BLACK_KING] != 0)
        {
            blackKingSquare = BitHelper.GetLSBIndex(BitBoards[BLACK_KING]);
        }
        else
        {
            blackKingSquare = -1;
        }
    }
    
    // 기물 이동
    public void MovePiece(int fromSquare, int toSquare)
    {
        int pieceNumValue = Board[fromSquare];
        int bitboardIndex = PieceNumToBitboardIndex(pieceNumValue);
        
        if (bitboardIndex < 0) return;
        
        // 이동 전 상태 (비트보드에서 출발 위치 비트 해제)
        BitBoards[bitboardIndex] = BitHelper.ClearBit(BitBoards[bitboardIndex], fromSquare);
        
        // 목적지에 있는 기물 제거 (모든 비트보드에서)
        for (int i = 0; i < 12; i++)
        {
            BitBoards[i] = BitHelper.ClearBit(BitBoards[i], toSquare);
        }
        
        // 이동 후 상태 (비트보드에 도착 위치 비트 설정)
        BitBoards[bitboardIndex] = BitHelper.SetBitOn(BitBoards[bitboardIndex], toSquare);
        
        // 보드 배열 업데이트
        Board[toSquare] = pieceNumValue;
        Board[fromSquare] = 0;
        
        // 킹 이동시 위치 업데이트
        if (bitboardIndex == WHITE_KING)
        {
            whiteKingSquare = toSquare;
        }
        else if (bitboardIndex == BLACK_KING)
        {
            blackKingSquare = toSquare;
        }
        
        // 앙파상 타겟 초기화 (폰 두 칸 이동이 아닌 경우)
        if (EnPassantTargetSquare != -1 && toSquare != EnPassantTargetSquare)
        {
            EnPassantTargetSquare = -1;
        }
    }
    
    // 기물 배치
    public void PlacePiece(int square, int pieceNumValue)
    {
        // 기존 기물 제거
        ClearSquare(square);
        
        // 새 기물 배치
        int bitboardIndex = PieceNumToBitboardIndex(pieceNumValue);
        if (bitboardIndex >= 0)
        {
            BitBoards[bitboardIndex] = BitHelper.SetBitOn(BitBoards[bitboardIndex], square);
            Board[square] = pieceNumValue;
            
            // 킹 배치시 위치 업데이트
            if (bitboardIndex == WHITE_KING)
            {
                whiteKingSquare = square;
            }
            else if (bitboardIndex == BLACK_KING)
            {
                blackKingSquare = square;
            }
        }
    }
    
    // 특정 위치의 기물 제거
    public void ClearSquare(int square)
    {
        for (int i = 0; i < 12; i++)
        {
            BitBoards[i] = BitHelper.ClearBit(BitBoards[i], square);
        }
        Board[square] = 0;
    }
    
    // 현재 상태의 FEN 문자열 얻기
    public string GetFEN()
    {
        // 비트보드와 킹 위치 동기화 확인
        UpdateKingPositions();
        
        return FENParser.GameStateToFEN(this);
    }
    
    // 특정 색상의 모든 기물 위치 얻기
    public List<int> GetPiecePositions(bool isWhite, int pieceType)
    {
        List<int> positions = new List<int>();
        
        int offset = isWhite ? 0 : 6;
        int bitboardIndex;
        
        if (pieceType == pieceNum.pwan) bitboardIndex = offset + 0;
        else if (pieceType == pieceNum.knight) bitboardIndex = offset + 1;
        else if (pieceType == pieceNum.bishop) bitboardIndex = offset + 2;
        else if (pieceType == pieceNum.rook) bitboardIndex = offset + 3;
        else if (pieceType == pieceNum.queen) bitboardIndex = offset + 4;
        else if (pieceType == pieceNum.king) bitboardIndex = offset + 5;
        else return positions;
        
        ulong bitboard = BitBoards[bitboardIndex];
        while (bitboard != 0)
        {
            int square = BitHelper.GetLSBIndex(bitboard);
            positions.Add(square);
            bitboard = BitHelper.ClearBit(bitboard, square);
        }
        
        return positions;
    }
    
    // 체크 상태인지 확인
    public bool IsInCheck(bool whiteKing)
    {
        int kingSquare = whiteKing ? whiteKingSquare : blackKingSquare;
        return IsSquareAttacked(kingSquare, !whiteKing);
    }

    // 기본 생성자
    public ChessGameState()
    {
        InitializeDefaultState();
    }

    // FEN 문자열로 초기화하는 생성자
    public ChessGameState(string fen)
    {
        InitializeDefaultState();
        LoadFromFEN(fen);
    }

    // 기본 상태 초기화
    private void InitializeDefaultState()
    {
        // 비트보드 초기화
        BitBoards = new ulong[12];
        Board = new int[64];
        
        // 게임 상태 초기화
        IsWhiteTurn = true;
        CurrentGameState = GameState.Playing;
        
        // 캐슬링 권한 초기화
        WhiteKingSideCastleRight = true;
        WhiteQueenSideCastleRight = true;
        BlackKingSideCastleRight = true;
        BlackQueenSideCastleRight = true;
        
        // 기타 상태 초기화
        EnPassantTargetSquare = -1;
        FiftyMoveCounter = 0;
        FullMoveCounter = 1;
        IsPromotionPending = false;
        PromotionSquare = -1;
        
        // 이동 기록 초기화
        MoveHistory = new Stack<uint>();
        
        // 킹 위치 초기화
        whiteKingSquare = -1;
        blackKingSquare = -1;

    }

    // 공격 맵 업데이트 메서드
    public void UpdateAttackMaps()
    {
        // 공격 맵 초기화
        WhiteAttackMap = 0UL;
        BlackAttackMap = 0UL;
        
        // 각 기물별 공격 맵 초기화
        WhitePawnAttackMap = 0UL;
        BlackPawnAttackMap = 0UL;
        WhiteKnightAttackMap = 0UL;
        BlackKnightAttackMap = 0UL;
        WhiteBishopAttackMap = 0UL;
        BlackBishopAttackMap = 0UL;
        WhiteRookAttackMap = 0UL;
        BlackRookAttackMap = 0UL;
        WhiteQueenAttackMap = 0UL;
        BlackQueenAttackMap = 0UL;
        WhiteKingAttackMap = 0UL;
        BlackKingAttackMap = 0UL;
        
        // 폰 공격 맵 계산
        CalculatePawnAttacks();
        
        // 나이트 공격 맵 계산
        CalculateKnightAttacks();
        
        // 킹 공격 맵 계산 (킹 위치가 유효한지 확인)
        if (whiteKingSquare >= 0 && whiteKingSquare < 64) {
            WhiteKingAttackMap = ChessCache.KingMoves[whiteKingSquare];
        }
        if (blackKingSquare >= 0 && blackKingSquare < 64) {
            BlackKingAttackMap = ChessCache.KingMoves[blackKingSquare];
        }
        
        // 슬라이딩 공격 맵 계산 (비숍, 룩, 퀸)
        // 비숍 공격 계산
        CalculateSlidingAttacks(WHITE_BISHOP, WHITE_QUEEN, true);
        CalculateSlidingAttacks(BLACK_BISHOP, BLACK_QUEEN, false);
        
        // 룩 공격 계산
        CalculateSlidingAttacks(WHITE_ROOK, WHITE_QUEEN, true);
        CalculateSlidingAttacks(BLACK_ROOK, BLACK_QUEEN, false);
        
        // 모든 공격 맵 통합
        WhiteAttackMap = WhitePawnAttackMap | WhiteKnightAttackMap | WhiteBishopAttackMap | 
                         WhiteRookAttackMap | WhiteQueenAttackMap | WhiteKingAttackMap;
                         
        BlackAttackMap = BlackPawnAttackMap | BlackKnightAttackMap | BlackBishopAttackMap | 
                         BlackRookAttackMap | BlackQueenAttackMap | BlackKingAttackMap;
        
        // 디버그 로깅
        //DebugAttackMaps();
    }
    
    // 폰 공격 맵 계산
    private void CalculatePawnAttacks()
    {
        // 백색 폰 공격 맵
        ulong whitePawns = BitBoards[WHITE_PAWN];
        while (whitePawns != 0)
        {
            int pawnSquare = BitHelper.GetLSBIndex(whitePawns);
            whitePawns = BitHelper.ClearBit(whitePawns, pawnSquare);
            
            // 폰의 공격 위치 계산 (좌상단, 우상단 대각선)
            if (pawnSquare < 56) // 마지막 랭크가 아닌 경우만
            {
                // 좌상단 대각선 (파일이 0이 아닌 경우)
                if (pawnSquare % 8 > 0)
                {
                    WhitePawnAttackMap |= BitHelper.SetBit(pawnSquare + 7);
                }
                
                // 우상단 대각선 (파일이 7이 아닌 경우)
                if (pawnSquare % 8 < 7)
                {
                    WhitePawnAttackMap |= BitHelper.SetBit(pawnSquare + 9);
                }
            }
        }
        
        // 흑색 폰 공격 맵
        ulong blackPawns = BitBoards[BLACK_PAWN];
        while (blackPawns != 0)
        {
            int pawnSquare = BitHelper.GetLSBIndex(blackPawns);
            blackPawns = BitHelper.ClearBit(blackPawns, pawnSquare);
            
            // 폰의 공격 위치 계산 (좌하단, 우하단 대각선)
            if (pawnSquare > 7) // 첫 랭크가 아닌 경우만
            {
                // 좌하단 대각선 (파일이 0이 아닌 경우)
                if (pawnSquare % 8 > 0)
                {
                    BlackPawnAttackMap |= BitHelper.SetBit(pawnSquare - 9);
                }
                
                // 우하단 대각선 (파일이 7이 아닌 경우)
                if (pawnSquare % 8 < 7)
                {
                    BlackPawnAttackMap |= BitHelper.SetBit(pawnSquare - 7);
                }
            }
        }
    }
    
    // 나이트 공격 맵 계산
    private void CalculateKnightAttacks()
    {
        // 백색 나이트 공격 맵
        ulong whiteKnights = BitBoards[WHITE_KNIGHT];
        while (whiteKnights != 0)
        {
            int knightSquare = BitHelper.GetLSBIndex(whiteKnights);
            whiteKnights = BitHelper.ClearBit(whiteKnights, knightSquare);
            
            ulong attacks = ChessCache.KnightMoves[knightSquare];
            WhiteKnightAttackMap |= attacks;
        }
        
        // 흑색 나이트 공격 맵
        ulong blackKnights = BitBoards[BLACK_KNIGHT];
        while (blackKnights != 0)
        {
            int knightSquare = BitHelper.GetLSBIndex(blackKnights);
            blackKnights = BitHelper.ClearBit(blackKnights, knightSquare);
            
            ulong attacks = ChessCache.KnightMoves[knightSquare];
            BlackKnightAttackMap |= attacks;
        }
    }
    
    // 디버그용 공격 맵 정보 출력
    private void DebugAttackMaps()
    {
        // 퀸 공격 맵 디버깅
        ulong whiteQueens = BitBoards[WHITE_QUEEN];
        ulong blackQueens = BitBoards[BLACK_QUEEN];
        
        if (whiteQueens != 0 || blackQueens != 0)
        {
            Debug.Log("===== 퀸 위치와 공격 맵 =====");
            if (whiteQueens != 0)
            {
                Debug.Log($"백색 퀸 비트보드: {BitHelper.BitboardToString(whiteQueens)}");
                Debug.Log($"백색 퀸 공격 맵: {BitHelper.BitboardToString(WhiteQueenAttackMap)}");
            }
            if (blackQueens != 0)
            {
                Debug.Log($"흑색 퀸 비트보드: {BitHelper.BitboardToString(blackQueens)}");
                Debug.Log($"흑색 퀸 공격 맵: {BitHelper.BitboardToString(BlackQueenAttackMap)}");
            }
        }
        
        // 기타 공격 맵 요약 정보
        Debug.Log("===== 공격 맵 비트 수 =====");
        Debug.Log($"백색 폰 공격: {BitHelper.CountBits(WhitePawnAttackMap)}");
        Debug.Log($"흑색 폰 공격: {BitHelper.CountBits(BlackPawnAttackMap)}");
        Debug.Log($"백색 나이트 공격: {BitHelper.CountBits(WhiteKnightAttackMap)}");
        Debug.Log($"흑색 나이트 공격: {BitHelper.CountBits(BlackKnightAttackMap)}");
        Debug.Log($"백색 비숍 공격: {BitHelper.CountBits(WhiteBishopAttackMap)}");
        Debug.Log($"흑색 비숍 공격: {BitHelper.CountBits(BlackBishopAttackMap)}");
        Debug.Log($"백색 룩 공격: {BitHelper.CountBits(WhiteRookAttackMap)}");
        Debug.Log($"흑색 룩 공격: {BitHelper.CountBits(BlackRookAttackMap)}");
        Debug.Log($"백색 퀸 공격: {BitHelper.CountBits(WhiteQueenAttackMap)}");
        Debug.Log($"흑색 퀸 공격: {BitHelper.CountBits(BlackQueenAttackMap)}");
        Debug.Log($"백색 킹 공격: {BitHelper.CountBits(WhiteKingAttackMap)}");
        Debug.Log($"흑색 킹 공격: {BitHelper.CountBits(BlackKingAttackMap)}");
    }
    // 공격 맵 강제 업데이트 메서드 추가
    public void ForceUpdateAttackMaps()
    {

        WhiteAttackMap = 0UL;
        BlackAttackMap = 0UL;
        WhitePawnAttackMap = 0UL;
        BlackPawnAttackMap = 0UL;
        WhiteKnightAttackMap = 0UL;
        BlackKnightAttackMap = 0UL;
        WhiteBishopAttackMap = 0UL;
        BlackBishopAttackMap = 0UL;
        WhiteRookAttackMap = 0UL;
        BlackRookAttackMap = 0UL;
        WhiteQueenAttackMap = 0UL;
        BlackQueenAttackMap = 0UL;
        WhiteKingAttackMap = 0UL;
        BlackKingAttackMap = 0UL;
        UpdateAttackMaps();
        UpdatePinInformation();
        UpdateCheckInformation();
    }
    // 슬라이딩 공격 계산 (비숍, 룩, 퀸 등)
    private void CalculateSlidingAttacks(int pieceTypeIndex, int queenIndex, bool isWhite)
    {
        ulong pieces = BitBoards[pieceTypeIndex];
        bool isBishop = (pieceTypeIndex == WHITE_BISHOP || pieceTypeIndex == BLACK_BISHOP);
        bool isRook = (pieceTypeIndex == WHITE_ROOK || pieceTypeIndex == BLACK_ROOK);
        
        // 각 기물에 대한 공격 맵 계산
        while (pieces != 0)
        {
            int pieceSquare = BitHelper.GetLSBIndex(pieces);
            pieces = BitHelper.ClearBit(pieces, pieceSquare);
            
            ulong attacks = 0UL;
            
            // 비숍 또는 퀸의 대각선 공격 계산
            if (isBishop)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    ulong[] dirPath = ChessCache.GetBishopDirectionPath(pieceSquare, dir);
                    if (dirPath == null || dirPath.Length == 0) continue;
                    
                    for (int i = 0; i < dirPath.Length; i++)
                    {
                        ulong squareBit = dirPath[i];
                        attacks |= squareBit;
                        
                        // 기물이 있으면 그 위치까지만 공격 가능
                        if ((AllPieces & squareBit) != 0)
                            break;
                    }
                }
            }
            // 룩 또는 퀸의 직선 공격 계산
            else if (isRook)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    ulong[] dirPath = ChessCache.GetRookDirectionPath(pieceSquare, dir);
                    if (dirPath == null || dirPath.Length == 0) continue;
                    
                    for (int i = 0; i < dirPath.Length; i++)
                    {
                        ulong squareBit = dirPath[i];
                        attacks |= squareBit;
                        
                        // 기물이 있으면 그 위치까지만 공격 가능
                        if ((AllPieces & squareBit) != 0)
                            break;
                    }
                }
            }
            if(isBishop && isWhite)
            {
                WhiteBishopAttackMap |= attacks;
            }
            else if (isBishop && !isWhite)
            {
                BlackBishopAttackMap |= attacks;
            }
            else if (isRook && isWhite)
            {
                WhiteRookAttackMap |= attacks;
            }
            else if (isRook && !isWhite)
            {
                BlackRookAttackMap |= attacks;
            }

        }
        
        // 퀸 공격 맵 별도 계산 (퀸은 비숍과 룩의 이동 조합)
        ulong queens = BitBoards[queenIndex];
        while (queens != 0)
        {
            int queenSquare = BitHelper.GetLSBIndex(queens);
            queens = BitHelper.ClearBit(queens, queenSquare);
            
            ulong attacks = 0UL;
            
            // 대각선 이동 (비숍처럼)
            for (int dir = 0; dir < 4; dir++)
            {
                ulong[] dirPath = ChessCache.GetBishopDirectionPath(queenSquare, dir);
                if (dirPath == null || dirPath.Length == 0) continue;
                
                for (int i = 0; i < dirPath.Length; i++)
                {
                    ulong squareBit = dirPath[i];
                    attacks |= squareBit;
                    
                    // 기물이 있으면 그 위치까지만 공격 가능
                    if ((AllPieces & squareBit) != 0)
                        break;
                }
            }
            
            // 직선 이동 (룩처럼)
            for (int dir = 0; dir < 4; dir++)
            {
                ulong[] dirPath = ChessCache.GetRookDirectionPath(queenSquare, dir);
                if (dirPath == null || dirPath.Length == 0) continue;
                
                for (int i = 0; i < dirPath.Length; i++)
                {
                    ulong squareBit = dirPath[i];
                    attacks |= squareBit;
                    
                    // 기물이 있으면 그 위치까지만 공격 가능
                    if ((AllPieces & squareBit) != 0)
                        break;
                }
            }
            if(isWhite)
            {
                WhiteQueenAttackMap |= attacks;
            }
            else
            {
                BlackQueenAttackMap |= attacks;
            }
        }
    }
    
    // 핀 감지 및 관련 비트보드 업데이트
    public void UpdatePinInformation()
    {
        PinnedPieces = 0UL;
        
        // 현재 차례의 킹 위치
        int kingSquare = IsWhiteTurn ? whiteKingSquare : blackKingSquare;
        ulong friendlyPieces = IsWhiteTurn ? WhitePieces : BlackPieces;
        ulong enemyPieces = IsWhiteTurn ? BlackPieces : WhitePieces;
        
        // 비숍/퀸에 의한 핀 (대각선)
        ulong enemyBishops = BitBoards[IsWhiteTurn ? BLACK_BISHOP : WHITE_BISHOP];
        ulong enemyQueens = BitBoards[IsWhiteTurn ? BLACK_QUEEN : WHITE_QUEEN];
        ulong bishopQueens = enemyBishops | enemyQueens;
        
        // 각 대각선 방향에 대해
        for (int dir = 0; dir < 4; dir++)
        {
            ulong[] dirPath = ChessCache.GetBishopDirectionPath(kingSquare, dir);
            if (dirPath == null || dirPath.Length == 0) continue;
            
            ulong potentialPin = 0UL;
            bool foundFriendly = false;
            
            for (int i = 0; i < dirPath.Length; i++)
            {
                ulong squareBit = dirPath[i];
                
                // 아군 기물 발견
                if ((friendlyPieces & squareBit) != 0)
                {
                    if (foundFriendly) // 이미 아군 기물 하나 발견했으면 핀 불가능
                    {
                        potentialPin = 0UL;
                        break;
                    }
                    
                    foundFriendly = true;
                    potentialPin = squareBit;
                }
                // 적 기물 발견
                else if ((enemyPieces & squareBit) != 0)
                {
                    // 핀 가능한 적 슬라이더(비숍/퀸)가 있는지 확인
                    if (foundFriendly && (bishopQueens & squareBit) != 0)
                    {
                        PinnedPieces |= potentialPin;
                    }
                    break;
                }
            }
        }
        
        // 룩/퀸에 의한 핀 (직선)
        ulong enemyRooks = BitBoards[IsWhiteTurn ? BLACK_ROOK : WHITE_ROOK];
        ulong rookQueens = enemyRooks | enemyQueens;
        
        // 각 직선 방향에 대해
        for (int dir = 0; dir < 4; dir++)
        {
            ulong[] dirPath = ChessCache.GetRookDirectionPath(kingSquare, dir);
            if (dirPath == null || dirPath.Length == 0) continue;
            
            ulong potentialPin = 0UL;
            bool foundFriendly = false;
            
            for (int i = 0; i < dirPath.Length; i++)
            {
                ulong squareBit = dirPath[i];
                
                // 아군 기물 발견
                if ((friendlyPieces & squareBit) != 0)
                {
                    if (foundFriendly) // 이미 아군 기물 하나 발견했으면 핀 불가능
                    {
                        potentialPin = 0UL;
                        break;
                    }
                    
                    foundFriendly = true;
                    potentialPin = squareBit;
                }
                // 적 기물 발견
                else if ((enemyPieces & squareBit) != 0)
                {
                    // 핀 가능한 적 슬라이더(룩/퀸)가 있는지 확인
                    if (foundFriendly && (rookQueens & squareBit) != 0)
                    {
                        PinnedPieces |= potentialPin;
                    }
                    break;
                }
            }
        }
    }
    
    // 체크 상태 감지 및 관련 비트보드 업데이트
    public void UpdateCheckInformation()
    {
        // 초기화
        CheckingPieces = 0UL;
        CheckBlockingMask = 0UL;
        
        // 현재 차례의 킹 위치
        int kingSquare = IsWhiteTurn ? whiteKingSquare : blackKingSquare;
        ulong kingBit = BitHelper.SetBit(kingSquare);
        
        // 이 함수 호출 전에 공격 맵이 업데이트되었는지 확인
        UpdateAttackMaps();
        
        // 폰 체크 확인
        ulong enemyPawns = BitBoards[IsWhiteTurn ? BLACK_PAWN : WHITE_PAWN];
        ulong pawnAttacks = 0UL;
        
        if (IsWhiteTurn) // 백색 킹에 대한 흑색 폰 공격
        {
            // 킹의 왼쪽과 오른쪽 위 대각선 위치에 폰이 있는지 확인
            if (kingSquare % 8 > 0 && kingSquare + 7 < 64) // 왼쪽 위 대각선
            {
                if ((enemyPawns & BitHelper.SetBit(kingSquare + 7)) != 0)
                    pawnAttacks |= BitHelper.SetBit(kingSquare + 7);
            }
            if (kingSquare % 8 < 7 && kingSquare + 9 < 64) // 오른쪽 위 대각선
            {
                if ((enemyPawns & BitHelper.SetBit(kingSquare + 9)) != 0)
                    pawnAttacks |= BitHelper.SetBit(kingSquare + 9);
            }
        }
        else // 흑색 킹에 대한 백색 폰 공격
        {
            // 킹의 왼쪽과 오른쪽 아래 대각선 위치에 폰이 있는지 확인
            if (kingSquare % 8 > 0 && kingSquare - 9 >= 0) // 왼쪽 아래 대각선
            {
                if ((enemyPawns & BitHelper.SetBit(kingSquare - 9)) != 0)
                    pawnAttacks |= BitHelper.SetBit(kingSquare - 9);
            }
            if (kingSquare % 8 < 7 && kingSquare - 7 >= 0) // 오른쪽 아래 대각선
            {
                if ((enemyPawns & BitHelper.SetBit(kingSquare - 7)) != 0)
                    pawnAttacks |= BitHelper.SetBit(kingSquare - 7);
            }
        }
        
        if (pawnAttacks != 0)
        {
            CheckingPieces |= pawnAttacks;
            // 폰은 이동 경로가 없으므로 체크 블로킹 마스크에 폰 자체만 추가
            CheckBlockingMask |= pawnAttacks;
        }
        
        // 나이트 체크 확인
        ulong enemyKnights = BitBoards[IsWhiteTurn ? BLACK_KNIGHT : WHITE_KNIGHT];
        ulong knightAttacks = ChessCache.KnightMoves[kingSquare] & enemyKnights;
        
        if (knightAttacks != 0)
        {
            CheckingPieces |= knightAttacks;
            // 나이트도 이동 경로가 없으므로 체크 블로킹 마스크에 나이트 자체만 추가
            CheckBlockingMask |= knightAttacks;
        }
        
        // 슬라이딩 체크 확인 (비숍, 룩, 퀸)
        ulong enemyBishops = BitBoards[IsWhiteTurn ? BLACK_BISHOP : WHITE_BISHOP];
        ulong enemyRooks = BitBoards[IsWhiteTurn ? BLACK_ROOK : WHITE_ROOK];
        ulong enemyQueens = BitBoards[IsWhiteTurn ? BLACK_QUEEN : WHITE_QUEEN];
        
        // 비숍/퀸에 의한 체크 (대각선)
        for (int dir = 0; dir < 4; dir++)
        {
            ulong[] dirPath = ChessCache.GetBishopDirectionPath(kingSquare, dir);
            if (dirPath == null || dirPath.Length == 0) continue;
            
            ulong blockingMask = 0UL;
            bool foundCheck = false;
            
            for (int i = 0; i < dirPath.Length; i++)
            {
                ulong squareBit = dirPath[i];
                int squareIndex = BitHelper.BitScanForward(squareBit);
                
                // 기물 발견
                if ((AllPieces & squareBit) != 0)
                {
                    // 적 비숍/퀸인 경우 체크
                    if (((enemyBishops | enemyQueens) & squareBit) != 0)
                    {
                        CheckingPieces |= squareBit;
                        blockingMask |= squareBit; // 공격자 자신을 추가
                        foundCheck = true;
                    }
                    break;
                }
                
                blockingMask |= squareBit;
            }
            
            // 체크가 있을 때만 블로킹 마스크 추가
            if (foundCheck)
            {
                CheckBlockingMask |= blockingMask;
            }
        }
        
        // 룩/퀸에 의한 체크 (직선)
        for (int dir = 0; dir < 4; dir++)
        {
            ulong[] dirPath = ChessCache.GetRookDirectionPath(kingSquare, dir);
            if (dirPath == null || dirPath.Length == 0) continue;
            
            ulong blockingMask = 0UL;
            bool foundCheck = false;
            
            for (int i = 0; i < dirPath.Length; i++)
            {
                ulong squareBit = dirPath[i];
                int squareIndex = BitHelper.BitScanForward(squareBit);
                
                // 기물 발견
                if ((AllPieces & squareBit) != 0)
                {
                    // 적 룩/퀸인 경우 체크
                    if (((enemyRooks | enemyQueens) & squareBit) != 0)
                    {
                        CheckingPieces |= squareBit;
                        blockingMask |= squareBit; // 공격자 자신을 추가
                        foundCheck = true;
                    }
                    break;
                }
                
                blockingMask |= squareBit;
            }
            
            // 체크가 있을 때만 블로킹 마스크 추가
            if (foundCheck)
            {
                CheckBlockingMask |= blockingMask;
            }
        }
        
        // 더블 체크 확인 (2개 이상의 기물이 체크하는 경우)
        bool isDoubleCheck = BitHelper.CountBits(CheckingPieces) > 1;
        
        // 더블 체크인 경우 체크 블로킹 마스크 제한
        // (더블 체크는 킹이 직접 피할 수밖에 없음)
        if (isDoubleCheck)
        {
            CheckBlockingMask = CheckingPieces; // 공격자 기물만 캡처 가능
        }
        

    } 
    
    // 특정 위치가 공격받고 있는지 확인
    public bool IsSquareAttacked(int square, bool byWhite)
    {
        if (square < 0 || square >= 64) return false;
        
        // 항상 최신 공격 맵 데이터로 계산
        UpdateAttackMaps();
        
        ulong squareBit = BitHelper.SetBit(square);
        
        if (byWhite)
        {
            return (WhiteAttackMap & squareBit) != 0;
        }
        else
        {
            return (BlackAttackMap & squareBit) != 0;
        }
    }
    // MoveGenerator가 접근할 수 있도록 추가한 메서드들
    public int GetCheckCount()
    {
        return BitHelper.CountBits(CheckingPieces);
    }

    public ulong GetCheckingPieces()
    {
        return CheckingPieces;
    }

    public ulong GetCheckBlockingMask()
    {
        return CheckBlockingMask;
    }
} 