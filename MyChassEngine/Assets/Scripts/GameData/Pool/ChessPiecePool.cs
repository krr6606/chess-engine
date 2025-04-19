using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessPiecePool : MonoBehaviour
{
    [System.Serializable]
    public class PieceType
    {
        public GameObject whitePrefab; // 백색 기물 프리팹
        public GameObject blackPrefab; // 흑색 기물 프리팹
        public int initialPoolSize = 2;
        public Transform poolParent;
    }
    
    [Header("기물 프리팹")]
    [SerializeField] private PieceType pawnPrefab;
    [SerializeField] private PieceType knightPrefab;
    [SerializeField] private PieceType bishopPrefab;
    [SerializeField] private PieceType rookPrefab;
    [SerializeField] private PieceType queenPrefab;
    [SerializeField] private PieceType kingPrefab;
    
    [Header("풀 설정")]
    [SerializeField] private Transform poolContainer;
    [SerializeField] private int extraPoolSize = 5;
    
    // 기물 풀
    private Dictionary<int, Queue<GameObject>> piecePools = new Dictionary<int, Queue<GameObject>>();
    
    private void Awake()
    {
        // 풀 컨테이너 생성
        if (poolContainer == null)
        {
            GameObject container = new GameObject("PiecePool");
            container.transform.SetParent(transform);
            poolContainer = container.transform;
        }
        
        // 각 기물 타입별 풀 초기화
        InitializePool(pieceNum.pwan, pawnPrefab);
        InitializePool(pieceNum.knight, knightPrefab);
        InitializePool(pieceNum.bishop, bishopPrefab);
        InitializePool(pieceNum.rook, rookPrefab);
        InitializePool(pieceNum.queen, queenPrefab);
        InitializePool(pieceNum.king, kingPrefab);
    }
    
    // 특정 타입의 풀 초기화
    private void InitializePool(int pieceType, PieceType prefabConfig)
    {
        if (prefabConfig == null)
        {
            Debug.LogWarning($"기물 타입 {pieceType}의 프리팹이 설정되지 않았습니다.");
            return;
        }
        
        // 풀 부모 생성
        Transform poolParent = prefabConfig.poolParent;
        if (poolParent == null)
        {
            GameObject parentObj = new GameObject($"Pool_{pieceType}");
            parentObj.transform.SetParent(poolContainer);
            poolParent = parentObj.transform;
            prefabConfig.poolParent = poolParent;
        }
        
        // 백색 기물 풀 초기화 (백색 프리팹이 있는 경우)
        if (prefabConfig.whitePrefab != null)
        {
            int whiteKey = pieceNum.white | pieceType;
            piecePools[whiteKey] = new Queue<GameObject>();
            
            for (int i = 0; i < prefabConfig.initialPoolSize; i++)
            {
                CreateNewPiece(pieceType, true, poolParent, prefabConfig);
            }
        }
        else
        {
            Debug.LogWarning($"기물 타입 {pieceType}의 백색 프리팹이 설정되지 않았습니다.");
        }
        
        // 흑색 기물 풀 초기화 (흑색 프리팹이 있는 경우)
        if (prefabConfig.blackPrefab != null)
        {
            int blackKey = pieceNum.black | pieceType;
            piecePools[blackKey] = new Queue<GameObject>();
            
            for (int i = 0; i < prefabConfig.initialPoolSize; i++)
            {
                CreateNewPiece(pieceType, false, poolParent, prefabConfig);
            }
        }
        else
        {
            Debug.LogWarning($"기물 타입 {pieceType}의 흑색 프리팹이 설정되지 않았습니다.");
        }
    }
    
    // 새 기물 생성 및 풀에 추가
    private GameObject CreateNewPiece(int pieceType, bool isWhite, Transform parent, PieceType prefabConfig)
    {
        // 색상에 맞는 프리팹 선택
        GameObject prefab = isWhite ? prefabConfig.whitePrefab : prefabConfig.blackPrefab;
        if (prefab == null) return null;
        
        GameObject newPiece = Instantiate(prefab, parent);
        newPiece.name = $"{(isWhite ? "White" : "Black")}_{pieceType}_{Random.Range(1000, 9999)}";
        newPiece.SetActive(false);
        
        // ChessPiece 컴포넌트 추가
        ChessPiece chessPiece = newPiece.GetComponent<ChessPiece>();
        if (chessPiece == null)
        {
            chessPiece = newPiece.AddComponent<ChessPiece>();
        }
        chessPiece.PieceType = pieceType;
        chessPiece.IsWhite = isWhite;
        chessPiece.Square = -1;
        
        // 풀에 추가
        int key = (isWhite ? pieceNum.white : pieceNum.black) | pieceType;
        piecePools[key].Enqueue(newPiece);
        
        return newPiece;
    }
    
    // 풀에서 기물 가져오기
    public GameObject GetPiece(int pieceType, bool isWhite)
    {
        int key = (isWhite ? pieceNum.white : pieceNum.black) | pieceType;
        
        // 해당 타입의 풀이 없으면 생성
        if (!piecePools.ContainsKey(key))
        {
            PieceType prefabConfig = GetPrefabConfig(pieceType);
            if (prefabConfig != null)
            {
                // 해당 색상의 프리팹이 있는지 확인
                GameObject prefab = isWhite ? prefabConfig.whitePrefab : prefabConfig.blackPrefab;
                if (prefab == null)
                {
                    Debug.LogError($"기물 타입 {pieceType}의 {(isWhite ? "백" : "흑")}색 프리팹이 없습니다.");
                    return null;
                }
                
                piecePools[key] = new Queue<GameObject>();
                
                // 풀 부모 생성
                Transform poolParent = prefabConfig.poolParent;
                if (poolParent == null)
                {
                    GameObject parentObj = new GameObject($"Pool_{pieceType}");
                    parentObj.transform.SetParent(poolContainer);
                    poolParent = parentObj.transform;
                    prefabConfig.poolParent = poolParent;
                }
            }
            else
            {
                Debug.LogError($"알 수 없는 기물 타입: {pieceType}");
                return null;
            }
        }
        
        // 풀에 기물이 없으면 추가 생성
        if (piecePools[key].Count == 0)
        {
            PieceType prefabConfig = GetPrefabConfig(pieceType);
            if (prefabConfig != null)
            {
                // 해당 색상의 프리팹이 있는지 확인
                GameObject prefab = isWhite ? prefabConfig.whitePrefab : prefabConfig.blackPrefab;
                if (prefab != null)
                {
                    for (int i = 0; i < extraPoolSize; i++)
                    {
                        CreateNewPiece(pieceType, isWhite, prefabConfig.poolParent, prefabConfig);
                    }
                }
                else
                {
                    Debug.LogError($"기물 타입 {pieceType}의 {(isWhite ? "백" : "흑")}색 프리팹이 없습니다.");
                    return null;
                }
            }
        }
        
        // 풀에서 기물 가져오기
        if (piecePools[key].Count > 0)
        {
            GameObject piece = piecePools[key].Dequeue();
            piece.SetActive(true);
            
            // ChessPiece 컴포넌트 확인 및 업데이트
            ChessPiece chessPiece = piece.GetComponent<ChessPiece>();
            if (chessPiece == null)
            {
                chessPiece = piece.AddComponent<ChessPiece>();
            }
            chessPiece.PieceType = pieceType;
            chessPiece.IsWhite = isWhite;
            
            return piece;
        }
        
        Debug.LogError($"기물을 가져오지 못했습니다: {pieceType}, {(isWhite ? "백" : "흑")}");
        return null;
    }
    
    // 기물을 풀로 반환
    public void ReturnPiece(GameObject piece, int pieceType, bool isWhite)
    {
        if (piece == null) return;
        
        // 기물 비활성화 및 초기화
        piece.SetActive(false);
        piece.transform.SetParent(GetPoolParent(pieceType));
        
        // ChessPiece 컴포넌트 리셋
        ChessPiece chessPiece = piece.GetComponent<ChessPiece>();
        if (chessPiece != null)
        {
            chessPiece.Reset();
        }
        
        // 풀에 반환
        int key = (isWhite ? pieceNum.white : pieceNum.black) | pieceType;
        if (!piecePools.ContainsKey(key))
        {
            piecePools[key] = new Queue<GameObject>();
        }
        piecePools[key].Enqueue(piece);
    }
    
    // 모든 기물 회수
    public void ReturnAllPieces()
    {
        ChessPiece[] activePieces = FindObjectsOfType<ChessPiece>();
        foreach (ChessPiece piece in activePieces)
        {
            ReturnPiece(piece.gameObject, piece.PieceType, piece.IsWhite);
        }
    }
    
    // 특정 기물 타입의 프리팹 설정 가져오기
    private PieceType GetPrefabConfig(int pieceType)
    {
        if ((pieceType & pieceNum.pwan) != 0) return pawnPrefab;
        if ((pieceType & pieceNum.knight) != 0) return knightPrefab;
        if ((pieceType & pieceNum.bishop) != 0) return bishopPrefab;
        if ((pieceType & pieceNum.rook) != 0) return rookPrefab;
        if ((pieceType & pieceNum.queen) != 0) return queenPrefab;
        if ((pieceType & pieceNum.king) != 0) return kingPrefab;
        
        return null;
    }
    
    // 특정 기물 타입의 풀 부모 가져오기
    private Transform GetPoolParent(int pieceType)
    {
        PieceType prefabConfig = GetPrefabConfig(pieceType);
        if (prefabConfig != null && prefabConfig.poolParent != null)
        {
            return prefabConfig.poolParent;
        }
        
        return poolContainer;
    }
} 