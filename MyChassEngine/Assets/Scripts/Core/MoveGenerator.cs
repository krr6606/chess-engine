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
		
		// MVV-LVA 이동 정렬을 위한 메소드 추가
		public void SortMovesByMVVLVA(List<Move> moves, ChessGameState state)
		{
			// MVV-LVA 점수 계산 함수
			int GetMoveScore(Move move)
			{
				// 기본 점수 - 캡처 이동 우선
				if (move.IsCapture)
				{
					int victimPiece = state.FindPieceBitboardIndex(move.ToSquare);
					int attackerPiece = state.FindPieceBitboardIndex(move.FromSquare);
					if (victimPiece < 0 || attackerPiece < 0)
						return 0;
					
					// MVV-LVA 점수: 희생자 가치 * 100 - 공격자 가치
					// victimType과 attackerType을 비트보드 인덱스에서 피스 타입으로 변환
					int victimType = victimPiece % 6; // 0=폰, 1=나이트, 2=비숍, 3=룩, 4=퀸, 5=킹
					int attackerType = attackerPiece % 6;
					
					// 피스 가치: 폰=1, 나이트=3, 비숍=3, 룩=5, 퀸=9, 킹=0(캡처 대상이 아님)
					int[] pieceValues = { 1, 3, 3, 5, 9, 0 };
					
					return pieceValues[victimType] * 100 - pieceValues[attackerType];
				}
				


            // 체크 이동 우선 (체크 확인은 비용이 많이 들어 여기서는 생략)
            if (move.HasFlag(Move.CheckFlag)) return 200;
            // 프로모션 우선
            if (move.IsPromotion) return 100;
            // 일반 이동
            return 0;
			}
			
			// 이동 정렬 - 일관된 비교를 위해 추가 기준 포함
			moves.Sort((a, b) => {
				// 먼저 MVV-LVA 점수 비교
				int scoreA = GetMoveScore(a);
				int scoreB = GetMoveScore(b);
				int scoreCompare = scoreB.CompareTo(scoreA);
				
				// 점수가 같으면 기물 타입으로 비교
				if (scoreCompare == 0)
				{
					// 출발 위치 비교
					int fromCompare = a.FromSquare.CompareTo(b.FromSquare);
					if (fromCompare != 0)
						return fromCompare;
					
					// 도착 위치 비교
					return a.ToSquare.CompareTo(b.ToSquare);
				}
				
				return scoreCompare;
			});
		}
		
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

		// 합법적인 이동만 생성하는 메서드 (체크 검사 포함)
		public List<Move> GenerateLegalMoves(ChessGameState board, bool includeQuietMoves = true)
		{
			// 먼저 모든 슈도 리걸 이동 생성
			List<Move> pseudoLegalMoves = GenerateMoves(board, includeQuietMoves);
			List<Move> legalMoves = new List<Move>();  

			// 현재 차례의 킹 위치
			bool isWhiteTurn = board.IsWhiteTurn;
			int kingSquare = isWhiteTurn ? board.WhiteKingSquare : board.BlackKingSquare;
			
			// 체크 상태인지 확인
			bool inCheck = board.IsInCheck(isWhiteTurn);
			int checkCount = BitHelper.CountBits(board.checkingPieces);
			
 
			
			// 더블 체크인 경우: 킹 이동만 가능
			if (checkCount > 1)
			{
				// 킹 이동만 필터링
				foreach (var move in pseudoLegalMoves)
				{
					// 움직이는 기물이 킹인지 확인
					int fromSquare = move.FromSquare;
					int pieceType = board.GetPieceAt(fromSquare) & pieceNum.pieceMask;
					
					if (pieceType == pieceNum.king)
					{
						// 이동 적용을 위한 게임 상태 복제
						ChessGameState tempState = board.Clone();
						
						// 이동 적용
						ApplyMove(tempState, move);
						
						// 이동 후 자신의 킹이 체크 상태가 아닌지 확인
						int newKingSquare = move.ToSquare; // 킹이 이동했으므로 새 위치
						
						if (!tempState.IsSquareAttacked(newKingSquare, !isWhiteTurn))
						{
							if (!includeQuietMoves)
							{
								if (move.IsCapture)
								{
									legalMoves.Add(move);
								}
							}
							else 
							{
								legalMoves.Add(move);
							}
						}
					}
				}
				
				return legalMoves;
			}
			// 싱글 체크인 경우: 1. 킹 이동, 2. 체크하는 기물 제거, 3. 체크를 막는 이동
			else if (inCheck && checkCount == 1)
			{
				foreach (var move in pseudoLegalMoves)
				{
					int fromSquare = move.FromSquare;
					int toSquare = move.ToSquare;
					int pieceType = board.GetPieceAt(fromSquare) & pieceNum.pieceMask;
					
					// 1. 킹 이동인 경우
					if (pieceType == pieceNum.king)
					{
						// 이동 적용을 위한 게임 상태 복제
						ChessGameState tempState = board.Clone();
						
						// 이동 적용
						ApplyMove(tempState, move);
						
						// 이동 후 자신의 킹이 체크 상태가 아닌지 확인
						int newKingSquare = move.ToSquare;
						
						if (!tempState.IsSquareAttacked(newKingSquare, !isWhiteTurn))
						{

                        if (!includeQuietMoves)
                        {
                            if (move.IsCapture)
                            {
                                legalMoves.Add(move);
                            }
                        }
                        else
                        {
                            legalMoves.Add(move);
                        }
                    }
					}
					// 2. 체크하는 기물을 제거하는 경우
					else if ((board.GetCheckingPieces() & (1UL << toSquare)) != 0)
					{
						// 이동 적용을 위한 게임 상태 복제
						ChessGameState tempState = board.Clone();
						
						// 이동 적용
						ApplyMove(tempState, move);
						
						// 이동 후 자신의 킹이 체크 상태가 아닌지 확인
						if (!tempState.IsSquareAttacked(kingSquare, !isWhiteTurn))
						{

                        if (!includeQuietMoves)
                        {
                            if (move.IsCapture)
                            {
                                legalMoves.Add(move);
                            }
                        }
                        else
                        {
                            legalMoves.Add(move);
                        }
                    }
					}
					// 3. 체크를 막는 이동 (블로킹)
					else if ((board.GetCheckBlockingMask() & (1UL << toSquare)) != 0)
					{
						// 이동 적용을 위한 게임 상태 복제
						ChessGameState tempState = board.Clone();
						
						// 이동 적용
						ApplyMove(tempState, move);
						
						// 이동 후 자신의 킹이 체크 상태가 아닌지 확인
						if (!tempState.IsSquareAttacked(kingSquare, !isWhiteTurn))
						{

                        if (!includeQuietMoves)
                        {
                            if (move.IsCapture)
                            {
                                legalMoves.Add(move);
                            }
                        }
                        else
                        {
                            legalMoves.Add(move);
                        }
                    }
					}
				}
				
				return legalMoves;
			}

			// 체크 상태가 아닌 경우: 모든 가능한 이동에 대해 체크 여부 검사
			foreach (var move in pseudoLegalMoves)
			{
				// 이동 적용을 위한 게임 상태 복제
				ChessGameState tempState = board.Clone();
				
				// 이동 적용
				ApplyMove(tempState, move);
				
				// 이동하는 기물이 킹인지 확인
				int fromSquare = move.FromSquare;
				int pieceType = board.GetPieceAt(fromSquare) & pieceNum.pieceMask;
				
				// 킹의 새 위치 결정 (킹이 이동한 경우 새 위치, 아니면 기존 위치)
				int newKingSquare = (pieceType == pieceNum.king) ? move.ToSquare : kingSquare;
				
				// 자신의 킹이 체크 상태가 아니면 합법적인 이동
				if (!tempState.IsSquareAttacked(newKingSquare, !isWhiteTurn))
				{

                if (!includeQuietMoves)
                {
                    if (move.IsCapture)
                    {
                        legalMoves.Add(move);
                    }
                }
                else
                {
                    legalMoves.Add(move);
                }
            }
			}

			return legalMoves;
		}
   
    // 이동 적용 도우미 메서드
    private void ApplyMove(ChessGameState state, Move move)
		{
			int fromSquare = move.FromSquare;
			int toSquare = move.ToSquare;
			
			// 기본 이동 적용
			state.MovePiece(fromSquare, toSquare);
			
			// 특수 이동 처리
			if (move.IsEnPassant)
			{
				// 앙파상인 경우 잡히는 폰 위치 계산
				int capturedPawnSquare = state.IsWhiteTurn ? 
					toSquare - 8 : toSquare + 8;
				
				// 잡히는 폰 제거
				state.ClearSquare(capturedPawnSquare);
			}
			else if (move.HasFlag(Move.KingSideCastleFlag))
			{
				// 킹사이드 캐슬링인 경우 룩 이동
				int rookFromSquare = state.IsWhiteTurn ? 7 : 63;
				int rookToSquare = state.IsWhiteTurn ? 5 : 61;
				state.MovePiece(rookFromSquare, rookToSquare);
			}
			else if (move.HasFlag(Move.QueenSideCastleFlag))
			{
				// 퀸사이드 캐슬링인 경우 룩 이동
				int rookFromSquare = state.IsWhiteTurn ? 0 : 56;
				int rookToSquare = state.IsWhiteTurn ? 3 : 59;
				state.MovePiece(rookFromSquare, rookToSquare);
			}
			else if (move.IsPromotion)
			{
				// 프로모션인 경우 폰을 해당 기물로 교체
				int promotionPiece = 0;
				
				// 프로모션 기물 타입 결정
				if (move.HasFlag(Move.PromoteToQueenFlag))
					promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.queen : pieceNum.black | pieceNum.queen;
				else if (move.HasFlag(Move.PromoteToRookFlag))
					promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.rook : pieceNum.black | pieceNum.rook;
				else if (move.HasFlag(Move.PromoteToBishopFlag))
					promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.bishop : pieceNum.black | pieceNum.bishop;
				else if (move.HasFlag(Move.PromoteToKnightFlag))
					promotionPiece = state.IsWhiteTurn ? pieceNum.white | pieceNum.knight : pieceNum.black | pieceNum.knight;
				

				
				// 프로모션 적용
				if (promotionPiece != 0) {
					state.ClearSquare(toSquare); // 폰 제거
					state.PlacePiece(toSquare, promotionPiece);
				}
			}
			
			// 차례 변경
			state.SwitchTurn();
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
				pawnBitboard = DeleteBit(pawnBitboard, pawnIndex);

				// 일반 전진 이동
				ulong pawnMovesBitboard = (isWhite ? ChessCache.WhitePawnMoves[pawnIndex] : ChessCache.BlackPawnMoves[pawnIndex]) & ~friendlyPieces;
				if (pawnMovesBitboard != 0)
				{
					int toIndex = BitHelper.BitScanForward(pawnMovesBitboard);
					if(( allPieces & BitHelper.SetBit(toIndex)) != 0)
					{
						isBlocked = true;
					}
					else if (toIndex != -1)
					{
						Coord toCoord = new Coord(toIndex);
						
						// 프로모션 처리
						if ((isWhite && toCoord.rankIndex == 7) || (!isWhite && toCoord.rankIndex == 0))
						{
							// 프로모션 플래그에 따라 다양한 프로모션 추가
							if (promotionsToGenerate == PromotionMode.All)
							{
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToQueenFlag));
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToRookFlag));
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToBishopFlag));
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToKnightFlag));
								count += 4;
							}
							else if (promotionsToGenerate == PromotionMode.QueenOnly)
							{
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToQueenFlag));
								count++;
							}
							else if (promotionsToGenerate == PromotionMode.QueenAndKnight)
							{
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToQueenFlag));
								moves.Add(new Move(fromCoord, toCoord, Move.PromoteToKnightFlag));
								count += 2;
							}
						}
						else
						{
							// 일반 이동
							moves.Add(new Move(fromCoord, toCoord, Move.NoFlag));
							pawnMovesBitboard = DeleteBit(pawnMovesBitboard, toIndex);
							count++;
						}
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
					
					// 프로모션 캡처 처리
					if ((isWhite && toCoord.rankIndex == 7) || (!isWhite && toCoord.rankIndex == 0))
					{
						if (promotionsToGenerate == PromotionMode.All)
						{
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToQueenFlag));
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToRookFlag));
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToBishopFlag));
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToKnightFlag));
							count += 4;
						}
						else if (promotionsToGenerate == PromotionMode.QueenOnly)
						{
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToQueenFlag));
							count++;
						}
						else if (promotionsToGenerate == PromotionMode.QueenAndKnight)
						{
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToQueenFlag));
							moves.Add(new Move(fromCoord, toCoord, Move.PromoteToKnightFlag));
							count += 2;
						}
					}
					else
					{
						moves.Add(new Move(fromCoord, toCoord, Move.CaptureFlag));
						count++;
					}
					
					captureTargets = DeleteBit(captureTargets, toIndex);
				}

				// 더블 폰 이동
				if(isBlocked) {isBlocked = false; continue;};
				if ((currentPawn & (isWhite ? ChessCache.WhitePawnStartRank : ChessCache.BlackPawnStartRank)) != 0)
				{
					// 먼저 한 칸 앞이 비어 있는지 확인
					int oneStepIndex = isWhite ? pawnIndex + 8 : pawnIndex - 8;
					if (oneStepIndex >= 0 && oneStepIndex < 64 && (allPieces & (1UL << oneStepIndex)) == 0)
					{
						ulong doublePawnMoves = (isWhite ? ChessCache.WhitePawnDoubleMoves[pawnIndex] : ChessCache.BlackPawnDoubleMoves[pawnIndex]) & ~friendlyPieces;
						if (doublePawnMoves != 0)
						{
							int toIndex = BitHelper.BitScanForward(doublePawnMoves);
							if(( allPieces & BitHelper.SetBit(toIndex)) == 0  && toIndex != -1)
							{
								Coord toCoord = new Coord(toIndex);
								moves.Add(new Move(fromCoord, toCoord, Move.PawnTwoUpFlag));
								doublePawnMoves = DeleteBit(doublePawnMoves, toIndex);
								count++;
							}
						}
					}
				}

				//  앙파상 처리
				if (board.EnPassantTargetSquare != -1)
				{
					// 현재 폰의 위치에서 앙파상 캡처가 가능한지 확인
					ulong epSquareBit = BitHelper.SetBit(board.EnPassantTargetSquare);
					
					// 앙파상 캡처가 가능하려면 대각선으로 이동 가능해야 함
					bool canCaptureEP = (isWhite ? 
						(ChessCache.WhitePawnCaptureMasks[pawnIndex] & epSquareBit) != 0 :
						(ChessCache.BlackPawnCaptureMasks[pawnIndex] & epSquareBit) != 0);
					
					if (canCaptureEP)
					{
						Coord toCoord = new Coord(board.EnPassantTargetSquare);
						moves.Add(new Move(fromCoord, toCoord, Move.EnPassantCaptureFlag));
						count++;
					}
				}
			}
			return count;
		}

    private static ulong DeleteBit(ulong pawnMovesBitboard, int toIndex)
    {
        pawnMovesBitboard &= ~BitHelper.SetBit(toIndex);
        return pawnMovesBitboard;
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
				knightBitboard = DeleteBit(knightBitboard, knightIndex);
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
					moves.Add(new Move(fromCoord, toCoord, Move.NoFlag));
					emptySquares = DeleteBit(emptySquares, toIndex);
					count++;
				}

				// 캡처 이동
				while (captureSquares != 0)
				{
					int toIndex = BitHelper.BitScanForward(captureSquares);
					if (toIndex == -1) break;
					Coord toCoord = new Coord(toIndex);
					moves.Add(new Move(fromCoord, toCoord, Move.CaptureFlag));
					captureSquares = DeleteBit(captureSquares, toIndex);
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
								moves.Add(new Move(fromCoord, toCoord, Move.CaptureFlag));
								count++;
							}
							// 이 방향의 이동 종료 (아군이든 적군이든 더 이상 진행 불가)
							break;
						}
						else
						{
							// 빈 칸이면 이동 가능
							Coord toCoord = new Coord(targetSquare);
							moves.Add(new Move(fromCoord, toCoord, Move.NoFlag));
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
								moves.Add(new Move(fromCoord, toCoord, Move.CaptureFlag));
								count++;
							}
							// 이 방향의 이동 종료 (아군이든 적군이든 더 이상 진행 불가)
							break;
						}
						else
						{
							// 빈 칸이면 이동 가능
							Coord toCoord = new Coord(targetSquare);
							moves.Add(new Move(fromCoord, toCoord, Move.NoFlag));
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
								moves.Add(new Move(fromCoord, toCoord, Move.CaptureFlag));
								count++;
							}
							// 이 방향의 이동 종료 (아군이든 적군이든 더 이상 진행 불가)
							break;
						}
						else
						{
							// 빈 칸이면 이동 가능
							Coord toCoord = new Coord(targetSquare);
							moves.Add(new Move(fromCoord, toCoord, Move.NoFlag));
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
					moves.Add(new Move(fromCoord, toCoord, Move.CaptureFlag));
					count++;
				}
				else if ((board.AllPieces & (1UL << toIndex)) == 0)
				{
					moves.Add(new Move(fromCoord, toCoord, Move.NoFlag));
					count++;
				}
			}
			
			// 캐슬링 이동 생성
			GenerateCastlingMoves(board, moves, isWhite, ref count);
			
			return count;
		}
		
		// 캐슬링 이동 생성
		private void GenerateCastlingMoves(ChessGameState board, List<Move> moves, bool isWhite, ref int count)
		{
			// 체크 상태에서는 캐슬링 불가
			if (board.IsInCheck(isWhite))
			{
				return;
			}

			
			// 킹의 위치
			int kingSquare = isWhite ? board.WhiteKingSquare : board.BlackKingSquare;
			
			// 킹 사이드 캐슬링
			if (CanKingSideCastle(board, isWhite))
			{
				// 킹이 지나는 경로
				int pathSquare1 = isWhite ? 5 : 61; // f1 또는 f8
				int pathSquare2 = isWhite ? 6 : 62; // g1 또는 g8
				ulong enemyAttacks = isWhite ? board.blackAttackMap : board.whiteAttackMap;
				
				// 킹이 지나는 길에 적의 공격이 없는지 확인
				bool pathClear = ((enemyAttacks & (1UL << pathSquare1)) == 0) && 
								 ((enemyAttacks & (1UL << pathSquare2)) == 0);
				
				if (pathClear)
				{
					Coord fromCoord = new Coord(kingSquare);
					Coord toCoord = new Coord(pathSquare2);
					moves.Add(new Move(fromCoord, toCoord, Move.KingSideCastleFlag));
					count++;
				}
			}
			
			// 퀸 사이드 캐슬링
			if (CanQueenSideCastle(board, isWhite))
			{
				// 킹이 지나는 경로
				int pathSquare1 = isWhite ? 3 : 59; // d1 또는 d8
				int pathSquare2 = isWhite ? 2 : 58; // c1 또는 c8
				ulong enemyAttacks = isWhite ? board.blackAttackMap : board.whiteAttackMap;
				
				// 킹이 지나는 길에 적의 공격이 없는지 확인
				bool pathClear = ((enemyAttacks & (1UL << pathSquare1)) == 0) && 
								 ((enemyAttacks & (1UL << pathSquare2)) == 0);
				
				if (pathClear)
				{
					Coord fromCoord = new Coord(kingSquare);
					Coord toCoord = new Coord(pathSquare2);
					moves.Add(new Move(fromCoord, toCoord, Move.QueenSideCastleFlag));
					count++;
				}
			}
		}
		
		// 킹 사이드 캐슬링 가능 여부
		private bool CanKingSideCastle(ChessGameState board, bool isWhite)
		{
			// 캐슬링 권한 확인
			if (isWhite && !board.WhiteKingSideCastleRight) return false;
			if (!isWhite && !board.BlackKingSideCastleRight) return false;
			
			// 경로 상의 기물 확인
			ulong castlePath = isWhite ? ChessCache.WhiteKingSideCastlePath : ChessCache.BlackKingSideCastlePath;
			
			// 경로가 비어있어야 함 (킹과 룩 위치 제외)
			ulong pathOccupancy = board.AllPieces & castlePath;
			return pathOccupancy == 0;
		}
		
		// 퀸 사이드 캐슬링 가능 여부
		private bool CanQueenSideCastle(ChessGameState board, bool isWhite)
		{
			// 캐슬링 권한 확인
			if (isWhite && !board.WhiteQueenSideCastleRight) return false;
			if (!isWhite && !board.BlackQueenSideCastleRight) return false;
			
			// 경로 상의 기물 확인
			ulong castlePath = isWhite ? ChessCache.WhiteQueenSideCastlePath : ChessCache.BlackQueenSideCastlePath;
			
			// 경로가 비어있어야 함 (킹과 룩 위치 제외)
			ulong pathOccupancy = board.AllPieces & castlePath;
			return pathOccupancy == 0;
		}

}
