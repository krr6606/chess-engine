using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro 네임스페이스 추가

public class ChessBoardVisualizer : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private ChessPiecePool piecePool;
    [SerializeField] private Transform boardTransform;
    
    [Header("설정")]
    [SerializeField] private float squareSize = 1f;
    [SerializeField] private float moveDuration = 0.23f;
    [SerializeField] private float captureAnimationDuration = 0.22f;
    [SerializeField] private bool useSquareBoardSize = true;
    [SerializeField] private float squareBoardSize = 8f;
    
    // 시각화 관련 색상
    [Header("시각화 색상")]
    [SerializeField] private Color checkHighlightColor = new Color(1f, 0f, 0f, 0.5f); // 체크 강조 색상
    [SerializeField] private Color castlingHighlightColor = new Color(0f, 1f, 0f, 0.5f); // 캐슬링 강조 색상
    [SerializeField] private Color enPassantHighlightColor = new Color(1f, 1f, 0f, 0.5f); // 앙파상 강조 색상
    [SerializeField] private Color promotionHighlightColor = new Color(0f, 1f, 1f, 0.5f); // 프로모션 강조 색상
    [SerializeField] private Color pinHighlightColor = new Color(1f, 0.5f, 0f, 0.5f); // 핀 강조 색상
    [SerializeField] private Color checkBlockingHighlightColor = new Color(0.5f, 0f, 1f, 0.5f); // 체크 블로킹 강조 색상
    
    // 현재 보드에 표시된 기물들을 관리
    private Dictionary<int, GameObject> pieceGameObjects = new Dictionary<int, GameObject>();
    private List<GameObject> activeAnimations = new List<GameObject>();
    
    // 체스판 칸 관리
    private GameObject[,] squares = new GameObject[8, 8];
    
    private void Awake()
    {
        if (piecePool == null)
        {
            piecePool = FindObjectOfType<ChessPiecePool>();
            if (piecePool == null)
            {
                Debug.LogError("ChessPiecePool이 참조되지 않았습니다. ChessBoardVisualizer 작동이 중단됩니다.");
            }
        }
        
        if (boardTransform == null)
        {
            boardTransform = transform;
        }
    }
    
    // 체스 보드 시각화 (int[] 배열 기반)
    public void VisualizeBoard(int[] board)
    {
        // 기존 기물 모두 회수
        ReturnAllPieces();
        
        // 보드 상태에 따라 기물 배치
        for (int square = 0; square < 64; square++)
        {
            int value = board[square];
            if (value == 0) continue; // 빈 칸 건너뛰기
            
            if (IsPieceNumFormat(value))
            {
                // pieceNum 형식(비트 표현)인 경우
                PlacePieceFromPieceNum(square, value);
            }
            else
            {
                // 표준 형식(+/- 1~6)인 경우
                PlacePieceFromStandard(square, value);
            }
        }
    }
    
    // 체스 보드 시각화 (비트보드 기반)
    public void VisualizeBitboards(ulong[] bitboards)
    {
        // 기존 기물 모두 회수
        ReturnAllPieces();
        
        // 각 비트보드 확인
        for (int bitboardIndex = 0; bitboardIndex < 12; bitboardIndex++)
        {
            ulong bitboard = bitboards[bitboardIndex];
            if (bitboard == 0) continue; // 비어있는 비트보드
            
            // 비트보드에서 1로 설정된 모든 비트(위치) 찾기
            List<int> squares = BitHelper.GetSetBitIndexes(bitboard);
            
            // 각 위치에 해당 기물 배치
            foreach (int square in squares)
            {
                int pieceValue = BitboardIndexToPieceNum(bitboardIndex);
                PlacePieceFromPieceNum(square, pieceValue);
            }
        }
    }
    
    // 비트보드 인덱스를 pieceNum 값으로 변환
    private int BitboardIndexToPieceNum(int bitboardIndex)
    {
        switch (bitboardIndex)
        {
            case ChessGameState.WHITE_PAWN: return pieceNum.whitePawn;
            case ChessGameState.WHITE_KNIGHT: return pieceNum.whiteKnight;
            case ChessGameState.WHITE_BISHOP: return pieceNum.whiteBishop;
            case ChessGameState.WHITE_ROOK: return pieceNum.whiteRook;
            case ChessGameState.WHITE_QUEEN: return pieceNum.whiteQueen;
            case ChessGameState.WHITE_KING: return pieceNum.whiteKing;
            case ChessGameState.BLACK_PAWN: return pieceNum.blackPawn;
            case ChessGameState.BLACK_KNIGHT: return pieceNum.blackKnight;
            case ChessGameState.BLACK_BISHOP: return pieceNum.blackBishop;
            case ChessGameState.BLACK_ROOK: return pieceNum.blackRook;
            case ChessGameState.BLACK_QUEEN: return pieceNum.blackQueen;
            case ChessGameState.BLACK_KING: return pieceNum.blackKing;
            default: return pieceNum.empty;
        }
    }
    
    // pieceNum 형식인지 확인 (비트 표현)
    private bool IsPieceNumFormat(int value)
    {
        // 비트 형식은 최소 64(0b01000000) 이상이거나, -64(0b11000000) 이하
        return value >= 64 || value <= -64;
    }
    
    // pieceNum 형식으로 기물 배치
    private void PlacePieceFromPieceNum(int square, int pieceValue)
    {
        // 타입과 색상 추출
        int pieceType = pieceNum.GetPieceType(pieceValue);
        bool isWhite = pieceNum.IsWhite(pieceValue);
        
        // 기물 배치
        PlacePiece(square, pieceType, isWhite);
    }
    
    // 표준 형식으로 기물 배치 (+/- 1~6)
    private void PlacePieceFromStandard(int square, int pieceValue)
    {
        // 표준 형식에서 색상과 타입 추출
        bool isWhite = pieceValue > 0;
        int pieceType = Mathf.Abs(pieceValue);
        
        // 표준 값을 pieceNum 형식으로 변환
        int pieceNumValue = pieceNum.FromStandardValue(pieceValue);
        int extractedType = pieceNum.GetPieceType(pieceNumValue);
        
        // 기물 배치
        PlacePiece(square, extractedType, isWhite);
    }
    
    // 특정 위치에 기물 배치
    private void PlacePiece(int square, int pieceType, bool isWhite)
    {
        // 이미 기물이 있으면 제거
        if (pieceGameObjects.ContainsKey(square))
        {
            GameObject existingPiece = pieceGameObjects[square];
            ChessPiece chessPiece = existingPiece.GetComponent<ChessPiece>();
            if (chessPiece != null)
            {
                piecePool.ReturnPiece(existingPiece, chessPiece.PieceType, chessPiece.IsWhite);
            }
            else
            {
                piecePool.ReturnPiece(existingPiece, pieceType, isWhite); // 기본값 사용
            }
            pieceGameObjects.Remove(square);
        }
        
        // 기물 생성 및 위치 설정
        GameObject pieceObj = piecePool.GetPiece(pieceType, isWhite);
        if (pieceObj == null)
        {
            Debug.LogError($"기물을 가져오지 못했습니다: 타입 {pieceType}, 색상 {(isWhite ? "백" : "흑")}");
            return;
        }
        
        // 위치 설정
        pieceObj.transform.SetParent(boardTransform);
        pieceObj.transform.localPosition = SquareToPosition(square);
        pieceObj.transform.localRotation = Quaternion.identity;

        
        // ChessPiece 컴포넌트 초기화
        ChessPiece piece = pieceObj.GetComponent<ChessPiece>();
        if (piece == null)
        {
            piece = pieceObj.AddComponent<ChessPiece>();
        }
        piece.PieceType = pieceType;
        piece.IsWhite = isWhite;
        piece.Square = square;
        
        // 관리 딕셔너리에 추가
        pieceGameObjects[square] = pieceObj;
    }
    
    // 모든 기물 회수
    public void ReturnAllPieces()
    {
        foreach (var keyValuePair in pieceGameObjects)
        {
            GameObject pieceObj = keyValuePair.Value;
            if (pieceObj != null)
            {
                ChessPiece piece = pieceObj.GetComponent<ChessPiece>();
                if (piece != null)
                {
                    piecePool.ReturnPiece(pieceObj, piece.PieceType, piece.IsWhite);
                }
                else
                {
                    piecePool.ReturnPiece(pieceObj, 0, true); // 기본값 사용
                }
            }
        }
        
        pieceGameObjects.Clear();
        
        // 진행 중인 애니메이션도 모두 제거
        foreach (var animObj in activeAnimations)
        {
            if (animObj != null)
            {
                Destroy(animObj);
            }
        }
        
        activeAnimations.Clear();
    }
    
    // 기물 이동
    public void MovePiece(int fromSquare, int toSquare, bool animate = true)
    {
        if (!pieceGameObjects.ContainsKey(fromSquare))
        {
            Debug.LogError($"이동할 기물이 없습니다: {fromSquare}");
            return;
        }
        
        GameObject pieceObj = pieceGameObjects[fromSquare];
        
        // 이동 대상 위치에 기물이 있으면 제거 (이동 전에 실행)
        if (pieceGameObjects.ContainsKey(toSquare))
        {
            StartCaptureAnimation(toSquare);
        }
        
        // 딕셔너리 업데이트
        pieceGameObjects.Remove(fromSquare);
        pieceGameObjects[toSquare] = pieceObj;
        
        // ChessPiece 컴포넌트 업데이트
        ChessPiece piece = pieceObj.GetComponent<ChessPiece>();
        if (piece != null)
        {
            piece.Square = toSquare;
        }
        
        if (animate)
        {
            // 애니메이션 시작
            StartCoroutine(AnimateMove(pieceObj, SquareToPosition(fromSquare), SquareToPosition(toSquare), moveDuration));
        }
        else
        {
            // 즉시 이동
            pieceObj.transform.localPosition = SquareToPosition(toSquare);
        }
    }
    
    // 캡처 애니메이션
    public void StartCaptureAnimation(int square)
    {
        if (!pieceGameObjects.ContainsKey(square)) return;
        
        GameObject pieceObj = pieceGameObjects[square];
        ChessPiece piece = pieceObj.GetComponent<ChessPiece>();
        pieceGameObjects.Remove(square);
        
        // 애니메이션 시작
        StartCoroutine(AnimateCapture(pieceObj, piece, captureAnimationDuration));
    }
    
    // 기물 이동 애니메이션
    private System.Collections.IEnumerator AnimateMove(GameObject pieceObj, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            pieceObj.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        pieceObj.transform.localPosition = endPos;
    }
    
    // 기물 캡처 애니메이션
    private System.Collections.IEnumerator AnimateCapture(GameObject pieceObj, ChessPiece piece, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = pieceObj.transform.localScale;
        Vector3 endScale = Vector3.zero;
        pieceObj.GetComponent<SpriteRenderer>().sortingOrder = 1;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.Pow(t, 5);
            pieceObj.transform.localScale = Vector3.Lerp(startScale, endScale, easedT * 1.4f);
            yield return null;
        }
        
        // 애니메이션 완료 후 기물 회수
        pieceObj.GetComponent<SpriteRenderer>().sortingOrder = 2;
        if (piece != null)
        {
            piecePool.ReturnPiece(pieceObj, piece.PieceType, piece.IsWhite);
        }
        else
        {
            piecePool.ReturnPiece(pieceObj, 0, true); // 기본값 사용
        }
    }
    
    // 특수 상황 처리 후 시각화 업데이트
    public void UpdateVisualizationAfterSpecialMove(ChessGameState state)
    {
        // 체크 상태 시각화
        UpdateCheckVisualization(state);
        
        // 캐슬링 권한 시각화
        UpdateCastlingRightsVisualization(state);
        
        // 앙파상 타겟 시각화
        UpdateEnPassantTargetVisualization(state);
        
        // 프로모션 대기 상태 시각화
        UpdatePromotionVisualization(state);
        
        // 핀된 기물 시각화
        UpdatePinnedPiecesVisualization(state);
        
        // 체크 블로킹 마스크 시각화
        UpdateCheckBlockingMaskVisualization(state);
    }

    // 체크 상태 시각화 업데이트
    private void UpdateCheckVisualization(ChessGameState state)
    {
        if (state.GetCheckingPieces() != 0)
        {
            // 체크하는 기물 강조
            List<int> checkingSquares = BitHelper.GetSetBitIndexes(state.GetCheckingPieces());
            foreach (int square in checkingSquares)
            {
                HighlightSquare(square, checkHighlightColor);
            }
            
            // 체크 상태인 킹 강조
            int kingSquare = state.IsWhiteTurn ? state.WhiteKingSquare : state.BlackKingSquare;
            if (kingSquare >= 0)
            {
                HighlightSquare(kingSquare, checkHighlightColor);
            }
        }
    }

    // 캐슬링 권한 시각화 업데이트
    private void UpdateCastlingRightsVisualization(ChessGameState state)
    {
        // 백색 킹사이드 캐슬링
        if (!state.WhiteKingSideCastleRight)
        {
            HighlightSquare(7, castlingHighlightColor);
        }
        
        // 백색 퀸사이드 캐슬링
        if (!state.WhiteQueenSideCastleRight)
        {
            HighlightSquare(0, castlingHighlightColor);
        }
        
        // 흑색 킹사이드 캐슬링
        if (!state.BlackKingSideCastleRight)
        {
            HighlightSquare(63, castlingHighlightColor);
        }
        
        // 흑색 퀸사이드 캐슬링
        if (!state.BlackQueenSideCastleRight)
        {
            HighlightSquare(56, castlingHighlightColor);
        }
    }

    // 앙파상 타겟 시각화 업데이트
    private void UpdateEnPassantTargetVisualization(ChessGameState state)
    {
        if (state.EnPassantTargetSquare != -1)
        {
            HighlightSquare(state.EnPassantTargetSquare, enPassantHighlightColor);
        }
    }

    // 프로모션 대기 상태 시각화 업데이트
    private void UpdatePromotionVisualization(ChessGameState state)
    {
        if (state.IsPromotionPending && state.PromotionSquare != -1)
        {
            HighlightSquare(state.PromotionSquare, promotionHighlightColor);
        }
    }

    // 핀된 기물 시각화 업데이트
    private void UpdatePinnedPiecesVisualization(ChessGameState state)
    {
        if (state.PinnedPieces != 0)
        {
            List<int> pinnedSquares = BitHelper.GetSetBitIndexes(state.PinnedPieces);
            foreach (int square in pinnedSquares)
            {
                HighlightSquare(square, pinHighlightColor);
            }
        }
    }

    // 체크 블로킹 마스크 시각화 업데이트
    private void UpdateCheckBlockingMaskVisualization(ChessGameState state)
    {
        if (state.GetCheckBlockingMask() != 0)
        {
            List<int> blockingSquares = BitHelper.GetSetBitIndexes(state.GetCheckBlockingMask());
            foreach (int square in blockingSquares)
            {
                HighlightSquare(square, checkBlockingHighlightColor);
            }
        }
    }

    // 특정 위치 강조
    private void HighlightSquare(int square, Color highlightColor)
    {
        if (square < 0 || square >= 64) return;
        
        int rank = square / 8;
        int file = square % 8;
        
        if (rank >= 0 && rank < 8 && file >= 0 && file < 8)
        {
            GameObject squareObj = squares[rank, file];
            if (squareObj != null)
            {
                Renderer renderer = squareObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = highlightColor;
                }
            }
        }
    }
    
    // 체스 보드 좌표를 Unity 좌표계로 변환
    public Vector3 SquareToPosition(int square)
    {
        int rank = square / 8;  // 행 (0-7)
        int file = square % 8;  // 열 (0-7)
        
        // 체스보드 좌표계를 Unity 좌표계로 변환
        if (useSquareBoardSize)
        {
            float x = file - (squareBoardSize / 2 - 0.5f);
            float y = rank - (squareBoardSize / 2 - 0.5f);
            return new Vector3(x, y, 0);
        }
        else
        {
            float x = file * squareSize;
            float y = rank * squareSize;
            return new Vector3(x, y, 0);
        }
    }    public Vector3 SquareToPosition(Coord coord)
    {
        int rank = coord.rankIndex;  // 행 (0-7)
        int file = coord.fileIndex;  // 열 (0-7)
        
        // 체스보드 좌표계를 Unity 좌표계로 변환
        if (useSquareBoardSize)
        {
            float x = file - (squareBoardSize / 2 - 0.5f);
            float y = rank - (squareBoardSize / 2 - 0.5f);
            return new Vector3(x, y, 0);
        }
        else
        {
            float x = file * squareSize;
            float y = rank * squareSize;
            return new Vector3(x, y, 0);
        }
    }
        // Unity 좌표를 체스 보드 좌표계로 변환
    public Coord PositionToSquare(Vector3 position)
    {

        
        // 체스보드 좌표계를 Unity 좌표계로 변환
        if (useSquareBoardSize)
        {
            float file = position.x + (squareBoardSize / 2 - 0.5f);
            float rank = position.y + (squareBoardSize / 2 - 0.5f);
            file = Mathf.RoundToInt(file);
            rank = Mathf.RoundToInt(rank);
            return new Coord((int)file, (int)rank);
        }
        else
        {
            float file =  position.x/squareSize;
            float rank = position.y/squareSize;
            file = Mathf.RoundToInt(file);
            rank = Mathf.RoundToInt(rank);
            return new Coord((int)file, (int)rank);
        }
    }
    public bool IsSquare(Vector3 position)
    {
        return position.x >= boardTransform.position.x - (squareBoardSize / 2) && position.x <= boardTransform.position.x + (squareBoardSize / 2) && position.y >= boardTransform.position.y - (squareBoardSize / 2) && position.y <= boardTransform.position.y + (squareBoardSize / 2);
    }
    public void HighlightSquare(Coord coord, bool isMove = false)
    {
        if (!coord.IsValidSquare() || BoardGenerator.Instance == null || BoardGenerator.Instance.tileDict == null)
        {
            Debug.LogWarning("HighlightSquare: 필요한 참조가 없습니다.");
            return;
        }

        // 체스보드 좌표를 타일 딕셔너리 인덱스로 변환 (1-8 범위)
        int file = coord.fileIndex + 1;
        int rank = coord.rankIndex + 1;

        // 범위 체크
        if (file < 1 || file > 8 || rank < 1 || rank > 8)
        {
            Debug.LogWarning($"HighlightSquare: 유효하지 않은 좌표입니다. (file: {file}, rank: {rank})");
            return;
        }

        Vector2Int tileKey = new Vector2Int(file, rank);
        if (BoardGenerator.Instance.tileDict.TryGetValue(tileKey, out GameObject tile))
        {
            if (coord.IsLightSquare())
            {
                if(isMove){
                    tile.GetComponent<SpriteRenderer>().color = BoardGenerator.HexColor("#AAFF80");
                }
                else{
                tile.GetComponent<SpriteRenderer>().color = BoardGenerator.HexColor("#FBD12F");

                }
            }
            else
            {
                if(isMove){
                    tile.GetComponent<SpriteRenderer>().color = BoardGenerator.HexColor("#62D12C");
                }
                else{
                tile.GetComponent<SpriteRenderer>().color = BoardGenerator.HexColor("#E7B31B");
                }
            }
        }
        else
        {
            Debug.LogWarning($"HighlightSquare: 타일을 찾을 수 없습니다. (file: {file}, rank: {rank})");
        }
    }
   
    public void UnhighlightSquare(Coord coord)
    {
        if (!coord.IsValidSquare() || BoardGenerator.Instance == null || BoardGenerator.Instance.tileDict == null)
        {
            return;
        }

        // 체스보드 좌표를 타일 딕셔너리 인덱스로 변환 (1-8 범위)
        int file = coord.fileIndex + 1;
        int rank = coord.rankIndex + 1;

        // 범위 체크
        if (file < 1 || file > 8 || rank < 1 || rank > 8)
        {
            Debug.LogWarning($"UnhighlightSquare: 유효하지 않은 좌표입니다. (file: {file}, rank: {rank})");
            return;
        }

        Vector2Int tileKey = new Vector2Int(file, rank);
        if (BoardGenerator.Instance.tileDict.TryGetValue(tileKey, out GameObject tile))
        {
            if (coord.IsLightSquare())
            {
                tile.GetComponent<SpriteRenderer>().color = BoardGenerator.HexColor(BoardGenerator.TileColor2);
            }
            else
            {
                tile.GetComponent<SpriteRenderer>().color = BoardGenerator.HexColor(BoardGenerator.TileColor);
            }
        }
        else
        {
            Debug.LogWarning($"UnhighlightSquare: 타일을 찾을 수 없습니다. (file: {file}, rank: {rank})");
        }
    }
    // 디버그용: 현재 보드 상태 콘솔에 시각화
    public void DebugVisualize(int[] board)
    {
        string boardStr = "";
        boardStr += "  a b c d e f g h\n";
        
        for (int rank = 7; rank >= 0; rank--)
        {
            boardStr += (rank + 1) + " ";
            
            for (int file = 0; file < 8; file++)
            {
                int square = rank * 8 + file;
                int piece = board[square];
                
                char pieceChar;
                if (piece == 0)
                {
                    pieceChar = '.';
                }
                else if (IsPieceNumFormat(piece)) // pieceNum 형식 확인
                {
                    pieceChar = pieceNum.PieceToFenChar(piece);
                }
                else // 표준 형식
                {
                    bool isWhite = piece > 0;
                    int pieceType = Mathf.Abs(piece);
                    
                    switch (pieceType)
                    {
                        case 1: pieceChar = 'p'; break;
                        case 2: pieceChar = 'n'; break;
                        case 3: pieceChar = 'b'; break;
                        case 4: pieceChar = 'r'; break;
                        case 5: pieceChar = 'q'; break;
                        case 6: pieceChar = 'k'; break;
                        default: pieceChar = '?'; break;
                    }
                    
                    if (isWhite)
                    {
                        pieceChar = char.ToUpper(pieceChar);
                    }
                }
                
                boardStr += pieceChar + " ";
            }
            
            boardStr += (rank + 1) + "\n";
        }
        
        boardStr += "  a b c d e f g h";
        
        Debug.Log("현재 보드 상태:\n" + boardStr);
    }

    #region 비트보드 시각화

    // 비트보드 오버레이 게임 오브젝트들
    private Dictionary<int, GameObject> bitboardOverlays = new Dictionary<int, GameObject>();
    
    // 비트보드 타입 열거형
    public enum BitboardType
    {
        None,
        WhiteAttacks,
        BlackAttacks,
        WhitePieces,
        BlackPieces,
        AllPieces,
        WhitePawns,
        BlackPawns,
        WhiteKnights,
        BlackKnights,
        WhiteBishops,
        BlackBishops,
        WhiteRooks,
        BlackRooks,
        WhiteQueens,
        BlackQueens,
        WhiteKing,
        BlackKing,
        PinnedPieces,
        CheckingPieces,
        CheckBlockingMask,
        LightSquares,
        DarkSquares,
        CentralSquares,
        WhiteAttackMap,
        BlackAttackMap,
        WhitePawnAttackMap,
        BlackPawnAttackMap,
        KingSafetyMask
    }

    // 현재 표시 중인 비트보드 타입
    public BitboardType currentBitboardType = BitboardType.None;

    // 비트보드 오버레이 색상 설정
    private Color bitboardOverlayColor = new Color(0f, 1f, 0f, 0.8f); // 녹색 반투명 (1의 값)
    private Color bitZeroColor = new Color(0.2f, 0.2f, 0.2f, 0.2f); // 회색 반투명 (0의 값)
    private Color bitOneColor = new Color(1f, 0.5f, 0.5f, 0.5f); // 빨간색 반투명

    /// <summary>
    /// 비트보드 타입을 선택하여 시각화합니다.
    /// </summary>
    /// <param name="bitboardType">표시할 비트보드 타입</param>
    /// <param name="gameState">체스 게임 상태</param>
    public void VisualizeBitboard(BitboardType bitboardType, ChessGameState gameState)
    {
        // 이전 오버레이 정리
        ClearBitboardOverlays();
        
        // 선택된 타입이 None이면 시각화하지 않음
        if (bitboardType == BitboardType.None)
        {
            currentBitboardType = BitboardType.None;
            return;
        }

        // 업데이트가 필요한 데이터 갱신
        gameState.UpdateAttackMaps();
        gameState.UpdatePinInformation();
        gameState.UpdateCheckInformation();
        
        // 비트보드 가져오기
        ulong bitboard = GetBitboardByType(bitboardType, gameState);
        
        // 비트보드 시각화
        VisualizeBitboardValue(bitboard);
        
        // 현재 타입 저장
        currentBitboardType = bitboardType;
    }

    /// <summary>
    /// 비트보드 타입에 따라 해당 비트보드 값을 반환합니다.
    /// </summary>
    private ulong GetBitboardByType(BitboardType bitboardType, ChessGameState gameState)
    {
        switch (bitboardType)
        {
            case BitboardType.WhitePieces:
                return gameState.WhitePieces;
            case BitboardType.BlackPieces:
                return gameState.BlackPieces;
            case BitboardType.AllPieces:
                return gameState.AllPieces;
            case BitboardType.WhitePawns:
                return gameState.BitBoards[ChessGameState.WHITE_PAWN];
            case BitboardType.BlackPawns:
                return gameState.BitBoards[ChessGameState.BLACK_PAWN];
            case BitboardType.WhiteKnights:
                return gameState.BitBoards[ChessGameState.WHITE_KNIGHT];
            case BitboardType.BlackKnights:
                return gameState.BitBoards[ChessGameState.BLACK_KNIGHT];
            case BitboardType.WhiteBishops:
                return gameState.BitBoards[ChessGameState.WHITE_BISHOP];
            case BitboardType.BlackBishops:
                return gameState.BitBoards[ChessGameState.BLACK_BISHOP];
            case BitboardType.WhiteRooks:
                return gameState.BitBoards[ChessGameState.WHITE_ROOK];
            case BitboardType.BlackRooks:
                return gameState.BitBoards[ChessGameState.BLACK_ROOK];
            case BitboardType.WhiteQueens:
                return gameState.BitBoards[ChessGameState.WHITE_QUEEN];
            case BitboardType.BlackQueens:
                return gameState.BitBoards[ChessGameState.BLACK_QUEEN];
            case BitboardType.WhiteKing:
                return gameState.BitBoards[ChessGameState.WHITE_KING];
            case BitboardType.BlackKing:
                return gameState.BitBoards[ChessGameState.BLACK_KING];
            case BitboardType.PinnedPieces:
                return gameState.PinnedPieces;
            case BitboardType.CheckingPieces:
                return gameState.GetCheckingPieces();
            case BitboardType.CheckBlockingMask:
                return gameState.GetCheckBlockingMask();
            case BitboardType.LightSquares:
                return ChessCache.LightSquaresMask;
            case BitboardType.DarkSquares:
                return ChessCache.DarkSquaresMask;
            case BitboardType.CentralSquares:
                return ChessCache.CentralSquaresMask;
            case BitboardType.WhiteAttackMap:
                return gameState.whiteAttackMap;
            case BitboardType.BlackAttackMap:
                return gameState.blackAttackMap;
            case BitboardType.WhitePawnAttackMap:
                return gameState.whitePawnAttackMap;
            case BitboardType.BlackPawnAttackMap:
                return gameState.blackPawnAttackMap;
            case BitboardType.KingSafetyMask:
                int kingSquare = gameState.IsWhiteTurn ? gameState.WhiteKingSquare : gameState.BlackKingSquare;
                return ChessCache.KingSafetyMask[kingSquare];
            default:
                return 0UL;
        }
    }

    /// <summary>
    /// 비트보드 값을 시각화합니다.
    /// </summary>
    private void VisualizeBitboardValue(ulong bitboard)
    {
        // 모든 칸에 대해 오버레이 생성 (0과 1 모두 표시)
        for (int square = 0; square < 64; square++)
        {
            bool isSet = ((bitboard >> square) & 1UL) != 0;
            CreateBitboardOverlay(square, isSet);
        }
        
        // 비트보드 값을 콘솔에 출력 (디버그용)
        Debug.Log("비트보드 값:\n" + BitHelper.BitboardToString(bitboard));
    }

    /// <summary>
    /// 특정 위치에 비트보드 오버레이를 생성합니다.
    /// </summary>
    private void CreateBitboardOverlay(int square, bool isSet)
    {
        Vector3 position = SquareToPosition(square);
        
        // 새 오버레이 생성
        GameObject overlay = new GameObject($"BitboardOverlay_{square}");
        overlay.transform.SetParent(boardTransform);
        overlay.transform.localPosition = position;
        
        // 스프라이트 렌더러 추가
        SpriteRenderer renderer = overlay.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 20; // 기물보다 위에 표시
        
        // 스퀘어 스프라이트 생성
        renderer.sprite = CreateSquareSprite();
        
        // 색상 설정
        renderer.color = isSet ? bitOneColor : bitZeroColor;
        
        // 중앙에 텍스트 추가 (0 또는 1 표시)
        GameObject textObj = new GameObject("BitText");
        textObj.transform.SetParent(overlay.transform);
        textObj.transform.localPosition = Vector3.zero;
        
        TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.sortingOrder = 3;
        textMesh.text = isSet ? "1" : "0";
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 10;
        textMesh.color = Color.blue;
        
        // 딕셔너리에 추가
        bitboardOverlays[square] = overlay;
    }

    /// <summary>
    /// 정사각형 스프라이트를 생성합니다.
    /// </summary>
    private Sprite CreateSquareSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];
        
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    /// <summary>
    /// 모든 비트보드 오버레이를 제거합니다.
    /// </summary>
    public void ClearBitboardOverlays()
    {
        foreach (var overlay in bitboardOverlays.Values)
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
        }
        
        bitboardOverlays.Clear();
        currentBitboardType = BitboardType.None;
    }

    /// <summary>
    /// 현재 표시 중인 비트보드 타입을 반환합니다.
    /// </summary>
    public BitboardType GetCurrentBitboardType()
    {
        return currentBitboardType;
    }

    /// <summary>
    /// 비트보드 오버레이 색상을 설정합니다.
    /// </summary>
    public void SetBitboardColors(Color zeroColor, Color oneColor)
    {
        bitZeroColor = zeroColor;
        bitOneColor = oneColor;
        
        // 색상 변경 시 현재 표시 중인 오버레이도 업데이트
        foreach (var pair in bitboardOverlays)
        {
            SpriteRenderer renderer = pair.Value.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                // 현재 색상이 zeroColor에 가까우면 0, oneColor에 가까우면 1
                bool isOne = renderer.color.g > 0.5f; // 녹색 성분으로 구분 (간단하게)
                renderer.color = isOne ? bitOneColor : bitZeroColor;
            }
        }
    }

    #endregion
} 