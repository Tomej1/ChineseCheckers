using Minimax;

// Utility types for describing pieces and placing them on the board.

public enum Piece { Red, Yellow, Green, Cyan, Blue, Magenta, Empty, Invalid, ColourMin = Piece.Red, ColourMax = Piece.Empty };

// A player is represented by pieces in a given colour, a player may be either human or machine
public struct Player : IPlayer
{
  private readonly Piece piece;
  private readonly bool human;

  public Player(Piece piece, bool human)
  {
    this.piece = piece;
    this.human = human;
  }

  public Piece Value()
  {
    return (piece);
  }

  public bool Human()
  {
    return (human);
  }
}

static class PieceInfo
{
  // This can be extended to multiple languages and read from a config file
  public static string[] pieceNames = { "Red", "Yellow", "Green", "Cyan", "Blue", "Magenta" };

  // A list of which colour is opposite of a given other colour
  public static readonly Piece[] opposites = { Piece.Cyan, Piece.Blue, Piece.Magenta, Piece.Red, Piece.Yellow, Piece.Green };

  // What pieces are included for a given number of players
  public static readonly Piece[][] setups = {
        new Piece[0],  // 0 players
        new Piece[0],  // 1 player
        new Piece[2]{ Piece.Red, Piece.Cyan },  // 2 players
        new Piece[3]{ Piece.Red, Piece.Green, Piece.Blue }, // 3 players
        new Piece[4]{ Piece.Red, Piece.Green, Piece.Cyan, Piece.Magenta }, // 4 players
        new Piece[0],  // 5 players
        new Piece[6]{ Piece.Red, Piece.Yellow, Piece.Green, Piece.Cyan, Piece.Blue, Piece.Magenta }  // 6 players
    };

  // How many pieces are involved for a given number of players
  public static readonly int[] numPieces = { 0, 0, 15, 10, 10, 0, 10 };

}