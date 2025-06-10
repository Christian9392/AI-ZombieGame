using UnityEngine;
using System.Collections.Generic;
using ConnectFour;

public class MCTSAgent : Agent
{
    // This is node class for the Monte Carlo Tree Search (MCTS) algorithm.
    class MonteCarloTreeSearchNode {
        public Connect4State state;
        public MonteCarloTreeSearchNode parent;
        public List<MonteCarloTreeSearchNode> children = new List<MonteCarloTreeSearchNode>();
        public List<int> possibleMoves;
        public int move;
        public int movedPlayer;
        public int numVisits = 0;
        public float win = 0;

        public MonteCarloTreeSearchNode(Connect4State state, MonteCarloTreeSearchNode parent, int move){
            this.state = state;
            this.parent = parent;
            this.move = move;
            this.possibleMoves = state.GetPossibleMoves();
            this.movedPlayer = state.GetPlayerTurn() == 0 ? 1 : 0;
        }

        public bool IsFullyExpanded() {
            return possibleMoves.Count == 0;
        }

        public bool IsTerminal() {
            return state.GetResult() != Connect4State.Result.Undecided;
        }
    }

    public int totalSims = 2500;
    public float c = Mathf.Sqrt(2.0f);

    public enum Difficulty {
        None,
        Hard,
        Medium,
        Easy
    }

    public Difficulty difficulty;

    public override int GetMove(Connect4State state)
    {
        // TODO: Override this method with an MCTS implementation.
        // Currently, it just returns a random move.
        // You can add other methods to the class if you like.
        // List<int> moves = state.GetPossibleMoves();
        // return moves[Random.Range(0, moves.Count)];

        MonteCarloTreeSearchNode root = new MonteCarloTreeSearchNode(state, null, -1);

        int newTotalSims;
        
        if (difficulty == Difficulty.Easy) {
            newTotalSims = 10;
        }
        else if (difficulty == Difficulty.Medium) {
            newTotalSims = 50;
        }
        else if (difficulty == Difficulty.Hard) {
            newTotalSims = 200;
        }
        else {
            newTotalSims = totalSims;
        }

        // Run the MCTS algorithm for a number of simulations
        for (int i = 0; i < newTotalSims; i++) {
            MonteCarloTreeSearchNode node = TreePolicy(root);
            float result = DefaultPolicy(node.state.Clone());
            Backup(node, result); // Backpropagation
        }

        // Select the best child based on the number of visits
        MonteCarloTreeSearchNode bestChild = root.children[0];
        foreach (var child in root.children) {
            if (child.numVisits > bestChild.numVisits) {
                bestChild = child;
            }
        }

        Debug.Log("=== MCTSAgent Decision Stats ===");
        foreach (var child in root.children)
        {
            float winRate = child.numVisits > 0 ? child.win / child.numVisits : 0f;
            Debug.Log($"Move: {child.move}, Visits: {child.numVisits}, WinRate: {winRate:F3}");
        }

        return bestChild.move;
    }

    // Tree Policy: Traverses the tree to find a leaf node to expand
    MonteCarloTreeSearchNode TreePolicy(MonteCarloTreeSearchNode node) {
        while (!node.IsTerminal()) {
            if (!node.IsFullyExpanded()) {
                return Expand(node); // Expand an unvisited child node
            } else {
                node = BestChild(node, c); // Select the best child node using UCT
            }
        }
        return node;
    }

    // Expand: Adds a new child node to the current node
    MonteCarloTreeSearchNode Expand(MonteCarloTreeSearchNode node) {
        int move = node.possibleMoves[UnityEngine.Random.Range(0, node.possibleMoves.Count)];
        Connect4State newState = node.state.Clone();
        newState.MakeMove(move);
        MonteCarloTreeSearchNode childNode = new MonteCarloTreeSearchNode(newState, node, move);
        node.children.Add(childNode);
        node.possibleMoves.Remove(move);
        return childNode;
    }

    // Best child: Selects the child node with the highest UCT value    
    MonteCarloTreeSearchNode BestChild(MonteCarloTreeSearchNode node, float c) {
        MonteCarloTreeSearchNode bestChild = null;
        float highestValue = float.NegativeInfinity;

        foreach (var child in node.children) {
            float uctValue1 = child.numVisits == 0 ? 0 : child.win / child.numVisits;
            float uctValue2 = c * Mathf.Sqrt(Mathf.Log(node.numVisits + 1) / (child.numVisits + 1e-4f));
            float uctValue = uctValue1 + uctValue2;

            if (uctValue > highestValue) {
                highestValue = uctValue;
                bestChild = child;
            }
        }
        return bestChild;
    }

    // Default Policy: Play randomly until the end of the game
    float DefaultPolicy(Connect4State state)
    {
        while (state.GetResult() == Connect4State.Result.Undecided) {
            List<int> moves = state.GetPossibleMoves();
            if (moves.Count == 0) break; // Safety check

            // Prefer center column
            int centerColumn = GameController.numColumns / 2;
            moves.Sort((a, b) => Mathf.Abs(a - centerColumn).CompareTo(Mathf.Abs(b - centerColumn)));
            int move = moves[0];
            state.MakeMove(move);
        }
        float originalResult  = Connect4State.ResultToFloat(state.GetResult());
        return (playerIdx == 0) ? 1f - originalResult  : originalResult ;
    }
    
    // Backpropagation: Updates the node and its parents with the result
    void Backup(MonteCarloTreeSearchNode node, float result) {
        while (node != null) {
            node.numVisits++;
            if (node.movedPlayer == playerIdx) {
                node.win += result; // Player 0 is the maximizing player
            } else {
                node.win += 1 - result; // Player 1 is the minimizing player
            }
            node = node.parent;
        }
    }
}
