using System;
using System.Collections.Generic;
using UnityEngine;
using Position = UnityEngine.Vector2Int;
using Minimax;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using Unity.VisualScripting;


// BoardModel has the responsibility of keeping track of the current state of the play, know the rules of the game and
// send updates to all listeners when the state changes.
public class BoardModel : IBoardModel
{

    private static BoardModel instance;

    // Creating a board
    private Piece[,] board;
    private List<IBoardListener> listeners;
    private List<Player> players;

    // Make the boudaries of the board
    private const int xMin = -8;
    private const int xMax = 8;
    private const int yMin = -8;
    private const int yMax = 8;

    // Making all the diffrent compass coordinates for the movement for the piece
    private readonly Position[] moveDirections = new[]
    {
        new Position(1, 0),  // x
        new Position(-1, 0), // -x
        new Position(-1, 1), // -x, -y
        new Position(1, -1), // +x, -y
        new Position(0, 1),  // y
        new Position(0, -1) // -y
    };

    // Creating some variables to keep information private in script
    private int numberOfPlayers;
    private int numberOfPieces;
    private int difficulty;

    // Creating a dictionary for the piece start positions
    public Dictionary<Piece, List<Position>> pieceStartPositions;

    private BoardModel() 
    {
        // Creating all the information directly in the constructor
        listeners = new List<IBoardListener>();
        players = new List<Player>();
        pieceStartPositions = new Dictionary<Piece, List<Position>>();

        board = Utility.MakeMatrix<Piece>(xMin, yMin, xMax, yMax);
    }

    public static BoardModel Instance()
    {
        if (instance == null)
          instance = new BoardModel();
        return instance;
    }

    public void StartGame(int numPlayers)
    {
        numberOfPlayers = numPlayers;
        numberOfPieces = PieceInfo.numPieces[numberOfPlayers];

        // Add information about pieces and placement
        CreateStartPiecePositions();
        CreatePlayers();
        CreateEmptyPositions();
        CreateInvalidPositions();
        SetDifficulty(1);
        PlacePiecesOnBoard();
    }

    public void AddListener(IBoardListener listener)
    {
        listeners.Add(listener);
    }

    public void SetDifficulty(int difficulty)
    {
        this.difficulty = difficulty;
    }

    public bool SetPiece(Position pos, Piece piece)
    {
        return false;
    }

    public Piece GetPiece(Position pos)
    {
        // Check if the player has clicked inside the borders for the gameboard
        // Hopefully this still works if there is no piece on the location that the player clicked
        if ((pos.x <= xMin && pos.x >= xMax) && (pos.y <= yMin && pos.y >= yMax))
        {
            return Piece.Invalid;
        }

        // If they have in fact pressed on the board, return the piece
        return board[pos.x, pos.y];
    }

    public bool MovePiece(Position startPos, Position endPos)
    {
        // I made this into a bool aswell to solve a problem where the AI just wouldnt move
        // Did not change anything for me but atleast it looks more clean in my opinion

        // Check if the move is allowed
        if(MoveIsAllowed(startPos, endPos))
        {
            // Update the board according to the move that has been done
            foreach (IBoardListener listener in listeners)
            {
                // Update the game before we continue
                listener.MovePiece(startPos, endPos);
            }
            // Making the last position empty
            board[endPos.x, endPos.y] = board[startPos.x, startPos.y];
            board[startPos.x, startPos.y] = Piece.Empty;

            // Check for winners
            CheckingForWinner(board[endPos.x, endPos.y]);
            return true;
        }
        return false;
    }

