using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class HumanPlayer : BasePlayer
{
    private ChessBoardVisualizer boardVisualizer;
    private Coord selectedSquare = new Coord(-1, -1);
    private Coord targetSquare = new Coord(-1, -1);
    private Coord nullSquare = new Coord(-1, -1);
    private Coord oldSelectedSquare = new Coord(-1, -1);
    private Coord oldTargetSquare = new Coord(-1, -1);
    private Coord[] highlightedMoves = new Coord[64];

    [Header("프로모션 UI")]
    [SerializeField] private GameObject promotionPanel;
    [SerializeField] private Button queenButton;
    [SerializeField] private Button rookButton;
    [SerializeField] private Button bishopButton;
    [SerializeField] private Button knightButton;
    
    private bool isPromotionPending = false;
    private Move pendingPromotionMove;

    public override void Initialize(BoardManager manager)
    {
        base.Initialize(manager);
        boardVisualizer = manager.GetBoardVisualizer();
        playerName = "인간 플레이어";
        
        // 프로모션 버튼 이벤트 설정
        if (promotionPanel != null)
        {
            promotionPanel.SetActive(false);
            
            if (queenButton != null)
                queenButton.onClick.AddListener(() => CompletePromotion(Move.PromoteToQueenFlag));
                
            if (rookButton != null)
                rookButton.onClick.AddListener(() => CompletePromotion(Move.PromoteToRookFlag));
                
            if (bishopButton != null)
                bishopButton.onClick.AddListener(() => CompletePromotion(Move.PromoteToBishopFlag));
                
            if (knightButton != null)
                knightButton.onClick.AddListener(() => CompletePromotion(Move.PromoteToKnightFlag));
        }
    }

    public override void Update()
    {
        base.Update();

        // 내 턴이 아니거나 움직임 계산 중이면 입력 처리 안함
        if (chessManager.IsProcessingMoves)
        {
            // 다른 플레이어의 턴일 때는 선택 내용도 초기화
            if (selectedSquare.CompareTo(nullSquare) != 0)
            {
                ClearSelection();
            }
            return;
        }
        
        // AI가 생각 중인 경우 사용자 입력 무시
        if (chessManager.IsAIThinking) return;
        
        // 프로모션 대기 중이면 다른 입력 처리 안 함
        if (isPromotionPending) return;

        // 디버그 모드 처리
        HandleDebugMode();

        // 게임이 종료된 경우 처리하지 않음
        if (chessManager.IsGameOver()) return;

        // 마우스 입력 처리
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseInput();
        }
        
        // ESC 키로 선택 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearSelection();
        }
    }

    private void HandleMouseInput()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        if (boardVisualizer.IsSquare(mousePosition))
        {
            Coord clickedSquare = boardVisualizer.PositionToSquare(mousePosition);
            
            // 빈 칸 선택시 처리
            if (chessManager.GetCurrentState().Board[clickedSquare.SquareIndex] == 0 && 
                selectedSquare.CompareTo(nullSquare) == 0)
            {
                return;
            }
            
            // 선택된 기물이 있고, 대상 칸을 선택한 경우
            else if (chessManager.GetCurrentState().IsWhitePieceAt(selectedSquare.SquareIndex) == chessManager.IsWhiteTurn() && 
                     selectedSquare.CompareTo(nullSquare) == 1 && 
                     selectedSquare.CompareTo(clickedSquare) == 1 && 
                     selectedSquare.IsValidSquare())
            {
                TryExecuteMove(clickedSquare);
            }
            
            // 기물 선택
            else if (selectedSquare.CompareTo(nullSquare) == 0)
            {
                SelectPiece(clickedSquare);
            }
            
            // 잘못된 선택
            else
            {
                ClearSelection();
                Debug.Log("이동 불가능");
            }
        }
        else
        {
            selectedSquare = nullSquare;
        }
    }

    private void TryExecuteMove(Coord targetSquare)
    {
        // 이동 가능한지 확인
        bool moveFound = false;
        Move selectedMove = new Move(0, 0);  // 기본값으로 초기화
        
        foreach (var move in chessManager.GetAllMoves())
        {
            if (move.FromSquare == selectedSquare.SquareIndex && move.ToSquare == targetSquare.SquareIndex)
            {
                selectedMove = move;
                moveFound = true;
                break;
            }
        }

        if (moveFound)
        {
            // 프로모션 필요한지 확인
            if (IsPromotionMove(selectedMove))
            {
                ShowPromotionUI(selectedMove);
            }
            else
            {
                // 일반 이동 실행
                ExecuteMoveAndUpdateUI(selectedMove);
            }
        }
        else
        {
            chessManager.UnhighlightSquare(selectedSquare);
            selectedSquare = nullSquare;
            chessManager.UnhighlightSquare(oldSelectedSquare);
            UnhighlightAllMoves();
            Debug.Log("이동 불가능");
        }
    }

    private void SelectPiece(Coord square)
    {
        selectedSquare = square;
        if (oldSelectedSquare.CompareTo(nullSquare) == 1)
        {
            chessManager.UnhighlightSquare(oldSelectedSquare);
        }
        if (oldTargetSquare.CompareTo(nullSquare) == 1)
        {
            chessManager.UnhighlightSquare(oldTargetSquare);
        }
        chessManager.HighlightSquare(square);
        
        // 선택한 기물의 가능한 이동 표시
        var moves = chessManager.GetAllMoves();
        foreach (var move in moves)
        {
            if (move.FromSquare == selectedSquare.SquareIndex)
            {
                Coord moveTo = new Coord(move.ToSquare);
                chessManager.HighlightSquare(moveTo, true);
                highlightedMoves[moveTo.SquareIndex] = moveTo;
            }
        }

        Debug.Log("선택된 위치: " + square.fileIndex + " " + square.rankIndex);
    }

    private void UnhighlightAllMoves()
    {
        if (highlightedMoves != null)
        {
            foreach (var coord in highlightedMoves)
            {
                if (coord.CompareTo(nullSquare) == 1)
                {
                    chessManager.UnhighlightSquare(coord);
                }
            }
            highlightedMoves = new Coord[64];
        }
    }

    private void ClearSelection()
    {
        chessManager.UnhighlightSquare(selectedSquare);
        selectedSquare = nullSquare;
        chessManager.UnhighlightSquare(oldSelectedSquare);
        UnhighlightAllMoves();
    }

    private void HandleDebugMode()
    {
        // F1-F2: 기존 디버그 키
        if (Input.GetKeyDown(KeyCode.F1))
        {
            // 디버그 함수 호출
            Debug.Log("F1 디버그 키 입력");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("F2 디버그 키 입력");
        }
    }

    public override bool IsHumanPlayer => true;

    public override void OnTurnStarted()
    {
        base.OnTurnStarted();
        Debug.Log($"{playerName}의 턴 시작");
        
        // 턴이 시작되면 선택 상태 초기화
        ClearSelection();
    }

    public override void OnMoveExecuted(Move move)
    {
        base.OnMoveExecuted(move);
        // AI 턴 종료를 ChessManager에 알림
        chessManager.NotifyAITurnEnded();
        Debug.Log($"{playerName}의 이동 완료");
    }

    public override void OnGameEnded()
    {
        base.OnGameEnded();
        
        // 게임이 종료되면 선택 상태 초기화
        ClearSelection();
    }

    // 프로모션 필요한 이동인지 확인
    private bool IsPromotionMove(Move move)
    {
        // 플레이어가 제어하는 기물 색상 확인
        bool isWhite = chessManager.IsWhiteTurn();
        int fromSquare = move.FromSquare;
        int toSquare = move.ToSquare;
        
        // 보드에서 말 정보 가져오기
        int piece = chessManager.GetCurrentState().Board[fromSquare];
        
        // 폰인지 확인
        bool isPawn = (piece & pieceNum.pieceMask) == pieceNum.pwan;
        
        if (!isPawn) return false;
        
        // 마지막 랭크에 도달하는지 확인
        int rank = toSquare / 8;
        return (isWhite && rank == 7) || (!isWhite && rank == 0);
    }
    
    // 프로모션 UI 표시
    private void ShowPromotionUI(Move baseMove)
    {
        if (promotionPanel == null)
        {
            // UI가 없으면 기본값(퀸)으로 프로모션
            ExecuteMoveAndUpdateUI(new Move(baseMove.FromSquare, baseMove.ToSquare, Move.PromoteToQueenFlag));
            return;
        }
        
        // 프로모션 UI 표시
        isPromotionPending = true;
        pendingPromotionMove = baseMove;
        promotionPanel.SetActive(true);
    }
    
    // 프로모션 종류 선택 완료
    private void CompletePromotion(int promotionFlag)
    {
        if (!isPromotionPending) return;
        
        // 프로모션 UI 숨기기
        promotionPanel.SetActive(false);
        
        // 프로모션 플래그가 포함된 새 이동 객체 생성
        Move promotionMove = new Move(
            pendingPromotionMove.FromSquare, 
            pendingPromotionMove.ToSquare, 
            promotionFlag
        );
        
        // 이동 실행
        ExecuteMoveAndUpdateUI(promotionMove);
        
        isPromotionPending = false;
        pendingPromotionMove = new Move(0, 0);
    }
    
    // 이동 실행 및 UI 업데이트 (공통 로직)
    private void ExecuteMoveAndUpdateUI(Move move)
    {

        // 하이라이트 처리
        UnhighlightAllMoves();
        Debug.Log("이동 요청 전송 전");
        // 실제 이동 비동기 실행
        chessManager.QueueMove(move);
        Debug.Log("이동 요청 전송됨");


        chessManager.HighlightSquare(new Coord(move.ToSquare));
        
        oldSelectedSquare = selectedSquare;
        oldTargetSquare = new Coord(move.ToSquare);
        selectedSquare = nullSquare;
    }
} 