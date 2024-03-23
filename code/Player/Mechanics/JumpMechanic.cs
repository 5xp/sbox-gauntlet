namespace Gauntlet;

/// <summary>
/// A jumping mechanic.
/// </summary>
public partial class JumpMechanic : BasePlayerControllerMechanic
{
	public int AirJumpsRemaining { get; set; }

	private TimeSince TimeSinceGroundJump { get; set; }
	private TimeSince TimeSinceAirJump { get; set; }
	private TimeSince TimeSinceWallJump { get; set; }
	private TimeSince TimeSinceJumpBuffered { get; set; }
	private bool JumpBuffered { get; set; }
	private SoundHandle JumpSoundHandle { get; set; }

	public override int Priority => 1;

	protected override void OnStart()
	{
		Controller.OnLanded += OnLanded;
	}

	public override bool ShouldBecomeActive()
	{
		if ( !JumpBuffered ) return false;

		if ( !PlayerSettings.CanJumpWhileUnducking )
		{
			if ( Controller.DuckFraction > 0f && !HasTag( "crouch" ) )
				return false;
		}

		bool canJump = ShouldGroundJump() || (ShouldAirJump() && !ShouldDiscardAirJump()) || ShouldWallJump();

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

		JumpBuffered = false;

		JumpType jumpType = JumpType.Ground;

		if ( ShouldGroundJump() )
		{
			DoGroundJump();
			jumpType = JumpType.Ground;
		}
		else if ( ShouldWallJump() )
		{
			DoWallJump();
			jumpType = JumpType.Wall;
		}
		else if ( ShouldAirJump() )
		{
			DoAirJump();
			jumpType = JumpType.Air;
		}

		Vector3 halfGravity = Vector3.Down * 0.5f * PlayerSettings.Gravity * PlayerSettings.GravityScale * Time.Delta;
		Velocity += halfGravity;

		PlayJumpSound( jumpType );
		Controller.BroadcastPlayerJumped( jumpType );
	}

	public override void Simulate()
	{
		if ( TimeSinceJumpBuffered > GetJumpBufferTime() )
		{
			JumpBuffered = false;
		}

		if ( Input.Pressed( "Jump" ) )
		{
			JumpBuffered = true;
			TimeSinceJumpBuffered = 0;
		}

		FadeSounds();

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

	private float GetJumpBufferTime()
	{
		if ( DebugConVars.DebugOverrideJumpBufferTicks > -1 )
		{
			return DebugConVars.DebugOverrideJumpBufferTicks * Scene.FixedDelta;
		}

		return PlayerSettings.JumpBufferTime;
	}

	private bool ShouldWallJump()
	{
		return Controller.GetMechanic<WallrunMechanic>().WallNormal.HasValue || RecentlyFellAwayFromWall();
	}

	/// <summary>
	/// Returns true if we are about to touch the ground or a walljumpable wall.
	/// </summary>
	private bool ShouldDiscardAirJump()
	{
		if ( PredictGroundTouch( GetJumpBufferTime() ) )
			return true;

		if ( Controller.GetMechanic<WallrunMechanic>().PredictWallrun( GetJumpBufferTime() ).HasValue )
			return true;

		return false;
	}

	/// <summary>
	/// Called when walljumping. Adds upward velocity, outward velocity, and input direction velocity
	/// </summary>
	private void DoWallJump()
	{
		float upSpeed = PlayerSettings.WallrunJumpUpSpeed;
		float outSpeed = PlayerSettings.WallrunJumpOutwardSpeed;
		float inputDirSpeed = PlayerSettings.WallrunJumpInputDirSpeed;

		WallrunMechanic wallrun = Controller.GetMechanic<WallrunMechanic>();
		Vector3 wallNormal = wallrun.LastWallNormal;
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


		if ( wallrun.TimeSinceTouchedWall < 0.5f )
		{
			Controller.RecordWallJumpThisTick = true;
			Controller.AfterWallJumpSpeed = HorzVelocity.Length;
			Controller.WallJumpTimeDiff = wallrun.TimeSinceTouchedWall;
		}

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
		}

		// Round our speed if near the threshold when slideboostjumping
		if ( slide.IsActive && slide.UsedBoost )
		{
			float speed = horzVelocity.Length;

			if ( speed >= PlayerSettings.SlideForceSlideSpeed - 50f && speed <= PlayerSettings.SlideForceSlideSpeed )
			{
				horzVelocity = horzVelocity.Normal * PlayerSettings.SlideForceSlideSpeed;
			}
			else if ( !isFullyDucked )
			{
				float boostAmount = slide.GetSpeedBoost();

				horzVelocity = horzVelocity.Approach( 0f, boostAmount );

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

	private bool PredictGroundTouch( float timeStep )
	{
		SceneTraceResult tr = Controller.TraceWithVelocity( timeStep );

		if ( !tr.Hit )
		{
			return false;
		}

		return Controller.IsFloor( tr.Normal );
	}

	private void OnLanded()
	{
		RefreshAirJumps();
	}

	private void FadeSounds()
	{
		if ( !Controller.IsGrounded )
		{
			return;
		}

		JumpSoundHandle.FadeVolume( Time.Delta );
	}

	private void PlayJumpSound( JumpType jumpType )
	{
		if ( jumpType == JumpType.Air )
		{
			JumpSoundHandle = Sound.Play( Controller.AirJumpSound );
		}
		else
		{
			JumpSoundHandle = Sound.Play( Controller.JumpSound );
		}
	}

	public void RefreshAirJumps()
	{
		AirJumpsRemaining = PlayerSettings.AirJumpMaxJumps;
	}

	public enum JumpType
	{
		Ground,
		Air,
		Wall
	}
}
