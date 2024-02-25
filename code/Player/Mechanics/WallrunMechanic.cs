namespace Tf;

/// <summary>
/// A wallrun mechanic.
/// </summary>
public partial class WallrunMechanic : BasePlayerControllerMechanic
{
	public bool ShouldWallrun { get; set; } = false;

	private Vector3? _wallNormal;

	/// <summary>
	/// Our current wall normal. Will be null when not on the wall.
	/// </summary>
	public Vector3? WallNormal
	{
		get
		{
			if ( Controller.IsGrounded )
			{
				return null;
			}

			if ( !PlayerSettings.WallrunEnable )
			{
				return null;
			}

			return _wallNormal;
		}
		set
		{
			if ( value.HasValue )
			{
				LastWallNormal = value.Value;
			}

			_wallNormal = value;
		}
	}

	private Vector3? _targetWallNormal;

	/// <summary>
	/// Our target wall normal. Mostly only relevant when the wall is rotating. Set to null when not on the wall.
	/// </summary>
	private Vector3? TargetWallNormal
	{
		get
		{
			if ( !PlayerSettings.WallrunEnable )
			{
				return null;
			}

			if ( Controller.IsGrounded )
			{
				return null;
			}

			return _targetWallNormal;
		}
		set => _targetWallNormal = value;
	}


	/// <summary>
	/// Also our current wall normal, but will not be set to null when not on the wall.
	/// </summary>
	public Vector3 LastWallNormal { get; private set; }

	/// <summary>
	/// Our predicted wall normal.
	/// </summary>
	private Vector3? PredictedWallNormal { get; set; } = null;


	/// <summary>
	/// Gets set when we start wallrunning, then set to null when we touch the ground.
	/// </summary>
	private Vector3? LastWallrunStartPos { get; set; } = null;

	private bool HasBoost { get; set; } = false;

	private Vector3 TiltVector { get; set; } = Vector3.Zero;

	/// <summary>
	/// A wallrun is considered weak if we land on the same wall twice, or if we touch the wall without jumping (i.e. after walking off a ledge or after falling off a wall)
	/// </summary>
	private bool IsWallrunWeak { get; set; }

	/// <summary>
	/// Timer to keep track of how long we've been pushing away from the wall
	/// </summary>
	private TimeSince TimeSincePushingAway { get; set; }

	/// <summary>
	/// When the wall normal towards the target wall normal, we add it to this and then apply it our eyes to maintain the same relative angle.
	/// </summary>
	private Angles RelativeAnglesOffset { get; set; } = Angles.Zero;

	/// <summary>
	/// When we have a new target wall normal, this will be set to the 2 * the yaw angle difference between the current wall normal and the target wall normal.
	/// At minimum, our angle will correct at this speed. It can be more if our wall normal is rotating fast so that our view doesn't lag behind.
	/// </summary>
	private float RelativeAngleCorrectSpeed { get; set; }

	/// <summary>
	/// This gets set when we fall away the wall (running out of time, pushing away, ducking )
	/// </summary>
	public TimeSince TimeSinceFellAwayFromWall { get; set; }

	[ConVar( "debug_wallrun", Help = "Enables wallrun normal and wishdir gizmos in the scene camera" )]
	public static bool DebugWallrun { get; set; } = true;

	[ConVar( "debug_wallrun_settings", Help = "Changes various wallrun settings to aid in testing wall movement" )]
	public static bool DebugWallrunSettings { get; set; } = false;

	public override int Priority => 28;

	protected override void OnStart()
	{
		Controller.OnJump += OnJump;
		Controller.OnLanded += OnLanded;
	}

	public override IEnumerable<string> GetTags()
	{
		yield return "wallrun";
	}

	public override bool ShouldBecomeActive()
	{
		if ( !PlayerSettings.WallrunEnable ) return false;

		if ( Controller.IsGrounded ) return false;

		if ( !WallNormal.HasValue ) return false;

		return true;
	}

