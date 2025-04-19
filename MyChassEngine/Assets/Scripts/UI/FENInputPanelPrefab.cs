#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class FENInputPanelPrefab
{
    [MenuItem("Chess/Create FEN Input Panel")]
    public static void CreateFENInputPanel()
    {
        // Canvas 확인 또는 생성
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // FEN 입력 패널 생성
        GameObject panelObj = new GameObject("FENInputPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -10);
        panelRect.sizeDelta = new Vector2(600, 120);
        
        // 패널 배경 추가
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // FEN 입력 필드 생성
        GameObject inputFieldObj = new GameObject("FENInputField");
        inputFieldObj.transform.SetParent(panelRect, false);
        RectTransform inputFieldRect = inputFieldObj.AddComponent<RectTransform>();
        inputFieldRect.anchorMin = new Vector2(0, 1);
        inputFieldRect.anchorMax = new Vector2(1, 1);
        inputFieldRect.pivot = new Vector2(0.5f, 1f);
        inputFieldRect.anchoredPosition = new Vector2(0, -20);
        inputFieldRect.sizeDelta = new Vector2(-20, 30);
        
        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
        
        // 텍스트 영역 생성
        GameObject textAreaObj = new GameObject("TextArea");
        textAreaObj.transform.SetParent(inputFieldRect, false);
        RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        
        // 입력 필드 텍스트 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textAreaRect, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Left;
        text.fontSize = 14;
        text.color = Color.white;
        
        // 입력 필드 플레이스홀더 생성
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textAreaRect, false);
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.alignment = TextAlignmentOptions.Left;
        placeholder.fontSize = 14;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        placeholder.text = "FEN 문자열 입력...";
        
        // 입력 필드 설정
        inputField.textViewport = textAreaRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        inputField.text = FENParser.StartPositionFEN;
        
        // 버튼 영역 생성
        GameObject buttonAreaObj = new GameObject("ButtonArea");
        buttonAreaObj.transform.SetParent(panelRect, false);
        RectTransform buttonAreaRect = buttonAreaObj.AddComponent<RectTransform>();
        buttonAreaRect.anchorMin = new Vector2(0, 0);
        buttonAreaRect.anchorMax = new Vector2(1, 0.5f);
        buttonAreaRect.pivot = new Vector2(0.5f, 0);
        buttonAreaRect.anchoredPosition = new Vector2(0, 15);
        buttonAreaRect.sizeDelta = new Vector2(-20, 30);
        
        // "적용" 버튼 생성
        GameObject applyButtonObj = CreateButton("ApplyButton", buttonAreaRect, "적용", new Vector2(0.1f, 0), new Vector2(120, 30));
        
        // "기본값" 버튼 생성
        GameObject resetButtonObj = CreateButton("ResetButton", buttonAreaRect, "기본값", new Vector2(0.4f, 0), new Vector2(120, 30));
        
        // "현재 FEN" 버튼 생성
        GameObject showFenButtonObj = CreateButton("ShowFENButton", buttonAreaRect, "현재 FEN", new Vector2(0.7f, 0), new Vector2(120, 30));
        
        // 상태 텍스트 생성
        GameObject statusTextObj = new GameObject("StatusText");
        statusTextObj.transform.SetParent(panelRect, false);
        RectTransform statusTextRect = statusTextObj.AddComponent<RectTransform>();
        statusTextRect.anchorMin = new Vector2(0, 0);
        statusTextRect.anchorMax = new Vector2(1, 0);
        statusTextRect.pivot = new Vector2(0.5f, 0);
        statusTextRect.anchoredPosition = new Vector2(0, 10);
        statusTextRect.sizeDelta = new Vector2(-20, 20);
        TextMeshProUGUI statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 12;
        statusText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        
        // FENInputPanel 컴포넌트 추가
        FENInputPanel fenInputPanel = panelObj.AddComponent<FENInputPanel>();
        fenInputPanel.fenInputField = inputField;
        fenInputPanel.applyButton = applyButtonObj.GetComponent<Button>();
        fenInputPanel.resetButton = resetButtonObj.GetComponent<Button>();
        fenInputPanel.showCurrentFenButton = showFenButtonObj.GetComponent<Button>();
        fenInputPanel.statusText = statusText;
        
        Debug.Log("FEN Input Panel 생성 완료!");
        
        // 새로 생성된 패널 선택
        Selection.activeGameObject = panelObj;
    }
    
    // 버튼 생성 헬퍼 메서드
    private static GameObject CreateButton(string name, RectTransform parent, string text, Vector2 anchorPosition, Vector2 size)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(anchorPosition.x, anchorPosition.y);
        buttonRect.anchorMax = new Vector2(anchorPosition.x, anchorPosition.y);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = size;
        
        Button button = buttonObj.AddComponent<Button>();
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        button.targetGraphic = buttonImage;
        
        // 버튼 텍스트 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonRect, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.text = text;
        
        return buttonObj;
    }
}
#endif 