namespace Tf;

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
	/// Get a quick reference to the real Camera GameObject.
	/// </summary>
	public GameObject CameraGameObject => CameraController.Camera.GameObject;

	[ConVar( "debug_viewPunch" )]
	public static bool DebugViewPunch { get; set; } = false;

	/// <summary>
	/// Mechanics can add to this camera offset. Resets to zero after every frame.
	/// </summary>
	public Vector3 CurrentCameraOffset { get; set; }

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
	public float ApexHeight { get; set; }


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
	[Property] public Action OnJump { get; set; }

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

	// Properties used only in this component.
	Vector3 WishVelocity;
	public Angles EyeAngles;

	protected float GetTargetHullHeight()
	{
		if ( CurrentHullHeightOverride is not null ) return CurrentHullHeightOverride.Value;
		return PlayerSettings.HullHeightStanding;
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		BuildWishInput();
		// Wish direction could change here
		OnUpdateMechanics();
		BuildWishVelocity();
	}

	protected override void OnUpdate()
	{
		// Eye input
		if ( !IsProxy )
		{
			SimulateEyes();
			CurrentCameraOffset = Vector3.Zero;

			EyeAngles.pitch += GetInputDelta().y;
			EyeAngles.yaw -= GetInputDelta().x;
			EyeAngles.pitch = EyeAngles.pitch.Clamp( -PlayerSettings.PitchMaxUp, PlayerSettings.PitchMaxDown );

			var cam = CameraController.Camera;

			ViewPunchMechanic viewPunch = GetMechanic<ViewPunchMechanic>();

			var lookDir = EyeAngles.ToRotation() * viewPunch.Spring.ToRotation();

			cam.Transform.Rotation = lookDir;
			EyeAngles.roll = 0;
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
			AnimationHelper.WithWishVelocity( WishVelocity );
			AnimationHelper.IsGrounded = IsGrounded;
			AnimationHelper.FootShuffle = rotateDifference;
			AnimationHelper.WithLook( EyeAngles.Forward, 1, 1, 1.0f );
			AnimationHelper.MoveStyle = HasTag( "sprint" ) ? AnimationHelper.MoveStyles.Run : AnimationHelper.MoveStyles.Walk;
			AnimationHelper.DuckLevel = HasTag( "slide" ) ? 0 : DuckFraction * 100f;
			AnimationHelper.HoldType = CurrentHoldType;
			AnimationHelper.SkidAmount = HasTag( "slide" ) ? 1 : 0;
		}

		foreach ( var mechanic in Mechanics )
		{
			mechanic.FrameSimulate();
		}
	}

	public void SimulateEyes()
	{
		float targetHullHeight = GetTargetHullHeight();
		float targetDuckFraction = Input.Down( "Duck" ) ? 1f : 0f;
		bool forceDuck = false;

		// Unducking
		if ( DuckFraction > targetDuckFraction )
		{
			float liftHead = PlayerSettings.HullHeightStanding - CurrentHullHeight;
			SceneTraceResult tr = TraceBBox( Position, Position, 0, liftHead );
			if ( tr.Hit )
			{
				forceDuck = true;
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
		else
		{
			CurrentHullHeight = targetHullHeight;
		}

		if ( HasTag( "slide" ) )
		{
			CurrentHullHeight = targetHullHeight;
		}

		GetMechanic<CrouchMechanic>().ForceDuck = forceDuck;

		StepSmoothingOffset = StepSmoothingOffset.Approach( 0f, PlayerSettings.StepSmoothingOffsetCorrectSpeed * Time.Delta );
		float duckTime = MathUtils.SmoothStep( DuckFraction );
		CurrentEyeHeight = PlayerSettings.ViewHeightStanding.LerpTo( PlayerSettings.ViewHeightCrouching, duckTime );
		CameraGameObject.Transform.LocalPosition = Vector3.Up * CurrentEyeHeight + CurrentCameraOffset + StepSmoothingOffset;
	}

	public void StepMove( float groundAngle = 46f, float stepSize = 18f )
	{
		MoveHelper mover = new( Position, Velocity )
		{
			Trace = Scene.Trace.Size( Hull )
			.WithoutTags( "player" ),
			MaxStandableAngle = groundAngle
		};

		mover.TryMoveWithStep( Time.Delta, stepSize, Vector3.Up, out float _ );
		Position = mover.Position;
		Velocity = mover.Velocity;
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

		if ( mover.HitWall )
		{
			// We hit the wall!
		}

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	public void AddStepOffset( Vector3 stepAmount )
	{
		StepSmoothingOffset += stepAmount;

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
	public void BroadcastPlayerJumped()
	{
		AnimationHelper?.TriggerJump();
		OnJump?.Invoke();
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

	public static Vector2 GetInputDelta()
	{
		return Input.MouseDelta * 0.066f;
	}

	public SceneTraceResult TraceBBox( Vector3 start, Vector3 end, float liftFeet = 0.0f, float liftHead = 0.0f )
	{
		return TraceBBox( start, end, Hull.Mins, Hull.Maxs, liftFeet, liftHead );
	}

	public void Accelerate( Vector3 wishDir, float wishSpeed, float acceleration, float extraAcceleration = 0f )
	{
		float currentSpeed = Velocity.Dot( wishDir );
		float addSpeed = MathF.Max( extraAcceleration * Time.Delta, wishSpeed - currentSpeed );

		float accelSpeed = MathF.Min( acceleration * Time.Delta, addSpeed );

		Velocity += wishDir * accelSpeed;
	}

	public float GetWishSpeed()
	{
		if ( CurrentSpeedOverride is not null ) return CurrentSpeedOverride.Value;

		// Default speed
		return PlayerSettings.WalkSpeed;
	}

	public Vector3 WishMove;

	public void BuildWishInput()
	{
		WishMove = 0;

		if ( Input.Down( "forward", false ) ) WishMove += Vector3.Forward;
		if ( Input.Down( "backward", false ) ) WishMove += Vector3.Backward;
		if ( Input.Down( "left", false ) ) WishMove += Vector3.Left;
		if ( Input.Down( "right", false ) ) WishMove += Vector3.Right;
	}

	private void CheckReachedApex( float previousVelocity, float currentVelocity )
	{
		if ( currentVelocity.AlmostEqual( 0f ) || (currentVelocity < 0f && previousVelocity > 0f) )
		{
			ApexHeight = Position.z;
		}
	}

	public Vector3 BuildWishVelocity( bool zeroPitch = true )
	{
		WishVelocity = 0;
		Angles angles = EyeAngles;

		if ( zeroPitch )
		{
			angles = angles.WithPitch( 0f );
		}

		var rot = angles.ToRotation();
		var wishDirection = WishMove * rot;
		wishDirection = wishDirection.WithZ( 0 ).Normal;

		WishVelocity = wishDirection * GetWishSpeed();

		return WishVelocity;
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
		Gizmo.Draw.LineBBox( Hull );
		Gizmo.Draw.ScreenText( HorzVelocity.Length.ToString(), new Vector2( 100, 100 ) );
	}
}
