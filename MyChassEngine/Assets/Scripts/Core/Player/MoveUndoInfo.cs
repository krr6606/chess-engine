/// <summary>
/// 이동 되돌리기 정보를 담는 구조체
/// </summary>
public struct MoveUndoInfo
{
    public Move Move;
    public int CapturedPiece;
    public bool WasWhiteTurn;
    public int EnPassantTarget;
    public bool WhiteKingSideCastle;
    public bool WhiteQueenSideCastle;
    public bool BlackKingSideCastle;
    public bool BlackQueenSideCastle;
    public int FiftyMoveCounter;
    public ChessGameState.GameState PreviousGameState;
}
