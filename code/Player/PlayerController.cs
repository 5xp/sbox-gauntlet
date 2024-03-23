namespace Gauntlet;

public partial class PlayerController : Component
{
	/// <summary>
	/// A reference to the player's body (the GameObject)
	/// </summary>
	[Property] public GameObject Body { get; set; }

	/// <summary>
	/// A reference to the animation helper (normally on the Body GameObject)
	/// </summary>
	[Property] public AnimationHelper AnimationHelper { get; set; }

	/// <summary>
	/// The current camera controller for this player.
	/// </summary>
	[Property] public CameraController CameraController { get; set; }

	/// <summary>
	/// A reference to the player's hull collider, used for colliding with triggers.
	/// </summary>
	[Property] public HullCollider HullCollider { get; set; }

	private SoundHandle LandSoundHandle { get; set; }

	/// <summary>
	/// Get a quick reference to the real Camera GameObject.
	/// </summary>
	public GameObject CameraGameObject => CameraController.Camera.GameObject;

	/// <summary>
	/// Mechanics can add to this positional camera offset. Resets to zero after every frame.
	/// </summary>
	public Vector3 CurrentCameraOffset { get; set; }

	/// <summary>
	/// Mechanics can add to this rotational camera offset. Resets to zero after every frame.
	/// </summary>
	public Rotation CurrentCameraRotationOffset { get; set; }

	/// <summary>
	/// When we step, we add to this offset
	/// This offset gets added to the eye position
	/// This slowly goes back to 0
	/// </summary>
	public Vector3 StepSmoothingOffset { get; set; }

	public GameObject LastGroundObject { get; set; }
	public GameObject GroundObject { get; set; }
	[Property, ReadOnly] public bool IsGrounded => GroundObject is not null;
	public TimeSince TimeSinceLastOnGround { get; set; }
	public TimeSince TimeSinceLastLanding { get; set; }

	public Vector3 GroundNormal { get; set; }
	public float CurrentEyeHeight { get; set; }
	public float CurrentHullHeight { get; set; }
	public float DuckFraction { get; set; }
	public Vector3 LastVelocity { get; set; }
	public float FallSpeed { get; set; }
	public float FallHeight => FallSpeed * FallSpeed / (2 * PlayerSettings.Gravity) / 12f * MathF.Sign( -FallSpeed );

	/// <summary>
	/// Finds the first <see cref="SkinnedModelRenderer"/> on <see cref="Body"/>
	/// </summary>
	public SkinnedModelRenderer BodyRenderer => Body.Components.Get<SkinnedModelRenderer>();

	/// <summary>
	/// An accessor to get the camera controller's aim ray.
	/// </summary>
	public Ray AimRay => CameraController.AimRay;

	public Vector3 Position
	{
		get => GameObject.Transform.Position;
		set => GameObject.Transform.Position = value;
	}

	public Vector3 Velocity { get; set; }
	public Vector3 HorzVelocity => Velocity.WithZ( 0f );

	public PlayerSettings PlayerSettings { get; set; } = PlayerSettings.Regular;

	/// <summary>
	/// The current holdtype for the player.
	/// </summary>
	[Property] AnimationHelper.HoldTypes CurrentHoldType { get; set; } = AnimationHelper.HoldTypes.None;

	/// <summary>
	/// Called when the player jumps.
	/// </summary>
	public Action<JumpMechanic.JumpType> OnJump;

	/// <summary>
	/// Called when the player lands.
	/// </summary>
	public Action OnLanded;

	public BBox Hull
	{
		get
		{
			float radius = PlayerSettings.HullRadius;
			float height = CurrentHullHeight;

			Vector3 mins = new Vector3( -radius, -radius, 0 );
			Vector3 maxs = new( radius, radius, height );

			return new BBox( mins, maxs );
		}
	}

	public Angles EyeAngles;

