namespace Tf;

public partial class MoveMechanic : BasePlayerControllerMechanic
{
  public override bool ShouldBecomeActive() => true;
  public TimeUntil TimeUntilStep = 0;
  public override int Priority => 9;

  public override void OnActiveUpdate()
  {
    UpdateFootSteps();

    if ( Controller.IsGrounded )
    {
      WalkMechanic walk = Controller.GetMechanic<WalkMechanic>();
      walk.WalkMove();
      Controller.CategorizePosition( true );
    }
    else if ( Controller.GetMechanic<WallrunMechanic>() is WallrunMechanic wallrun && wallrun.WallNormal.HasValue )
    {
      wallrun.WallrunMove();
      wallrun.CategorizePosition();
    }
    else
    {
      AirMoveMechanic airMove = Controller.GetMechanic<AirMoveMechanic>();
      airMove.AirMove();
      Controller.CategorizePosition( Controller.IsGrounded );
    }
  }

  private void UpdateFootSteps()
  {
    if ( TimeUntilStep > 0 )
    {
      return;
    }

    float speed = Velocity.Length;

    (float walkSpeed, float runSpeed) = GetStepSoundVelocities();

    bool movingFastEnough = speed >= walkSpeed;

    // If we're moving fast enough and on the ground or wallrunning, play a footstep sound.
    if ( !movingFastEnough || (!Controller.IsGrounded && !HasTag( "wallrun" )) || HasTag( "slide" ) )
    {
      return;
    }

    bool isWalking = speed < runSpeed;

    SetFootStepTime( isWalking );
    PlayFootStepSound( isWalking );
  }

  /// <summary>
  /// The walk speed represents the minimum speed required to play step sound.
  /// The run speed represents the minimum speed required to play the step sound at max speed.
  /// </summary>
  private (float walkSpeed, float runSpeed) GetStepSoundVelocities()
  {
    if ( HasTag( "crouch" ) )
    {
      return (PlayerSettings.FootStepDuckWalkSpeed, PlayerSettings.FootStepDuckRunSpeed);
    }

    return (PlayerSettings.FootStepNormalWalkSpeed, PlayerSettings.FootStepNormalRunSpeed);
  }

  private void SetFootStepTime( bool isWalking )
  {
    TimeUntilStep = isWalking ? PlayerSettings.FootStepWalkInterval : PlayerSettings.FootStepSprintInterval;

    if ( HasTag( "crouch" ) )
    {
      TimeUntilStep += PlayerSettings.FootStepDuckIntervalAdd;
    }
  }

  private void PlayFootStepSound( bool isWalking )
  {
    SoundEvent soundToPlay;

    if ( HasTag( "wallrun" ) )
    {
      soundToPlay = Controller.FootStepSoundWallrun;
    }
    else
    {
      soundToPlay = isWalking ? Controller.FootStepSoundWalk : Controller.FootStepSoundRun;
    }

    SoundHandle soundHandle = Sound.Play( soundToPlay );

    if ( HasTag( "crouch" ) )
    {
      soundHandle.Volume *= 0.65f;
    }
  }
}