    // THis was found on stackoverflow
    private bool MoveIsAllowed(Position startPos, Position endPos)
    {
        // Add all the possible moves into new variables
        List<Position> allNormalMoves = FindMoveDirection(startPos, new List<Position>());
        List<Position> allJumpMoves = FindJumpDirection(startPos, new List<Position>());
        List<Position> moveSelectedPiece = new();

        // Add all possible moves into the selected piece that we want to move
        moveSelectedPiece.AddRange(allJumpMoves);
        moveSelectedPiece.AddRange(allNormalMoves);

        foreach (var pos in moveSelectedPiece)
        {
            if (pos == endPos)
            {
                if (IsWithinBounds(endPos))
                {
                    return true;
                }
                else
                {
                    Debug.LogError("Move is outside of valid bounds!");
                    return false;
                }
            }
        }

        Debug.Log($"Move not allowed. startPos: {startPos}, endPos: {endPos}");
        return false;
    }

    private bool IsWithinBounds(Position pos)
    {
        return pos.x >= xMin && pos.x <= xMax && pos.y >= yMin && pos.y <= yMax;
    }

    // Most of this was found on stackoverflow, I also took help from classmates
    public void MakeMove(Player player)
    {
        // Making a newstate by copying the current board we have
        State currentState = new State((Piece[,])board.Clone());
        // Call for Minimax so we can run through the recursive move logic
        Minimax.IState enemyState = MiniMax.Select(currentState, player, player, players, 0, difficulty, true);

        // Step 2, fetch the board condition for the enemy boards
        // This was quite tricky, it was not able to call for the method so I had to
        // put it in minimax constructor
        Piece[,] enemyBoard = enemyState.ReturnGameBoard();

        // Step 3, create variables to handle enemy positions
        //Position enemyStartPos = new Position(Int32.MaxValue, Int32.MaxValue);
        //Position enemyEndPos = new Position(Int32.MaxValue, Int32.MaxValue); 

        Position enemyStartPos = new Position(Int32.MaxValue, Int32.MaxValue);
        Position enemyEndPos = new Position(Int32.MaxValue, Int32.MaxValue);

        // Step 4, Check if it is the enemys turn
        // Taking the board into account
        for (int x = xMin; x <= xMax; x++)
        {
            for(int y = yMin; y <= yMax; y++)
            {
                // Check if the current board is the enemysboard
                if (board[x, y] != enemyBoard[x, y])
                {
                    // Check if the position is empty or not
                    if (enemyBoard[x, y] == Piece.Empty)
                    {
                        enemyStartPos = new Position(x, y);
                    }

                    else
                    {
                        enemyEndPos = new Position(x, y);
                    }
                }
               
            }
        }

        if(enemyEndPos != null && enemyStartPos != null)
        {

        // Step 5, update the board and the game view
        // Updating the board
            board = enemyBoard;

            try
            {
                foreach (var listener in listeners)
                {
                    listener.MovePiece(enemyStartPos, enemyEndPos);
                }

                // Check if the move made was a winning move
                CheckingForWinner(board[enemyEndPos.x, enemyEndPos.y]);

            }
            catch (IndexOutOfRangeException ex)
            {
                Debug.Log("enemyStartPos: " + enemyStartPos);
                Debug.Log("enemyEndPos: " + enemyEndPos);
                Debug.LogError("IndexOutOfRangeException: " + ex.Message);
            }
        }
    }

    // This was found on Stackoverflow in multiple steps
    // Was almost finished on stackoverflow
    public List<Position> FindMoveDirection(Position pos, List<Position> availableDirections) 
    {
        // Go through all possible directions in moveDirections
        foreach(Position directions in moveDirections)
        {
            // Step 1, create a variable to handle the current direction that we are moving
            Position currentDirection = pos + directions;

            // Step 2, check to see if the are moving outside of board, if we are then continue
            if(currentDirection.y > yMax || currentDirection.y < yMin || currentDirection.x > xMax || currentDirection.x < xMin)
            {
                // If this happens we want to continue
                continue;
            }

            // Step 3, check if the move we made is on an empty spot
            if (board[currentDirection.x, currentDirection.y] == Piece.Empty) 
            {
                // Perhaps i need to check if the move is already added to the list?
                // Will come back and change if it creates any bugs :)
                if (!availableDirections.Contains(currentDirection))
                {
                    // Step 4, add value to the list
                    availableDirections.Add(currentDirection);
                }
                // Maybe remove the old value aswell??
            }
        }
        
        // Return all possible move directions
        return availableDirections;
    }

