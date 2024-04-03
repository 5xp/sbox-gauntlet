using Gauntlet.Utils;

namespace Gauntlet.Player.Mechanics;

/// <summary>
/// A sliding mechanic.
/// </summary>
public class SlideMechanic : BasePlayerControllerMechanic
{
	public bool UsedBoost { get; private set; } = false;
	private float SlideTiltFrac { get; set; }
	private Vector3 SlideTiltAxis { get; set; }
	private float SlideTiltLowerSpeedBound { get; set; }
	private TimeSince TimeSinceUsedBoost { get; set; }
	private float FovScaleTargetFraction { get; set; }
	private float FovScaleFraction { get; set; }
	private SoundHandle SlideSoundHandle { get; set; }
	private SoundHandle SlideBoostSoundHandle { get; set; }
	private SoundHandle SlideEndSoundHandle { get; set; }
	private float StartSpeed { get; set; }
	private bool SlidOffGround { get; set; }

	public override int Priority => 15;

	public override IEnumerable<string> GetTags()
	{
		yield return "slide";
	}

	public override float? GetAcceleration()
	{
		return 0f;
	}

	protected override void OnStart()
	{
		Controller.OnLanded += OnLanded;
	}

	public override bool ShouldBecomeActive()
	{
		if ( !Controller.GetMechanic<CrouchMechanic>().ShouldBecomeActive() ) return false;
		if ( HasAnyTag( "wallrun", "jump", "airjump" ) ) return false;

		// Can't initiate a slide in the air.
		if ( !IsActive && !Controller.IsGrounded ) return false;

		// Are we above the required start speed to initiate a slide?
		float requiredSpeed = PlayerSettings.SlideRequiredStartSpeed;
		if ( !IsActive && HorzVelocity.LengthSquared < requiredSpeed * requiredSpeed ) return false;

		// Has our speed dipped too low?
		float minSpeed = PlayerSettings.SlideEndSpeed;
		if ( IsActive && Controller.IsGrounded && HorzVelocity.LengthSquared < minSpeed * minSpeed ) return false;

		bool startedFromAir = Controller.TimeSinceLastLanding <= Time.Delta;

		if ( !IsActive && !startedFromAir &&
		     Controller.BuildWishDir().Dot( Velocity.Normal ) < PlayerSettings.SlideMaxAngleDot ) return false;

		return true;
	}

	public override void OnActiveUpdate()
	{
		if ( !Controller.IsGrounded )
		{
			if ( !SlidOffGround )
			{
				SlideEndSoundHandle = Sound.Play( Controller.SlideEndSound );
				SlideEndSoundHandle.Volume *= 0.4f;
			}

			SlidOffGround = true;

			return;
		}

		// We are trying to unduck
		float forceSpeed = PlayerSettings.SlideForceSlideSpeed;
		float decelAmount = PlayerSettings.SlideDecel;

		if ( !Common.DuckDown() && HorzVelocity.LengthSquared > forceSpeed * forceSpeed )
		{
			// This should somehow depend on SlideVelocityDecay and current speed but I can't figure it out
			decelAmount += PlayerSettings.SlideForceSlideUnduckDecel;
		}

		float decayAmount = MathF.Min( 1f, PlayerSettings.SlideVelocityDecay );
		Velocity *= MathF.Pow( decayAmount, Time.Delta );
		Velocity = Velocity.Approach( 0f, decelAmount * Time.Delta );

		// Let the player apply a braking effect if they want
		Vector3 wishDir = Controller.BuildWishDir();
		float wishSpeed = Controller.GetWishSpeed();

		Decelerate( wishDir, wishSpeed, PlayerSettings.Acceleration * 0.2f );
		ApplyGravity();
	}

	public override void FrameSimulate()
	{
		ApplySlideTilt();
		ApplyFovScale();
	}

	public override void Simulate()
	{
		FadeSounds();
	}

	private void OnLanded()
	{
		// While already sliding, we landed on the ground again, so we want to redo slide
		if ( IsActive )
		{
			OnSlideStart();
		}
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		if ( after )
		{
			OnSlideStart();
		}
		else
		{
			OnSlideStop();
		}
	}

	private void OnSlideStart()
	{
		UsedBoost = false;
		SlidOffGround = false;
		StartSpeed = Velocity.Length;

		if ( TimeSinceLastStart >= PlayerSettings.SlideBoostCooldown )
		{
			float speedBoost = GetSpeedBoost();

			Vector3 dir = HorzVelocity.Normal;
			Velocity += dir * speedBoost;

			SlideTiltAxis = dir;
			TimeSinceUsedBoost = 0;
			UsedBoost = true;
			FovScaleTargetFraction = 1f;

			SlideBoostSoundHandle = Sound.Play( Controller.SlideBoostSound );
		}

		if ( SlideTiltFrac.AlmostEqual( 0f ) )
		{
			SlideTiltAxis = HorzVelocity.Normal;
		}

		// Don't scale FOV if we're skipping
		if ( Controller.TimeSinceLastLanding > PlayerSettings.SkipTime )
		{
			FovScaleTargetFraction = 1f;
		}

		SlideTiltLowerSpeedBound = PlayerSettings.SlideEndSpeed;

		if ( SlideSoundHandle.IsValid() )
		{
			SlideSoundHandle.Stop();
		}

		SlideSoundHandle = Sound.Play( Controller.SlideSound );
	}

