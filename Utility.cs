using System;
using UnityEngine;

// A *Position* is where a piece is in terms of the game board
// A *Coordinate* is the pixel coordinates on the screen.
// We rename Vector2Int and Vector2 for convenience
using Position = UnityEngine.Vector2Int;
using Coordinate = UnityEngine.Vector2;

public class Utility
{
  // The board representation is expected to be scaled so that each hexagon has a diameter of 1
  // and the origin of the board is a the centre of the window.

  // These are used to convert a position to coordinates.
  private const float deltaX = 0.5f;
  private const float deltaY = 0.8660254f; // √3/2

  // The board is at the lowest level, the pieces placed above it.  Moving pieces are above static pieces.
  public const float boardLevel = 0.0f;
  public const float pieceLevel = -1.0f;
  public const float movingPieceLevel = -2.0f;

  // Convert a board position into world coordinates
  public static Coordinate PositionToCoordinates(Position pos)
  {
    return (new Coordinate(pos.x + deltaX * pos.y, pos.y * deltaY));
  }

  // Convert world coordinates into a matching board position
  // The coordinate system sets the centre hexagon as (0, 0);
  // the x axis goes from left to right; the y axis goes at 60° from left to right.
  public static Position CoordinatesToPosition(Coordinate coords)
  {
    int y = Mathf.FloorToInt((coords.y + deltaY / 2.0f) / deltaY);
    int x = Mathf.FloorToInt((coords.x + deltaX) - (deltaX * y));

    return new Position(x, y);
  }

  public static T[,] MakeMatrix<T>(int xMin, int yMin, int xMax, int yMax)
  {
    return (T[,])Array.CreateInstance(typeof(T), new int[] { xMax - xMin + 1, yMax - yMin + 1 }, new int[] { xMin, yMin });
  }
}