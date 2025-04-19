using System;
using System.Collections.Generic;
using UnityEngine;

// 비트보드 연산 도우미 클래스
public static class BitHelper
{
    // 비트보드에서 가장 낮은 위치에 있는 1 비트의 인덱스 찾기
    public static int GetLSBIndex(ulong bitboard)
    {
        if (bitboard == 0) return -1;
        return BitScanForward(bitboard);
    }
    
    // 특정 위치의 비트가 설정되어 있는지 확인
    public static bool IsBitSet(ulong bitboard, int square)
    {
        if (square < 0 || square >= 64)
        {
            Debug.LogWarning($"IsBitSet: 유효하지 않은 인덱스: {square}");
            return false;
        }
        return ((bitboard >> square) & 1UL) != 0;
    }
    
    // 특정 위치의 비트를 설정(1로 만들기)하여 반환
    public static ulong SetBit(int square)
    {
        if (square < 0 || square >= 64)
        {
            Debug.LogWarning($"SetBit: 유효하지 않은 인덱스: {square}");
            return 0;
        }
        return 1UL << square;
    }
    
    // 특정 위치의 비트를 설정(1로 만들기)
    public static ulong SetBitOn(ulong bitboard, int square)
    {
        if (square < 0 || square >= 64) return bitboard;
        return bitboard | (1UL << square);
    }
    
    // 특정 위치의 비트를 해제(0으로 만들기)
    public static ulong ClearBit(ulong bitboard, int square)
    {
        if (square < 0 || square >= 64) return bitboard;
        return bitboard & ~(1UL << square);
    }
    
    // 비트보드에서 설정된 비트(1)의 개수 계산
    public static int CountBits(ulong bitboard)
    {
        int count = 0;
        while (bitboard != 0)
        {
            count++;
            bitboard &= bitboard - 1; // 가장 낮은 비트 제거
        }
        return count;
    }
    
    // 비트보드의 모든 비트 인덱스를 리스트로 반환
    public static List<int> GetSetBitIndexes(ulong bitboard)
    {
        List<int> indexes = new List<int>();
        while (bitboard != 0)
        {
            int index = GetLSBIndex(bitboard);
            indexes.Add(index);
            bitboard = ClearBit(bitboard, index);
        }
        return indexes;
    }
    