	public override void OnActiveUpdate()
	{
		if ( !WallNormal.HasValue )
		{
			return;
		}

		WallrunMove();

		CategorizePosition();

		if ( WallNormal.HasValue && TargetWallNormal.HasValue )
		{
			Vector3 originalWallNormal = WallNormal.Value;
			WallNormal = WallNormal.Value.RotateTowards( TargetWallNormal.Value, PlayerSettings.WallrunRotateMaxRate * Time.Delta );
			Angles angleDiff = WallNormal.Value.EulerAngles - originalWallNormal.EulerAngles;
			RelativeAnglesOffset += angleDiff;
		}
	}

	public override void Simulate()
	{
		if ( !IsActive && !Controller.IsGrounded )
		{
			PredictedWallNormal = PredictWallrun( PlayerSettings.WallrunTiltPredictTime );
		}
		else
		{
			PredictedWallNormal = null;
		}
	}

	public override void FrameSimulate()
	{
		ApplyWallrunTilt();
		CorrectView();
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		RelativeAngleCorrectSpeed = 0f;
		TimeSincePushingAway = 0;
		RelativeAnglesOffset = Angles.Zero;

		if ( !after )
		{
			ClearWallNormal();
			return;
		}

		Controller.GetMechanic<JumpMechanic>().RefreshAirJumps();
		LastWallrunStartPos = Position;
		ApplyBoost();
	}

	public void OnWallTouch( Vector3 wallNormal )
	{
		if ( IsActive )
		{
			return;
		}

		WallrunEligibility eligibility = IsWallEligibleForWallrun( Position, wallNormal );

		if ( eligibility == WallrunEligibility.Ineligible )
		{
			return;
		}

		IsWallrunWeak = eligibility.HasFlag( WallrunEligibility.Weak );
		UpdateWallNormal( wallNormal );
	}

	private void WallrunMove()
	{
		Vector3 wishDir = BuildWishDirection( WallNormal.Value );

		Vector3 halfGravity = 0.5f * Controller.GetPlayerGravity() * GetWallrunGravityScale() * Time.Delta;
		Velocity += halfGravity;

		Velocity = Vector3.VectorPlaneProject( Velocity, WallNormal.Value );

		float horzFriction, vertFriction;
		horzFriction = vertFriction = PlayerSettings.WallrunFriction;

		if ( Velocity.z < 0f )
		{
			vertFriction *= GetSlipScale();

			if ( wishDir.AlmostEqual( 0f ) )
				vertFriction *= 1f - PlayerSettings.WallrunNoInputSlipFrac;
		}

		ApplyFriction( horzFriction, vertFriction );
		Accelerate( wishDir, PlayerSettings.WallrunAccelerationHorizontal, PlayerSettings.WallrunAccelerationVertical * GetSlipScale() );
		Velocity = Vector3.VectorPlaneProject( Velocity, WallNormal.Value );

		if ( Velocity.LengthSquared < 1f )
		{
			Velocity = Vector3.Zero;
		}

		Vector3 dest = Position + Velocity * Time.Delta;
		SceneTraceResult tr = Controller.TraceBBox( Position, dest );
		Velocity += halfGravity;

		if ( Controller.IsFloor( tr.Normal ) )
		{
			ClearWallNormal();
			Position = tr.EndPosition;
			return;
		}

		if ( tr.Fraction == 1f )
		{
			Position = tr.EndPosition;
			return;
		}

		float stepAmount = StepMove( PlayerSettings.GroundAngle, PlayerSettings.StepHeightMax, WallNormal.Value );
		Controller.AddStepOffset( stepAmount * -WallNormal.Value );
	}

