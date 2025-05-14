using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BoardManager chessManager;
    
    [Header("UI 요소")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown aiDifficultyDropdown;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Toggle aiDebugToggle; // AI 디버그 모드 토글
    
    [Header("게임 종료 UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverText;
    [SerializeField] private Button playAgainButton;
    
    [Header("설정")]
    [SerializeField] private int aiDifficulty = 1; // 1-3: 초급, 중급, 고급
    [SerializeField] private bool aiDebugMode = false; // AI 디버그 모드
    
    private void Start()
    {
        // UI 요소 초기화
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        if (undoButton != null)
        {
            undoButton.onClick.AddListener(UndoLastMove);
        }
        
        if (gameModeDropdown != null)
        {
            InitializeGameModeDropdown();
            gameModeDropdown.onValueChanged.AddListener(ChangeGameMode);
        }
        
        if (aiDifficultyDropdown != null)
        {
            InitializeAIDifficultyDropdown();
            aiDifficultyDropdown.onValueChanged.AddListener(ChangeAIDifficulty);
        }
        
        if (aiDebugToggle != null)
        {
            aiDebugToggle.isOn = aiDebugMode;
            aiDebugToggle.onValueChanged.AddListener(ToggleAIDebugMode);
        }
        
        if (chessManager != null)
        {
            chessManager.OnGameStateChanged.AddListener(UpdateStatusText);
            chessManager.OnGameStateChanged.AddListener(CheckGameOver);
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
            
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(RestartGame);
            }
        }
        
        // AI 디버그 모드 초기 설정
        UpdateAIDebugMode();
    }
    
    private void Update()
    {
        // AI 생각 중일 때 UI 비활성화
        bool aiThinking = chessManager != null && chessManager.IsAIThinking;
        
        // 게임 조작 버튼 비활성화
        if (undoButton != null)
        {
            undoButton.interactable = !aiThinking;
        }
        
        if (restartButton != null)
        {
            restartButton.interactable = !aiThinking;
        }
        
        if (gameModeDropdown != null)
        {
            gameModeDropdown.interactable = !aiThinking;
        }
        
        if (aiDifficultyDropdown != null)
        {
            aiDifficultyDropdown.interactable = !aiThinking;
        }
        
        if (aiDebugToggle != null)
        {
            aiDebugToggle.interactable = !aiThinking;
        }
        
        // AI 생각 중일 때 상태 텍스트 업데이트
        if (aiThinking && statusText != null)
        {
            string currentPlayer = chessManager.IsWhiteTurn() ? "백색 AI" : "흑색 AI";
            statusText.text = $"{currentPlayer} 생각 중...";
        }
    }
    
    // AI 디버그 모드 토글
    private void ToggleAIDebugMode(bool isOn)
    {
        aiDebugMode = isOn;
        UpdateAIDebugMode();
    }
    
    // AI 디버그 모드 업데이트
    private void UpdateAIDebugMode()
    {
        // 모든 AI 플레이어에게 디버그 모드 설정
        BasePlayer[] players = chessManager.GetComponentsInChildren<BasePlayer>();
        
        foreach (BasePlayer player in players)
        {
            if (player is AIPlayer)
            {
                (player as AIPlayer).SetDebugMode(aiDebugMode);
            }
        }
    }
    
    private void InitializeGameModeDropdown()
    {
        gameModeDropdown.ClearOptions();
        
        // 게임 모드 이름 목록
        string[] modeNames = {
            "인간 vs 인간",
            "인간 vs 랜덤 AI",
            "인간 vs 미니맥스 AI",
            "랜덤 AI vs 인간",
            "미니맥스 AI vs 인간",
            "랜덤 AI vs 랜덤 AI",
            "미니맥스 AI vs 랜덤 AI",
            "랜덤 AI vs 미니맥스 AI",
            "미니맥스 AI vs 미니맥스 AI"
        };
        
        // 드롭다운 옵션 추가
        foreach (string modeName in modeNames)
        {
            gameModeDropdown.options.Add(new TMP_Dropdown.OptionData(modeName));
        }
        
        // 현재 모드 설정
        gameModeDropdown.value = (int)chessManager.gameMode;
        gameModeDropdown.RefreshShownValue();
    }
    
    private void ChangeGameMode(int modeIndex)
    {
        // ChessManager의 게임 모드 변경
        chessManager.SetGameMode((BoardManager.GameMode)modeIndex);
        
        // 게임 재시작
        RestartGame();
    }
    
    private void RestartGame()
    {
        chessManager.RestartGame();
        
        // AI 디버그 모드 재설정
        UpdateAIDebugMode();
    }
    
    private void UndoLastMove()
    {
        if (chessManager != null)
        {
            chessManager.UndoMove();
        }
    }
    
    private void UpdateStatusText(ChessGameState state)
    {
        if (statusText == null) return;
        
        string statusMessage;
        
        switch (state.CurrentGameState)
        {
            case ChessGameState.GameState.Playing:
                statusMessage = state.IsWhiteTurn ? "백색 차례" : "흑색 차례";
                break;
                
            case ChessGameState.GameState.Check:
                statusMessage = state.IsWhiteTurn ? "백색 체크!" : "흑색 체크!";
                break;
                
            case ChessGameState.GameState.Checkmate:
                statusMessage = state.IsWhiteTurn ? "흑색 승리! 체크메이트" : "백색 승리! 체크메이트";
                break;
                
            case ChessGameState.GameState.Stalemate:
                statusMessage = "스테일메이트! 무승부";
                break;
                
            case ChessGameState.GameState.DrawByFiftyMoveRule:
                statusMessage = "50수 규칙에 의한 무승부";
                break;
                
            default:
                statusMessage = "게임 중";
                break;
        }
        
        statusText.text = statusMessage;
    }
    
    private void InitializeAIDifficultyDropdown()
    {
        aiDifficultyDropdown.ClearOptions();
        
        // 난이도 이름 목록
        string[] difficultyNames = {
            "초급 (레벨 1)",
            "중급 (레벨 2)",
            "고급 (레벨 3)"
        };
        
        // 드롭다운 옵션 추가
        foreach (string name in difficultyNames)
        {
            aiDifficultyDropdown.options.Add(new TMP_Dropdown.OptionData(name));
        }
        
        // 현재 난이도 설정
        aiDifficultyDropdown.value = aiDifficulty - 1; // 0-based 인덱스
        aiDifficultyDropdown.RefreshShownValue();
    }
    
    private void ChangeAIDifficulty(int difficultyIndex)
    {
        // 난이도 설정 (1-based)
        aiDifficulty = difficultyIndex + 1;
        
        // RandomAIPlayer 설정 업데이트
        UpdateAIPlayers();
        
        // 게임 재시작 (선택적)
        if (IsAIInvolved())
        {
            RestartGame();
        }
    }
    
    private bool IsAIInvolved()
    {
        // 현재 게임 모드에 AI가 포함되어 있는지 확인
        var mode = chessManager.gameMode;
        return mode != BoardManager.GameMode.HumanVsHuman;
    }
    
    private void UpdateAIPlayers()
    {
        // chessManager의 모든 AI 플레이어에게 난이도 설정
        BasePlayer[] players = chessManager.GetComponentsInChildren<BasePlayer>();
        
        foreach (BasePlayer player in players)
        {
            if (player is RandomAIPlayer)
            {
                // 리플렉션으로 난이도 설정
                var aiPlayer = player as RandomAIPlayer;
                var difficultyField = aiPlayer.GetType().GetField("difficultyLevel", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                    
                if (difficultyField != null)
                {
                    difficultyField.SetValue(aiPlayer, aiDifficulty);
                }
                
                // 플레이어 재초기화
                aiPlayer.Initialize(chessManager);
                
                // 디버그 모드 설정
                aiPlayer.SetDebugMode(aiDebugMode);
            }
            else if (player is AIPlayer)
            {
                // 다른 AI 플레이어 유형에도 디버그 모드 설정
                (player as AIPlayer).SetDebugMode(aiDebugMode);
            }
        }
    }
    
    private void CheckGameOver(ChessGameState state)
    {
        bool isGameOver = state.CurrentGameState == ChessGameState.GameState.Checkmate ||
                          state.CurrentGameState == ChessGameState.GameState.Stalemate ||
                          state.CurrentGameState == ChessGameState.GameState.DrawByFiftyMoveRule ||
                          state.CurrentGameState == ChessGameState.GameState.DrawByRepetition ||
                          state.CurrentGameState == ChessGameState.GameState.DrawByInsufficientMaterial;
        
        if (isGameOver && gameOverPanel != null)
        {
            string gameOverMessage = "";
            
            switch (state.CurrentGameState)
            {
                case ChessGameState.GameState.Checkmate:
                    gameOverMessage = state.IsWhiteTurn ? 
                        "체크메이트! 흑색 플레이어의 승리!" : 
                        "체크메이트! 백색 플레이어의 승리!";
                    break;
                    
                case ChessGameState.GameState.Stalemate:
                    gameOverMessage = "스테일메이트! 무승부입니다.";
                    break;
                    
                case ChessGameState.GameState.DrawByFiftyMoveRule:
                    gameOverMessage = "50수 규칙에 의한 무승부입니다.";
                    break;
                    
                case ChessGameState.GameState.DrawByRepetition:
                    gameOverMessage = "3회 동형 반복에 의한 무승부입니다.";
                    break;
                    
                case ChessGameState.GameState.DrawByInsufficientMaterial:
                    gameOverMessage = "부족한 자원에 의한 무승부입니다.";
                    break;
            }
            
            if (gameOverText != null)
            {
                gameOverText.text = gameOverMessage;
            }
            
            // 게임 종료 패널 표시
            gameOverPanel.SetActive(true);
            
            // 버튼 비활성화
            if (undoButton != null) undoButton.interactable = false;
        }
        else if (!isGameOver && gameOverPanel != null)
        {
            // 게임 중에는 패널 숨기기
            gameOverPanel.SetActive(false);
            
            // 버튼 활성화
            if (undoButton != null) undoButton.interactable = true;
        }
    }
} 