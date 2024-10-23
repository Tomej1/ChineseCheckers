using System.Collections.Generic;
using UnityEngine;

using Position = UnityEngine.Vector2Int;

// Interfaces that describe the communication between the Model and the Views/Controllers.
// Certain enhancements may require extending the interfaces, do so carefully.

public interface IBoardModel
{
  void AddListener(IBoardListener listener);  // Add the listener to the list
  void StartGame(int numPlayers);             // A new game with numPlayers players will begin
  void SetDifficulty(int difficulty);         // Set the difficulty level of the game
  Piece GetPiece(Position pos);               // Get the piece at pos
  void MakeMove(Player player);
  bool MovePiece(Position startPos, Position endPos);      // Ask to move the piece at position (startX, startY) to (endX, endY).  Returns true if successful.
  void SaveGame();  // Save the current game
  void LoadGame();  // Load a previous game
}

public interface IBoardListener
{
  void NewGame(List<Player> players);  // Restart the game
  void SetNewWinner(Player player);     // player has finished the game
  void PlacePiece(Position pos, Piece piece);  // Put piece at position (pos.x, pos.y).
  void MovePiece(Position startPos, Position endPos);     // Move the piece at position (startPos.x, startPos.y) to (endPos.x, endPos.y).
}
