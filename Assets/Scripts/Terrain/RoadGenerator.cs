using UnityEngine;
using UnityEngine.Tilemaps;

public class RoadGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 100;
    public int height = 100;

    [Header("Road Generation")]
    public int numberOfRoads = 5;
    public int pathLength = 100;
    public int roadRadius = 1;
    [Range(0f, 1f)] public float straightChance = 0.6f;

    [Header("Building Generation")]
    public int numberOfBuildings = 20;
    public Vector2Int buildingSize = new Vector2Int(3, 3);

    [Header("Tile References")]
    public Tilemap groundTilemap;
    public TileBase grassTile;
    public TileBase roadTile;
    public TileBase buildingTile;

    [Header("Seed")]
    public int seed;
    private bool useRandomSeed = true;

    // 0 = grass, 1 = road, 2 = building
    private int[,] mapData;

    void Start()
    {
        if (seed == 0)
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
            useRandomSeed = true;
        }
        Random.InitState(seed);

        GenerateMap();
        GenerateRoads();
        GenerateBuildings();
        RenderMap();
    }

    [ContextMenu("Generate New Map")]
    void EditorGenerateMap()
    {
        if (useRandomSeed)
        {
            seed = Random.Range(int.MinValue, int.MaxValue);
        }
        Random.InitState(seed);

        GenerateMap();
        GenerateRoads();
        GenerateBuildings();
        RenderMap();
    }

    void GenerateMap()
    {
        mapData = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                mapData[x, y] = 0;
            }
        }
    }

    void GenerateRoads()
    {
        for (int i = 0; i < numberOfRoads; i++)
        {
            Vector2Int startPos = new Vector2Int(
                Random.Range(roadRadius + 1, width - roadRadius - 1),
                Random.Range(roadRadius + 1, height - roadRadius - 1)
            );
            Vector2Int currentDir = GetRandomCardinalDirection();

            GenerateDrunkardRoad(startPos, currentDir, pathLength);
        }
    }

void GenerateDrunkardRoad(Vector2Int startPos, Vector2Int initialDir, int length)
{
    Vector2Int currentPos = startPos;
    Vector2Int currentDir = initialDir;

    int stepsRemaining = length;

    while (stepsRemaining > 0)
    {
        int segmentLength = Random.Range(3, 10); // chunk of straight steps
        segmentLength = Mathf.Min(segmentLength, stepsRemaining);

        for (int i = 0; i < segmentLength; i++)
        {
            SetRoadTiles(currentPos, roadRadius);
            currentPos += currentDir;

            // Clamp position to stay within bounds
            currentPos.x = Mathf.Clamp(currentPos.x, roadRadius, width - 1 - roadRadius);
            currentPos.y = Mathf.Clamp(currentPos.y, roadRadius, height - 1 - roadRadius);

            stepsRemaining--;

            // Early exit if we reach the edge
            if (currentPos.x == roadRadius || currentPos.x == width - 1 - roadRadius ||
                currentPos.y == roadRadius || currentPos.y == height - 1 - roadRadius)
                return;
        }

        // Decide if we should turn after a segment
        float turnChance = Random.value;
        if (turnChance > straightChance)
        {
            if (Random.value < 0.5f)
                currentDir = TurnLeft(currentDir);
            else
                currentDir = TurnRight(currentDir);
        }
    }
}


    void SetRoadTiles(Vector2Int center, int radius)
{
    radius = Mathf.Max(radius, 1);

    for (int dx = -radius; dx < radius; dx++) 
    {
        for (int dy = -radius; dy < radius; dy++)
        {
            int tileX = center.x + dx;
            int tileY = center.y + dy;

            if (tileX >= 0 && tileX < width && tileY >= 0 && tileY < height)
            {
                mapData[tileX, tileY] = 1;
            }
        }
    }
}


void GenerateBuildings()
{
    int attempts = 0;
    int placed = 0;

    while (placed < numberOfBuildings && attempts < numberOfBuildings * 10)
    {
        attempts++;

        // Generate random building size
        int widthSize = Random.Range(4, 11);
        int heightSize = Random.Range(4, 11);
        Vector2Int size = new Vector2Int(widthSize, heightSize);

        // Pick a valid position
        Vector2Int pos = new Vector2Int(
            Random.Range(1, width - size.x - 1),
            Random.Range(1, height - size.y - 1)
        );

        if (CanPlaceBuilding(pos, size))
        {
            PlaceBuilding(pos, size);
            placed++;
        }
    }

    Debug.Log($"Buildings Placed: {placed}/{numberOfBuildings}");
}

bool CanPlaceBuilding(Vector2Int pos, Vector2Int size)
{
    bool nearRoad = false;

    for (int x = 0; x < size.x; x++)
    {
        for (int y = 0; y < size.y; y++)
        {
            int checkX = pos.x + x;
            int checkY = pos.y + y;

            if (mapData[checkX, checkY] != 0)
            {
                Debug.Log($"Blocked: {checkX}, {checkY}");
                return false;
            }

            if (!nearRoad && IsNearRoad(new Vector2Int(checkX, checkY)))
                nearRoad = true;
        }
    }

    if (!nearRoad)
    {
        Debug.Log($"No road near: {pos}");
        return false;
    }

    return true;
}



    void PlaceBuilding(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                mapData[pos.x + x, pos.y + y] = 2;
            }
        }
    }

    bool IsNearRoad(Vector2Int pos)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                int nx = pos.x + dx;
                int ny = pos.y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (mapData[nx, ny] == 1)
                        return true;
                }
            }
        }
        return false;
    }

    void RenderMap()
    {
        groundTilemap.ClearAllTiles();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (mapData[x, y] == 0)
                {
                    groundTilemap.SetTile(tilePos, grassTile);
                }
                else if (mapData[x, y] == 1)
                {
                    groundTilemap.SetTile(tilePos, roadTile);
                }
                else if (mapData[x, y] == 2)
                {
                    groundTilemap.SetTile(tilePos, buildingTile);
                }
            }
        }
    }

    Vector2Int GetRandomCardinalDirection()
    {
        Vector2Int[] cardinalDirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        return cardinalDirs[Random.Range(0, cardinalDirs.Length)];
    }

    Vector2Int TurnLeft(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.left;
        if (dir == Vector2Int.left) return Vector2Int.down;
        if (dir == Vector2Int.down) return Vector2Int.right;
        if (dir == Vector2Int.right) return Vector2Int.up;
        return Vector2Int.zero;
    }

    Vector2Int TurnRight(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.right;
        if (dir == Vector2Int.right) return Vector2Int.down;
        if (dir == Vector2Int.down) return Vector2Int.left;
        if (dir == Vector2Int.left) return Vector2Int.up;
        return Vector2Int.zero;
    }
}
