using System;
using System.Collections.Generic;
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
    private void UpdateBitboardsFromBoard(int[] boardArray)
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
        // 체크 검사 로직 구현
        int kingSquare = whiteKing ? whiteKingSquare : blackKingSquare;
        if (kingSquare < 0) return false;
        
        // TODO: 여기에 체크 검사 로직 추가
        
        return false;
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
        
        Debug.Log("ChessGameState 초기화 완료");
    }
} 