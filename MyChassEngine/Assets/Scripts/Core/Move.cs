using System;
using UnityEngine;

public readonly struct Move
{
    // 16bit move value
    readonly ushort moveValue;

    // Flags
    public const int NoFlag = 0b0000;
    public const int EnPassantCaptureFlag = 0b0001;
    public const int CastleFlag = 0b0010;
    public const int PawnTwoUpFlag = 0b0011;

    public const int PromoteToQueenFlag = 0b0100;
    public const int PromoteToKnightFlag = 0b0101;
    public const int PromoteToRookFlag = 0b0110;
    public const int PromoteToBishopFlag = 0b0111;

    // 캐슬링 세부 플래그 (CastleFlag와 함께 사용)
    public const int KingSideCastleFlag = 0b1000;
    public const int QueenSideCastleFlag = 0b1001;

    // 추가 플래그
    public const int CaptureFlag = 0b1010;
    public const int CheckFlag = 0b1011;
    public const int CheckmateFlag = 0b1100;

    // Masks
    const ushort startSquareMask = 0b0000000000111111;
    const ushort targetSquareMask = 0b0000111111000000;
    const ushort flagMask = 0b1111000000000000;

    // 생성자들
    public Move(int fromSquare, int toSquare)
    {
        moveValue = (ushort)(fromSquare | (toSquare << 6));
    }

    public Move(int fromSquare, int toSquare, int flag)
    {
        moveValue = (ushort)(fromSquare | (toSquare << 6) | (flag << 12));
    }

    public Move(Coord from, Coord to)
    {
        moveValue = (ushort)(from.SquareIndex | (to.SquareIndex << 6));
    }

    public Move(Coord from, Coord to, int flag)
    {
        moveValue = (ushort)(from.SquareIndex | (to.SquareIndex << 6) | (flag << 12));
    }

    // 속성들
    public int FromSquare => moveValue & startSquareMask;
    public int ToSquare => (moveValue & targetSquareMask) >> 6;
    public int Flag => (moveValue & flagMask) >> 12;

    // 유틸리티 메서드들
    public bool IsCapture => Flag == CaptureFlag || Flag == EnPassantCaptureFlag;
    public bool IsPromotion => Flag >= PromoteToQueenFlag && Flag <= PromoteToBishopFlag;
    public bool IsCastle => Flag == CastleFlag || Flag == KingSideCastleFlag || Flag == QueenSideCastleFlag;
    public bool IsEnPassant => Flag == EnPassantCaptureFlag;
    public bool IsPawnTwoUp => Flag == PawnTwoUpFlag;

    public bool HasFlag(int flag) => Flag == flag;

    // 이전 MoveFlag 열거형 호환을 위한 메서드
    public bool HasOldFlag(MoveFlag flag)
    {
        switch (flag)
        {
            case MoveFlag.Capture: return Flag == CaptureFlag;
            case MoveFlag.PawnTwoForward: return Flag == PawnTwoUpFlag;
            case MoveFlag.EnPassant: return Flag == EnPassantCaptureFlag;
            case MoveFlag.Promotion: return IsPromotion;
            case MoveFlag.KingSideCastle: return Flag == KingSideCastleFlag;
            case MoveFlag.QueenSideCastle: return Flag == QueenSideCastleFlag;
            case MoveFlag.Check: return Flag == CheckFlag;
            case MoveFlag.Checkmate: return Flag == CheckmateFlag;
            default: return false;
        }
    }

    // 프로모션 관련 확인 메서드
    public bool IsQueenPromotion => Flag == PromoteToQueenFlag;
    public bool IsRookPromotion => Flag == PromoteToRookFlag;
    public bool IsBishopPromotion => Flag == PromoteToBishopFlag;
    public bool IsKnightPromotion => Flag == PromoteToKnightFlag;

    // 문자열 표현
    public override string ToString()
    {
        string fromFile = ((char)('a' + (FromSquare % 8))).ToString();
        string fromRank = ((FromSquare / 8) + 1).ToString();
        string toFile = ((char)('a' + (ToSquare % 8))).ToString();
        string toRank = ((ToSquare / 8) + 1).ToString();
        
        string moveStr = fromFile + fromRank + toFile + toRank;
        
        // 프로모션 표시
        if (IsPromotion)
        {
            char promotionChar = ' ';
            if (IsQueenPromotion) promotionChar = 'q';
            else if (IsRookPromotion) promotionChar = 'r';
            else if (IsBishopPromotion) promotionChar = 'b';
            else if (IsKnightPromotion) promotionChar = 'n';
            
            moveStr += promotionChar;
        }
        
        return moveStr;
    }

    // 이동 객체 풀링을 위한 메소드: 새 값으로 이동 생성
    public Move With(int fromSquare, int toSquare, int flag)
    {
        return new Move(fromSquare, toSquare, flag);
    }

    // 이전 코드와의 호환성을 위한 열거형 (레거시)
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
        Checkmate = 1 << 7,         // 체크메이트를 주는 이동
    }
}