	private void CategorizePosition()
	{
		if ( !WallNormal.HasValue )
		{
			return;
		}

		float maxStep = MathF.Max( PlayerSettings.StepHeightMax, PlayerSettings.WallrunAllowedWallDist );
		Vector3 point = Position - TargetWallNormal.Value * maxStep;
		Vector3 bumpOrigin = Position;
		bool moveToEndPos = true;

		if ( !CanFeetReachWall( Position, WallNormal.Value ) )
		{
			FallAwayFromWall();
			return;
		}

		if ( Velocity.z > 0f && IsNearTopWall( Position, WallNormal.Value ) )
		{
			ApplyTopWallDecel();
		}

		if ( Input.Down( "Duck" ) )
		{
			FallAwayFromWall( false );
			return;
		}

		if ( TimeSinceStart > PlayerSettings.WallrunTimeLimit )
		{
			FallAwayFromWall();
			return;
		}

		// If our target wall normal and current wall normal are too different, don't step down
		if ( moveToEndPos && WallNormal?.Dot( TargetWallNormal.Value ) < PlayerSettings.WallrunAngleChangeMinCos )
		{
			moveToEndPos = false;
		}

		SceneTraceResult tr = Controller.TraceBBox( bumpOrigin, point );

		if ( tr.GameObject is null || Controller.IsFloor( tr.Normal ) )
		{
			ClearWallNormal();
			return;
		}
		else
		{
			UpdateWallNormal( tr.Normal );
		}

		Vector3 wishDir = Controller.BuildWishDir();
		if ( wishDir.Dot( WallNormal.Value ) < 0.71f )
		{
			TimeSincePushingAway = 0;
		}
		else if ( TimeSincePushingAway > PlayerSettings.WallrunPushAwayFallOffTime )
		{
			FallAwayFromWall();
			return;
		}

		if ( !moveToEndPos || tr.StartedSolid )
		{
			return;
		}

		Position = tr.EndPosition;

		Vector3 stepOffset = Vector3.Dot( Position - bumpOrigin, TargetWallNormal.Value ) * -TargetWallNormal.Value;
		Controller.AddStepOffset( stepOffset );
	}

	private float StepMove( float wallAngle, float stepSize, Vector3 wallNormal )
	{
		MoveHelper mover = new( Position, Velocity )
		{
			Trace = Scene.Trace.Size( Controller.Hull )
				.WithoutTags( "player" ),
			MaxStandableAngle = wallAngle
		};

		mover.TryMoveWithStep( Time.Delta, stepSize, wallNormal, out float stepAmount );
		Position = mover.Position;
		Velocity = mover.Velocity;

		if ( mover.HitNormal.HasValue && Controller.IsFloor( mover.HitNormal.Value ) )
		{
			ClearWallNormal();
		}
		else if ( mover.HitNormal.HasValue )
		{
			WallrunEligibility eligibility = IsWallEligibleForWallrun( Position, mover.HitNormal.Value );

			if ( eligibility.HasFlag( WallrunEligibility.Eligible ) )
			{
				UpdateWallNormal( mover.HitNormal.Value );
			}
		}

		return stepAmount;
	}

	/// <summary>
	/// Updates the wall normal and sets the target wall normal.
	/// </summary>
	/// <param name="wallNormal">The new wall normal.</param>
	private void UpdateWallNormal( Vector3 wallNormal )
	{
		if ( !WallNormal.HasValue )
		{
			WallNormal = wallNormal;
		}

		if ( !TargetWallNormal.HasValue || !wallNormal.AlmostEqual( TargetWallNormal.Value ) )
		{
			TargetWallNormal = wallNormal;
			Angles angleDiff = TargetWallNormal.Value.EulerAngles - WallNormal.Value.EulerAngles;
			RelativeAngleCorrectSpeed = MathF.Abs( angleDiff.Normal.yaw ) * 2f;
		}
	}

	private void ClearWallNormal()
	{
		WallNormal = null;
		TargetWallNormal = null;
		ShouldWallrun = false;
	}

	/// <summary>
	/// Called when we fall away from the wall, i.e. when we run out of time, push away, or duck.
	/// </summary>
	private void FallAwayFromWall( bool setTimeSince = true )
	{
		Velocity += WallNormal.Value * PlayerSettings.WallrunFallAwaySpeed;
		ClearWallNormal();

		if ( setTimeSince )
		{
			TimeSinceFellAwayFromWall = 0;
		}
	}

