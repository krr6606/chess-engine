using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;


public class BoardManager : MonoBehaviour
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
    
    private List<Move> AllMoves = new List<Move>();
    private MoveGenerator moveGenerator;
    // 체스 상태 매니저 추가
    private ChessStateCalculator stateManager;
    
    // 이벤트
    [Serializable] public class BoardUpdatedEvent : UnityEvent<int[]> { }
    [Serializable] public class GameStateChangedEvent : UnityEvent<ChessGameState> { }
    
    public BoardUpdatedEvent OnBoardUpdated = new BoardUpdatedEvent();
    public GameStateChangedEvent OnGameStateChanged = new GameStateChangedEvent();
    private bool isExecutingMove = false;
    public bool IsExecutingMove => isExecutingMove;

    [Header("디버그")]
    [SerializeField] private int debugSquare = 0;
    
    // 게임 모드 선택을 위한 enum 추가
    public enum GameMode
    {
        HumanVsHuman,           // 사람 대 사람
        HumanVsRandomAI,        // 사람 대 랜덤 AI
        HumanVsMinimaxAI,       // 사람 대 미니맥스 AI
        RandomAIVsHuman,        // 랜덤 AI 대 사람
        MinimaxAIVsHuman,       // 미니맥스 AI 대 사람
        RandomAIVsRandomAI,     // 랜덤 AI 대 랜덤 AI
        MinimaxAIVsRandomAI,    // 미니맥스 AI 대 랜덤 AI
        RandomAIVsMinimaxAI,    // 랜덤 AI 대 미니맥스 AI
        MinimaxAIVsMinimaxAI    // 미니맥스 AI 대 미니맥스 AI
    }

    [Header("게임 모드")]
    [SerializeField] public GameMode gameMode = GameMode.HumanVsHuman;

    [Header("플레이어 설정")]
    [SerializeField] private IPlayer whitePlayer;
    [SerializeField] private IPlayer blackPlayer;
    
    // 이전 상태를 저장하기 위한 변수들
    private Stack<ChessGameState> stateHistory = new Stack<ChessGameState>();
    private Stack<Move> moveHistory = new Stack<Move>();

    // AI 턴 관리를 위한 변수
    private bool isAIThinking = false;
    public bool IsAIThinking => isAIThinking;

    private Queue<Move> moveQueue = new Queue<Move>();
    private bool isProcessingMoves = false;
    public bool IsProcessingMoves => isProcessingMoves;

    [Header("디버깅 도구")]
    [SerializeField] private bool enableBitboardVisualization = false;
    [SerializeField] private ChessBoardVisualizer.BitboardType currentBitboardType = ChessBoardVisualizer.BitboardType.None;

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
        // 체스 상태 매니저 초기화
        stateManager = new ChessStateCalculator();
        
        // 게임 상태 초기화
        currentState = new ChessGameState();
        
        // 게임 모드에 따라 플레이어 설정
        SetupPlayers();
    }
    
    private void Start()
    {
        if (autoStartGame)
        {
            InitializeGame();
            
            // 첫 번째 플레이어(백색) 턴 시작
            whitePlayer?.OnTurnStarted();
        }
    }
    readonly Coord NullChoiceSquare = new Coord(-1, -1);
    Coord CurrentSquare = new Coord(-1, -1);
    Coord OldSquare = new Coord(-1, -1);
    Coord[] oldMoves = new Coord[64];
    Coord OldChoiceSquare = new Coord(-1, -1);


    public List<Move> GetAllMoves()
    {
        return AllMoves;
    }

    // 기물 선택 상태 변경
    public bool SelectSquare(Coord square)
    {
        if (currentState.CurrentGameState == ChessGameState.GameState.Checkmate || 
            currentState.CurrentGameState == ChessGameState.GameState.Stalemate || 
            currentState.CurrentGameState == ChessGameState.GameState.DrawByFiftyMoveRule)
            return false;
        
        if (square.IsValidSquare() && currentState.IsSquareOccupied(square.SquareIndex))
        {
            bool isPieceWhite = currentState.IsWhitePieceAt(square.SquareIndex);
            if (isPieceWhite == currentState.IsWhiteTurn)
            {
                return true;
            }
        }
        return false;
    }

    // 하이라이트 함수들
    public void HighlightSquare(Coord square, bool isMove = false)
    {
        if (boardVisualizer != null)
        {
            boardVisualizer.HighlightSquare(square, isMove);
        }
    }

    public void UnhighlightSquare(Coord square)
    {
        if (boardVisualizer != null)
        {
            boardVisualizer.UnhighlightSquare(square);
        }
    }

    // 게임 상태 확인
    public bool IsGameOver()
    {
        return stateManager.IsGameOver(currentState);
    }

    // 현재 차례 정보
    public bool IsWhiteTurn()
    {
        return currentState.IsWhiteTurn;
    }

    // 비주얼라이저 참조 제공
    public ChessBoardVisualizer GetBoardVisualizer()
    {
        return boardVisualizer;
    }

    // 게임 초기화 (기본 FEN 사용)
    public void InitializeGame()
    {
        // 히스토리 초기화
        stateHistory.Clear();
        moveHistory.Clear();
        
        InitializeGameFromFEN(startingFEN);
        // 초기 이동 리스트 생성
        GenerateMovesForCurrentState();
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

    // 현재 게임 상태에 대한 이동 생성
    private void GenerateMovesForCurrentState()
    {
        AllMoves = stateManager.GenerateLegalMoves(currentState);
        Debug.Log($"생성된 이동 수: {AllMoves.Count}");
    }
    // Move 객체를 이용해 체스 이동을 실행하는 메서드
    private  void ExecuteMove(Move move)
    {
        Debug.Log($"isExecutingMove: {isExecutingMove}");
        if (!IsValidMove(move) || isExecutingMove) return;

        isExecutingMove = true;

        // 현재 상태를 히스토리에 저장
        stateHistory.Push(currentState.Clone());
        moveHistory.Push(move);

        // 현재 플레이어 기록 (턴 전환을 위해)
        bool wasWhiteTurn = currentState.IsWhiteTurn;

        // 이동 로그
        Debug.Log($"이동 실행: {move}");

        // ChessStateManager를 통해 이동 적용
        stateManager.ApplyMoveToState(currentState, move);

        // 시각화 업데이트
        UpdateVisualization(move);

        // 이벤트 발생
        OnGameStateChanged.Invoke(currentState);


  
        return;

    }

    // 이동 요청을 큐에 추가하는 메서드
    public void QueueMove(Move move)
    {
        if (IsValidMove(move))
        {
            moveQueue.Enqueue(move);
            Debug.Log($"이동 요청 큐에 추가됨: {move}");
            Debug.Log("isProcessingMoves = " + isProcessingMoves);
            // 아직 처리 중이 아니면 처리 시작
            if (!isProcessingMoves)
            {
                ProcessMoveQueue();
            }
        }
        else
        {
            Debug.LogWarning($"유효하지 않은 이동 요청: {move}"); 
        }
    }
    private void ProcessMoveQueue()
    {
        if (isProcessingMoves)
        {
            Debug.LogWarning("이미 이동 큐 처리 중입니다.");
            return;
        }

        isProcessingMoves = true;
        Debug.Log($"이동 큐 처리 시작 (큐 크기: {moveQueue.Count})");

        if (moveQueue.Count > 0)
        { 
            // 실행 중인 이동이 없을 때만 다음 이동 실행
            if (!isExecutingMove)
            {
                Move nextMove = moveQueue.Dequeue();
                Debug.Log($"큐에서 다음 이동 실행: {nextMove}, 남은 이동: {moveQueue.Count}");

                ExecuteMove(nextMove);

                // 큐 처리 중 게임이 종료된 경우 나머지 큐 처리 중단
                if (IsGameOver())
                {
                    Debug.Log("게임이 종료되었습니다. 남은 이동 요청 취소됨.");
                    moveQueue.Clear();
                    return;
                }

            }
            else
            {
                Debug.Log("이동 실행 중, 대기 중...");

            }
        }

        isProcessingMoves = false;

    }

    // 이전 움직임을 취소하고 상태를 되돌리는 메서드
    public bool UndoMove()
    {
        // 히스토리가 비어있으면 실행 불가
        if (stateHistory.Count == 0 || moveHistory.Count == 0)
        {
            Debug.Log("되돌릴 이동이 없습니다.");
            return false;
        }
        
        // AI vs AI 게임에서는 차례에 상관없이 하나만 되돌리기
        bool isAIvsAI = !whitePlayer.IsHumanPlayer && !blackPlayer.IsHumanPlayer;
        
        // 현재 차례가 AI이고 상대방이 인간 플레이어인 경우, 인간 플레이어의 이동까지 되돌리기
        bool needsDoubleUndo = false;
        
        if (!isAIvsAI)
        {
            bool isCurrentAI = (currentState.IsWhiteTurn && !whitePlayer.IsHumanPlayer) ||
                               (!currentState.IsWhiteTurn && !blackPlayer.IsHumanPlayer);
                               
            bool isOpponentHuman = (currentState.IsWhiteTurn && blackPlayer.IsHumanPlayer) ||
                                  (!currentState.IsWhiteTurn && whitePlayer.IsHumanPlayer);
                                  
            needsDoubleUndo = isCurrentAI && isOpponentHuman;
        }
        
        // 기본 실행 취소
        ChessGameState previousState = stateHistory.Pop();
        Move lastMove = moveHistory.Pop();
        
        // 현재 턴 기록
        bool wasCurrentTurnWhite = currentState.IsWhiteTurn;
        
        // 필요한 경우 두 번 실행 취소 (인간 vs AI 게임에서 인간 플레이어의 턴까지)
        if (needsDoubleUndo && stateHistory.Count > 0 && moveHistory.Count > 0)
        {
            previousState = stateHistory.Pop();
            lastMove = moveHistory.Pop();
        }
        
        // 이전 상태 복원
        currentState = previousState;
        
        Debug.Log($"이동 취소 후 턴: {(currentState.IsWhiteTurn ? "백색" : "흑색")}");
        
        // 상태 업데이트
        UpdateBoard();
        
        // 이동 목록 재생성
        GenerateMovesForCurrentState();
        
        // 시각화 업데이트
        if (boardVisualizer != null)
        {
            boardVisualizer.UpdateVisualizationAfterSpecialMove(currentState);
        }
        
        // 이벤트 발생
        OnGameStateChanged.Invoke(currentState);
        
        // 디버그 로그
        Debug.Log($"이동 취소: {BitHelper.IndexToCoordinate(lastMove.FromSquare)} -> {BitHelper.IndexToCoordinate(lastMove.ToSquare)}");
        
        // 플레이어 턴 재설정
        ResetPlayerTurns();
        
        return true;
    }

    // 플레이어 턴 재설정
    private async void ResetPlayerTurns()
    {
       await Task.Run(() =>
        {
            // 모든 플레이어의 이전 상태 초기화
            whitePlayer?.OnGameEnded();
            blackPlayer?.OnGameEnded();
        });
        await Task.Run(() =>
        {
            SetPlayerTurn();
        });
        

    }
    private void SetPlayerTurn()
    {
        // 현재 턴에 맞는 플레이어만 턴 시작
        if (currentState.IsWhiteTurn)
        {
            Debug.Log("백색 플레이어 턴 시작");
            whitePlayer?.OnTurnStarted();
        }
        else
        {
            Debug.Log("흑색 플레이어 턴 시작");
            blackPlayer?.OnTurnStarted();
        }
    }

    // 시각화 업데이트
    private void UpdateVisualization(Move move)
    {
        if (boardVisualizer != null)
        {
            // 모든 이동에 대해 기본 보드 업데이트
            UpdateBoard();
            
            // 특수 상황 처리 후 시각화 업데이트
            boardVisualizer.UpdateVisualizationAfterSpecialMove(currentState);
        }
    }

    // 디버그 정보 출력
    private void DebugPrintMoveInformation()
    {
        bool isWhiteTurn = currentState.IsWhiteTurn;
        Debug.Log($"\n===== 생성된 이동 목록 ({(isWhiteTurn ? "백색" : "흑색")} 차례) =====");
        
        foreach (var legalmove in AllMoves)
        {
            string moveType = GetMoveTypeString(legalmove);
            string pieceType = GetPieceTypeString(legalmove);
            string fromCoord = BitHelper.IndexToCoordinate(legalmove.FromSquare);
            string toCoord = BitHelper.IndexToCoordinate(legalmove.ToSquare);
            Debug.Log($"{pieceType} {fromCoord} -> {toCoord} ({moveType})");
        }
        
        Debug.Log($"가능한 이동 수: {AllMoves.Count}");
        
        if (currentState.CurrentGameState == ChessGameState.GameState.Checkmate)
        {
            Debug.Log($"체크메이트! {(currentState.IsWhiteTurn ? "흑" : "백")}의 승리!");
        }
        else if (currentState.CurrentGameState == ChessGameState.GameState.Stalemate)
        {
            Debug.Log("스테일메이트! 무승부!");
        }
    }

    // 이동 타입 문자열 반환
    private string GetMoveTypeString(Move move)
    {
        if (move.IsCapture) return "캡처";
        if (move.IsPawnTwoUp) return "폰 2칸 전진";
        if (move.IsEnPassant) return "앙파상";
        if (move.HasFlag(Move.KingSideCastleFlag)) return "킹 사이드 캐슬링";
        if (move.HasFlag(Move.QueenSideCastleFlag)) return "퀸 사이드 캐슬링";
        if (move.IsPromotion) return "프로모션";
        return "일반 이동";
    }

    // 기물 타입 문자열 반환
    private string GetPieceTypeString(Move move)
    {
        if (move.FromSquare >= 0 && move.FromSquare < 64)
        {
            int pieceValue = currentState.Board[move.FromSquare];
            if (pieceValue != 0)
            {
                bool isWhite = (pieceValue & pieceNum.colorMask) == pieceNum.white;
                int pieceTypeValue = pieceValue & pieceNum.pieceMask;
                
                if ((pieceTypeValue & pieceNum.pwan) != 0) return "폰";
                if ((pieceTypeValue & pieceNum.knight) != 0) return "나이트";
                if ((pieceTypeValue & pieceNum.bishop) != 0) return "비숍";
                if ((pieceTypeValue & pieceNum.rook) != 0) return "룩";
                if ((pieceTypeValue & pieceNum.queen) != 0) return "퀸";
                if ((pieceTypeValue & pieceNum.king) != 0) return "킹";
            }
        }
        return "알 수 없음";
    }

    // 이동이 현재 유효한지 확인
    public bool IsValidMove(Move move)
    {
        return stateManager.IsValidMove(currentState, move);
    }
    
    // 게임 모드에 따라 플레이어 설정
    private void SetupPlayers()
    {
        // 기존 플레이어 컴포넌트 제거
        if (whitePlayer != null && (whitePlayer as MonoBehaviour) != null)
        {
            Destroy((whitePlayer as MonoBehaviour).gameObject);
        }
        
        if (blackPlayer != null && (blackPlayer as MonoBehaviour) != null)
        {
            Destroy((blackPlayer as MonoBehaviour).gameObject);
        }
        
        // 새 플레이어 생성
        switch (gameMode)
        {
            case GameMode.HumanVsHuman:
                whitePlayer = CreatePlayer<HumanPlayer>("백색 플레이어");
                blackPlayer = CreatePlayer<HumanPlayer>("흑색 플레이어");
                break;
            
            case GameMode.HumanVsRandomAI:
                whitePlayer = CreatePlayer<HumanPlayer>("백색 플레이어");
                blackPlayer = CreatePlayer<RandomAIPlayer>("흑색 랜덤 AI");
                break;
            
            case GameMode.HumanVsMinimaxAI:
                whitePlayer = CreatePlayer<HumanPlayer>("백색 플레이어");
                blackPlayer = CreatePlayer<MinimaxAIPlayer>("흑색 미니맥스 AI");
                break;
            
            case GameMode.RandomAIVsHuman:
                whitePlayer = CreatePlayer<RandomAIPlayer>("백색 랜덤 AI");
                blackPlayer = CreatePlayer<HumanPlayer>("흑색 플레이어");
                break;
            
            case GameMode.MinimaxAIVsHuman:
                whitePlayer = CreatePlayer<MinimaxAIPlayer>("백색 미니맥스 AI");
                blackPlayer = CreatePlayer<HumanPlayer>("흑색 플레이어");
                break;
            
            case GameMode.RandomAIVsRandomAI:
                whitePlayer = CreatePlayer<RandomAIPlayer>("백색 랜덤 AI");
                blackPlayer = CreatePlayer<RandomAIPlayer>("흑색 랜덤 AI");
                break;
            
            case GameMode.MinimaxAIVsRandomAI:
                whitePlayer = CreatePlayer<MinimaxAIPlayer>("백색 미니맥스 AI");
                blackPlayer = CreatePlayer<RandomAIPlayer>("흑색 랜덤 AI");
                break;
            
            case GameMode.RandomAIVsMinimaxAI:
                whitePlayer = CreatePlayer<RandomAIPlayer>("백색 랜덤 AI");
                blackPlayer = CreatePlayer<MinimaxAIPlayer>("흑색 미니맥스 AI");
                break;
            
            case GameMode.MinimaxAIVsMinimaxAI:
                whitePlayer = CreatePlayer<MinimaxAIPlayer>("백색 미니맥스 AI");
                blackPlayer = CreatePlayer<MinimaxAIPlayer>("흑색 미니맥스 AI");
                break;
        }
        
        // 플레이어 초기화
        whitePlayer.Initialize(this);
        blackPlayer.Initialize(this);
    }

    // 지정된 유형의 플레이어 생성
    private T CreatePlayer<T>(string playerName) where T : BasePlayer
    {
        GameObject playerObj = new GameObject(playerName);
        playerObj.transform.parent = transform;
        T player = playerObj.AddComponent<T>();
        return player;
    }

    // 게임 모드 설정
    public void SetGameMode(GameMode newMode)
    {
        gameMode = newMode;
    }

    // 게임 재시작
    public void RestartGame()
    {
        // AI 생각 상태 초기화
        isAIThinking = false;
        
        // 히스토리 초기화
        stateHistory.Clear();
        moveHistory.Clear();
        
        // 기존 플레이어 제거 및 새 플레이어 설정
        SetupPlayers();
        
        // 기본 FEN으로 게임 초기화
        InitializeGame();
        
        // 확실히 모든 플레이어 턴 상태 초기화
        whitePlayer?.OnGameEnded();
        blackPlayer?.OnGameEnded();
        
        // 첫 번째 플레이어(백색) 턴 시작
        whitePlayer?.OnTurnStarted();
    }

    // AI 턴 시작 알림 (AI 플레이어가 호출)
    public void NotifyAITurnStarted()
    {
        isAIThinking = true;
        Debug.Log("AI 턴 시작 - 사용자 입력 제한됨");
    }

    // AI 턴 종료 알림 (AI 플레이어가 호출)
    public  void NotifyAITurnEnded(Move move)
    {
        isAIThinking = false;
        QueueMove(move);
        // 새 이동 리스트 생성
        GenerateMovesForCurrentState();


        // 다음 차례 플레이어 결정
        IPlayer nextPlayer = currentState.IsWhiteTurn ? whitePlayer : blackPlayer;
        // 게임 종료 확인
        if (IsGameOver())
        {
            Debug.Log($"게임 종료! 상태: {currentState.CurrentGameState}");

            // 게임 종료 상태가 변경되었으므로 이벤트 다시 발생
            OnGameStateChanged.Invoke(currentState);

            // 양쪽 플레이어에게 게임 종료 알림
            if(nextPlayer != null)
            {
               if(currentState.IsWhiteTurn)
                {
                    whitePlayer?.OnGameEnded();

                }
                else
                {
                    blackPlayer?.OnGameEnded();
                }
            }
            
            isExecutingMove = false;

            return;
        }
        isExecutingMove = false;


        Debug.Log("다음 플레이어 상태");

        // 다음 플레이어가 AI인 경우
        if (nextPlayer != null && !nextPlayer.IsHumanPlayer)
        {
            // 한 프레임 뒤에 턴 시작
            StartCoroutine(WaitAndStartNextTurn(nextPlayer));

        }




        Debug.Log("AI 턴 종료 - 사용자 입력 가능");
    }
    //유니티 입력 시스템이 끝날 때까지 대기
    private IEnumerator WaitAndStartNextTurn(IPlayer nextPlayer)
    {
        yield return null; // 한 프레임 대기
        nextPlayer?.OnTurnStarted();
        Debug.Log("다음 턴 시작, 게임 진행 계속됩니다. 이동 큐 상태: " + moveQueue.Count);
    }
    public void NotifyHumanTurnEnded(Move move)
    {

        QueueMove(move);
        // 새 이동 리스트 생성
        GenerateMovesForCurrentState();
        // 다음 차례 플레이어 결정
        IPlayer nextPlayer = currentState.IsWhiteTurn ? whitePlayer : blackPlayer;
        // 게임 종료 확인
        if (IsGameOver())
        {
            Debug.Log($"게임 종료! 상태: {currentState.CurrentGameState}");

            // 게임 종료 상태가 변경되었으므로 이벤트 다시 발생
            OnGameStateChanged.Invoke(currentState);

            // 양쪽 플레이어에게 게임 종료 알림
            if (nextPlayer != null)
            {
                if (nextPlayer.PlayerName == whitePlayer.PlayerName)
                {
                    whitePlayer?.OnGameEnded();

                }
                else
                {
                    blackPlayer?.OnGameEnded();
                }
            }

            isExecutingMove = false;

            return;
        }
        isExecutingMove = false;


        Debug.Log("다음 플레이어 상태");
        if (nextPlayer != null)
        {
            // 한 프레임 뒤에 턴 시작
            StartCoroutine(WaitAndStartNextTurn(nextPlayer));

        }
        Debug.Log("사용자 턴 종료");
    }
    // 현재 활성화된 플레이어가 AI인지 확인
    public bool IsCurrentPlayerAI()
    {
        return (currentState.IsWhiteTurn && whitePlayer != null && !whitePlayer.IsHumanPlayer) ||
               (!currentState.IsWhiteTurn && blackPlayer != null && !blackPlayer.IsHumanPlayer);
    }

    // 현재 가능한 모든 합법적 이동을 반환하는 메서드
    public List<Move> GetLegalMoves()
    {
        if (AllMoves == null || AllMoves.Count == 0)
        {
            GenerateMovesForCurrentState();
        }
        return AllMoves;
    }

    // 비트보드 시각화 함수를, Update 메서드 내에 추가
    private void Update()
    {
        // 키보드 단축키로 비트보드 시각화 전환
        if (Input.GetKeyDown(KeyCode.B))
        {
            ToggleBitboardVisualization();
        }

        // 다양한 비트보드 시각화 단축키
        if (enableBitboardVisualization)
        {
            // 숫자 키로 다양한 비트보드 선택
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.WhitePieces);
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.BlackPieces);
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.WhiteAttackMap);
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.BlackAttackMap);
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.PinnedPieces);
            else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.CheckingPieces);
            else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
                VisualizeBitboard(ChessBoardVisualizer.BitboardType.CheckBlockingMask);
            else if (Input.GetKeyDown(KeyCode.Escape))
                ClearBitboardVisualization();
        }
    }

    /// <summary>
    /// 비트보드 시각화를 켜거나 끕니다.
    /// </summary>
    public void ToggleBitboardVisualization()
    {
        enableBitboardVisualization = !enableBitboardVisualization;
        
        if (enableBitboardVisualization)
        {
            Debug.Log("비트보드 시각화 활성화 - 숫자 키(1-7)로 다양한 비트보드 보기, ESC로 해제");
            // 초기 비트보드 표시 (기본적으로 백색 기물)
            VisualizeBitboard(ChessBoardVisualizer.BitboardType.WhitePieces);
        }
        else
        {
            ClearBitboardVisualization();
            Debug.Log("비트보드 시각화 비활성화");
        }
    }

    /// <summary>
    /// 지정된 유형의 비트보드를 시각화합니다.
    /// </summary>
    public void VisualizeBitboard(ChessBoardVisualizer.BitboardType bitboardType)
    {
        if (boardVisualizer == null) return;
        
        currentBitboardType = bitboardType;
        boardVisualizer.VisualizeBitboard(bitboardType, currentState);
    }

    /// <summary>
    /// 비트보드 시각화를 지웁니다.
    /// </summary>
    public void ClearBitboardVisualization()
    {
        if (boardVisualizer == null) return;
        
        boardVisualizer.ClearBitboardOverlays();
        currentBitboardType = ChessBoardVisualizer.BitboardType.None;
    }

    // 중복되는 모든 ContextMenu 항목(7개)을 삭제하고, 다음의 하나만 유지합니다.
    [ContextMenu("비트보드 시각화 토글")]
    private void ToggleBitboardVisualizationMenu()
    {
        ToggleBitboardVisualization();
    }
    // 인터럽트 처리: 현재 처리 중인 모든 이동 큐를 취소
    public void ClearMoveQueue()
    {
        moveQueue.Clear();
    }

    private void OnDestroy()
    {
        // 종료 시 모든 자원 정리
        ClearMoveQueue();

        // 메모리 명시적 정리
        AllMoves?.Clear();
        stateHistory?.Clear();
        moveHistory?.Clear();
    }
} 