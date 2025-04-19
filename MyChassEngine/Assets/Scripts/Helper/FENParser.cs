using System;
using UnityEngine;

public static class FENParser
{
    // 표준 시작 위치의 FEN 문자열
    public const string StartPositionFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    
    // FEN 문자열에서 보드 상태 추출
    public static int[] ParseFEN(string fen)
    {
        int[] board = new int[64];
        
        // 기본값으로 초기화
        for (int i = 0; i < 64; i++)
            board[i] = 0;
        
        string[] fenParts = fen.Split(' ');
        string piecePositions = fenParts[0];
        
        int file = 0;
        int rank = 7;  // FEN은 8번 랭크(맨 위)부터 시작
        
        foreach (char c in piecePositions)
        {
            if (c == '/')
            {
                // 새로운 랭크 시작
                file = 0;
                rank--;
            }
            else if (char.IsDigit(c))
            {
                // 빈 칸 건너뛰기
                file += int.Parse(c.ToString());
            }
            else
            {
                // 기물 배치
                int square = rank * 8 + file;
                
                // pieceNum 형식으로 변환
                board[square] = pieceNum.FenCharToPiece(c);
                file++;
            }
        }
        
        return board;
    }
    
    // FEN 문자열 파싱 - 전체 게임 상태
    public static ChessGameState ParseGameState(string fen)
    {
        ChessGameState state = new ChessGameState();
        
        string[] fenParts = fen.Split(' ');
        
        // 보드 상태
        state.Board = ParseFEN(fen);
        
        // 차례
        state.IsWhiteTurn = fenParts[1] == "w";
        
        // 캐슬링 권한
        if (fenParts.Length > 2)
        {
            string castlingRights = fenParts[2];
            state.WhiteKingSideCastleRight = castlingRights.Contains("K");
            state.WhiteQueenSideCastleRight = castlingRights.Contains("Q");
            state.BlackKingSideCastleRight = castlingRights.Contains("k");
            state.BlackQueenSideCastleRight = castlingRights.Contains("q");
        }
        
        // 앙파상 대상
        if (fenParts.Length > 3 && fenParts[3] != "-")
        {
            string enPassantTarget = fenParts[3];
            int file = enPassantTarget[0] - 'a';
            int rank = int.Parse(enPassantTarget[1].ToString()) - 1;
            state.EnPassantTargetSquare = rank * 8 + file;
        }
        else
        {
            state.EnPassantTargetSquare = -1;
        }
        
        // 하프 무브 카운터
        if (fenParts.Length > 4)
        {
            state.FiftyMoveCounter = int.Parse(fenParts[4]);
        }
        
        // 풀 무브 카운터
        if (fenParts.Length > 5)
        {
            state.FullMoveCounter = int.Parse(fenParts[5]);
        }
        
        // 비트보드 초기화
        InitializeBitboards(state);
        
        return state;
    }
    
    // 비트보드 초기화
    private static void InitializeBitboards(ChessGameState state)
    {
        // 비트보드 초기화
        for (int i = 0; i < 12; i++)
        {
            state.BitBoards[i] = 0UL;
        }
        
        // 보드 배열을 순회하면서 비트보드 설정
        for (int square = 0; square < 64; square++)
        {
            int pieceValue = state.Board[square];
            if (pieceValue == 0) continue; // 빈 칸
            
            int bitboardIndex = state.PieceNumToBitboardIndex(pieceValue);
            if (bitboardIndex >= 0)
            {
                state.BitBoards[bitboardIndex] |= BitHelper.SetBit(square);
            }
        }
        
        // 킹 위치 업데이트 (비트보드에서 킹 찾기)
        if (state.BitBoards[ChessGameState.WHITE_KING] != 0)
        {
            // whiteKingSquare는 private이므로 PlacePiece를 통해 업데이트
            int kingSquare = BitHelper.GetLSBIndex(state.BitBoards[ChessGameState.WHITE_KING]);
            state.PlacePiece(kingSquare, pieceNum.whiteKing);
        }
        
        if (state.BitBoards[ChessGameState.BLACK_KING] != 0)
        {
            int kingSquare = BitHelper.GetLSBIndex(state.BitBoards[ChessGameState.BLACK_KING]);
            state.PlacePiece(kingSquare, pieceNum.blackKing);
        }
    }
    
    // 체스 게임 상태를 FEN 문자열로 변환
    public static string GameStateToFEN(ChessGameState state)
    {
        string fen = BoardToFEN(state.Board);
        
        // 차례
        fen += state.IsWhiteTurn ? " w " : " b ";
        
        // 캐슬링 권한
        string castlingRights = "";
        if (state.WhiteKingSideCastleRight) castlingRights += "K";
        if (state.WhiteQueenSideCastleRight) castlingRights += "Q";
        if (state.BlackKingSideCastleRight) castlingRights += "k";
        if (state.BlackQueenSideCastleRight) castlingRights += "q";
        if (castlingRights == "") castlingRights = "-";
        fen += castlingRights;
        
        // 앙파상 대상
        fen += " ";
        if (state.EnPassantTargetSquare >= 0)
        {
            int file = state.EnPassantTargetSquare % 8;
            int rank = state.EnPassantTargetSquare / 8;
            fen += (char)('a' + file);
            fen += (rank + 1).ToString();
        }
        else
        {
            fen += "-";
        }
        
        // 하프 무브 및 풀 무브 카운터
        fen += " " + state.FiftyMoveCounter + " " + state.FullMoveCounter;
        
        return fen;
    }
    
    // 보드 상태를 FEN 표기법으로 변환
    private static string BoardToFEN(int[] board)
    {
        string fen = "";
        
        for (int rank = 7; rank >= 0; rank--)
        {
            int emptyCount = 0;
            
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                int piece = board[square];
                
                if (piece == 0)
                {
                    emptyCount++;
                }
                else
                {
                    if (emptyCount > 0)
                    {
                        fen += emptyCount.ToString();
                        emptyCount = 0;
                    }
                    
                    // 기물을 FEN 문자로 변환
                    fen += pieceNum.PieceToFenChar(piece);
                }
            }
            
            if (emptyCount > 0)
            {
                fen += emptyCount.ToString();
            }
            
            if (rank > 0)
            {
                fen += "/";
            }
        }
        
        return fen;
    }
} 