	/// <summary>
	/// Builds the wish direction for wallrunning.
	/// If we're looking perpendicular to the wall, forward input is vertical.
	/// Otherwise, forward input is camera forward, and into the wall input is camera up. (Out of the wall input is not allowed).
	/// Also we add a small amount of upward auto push when moving forward.
	/// Then we project the wish direction onto the wall plane to make sure our input is always on the wall plane.
	/// 
	/// Kinda feels like a mess right now.
	/// </summary>
	/// <param name="wallNormal">The normal vector of the wall.</param>
	/// <returns>The normalized input direction on the plane of the wall normal </returns>
	private Vector3 BuildWishDirection( Vector3 wallNormal )
	{
		Angles angles = Controller.EyeAngles;
		Vector3 wishDir;

		float lookingNormalAmount = angles.WithPitch( 0f ).Forward.Dot( wallNormal );
		bool tryingToClimb = MathF.Abs( lookingNormalAmount ) > 0.71f;

		if ( tryingToClimb )
		{
			wishDir = Controller.WishMove * Rotation.LookAt( Vector3.Down * MathF.Sign( lookingNormalAmount ), wallNormal );
			wishDir = wishDir.Normal;
			return wishDir;
		}

		Vector3 wallHorizontal = wallNormal.Cross( Vector3.Up );
		float lookingRightAmount = angles.WithPitch( 0f ).Forward.Dot( wallHorizontal );
		Vector3 leftRightMove = Controller.WishMove.WithX( 0f );

		// Don't allow input away from wall
		leftRightMove.y = leftRightMove.y.Clamp( 0f, MathF.Sign( -lookingRightAmount ) );
		leftRightMove *= Rotation.LookAt( angles.Forward, wallNormal );

		Vector3 forwardBackMove = Controller.WishMove.WithY( 0f );

		float upwardAutoPushAmount = MathF.Max( 0f, PlayerSettings.WallrunUpwardAutoPush - angles.Forward.Dot( Vector3.Up ).Clamp( 0f, 1f ) );
		Vector3 upwardAutoPush = Vector3.Zero;

		if ( forwardBackMove.x > 0f )
		{
			upwardAutoPush = upwardAutoPushAmount * Vector3.Up * angles.ToRotation();
			upwardAutoPush = Vector3.VectorPlaneProject( upwardAutoPush, wallNormal ).Normal * upwardAutoPushAmount;
		}

		forwardBackMove *= angles.ToRotation();
		forwardBackMove = Vector3.VectorPlaneProject( forwardBackMove, wallNormal ).Normal;

		wishDir = leftRightMove + forwardBackMove;
		wishDir += upwardAutoPush * wishDir.WithZ( 0f ).Length;

		return wishDir;
	}

	private void CorrectView()
	{
		Angles horzEyeAngles = Controller.EyeAngles.WithPitch( 0f ).Normal;
		Angles vertEyeAngles = Controller.EyeAngles.WithYaw( 0f );

		float speedFraction = HorzVelocity.Length.LerpInverse( 0f, 100f );

		MaintainRelativeYaw( horzEyeAngles.Normal );

		// If we're wallrunning and looking and moving forward
		if ( WallNormal.HasValue && horzEyeAngles.Forward.Dot( Velocity.Normal ) >= 0f )
		{
			Angles wallAngles = WallNormal.Value.EulerAngles.WithPitch( 0f ).Normal;
			CorrectYaw( wallAngles, horzEyeAngles.Normal, speedFraction );
			CorrectPitch( wallAngles, vertEyeAngles, speedFraction );
		}
	}

	/// <summary>
	/// When the wall curves, maintain the same relative yaw angle to the wall.
	/// </summary>
	/// <param name="horzEyeAngles"></param>
	private void MaintainRelativeYaw( Angles horzEyeAngles )
	{
		RelativeAnglesOffset = RelativeAnglesOffset.Normal;

		if ( RelativeAnglesOffset.yaw.AlmostEqual( 0f ) )
		{
			RelativeAnglesOffset = RelativeAnglesOffset.WithYaw( 0f );
			return;
		}

		float yaw = RelativeAnglesOffset.yaw;
		float approachSpeed = MathF.Abs( yaw ) * 2f * Time.Delta;
		approachSpeed = MathF.Max( approachSpeed, RelativeAngleCorrectSpeed * Time.Delta );

		RelativeAnglesOffset = RelativeAnglesOffset.WithYaw( RelativeAnglesOffset.yaw.Approach( 0f, approachSpeed ) );
		horzEyeAngles = horzEyeAngles.WithYaw( horzEyeAngles.yaw.Approach( horzEyeAngles.yaw + yaw, approachSpeed ) );

		Controller.EyeAngles = Controller.EyeAngles.WithYaw( horzEyeAngles.yaw );
	}

