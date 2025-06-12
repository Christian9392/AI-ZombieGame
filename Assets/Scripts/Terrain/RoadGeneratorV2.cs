using UnityEngine;
using UnityEngine.Tilemaps;

public class RoadGeneratorV2 : MonoBehaviour
{
    [Header("Map Settings")]
    public int width = 100;
    public int height = 100;
    [Range(0.1f, 100f)] // Allow scaling from smaller than 1 unit to 100 units per tile
    public float mapScale = 1f; // <--- NEW: Scale factor for the entire map

    [Header("Road Generation")]
    public int numberOfRoads = 5;
    public int pathLength = 100;
    public int roadRadius = 1;
    [Range(0f, 1f)] public float straightChance = 0.7f;

    [Header("Tile References")]
    public Tilemap roadTilemap;
    public TileBase fillerTile;
    public TileBase[] roadTiles;

    [Header("Seed")]
    public int seed;
    private bool useRandomSeed = true;

    // Map data: 0 = filler, 1 = road
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
        RenderMap();
    }

    void GenerateMap()
    {
        mapData = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                mapData[x, y] = 0;
    }

    void GenerateRoads()
    {
        for (int i = 0; i < numberOfRoads; i++)
        {
            Vector2Int startPos = new Vector2Int(
                Random.Range(roadRadius + 1, width - roadRadius - 1),
                Random.Range(roadRadius + 1, height - roadRadius - 1)
            );
            Vector2Int dir = GetRandomDirection();

            CarveRoad(startPos, dir, pathLength);
        }
    }

    void CarveRoad(Vector2Int pos, Vector2Int dir, int length)
    {
        for (int i = 0; i < length; i++)
        {
            SetRoadTiles(pos, roadRadius);

            if (Random.value > straightChance)
                dir = Random.value > 0.5f ? TurnLeft(dir) : TurnRight(dir);

            pos += dir;

            if (!IsInBoundsWithRadius(pos, roadRadius))
                break;
        }
    }

    void SetRoadTiles(Vector2Int center, int radius)
    {
        radius = Mathf.Max(0, radius); // Ensure radius is non-negative

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
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

    void RenderMap()
    {
        roadTilemap.ClearAllTiles();

        // <--- NEW: Apply the map scale to the Tilemap GameObject's transform ---
        roadTilemap.transform.localScale = new Vector3(mapScale, mapScale, 1f);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                if (mapData[x, y] == 1)
                {
                    int index = GetRoadTileIndex(x, y);
                    roadTilemap.SetTile(tilePos, roadTiles[index]);
                }
                else
                {
                    roadTilemap.SetTile(tilePos, fillerTile);
                }
            }
        }
    }

    int GetRoadTileIndex(int currX, int currY)
    {
        bool up = IsRoad(currX, currY + 1);
        bool down = IsRoad(currX, currY - 1);
        bool left = IsRoad(currX - 1, currY);
        bool right = IsRoad(currX + 1, currY);

        // 4-way Intersection
        if (up && down && left && right) return 10;

        // T-Junctions
        if (down && left && right && !up) return 6;
        if (up && left && right && !down) return 7;
        if (up && down && right && !left) return 8;
        if (up && down && left && !right) return 9;

        // Straights
        if (left && right && !up && !down) return 0;
        if (up && down && !left && !right) return 1;

        // Corners
        if (up && left && !down && !right) return 2;
        if (up && right && !down && !left) return 3;
        if (down && left && !up && !right) return 4;
        if (down && right && !up && !left) return 5;

        // Dead End / Isolated Road
        return 11;
    }

    bool IsRoad(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height && mapData[x, y] == 1;
    }

    Vector2Int GetRandomDirection()
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        return directions[Random.Range(0, directions.Length)];
    }

    Vector2Int TurnLeft(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.left;
        if (dir == Vector2Int.left) return Vector2Int.down;
        if (dir == Vector2Int.down) return Vector2Int.right;
        if (dir == Vector2Int.right) return Vector2Int.up;
        return dir;
    }

    Vector2Int TurnRight(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.right;
        if (dir == Vector2Int.right) return Vector2Int.down;
        if (dir == Vector2Int.down) return Vector2Int.left;
        if (dir == Vector2Int.left) return Vector2Int.up;
        return dir;
    }

    bool IsInBoundsWithRadius(Vector2Int pos, int radius)
    {
        return (pos.x - radius) >= 0 && (pos.x + radius) < width &&
               (pos.y - radius) >= 0 && (pos.y + radius) < height;
    }

    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 1 && pos.x < width - 1 && pos.y >= 1 && pos.y < height - 1;
    }
}