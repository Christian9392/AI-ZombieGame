// Adapted from: https://github.com/SebLague/Pathfinding-2D

using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor.Experimental.GraphView;

public class Pathfinding : MonoBehaviour
{
    public static AStarGrid grid;
    static Pathfinding instance;

    public enum DistanceType {
        Manhattan,
        Euclidean,
        Chebyshev,
        Octile,
        Custom
    }

    // public DistanceType distanceType;

    // Rate at which g cost is multiplied
    public float gCostMultiplier;

    // Enable path smoothing
    public bool enablePathSmoothing;

    // This is used instead of Start() so that the
    // A* grid is only greated once when the game is launched
    void Awake()
    {
        grid = GetComponent<AStarGrid>();
        instance = this;
    }

    // Public callable method
    public static Node[] RequestPath(Vector2 from, Vector2 to)
    {   
        return instance.FindPath(from, to);
    }

    static List<Node> dynamicObstacleNodes = new List<Node>();

    public static void UpdateObstaclesExternally()
    {
        foreach (Node node in grid.GetAllNodes())
        {
            node.walkable = Node.Walkable.Walkable;

            Collider2D unwalk = Physics2D.OverlapCircle(node.worldPosition, grid.overlapCircleRadius, grid.unwalkableMask);
            Collider2D slow = Physics2D.OverlapCircle(node.worldPosition, grid.overlapCircleRadius, grid.slowMask);

            if (unwalk != null)
                node.walkable = Node.Walkable.Obstacle;
            else if (slow != null)
                node.walkable = Node.Walkable.Slow;
        }

        dynamicObstacleNodes.Clear();
        Block[] blocks = GameObject.FindObjectsOfType<Block>();
        int width = 2;    
        int height = 5;   
        foreach (Block block in blocks)
        {
            Vector2 center = block.transform.position;

            for (int dx = -width / 2; dx <= width / 2; dx++)
            {
                for (int dy = -height / 2; dy <= height / 2; dy++)
                {
                    Vector2 offset = new Vector2(dx * grid.gridSize * 2, dy * grid.gridSize * 2);
                    Vector2 positionToBlock = center + offset;

                    Node node = grid.NodeFromWorldPoint(positionToBlock);
                    if (node != null)
                    {
                        node.walkable = Node.Walkable.Obstacle;
                        dynamicObstacleNodes.Add(node);

                        Debug.DrawRay(node.worldPosition, Vector2.up * 0.2f, Color.magenta, 0.1f);
                    }
                }
            }
        }
    }