	protected override void OnStart()
	{
		if ( HullCollider is not null )
		{
			HullCollider.Center = Vector3.Up * PlayerSettings.HullHeightStanding * 0.5f;
			HullCollider.BoxSize = new Vector3( PlayerSettings.HullRadius * 2, PlayerSettings.HullRadius * 2, PlayerSettings.HullHeightStanding );
		}

		if ( DebugConVars.DebugWallrunSettings )
		{
			PlayerSettings = PlayerSettings.Debug;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishInput();
		OnUpdateMechanics();

		if ( Input.Pressed( "Restart" ) )
		{
			Restart();
		}
	}

	protected override void OnUpdate()
	{
		// Eye input
		if ( !IsProxy )
		{
			SimulateEyes();
			CurrentCameraOffset = Vector3.Zero;

			EyeAngles.pitch += GetInputDelta().pitch;
			EyeAngles.yaw += GetInputDelta().yaw;
			EyeAngles.pitch = EyeAngles.pitch.Clamp( -PlayerSettings.PitchMaxUp, PlayerSettings.PitchMaxDown );

			var cam = CameraController.Camera;

			ViewPunchMechanic viewPunch = GetMechanic<ViewPunchMechanic>();

			var lookDir = EyeAngles.ToRotation() * viewPunch.Spring.ToRotation();

			cam.Transform.Rotation = lookDir * CurrentCameraRotationOffset;
			EyeAngles.roll = 0;
			CurrentCameraRotationOffset = Rotation.Identity;

		}

		float rotateDifference = 0;

		// rotate body to look angles
		if ( Body is not null )
		{
			var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

			rotateDifference = Body.Transform.Rotation.Distance( targetAngle );

			if ( rotateDifference > 50.0f || (Velocity.Length > 10.0f) )
			{
				Body.Transform.Rotation = Rotation.Lerp( Body.Transform.Rotation, targetAngle, Time.Delta * 10.0f );
			}
		}

		if ( AnimationHelper is not null )
		{
			AnimationHelper.WithVelocity( Velocity );
			AnimationHelper.WithWishVelocity( BuildWishDir() * GetWishSpeed() );
			AnimationHelper.IsGrounded = IsGrounded;
			AnimationHelper.FootShuffle = rotateDifference;
			AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			AnimationHelper.MoveStyle = AnimationHelper.MoveStyles.Auto;
			AnimationHelper.DuckLevel = HasTag( "slide" ) ? 0 : DuckFraction;
			AnimationHelper.HoldType = CurrentHoldType;
			AnimationHelper.SkidAmount = HasTag( "slide" ) ? 1 : 0;
		}

		foreach ( var mechanic in Mechanics )
		{
			mechanic.FrameSimulate();
		}
	}

	public void Restart()
	{
		GameManager gameManager = Scene.Components.Get<GameManager>( FindMode.InChildren );

		if ( gameManager is not null )
		{
			gameManager.ShouldRespawn = true;
		}
	}

	public void SimulateEyes()
	{
		float targetDuckFraction = Input.Down( "Duck" ) ? 1f : 0f;
		float targetHullHeight = PlayerSettings.HullHeightStanding.LerpTo( PlayerSettings.HullHeightCrouching, targetDuckFraction );
		// Unducking
		if ( DuckFraction > targetDuckFraction )
		{
			if ( GetMechanic<CrouchMechanic>().ForceDuck )
			{
				DuckFraction = 1f;
			}
			else
			{
				DuckFraction = DuckFraction.Approach( 0f, PlayerSettings.UnduckSpeed * Time.Delta );
			}
		}
		// Ducking
		else if ( DuckFraction < targetDuckFraction )
		{
			float duckSpeed = HasTag( "slide" ) ? PlayerSettings.UnduckSpeed : PlayerSettings.DuckSpeed;
			DuckFraction = DuckFraction.Approach( 1f, duckSpeed * Time.Delta );
		}

		// Finished unducking or ducking
		if ( DuckFraction.AlmostEqual( targetDuckFraction ) )
		{
			CurrentHullHeight = targetHullHeight;
		}

		if ( HasTag( "slide" ) )
		{
			CurrentHullHeight = PlayerSettings.HullHeightCrouching;
		}

		StepSmoothingOffset = StepSmoothingOffset.Approach( 0f, PlayerSettings.StepSmoothingOffsetCorrectSpeed * Time.Delta );
		float duckTime = MathUtils.SmoothStep( DuckFraction );
		CurrentEyeHeight = PlayerSettings.ViewHeightStanding.LerpTo( PlayerSettings.ViewHeightCrouching, duckTime );
		CameraGameObject.Transform.LocalPosition = Vector3.Up * CurrentEyeHeight + CurrentCameraOffset + StepSmoothingOffset;
	}

	public bool CanUnduck()
	{
		float liftHead = PlayerSettings.HullHeightStanding - CurrentHullHeight;
		SceneTraceResult tr = TraceBBox( Position, Position, 0, liftHead );
		return !tr.Hit;
	}

	public float StepMove( float groundAngle, float stepSize, Vector3 up )
	{
		MoveHelper mover = new( Position, Velocity )
		{
			Trace = Scene.Trace.Size( Hull )
			.WithoutTags( "player" ),
			MaxStandableAngle = groundAngle
		};

		mover.TryMoveWithStep( Time.Delta, stepSize, up, out float stepAmount );
		Position = mover.Position;
		Velocity = mover.Velocity;

		return stepAmount;
	}

	public void StepMove( float groundAngle = 46f, float stepSize = 18f )
	{
		StepMove( groundAngle, stepSize, Vector3.Up );
	}

	public void Move( float groundAngle = 46f )
	{
		MoveHelper mover = new( Position, Velocity )
		{
			Trace = Scene.Trace.Size( Hull )
			.WithoutTags( "player" ),
			MaxStandableAngle = groundAngle
		};

		mover.TryMove( Time.Delta, Vector3.Up );

		float beforeSpeed = HorzVelocity.Length;
		float beforeZ = Velocity.z;
		Position = mover.Position;
		Velocity = mover.Velocity;

		if ( mover.HitWall && mover.HitNormal.HasValue )
		{
			FallSpeed = beforeZ;
			PreWallTouchSpeed = beforeSpeed;
			GetMechanic<WallrunMechanic>().OnWallTouch( mover.HitNormal.Value );
		}
	}

	public void AddStepOffset( Vector3 stepOffset )
	{
		if ( stepOffset.LengthSquared < PlayerSettings.StepHeightMin * PlayerSettings.StepHeightMin )
			return;

		StepSmoothingOffset += stepOffset;

		// Clear transform lerping, otherwise it looks bad on high framerate
		GameObject.Transform.ClearLerp();
	}

	public void ClearGroundObject()
	{
		if ( GroundObject is null ) return;

		LastGroundObject = GroundObject;
		GroundObject = null;
		TimeSinceLastOnGround = 0;
	}

	/// <summary>
	/// A network message that lets other users that we've triggered a jump.
	/// </summary>
	[Broadcast]
	public void BroadcastPlayerJumped( JumpMechanic.JumpType jumpType )
	{
		AnimationHelper?.TriggerJump();
		OnJump?.Invoke( jumpType );
	}

	public SceneTraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		if ( liftFeet > 0 )
		{
			start += Vector3.Up * liftFeet;
			maxs = maxs.WithZ( maxs.z - liftFeet );
		}

		if ( liftHead > 0 )
		{
			end += Vector3.Up * liftHead;
		}

		SceneTraceResult tr = Scene.Trace.Ray( start, end )
			.Size( mins, maxs )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		return tr;
	}

