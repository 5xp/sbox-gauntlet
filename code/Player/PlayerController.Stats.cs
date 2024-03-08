namespace Tf;

public partial class PlayerController
{
  public float PreWallTouchSpeed { get; set; }
  public bool RecordWallJumpThisTick { get; set; }
  public float AfterWallJumpSpeed { get; set; }
  public float WallJumpTimeDiff { get; set; }

  /// <summary>
  /// Called when the player walljumps within a certain time after touching the wall.
  /// The first float parameter is the difference in speed between the player's speed before touching the wall and after walljumping.
  /// The second float parameter is the time since the player touched the wall.
  /// </summary>
  public Action<float, float> OnFastWallJump;
}