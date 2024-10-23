using System;
using System.Collections.Generic;
using UnityEngine;
using Minimax;

using Position = UnityEngine.Vector2Int;
using Coordinate = UnityEngine.Vector2;
using UnityEngine.Analytics;
using UnityEngine.SocialPlatforms.Impl;
using System.Linq;

// State records where all pieces are on the board.
// State is used by BoardModel.
public class State : IState
{
    //-------------------------------------------------------------------------------------------------------------------------------------------
    // Most of the things used in expand i found on stackoverflow
    //___________________________________________________________________________________________________________________________________________


    private int xMin = -8, xMax = 8, yMin = -8, yMax = 8;
    private Piece[,] gameBoard ;
    private BoardModel boardModel;

    // This was inspired by a classmate
    // Making a constructor for the state that we can use
    public State(Piece[,] gameBoard)
    {
        boardModel = BoardModel.Instance();
        this.gameBoard = gameBoard;
    }

    // Check for all possible movements for the every piece
    // Alot of this was inspired from the programming pattern book and stackoverflow mostly
    // I also played around with ChatGPT
    public List<IState> Expand(IPlayer player)
    {
        // Step 1, create start and end positions
        Position startPosition = new Position();
        Position endPosition = new Position();

        // This is taken from the game programming patterns book
        // Step 2, Create a state list that handles the current state of the state
        // The idea is to load all possible moves similar to movepiece
        List<IState> currentState = new List<IState>();

        // Step 3, we need to check the boundaries of the board
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                // Step 4, check which player is playing
                if (gameBoard[x, y] == player.Value())
                {
                    // Step 5, call for boardmodel so we can handle movement methods
                    // Set the positions in start for the piece to x and y
                    // The method Set was found in the book and hopefully it works for this logic
                    startPosition.Set(x, y);
                    List<Position> allowedMoves = new();
                    allowedMoves.AddRange(boardModel.FindJumpDirection(startPosition, new List<Position>()));
                    allowedMoves.AddRange(boardModel.FindMoveDirection(startPosition, new List<Position>()));

                    // Make the new positions empty after the move has been done
                    foreach (Position pos in allowedMoves)
                    {
                        // This was inspired by one of the lectures for bubblesort and quicksort

                        // Update the gameboard and make the old positions empty
                        endPosition.Set(pos.x, pos.y);

                        // Create a copy the board and set as a nextState
                        State nextState = new State((Piece[,])gameBoard.Clone());

                        // Create a variable to handle the current piece being moved
                        Piece currentPiece = nextState.gameBoard[startPosition.x, startPosition.y];
                        nextState.gameBoard[startPosition.x, startPosition.y] = Piece.Empty;
                        nextState.gameBoard[endPosition.x, endPosition.y] = currentPiece;

                        // Add this move into the currentState
                        currentState.Add(nextState);
                    }
                }
            }
        }

        // Return the states
        return currentState;
    }

    public int Score(IPlayer player)
    {
        List<Position> ownPiecePosition = new List<Position>();
        Piece piece = ((Player)player).Value();
        int score = new int();

        //Find goal area for the current player and current players own area.
        List<Position> oppositePos = boardModel.pieceStartPositions[PieceInfo.opposites[(int)piece]];

        //Y-axis is tilted, so we need an offset to the goal position.
        int tilt = (int)Mathf.Round((float)Mathf.Sqrt(3));
        Position goalPosition = new Position(oppositePos[0].x + tilt, oppositePos[0].y + tilt);

        //The goal position needs to be inverted for the colors that does not go to the right or straight up or down.
        if (piece == Piece.Green || piece == Piece.Yellow)
        {
            goalPosition = new Position(oppositePos[0].x + -tilt, oppositePos[0].y + -tilt);

        }

        //Get positions for own balls
        for (int x = -8; x < 9; x++)
        {
            for (int y = -8; y < 9; y++)
            {
                if (gameBoard[x, y] == piece)
                {
                    ownPiecePosition.Add(new Position(x, y));
                }
            }
        }

        //Create a list of positions that will punish the players; that is colors that are not the goal area
        List<Position> oppPos = new List<Position>(oppositePos.Take(ownPiecePosition.Count));

        List<int> allPieceColors = new List<int>() { 0, 1, 2, 3, 4, 5 };
        string oppositePiece = PieceInfo.pieceNames[(int)PieceInfo.opposites[(int)piece]];
        allPieceColors.Remove((int)PieceInfo.opposites[(int)Enum.Parse(typeof(Piece), oppositePiece)]);

        List<Position> otherPlayerPos = new List<Position>();
        List<Piece> otherPlayerColors = new List<Piece>();


        foreach (int color in allPieceColors)
        {
            otherPlayerColors.Add(PieceInfo.opposites[(color)]);
        }
        foreach (Piece currentColor in otherPlayerColors)
        {
            List<Position> listPos = new List<Position>(boardModel.pieceStartPositions[currentColor]);
            otherPlayerPos.AddRange(listPos);
        }

        //Calculate own score
        int piecesInGoal = 0;
        foreach (Position position in ownPiecePosition)
        {
            float distanceCheck = Position.Distance(goalPosition, position);

            //A quick fix to make sure the pieces on the edges does not get stuck in their home by opponent pieces
            if (position == boardModel.pieceStartPositions[piece][0])
            {
                score -= 1000;
            }
            score = score + (int)(1000 / (distanceCheck + 1));
            if (oppPos.Contains(position))
            {
                score += 5000;
                piecesInGoal++;

                if (piecesInGoal == oppPos.Count())
                {
                    score = Int32.MaxValue;
                    break;

                }
            }
            if (otherPlayerPos.Contains(position))
            {
                score -= 1000;
            }
            score -= (int)distanceCheck;

        }
        return score;
    }

    // A method that returns the board
    // Im making this method here since i will be using it for scoring
    public Piece[,] ReturnGameBoard()
    {
        return gameBoard;
    }


}