	public static Angles GetInputDelta()
	{
		if ( Input.UsingController )
		{
			return Input.AnalogLook;
		}
		else
		{
			float multiplier = 0.022f * Preferences.Sensitivity;
			return new Angles( Input.MouseDelta.y * multiplier, -Input.MouseDelta.x * multiplier, 0 );
		}
	}

	public SceneTraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet, liftHead );
	}

	public static bool IsFloor( Vector3 up, Vector3 normal, float maxAngle )
	{
		return Vector3.GetAngle( up, normal ) <= maxAngle;
	}

	public bool IsFloor( Vector3 normal )
	{
		return IsFloor( Vector3.Up, normal, PlayerSettings.GroundAngle );
	}

	public Vector3 GetPlayerGravity()
	{
		return Vector3.Down * PlayerSettings.Gravity * PlayerSettings.GravityScale;
	}

	public float GetWishSpeed()
	{
		if ( CurrentSpeedOverride is not null ) return CurrentSpeedOverride.Value;

		return PlayerSettings.WalkSpeed;
	}

	public Vector3 WishMove;

	public void BuildWishInput()
	{
		WishMove = 0;

		if ( Input.UsingController )
		{
			WishMove = Input.AnalogMove;
		}
		else
		{
			if ( Input.Down( "forward", false ) ) WishMove += Vector3.Forward;
			if ( Input.Down( "backward", false ) ) WishMove += Vector3.Backward;
			if ( Input.Down( "left", false ) ) WishMove += Vector3.Left;
			if ( Input.Down( "right", false ) ) WishMove += Vector3.Right;
		}
	}

	public Vector3 BuildWishDir( bool zeroPitch = true )
	{
		Angles angles = EyeAngles.WithRoll( 0f );

		if ( zeroPitch )
		{
			angles = angles.WithPitch( 0f );
		}

		var rot = angles.ToRotation();
		var wishDirection = WishMove * rot;
		wishDirection = wishDirection.Normal;

		return wishDirection;
	}

	public void CategorizePosition( bool stayOnGround )
	{
		Vector3 point = Position + Vector3.Down * 2f;
		Vector3 bumpOrigin = Position;
		bool movingUpRapidly = Velocity.z > PlayerSettings.MaxNonJumpVelocity;
		bool moveToEndPos = false;

		if ( movingUpRapidly )
		{
			ClearGroundObject();
			return;
		}

		if ( IsGrounded )
		{
			moveToEndPos = true;
			point.z -= PlayerSettings.StepHeightMax;
		}
		else if ( stayOnGround )
		{
			moveToEndPos = true;
			point.z -= PlayerSettings.StepHeightMax;
		}

		SceneTraceResult tr = TraceBBox( bumpOrigin, point, 4.0f );
		float angle = Vector3.GetAngle( Vector3.Up, tr.Normal );

		if ( tr.GameObject is null || angle > PlayerSettings.GroundAngle )
		{
			ClearGroundObject();
			moveToEndPos = false;
		}
		else
		{
			UpdateGroundObject( tr );
		}

		// if ( moveToEndPos && !tr.StartedSolid && tr.Fraction > 0f && tr.Fraction < 1f )
		if ( moveToEndPos && !tr.StartedSolid )
		{
			Position = tr.EndPosition;
		}
	}

	public void UpdateGroundObject( SceneTraceResult tr )
	{
		GroundNormal = tr.Normal;
		SetGroundObject( tr.GameObject );
	}

	public void SetGroundObject( GameObject groundObject )
	{
		LastGroundObject = GroundObject;
		GroundObject = groundObject;

		if ( groundObject is not null )
		{
			Velocity = HorzVelocity;
			TimeSinceLastOnGround = 0;
		}

		if ( LastGroundObject is null && groundObject is not null )
		{
			FallSpeed = LastVelocity.z;

			if ( FallHeight >= PlayerSettings.HardFallDist )
			{
				LandSoundHandle = Sound.Play( HardLandSound );
			}
			else
			{
				LandSoundHandle = Sound.Play( LandSound );
			}
			TimeSinceLastLanding = 0;
			OnLanded?.Invoke();
		}
	}

	/// <summary>
	/// Uses the current velocity to trace with a given timestep. If we hit anything return the hit normal.
	/// </summary>
	/// <param name="timeStep"></param>
	/// <returns></returns>
	public SceneTraceResult TraceWithVelocity( float timeStep )
	{
		Vector3 nextPos = Position + Velocity * timeStep;
		return TraceBBox( Position, nextPos );
	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( EyeAngles );
	}

	public void Read( ByteStream stream )
	{
		EyeAngles = stream.Read<Angles>();
	}

	protected override void DrawGizmos()
	{
		if ( !DebugConVars.DebugControllerGizmos ) return;

		Gizmo.Draw.LineBBox( Hull );

		Vector2 basePos = new( 25, 25 );
		Vector2 offset = new( 0, 20 );
		int offsetIndex = 0;

		CameraComponent cam = CameraController.Camera;
		Angles camAngles = cam.Transform.Rotation.Angles();

		float fontSize = 16;

		Gizmo.Draw.ScreenText( $"Velocity: {Velocity.Length:F2}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );
		Gizmo.Draw.ScreenText( $"Horz vel: {HorzVelocity.Length:F2}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );
		Gizmo.Draw.ScreenText( $"Vert vel: {Velocity.z:F2}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );
		Gizmo.Draw.ScreenText( $"Fall speed: {FallSpeed:F2}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );
		Gizmo.Draw.ScreenText( $"CamAngle: {camAngles.pitch:F2}, {camAngles.yaw:F2}, {camAngles.roll:F2}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );
		Gizmo.Draw.ScreenText( $"CamPos: {cam.Transform.Position.x:F2}, {cam.Transform.Position.y:F2}, {cam.Transform.Position.z:F2}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );

		offsetIndex++;

		var sortedMechanics = Mechanics;
		foreach ( var mechanic in sortedMechanics )
		{
			Gizmo.Draw.ScreenText( $"{mechanic.GetType().Name} {(mechanic.IsActive ? "Active" : "Inactive")}", basePos + offset * offsetIndex++, size: fontSize, flags: TextFlag.Left );
		}
	}
}