    // This was found on Stackoverflow in multiple steps
    public List<Position> FindJumpDirection(Position pos, List <Position> availableJumpPositions)
    {
        foreach(Position directions in moveDirections)
        {
            // Step 1, create a variable to handle the current direction that we are moving
            Position currentDirection = pos + directions;

            // Step 2, check if we are moving outside of the board
            if (currentDirection.y > yMax || currentDirection.y < yMin || currentDirection.x > xMax || currentDirection.x < xMin)
            {
                // If we are not jumping outside of board we want to continue
                continue;
            }

            // Step 3, if we are making a jump check so that the new position is not invalid or empty
            if (board[currentDirection.x, currentDirection.y] != Piece.Invalid && board[currentDirection.x, currentDirection.y] != Piece.Empty)
            {
                // Create a new variable so we can see if we are jumping out of the board
                Position jumpDirection = currentDirection + directions;
                // Step 4, check again if the new position is outside of the board after the jump
                if (jumpDirection.y > yMax || jumpDirection.y < yMin || jumpDirection.x > xMax || jumpDirection.x < xMin)
                {
                    // If this happens we want to continue
                    continue;
                }

                // Step 5, check if the position behind is empty
                if (board[jumpDirection.x, jumpDirection.y] == Piece.Empty)
                {
                    // Here i apparently did not check if the move was already added which made it bugg the game
                    // Luckely after changing the parameters for the allowed move into a bool method and adding an if here
                    // I was able to solve it
                    if (!availableJumpPositions.Contains(jumpDirection))
                    {
                        // Step 6, add jumpdirection to the list
                        availableJumpPositions.Add(jumpDirection);

                        // Step 7, make it so we can jump again if we want to after making our jump
                        // Maybe i can call the method again
                        FindJumpDirection(jumpDirection, availableJumpPositions);
                    }
                }
            }
        }

        // Return all possible jump locations
        return availableJumpPositions;
    }

    public void SaveGame()
    {
        // This was found on Stack Overflow, had to make some adjustments
        // Creating the necassary information for the location of the file
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream;
        stream = new FileStream("SaveGameData.data", FileMode.Create);

        try
        {
            // Perhaps i should make A json so it is readable
            formatter.Serialize(stream, board);
            formatter.Serialize(stream, numberOfPlayers);
            formatter.Serialize(stream, numberOfPieces);
            formatter.Serialize(stream, difficulty);
        }
        catch (SerializationException exception)
        {
            Console.WriteLine("Failed to serialize. Reason: " + exception.Message);
            throw;
        }
        finally
        {
            stream.Close();
        }
    }

    public void LoadGame()
    {
        string fileToLoad = "SaveGameData.data";
        FileStream stream;

        // A variation of this was found on Stack Overflow
        // Check if there is a savefile
        if(File.Exists(fileToLoad))
        {
            // Load in all the information in the savefile
            BinaryFormatter formatter = new BinaryFormatter();
            stream = new FileStream (fileToLoad, FileMode.Open);

            // load out information about the game, players, pieces, score etc
            board = (Piece[,])formatter.Deserialize(stream);
            numberOfPlayers = (int)formatter.Deserialize(stream);
            numberOfPieces = (int)formatter.Deserialize(stream);
            difficulty = (int)formatter.Deserialize(stream);


            // Making it so that the board pieces information is loaded into the game
            foreach(IBoardListener listener in listeners)
            {
                listener.NewGame(players);
                for(int x = xMin; x <= xMax; x++)
                {
                    for(int y = yMin; y <= yMax; y++)
                    {
                        // Checking if the piece is within the board
                        if (board[x,y] != Piece.Empty && board[x, y] != Piece.Invalid)
                        {
                            // Placing the piece on the board
                            Position piecePos = new Position(x, y);
                            listener.PlacePiece(piecePos, board[x, y]);
                        }
                    }
                }
            }
           
            // Resetting values when loading
            CreatePlayers();
            CreateInvalidPositions();
            CreateStartPiecePositions();

            stream.Close();
        }
    }

