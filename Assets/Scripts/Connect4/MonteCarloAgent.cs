using UnityEngine;
using System.Collections.Generic;

public class MonteCarloAgent : Agent
{
    public int totalSims = 2500;

    public enum Difficulty {
        None,
        Hard,
        Medium,
        Easy
    }

    public Difficulty difficulty;

    public override int GetMove(Connect4State state)
    {
        // TODO: Override this method with the logic described in class.
        // Currently, it just returns a random move.
        // You can add other methods to the class if you like.
        // List<int> moves = state.GetPossibleMoves();
        // return moves[Random.Range(0, moves.Count)];
        
        List<int> steps = state.GetPossibleMoves();
        int CNsteps = steps.Count;
        float[] tally = new float[CNsteps]; 
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
        
        int Simulmove = newTotalSims / CNsteps;
        for (int i = 0; i < CNsteps; i++)
        {
            int move = steps[i];
            float Totaltally = 0f;
            for (int j = 0; j < Simulmove; j++)
            {
                Connect4State SIMst = state.Clone();
                SIMst.MakeMove(move);           
                Totaltally += GAITsim(SIMst, playerIdx);
            }
            tally[i] = Totaltally / Simulmove; 
        }
        int SuperiorMove = argMax(tally);
        return steps[SuperiorMove];
    }
    float GAITsim(Connect4State SIMst, int agentPlayerIdx)
    {
        while (SIMst.GetResult() == Connect4State.Result.Undecided)
        {
            List<int> legalMoves = SIMst.GetPossibleMoves();
            if (legalMoves.Count == 0) // Handle potential draw if no moves left
            {
                return 0f;
            }
            int randomMove = legalMoves[Random.Range(0, legalMoves.Count)];
            SIMst.MakeMove(randomMove);
        }

        Connect4State.Result result = SIMst.GetResult();

        // If the player wins and the current player is that colour, return a win
        if ((result == Connect4State.Result.RedWin && agentPlayerIdx == 1) || (result == Connect4State.Result.YellowWin && agentPlayerIdx == 0))
        {
            return 1f; 
        }

        // Return a loss score
        else if ((result == Connect4State.Result.YellowWin && agentPlayerIdx == 1) || (result == Connect4State.Result.RedWin && agentPlayerIdx == 0)) {
            return -1f; 
        }

        // Return a draw score
        else {
            return 0f; 
        }
    }
}
