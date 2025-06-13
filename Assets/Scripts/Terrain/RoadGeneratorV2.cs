using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System; 

public class RoadGeneratorV2 : MonoBehaviour
{   
    [Header("Map Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    [Range(0.1f, 100f)]
    public float mapScale = 1f;

    [Header("A* Road Generation")]
    public int numRoads = 50; 
    public int initialRoadLength = 5; 
    public float gCostMultiplier = 1f; 

    [Header("Tile References")]
    public Tilemap roadTilemap;
    public TileBase fillerTile;
    public TileBase[] roadTiles;


    // mapData where 0 = filler tiles, 1 = road tiles
    private int[,] mapData;

    // store all pathNodes
    private PathNode[,] pathNodes;

    // Node class
    private class PathNode : IHeapItem<PathNode>
    {       
        public Vector2Int position;
        public int gridX; 
        public int gridY;

        public PathNode parent;

        public int gCost; 
        public int hCost; 
        public int fCost { get { return gCost + hCost; } }

        int heapIndex;
        public int HeapIndex
        {
            get { return heapIndex; }
            set { heapIndex = value; }
        }

        // Constructor
        public PathNode(Vector2Int pos, int x, int y)
        {
            position = pos;
            gridX = x;
            gridY = y;

            parent = null;
            gCost = 0;
            hCost = 0;
            heapIndex = 0; 
        }

        public int CompareTo(PathNode other)
        {
            int compare = fCost.CompareTo(other.fCost);
            if (compare == 0)
            {
                compare = hCost.CompareTo(other.hCost);
            }
            return -compare; 
        }

        // Reset node data before each new pathfinding run
        public void ResetNodeData()
        {
            gCost = 0;
            hCost = 0;
            parent = null;
            heapIndex = 0; 
        }
    }

    void Start()
    {   
        InitializeRandomState();
        InitializeMap();
        GenerateRoads();
        RenderMap();
    }

    // Generate a random seed for the world
    void InitializeRandomState()
    {
        UnityEngine.Random.InitState(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
    }

    // Initialize the map with filler tiles
    void InitializeMap()
    {
        mapData = new int[mapWidth, mapHeight];
        pathNodes = new PathNode[mapWidth, mapHeight]; 

        // Generate map
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {   
                // Set map data with filler tiles (0 is filler, 1 is road tile)
                mapData[x, y] = 0; 

                // Create new pathnode
                pathNodes[x, y] = new PathNode(new Vector2Int(x, y), x, y); 
            }
        }
    }

    // Generate the road
    void GenerateRoads()
    {    
        // Generate a random start road for A* to work
        CreateInitialRoad();

        for (int i = 0; i < numRoads; i++) {
            Vector2Int startPoint = GetRandomUnroadedTile();
            if (startPoint == Vector2Int.left * 999)
            {
                break;
            }

            List<Vector2Int> path = FindPathAStarToAnyRoad(startPoint);
            if (path != null && path.Count > 0)
            {
                AddPathToMapData(path);
            }
        }
    }

    // Generates an initial road
    void CreateInitialRoad()
    {
        Vector2Int startPos = new Vector2Int(mapWidth / 2, mapHeight / 2);
        Vector2Int dir = GetRandomDirection();

        for (int i = 0; i < initialRoadLength; i++)
        {   
            // If tile is not in bound, break
            if (!IsInBounds(startPos)) {
                break;
            }
            SetRoadTile(startPos);
            startPos += dir;
        }
    }

    /// Finds a random tile that is currently not part of a road.
    Vector2Int GetRandomUnroadedTile()
    {
        for (int i = 0; i < 100; i++)
        {
            int randX = UnityEngine.Random.Range(1, mapWidth - 1);
            int randY = UnityEngine.Random.Range(1, mapHeight - 1);
            Vector2Int pos = new Vector2Int(randX, randY);

            if (!IsRoad(pos.x, pos.y))
            {
                return pos;
            }
        }
        return Vector2Int.left * 999;
    }

    /// Adds a path to mapData
    void AddPathToMapData(List<Vector2Int> path)
    {
        foreach (Vector2Int pos in path) {
            SetRoadTile(pos);
        }
    }

    // A* Path to find an existing road
    List<Vector2Int> FindPathAStarToAnyRoad(Vector2Int startPos)
    {
        // Reset all nodes first
        for (int x = 0; x < mapWidth; x++) {
            for (int y = 0; y < mapHeight; y++) {
                pathNodes[x, y].ResetNodeData();
            }
        }

        // Create open and closed set
        Heap<PathNode> openSet = new Heap<PathNode>(mapWidth * mapHeight);
        HashSet<PathNode> closedSet = new HashSet<PathNode>(); 

        PathNode startNode = pathNodes[startPos.x, startPos.y];
        startNode.parent = startNode; 
        openSet.Add(startNode);

        // Stop if we have a path or run out of nodes to explore (means no path can be found!)
        while (openSet.Count > 0)
        {
            PathNode currentNode = openSet.RemoveFirst();
            closedSet.Add(currentNode);

            // Check if the currentnode hits an existing road tile, if so reconstruct path and end the A* loop
            if (IsRoad(currentNode.position.x, currentNode.position.y) && currentNode != startNode)
            {
                return ReconstructPath(currentNode);
            }

            // Get neighbors
            Vector2Int[] neighbours = GetNeighbours();
            foreach (Vector2Int neigh in neighbours)
            {
                Vector2Int neighborPos = currentNode.position + neigh;
                if (!IsInBounds(neighborPos))
                {
                    continue;
                }

                PathNode neighborNode = pathNodes[neighborPos.x, neighborPos.y];

                // Continue if neighboring node is in closed set
                if (closedSet.Contains(neighborNode))
                {
                    continue;
                }

                // calculate gcost with heuristic (getdistance in thsi case)
                int newGCost = currentNode.gCost + GetDistance(currentNode, neighborNode);

                //If the neighbour is not in the open set OR
                //the neighour was previously checked and the new G Cost is less than the previous G Cost 
                if (!openSet.Contains(neighborNode) || newGCost < neighborNode.gCost)
                {
                    neighborNode.gCost = newGCost;
                    neighborNode.parent = currentNode;
                    neighborNode.hCost = 0; 

                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Add(neighborNode);
                    }
                    else
                    {
                        openSet.UpdateItem(neighborNode); 
                    }
                }
            }
        }

        // No path found
        return null;
    }

    /// Reconstructs the path from the end node back to the start node.
    List<Vector2Int> ReconstructPath(PathNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = endNode;

        // stop looping when current node is the start node
        while (currentNode != null && currentNode.parent != currentNode) 
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        path.Add(currentNode.position);
 
        // Reverse the path to reconstruct it from the start
        path.Reverse();
        return path;
    }

    // Sets a tile on the map as 1 (has a tile)
    void SetRoadTile(Vector2Int pos)
    {
        if (pos.x >= 0 && pos.x < mapWidth && pos.y >= 0 && pos.y < mapHeight) {
            mapData[pos.x, pos.y] = 1;
        }
    }

    void RenderMap()
{   
    // Clear all tiles first
    roadTilemap.ClearAllTiles();
    roadTilemap.transform.localScale = new Vector3(mapScale, mapScale, 1f);

    // adjust tilemap so that its in the middle of the world
    Vector3 offset = new Vector3(mapWidth * mapScale / 2f, mapHeight * mapScale / 2f, 0f);
    roadTilemap.transform.position = new Vector3(-offset.x, -offset.y, 0);

    // Render the tiles
    for (int x = 0; x < mapWidth; x++) {
        for (int y = 0; y < mapHeight; y++) {

            // Generate the tiles if 1 on map data (0 is filler)
            Vector3Int tilePos = new Vector3Int(x, y, 0);
            if (mapData[x, y] == 1) {
                int index = GetRoadTileIndex(x, y);
                roadTilemap.SetTile(tilePos, roadTiles[index]);
            }
            else {
                roadTilemap.SetTile(tilePos, fillerTile);
            }
        }
    }
}

    // Checks the current road tile (eg if its intersection, horizontal vertical etc)
    int GetRoadTileIndex(int currX, int currY)
    {    
        // Check all directions if road exists
        bool up = IsRoad(currX, currY + 1);
        bool down = IsRoad(currX, currY - 1);
        bool left = IsRoad(currX - 1, currY);
        bool right = IsRoad(currX + 1, currY);

        // 4 way intersection
        if (up && down && left && right) return 10;
        // T-down
        if (down && left && right && !up) return 7;
        // T-Up
        if (up && left && right && !down) return 6;
        //T-Left
        if (up && down && right && !left) return 9;
        //T-right
        if (up && down && left && !right) return 8;
        // Horizontal
        if (left && right && !up && !down) return 0;
        // Vertical
        if (up && down && !left && !right) return 1;
        // Corner TL
        if (up && left && !down && !right) return 2;
        // Corner TR
        if (up && right && !down && !left) return 3;
        // Corner BL
        if (down && left && !up && !right) return 4;
        // Corner BR
        if (down && right && !up && !left) return 5;

        // No roads (filler tile)
        return 11; 
    }

    // Checks if a given tile coordinate is within map bounds and its a road on mapData
    bool IsRoad(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight && mapData[x, y] == 1;
    }

    // returns a random direction
    Vector2Int GetRandomDirection()
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        return directions[UnityEngine.Random.Range(0, directions.Length)];
    }

    /// Checks if a single tile position is within map bounds.
    bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapWidth && pos.y >= 0 && pos.y < mapHeight;
    }

    /// Check neighbours 
    private Vector2Int[] GetNeighbours()
    {
        return new Vector2Int[] {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
    }

    private int GetDistance(PathNode nodeA, PathNode nodeB)
    {       
        int dx = Math.Abs(nodeA.gridX - nodeB.gridX);
        int dy = Math.Abs(nodeA.gridY - nodeB.gridY);

        return 10 * (dx + dy);
    }
}