    // Looks if the player or the AI has won
    // This was found on stack overflow, also got help from classmates
    // I dont know why this is not working??
    private void CheckingForWinner(Piece piece)
    {
        // Creating variables
        int playerIndex = 0;
        int redCounter = 0, cyanCounter = 0, blueCounter = 0, yellowCounter = 0, magentaCounter = 0, greenCounter = 0;
        List<int> allCounters = new List<int>();

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                Position pos = new Position(x, y);
                if (board[x, y] == piece)
                {

                    // Check through the dictionary if the piece is standing in the oppisite goal, if it does add to the counter of that color
                    if (pieceStartPositions[PieceInfo.opposites[(int)piece]].Contains(pos))
                    {
                        if (piece == Piece.Red)
                            redCounter++;
                        if (piece == Piece.Cyan)
                            cyanCounter++;
                        if (piece == Piece.Blue)
                            blueCounter++;
                        if (piece == Piece.Yellow)
                            yellowCounter++;
                        if (piece == Piece.Magenta)
                            magentaCounter++;
                        if (piece == Piece.Green)
                            greenCounter++;
                    }
                }
            }
        }

        // adding all the counters into a list
        allCounters.Add(redCounter);
        allCounters.Add(cyanCounter);
        allCounters.Add(blueCounter);
        allCounters.Add(yellowCounter);
        allCounters.Add(magentaCounter);
        allCounters.Add(greenCounter);

        // Go through the counter and check if it contains 10 or 15 pieces
        for (int i = 0; i < allCounters.Count; i++)  
        {
            if (allCounters[i] == numberOfPieces)
            {
                // This will update and remove the player from the game if they reach the opposite side with all their pieces
                for (int j = 0; j < numberOfPlayers; j++)
                {
                    if (players[j].Value() == piece)
                    {
                        playerIndex = j;
                    }
                }

                Debug.Log($"Player was removed at index{playerIndex}");
                // Player is removed from the game as long as there is more than 1 player in the game
                players.RemoveAt(playerIndex);
                // Call SetNewWinner method to set the winner
                foreach (var listener in listeners) 
                {
                    // Hopefully this allows the game to continue
                    listener.SetNewWinner(players[playerIndex]);
                }
            }
        }
    }

    private void CreatePlayers()
    {
        // EDIT CREATE TAG BALL...... Holy.. This took quite some time to figure out, weirdly enough...

        // Need to create Players in the game in order to play, wish i realized this sooner :(
        // Debugging for an hour trying to figure out the problem
        // Got help from classmates luckely...
        if (players.Count > 0)
        {
            players.Clear();
        }

        // Create Human player, the bool is true in Setups for human
        players.Add(new Player(PieceInfo.setups[numberOfPlayers][0], true));

        // Create computer players, the bool is false 
        for(int i = 1; i < numberOfPlayers; i++)
        {
            players.Add(new Player(PieceInfo.setups[numberOfPlayers][i], false));
        }
    }

    private void CreateInvalidPositions()
    {
        // Top Left corner
        // Take xMin and xMax
        for (int i = -5; i >= -8; i--)
        {
            // Take yMin and yMax
            for (int j = 5; j <= 8; j++)
            {
                board[i, j] = Piece.Invalid;
            }
        }

        // Top Right
        for (int i = 0; i <= 8; i++)
        {
            for (int j = 5; j <= 8; j++)
            {
                board[i, j] = Piece.Invalid;
            }
        }

        // Right
        for (int i = 5; i <= 8; i++)
        {
            for (int j = 0; j <= 4; j++)
            {
                board[i, j] = Piece.Invalid;
            }
        }

        // Bottom Right corner
        for (int i = 5; i <= 8; i++)
        {
            for (int j = -5; j >= -8; j--)
            {
                board[i, j] = Piece.Invalid;
            }
        }

        // Bottom Left
        for (int i = 0; i >= -8; i--)
        {
            for (int j = -5; j >= -8; j--)
            {
                board[i, j] = Piece.Invalid;
            }
        }

        // Left
        for (int i = -5; i >= -8; i--)
        {
            for (int j = 0; j >= -4; j--)
            {
                board[i, j] = Piece.Invalid;
            }
        }

        // To take all the positions I could not make into squares
        // I know i can use triangles but i dont know how to put it into code
        List<Position> invalidPiecePositions = new()
        {
            // All the triangles of positions that i didnt know how to put through for loops

            // Top
            new Position(-3, 8),
            new Position(-2, 7),
            new Position(-2, 8),
            new Position(-1, 6),
            new Position(-1, 7),
            new Position(-1, 8),

            // Right
            new Position(6, -1),
            new Position(7, -1),
            new Position(7, -2),
            new Position(8, -1),
            new Position(8, -2),
            new Position(8, -3),

            // Bottom
            new Position(1, -6),
            new Position(1, -7),
            new Position(1, -8),
            new Position(2, -7),
            new Position(2, -8),
            new Position(3, -8),

            // Left
            new Position(-8, 3),
            new Position(-8, 2),
            new Position(-8, 1),
            new Position(-7, 2),
            new Position(-7, 1),
            new Position(-6, 1)
        };

        // Take all the positions added to the list and make them invalid
        foreach (var position in invalidPiecePositions)
        {
            board[position.x, position.y] = Piece.Invalid;
        }
    }

    private void CreateEmptyPositions()
    {
        // Make the whole board Empty, before adding piece positions and invalid positions
        for (int i = xMin; i <= xMax; i++)
        {
            for (int j = yMin; j <= yMax; j++)
            {
                board[i, j] = Piece.Empty;
            }
        }
    }

    private void PlacePiecesOnBoard()
    {
        foreach (IBoardListener listener in listeners)
        {
            // Check for the players in NewGame
            // NewGame removes all pieces from the board
            // Start a new game, we use it here to add the new pieces to the board
            listener.NewGame(players);

            // We run the for loop depending on the amount of players
            for (int i = 0; i < numberOfPlayers; i++)
            {
                // Run the loop for number of pieces each player has
                for (int j = 0; j < numberOfPieces; j++)
                {
                    // Depending on the information place the pieces on their start positions for each player
                    Position setPosition = pieceStartPositions[PieceInfo.setups[numberOfPlayers][i]][j];
                    listener.PlacePiece(pieceStartPositions[PieceInfo.setups[numberOfPlayers][i]][j], PieceInfo.setups[numberOfPlayers][i]);
                    board[setPosition.x, setPosition.y] = PieceInfo.setups[numberOfPlayers][i];
                }
            }
        }
    }

    public Dictionary<Piece, List<Position>> AllStartPositions()
    {
        return pieceStartPositions;
    }

    public Piece[,] RecieveGameBoard()
    {
        return board;
    }

    public int RecieveNrOfPl()
    {
        return numberOfPlayers;
    }

    public void CreateStartPiecePositions()
    {
        // Create a list that will hold the piece positions
        List<Position> piecePositions;
        if(pieceStartPositions.Count > 0)
        {
            pieceStartPositions.Clear();
        }

        for(int i = 0; i < 6; i++)
        {
            // Found a similar setup for this on Stackoverflow, and changed it so that it hopefully works for this setup
            switch(PieceInfo.setups[6][i])
            {
                // Red start
                case Piece.Red:
                    piecePositions = new List<Position>();

                    // If there are 2 players we want to add more pieces to red
                    if(numberOfPlayers == 2)
                    {
                        piecePositions.Add(new Position(-4, 4));
                        piecePositions.Add(new Position(-3, 4));
                        piecePositions.Add(new Position(-2, 4));
                        piecePositions.Add(new Position(-1, 4));
                        piecePositions.Add(new Position(0, 4));
                    }
                    // We want to create the other 10 pieces either way
                    piecePositions.Add(new Position(-4, 8));
                    piecePositions.Add(new Position(-4, 7));
                    piecePositions.Add(new Position(-4, 6));
                    piecePositions.Add(new Position(-4, 5));

                    piecePositions.Add(new Position(-3, 7));
                    piecePositions.Add(new Position(-3, 6));
                    piecePositions.Add(new Position(-3, 5));

                    piecePositions.Add(new Position(-2, 6));
                    piecePositions.Add(new Position(-2, 5));
                    piecePositions.Add(new Position(-1, 5));

                    // Adding all the created positions to the pieceStartPositions Dictionary
                    pieceStartPositions.Add(Piece.Red, piecePositions);
                    break;

                // Cyan start
                case Piece.Cyan:
                    piecePositions = new List<Position>();

                    // Check if there are 2 players, similar to red
                    if (numberOfPlayers == 2)
                    {
                        piecePositions.Add(new Position(0, -4));
                        piecePositions.Add(new Position(1, -4));
                        piecePositions.Add(new Position(2, -4));
                        piecePositions.Add(new Position(3, -4));
                        piecePositions.Add(new Position(4, -4));
                    }
                    // Similar to above create the pieces
                    piecePositions.Add(new Position(4, -8));
                    piecePositions.Add(new Position(4, -7));
                    piecePositions.Add(new Position(4, -6));
                    piecePositions.Add(new Position(4, -5));

                    piecePositions.Add(new Position(3, -7));
                    piecePositions.Add(new Position(3, -6));
                    piecePositions.Add(new Position(3, -5));

                    piecePositions.Add(new Position(2, -6));
                    piecePositions.Add(new Position(2, -5));
                    piecePositions.Add(new Position(1, -5));

                    pieceStartPositions.Add(Piece.Cyan, piecePositions);
                    break;

                // Green start
                case Piece.Green:
                    piecePositions = new List<Position>()
                    {
                        new Position(5, -1),
                        new Position(5, -2),
                        new Position(5, -3),
                        new Position(5, -4),

                        new Position(6, -2),
                        new Position(6, -3),
                        new Position(6, -4),

                        new Position(7, -4),
                        new Position(7, -3),
                        new Position(8, -4)
                    };

                    pieceStartPositions.Add(Piece.Green, piecePositions);
                    break;
            
                // Blue start
                case Piece.Blue:
                    piecePositions = new List<Position>()
                    {
                        new Position(-4, -1),
                        new Position(-4, -2),
                        new Position(-4, -3), 
                        new Position(-4, -4),

                        new Position(-3, -2),
                        new Position(-3, -3),
                        new Position(-3, -4),

                        new Position(-2, -3),
                        new Position(-2, -4),
                        new Position(-1, -4)
                    };

                    pieceStartPositions.Add(Piece.Blue, piecePositions);
                    break;

                // Magenta start
                case Piece.Magenta:
                    piecePositions = new List<Position>()
                    {
                        new Position(-5, 1),
                        new Position(-5, 2),
                        new Position(-5, 3),
                        new Position(-5, 4),

                        new Position(-6, 2),
                        new Position(-6, 3),
                        new Position(-6, 4),

                        new Position(-7, 3),
                        new Position(-7, 4),
                        new Position(-8, 4)
                    };

                    pieceStartPositions.Add(Piece.Magenta, piecePositions);
                    break;

                // Yellow start
                case Piece.Yellow:
                    piecePositions = new List<Position>()
                    {
                        new Position(4, 1), 
                        new Position(4, 2), 
                        new Position(4, 3), 
                        new Position(4, 4),

                        new Position(3, 2), 
                        new Position(3, 3), 
                        new Position(3, 4),

                        new Position(2, 3),
                        new Position(2, 4),
                        new Position(1, 4)
                    };

                    pieceStartPositions.Add(Piece.Yellow, piecePositions);
                    break;
            }
        }
    }
}
