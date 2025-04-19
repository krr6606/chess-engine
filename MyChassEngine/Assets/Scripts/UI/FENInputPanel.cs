using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FENInputPanel : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] public ChessManager chessManager;
    [SerializeField] public TMP_InputField fenInputField;
    [SerializeField] public Button applyButton;
    [SerializeField] public Button resetButton;
    [SerializeField] public Button showCurrentFenButton;
    [SerializeField] public TextMeshProUGUI statusText;
    
    [Header("설정")]
    [SerializeField] private float statusMessageDuration = 3f;
    [SerializeField] private bool showBitboardsInDebug = false;
    
    private float statusMessageTimer = 0f;
    
    private void Awake()
    {
        if (chessManager == null)
        {
            chessManager = FindObjectOfType<ChessManager>();
        }
        
        // 기본 FEN 문자열로 초기화
        if (fenInputField != null && string.IsNullOrEmpty(fenInputField.text))
        {
            fenInputField.text = FENParser.StartPositionFEN;
        }
    }
    
    private void Start()
    {
        // 버튼 이벤트 설정
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplyFEN);
        }
        
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefault);
        }
        
        if (showCurrentFenButton != null)
        {
            showCurrentFenButton.onClick.AddListener(ShowCurrentFEN);
        }
    }
    
    private void Update()
    {
        // 상태 메시지 타이머 처리
        if (statusMessageTimer > 0)
        {
            statusMessageTimer -= Time.deltaTime;
            if (statusMessageTimer <= 0 && statusText != null)
            {
                statusText.text = "";
            }
        }
    }
    
    // 입력된 FEN 적용
    public void ApplyFEN()
    {
        if (fenInputField == null || chessManager == null) return;
        
        string fenString = fenInputField.text.Trim();
        if (string.IsNullOrEmpty(fenString))
        {
            ShowStatusMessage("FEN 문자열이 비어있습니다.");
            return;
        }
        
        try
        {
            // FEN 유효성 검사 (간단한 검사)
            if (!IsValidFEN(fenString))
            {
                ShowStatusMessage("유효하지 않은 FEN 문자열입니다.");
                return;
            }
            
            // 체스 매니저를 통해 FEN 적용
            chessManager.SetFEN(fenString);
            ShowStatusMessage("FEN 문자열이 적용되었습니다.");
            
            // 디버그 출력
            chessManager.DebugPrintFEN();
            chessManager.DebugPrintBoard();
            
            if (showBitboardsInDebug)
            {
                chessManager.DebugPrintBitboards();
            }
        }
        catch (System.Exception e)
        {
            ShowStatusMessage("FEN 적용 중 오류: " + e.Message);
            Debug.LogError("FEN 적용 중 오류: " + e.Message);
        }
    }
    
    // 기본 FEN으로 초기화
    public void ResetToDefault()
    {
        if (fenInputField != null)
        {
            fenInputField.text = FENParser.StartPositionFEN;
            ApplyFEN();
        }
    }
    
    // 현재 FEN 표시
    public void ShowCurrentFEN()
    {
        if (chessManager == null) return;
        
        string currentFEN = chessManager.GetCurrentFEN();
        if (fenInputField != null)
        {
            fenInputField.text = currentFEN;
        }
        
        ShowStatusMessage("현재 FEN: " + currentFEN);
        Debug.Log("현재 FEN: " + currentFEN);
    }
    
    // 상태 메시지 표시
    private void ShowStatusMessage(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusMessageTimer = statusMessageDuration;
        }
    }
    
    // 간단한 FEN 유효성 검사
    private bool IsValidFEN(string fen)
    {
        string[] parts = fen.Split(' ');
        
        // 최소 4개 부분(보드 상태, 차례, 캐슬링, 앙파상)이 있어야 함
        if (parts.Length < 4) return false;
        
        // 보드 상태 검사 (8개 랭크)
        string[] ranks = parts[0].Split('/');
        if (ranks.Length != 8) return false;
        
        // 차례 검사 (w 또는 b)
        if (parts[1] != "w" && parts[1] != "b") return false;
        
        // 여기서 더 상세한 검사를 할 수 있지만, 
        // FENParser가 나중에 유효하지 않은 FEN을 처리할 것이므로 기본적인 검사만 수행
        
        return true;
    }
} 