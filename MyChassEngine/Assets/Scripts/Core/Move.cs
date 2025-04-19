using System;
using UnityEngine;

public class Move
{
    // 이동 비트 표현 (24비트 사용)
    // - 비트 0-5: 출발 위치 (0-63)
    // - 비트 6-11: 도착 위치 (0-63)
    // - 비트 12-14: 프로모션 기물 타입 (0: 없음, 1: 나이트, 2: 비숍, 3: 룩, 4: 퀸)
    // - 비트 15-19: 캡처된 기물 타입 (0: 없음, 1: 폰, 2: 나이트, 3: 비숍, 4: 룩, 5: 퀸)
    // - 비트 20-23: 이동 플래그
    private int moveData;

    // 이동 플래그 (비트 20-23)
    [Flags]
    public enum MoveFlag
    {
        None = 0,
        Capture = 1 << 0,           // 기물 캡처
        PawnTwoForward = 1 << 1,    // 폰 두 칸 전진
        EnPassant = 1 << 2,         // 앙파상
        Promotion = 1 << 3,         // 프로모션
        KingSideCastle = 1 << 4,    // 킹 사이드 캐슬링
        QueenSideCastle = 1 << 5,   // 퀸 사이드 캐슬링
        Check = 1 << 6,             // 체크를 주는 이동
        Checkmate = 1 << 7          // 체크메이트를 주는 이동
    }

    // 기물 타입 상수
    public const int NONE = 0;
    public const int PAWN = 1;
    public const int KNIGHT = 2;
    public const int BISHOP = 3;
    public const int ROOK = 4;
    public const int QUEEN = 5;
    public const int KING = 6;

    // 기본 생성자
    public Move(int fromSquare, int toSquare)
    {
        moveData = (fromSquare & 0x3F) | ((toSquare & 0x3F) << 6);
    }
    public Move(ulong fromBitboard, ulong toBitboard)
    {
        moveData = (int)(BitHelper.GetLSBIndex(fromBitboard) & 0x3F) | ((int)(BitHelper.GetLSBIndex(toBitboard) & 0x3F) << 6);
    }
    // 플래그 포함 생성자
    public Move(int fromSquare, int toSquare, MoveFlag flag)
    {
        moveData = (fromSquare & 0x3F) | ((toSquare & 0x3F) << 6) | ((int)flag << 20);
    }

    // 프로모션 포함 생성자
    public Move(int fromSquare, int toSquare, MoveFlag flag, int promotionPieceType)
    {
        moveData = (fromSquare & 0x3F) | ((toSquare & 0x3F) << 6) | 
                  ((promotionPieceType & 0x7) << 12) | ((int)flag << 20);
    }

    // 캡처 포함 생성자
    public Move(int fromSquare, int toSquare, MoveFlag flag, int capturedPieceType, int promotionPieceType = NONE)
    {
        moveData = (fromSquare & 0x3F) | ((toSquare & 0x3F) << 6) | 
                  ((promotionPieceType & 0x7) << 12) | ((capturedPieceType & 0x1F) << 15) | 
                  ((int)flag << 20);
    }

    // Coord를 사용하는 생성자들
    public Move(Coord from, Coord to)
    {
        moveData = (from.SquareIndex & 0x3F) | ((to.SquareIndex & 0x3F) << 6);
    }

    public Move(Coord from, Coord to, MoveFlag flag)
    {
        moveData = (from.SquareIndex & 0x3F) | ((to.SquareIndex & 0x3F) << 6) | ((int)flag << 20);
    }

    public Move(Coord from, Coord to, MoveFlag flag, int promotionPieceType)
    {
        moveData = (from.SquareIndex & 0x3F) | ((to.SquareIndex & 0x3F) << 6) | 
                  ((promotionPieceType & 0x7) << 12) | ((int)flag << 20);
    }

    public Move(Coord from, Coord to, MoveFlag flag, int capturedPieceType, int promotionPieceType = NONE)
    {
        moveData = (from.SquareIndex & 0x3F) | ((to.SquareIndex & 0x3F) << 6) | 
                  ((promotionPieceType & 0x7) << 12) | ((capturedPieceType & 0x1F) << 15) | 
                  ((int)flag << 20);
    }

    // 출발 위치 접근자
    public int FromSquare => moveData & 0x3F;

    // 도착 위치 접근자
    public int ToSquare => (moveData >> 6) & 0x3F;

    // 프로모션 기물 타입 접근자
    public int PromotionPieceType => (moveData >> 12) & 0x7;

    // 캡처된 기물 타입 접근자
    public int CapturedPieceType => (moveData >> 15) & 0x1F;

    // 이동 플래그 접근자
    public MoveFlag Flag => (MoveFlag)((moveData >> 20) & 0xF);

    // 플래그 확인 메서드
    public bool HasFlag(MoveFlag flag) => (Flag & flag) == flag;

    // 캡처 이동인지 확인
    public bool IsCapture => HasFlag(MoveFlag.Capture) || HasFlag(MoveFlag.EnPassant);

    // 특수 이동인지 확인 (캐슬링, 앙파상, 프로모션, 두 칸 전진)
    public bool IsSpecialMove => HasFlag(MoveFlag.KingSideCastle) || HasFlag(MoveFlag.QueenSideCastle) ||
                                HasFlag(MoveFlag.EnPassant) || HasFlag(MoveFlag.Promotion) ||
                                HasFlag(MoveFlag.PawnTwoForward);

    // 이동에 대한 문자열 표현
    public override string ToString()
    {
        string fromFile = ((char)('a' + (FromSquare % 8))).ToString();
        string fromRank = ((FromSquare / 8) + 1).ToString();
        string toFile = ((char)('a' + (ToSquare % 8))).ToString();
        string toRank = ((ToSquare / 8) + 1).ToString();
        
        string moveStr = fromFile + fromRank + toFile + toRank;
        
        // 프로모션 처리
        if (HasFlag(MoveFlag.Promotion))
        {
            char promotionChar = ' ';
            switch (PromotionPieceType)
            {
                case KNIGHT: promotionChar = 'n'; break;
                case BISHOP: promotionChar = 'b'; break;
                case ROOK: promotionChar = 'r'; break;
                case QUEEN: promotionChar = 'q'; break;
            }
            moveStr += promotionChar;
        }
        
        return moveStr;
    }
}

