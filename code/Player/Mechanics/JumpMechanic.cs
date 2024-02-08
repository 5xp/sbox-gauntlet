namespace Tf;

/// <summary>
/// A jumping mechanic.
/// </summary>
public partial class JumpMechanic : BasePlayerControllerMechanic
{
	public int AirJumpsRemaining { get; set; }

	public override bool ShouldBecomeActive()
	{
		if ( !Input.Pressed( "Jump" ) ) return false;

		bool shouldJump = Controller.IsGrounded || AirJumpsRemaining > 0;

		shouldJump |= RecentlyLeftGround();

		if ( !shouldJump ) return false;
		return true;
	}

	public override IEnumerable<string> GetTags()
	{
		yield return "jump";
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		if ( !after ) return;

		if ( Controller.IsGrounded || RecentlyLeftGround() )
		{
			DoGroundJump();
		}
		else if ( AirJumpsRemaining > 0 )
		{
			DoAirJump();
		}
	}

	public override void Simulate()
	{
		if ( Controller.IsGrounded ) return;
		if ( !DidPressMovementKey() ) return;
		RedirectVelocity( TimeSinceStart );
	}

	private void DoAirJump()
	{
		float startZ = Velocity.z;

		float minUpSpeed = MathF.Sqrt( 2f * PlayerSettings.AirJumpHeight * PlayerSettings.Gravity );

		// Still give some jump speed even if we're already moving up fast
		float speedDiff = MathF.Max( minUpSpeed - startZ, minUpSpeed * PlayerSettings.AirJumpMinHeightFraction );

		Velocity += Vector3.Up * speedDiff;
		AirJumpsRemaining--;
		RedirectVelocity();
	}

	private void DoGroundJump()
	{
		Controller.ClearGroundObject();

		float startZ = Velocity.z;

		float jumpHeight = HasTag( "slide" ) ? PlayerSettings.SlideJumpHeight : PlayerSettings.JumpHeight;

		bool isFullyDucked = Controller.DuckFraction.AlmostEqual( 1f );

		Vector3 horzVelocity = HorzVelocity;

		if ( !isFullyDucked && HasTag( "crouch" ) )
		{
			Controller.DuckFraction = 0f;

			// If we try jumping before we're fully crouching, refund our speed boost
			SlideMechanic slide = Controller.GetMechanic<SlideMechanic>();

			if ( slide.IsActive && slide.UsedBoost )
			{
				Velocity = Velocity.Normal * slide.StartSpeed;

				Controller.DuckFraction = 1f;
			}
		}

		if ( IsSkipping() )
		{
			jumpHeight *= PlayerSettings.SkipJumpHeightFraction;

			if ( horzVelocity.LengthSquared > PlayerSettings.SkipSpeedRetain * PlayerSettings.SkipSpeedRetain )
			{
				float newSpeed = MathF.Max( horzVelocity.Length - PlayerSettings.SkipSpeedReduce, PlayerSettings.SkipSpeedRetain );
				horzVelocity = horzVelocity.Normal * newSpeed;
			}
		}

		float upSpeed = MathF.Sqrt( 2f * jumpHeight * PlayerSettings.Gravity );
		Velocity = horzVelocity.WithZ( startZ + upSpeed );

		Controller.BroadcastPlayerJumped();
		RefreshAirJumps();
	}

	/// <summary>
	/// Handles how velocity should be redirected when airjumping or during keyboard grace period
	/// </summary>
	/// <param name="inputDir">Player's non-zero input direction to interpolate towards</param>
	/// <param name="velocity"></param>
	/// <param name="max">The maximum allowed velocity change</param>
	/// <param name="strength">Fraction of change towards the input direction</param>
	/// <param name="speedOverride"></param>
	private void RedirectVelocity( Vector3 inputDir, Vector3 velocity, float max, float strength, float? speedOverride = null )
	{
		float speed = speedOverride ?? velocity.Length;

		Vector3 velocityPrime = Vector3.Lerp( velocity, inputDir * speed, strength ).Normal * speed;
		Vector3 velocityChange = (velocityPrime - velocity).ClampLength( max );

		Velocity += velocityChange;
	}

	/// <summary>
	/// Called when airjumping
	/// </summary>
	private void RedirectVelocity()
	{
		Vector3 wishVel = Controller.BuildWishVelocity();

		if ( wishVel.AlmostEqual( 0f ) )
			return;

		Vector3 wishDir = wishVel.Normal;
		Vector3 horzVelocity = HorzVelocity;
		float airJumpHorzSpeed = PlayerSettings.AirJumpHorizontalSpeed;

		float speed = MathF.Min( HorzVelocity.Length, airJumpHorzSpeed );
		float max = 2f * airJumpHorzSpeed;

		RedirectVelocity( wishDir, horzVelocity.Normal * speed, max, 1f, airJumpHorzSpeed );
	}

	/// <summary>
	/// Called when inputting a strafe during the keyboard grace period
	/// </summary>
	/// <param name="timeSinceJump"></param>
	private void RedirectVelocity( float timeSinceJump )
	{
		Vector3 wishVel = Controller.BuildWishVelocity();

		if ( wishVel.AlmostEqual( 0f ) )
			return;

		Vector3 wishDir = wishVel.Normal;

		float max = PlayerSettings.JumpKeyboardGraceMax * PlayerSettings.SprintSpeed;
		float strength = PlayerSettings.JumpKeyboardGraceStrength * GetKeyboardGraceFraction( timeSinceJump );
		RedirectVelocity( wishDir, HorzVelocity, max, strength );
	}

	/// <summary>
	/// Returns the amount of keyboard grace given how long since we jumped
	/// </summary>
	/// <param name="timeSinceJump"></param>
	/// <returns>1 if t less than periodMin, 0 if t greater than periodMax, and linear decrease in between</returns>
	private float GetKeyboardGraceFraction( float timeSinceJump )
	{
		float range = PlayerSettings.JumpKeyboardGracePeriodMax - PlayerSettings.JumpKeyboardGracePeriodMin;
		float keyboardGraceFrac = (timeSinceJump - PlayerSettings.JumpKeyboardGracePeriodMin) / range;
		return 1f - keyboardGraceFrac.Clamp( 0f, 1f );
	}

	/// <summary>
	/// We are considered skipping after a short duration after landing. This will reduce our speed and reduce our jump height
	/// </summary>
	/// <returns></returns>
	private bool IsSkipping()
	{
		return Controller.TimeSinceLastLanding <= PlayerSettings.SkipTime;
	}

	private static bool DidPressMovementKey()
	{
		return Input.Pressed( "Forward" ) || Input.Pressed( "Backward" ) || Input.Pressed( "Left" ) || Input.Pressed( "Right" );
	}

	/// <summary>
	/// Returns true if we recently walked off a ledge within the allowed coyote time.
	/// Will not return true if we just jumped.
	/// </summary>
	/// <returns></returns>
	private bool RecentlyLeftGround()
	{
		return Controller.TimeSinceLastOnGround <= PlayerSettings.JumpGracePeriod && TimeSinceLastStart > Controller.TimeSinceLastOnGround;
	}

	public void RefreshAirJumps()
	{
		AirJumpsRemaining = PlayerSettings.AirJumpMaxJumps;
	}
}