    // 비트보드 출력 (디버그용)
    public static string BitboardToString(ulong bitboard)
    {
        string result = "";
        for (int rank = 7; rank >= 0; rank--)
        {
            result += (rank + 1) + " ";
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                result += IsBitSet(bitboard, square) ? "1 " : "0 ";
            }
            result += "\n";
        }
        result += "  a b c d e f g h";
        return result;
    }
    
    // BitScan 알고리즘 (MSB: Most Significant Bit)
    public static int BitScanReverse(ulong bb)
    {
        if (bb == 0) return -1;
        
        int result = 0;
        if ((bb & 0xFFFFFFFF00000000) != 0) { result += 32; bb >>= 32; }
        if ((bb & 0x00000000FFFF0000) != 0) { result += 16; bb >>= 16; }
        if ((bb & 0x000000000000FF00) != 0) { result += 8; bb >>= 8; }
        if ((bb & 0x00000000000000F0) != 0) { result += 4; bb >>= 4; }
        if ((bb & 0x000000000000000C) != 0) { result += 2; bb >>= 2; }
        if ((bb & 0x0000000000000002) != 0) { result += 1; }
        
        // 결과가 유효한 범위(0-63) 내에 있는지 확인
        if (result < 0 || result >= 64)
        {
            Debug.LogWarning($"BitScanReverse: 유효하지 않은 인덱스 반환됨: {result}");
            return -1;
        }
        
        return result;
    }
    
    public static int BitScanReverseOn( ref ulong bb)
    {
        if (bb == 0) return -1;
        
        int result = 0;
        if ((bb & 0xFFFFFFFF00000000) != 0) { result += 32; bb >>= 32; }
        if ((bb & 0x00000000FFFF0000) != 0) { result += 16; bb >>= 16; }
        if ((bb & 0x000000000000FF00) != 0) { result += 8; bb >>= 8; }
        if ((bb & 0x00000000000000F0) != 0) { result += 4; bb >>= 4; }
        if ((bb & 0x000000000000000C) != 0) { result += 2; bb >>= 2; }
        if ((bb & 0x0000000000000002) != 0) { result += 1; }
        
        return result;
    }
    
    // BitScan 알고리즘 (LSB: Least Significant Bit)
    public static int BitScanForward(ulong bb)
    {
        if (bb == 0) return -1;
        
        int result = 0;
        if ((bb & 0x00000000FFFFFFFF) == 0) { result += 32; bb >>= 32; }
        if ((bb & 0x000000000000FFFF) == 0) { result += 16; bb >>= 16; }
        if ((bb & 0x00000000000000FF) == 0) { result += 8; bb >>= 8; }
        if ((bb & 0x000000000000000F) == 0) { result += 4; bb >>= 4; }
        if ((bb & 0x0000000000000003) == 0) { result += 2; bb >>= 2; }
        if ((bb & 0x0000000000000001) == 0) { result += 1; }
        
        // 결과가 유효한 범위(0-63) 내에 있는지 확인
        if (result < 0 || result >= 64)
        {
            Debug.LogWarning($"BitScanForward: 유효하지 않은 인덱스 반환됨: {result}");
            return -1;
        }
        
        return result;
    }
    public static int BitScanForwardOn(ref ulong bb)
    {
        if (bb == 0) return -1;
        
        int result = 0;
        if ((bb & 0x00000000FFFFFFFF) == 0) { result += 32; bb >>= 32; }
        if ((bb & 0x000000000000FFFF) == 0) { result += 16; bb >>= 16; }
        if ((bb & 0x00000000000000FF) == 0) { result += 8; bb >>= 8; }
        if ((bb & 0x000000000000000F) == 0) { result += 4; bb >>= 4; }
        if ((bb & 0x0000000000000003) == 0) { result += 2; bb >>= 2; }
        if ((bb & 0x0000000000000001) == 0) { result += 1; }
        
        return result;
    }
    // 파일(열) 비트보드 생성
    public static ulong FileMask(int file)
    {
        if (file < 0 || file >= 8) return 0;
        return 0x0101010101010101UL << file;
    }
    
    // 랭크(행) 비트보드 생성
    public static ulong RankMask(int rank)
    {
        if (rank < 0 || rank >= 8) return 0;
        return 0xFFUL << (rank * 8);
    }
    
    // 대각선 비트보드 생성
    public static ulong DiagonalMask(int square)
    {
        int file = square % 8;
        int rank = square / 8;
        int diag = file - rank;
        
        ulong result = 0UL;
        for (int r = 0; r < 8; r++)
        {
            int f = r + diag;
            if (f >= 0 && f < 8)
            {
                int sq = r * 8 + f;
                result |= 1UL << sq;
            }
        }
        
        return result;
    }
    
    // 역대각선 비트보드 생성
    public static ulong AntiDiagonalMask(int square)
    {
        int file = square % 8;
        int rank = square / 8;
        int antidiag = file + rank;
        
        ulong result = 0UL;
        for (int r = 0; r < 8; r++)
        {
            int f = antidiag - r;
            if (f >= 0 && f < 8)
            {
                int sq = r * 8 + f;
                result |= 1UL << sq;
            }
        }
        
        return result;
    }
    
    // 체스판 좌표(예: "e4")를 인덱스로 변환
    public static int CoordinateToIndex(string coordinate)
    {
        if (coordinate.Length != 2) return -1;
        
        int file = char.ToLower(coordinate[0]) - 'a';
        int rank = int.Parse(coordinate[1].ToString()) - 1;
        
        if (file < 0 || file > 7 || rank < 0 || rank > 7) return -1;
        
        return rank * 8 + file;
    }
    
    // 인덱스를 체스판 좌표로 변환
    public static string IndexToCoordinate(int index)
    {
        if (index < 0 || index > 63) return "??";
        
        int file = index % 8;
        int rank = index / 8;
        
        return $"{(char)('a' + file)}{rank + 1}";
    }
}
