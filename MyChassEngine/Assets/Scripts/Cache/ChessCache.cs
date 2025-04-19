using System;
using UnityEngine;
using System.Collections.Generic;

public static class ChessCache
{
    #region 캐시 데이터
    // 기본 이동 패턴
    public static readonly ulong[] KnightMoves = new ulong[64];
    public static readonly ulong[] KingMoves = new ulong[64];
    
    // 폰 이동 및 공격
    public static readonly ulong[] WhitePawnMoves = new ulong[64];
    public static readonly ulong[] BlackPawnMoves = new ulong[64];
    public static readonly ulong[] WhitePawnAttacks = new ulong[64];
    public static readonly ulong[] BlackPawnAttacks = new ulong[64];
    public static readonly ulong[] WhitePawnDoubleMoves = new ulong[64];
    public static readonly ulong[] BlackPawnDoubleMoves = new ulong[64];
    
    // 슬라이딩 피스 이동
    public static readonly ulong[] RankMasks = new ulong[64];
    public static readonly ulong[] FileMasks = new ulong[64];
    public static readonly ulong[] DiagonalMasks = new ulong[64];
    public static readonly ulong[] AntiDiagonalMasks = new ulong[64];
    
    // 특수 마스크
    public static readonly ulong[,] BetweenSquares = new ulong[64, 64];
    public static readonly ulong[,] LineSquares = new ulong[64, 64];
    
    // 거리 및 방향
    public static readonly int[,] ManhattanDistance = new int[64, 64];
    public static readonly int[,] ChebyshevDistance = new int[64, 64];
    public static readonly int[,] Direction = new int[64, 64];
    
    // 특수 상수
    public static readonly ulong WhitePromotionRank = 0xFF00000000000000UL;
    public static readonly ulong BlackPromotionRank = 0x00000000000000FFUL;
    public static readonly ulong WhitePawnStartRank = 0x000000000000FF00UL;
    public static readonly ulong BlackPawnStartRank = 0x00FF000000000000UL;
    
    // 캐슬링 관련
    public static readonly ulong WhiteKingSideCastlePath = 0x0000000000000060UL;
    public static readonly ulong WhiteQueenSideCastlePath = 0x000000000000000EUL;
    public static readonly ulong BlackKingSideCastlePath = 0x6000000000000000UL;
    public static readonly ulong BlackQueenSideCastlePath = 0x0E00000000000000UL;