	/// <summary>
	/// If we're wallrunning and looking forward and slightly inward, correct the view to look more outwards.
	/// </summary>
	private void CorrectYaw( Angles wallAngles, Angles horzEyeAngles, float speedFraction )
	{
		Angles angleDiff = wallAngles - horzEyeAngles;
		float correctedAngleOffset = PlayerSettings.WallrunViewYawOffset;

		if ( MathF.Abs( angleDiff.yaw ) < correctedAngleOffset )
			return;

		// We are looking too much into the wall, correct our view back away from the wall
		Angles correctedAngle1 = wallAngles + Angles.Zero.WithYaw( correctedAngleOffset );
		Angles correctedAngle2 = wallAngles - Angles.Zero.WithYaw( correctedAngleOffset );
		Angles closerAngle = horzEyeAngles.Distance( correctedAngle1 ) < horzEyeAngles.Distance( correctedAngle2 ) ? correctedAngle1 : correctedAngle2;

		float t = MathUtils.EaseOutCubic( speedFraction * Time.Delta );
		horzEyeAngles = horzEyeAngles.LerpTo( closerAngle, t );
		Controller.EyeAngles = Controller.EyeAngles.WithYaw( horzEyeAngles.yaw );
	}

	/// <summary>
	/// If we're wallrunning and looking forward and slightly downward or upward, correct the view to be more level.
	/// </summary>
	private void CorrectPitch( Angles wallAngles, Angles vertEyeAngles, float speedFraction )
	{
		Angles angleDiff = wallAngles - vertEyeAngles;
		float correctedAngleOffsetMin = PlayerSettings.WallrunViewPitchOffsetMin;
		float correctedAngleOffsetMax = PlayerSettings.WallrunViewPitchOffsetMax;
		float correctSpeed = PlayerSettings.WallrunViewPitchOffsetCorrectSpeed;

		if ( MathF.Abs( angleDiff.pitch ) < correctedAngleOffsetMin || MathF.Abs( angleDiff.pitch ) > correctedAngleOffsetMax )
			return;

		Angles correctedAngle1 = wallAngles + Angles.Zero.WithPitch( correctedAngleOffsetMin );
		Angles correctedAngle2 = wallAngles - Angles.Zero.WithPitch( correctedAngleOffsetMin );
		Angles closerAngle = vertEyeAngles.Distance( correctedAngle1 ) < vertEyeAngles.Distance( correctedAngle2 ) ? correctedAngle1 : correctedAngle2;

		vertEyeAngles.pitch = vertEyeAngles.pitch.Approach( closerAngle.pitch, speedFraction * correctSpeed * Time.Delta );
		Controller.EyeAngles = Controller.EyeAngles.WithPitch( vertEyeAngles.pitch );
	}

	private void OnJump( JumpMechanic.JumpType _ )
	{
		HasBoost = true;

		if ( IsActive )
		{
			ClearWallNormal();
		}
	}

	private void OnLanded()
	{
		// We're eligible for the up wall boost if we have jumped after leaving the ground
		HasBoost = false;
		LastWallrunStartPos = null;
	}

	private void ApplyWallrunTilt()
	{
		bool wallrunEndingSoon = IsActive && TimeSinceStart > PlayerSettings.WallrunTimeLimit - 1f / PlayerSettings.WallrunTiltEndSpeed;
		float tiltSpeed = wallrunEndingSoon ? PlayerSettings.WallrunTiltEndSpeed : PlayerSettings.WallrunTiltSpeed;

		Vector3? wallNormal = WallNormal ?? PredictedWallNormal;
		Vector3 approachTo = wallNormal.HasValue && !wallrunEndingSoon ? wallNormal.Value : Vector3.Zero;

		TiltVector = TiltVector.ApproachVector( approachTo, tiltSpeed * Time.Delta );

		float t = MathUtils.EaseOutSine( TiltVector.Length );
		float tilt = MathX.Lerp( 0f, PlayerSettings.WallrunTiltMaxRoll, t, false );

		float angleFraction = Controller.EyeAngles.Forward.Cross( TiltVector.Normal ).z;
		float totalRoll = -tilt * angleFraction;

		Controller.EyeAngles += new Angles( 0f, 0f, totalRoll );
	}

