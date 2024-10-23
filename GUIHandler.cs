using UnityEngine;

// GUIHandler is the recipient of events from any GUI elements,
// it calls the appropriate objects.
public class GUIHandler : MonoBehaviour {

  private int numPlayers = 2;  // Default number of players
  private readonly int[] menuConversion = { 2, 3, 4, 6 };  // Convert from the value given by the Dropdown element
  private readonly IBoardModel board = BoardModel.Instance();  // We need a reference to the board Model.

  // Select the number of players
  // With the current GUI only one will be selected to be a human,
  // but with checkboxes it would be possible to indicate which players are human and which are computer-operated.
  public void SetNumPlayers(int menuItem)
  {
    numPlayers = menuConversion[menuItem];
  }

  // Tell the Model that a game should start.
	public void StartGame() {
    board.StartGame(numPlayers);
	}

  // Quit the game
  public void QuitGame()
  {
    Application.Quit();
  }

  // Ask the board to save its state
  // Note that we get a boolean value in return, letting us know if the save
  // was successful.  This should be handled.
	public void SaveGame() {
    if (board != null)
      board.SaveGame();
	}

  // Ask for a previously saved game to be restored.
  // We get the restored board in return.
  // The Listeners have to be told of this.
	public void LoadGame() {
    if (board != null)
      board.LoadGame();
	}

  // Show a help page to the player
	public void Help() {
		
	}

}
