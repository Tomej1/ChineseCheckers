using System.Collections.Generic;
using UnityEngine;

using Position = UnityEngine.Vector2Int;
using Coordinate = UnityEngine.Vector2;

// This is the Controller, it reads the player’s interaction with the board,
// and asks the Model to update itself when needed.

public class BoardController : MonoBehaviour, IBoardListener
{
  [SerializeField]
  private GameObject board;  // Set in the Inspector, the visual representation of the board

  private readonly IBoardModel boardModel = BoardModel.Instance();  // A reference to the board Model
  private BoardView boardView;    // A reference to the board View, which has to be told updates
  private GameObject movingPiece; // A piece that has been grabbed by the player
  private Position startPosition;  // The starting position of a grab
  private Coordinate grabOffset;   // The grab will probably not be exactly at the centre of the piece, so we use the offset to avoid sudden jerks in the animation.
  private List<Player> players;    // The players in this game
  private int currentPlayerIndex = -1;   // An index into the list of players 
  private bool gameOver = false;    // When the next to last player has moved into the opposite nest, the game is over.
  private bool updated = false;

  // If the game is restarted, we reset the player index
  public void NewGame(List<Player> players)
  {
    this.players = players;
    currentPlayerIndex = 0;
    gameOver = false;
  }

  // If there is only a single player who hasn’t reached their nest, the game is over.
  public void SetNewWinner(Player player)
  {
    players.Remove(player);
    if (players.Count <= 1)
      gameOver = true;
    updated = true;
  }

  // Required by IBoardListener, but we don’t do anything with them in this class.
  public void PlacePiece(Position pos, Piece piece) { }
  public void MovePiece(Position startPos, Position endPos) { }

  void Start()
  {
    boardModel.AddListener(this);
    // The view and controller work in pairs and know of each other
    boardView = board.GetComponent<BoardView>();
  }

  public void Update()
  {
    if (gameOver || currentPlayerIndex < 0 || players[currentPlayerIndex].Human()) // If current player is human, no automatic move
      return;

    // Since, as noted below, MakeMove will lock the screen until it’s finished, this is a kludge to display updated text fields,
    // by making sure there will be at least one call to Update before MakeMove is executed.
    if (updated)
    {
      updated = false;
      return;
    }

    // MakeMove can take considerable time, it could be turned into a coroutine with the appropriate changes and
    // let other events update the screen, such as pressing Quit or Save.
    boardModel.MakeMove(players[currentPlayerIndex]);  // Let this automatic player make its move
    AdvancePlayer();
  }

  // Check if the player has grabbed one of their own pieces, if so, prepare for dragging it
  void OnMouseDown()
  {
    Coordinate mouseCoords = Camera.main.ScreenToWorldPoint(Input.mousePosition);

    startPosition = Utility.CoordinatesToPosition(mouseCoords);
    if (boardModel.GetPiece(startPosition) != players[currentPlayerIndex].Value())
      return;

    Coordinate originalCoords = Utility.PositionToCoordinates(startPosition);
    grabOffset = originalCoords - mouseCoords;
    movingPiece = boardView.GetPiece(startPosition);
  }

  void OnMouseDrag()
  {
    if (gameOver || !movingPiece)
      return;

    Coordinate mouseCoords = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    // Moving piece floating above the others
    movingPiece.transform.position = new Vector3(mouseCoords.x + grabOffset.x, mouseCoords.y + grabOffset.y, Utility.movingPieceLevel);
  }

  // The player releases the piece, 
  void OnMouseUp()
  {
    if (gameOver || !movingPiece)
      return;

    Position endPosition = Utility.CoordinatesToPosition(Camera.main.ScreenToWorldPoint(Input.mousePosition));
    Coordinate endCoords;

    // If the drag ends on a permissible position, move the piece there and
    // hand over to the next player to make a move, otherwise return the piece to the starting position.  
    if (boardModel.MovePiece(startPosition, endPosition))
    {
      endCoords = Utility.PositionToCoordinates(endPosition);
      AdvancePlayer();
    }
    else
    {
      endCoords = Utility.PositionToCoordinates(startPosition);
    }

    movingPiece.transform.position = new Vector3(endCoords.x, endCoords.y, Utility.pieceLevel);
    movingPiece = null;
  }

  // Let the next player do their update. If human, by dragging; if automatic, in the Update method.
  private void AdvancePlayer()
  {
    currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
    boardView.SetCurrentPlayer(players[currentPlayerIndex]);
    updated = true;
  }
    // Advancing player while keeping the AdvancePlayer method private
    // This will be connected to a Endturn button
    public void EndTurn()
    {
        AdvancePlayer();
    }

}
