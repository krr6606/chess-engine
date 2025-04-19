using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class MoveGenerator
{
    	public const int MaxMoves = 100;

		public enum PromotionMode { All, QueenOnly, QueenAndKnight }

		public PromotionMode promotionsToGenerate = PromotionMode.All;

		private ChessGameState gameState;
		private int currMoveIndex;
		private ulong moveTypeMask;
    		public List<Move> GenerateMoves(ChessGameState board, bool includeQuietMoves = true)
		{
			if (board == null) throw new ArgumentNullException(nameof(board));
			
			List<Move> moves = new List<Move>(MaxMoves);
			try
			{
				this.gameState = board;
				bool isWhiteTurn = board.IsWhiteTurn;
				
				if (isWhiteTurn)
				{
					GeneratePawnMoves(board, moves, true);
					GenerateKingMoves(board, moves, true);
					GenerateBishopMoves(board, moves, true);
					GenerateRookMoves(board, moves, true);
					GenerateKnightMoves(board, moves, true);
					GenerateQueenMoves(board, moves, true);
				}
				else
				{
					GeneratePawnMoves(board, moves, false);
					GenerateKingMoves(board, moves, false);
					GenerateBishopMoves(board, moves, false);
					GenerateRookMoves(board, moves, false);
					GenerateKnightMoves(board, moves, false);
					GenerateQueenMoves(board, moves, false);
				}

				// 생성된 모든 이동 디버그 출력
				Debug.Log($"\n===== 생성된 이동 목록 ({(isWhiteTurn ? "백색" : "흑색")} 차례) =====");
				foreach (var move in moves)
				{
					string moveType = "일반 이동";
					if (move.HasFlag(Move.MoveFlag.Capture)) moveType = "캡처";
					else if (move.HasFlag(Move.MoveFlag.PawnTwoForward)) moveType = "폰 2칸 전진";
					else if (move.HasFlag(Move.MoveFlag.EnPassant)) moveType = "앙파상";
					else if (move.HasFlag(Move.MoveFlag.KingSideCastle)) moveType = "킹 사이드 캐슬링";
					else if (move.HasFlag(Move.MoveFlag.QueenSideCastle)) moveType = "퀸 사이드 캐슬링";
					else if (move.HasFlag(Move.MoveFlag.Promotion)) moveType = "프로모션";

					// 기물 타입 확인
					string pieceType = "알 수 없음";
					int fromSquare = move.FromSquare;
					if (fromSquare >= 0 && fromSquare < 64)
					{
						int pieceValue = board.Board[fromSquare];
						if (pieceValue != 0)
						{
							bool isWhite = (pieceValue & pieceNum.colorMask) == pieceNum.white;
							int pieceTypeValue = pieceValue & pieceNum.pieceMask;
							
							if ((pieceTypeValue & pieceNum.pwan) != 0) pieceType = "폰";
							else if ((pieceTypeValue & pieceNum.knight) != 0) pieceType = "나이트";
							else if ((pieceTypeValue & pieceNum.bishop) != 0) pieceType = "비숍";
							else if ((pieceTypeValue & pieceNum.rook) != 0) pieceType = "룩";
							else if ((pieceTypeValue & pieceNum.queen) != 0) pieceType = "퀸";
							else if ((pieceTypeValue & pieceNum.king) != 0) pieceType = "킹";
						}
					}

					string fromCoord = BitHelper.IndexToCoordinate(move.FromSquare);
					string toCoord = BitHelper.IndexToCoordinate(move.ToSquare);
					Debug.Log($"{pieceType} {fromCoord} -> {toCoord} ({moveType})");
				}
				Debug.Log($"총 {moves.Count}개의 이동이 생성되었습니다.");
			}
			catch (Exception e)
			{
				Debug.LogError($"이동 생성 중 오류 발생: {e.Message}");
			}
			finally
			{
				this.gameState = null;
			}
			return moves;
		}
        void Init()
		{
			// Reset state
			currMoveIndex = 0;
		}

		 int GeneratePawnMoves(ChessGameState board, List<Move> moves, bool isWhite)
		{
			int count = 0;
			ulong pawnBitboard = board.BitBoards[isWhite ? ChessGameState.WHITE_PAWN : ChessGameState.BLACK_PAWN];
			ulong friendlyPieces = isWhite ? board.WhitePieces : board.BlackPieces;
			ulong allPieces = board.AllPieces;
			ulong enemyPieces = isWhite ? board.BlackPieces : board.WhitePieces;

			bool isBlocked = false;
			while (pawnBitboard != 0)
			{
				int pawnIndex = BitHelper.BitScanForward(pawnBitboard);
				if (pawnIndex == -1) break;
				Coord fromCoord = new Coord(pawnIndex);
				ulong currentPawn = BitHelper.SetBit(pawnIndex);
				pawnBitboard &= ~currentPawn; // 현재 폰 비트 제거

				// 일반 전진 이동
				ulong pawnMovesBitboard = (isWhite ? ChessCache.WhitePawnMoves[pawnIndex] : ChessCache.BlackPawnMoves[pawnIndex]) & ~friendlyPieces;
				if (pawnMovesBitboard != 0)
				{
					int toIndex = BitHelper.BitScanForward(pawnMovesBitboard);
					if(( allPieces & BitHelper.SetBit(toIndex)) != 0){
						isBlocked = true;
						break;
						}
					if (toIndex != -1)
					{

						Coord toCoord = new Coord(toIndex);
						moves.Add(new Move(fromCoord, toCoord));
						pawnMovesBitboard &= ~BitHelper.SetBit(toIndex);
						count++;
					}
				}


				// 캡처 이동
				ulong captureMask = isWhite ? ChessCache.WhitePawnCaptureMasks[pawnIndex] : ChessCache.BlackPawnCaptureMasks[pawnIndex];
				ulong captureTargets = captureMask & enemyPieces;

				while (captureTargets != 0)
				{
					int toIndex = BitHelper.BitScanForward(captureTargets);
					if (toIndex == -1) break;
					Coord toCoord = new Coord(toIndex);
					moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.Capture));
					captureTargets &= captureTargets - 1;
					count++;
				}

				if(isBlocked) continue;
								// 더블 폰 이동
				if ((currentPawn & (isWhite ? ChessCache.WhitePawnStartRank : ChessCache.BlackPawnStartRank)) != 0)
				{
					ulong doublePawnMoves = (isWhite ? ChessCache.WhitePawnDoubleMoves[pawnIndex] : ChessCache.BlackPawnDoubleMoves[pawnIndex]) & ~friendlyPieces;
					if (doublePawnMoves != 0)
					{
						int toIndex = BitHelper.BitScanForward(doublePawnMoves);
						if (toIndex != -1)
						{
							Coord toCoord = new Coord(toIndex);
							moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.PawnTwoForward));
							doublePawnMoves &= ~BitHelper.SetBit(toIndex);
							count++;
						}
					}
				}

			}
			return count;
		}
		int GenerateKnightMoves(ChessGameState board, List<Move> moves, bool isWhite)
		{
			int count = 0;
			ulong knightBitboard = board.BitBoards[isWhite ? ChessGameState.WHITE_KNIGHT : ChessGameState.BLACK_KNIGHT];
			ulong friendlyPieces = isWhite ? board.WhitePieces : board.BlackPieces;
			ulong enemyPieces = isWhite ? board.BlackPieces : board.WhitePieces;

			while (knightBitboard != 0)
			{
				int knightIndex = BitHelper.BitScanForward(knightBitboard);
				if (knightIndex == -1) break;
				knightBitboard &= ~BitHelper.SetBit(knightIndex);
				Coord fromCoord = new Coord(knightIndex);
				ulong knightMoves = ChessCache.KnightMoves[knightIndex];
				ulong emptySquares = knightMoves & ~board.AllPieces;
				ulong captureSquares = knightMoves & (isWhite ? board.BlackPieces : board.WhitePieces);

				// 빈 칸으로의 이동
				while (emptySquares != 0)
				{
					int toIndex = BitHelper.BitScanForward(emptySquares);
					if (toIndex == -1) break;
					Coord toCoord = new Coord(toIndex);
					moves.Add(new Move(fromCoord, toCoord));
					emptySquares &= ~BitHelper.SetBit(toIndex);
					count++;
				}

				// 캡처 이동
				while (captureSquares != 0)
				{
					int toIndex = BitHelper.BitScanForward(captureSquares);
					if (toIndex == -1) break;
					Coord toCoord = new Coord(toIndex);
					moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.Capture));
					captureSquares &= ~BitHelper.SetBit(toIndex);
					count++;
				}
			}
			return count;
		}
		int GenerateBishopMoves(ChessGameState board, List<Move> moves, bool isWhite)
		{
			if(board == null)  
			{
				Debug.LogError("board is null");
				return 0;
			}
			int count = 0;
			ulong bishopBitboard = board.BitBoards[isWhite ? ChessGameState.WHITE_BISHOP : ChessGameState.BLACK_BISHOP];
			ulong friendlyPieces = isWhite ? board.WhitePieces : board.BlackPieces;
			ulong enemyPieces = isWhite ? board.BlackPieces : board.WhitePieces;
			ulong allPieces = board.AllPieces;

			while (bishopBitboard != 0)
			{
				int bishopIndex = BitHelper.BitScanForward(bishopBitboard);
				if (bishopIndex == -1) break;
				bishopBitboard = BitHelper.ClearBit(bishopBitboard, bishopIndex);

				Coord fromCoord = new Coord(bishopIndex);

				// 각 대각선 방향으로 이동 생성
				for (int dir = 0; dir < 4; dir++)
				{
					// 이 방향으로의 순차적 경로 가져오기
					ulong[] directionPath = ChessCache.GetBishopDirectionPath(bishopIndex, dir);
					if (directionPath == null || directionPath.Length == 0) continue;
					
					// 한 칸씩 확인 (각 단계는 개별 비트로 저장되어 있음)
					for (int step = 0; step < directionPath.Length; step++)
					{
						if (directionPath[step] == 0) continue;
						
						// 이미 개별 비트로 저장되어 있으므로 바로 사용
						ulong squareBit = directionPath[step];
						int targetSquare = BitHelper.BitScanForward(squareBit);
						if (targetSquare == -1) continue;
						
						// 기물 충돌 검사
						if ((allPieces & squareBit) != 0)
						{
							// 적 기물이면 캡처하고 이 방향 종료
							if ((enemyPieces & squareBit) != 0)
							{
								Coord toCoord = new Coord(targetSquare);
								moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.Capture));
								count++;
							}
							// 이 방향의 이동 종료 (아군이든 적군이든 더 이상 진행 불가)
							break;
						}
						else
						{
							// 빈 칸이면 이동 가능
							Coord toCoord = new Coord(targetSquare);
							moves.Add(new Move(fromCoord, toCoord));
							count++;
						}
					}
				}
			}
			return count;
		}
		int GenerateRookMoves(ChessGameState board, List<Move> moves, bool isWhite)
		{
			if(board == null)  
			{
				Debug.LogError("board is null");
				return 0;
			}
			int count = 0;
			ulong rookBitboard = board.BitBoards[isWhite ? ChessGameState.WHITE_ROOK : ChessGameState.BLACK_ROOK];
			ulong friendlyPieces = isWhite ? board.WhitePieces : board.BlackPieces;
			ulong enemyPieces = isWhite ? board.BlackPieces : board.WhitePieces;
			ulong allPieces = board.AllPieces;

			while (rookBitboard != 0)
			{
				int rookIndex = BitHelper.BitScanForward(rookBitboard);
				if (rookIndex == -1) break;
				rookBitboard = BitHelper.ClearBit(rookBitboard, rookIndex);
				Coord fromCoord = new Coord(rookIndex);

				// 각 직선 방향으로 이동 생성
				for (int dir = 0; dir < 4; dir++)
				{
					// 이 방향으로의 순차적 경로 가져오기
					ulong[] directionPath = ChessCache.GetRookDirectionPath(rookIndex, dir);
					if (directionPath == null || directionPath.Length == 0) continue;
					
					// 한 칸씩 확인 (각 단계는 개별 비트로 저장되어 있음)
					for (int step = 0; step < directionPath.Length; step++)
					{
						if (directionPath[step] == 0) continue;
						
						// 이미 개별 비트로 저장되어 있으므로 바로 사용
						ulong squareBit = directionPath[step];
						int targetSquare = BitHelper.BitScanForward(squareBit);
						if (targetSquare == -1) continue;
						
						// 기물 충돌 검사
						if ((allPieces & squareBit) != 0)
						{
							// 적 기물이면 캡처하고 이 방향 종료
							if ((enemyPieces & squareBit) != 0)
							{
								Coord toCoord = new Coord(targetSquare);
								moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.Capture));
								count++;
							}
							// 이 방향의 이동 종료 (아군이든 적군이든 더 이상 진행 불가)
							break;
						}
						else
						{
							// 빈 칸이면 이동 가능
							Coord toCoord = new Coord(targetSquare);
							moves.Add(new Move(fromCoord, toCoord));
							count++;
						}
					}
				}
			}
			return count;
		}
		int GenerateQueenMoves(ChessGameState board, List<Move> moves, bool isWhite)
		{
			if(board == null)  
			{
				Debug.LogError("board is null");
				return 0;
			}
			int count = 0;
			ulong queenBitboard = board.BitBoards[isWhite ? ChessGameState.WHITE_QUEEN : ChessGameState.BLACK_QUEEN];
			ulong friendlyPieces = isWhite ? board.WhitePieces : board.BlackPieces;
			ulong enemyPieces = isWhite ? board.BlackPieces : board.WhitePieces;
			ulong allPieces = board.AllPieces;

			while (queenBitboard != 0)
			{
				int queenIndex = BitHelper.BitScanForward(queenBitboard);
				if (queenIndex == -1) break;
				queenBitboard = BitHelper.ClearBit(queenBitboard, queenIndex);
				Coord fromCoord = new Coord(queenIndex);

				// 각 방향으로 이동 생성 (대각선 + 직선)
				for (int dir = 0; dir < 8; dir++)
				{
					// 이 방향으로의 순차적 경로 가져오기
					ulong[] directionPath = ChessCache.GetQueenDirectionPath(queenIndex, dir);
					if (directionPath == null || directionPath.Length == 0) continue;
					
					// 한 칸씩 확인 (각 단계는 개별 비트로 저장되어 있음)
					for (int step = 0; step < directionPath.Length; step++)
					{
						if (directionPath[step] == 0) continue;
						
						// 이미 개별 비트로 저장되어 있으므로 바로 사용
						ulong squareBit = directionPath[step];
						int targetSquare = BitHelper.BitScanForward(squareBit);
						if (targetSquare == -1) continue;
						
						// 기물 충돌 검사
						if ((allPieces & squareBit) != 0)
						{
							// 적 기물이면 캡처하고 이 방향 종료
							if ((enemyPieces & squareBit) != 0)
							{
								Coord toCoord = new Coord(targetSquare);
								moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.Capture));
								count++;
							}
							// 이 방향의 이동 종료 (아군이든 적군이든 더 이상 진행 불가)
							break;
						}
						else
						{
							// 빈 칸이면 이동 가능
							Coord toCoord = new Coord(targetSquare);
							moves.Add(new Move(fromCoord, toCoord));
							count++;
						}
					}
				}
			}
			return count;
		}
		int GenerateKingMoves(ChessGameState board, List<Move> moves, bool isWhite)
		{
			int count = 0;
			int kingIndex = isWhite ? board.WhiteKingSquare : board.BlackKingSquare;
			if (kingIndex < 0 || kingIndex >= 64) return count;

			var validMoves = ChessCache.ValidKingMoves[kingIndex];
			if (validMoves == null) return count;

			Coord fromCoord = new Coord(kingIndex);
			ulong enemyPieces = isWhite ? board.BlackPieces : board.WhitePieces;

			foreach (int offset in validMoves)
			{
				int toIndex = kingIndex + offset;
				if (toIndex < 0 || toIndex >= 64) continue;
				
				Coord toCoord = new Coord(toIndex);
				
				if ((enemyPieces & (1UL << toIndex)) != 0)
				{
					moves.Add(new Move(fromCoord, toCoord, Move.MoveFlag.Capture));
					count++;
				}
				else if ((board.AllPieces & (1UL << toIndex)) == 0)
				{
					moves.Add(new Move(fromCoord, toCoord));
					count++;
				}
			}
			return count;
		}
}