    // 기물 이동 오프셋
    public static readonly int[] KnightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 }; // 나이트 이동
    public static readonly int[] KingOffsets = { -9, -8, -7, -1, 1, 7, 8, 9 }; // 킹 이동
    public static readonly int[] BishopOffsets = { -9, -7, 7, 9 }; // 비숍 이동
    public static readonly int[] RookOffsets = { -8, -1, 1, 8 }; // 룩 이동
    public static readonly int[] QueenOffsets = { -9, -8, -7, -1, 1, 7, 8, 9 }; // 퀸 이동
    public static readonly int[] WhitePawnOffsets = { 7, 8, 9 }; // 흰색 폰 이동 (좌대각선, 전진, 우대각선)
    public static readonly int[] BlackPawnOffsets = { -9, -8, -7 }; // 검은색 폰 이동 (좌대각선, 전진, 우대각선)

    // 각 위치별 유효한 이동 오프셋
    public static readonly int[][] ValidKnightMoves = new int[64][]; // 각 위치별 유효한 나이트 이동
    public static readonly int[][] ValidKingMoves = new int[64][]; // 각 위치별 유효한 킹 이동
    public static readonly int[][] ValidBishopMoves = new int[64][]; // 각 위치별 유효한 비숍 이동
    public static readonly int[][] ValidRookMoves = new int[64][]; // 각 위치별 유효한 룩 이동
    public static readonly int[][] ValidQueenMoves = new int[64][]; // 각 위치별 유효한 퀸 이동
    public static readonly int[][] ValidWhitePawnMoves = new int[64][]; // 각 위치별 유효한 흰색 폰 이동
    public static readonly int[][] ValidBlackPawnMoves = new int[64][]; // 각 위치별 유효한 검은색 폰 이동

    // 슬라이딩 피스 이동 경로 캐시
    public static readonly ulong[][] BishopPaths = new ulong[64][]; // 각 위치별 비숍의 가능한 모든 이동 경로

    public static readonly ulong[] BishopAllPaths = new ulong[64]; // 각 위치별 비숍의 가능한 모든 방향의 이동 경로
    public static readonly ulong[][] RookPaths = new ulong[64][]; // 각 위치별 룩의 가능한 모든 이동 경로
    public static readonly ulong[] RookAllPaths = new ulong[64]; // 각 위치별 룩의 가능한 모든 이동 경로
    public static readonly ulong[][] QueenPaths = new ulong[64][]; // 각 위치별 퀸의 가능한 모든 이동 경로
    public static readonly ulong[] QueenAllPaths = new ulong[64]; // 각 위치별 퀸의 가능한 모든 이동 경로

    // 각 방향별 이동 경로 캐시
    public static readonly ulong[][][] BishopDirectionPaths = new ulong[64][][]; // 각 위치별 비숍의 방향별 이동 경로
    public static readonly ulong[][][] RookDirectionPaths = new ulong[64][][]; // 각 위치별 룩의 방향별 이동 경로
    public static readonly ulong[][][] QueenDirectionPaths = new ulong[64][][]; // 각 위치별 퀸의 방향별 이동 경로

    // 폰 캡처 관련 캐시

    public static readonly ulong[] WhitePawnCaptureMasks = new ulong[64]; // 각 위치별 흰색 폰의 캡처 가능 위치
    public static readonly ulong[] BlackPawnCaptureMasks = new ulong[64]; // 각 위치별 검은색 폰의 캡처 가능 위치
    #endregion

    #region 초기화
    static ChessCache()
    {
        try
        {
            // 1. 기본 마스크 초기화
            InitializeSliderMasks();
            InitializeDistances();
            InitializeBetweenSquares();
            
            // 2. 이동 경로 초기화
            InitializeSliderPaths();
            InitializePawnMoves();
            InitializeKnightMoves();
            InitializeKingMoves();
            
            // 3. 유효한 이동 초기화
            InitializeValidMoves();
            InitializePawnCaptures();
            
            Debug.Log("ChessCache 초기화 완료");
        }
        catch (Exception e)
        {
            Debug.LogError($"ChessCache 초기화 중 오류 발생: {e.Message}");
            throw;
        }
    }

    private static void InitializeKnightMoves()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            ulong bb = 0UL;

            int[] knightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 };
            foreach (int offset in knightOffsets)
            {
                int targetSq = sq + offset;
                if (targetSq >= 0 && targetSq < 64)
                {
                    int targetRank = targetSq / 8;
                    int targetFile = targetSq % 8;
                    if (Math.Abs(rank - targetRank) + Math.Abs(file - targetFile) == 3)
                    {
                        bb |= 1UL << targetSq;
                    }
                }
            }
            KnightMoves[sq] = bb;
        }
    }

    private static void InitializeKingMoves()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            ulong bb = 0UL;

            for (int r = -1; r <= 1; r++)
            {
                for (int f = -1; f <= 1; f++)
                {
                    if (r == 0 && f == 0) continue;
                    
                    int targetRank = rank + r;
                    int targetFile = file + f;
                    
                    if (targetRank >= 0 && targetRank < 8 && targetFile >= 0 && targetFile < 8)
                    {
                        int targetSq = targetRank * 8 + targetFile;
                        bb |= 1UL << targetSq;
                    }
                }
            }
            KingMoves[sq] = bb;
        }
    }

    private static void InitializePawnMoves()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            
            // 흰색 폰 이동
            if (rank < 7)
            {
                WhitePawnMoves[sq] = 1UL << (sq + 8);
                if (rank == 1)
                    WhitePawnDoubleMoves[sq] = 1UL << (sq + 16);
            }
            
            // 흰색 폰 공격
            if (rank < 7)
            {
                if (file > 0) WhitePawnAttacks[sq] |= 1UL << (sq + 7);
                if (file < 7) WhitePawnAttacks[sq] |= 1UL << (sq + 9);
            }
            
            // 검은색 폰 이동
            if (rank > 0)
            {
                BlackPawnMoves[sq] = 1UL << (sq - 8);
                if (rank == 6)
                    BlackPawnDoubleMoves[sq] = 1UL << (sq - 16);
            }
            
            // 검은색 폰 공격
            if (rank > 0)
            {
                if (file > 0) BlackPawnAttacks[sq] |= 1UL << (sq - 9);
                if (file < 7) BlackPawnAttacks[sq] |= 1UL << (sq - 7);
            }
        }
    }

    private static void InitializeSliderMasks()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            int rank = sq / 8;
            int file = sq % 8;
            
            // 가로 방향
            RankMasks[sq] = 0xFFUL << (rank * 8);
            
            // 세로 방향
            FileMasks[sq] = 0x0101010101010101UL << file;
            
            // 대각선 방향
            ulong diagonal = 0UL;
            int r = rank;
            int f = file;
            while (r < 7 && f < 7) { r++; f++; diagonal |= 1UL << (r * 8 + f); }
            r = rank;
            f = file;
            while (r > 0 && f > 0) { r--; f--; diagonal |= 1UL << (r * 8 + f); }
            DiagonalMasks[sq] = diagonal;
            
            // 반대 대각선 방향
            ulong antiDiagonal = 0UL;
            r = rank;
            f = file;
            while (r < 7 && f > 0) { r++; f--; antiDiagonal |= 1UL << (r * 8 + f); }
            r = rank;
            f = file;
            while (r > 0 && f < 7) { r--; f++; antiDiagonal |= 1UL << (r * 8 + f); }
            AntiDiagonalMasks[sq] = antiDiagonal;
        }
    }

    private static void InitializeDistances()
    {
        for (int sq1 = 0; sq1 < 64; sq1++)
        {
            int rank1 = sq1 / 8;
            int file1 = sq1 % 8;
            
            for (int sq2 = 0; sq2 < 64; sq2++)
            {
                int rank2 = sq2 / 8;
                int file2 = sq2 % 8;
                
                // 맨해튼 거리
                ManhattanDistance[sq1, sq2] = Math.Abs(rank1 - rank2) + Math.Abs(file1 - file2);
                
                // 체비셰프 거리
                ChebyshevDistance[sq1, sq2] = Math.Max(Math.Abs(rank1 - rank2), Math.Abs(file1 - file2));
                
                // 방향
                Direction[sq1, sq2] = GetDirection(sq1, sq2);
            }
        }
    }

    private static void InitializeBetweenSquares()
    {
        for (int sq1 = 0; sq1 < 64; sq1++)
        {
            for (int sq2 = 0; sq2 < 64; sq2++)
            {
                BetweenSquares[sq1, sq2] = GetBetweenSquaresMask(sq1, sq2);
                LineSquares[sq1, sq2] = GetLineSquaresMask(sq1, sq2);
            }
        }
    }

    private static void InitializeValidMoves()
    {
        for (int square = 0; square < 64; square++)
        {
            // 나이트 이동 초기화
            ValidKnightMoves[square] = new int[8];
            int validKnightCount = 0;
            foreach (int offset in KnightOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.Knight))
                {
                    ValidKnightMoves[square][validKnightCount++] = offset;
                }
            }
            Array.Resize(ref ValidKnightMoves[square], validKnightCount);

            // 킹 이동 초기화
            ValidKingMoves[square] = new int[8];
            int validKingCount = 0;
            foreach (int offset in KingOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.King))
                {
                    ValidKingMoves[square][validKingCount++] = offset;
                }
            }
            Array.Resize(ref ValidKingMoves[square], validKingCount);

            // 비숍 이동 초기화
            ValidBishopMoves[square] = new int[4];
            int validBishopCount = 0;
            foreach (int offset in BishopOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.Bishop))
                {
                    ValidBishopMoves[square][validBishopCount++] = offset;
                }
            }
            Array.Resize(ref ValidBishopMoves[square], validBishopCount);

            // 룩 이동 초기화
            ValidRookMoves[square] = new int[4];
            int validRookCount = 0;
            foreach (int offset in RookOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.Rook))
                {
                    ValidRookMoves[square][validRookCount++] = offset;
                }
            }
            Array.Resize(ref ValidRookMoves[square], validRookCount);

            // 퀸 이동 초기화
            ValidQueenMoves[square] = new int[8];
            int validQueenCount = 0;
            foreach (int offset in QueenOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.Queen))
                {
                    ValidQueenMoves[square][validQueenCount++] = offset;
                }
            }
            Array.Resize(ref ValidQueenMoves[square], validQueenCount);

            // 폰 이동 초기화
            ValidWhitePawnMoves[square] = new int[3];
            int validWhitePawnCount = 0;
            foreach (int offset in WhitePawnOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.Pawn))
                {
                    ValidWhitePawnMoves[square][validWhitePawnCount++] = offset;
                }
            }
            Array.Resize(ref ValidWhitePawnMoves[square], validWhitePawnCount);

            ValidBlackPawnMoves[square] = new int[3];
            int validBlackPawnCount = 0;
            foreach (int offset in BlackPawnOffsets)
            {
                int target = square + offset;
                if (IsValidSquare(square, target, offset, PieceType.Pawn))
                {
                    ValidBlackPawnMoves[square][validBlackPawnCount++] = offset;
                }
            }
            Array.Resize(ref ValidBlackPawnMoves[square], validBlackPawnCount);
        }
    }

    private static bool IsValidSquare(int from, int to, int offset, PieceType pieceType)
    {
        // 기본 범위 검사
        if (to < 0 || to >= 64) return false;
        
        int fromRank = from / 8;
        int fromFile = from % 8;
        int toRank = to / 8;
        int toFile = to % 8;
        
        int rankDiff = Math.Abs(fromRank - toRank);
        int fileDiff = Math.Abs(fromFile - toFile);
        
        switch (pieceType)
        {
            case PieceType.Knight:
                // 나이트는 L자 모양으로 이동 (2칸 + 1칸)
                return (rankDiff == 2 && fileDiff == 1) || (rankDiff == 1 && fileDiff == 2);
                
            case PieceType.King:
                // 킹은 한 칸만 이동 가능
                return rankDiff <= 1 && fileDiff <= 1 && (rankDiff + fileDiff) > 0;
                
            case PieceType.Bishop:
                // 비숍은 대각선으로만 이동
                return rankDiff == fileDiff && rankDiff > 0;
                
            case PieceType.Rook:
                // 룩은 직선으로만 이동
                return (rankDiff == 0 && fileDiff > 0) || (fileDiff == 0 && rankDiff > 0);
                
            case PieceType.Queen:
                // 퀸은 대각선과 직선 모두 이동 가능
                return (rankDiff == fileDiff && rankDiff > 0) || 
                       (rankDiff == 0 && fileDiff > 0) || 
                       (fileDiff == 0 && rankDiff > 0);
                
            case PieceType.Pawn:
                // 폰의 이동 처리
                int forwardDir = fromRank < toRank ? 1 : -1; // 흰색은 위로, 검은색은 아래로
                int pawnRankDiff = toRank - fromRank;
                int pawnFileDiff = Math.Abs(toFile - fromFile);
                
                // 일반 전진 (한 칸)
                if (pawnFileDiff == 0 && pawnRankDiff == forwardDir)
                    return true;
                
                // 첫 이동 시 두 칸 전진
                if (pawnFileDiff == 0 && pawnRankDiff == 2 * forwardDir && 
                    ((fromRank == 1 && forwardDir == 1) || (fromRank == 6 && forwardDir == -1)))
                    return true;
                
                // 대각선 캡처 (한 칸)
                if (pawnFileDiff == 1 && pawnRankDiff == forwardDir)
                    return true;
                
                return false;
                
            default:
                return false;
        }
    }

    private static void InitializeSliderPaths()
    {
        // 방향 정의 (시계 방향으로 8방향)
        int[] directions = { -9, -8, -7, 1, 9, 8, 7, -1 }; // 북서, 북, 북동, 동, 남동, 남, 남서, 서

        for (int square = 0; square < 64; square++)
        {
            int rank = square / 8;
            int file = square % 8;

            // 비숍 경로 초기화 (대각선 방향: 0, 2, 4, 6)
            BishopPaths[square] = new ulong[4];
            BishopDirectionPaths[square] = new ulong[4][];
            for (int i = 0; i < 4; i++)
            {
                BishopDirectionPaths[square][i] = new ulong[8]; // 최대 8칸까지 이동 가능
            }
            BishopAllPaths[square] = 0;

            // 룩 경로 초기화 (직선 방향: 1, 3, 5, 7)
            RookPaths[square] = new ulong[4];
            RookDirectionPaths[square] = new ulong[4][];
            for (int i = 0; i < 4; i++)
            {
                RookDirectionPaths[square][i] = new ulong[8]; // 최대 8칸까지 이동 가능
            }
            RookAllPaths[square] = 0;

            // 퀸 경로 초기화 (모든 방향)
            QueenPaths[square] = new ulong[8];
            QueenDirectionPaths[square] = new ulong[8][];
            for (int i = 0; i < 8; i++)
            {
                QueenDirectionPaths[square][i] = new ulong[8]; // 최대 8칸까지 이동 가능
            }
            QueenAllPaths[square] = 0;

            // 각 방향별로 경로 계산
            for (int dirIndex = 0; dirIndex < 8; dirIndex++)
            {
                int dir = directions[dirIndex];
                int currentSquare = square;
                ulong path = 0;
                List<ulong> directionPath = new List<ulong>();

                while (true)
                {
                    int nextSquare = currentSquare + dir;
                    if (nextSquare < 0 || nextSquare >= 64) break;

                    int nextRank = nextSquare / 8;
                    int nextFile = nextSquare % 8;

                    // 경계 체크
                    if (Math.Abs(nextRank - rank) > 1 || Math.Abs(nextFile - file) > 1)
                    {
                        // 대각선 이동인 경우
                        if (dirIndex % 2 == 0) // 대각선 방향
                        {
                            if (Math.Abs(nextRank - rank) != Math.Abs(nextFile - file)) break;
                        }
                        // 직선 이동인 경우
                        else
                        {
                            if (nextRank != rank && nextFile != file) break;
                        }
                    }

                    // 각 단계의 개별 비트만 저장 (001, 010, 100 방식)
                    ulong singleBit = 1UL << nextSquare;
                    directionPath.Add(singleBit);
                    
                    // 전체 경로에는 누적
                    path |= singleBit;
                    currentSquare = nextSquare;
                }

                // 경로 저장
                if (dirIndex % 2 == 0) // 대각선 방향 (비숍)
                {
                    int bishopDirIndex = dirIndex / 2;
                    BishopPaths[square][bishopDirIndex] = path;
                    BishopDirectionPaths[square][bishopDirIndex] = directionPath.ToArray();
                    BishopAllPaths[square] |= path;
                }
                else // 직선 방향 (룩)
                {
                    int rookDirIndex = (dirIndex - 1) / 2;
                    RookPaths[square][rookDirIndex] = path;
                    RookDirectionPaths[square][rookDirIndex] = directionPath.ToArray();
                    RookAllPaths[square] |= path;
                }

                // 퀸은 모든 방향 저장
                QueenPaths[square][dirIndex] = path;
                QueenDirectionPaths[square][dirIndex] = directionPath.ToArray();
                QueenAllPaths[square] |= path;
            }
        }
    }

    private static void InitializePawnCaptures()
    {
        for (int square = 0; square < 64; square++)
        {
            int rank = square / 8;
            int file = square % 8;

            // 흰색 폰 캡처 초기화
            List<int> validWhiteCaptures = new List<int>();
            ulong whiteCaptureMask = 0;
            int[] whiteCaptureOffsets = { 7, 9 }; // 좌대각선, 우대각선

            foreach (int offset in whiteCaptureOffsets)
            {
                int targetSquare = square + offset;
                if (IsValidSquare(square, targetSquare, offset, PieceType.Pawn))
                {
                    validWhiteCaptures.Add(offset);
                    whiteCaptureMask |= 1UL << targetSquare;
                }
            }

            WhitePawnCaptureMasks[square] = whiteCaptureMask;

            // 검은색 폰 캡처 초기화
            List<int> validBlackCaptures = new List<int>();
            ulong blackCaptureMask = 0;
            int[] blackCaptureOffsets = { -9, -7 }; // 좌대각선, 우대각선

            foreach (int offset in blackCaptureOffsets)
            {
                int targetSquare = square + offset;
                if (IsValidSquare(square, targetSquare, offset, PieceType.Pawn))
                {
                    validBlackCaptures.Add(offset);
                    blackCaptureMask |= 1UL << targetSquare;
                }
            }

            BlackPawnCaptureMasks[square] = blackCaptureMask;
        }
    }
    #endregion

    #region 유틸리티 메서드
    private static int GetDirection(int from, int to)
    {
        int rankDiff = (to / 8) - (from / 8);
        int fileDiff = (to % 8) - (from % 8);
        
        if (rankDiff == 0 && fileDiff > 0) return 0; // 동
        if (rankDiff < 0 && fileDiff > 0) return 1; // 북동
        if (rankDiff < 0 && fileDiff == 0) return 2; // 북
        if (rankDiff < 0 && fileDiff < 0) return 3; // 북서
        if (rankDiff == 0 && fileDiff < 0) return 4; // 서
        if (rankDiff > 0 && fileDiff < 0) return 5; // 남서
        if (rankDiff > 0 && fileDiff == 0) return 6; // 남
        if (rankDiff > 0 && fileDiff > 0) return 7; // 남동
        
        return -1; // 같은 위치
    }

    private static ulong GetBetweenSquaresMask(int sq1, int sq2)
    {
        ulong result = 0UL;
        int dir = GetDirection(sq1, sq2);
        if (dir == -1) return result;

        int current = sq1;
        while (current != sq2)
        {
            switch (dir)
            {
                case 0: current += 1; break;
                case 1: current -= 7; break;
                case 2: current -= 8; break;
                case 3: current -= 9; break;
                case 4: current -= 1; break;
                case 5: current += 7; break;
                case 6: current += 8; break;
                case 7: current += 9; break;
            }
            
            if (current == sq2) break;
            if (current < 0 || current > 63) return 0UL;
            
            result |= 1UL << current;
        }
        
        return result;
    }

    private static ulong GetLineSquaresMask(int sq1, int sq2)
    {
        ulong result = GetBetweenSquaresMask(sq1, sq2);
        result |= 1UL << sq1;
        result |= 1UL << sq2;
        return result;
    }

    // 체스판 좌표를 인덱스로 변환
    public static int CoordinateToIndex(string coordinate)
    {
        if (coordinate.Length != 2) return -1;
        int file = char.ToLower(coordinate[0]) - 'a';
        int rank = coordinate[1] - '1';
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        return rank * 8 + file;
    }

    // 인덱스를 체스판 좌표로 변환
    public static string IndexToCoordinate(int index)
    {
        if (index < 0 || index > 63) return "??";
        int rank = index / 8;
        int file = index % 8;
        return $"{(char)('a' + file)}{rank + 1}";
    }
    #endregion

    #region 캐시 접근 메서드
    // 나이트 이동 가능 위치 얻기
    public static ulong GetKnightAttacks(int square)
    {
        return KnightMoves[square];
    }

    // 킹 이동 가능 위치 얻기
    public static ulong GetKingAttacks(int square)
    {
        return KingMoves[square];
    }

    // 폰 공격 위치 얻기
    public static ulong GetPawnAttacks(int square, bool isWhite)
    {
        return isWhite ? WhitePawnAttacks[square] : BlackPawnAttacks[square];
    }    


    // 두 지점 사이의 경로 얻기   
    public static ulong GetBetweenMask(int from, int to)
    {
        return BetweenSquares[from, to];
    }

    // 한 줄의 모든 위치 얻기
    public static ulong GetLineMask(int from, int to)
    {
        return LineSquares[from, to];
    }

    // 거리 얻기
    public static int GetManhattanDistance(int from, int to)
    {
        return ManhattanDistance[from, to];
    }

    public static int GetChebyshevDistance(int from, int to)
    {
        return ChebyshevDistance[from, to];
    }

    // 슬라이딩 피스의 이동 경로 계산을 위한 유틸리티 메서드
    public static ulong GetBishopPath(int square, int direction)
    {
        return BishopPaths[square][direction];
    }

    public static ulong GetRookPath(int square, int direction)
    {
        return RookPaths[square][direction];
    }

    public static ulong GetQueenPath(int square, int direction)
    {
        return QueenPaths[square][direction];
    }

    public static ulong[] GetBishopDirectionPath(int square, int direction)
    {
        return BishopDirectionPaths[square][direction];
    }

    public static ulong[] GetRookDirectionPath(int square, int direction)
    {
        return RookDirectionPaths[square][direction];
    }

    public static ulong[] GetQueenDirectionPath(int square, int direction)
    {
        return QueenDirectionPaths[square][direction];
    }
    #endregion

    #region 디버그 메서드
    public static void DebugPrintAllCaches()
    {
        Debug.Log("===== 체스 캐시 디버그 정보 =====");
        
        // 슬라이딩 피스 마스크
        Debug.Log("\n슬라이딩 피스 마스크:");
        for (int i = 0; i < 64; i++)
        {
            Debug.Log($"\n위치 {i} ({(char)('a' + (i % 8))}{i / 8 + 1}):");
            Debug.Log($"랭크 마스크:\n{BitHelper.BitboardToString(RankMasks[i])}");
            Debug.Log($"파일 마스크:\n{BitHelper.BitboardToString(FileMasks[i])}");
            Debug.Log($"대각선 마스크:\n{BitHelper.BitboardToString(DiagonalMasks[i])}");
            Debug.Log($"역대각선 마스크:\n{BitHelper.BitboardToString(AntiDiagonalMasks[i])}");
        }

        // 기물별 이동 가능 위치
        Debug.Log("\n기물별 이동 가능 위치:");
        for (int i = 0; i < 64; i++)
        {
            Debug.Log($"\n위치 {i} ({(char)('a' + (i % 8))}{i / 8 + 1}):");
            Debug.Log($"나이트 이동:\n{BitHelper.BitboardToString(KnightMoves[i])}");
            Debug.Log($"킹 이동:\n{BitHelper.BitboardToString(KingMoves[i])}");
            Debug.Log($"백색 폰 이동:\n{BitHelper.BitboardToString(WhitePawnMoves[i])}");
            Debug.Log($"흑색 폰 이동:\n{BitHelper.BitboardToString(BlackPawnMoves[i])}");
            Debug.Log($"백색 폰 캡처:\n{BitHelper.BitboardToString(WhitePawnCaptureMasks[i])}");
            Debug.Log($"흑색 폰 캡처:\n{BitHelper.BitboardToString(BlackPawnCaptureMasks[i])}");
        }

        // 슬라이딩 피스 경로
        Debug.Log("\n슬라이딩 피스 경로:");
        for (int i = 0; i < 64; i++)
        {
            Debug.Log($"\n위치 {i} ({(char)('a' + (i % 8))}{i / 8 + 1}):");
            for (int dir = 0; dir < 4; dir++)
            {
                Debug.Log($"비숍 경로 {dir}:\n{BitHelper.BitboardToString(BishopPaths[i][dir])}");
                Debug.Log($"룩 경로 {dir}:\n{BitHelper.BitboardToString(RookPaths[i][dir])}");
            }
            for (int dir = 0; dir < 8; dir++)
            {
                Debug.Log($"퀸 경로 {dir}:\n{BitHelper.BitboardToString(QueenPaths[i][dir])}");
            }
        }
    }

    public static void DebugPrintCacheForSquare(int square)
    {
        if (square < 0 || square >= 64)
        {
            Debug.LogError($"유효하지 않은 위치입니다: {square}");
            return;
        }

        Debug.Log($"===== 위치 {square} ({(char)('a' + (square % 8))}{square / 8 + 1}) 캐시 정보 =====");
        
        // 기본 마스크
        Debug.Log("\n기본 마스크:");
        Debug.Log($"랭크 마스크:\n{BitHelper.BitboardToString(RankMasks[square])}");
        Debug.Log($"파일 마스크:\n{BitHelper.BitboardToString(FileMasks[square])}");
        Debug.Log($"대각선 마스크:\n{BitHelper.BitboardToString(DiagonalMasks[square])}");
        Debug.Log($"역대각선 마스크:\n{BitHelper.BitboardToString(AntiDiagonalMasks[square])}");

        // 기물 이동
        Debug.Log("\n기물 이동:");
        Debug.Log($"나이트 이동:\n{BitHelper.BitboardToString(KnightMoves[square])}");
        Debug.Log($"킹 이동:\n{BitHelper.BitboardToString(KingMoves[square])}");
        Debug.Log($"백색 폰 이동:\n{BitHelper.BitboardToString(WhitePawnMoves[square])}");
        Debug.Log($"흑색 폰 이동:\n{BitHelper.BitboardToString(BlackPawnMoves[square])}");
        Debug.Log($"백색 폰 캡처:\n{BitHelper.BitboardToString(WhitePawnCaptureMasks[square])}");
        Debug.Log($"흑색 폰 캡처:\n{BitHelper.BitboardToString(BlackPawnCaptureMasks[square])}");

        // 슬라이딩 피스 경로
        Debug.Log("\n슬라이딩 피스 경로:");
        for (int dir = 0; dir < 4; dir++)
        {
            Debug.Log($"비숍 경로 {dir}:\n{BitHelper.BitboardToString(BishopPaths[square][dir])}");
            Debug.Log($"룩 경로 {dir}:\n{BitHelper.BitboardToString(RookPaths[square][dir])}");
        }
        for (int dir = 0; dir < 8; dir++)
        {
            Debug.Log($"퀸 경로 {dir}:\n{BitHelper.BitboardToString(QueenPaths[square][dir])}");
        }
    }
    #endregion
}

public enum PieceType
{
    None = 0,
    Pawn = 1,
    Knight = 2,
    Bishop = 3,
    Rook = 4,
    Queen = 5,
    King = 6
} 