	/// <summary>
	/// Applies the wallrun start upboost if eligible.
	/// </summary>
	private void ApplyBoost()
	{
		if ( !HasBoost ) return;

		HasBoost = false;

		float upBoost = PlayerSettings.WallrunUpWallBoost - Velocity.z;
		upBoost = upBoost.Clamp( 0f, PlayerSettings.WallrunUpWallBoost );
		Velocity += Vector3.Up * upBoost;
	}

	/// <summary>
	/// Applies vertical deceleration.
	/// Called when we are nearing the top edge of a wall.
	/// </summary>
	private void ApplyTopWallDecel()
	{
		float projSpeed = Velocity.Dot( HorzVelocity.Normal );
		Vector3 proj = HorzVelocity.Normal * projSpeed;
		Vector3 decel = proj - Velocity;
		decel = decel.ClampLength( PlayerSettings.WallrunAvoidTopWallDecel * Time.Delta );

		Velocity += decel;
	}

	/// <summary>
	/// Predicts if a wallrun is possible based on the current position and velocity of the player.
	/// </summary>
	/// <param name="timeStep">The time step used for the prediction.</param>
	/// <returns>
	/// Returns the normal vector of the wall if a wallrun is possible, otherwise returns null.
	/// </returns>
	private Vector3? PredictWallrun( float timeStep )
	{
		Vector3 nextPos = Position + Velocity * timeStep;

		SceneTraceResult tr = Controller.TraceBBox( Position, nextPos );

		if ( !tr.Hit )
		{
			return null;
		}

		if ( Controller.IsFloor( tr.Normal ) )
		{
			return null;
		}

		WallrunEligibility eligibility = IsWallEligibleForWallrun( tr.EndPosition, tr.Normal );

		if ( eligibility == WallrunEligibility.Ineligible || !CanFeetReachWall( tr.EndPosition, tr.Normal ) )
		{
			return null;
		}

		return tr.Normal;
	}

	/// <summary>
	/// Checks if the player's feet can reach the wall at the given position and with the given wall normal.
	/// </summary>
	/// <param name="position">The position of the player from which to trace towards the wall.</param>
	/// <param name="wallNormal">The normal vector of the wall.</param>
	/// <returns>True if the player's feet can reach the wall, false otherwise.</returns>
	private bool CanFeetReachWall( Vector3 position, Vector3 wallNormal )
	{
		Vector3 nextPos = position - wallNormal * PlayerSettings.WallrunAllowedWallDist;
		BBox halfHull = Controller.Hull;
		halfHull.Maxs.z *= 0.5f;

		SceneTraceResult tr = Controller.TraceBBox( position, nextPos, halfHull.Mins, halfHull.Maxs );

		return tr.Hit;
	}

