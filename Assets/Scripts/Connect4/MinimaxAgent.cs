using UnityEngine;
using System.Collections.Generic;
using ConnectFour;


public class MinimaxAgent : Agent
{   
    public int searchDepth = 3;
    public bool enablePruning;

    public override int GetMove(Connect4State state) {
        List<int> steps = state.GetPossibleMoves();

        int bestMove = 0;
        float bestValue = -Mathf.Infinity;

        foreach (int move in steps) {
            Connect4State nextState = state.Clone();
            nextState.MakeMove(move);
            float value = Minimax(nextState, searchDepth, Mathf.NegativeInfinity, Mathf.Infinity, true);

            if (value > bestValue) {
                bestValue = value;
                bestMove = move;
            }
        }
        return bestMove;
    }

    private float Minimax(Connect4State state, int depth, float alpha, float beta, bool maximizingPlayer)
    {
        Connect4State.Result result = state.GetResult();
        if (depth == 0 || result != Connect4State.Result.Undecided) {
            float evaluation = EvaluateState(state, result);
            return evaluation;
        }

        List<int> possibleMoves = state.GetPossibleMoves();

        // If the current player is maximizing (finds the highest)
        if (maximizingPlayer) {
            float maxEval = Mathf.NegativeInfinity;
            foreach (int move in possibleMoves) {
                Connect4State nextState = state.Clone();
                nextState.MakeMove(move);

                // Recursively call its child states
                float eval = Minimax(nextState, depth - 1, alpha, beta, false);
                maxEval = Mathf.Max(maxEval, eval);

                // Pruning
                if (enablePruning) {
                    alpha = Mathf.Max(alpha, eval);
                    if (beta <= alpha){
                        break;
                    }
                }
            }
            return maxEval;
        }

        // If the current player is minimizng (finds the lowest)
        else {
            float minEval = Mathf.Infinity;
            foreach (int move in possibleMoves) {

                Connect4State nextState = state.Clone();
                nextState.MakeMove(move);

                // Recursively call its child states
                float eval = Minimax(nextState, depth - 1, alpha, beta, true);
                minEval = Mathf.Min(minEval, eval);

                // Pruning
                if (enablePruning) {
                    beta = Mathf.Min(beta, eval);
                    if (beta <= alpha) {
                        break;
                    }
                }
            }
            return minEval;
        }
    }

    private float EvaluateState(Connect4State state, Connect4State.Result result)
    {   
        // If red wins, and the current player is red (1), return positive score
        if (result == Connect4State.Result.RedWin && playerIdx == 1) {
            return 100f;
        }
        // If yellow wins, and the current player is red (1), return negative score
        if (result == Connect4State.Result.YellowWin && playerIdx == 1) {
            return -100f;
        }
        if (result == Connect4State.Result.RedWin && playerIdx == 0) {
            return -100f;
        }
        if (result == Connect4State.Result.YellowWin && playerIdx == 0) {
            return 100f; 
        }
        if (result == Connect4State.Result.Draw) {
            return 0f;
        }

        // If there has not been a winner or draw, evaluate the value of current states lines
        return EvaluateLines(state);
    }

    private float EvaluateLines(Connect4State state) {

        float score = 0f;

        //  Horizontal lines
        for (int r = 0; r < GameController.numRows; r++) {
            for (int c = 0; c <= GameController.numColumns - GameController.numPiecesToWin; c++) {

                int playerPieces = 0;
                int opponentPieces = 0;
                for (int i = 0; i < GameController.numPiecesToWin; i++) {

                    // Check that the current player has pieces in each field 
                    if (state.field[c + i, r] == playerIdx + 1) {
                        playerPieces++;
                    }
                    else if (state.field[c + i, r] != (byte)Connect4State.Piece.Empty) {
                        opponentPieces++;
                    }
                }

                score += GetLineScore(playerPieces, opponentPieces);
            }
        }

        // Vertical lines
        for (int c = 0; c < GameController.numColumns; c++) {
            for (int r = 0; r <= GameController.numRows - GameController.numPiecesToWin; r++) {
                int playerPieces = 0;
                int opponentPieces = 0;

                // Check that the current player has pieces in each field 
                for (int i = 0; i < GameController.numPiecesToWin; i++) {
                    if (state.field[c, r + i] == playerIdx + 1) {
                        playerPieces++;
                    }
                    else if (state.field[c, r + i] != (byte)Connect4State.Piece.Empty) {
                        opponentPieces++;
                    }
                }
                score += GetLineScore(playerPieces, opponentPieces);
            }
        }

        //  Diagonal top-left to bottom-right
        for (int r = 0; r <= GameController.numRows - GameController.numPiecesToWin; r++) {
            for (int c = 0; c <= GameController.numColumns - GameController.numPiecesToWin; c++) {

                int playerPieces = 0;
                int opponentPieces = 0;

                // Check that the current player has pieces in each field 
                for (int i = 0; i < GameController.numPiecesToWin; i++) {
                    if (state.field[c + i, r + i] == playerIdx + 1) {
                        playerPieces++;
                    }
                    else if (state.field[c + i, r + i] != (byte)Connect4State.Piece.Empty) {
                        opponentPieces++;
                    }
                }
                score += GetLineScore(playerPieces, opponentPieces);
            }
        }

        //  Diagonal top-right to bottom-left
        for (int r = 0; r <= GameController.numRows - GameController.numPiecesToWin; r++) {
            for (int c = GameController.numColumns - 1; c >= GameController.numPiecesToWin - 1; c--) {

                int playerPieces = 0;
                int opponentPieces = 0;
                
                // Check that the current player has pieces in each field 
                for (int i = 0; i < GameController.numPiecesToWin; i++) {
                    if (state.field[c - i, r + i] == (playerIdx + 1)) {
                        playerPieces++;
                    }
                    else if (state.field[c - i, r + i] != (byte)Connect4State.Piece.Empty) {
                        opponentPieces++;
                    }
                }
                score += GetLineScore(playerPieces, opponentPieces);
            }
        }
        return score;
    }

    private float GetLineScore(int playerPieces, int opponentPieces)
    {   
        // If player wins return high score, if opponent wins return low score
        if (playerPieces == GameController.numPiecesToWin) {
            return 100f;
        }
        if (opponentPieces == GameController.numPiecesToWin) {
            return -100f;
        }

        // Return score based on how many coins in a row are placed
        if (playerPieces == 3 && opponentPieces == 0) {
            return 5f;
        }
        if (opponentPieces == 3 && playerPieces == 0) {
            return -5f;
        }
        if (playerPieces == 2 && opponentPieces == 0) {
            return 2f;
        }
        if (opponentPieces == 2 && playerPieces == 0) {
            return -2f;
        }
        return 0f;
    }
}