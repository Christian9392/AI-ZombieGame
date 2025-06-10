﻿// Adapted from: https://github.com/SebLague/Pathfinding-2D

using System.Security.Cryptography.X509Certificates;
using UnityEngine;

// This is a class to track a node in the A* Grid
// It tracks a number of properties that we need to compute A*
public class Node : IHeapItem<Node>
{
    // Can the node be reached (ie, no obstacle)
    // public bool walkable;
    public enum Walkable {
        Walkable,
        Obstacle,
        Slow
    }
    public Walkable walkable;

    // The actual world position of the node
    public Vector2 worldPosition;

    // The grid cell of the node
    public int gridX;
    public int gridY;

    // G Cost to get to this node
    public float gCost;

    // H Cost for this node
    public float hCost;

    // Parent for this node (for reconstructing the path)
    public Node parent;

    // For heap management
    int heapIndex;

    public Node(Walkable _walkable, Vector2 _worldPos, int _gridX, int _gridY)
    {
        walkable = _walkable;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
    }

    public Node Clone()
    {
        return new Node(walkable, worldPosition, gridX, gridY);
    }

    public float fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }

    // Reset all node data
    public void ResetNodeData()
    {
        gCost = 0;
        hCost = 0;
        parent = null;
    }

}
