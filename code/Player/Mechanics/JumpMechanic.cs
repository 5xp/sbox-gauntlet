namespace Tf;

/// <summary>
/// A jumping mechanic.
/// </summary>
public partial class JumpMechanic : BasePlayerControllerMechanic
{
	public int AirJumpsRemaining { get; set; }

	private TimeSince TimeSinceGroundJump { get; set; }
	private TimeSince TimeSinceAirJump { get; set; }
	private TimeSince TimeSinceWallJump { get; set; }

	public override bool ShouldBecomeActive()
	{
		if ( !Input.Pressed( "Jump" ) ) return false;

		if ( !PlayerSettings.CanJumpWhileUnducking )
		{
			if ( Controller.DuckFraction > 0f && !HasTag( "crouch" ) )
				return false;
		}

		bool canJump = ShouldGroundJump() || ShouldAirJump() || ShouldWallJump();

		if ( !canJump ) return false;

		return true;
	}

	public override IEnumerable<string> GetTags()
	{
		if ( TimeSinceGroundJump == 0 )
			yield return "jump";

		if ( TimeSinceAirJump == 0 )
			yield return "airjump";

		if ( TimeSinceWallJump == 0 )
			yield return "walljump";
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		if ( !after ) return;

		if ( ShouldGroundJump() )
		{
			DoGroundJump();
		}
		else if ( ShouldWallJump() )
		{
			DoWallJump();
		}
		else if ( ShouldAirJump() )
		{
			DoAirJump();
		}

		Controller.ApexHeight = Position.z;
	}

	public override void Simulate()
	{
		if ( Controller.IsGrounded ) return;
		if ( !DidPressMovementKey() ) return;
		RedirectVelocity( TimeSinceStart );
	}

	private bool ShouldAirJump()
	{
		return AirJumpsRemaining > 0;
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
		TimeSinceAirJump = 0;
	}

	private bool ShouldWallJump()
	{
		return Controller.GetMechanic<WallrunMechanic>().IsActive || RecentlyFellAwayFromWall();
	}

	/// <summary>
	/// Called when walljumping. Adds upward velocity, outward velocity, and input direction velocity
	/// </summary>
	private void DoWallJump()
	{
		float upSpeed = PlayerSettings.WallrunJumpUpSpeed;
		float outSpeed = PlayerSettings.WallrunJumpOutwardSpeed;
		float inputDirSpeed = PlayerSettings.WallrunJumpInputDirSpeed;

		Vector3 wallNormal = Controller.GetMechanic<WallrunMechanic>().LastWallNormal;
		Vector3 wishDir = Controller.BuildWishDir();

		float lookingNormalAmount = Controller.EyeAngles.WithPitch( 0f ).Forward.Dot( wallNormal );
		bool tryingToClimbInwards = lookingNormalAmount < -0.71f && Controller.WishMove.x > 0f;

		if ( wishDir.Dot( wallNormal ) < 0f )
		{
			wishDir = Vector3.VectorPlaneProject( wishDir, wallNormal );
		}

		if ( tryingToClimbInwards )
		{
			outSpeed *= 0.2f;
		}

		float startZ = Velocity.z;

		// Don't add any speed if we are already moving up fast enough
		// Or give extra speed if we're falling
		upSpeed = (upSpeed - startZ).Clamp( 0f, upSpeed * 1.47f );

		float maxOutSpeed = MathF.Max( outSpeed, inputDirSpeed );

		Vector3 addVelocity = Vector3.Up * upSpeed + wallNormal * outSpeed + wishDir * inputDirSpeed;
		addVelocity = addVelocity.ClampLengthOnAxis( wallNormal, maxOutSpeed );

		Velocity += addVelocity;
		TimeSinceWallJump = 0;
	}

	private bool ShouldGroundJump()
	{
		return Controller.IsGrounded || RecentlyLeftGround();
	}

	private void DoGroundJump()
	{
		Controller.ClearGroundObject();

		float startZ = Velocity.z;

		float jumpHeight = HasTag( "slide" ) ? PlayerSettings.SlideJumpHeight : PlayerSettings.JumpHeight;

		bool isFullyDucked = Controller.DuckFraction.AlmostEqual( 1f );

		Vector3 horzVelocity = HorzVelocity;

		SlideMechanic slide = Controller.GetMechanic<SlideMechanic>();

		if ( !isFullyDucked && HasTag( "crouch" ) )
		{
			Controller.DuckFraction = 0f;

			// If we try jumping before we're fully crouching, refund our speed boost
			if ( slide.IsActive && slide.UsedBoost )
			{
				float boostAmount = slide.GetSpeedBoost();

				horzVelocity = horzVelocity.Approach( 0f, boostAmount );

				Controller.DuckFraction = 1f;
			}
		}

		// Round our speed if near the threshold when slideboostjumping
		if ( HasTag( "slide" ) && slide.UsedBoost )
		{
			float speed = horzVelocity.Length;

			if ( speed >= PlayerSettings.SlideForceSlideSpeed - 50f && speed <= PlayerSettings.SlideForceSlideSpeed + 5f )
			{
				horzVelocity = horzVelocity.Normal * PlayerSettings.SlideForceSlideSpeed;
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
		TimeSinceGroundJump = 0;
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
		Vector3 wishDir = Controller.BuildWishDir();

		if ( wishDir.AlmostEqual( 0f ) )
			return;

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
		Vector3 wishDir = Controller.BuildWishDir();

		if ( wishDir.AlmostEqual( 0f ) )
			return;

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

	/// <summary>
	/// Returns true if we recently fell away from a wall within the allowed grace period.
	/// Will not return true if we just walljumped or didn't fall away.
	/// </summary>
	/// <returns></returns>
	private bool RecentlyFellAwayFromWall()
	{
		WallrunMechanic wallrun = Controller.GetMechanic<WallrunMechanic>();

		return wallrun.TimeSinceFellAwayFromWall <= PlayerSettings.JumpGracePeriod && TimeSinceLastStart > wallrun.TimeSinceFellAwayFromWall;
	}

	public void RefreshAirJumps()
	{
		AirJumpsRemaining = PlayerSettings.AirJumpMaxJumps;
	}
}