	private void OnSlideStop()
	{
		FovScaleTargetFraction = 0f;
		SlideTiltLowerSpeedBound = PlayerSettings.SlideRequiredStartSpeed;

		if ( Controller.IsGrounded )
		{
			SlideEndSoundHandle = Sound.Play( Controller.SlideEndSound );
		}
	}

	private void FadeSounds()
	{
		if ( !Controller.IsGrounded || !IsActive )
		{
			SlideSoundHandle.FadeVolume( 0.5f * Time.Delta );
		}

		SlideBoostSoundHandle.FadeVolume( 0.2f * Time.Delta );
	}

	public float GetSpeedBoost()
	{
		float speedDifference = PlayerSettings.SlideSpeedBoostCap - StartSpeed;
		float speedBoost = speedDifference.Clamp( 0f, PlayerSettings.SlideSpeedBoost );
		return speedBoost;
	}

	/// <summary>
	/// We don't want the player to be able to steer much, but they should be able to brake if they want
	/// </summary>
	private void Decelerate( Vector3 wishDir, float wishSpeed, float deceleration )
	{
		float currentSpeed = Velocity.Dot( wishDir );
		float addSpeed = wishSpeed - currentSpeed;

		// Scale down the perpendicular component
		Vector3 proj = Velocity.Normal.Dot( wishDir ) * Velocity.Normal;
		Vector3 perpendicular = wishDir - proj;
		wishDir = proj + perpendicular * 0.1f;

		if ( addSpeed <= 0f )
		{
			return;
		}

		float magnitudeSquared = MathF.Max( wishSpeed * wishSpeed, Velocity.LengthSquared );

		float accelSpeed = MathF.Min( deceleration * Time.Delta, addSpeed );
		Velocity += wishDir * accelSpeed;

		Velocity = Velocity.ClampLength( MathF.Sqrt( magnitudeSquared ) );
	}

	/// <summary>
	/// Apply camera tilt when we're looking to the side
	/// </summary>
	private void ApplySlideTilt()
	{
		float speed = HorzVelocity.Length;

		// More tilt if we are at a higher speed
		float speedFraction = speed.LerpInverse( SlideTiltLowerSpeedBound, PlayerSettings.SlideTiltPlayerSpeed );

		// If got a boost, lerp the tilt axis
		if ( IsActive && TimeSinceActiveChanged.Equals( TimeSinceUsedBoost ) )
		{
			SlideTiltAxis = SlideTiltAxis.ApproachVector( HorzVelocity.Normal,
				PlayerSettings.SlideTiltIncreaseSpeed * Time.Delta );
		}

		Angles left = Controller.InputAngles.WithPitch( 0f );
		left.yaw += 90f;

		float angleFraction = SlideTiltAxis.Normal.Dot( left.Forward );

		float approachTowards = IsActive ? speedFraction : 0f;

		float approachSpeed = IsActive && SlideTiltFrac < speedFraction
			? PlayerSettings.SlideTiltIncreaseSpeed
			: PlayerSettings.SlideTiltDecreaseSpeed;

		SlideTiltFrac = SlideTiltFrac.Approach( approachTowards, approachSpeed * Time.Delta );

		float totalRoll = SlideTiltFrac * angleFraction * PlayerSettings.SlideTiltMaxRoll;

		Controller.InputAngles += new Angles( 0f, 0f, totalRoll );
	}

	/// <summary>
	/// When we start sliding, scale out the FOV, and when we stop, scale it back in
	/// </summary>
	private void ApplyFovScale()
	{
		float baseFov = Controller.CameraController.BaseFieldOfView;

		float lerpTime = FovScaleTargetFraction == 0f
			? PlayerSettings.SlideFovLerpOutTime
			: PlayerSettings.SlideFovLerpInTime;
		FovScaleFraction = FovScaleFraction.Approach( FovScaleTargetFraction, 1f / lerpTime * Time.Delta );

		CameraComponent cam = Controller.CameraController.Camera;
		cam.FieldOfView = baseFov.LerpTo( baseFov * PlayerSettings.SlideFovScale, FovScaleFraction );
	}

	/// <summary>
	/// Gravity has no effect on moving on inclines, so we need to add it manually when sliding
	/// </summary>
	private void ApplyGravity()
	{
		Vector3 inclineComponent = Vector3.Up.ProjectOnNormal( Controller.GroundNormal );
		inclineComponent.z = 0f;
		inclineComponent *= Controller.PlayerGravity;
		Velocity += inclineComponent * Time.Delta;
	}
}
