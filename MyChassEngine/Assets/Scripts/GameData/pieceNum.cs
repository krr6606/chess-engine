using System;

public static class pieceNum 
{
  public const int empty = 0b00000000;
  public const int pwan = 0b00000001;
  public const int knight = 0b00000010;
  public const int bishop = 0b00000100;
  public const int rook = 0b00001000;
  public const int queen = 0b00010000;
  public const int king = 0b00100000;

  public const int white = 0b01000000;
  public const int black = 0b10000000;

  public const int whitePawn = white | pwan;
  public const int blackPawn = black | pwan;

  public const int whiteKnight = white | knight;
  public const int blackKnight = black | knight;

  public const int whiteBishop = white | bishop;
  public const int blackBishop = black | bishop;

  public const int whiteRook = white | rook;
  public const int blackRook = black | rook;

  public const int whiteQueen = white | queen;
  public const int blackQueen = black | queen;

  public const int whiteKing = white | king;
  public const int blackKing = black | king;

  // 색상 마스크 (색상 비트만 추출)
  public const int colorMask = white | black;
  
  // 기물 타입 마스크 (기물 타입 비트만 추출)
  public const int pieceMask = pwan | knight | bishop | rook | queen | king;
  
  // 기물 타입 추출 (색상 제거)
  public static int GetPieceType(int piece)
  {
    return piece & pieceMask;
  }
  
  // 기물 색상 추출
  public static int GetColor(int piece)
  {
    return piece & colorMask;
  }
  
  // 기물이 흰색인지 확인
  public static bool IsWhite(int piece)
  {
    return (piece & colorMask) == white;
  }
  
  // 기물이 검은색인지 확인
  public static bool IsBlack(int piece)
  {
    return (piece & colorMask) == black;
  }
  
  // 기물이 폰인지 확인
  public static bool IsPawn(int piece)
  {
    return GetPieceType(piece) == pwan;
  }
  
  // 기물이 나이트인지 확인
  public static bool IsKnight(int piece)
  {
    return GetPieceType(piece) == knight;
  }
  
  // 기물이 비숍인지 확인
  public static bool IsBishop(int piece)
  {
    return GetPieceType(piece) == bishop;
  }
  
  // 기물이 룩인지 확인
  public static bool IsRook(int piece)
  {
    return GetPieceType(piece) == rook;
  }
  
  // 기물이 퀸인지 확인
  public static bool IsQueen(int piece)
  {
    return GetPieceType(piece) == queen;
  }
  
  // 기물이 킹인지 확인
  public static bool IsKing(int piece)
  {
    return GetPieceType(piece) == king;
  }
  
  // 기물이 슬라이딩 기물인지 확인 (비숍, 룩, 퀸)
  public static bool IsSliding(int piece)
  {
    int type = GetPieceType(piece);
    return type == bishop || type == rook || type == queen;
  }
  
  // FEN 문자 -> pieceNum 변환
  public static int FenCharToPiece(char c)
  {
    bool isWhite = char.IsUpper(c);
    int color = isWhite ? white : black;
    char lowerChar = char.ToLower(c);
    
    switch (lowerChar)
    {
      case 'p': return color | pwan;
      case 'n': return color | knight;
      case 'b': return color | bishop;
      case 'r': return color | rook;
      case 'q': return color | queen;
      case 'k': return color | king;
      default: return empty;
    }
  }
  
  // pieceNum -> FEN 문자 변환
  public static char PieceToFenChar(int piece)
  {
    if (piece == empty) return '.';
    
    char c;
    int type = GetPieceType(piece);
    
    if (type == pwan) c = 'p';
    else if (type == knight) c = 'n';
    else if (type == bishop) c = 'b';
    else if (type == rook) c = 'r';
    else if (type == queen) c = 'q';
    else if (type == king) c = 'k';
    else return '?';
    
    return IsWhite(piece) ? char.ToUpper(c) : c;
  }
  
  // pieceNum -> 표준 기물값 변환 (FENParser와 호환)
  public static int ToStandardValue(int piece)
  {
    if (piece == empty) return 0;
    
    int type = 0;
    int pieceType = GetPieceType(piece);
    
    if (pieceType == pwan) type = 1;
    else if (pieceType == knight) type = 2;
    else if (pieceType == bishop) type = 3;
    else if (pieceType == rook) type = 4;
    else if (pieceType == queen) type = 5;
    else if (pieceType == king) type = 6;
    
    return IsWhite(piece) ? type : -type;
  }
  
  // 표준 기물값 -> pieceNum 변환
  public static int FromStandardValue(int standardValue)
  {
    if (standardValue == 0) return empty;
    
    bool isWhite = standardValue > 0;
    int absValue = Math.Abs(standardValue);
    int color = isWhite ? white : black;
    
    switch (absValue)
    {
      case 1: return color | pwan;
      case 2: return color | knight;
      case 3: return color | bishop;
      case 4: return color | rook;
      case 5: return color | queen;
      case 6: return color | king;
      default: return empty;
    }
  }
  
  // 같은 색상인지 확인
  public static bool SameColor(int piece1, int piece2)
  {
    return (piece1 & colorMask) == (piece2 & colorMask);
  }
  
  // 반대 색상인지 확인
  public static bool OppositeColor(int piece1, int piece2)
  {
    return (piece1 & colorMask) != 0 && (piece2 & colorMask) != 0 && !SameColor(piece1, piece2);
  }
  
  // 기물에 색상 적용
  public static int ApplyColor(int pieceType, bool isWhite)
  {
    return (pieceType & pieceMask) | (isWhite ? white : black);
  }
  
  // 기물 색상 반전
  public static int FlipColor(int piece)
  {
    if (piece == empty) return empty;
    int type = GetPieceType(piece);
    return type | (IsWhite(piece) ? black : white);
  }
}
