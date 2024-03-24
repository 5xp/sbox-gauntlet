namespace Gauntlet.Player.Mechanics;

public class MoveMechanic : BasePlayerControllerMechanic
{
	public override bool ShouldBecomeActive() => true;
	private TimeUntil _timeUntilStep = 0;
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
		else if ( Controller.GetMechanic<WallrunMechanic>() is { WallNormal: not null } wallrun )
		{
			wallrun.WallrunMove( wallrun.WallNormal.Value );
			wallrun.CategorizePosition();
		}
		else
		{
			AirMoveMechanic airMove = Controller.GetMechanic<AirMoveMechanic>();
			airMove.AirMove();
			Controller.CategorizePosition( Controller.IsGrounded );
		}

		Controller.LastVelocity = Velocity;
	}

	private void UpdateFootSteps()
	{
		if ( _timeUntilStep > 0 )
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
		return HasTag( "crouch" )
			? (PlayerSettings.FootStepDuckWalkSpeed, PlayerSettings.FootStepDuckRunSpeed)
			: (PlayerSettings.FootStepNormalWalkSpeed, PlayerSettings.FootStepNormalRunSpeed);
	}

	private void SetFootStepTime( bool isWalking )
	{
		_timeUntilStep = isWalking ? PlayerSettings.FootStepWalkInterval : PlayerSettings.FootStepSprintInterval;

		if ( HasTag( "crouch" ) )
		{
			_timeUntilStep += PlayerSettings.FootStepDuckIntervalAdd;
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
