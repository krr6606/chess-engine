using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    private static BoardGenerator instance;
    public static BoardGenerator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BoardGenerator>();
                if (instance == null)
                {
                    GameObject go = new GameObject("BoardGenerator");
                    instance = go.AddComponent<BoardGenerator>();
                }
            }
            return instance;
        }
    }

    int[,] tiles;    
    public Dictionary<Vector2Int, GameObject> tileDict;

    public GameObject tilePrefab;
    public int width = 8;
    public int height = 8;

    public static string TileColor = "#CA8743";
    public static string TileColor2 = "#F7C99A";

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        tiles = new int[width, height];
        tileDict = new Dictionary<Vector2Int, GameObject>();
    }
    void Start()
    {
        GenerateBoard();

    }

    public void GenerateBoard()
    {
        for (int x = 1; x < width+1; x++)
        {
            for (int y = 1; y < height+1; y++)
            {
                GameObject tile = Instantiate(tilePrefab, new Vector3(x-width/2 -0.5f, y-height/2 -0.5f, 0), Quaternion.identity);
                tile.transform.parent = transform;
                tile.name = "Tile (" + (x) + ", " + (y) + ")";
                tile.GetComponent<SpriteRenderer>().color = (x+y) % 2 == 0 ? HexColor(TileColor) : HexColor(TileColor2);
                tileDict.Add(new Vector2Int(x, y), tile);
            }
        }
    }
            // 헥사값 컬러 반환( 코드 순서 : RGBA )
        public static Color HexColor( string hexCode )
        {
            Color color;
            if ( ColorUtility.TryParseHtmlString( hexCode, out color ) )
            {
                return color;
            }
            
            Debug.LogError( "[UnityExtension::HexColor]invalid hex code - " + hexCode );
            return Color.white;
        }
}