    // Internal private implementation
    Node[] FindPath(Vector2 from, Vector2 to)
    {   
        // A* Waypoints to return
        Node[] waypoints = new Node[0];

        // Set to true if a path is found
        bool pathSuccess = false;

        bool isNewPath = true;

        // Starting node point - selected from the A* Grid
        Node startNode = grid.NodeFromWorldPoint(from);

        // Goal node point - selected from the A* Grid
        Node targetNode = grid.NodeFromWorldPoint(to);

        // Ensure the starting node's parent is not null
        // Also let's us detect the start node if needed
        startNode.parent = startNode;

        // Niceity check to ensure the start and target nodes are walkable
        // by the frog (such as if you clock on a object)
        // If not, we find the closest walkable point in the grid
        if (startNode.walkable == Node.Walkable.Obstacle)
        {
            startNode = grid.ClosestWalkableNode(startNode);
        }
        if (targetNode.walkable == Node.Walkable.Obstacle)
        {
            targetNode = grid.ClosestWalkableNode(targetNode);
        }

        if ((startNode.walkable != Node.Walkable.Obstacle) && (targetNode.walkable != Node.Walkable.Obstacle))
        {
            // A* Starts here!!!
            // TODO: Your job is to fill in the missing code below the marked comments

            // Track the open set of nodes to explore, as a heap sorted by the A* Cost
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);

            // Track closed set of all visited nodes
            HashSet<Node> closedSet = new HashSet<Node>();
            
            // Reset all nodes if a new path is made
            if (isNewPath) {
                foreach (Node node in grid.GetAllNodes())
                {
                    node.ResetNodeData();
                }
                isNewPath = false;
            }

            // TODO: Commence A* by adding the start node to the open set
            openSet.Add(startNode);
            

            // Stop if we have a path or run out of nodes to explore (means no path can be found!)
            while (!pathSuccess && openSet.Count > 0)
            {
                // TODO: Get the node with the lowest F cost from the open set
                //     and add it to the closed set
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                // TODO: If we have reached the target node, we have found a path! (repalce false)
                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                }
                else
                {
                    // TODO: Otherwise, explore the neighbours of the current node
                    //       You'll need to get all of the neighbours of the current node
                    //       and then loop through them to find the best path
                    List<Node> neighbours = grid.GetNeighbours(currentNode);
                    foreach (Node node in neighbours)
                    {
                        // TODO:If we can reach the neighbour and it is not in the closed set (repalce false)
                        if (!closedSet.Contains(node) && (node.walkable != Node.Walkable.Obstacle))
                        {
                            float gCost = GCost(currentNode, node);

                            

                            // TODO: Calculate the G Cost of the neighbour node

                            // Debug.Log(gCost); 


                            // TOSO: If the neighbour is not in the open set OR
                            //    the neighour was previously checked and the new G Cost is less than the previous G Cost 
                            //    (repalce false)
                            if (!openSet.Contains(node) || gCost < node.gCost)
                            {
                                // TODO: Set neightbour G Cost
                                node.gCost = gCost;
                                

                                // TODO: Compute and set the H Cost for the neighbour
                                node.hCost = Heuristic(node, targetNode);
                                

                                // TODO: Set the parent of the neighbour to the current node
                                node.parent = currentNode;
                                

                                // TODO: Add neighbour to the open set, but need to check if the neighbour is already in the open set
                                // If not in the open set, then add to the heap
                                // If in the open set, then UDPATE the neighbour in the heap
                                if (!openSet.Contains(node))
                                {
                                    // TODO: (see above comment)
                                    openSet.Add(node);
                                }
                                else
                                {
                                    // TODO: (see above comment)
                                    openSet.UpdateItem(node);
                                }
                            }
                        }
                    }
                }
            }
        }

        // If we have a path, then actually get the path from the start to goal
        if (pathSuccess)
        {
            Node[] rawPath = RetracePath(startNode, targetNode);

            if (enablePathSmoothing) {
                waypoints = SmoothPath(rawPath);
            }
            else {
                waypoints = rawPath;
            }

        }

        return waypoints;
    }

    // Creates the actual A* Path from the start to the goal
    // TODO: Your job is to fill in the missing code below the marked comments
    Node[] RetracePath(Node startNode, Node endNode)
    {
        // Store the computed path
        List<Node> path = new List<Node>();

        // TODO: Commence retracing the path from the end node
        Node currentNode = endNode;
        

        // TODO: Loop while the current node isn't the start node (replace false)
        while (currentNode != startNode)
        {
            // TODO: Add the current node to the path
            path.Add(currentNode);
            

            // TODO: Set the current node to the parent of the current node
            currentNode = currentNode.parent;
            
        }

        // Convert this list to an array and reverse it
        Node[] waypoints = path.ToArray();
        Array.Reverse(waypoints);
        return waypoints;
    }

    Node[] SmoothPath(Node[] unsmoothedPath)
    {
        if (unsmoothedPath == null || unsmoothedPath.Length <= 2)
            return unsmoothedPath;

        List<Node> smoothedPath = new List<Node>();
        smoothedPath.Add(unsmoothedPath[0]);

        int anchorIndex = 0;

        float radius = 0.25f;
        GameObject frog = GameObject.Find("Frog");
        if (frog != null)
        {
            CircleCollider2D frogCollider = frog.GetComponent<CircleCollider2D>();
            if (frogCollider != null)
            {
                float scale = frog.transform.localScale.x;
                radius = frogCollider.radius * scale * 1.15f; 
            }
        }

        for (int i = 1; i < unsmoothedPath.Length; i++)
        {
            Vector2 anchor = unsmoothedPath[anchorIndex].worldPosition;
            Vector2 test = unsmoothedPath[i].worldPosition;

            Vector2 direction = (test - anchor).normalized;
            float distance = Vector2.Distance(anchor, test) - 0.1f;

            bool hitCollider = Physics2D.CircleCast(anchor, radius, direction, distance, grid.unwalkableMask);
            bool hitUnwalkableNode = false;

            int steps = Mathf.CeilToInt(distance / grid.gridSize);
            for (int step = 1; step <= steps; step++)
            {
                Vector2 samplePoint = Vector2.Lerp(anchor, test, step / (float)steps);
                Node sampleNode = grid.NodeFromWorldPoint(samplePoint);
                if (sampleNode.walkable == Node.Walkable.Obstacle || sampleNode.walkable == Node.Walkable.Slow)
                {
                    hitUnwalkableNode = true;
                    break;
                }
            }

            // Debug.DrawLine(anchor, test, (hitCollider || hitUnwalkableNode) ? Color.red : Color.cyan, 10.5f);

            if (hitCollider || hitUnwalkableNode)
            {
                smoothedPath.Add(unsmoothedPath[i - 1]);
                anchorIndex = i - 1;
            }
        }

        smoothedPath.Add(unsmoothedPath[unsmoothedPath.Length - 1]);

        Debug.Log("Raw path length: " + unsmoothedPath.Length);
        Debug.Log("Smoothed path length: " + smoothedPath.Count);

        return smoothedPath.ToArray();
    }

    private float DistanceFunction(DistanceType distanceType, Node nodeA, Node nodeB) {
        
        float distance = 0;
        float dx = Math.Abs(nodeA.gridX - nodeB.gridX);
        float dy = Math.Abs(nodeA.gridY - nodeB.gridY);

        // Euclidean
        if (distanceType == DistanceType.Euclidean) {
            float x = (float)Math.Pow(nodeA.gridX - nodeB.gridX, 2);
            float y = (float)Math.Pow(nodeA.gridY - nodeB.gridY, 2);
            distance = (float)Math.Sqrt(x + y);
        }
        
        // Manhattan
        else if (distanceType == DistanceType.Manhattan) {
            distance = dx + dy;
        }

        // Custom
        else if  (distanceType == DistanceType.Custom) {
            if (dx > dy) {
                distance = 14 * dy + 10 * (dx - dy);
            }
            else {
                distance = 14 * dx + 10 * (dy - dx);
            }
        }

        // Octile
        else if (distanceType == DistanceType.Octile) {
            float x = Math.Max(nodeA.gridX - nodeB.gridX, nodeA.gridY - nodeB.gridY);
            float y = Math.Min(nodeA.gridX - nodeB.gridX, nodeA.gridY - nodeB.gridY);
            distance = x + ((float)Math.Sqrt(2) - 1) + y;
        }   
        return distance;
    }   

    private float GCost(Node currentNode, Node neighborNode)
    {      

        float distanceNodes;

        // If diagonals are used then use this gcost (includes diagonal cost)
        if (grid.includeDiagonalNeighbours) {
            distanceNodes = DistanceFunction(DistanceType.Custom, currentNode, neighborNode);
            // distanceNodes = DistanceFunction(DistanceType.Octile, currentNode, neighborNode);
        }
        // Else 4 directional should use manhattan or euclidean
        else {
            distanceNodes = DistanceFunction(DistanceType.Manhattan, currentNode, neighborNode);
            // distanceNodes = DistanceFunction(DistanceType.Euclidean, currentNode, neighborNode);
        }

        float gCost;

        if (neighborNode.walkable == Node.Walkable.Slow) {
            gCost = currentNode.gCost + distanceNodes * gCostMultiplier;
        }
        else {
            gCost = currentNode.gCost + distanceNodes;
        }

        return gCost;
    }


    private float Heuristic(Node nodeA, Node nodeB)
    {   
        // If diagonals are used then use this gcost (includes diagonal cost)
        if (grid.includeDiagonalNeighbours) {
            return DistanceFunction(DistanceType.Custom, nodeA, nodeB);
            // return DistanceFunction(DistanceType.Octile, nodeA, nodeB);
        }

        // Else 4 directional should use manhattan or euclidean
        else {
            return DistanceFunction(DistanceType.Manhattan, nodeA, nodeB);
            // return DistanceFunction(DistanceType.Euclidean, nodeA, nodeB);
        }
    }
}
