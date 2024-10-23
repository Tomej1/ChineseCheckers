using System;
using System.Collections.Generic;
using UnityEngine;
using Position = UnityEngine.Vector2Int;

// The interface for using Minimax
// It has its own namespace to be reusable

namespace Minimax
{
    public interface IState
    {
        List<IState> Expand(IPlayer player);  // Generate a list of the states reachable from the current state by player
        int Score(IPlayer player);            // The heuristic score for player in the current state

        // I had to put this here since it could not reach the method from the State class
        // This allowes me to call for the current board in state and put that into the Enemysmove in MakeMove
        Piece[,] ReturnGameBoard();
    }

    public interface IPlayer  // The IPlayer interface is just for identity, does not require any methods
    {
        Piece Value();
        //bool Human();
    }

    public class MiniMax
    {
        // This is Kai Mikaels code, also added in otherplayer variables to keep track of all the
        // computers moves aswell, inspired by a classmate
        
            public static IState Select(IState state, IPlayer player, IPlayer otherPlayer, List<Player> listOfOtherPlayer, int playerIndex, int depth, bool maximising)
            {
                int currentValue;

                if (player.Value() == otherPlayer.Value()) //Maximerar nurvarnade spelare som ska göra drag
                    maximising = true;

                otherPlayer = listOfOtherPlayer[playerIndex];

                if (playerIndex < listOfOtherPlayer.Count - 1) //plussar på indexet för varje gång "select" körs
                    playerIndex++;
                else
                    playerIndex = 0;

                // We count down the depth in each recursion, when we reach 0 we simply return the given state,
                // same if the current state is a winning state for either player. 
                if (depth == 0 || state.Score(player) == Int32.MaxValue || state.Score(player) == Int32.MinValue)
                    return (state);

                IState childState;
                IState nextState;
                if (maximising)  // The player’s move
                {
                    currentValue = Int32.MinValue;
                    nextState = null;
                    List<IState> childstates = state.Expand(player);  // Find all moves player can make
                    if (childstates.Count == 0)  // If no further moves are possible, return the given state
                        return (state);

                    foreach (IState s in childstates) // For each found state, choose the move that will give the highest score
                    {
                        childState = Select(s, player, otherPlayer, listOfOtherPlayer, playerIndex, depth - 1, false);
                        if (childState != null && childState.Score(player) > currentValue)  // If this move is better than any previous, update
                        {
                            nextState = s;
                            currentValue = childState.Score(player);
                        }
                    }
                }
                else  // The opponent’s move, same as above, but choosing the lowest score for the player
                {
                    currentValue = Int32.MaxValue;
                    nextState = null;

                    List<IState> childstates = state.Expand(otherPlayer);
                    if (childstates.Count == 0)
                        return (state);

                    foreach (IState s in childstates)
                    {
                        childState = Select(s, player, otherPlayer, listOfOtherPlayer, playerIndex, depth - 1, true);

                        if (childState != null && childState.Score(player) < currentValue)
                        {
                            nextState = s;
                            currentValue = childState.Score(player);
                        }
                    }
                }

                return (nextState);  // Return the selected state
            }
        }
}