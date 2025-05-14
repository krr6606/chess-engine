using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 종료 패널을 관리하는 클래스
/// </summary>
public class GameOverPanel : MonoBehaviour
{
    [Header("UI 요소")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("참조")]
    [SerializeField] private BoardManager chessManager;
    
    private void Awake()
    {
        // 시작 시 패널 숨기기
        gameObject.SetActive(false);
        
        // 버튼 이벤트 연결
        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(OnPlayAgainClick);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClick);
        }
        
        // ChessManager 찾기
        if (chessManager == null)
        {
            chessManager = FindObjectOfType<BoardManager>();
        }
        
        // ChessManager에 게임 상태 변경 이벤트 연결
        if (chessManager != null)
        {
            chessManager.OnGameStateChanged.AddListener(OnGameStateChanged);
        }
    }
    
    // 게임 상태 변경 시 호출
    private void OnGameStateChanged(ChessGameState state)
    {
        CheckGameOver(state);
    }
    
    // 게임 종료 상태 확인
    private void CheckGameOver(ChessGameState state)
    {
        bool isGameOver = state.CurrentGameState == ChessGameState.GameState.Checkmate ||
                         state.CurrentGameState == ChessGameState.GameState.Stalemate ||
                         state.CurrentGameState == ChessGameState.GameState.DrawByFiftyMoveRule ||
                         state.CurrentGameState == ChessGameState.GameState.DrawByRepetition ||
                         state.CurrentGameState == ChessGameState.GameState.DrawByInsufficientMaterial;
        
        if (isGameOver)
        {
            ShowGameOverPanel(state);
        }
        else
        {
            HideGameOverPanel();
        }
    }
    
    // 게임 종료 패널 표시
    public void ShowGameOverPanel(ChessGameState state)
    {
        // 타이틀 설정
        if (titleText != null)
        {
            switch (state.CurrentGameState)
            {
                case ChessGameState.GameState.Checkmate:
                    titleText.text = "체크메이트!";
                    break;
                case ChessGameState.GameState.Stalemate:
                case ChessGameState.GameState.DrawByFiftyMoveRule:
                case ChessGameState.GameState.DrawByRepetition:
                case ChessGameState.GameState.DrawByInsufficientMaterial:
                    titleText.text = "무승부!";
                    break;
                default:
                    titleText.text = "게임 종료";
                    break;
            }
        }
        
        // 메시지 설정
        if (messageText != null)
        {
            switch (state.CurrentGameState)
            {
                case ChessGameState.GameState.Checkmate:
                    messageText.text = state.IsWhiteTurn ? 
                        "흑색 플레이어의 승리!" : 
                        "백색 플레이어의 승리!";
                    break;
                case ChessGameState.GameState.Stalemate:
                    messageText.text = "스테일메이트로 인한 무승부입니다.";
                    break;
                case ChessGameState.GameState.DrawByFiftyMoveRule:
                    messageText.text = "50수 규칙에 의한 무승부입니다.";
                    break;
                case ChessGameState.GameState.DrawByRepetition:
                    messageText.text = "3회 동형 반복에 의한 무승부입니다.";
                    break;
                case ChessGameState.GameState.DrawByInsufficientMaterial:
                    messageText.text = "부족한 자원에 의한 무승부입니다.";
                    break;
                default:
                    messageText.text = "게임이 종료되었습니다.";
                    break;
            }
        }
        
        // 패널 활성화
        gameObject.SetActive(true);
    }
    
    // 게임 종료 패널 숨기기
    public void HideGameOverPanel()
    {
        gameObject.SetActive(false);
    }
    
    // 다시 하기 버튼 클릭 처리
    private void OnPlayAgainClick()
    {
        if (chessManager != null)
        {
            chessManager.RestartGame();
            HideGameOverPanel();
        }
    }
    
    // 메인 메뉴 버튼 클릭 처리
    private void OnMainMenuClick()
    {
        // 메인 메뉴로 이동 로직
        // 현재는 게임 재시작으로 대체
        if (chessManager != null)
        {
            chessManager.RestartGame();
            HideGameOverPanel();
        }
    }
} 