	/// <summary>
	/// Takes a given position and traces backwards towards the wall and finds if we are near the top of the wall (but not a ceiling).
	/// </summary>
	/// <param name="position">The position of the player.</param>
	/// <param name="wallNormal">The normal vector of the wall.</param>
	/// <returns>True if the player's head can reach the wall, false otherwise.</returns>
	private bool IsNearTopWall( Vector3 position, Vector3 wallNormal )
	{
		// First find the wall position
		Vector3 nextPos = position - wallNormal * PlayerSettings.WallrunAllowedWallDist;
		SceneTraceResult tr = Controller.TraceBBox( position, nextPos );

		if ( !tr.Hit )
		{
			return false;
		}

		float stepSize = 20f;

		// First trace up
		position = tr.EndPosition;
		nextPos = position + Vector3.Up * stepSize;
		tr = Controller.TraceBBox( position, nextPos );

		if ( tr.Hit )
		{
			return false;
		}

		// Then trace across
		position = tr.EndPosition;
		nextPos = position - wallNormal * 2f;
		tr = Controller.TraceBBox( position, nextPos );

		// Then trace back down
		position = tr.EndPosition;
		nextPos = position - Vector3.Up * stepSize;
		tr = Controller.TraceBBox( position, nextPos );

		if ( tr.Hit && Controller.IsFloor( tr.Normal ) )
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Determines if a "wall" at a certain position is actually a step (floor underneath and is steppable).
	/// </summary>
	/// <param name="position"></param>
	/// <param name="wallNormal"></param>
	/// <returns></returns>
	private bool IsStep( Vector3 position, Vector3 wallNormal )
	{
		Vector3 nextPos = position - Vector3.Up * 20f;
		SceneTraceResult tr = Controller.TraceBBox( position, nextPos );

		if ( !tr.Hit || !Controller.IsFloor( tr.Normal ) )
		{
			return false;
		}

		return IsNearTopWall( tr.EndPosition, wallNormal );
	}

	/// <summary>
	/// Determines eligibility for wallrun at a certain position
	/// </summary>
	/// <param name="wallPosition"></param>
	/// <param name="wallNormal"></param>
	/// <returns></returns>
	private WallrunEligibility IsWallEligibleForWallrun( Vector3 wallPosition, Vector3 wallNormal )
	{
		// The bottom half of our hull isn't in contact with the wall
		if ( !CanFeetReachWall( wallPosition, wallNormal ) )
		{
			return WallrunEligibility.Ineligible;
		}

		// We don't want to wallrun on stairs
		if ( IsStep( wallPosition, wallNormal ) )
		{
			return WallrunEligibility.Ineligible;
		}

		// If we haven't started wallrunning yet or the wall normal is different enough
		if ( !LastWallrunStartPos.HasValue || wallNormal.Dot( LastWallNormal ) <= PlayerSettings.WallrunSameWallDot )
		{
			return HasBoost ? WallrunEligibility.Eligible : WallrunEligibility.Weak;
		}

		// If we are too high on the wall relative to our latest height
		if ( wallPosition.z - LastWallrunStartPos?.z > PlayerSettings.WallrunSameWallHeight )
		{
			return WallrunEligibility.Ineligible;
		}

		// We can wallrun lower on the wall, but it will be a weak wallrun
		return WallrunEligibility.Weak;
	}

	[Flags]
	private enum WallrunEligibility
	{
		Ineligible = 0,
		Eligible = 1 << 0,
		Weak = 1 << 1 | Eligible,
	}

	/// <summary>
	/// Applies friction to the given velocity vector.
	/// </summary>
	/// <param name="velocity">The velocity vector to apply friction to.</param>
	/// <param name="friction">The friction amount.</param>
	/// <returns>The velocity vector after applying friction.</returns>
	private static Vector3 ApplyFriction( Vector3 velocity, float friction )
	{
		float speed = velocity.Length;

		if ( speed < 0.1f )
		{
			return velocity;
		}

		float drop = speed * friction * Time.Delta;

		float newSpeed = speed - drop;

		if ( newSpeed < 0f )
		{
			newSpeed = 0f;
		}

		if ( newSpeed != speed )
		{
			newSpeed /= speed;
			velocity *= newSpeed;
		}

		return velocity;
	}

	/// <summary>
	/// Applies friction separately to the horizontal and vertical components of the velocity
	/// </summary>
	/// <param name="horizontalFriction">Horizontal friction component</param>
	/// <param name="verticalFriction">Vertical friction component</param>
	private void ApplyFriction( float horizontalFriction, float verticalFriction )
	{
		Vector3 horzVelocity = HorzVelocity;
		Vector3 vertVelocity = Velocity - horzVelocity;

		horzVelocity = ApplyFriction( horzVelocity, horizontalFriction );
		vertVelocity = ApplyFriction( vertVelocity, verticalFriction );

		Velocity = horzVelocity + vertVelocity;
	}

	/// <summary>
	/// Accelerates the player separately in the horizontal and vertical components.
	/// </summary>
	/// <param name="velocity">The current velocity of the player.</param>
	/// <param name="wishDir">The desired direction of acceleration.</param>
	/// <param name="wishSpeed">The desired speed.</param>
	/// <param name="acceleration">The acceleration amount.</param>
	/// <returns>The updated velocity after acceleration.</returns>
	private static Vector3 Accelerate( Vector3 velocity, Vector3 wishDir, float wishSpeed, float acceleration )
	{
		float currentSpeed = velocity.Dot( wishDir );
		float addSpeed = MathF.Max( 0f, wishSpeed - currentSpeed );
		float accelSpeed = MathF.Min( addSpeed, acceleration * Time.Delta );

		float magnitudeSquared = MathF.Max( wishSpeed * wishSpeed, velocity.LengthSquared );

		velocity += wishDir * accelSpeed;

		return velocity.ClampLength( MathF.Sqrt( magnitudeSquared ) );
	}

	/// <summary>
	/// Accelerates the player separately in the horizontal and vertical components
	/// </summary>
	/// <param name="wishDir">The desired direction of acceleration.</param>
	/// <param name="horzAcceleration">The horizontal acceleration amount.</param>
	/// <param name="vertAcceleration">The vertical acceleration amount.</param>
	private void Accelerate( Vector3 wishDir, float horzAcceleration, float vertAcceleration )
	{
		Vector3 horzWishDir = wishDir.WithZ( 0f );
		Vector3 vertWishDir = Vector3.Up * wishDir.z;

		float wishDirEyeDot = Controller.EyeAngles.WithPitch( 0f ).Forward.Dot( wishDir );
		bool movingBackwards = wishDirEyeDot < -0.65f;

		float horzMaxSpeed, vertMaxSpeed;

		if ( movingBackwards )
		{
			horzMaxSpeed = (horzWishDir * PlayerSettings.WallrunMaxSpeedBackwards).Length;
			vertMaxSpeed = (vertWishDir * PlayerSettings.WallrunMaxSpeedBackwards).Length;
		}
		else
		{
			horzMaxSpeed = MathF.Min( (horzWishDir * PlayerSettings.WallrunMaxSpeedHorizontal).Length, PlayerSettings.WallrunMaxSpeedHorizontal );
			vertMaxSpeed = MathF.Min( (vertWishDir * PlayerSettings.WallrunMaxSpeedVertical).Length, PlayerSettings.WallrunMaxSpeedVertical );

		}

		Vector3 horzVelocity = HorzVelocity;
		Vector3 vertVelocity = Vector3.Up * Velocity.z;

		horzVelocity = Accelerate( horzVelocity, horzWishDir.Normal, horzMaxSpeed, horzAcceleration );
		vertVelocity = Accelerate( vertVelocity, vertWishDir.Normal, vertMaxSpeed, vertAcceleration );

		Velocity = horzVelocity + vertVelocity;
	}

	/// <summary>
	/// Calculates the slip fraction for wall running based on how long we've been on the wall.
	/// </summary>
	/// <returns>The slip fraction value.</returns>
	private float GetSlipFraction()
	{
		return ((TimeSinceStart - PlayerSettings.WallrunSlipStartTime) / PlayerSettings.WallrunSlipDuration).Clamp( 0f, 1f );
	}

	/// <summary>
	/// Gets the slip scale for wallrunning based on how long we've been on the wall.
	/// </summary>
	/// <returns></returns>
	private float GetSlipScale()
	{
		return 1f - GetSlipFraction();
	}

	/// <summary>
	/// Gets the gravity scale for wallrunning based on how long we've been on the wall.
	/// </summary>
	/// <returns>The gravity scale value.</returns>
	private float GetWallrunGravityScale()
	{
		return IsWallrunWeak ? 1f : (TimeSinceStart / PlayerSettings.WallrunGravityRampUpTime).Clamp( 0f, 1f );
	}

	protected override void DrawGizmos()
	{
		if ( !DebugWallrun ) return;

		Gizmo.Draw.Color = Color.White;

		if ( WallNormal.HasValue )
			Gizmo.Draw.Arrow( Vector3.Left * 5f, Vector3.Left * 5f + WallNormal.Value * 50f );

		Gizmo.Draw.Color = Color.Red;

		if ( TargetWallNormal.HasValue )
			Gizmo.Draw.Arrow( 0f, TargetWallNormal.Value * 50f );

		Gizmo.Draw.Color = Color.Green;

		if ( WallNormal.HasValue )
			Gizmo.Draw.Arrow( Vector3.Zero, BuildWishDirection( WallNormal.Value ) * 60f );
	}
}