using UnityEngine;

[DisallowMultipleComponent]
public class ChessPiece : MonoBehaviour
{
    // 기물 타입 (pieceNum 클래스의 비트 값 사용)
    public int PieceType { get; set; }
    
    // 색상 (true: 백, false: 흑)
    public bool IsWhite { get; set; }
    
    // 현재 위치 (0-63)
    public int Square { get; set; }
    
    // 초기화 메서드
    public void Initialize(int pieceType, bool isWhite, int square)
    {
        PieceType = pieceType;
        IsWhite = isWhite;
        Square = square;
    }
    
    // 기물 상태 리셋 (풀에 반환될 때 호출)
    public void Reset()
    {
        PieceType = 0;
        Square = -1;
        // IsWhite는 유지 (색상별 풀링을 위해)
    }
    
    // 기물 표현을 FEN 문자로 변환
    public char ToFENChar()
    {
        if (PieceType == 0) return '.';
        
        int pieceValue = PieceType | (IsWhite ? pieceNum.white : pieceNum.black);
        return pieceNum.PieceToFenChar(pieceValue);
    }
    
    // 디버그용 문자열 반환
    public override string ToString()
    {
        string typeStr = "";
        
        if ((PieceType & pieceNum.pwan) != 0) typeStr = "폰";
        else if ((PieceType & pieceNum.knight) != 0) typeStr = "나이트";
        else if ((PieceType & pieceNum.bishop) != 0) typeStr = "비숍";
        else if ((PieceType & pieceNum.rook) != 0) typeStr = "룩";
        else if ((PieceType & pieceNum.queen) != 0) typeStr = "퀸";
        else if ((PieceType & pieceNum.king) != 0) typeStr = "킹";
        
        string colorStr = IsWhite ? "백" : "흑";
        string squareStr = Square >= 0 && Square < 64 ? 
            $"{(char)('a' + Square % 8)}{Square / 8 + 1}" : "미배치";
        
        return $"{colorStr} {typeStr} ({squareStr})";
    }
} 