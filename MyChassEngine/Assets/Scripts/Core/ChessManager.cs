using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChessManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private ChessBoardVisualizer boardVisualizer;
    
    [Header("설정")]
    [SerializeField] private string startingFEN = FENParser.StartPositionFEN;
    [SerializeField] private bool autoStartGame = true;
    [SerializeField] private bool debugMode = false;
    
    // 게임 상태
    [SerializeField] 
    private ChessGameState currentState;
    
    List<Move> AllMoves = new List<Move>();
    MoveGenerator moveGenerator;
    // 이벤트
    [Serializable] public class BoardUpdatedEvent : UnityEvent<int[]> { }
    [Serializable] public class GameStateChangedEvent : UnityEvent<ChessGameState> { }
    
    public BoardUpdatedEvent OnBoardUpdated = new BoardUpdatedEvent();
    public GameStateChangedEvent OnGameStateChanged = new GameStateChangedEvent();
    
    [Header("디버그")]
    [SerializeField] private int debugSquare = 0;
    
    private void Awake()
    {
        // 컴포넌트 참조 확인
        if (boardVisualizer == null)
        {
            boardVisualizer = FindObjectOfType<ChessBoardVisualizer>();
            if (boardVisualizer == null)
            {
                Debug.LogError("ChessBoardVisualizer를 찾을 수 없습니다. 체스 보드 시각화가 작동하지 않습니다.");
            }
        }
        moveGenerator = new MoveGenerator();
        
        // 게임 상태 초기화
        currentState = new ChessGameState();
    }
    
    private void Start()
    {
        if (autoStartGame)
        {
            InitializeGame();
            AllMoves = moveGenerator.GenerateMoves(currentState);
            Debug.Log(AllMoves.Count);
        }
    }
    readonly Coord NullChoiceSquare = new Coord(-1, -1);
    Coord CurrentSquare = new Coord(-1, -1);
    Coord OldSquare = new Coord(-1, -1);

    String choiceMove = null;
    bool isMoving = false;
    Coord[] oldMoves = new Coord[64];
    Coord OldChoiceSquare = new Coord(-1, -1);
    public void Update()
    { 
        if (currentState.CurrentGameState == ChessGameState.GameState.Playing && Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            if (boardVisualizer.IsSquare(mousePosition))
            {
                Coord choiceSquare = boardVisualizer.PositionToSquare(mousePosition);

                if(currentState.Board[choiceSquare.SquareIndex] == 0 && CurrentSquare.CompareTo(NullChoiceSquare) == 0)
                {
                    return;
                }
                else if (currentState.IsWhitePieceAt(CurrentSquare.SquareIndex) == currentState.IsWhiteTurn && CurrentSquare.CompareTo(NullChoiceSquare) == 1 && CurrentSquare.CompareTo(choiceSquare) == 1 && CurrentSquare.IsValidSquare())
                {
                    choiceMove = new Move(CurrentSquare.SquareIndex, choiceSquare.SquareIndex).ToString();
                    foreach(var move in AllMoves){
                        if(move.ToString() == choiceMove){
                            isMoving = true;
                        }
                    }

                    if(isMoving){
                        MovePiece(CurrentSquare.SquareIndex, choiceSquare.SquareIndex);

                    UnhighlightAllMoves();
                    boardVisualizer.HighlightSquare(choiceSquare);
                     AllMoves = moveGenerator.GenerateMoves(currentState);
                    Debug.Log(AllMoves.Count);
                    OldSquare = CurrentSquare;
                    OldChoiceSquare = choiceSquare;
                    CurrentSquare = NullChoiceSquare;

                    isMoving = false;
                    Debug.Log("이동 완료");
                    }
                    else{

                    boardVisualizer.UnhighlightSquare(CurrentSquare);

                    CurrentSquare = NullChoiceSquare;
                    boardVisualizer.UnhighlightSquare(OldSquare);
                    UnhighlightAllMoves();

                        Debug.Log("이동 불가능");
                    }
                }
                else if(CurrentSquare.CompareTo(NullChoiceSquare) == 0)
                {
                    CurrentSquare = choiceSquare;
                    if(OldSquare.CompareTo(NullChoiceSquare) == 1)
                    {
                        boardVisualizer.UnhighlightSquare(OldSquare);
                    }
                    if(OldChoiceSquare.CompareTo(NullChoiceSquare) == 1)
                    {
                        boardVisualizer.UnhighlightSquare(OldChoiceSquare);
                    }
                    boardVisualizer.HighlightSquare(choiceSquare);
                    foreach(var move in AllMoves){
                        if(move.FromSquare == CurrentSquare.SquareIndex){
                            Coord moveTo = new Coord(move.ToSquare);
                            boardVisualizer.HighlightSquare(moveTo, true);
                            oldMoves[moveTo.SquareIndex] = moveTo;
                        }
                    }

                    Debug.Log("선택된 위치: " + choiceSquare.fileIndex + " " + choiceSquare.rankIndex);
                }
                else{
                    Debug.Log("이동 불가능");
                    boardVisualizer.UnhighlightSquare(CurrentSquare);

                    CurrentSquare = NullChoiceSquare;
                    boardVisualizer.UnhighlightSquare(OldSquare);
                    UnhighlightAllMoves();

                }
            }
            else
            {
                CurrentSquare = NullChoiceSquare;
            }
        }

        // 디버그 단축키
        if (Input.GetKeyDown(KeyCode.F1))
        {
            DebugPrintAllCaches();
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            DebugPrintCacheForSquare();
        }
    }
    void UnhighlightAllMoves(){
        if(oldMoves != null){
        foreach(var coord in oldMoves){
            if(coord.CompareTo(NullChoiceSquare) == 1){
                boardVisualizer.UnhighlightSquare(coord);
                }
            }
            oldMoves = new Coord[64];
        }
    }

    // 게임 초기화 (기본 FEN 사용)
    public void InitializeGame()
    {
        InitializeGameFromFEN(startingFEN);
    }
    
    // 지정한 FEN으로 게임 초기화
    public void InitializeGameFromFEN(string fen)
    {
        try
        {
            // FEN 문자열로 게임 상태 초기화
            currentState.LoadFromFEN(fen);
            
            // 시각적 표현 업데이트
            UpdateBoard();
            
            // 이벤트 발생
            OnGameStateChanged.Invoke(currentState);
            Debug.Log($"FEN 문자열로 게임이 초기화되었습니다: {fen}");
            
            if (debugMode)
            {
                DebugPrintFEN();
                DebugPrintBoard();
                DebugPrintBitboards();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"FEN 문자열 처리 중 오류 발생: {e.Message}");
        }
    }
    
    // 시각적 보드 업데이트
    private void UpdateBoard()
    {
        if (boardVisualizer != null)
        {
            // 보드 시각화
            boardVisualizer.VisualizeBoard(currentState.Board);
            
            // 이벤트 발생
            OnBoardUpdated.Invoke(currentState.Board);
        }
        else
        {
            Debug.LogWarning("ChessBoardVisualizer가 없습니다. 시각적 업데이트가 수행되지 않습니다.");
        }
    }
    
    // 현재 게임 상태 가져오기
    public ChessGameState GetCurrentState()
    {
        return currentState.Clone();
    }
    
    // FEN 문자열 설정
    public void SetFEN(string fen)
    {
        if (string.IsNullOrEmpty(fen))
        {
            Debug.LogError("FEN 문자열이 비어있습니다.");
            return;
        }
        
        InitializeGameFromFEN(fen);
    }
    
    // 현재 FEN 문자열 가져오기
    public string GetCurrentFEN()
    {
        return currentState.GetFEN();
    }
    
    // 특정 위치에 기물 배치 (테스트/에디터 용)
    public void PlacePiece(int square, int pieceValue)
    {
        if (square < 0 || square >= 64) return;
        
        currentState.PlacePiece(square, pieceValue);
        UpdateBoard();
        OnGameStateChanged.Invoke(currentState);
    }
    
    // 기물 이동
    public void MovePiece(int fromSquare, int toSquare)
    {
        if (fromSquare < 0 || fromSquare >= 64 || toSquare < 0 || toSquare >= 64) return;
        
        int piece = currentState.GetPieceAt(fromSquare);
        if (piece == 0) return; // 출발 위치에 기물이 없음
        
        // 현재 차례인지 확인
        bool isPieceWhite = pieceNum.IsWhite(piece);
        if (isPieceWhite != currentState.IsWhiteTurn)
        {
            Debug.LogWarning("자신의 차례가 아닙니다.");
            return;
        }
        
        // 이동 실행
        currentState.MovePiece(fromSquare, toSquare);
        
        // 차례 변경
        currentState.IsWhiteTurn = !currentState.IsWhiteTurn;
        
        // 시각적 표현 업데이트
        UpdateBoard();
        OnGameStateChanged.Invoke(currentState);
    }
    
    // 디버그용: 현재 보드 상태 콘솔에 출력
    [ContextMenu("Debug: Print Board")]
    public void DebugPrintBoard()
    {
        if (boardVisualizer != null)
        {
            boardVisualizer.DebugVisualize(currentState.Board);
        }
        else
        {
            Debug.LogWarning("ChessBoardVisualizer가 없습니다.");
        }
    }
    
    // 디버그용: 현재 FEN 콘솔에 출력
    [ContextMenu("Debug: Print FEN")]
    public void DebugPrintFEN()
    {
        Debug.Log("현재 FEN: " + GetCurrentFEN());
    }
    
    // 디버그용: 현재 비트보드 콘솔에 출력
    [ContextMenu("Debug: Print Bitboards")]
    public void DebugPrintBitboards()
    {
        Debug.Log("===== 비트보드 상태 =====");
        
        string[] pieceNames = {
            "백색 폰", "백색 나이트", "백색 비숍", "백색 룩", "백색 퀸", "백색 킹",
            "흑색 폰", "흑색 나이트", "흑색 비숍", "흑색 룩", "흑색 퀸", "흑색 킹"
        };
        
        for (int i = 0; i < 12; i++)
        {
            Debug.Log($"{pieceNames[i]}:\n{BitHelper.BitboardToString(currentState.BitBoards[i])}");
        }
        
        Debug.Log($"백색 전체:\n{BitHelper.BitboardToString(currentState.WhitePieces)}");
        Debug.Log($"흑색 전체:\n{BitHelper.BitboardToString(currentState.BlackPieces)}");
        Debug.Log($"모든 기물:\n{BitHelper.BitboardToString(currentState.AllPieces)}");
    }

    [ContextMenu("Debug: Print All Caches")]
    public void DebugPrintAllCaches()
    {
        if (!debugMode) return;
        ChessCache.DebugPrintAllCaches();
    }

    [ContextMenu("Debug: Print Cache For Square")]
    public void DebugPrintCacheForSquare()
    {
        if (!debugMode) return;
        ChessCache.DebugPrintCacheForSquare(debugSquare);
    }
} 