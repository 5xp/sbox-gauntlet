namespace Gauntlet.Player;

public sealed class PlayerSettings
{
	public enum PlayerTypes
	{
		Regular,
		Faster
	};

	public PlayerSettings() { }

	public static PlayerSettings Debug
	{
		get
		{
			return new PlayerSettings
			{
				IsDebug = true,
				WallrunTimeLimit = 9999f,
				WallrunGravityRampUpTime = 9999f,
				WallrunSlipStartTime = 9999f,
				WallrunPushAwayFallOffTime = 9999f,
				WallrunUpwardAutoPush = 0f,
				WallrunNoInputSlipFrac = 0f,
				WallrunTiltMaxRoll = 0f,
			};
		}
	}

	public static PlayerSettings Regular
	{
		get
		{
			return new PlayerSettings();
		}
	}

	public static PlayerSettings Faster
	{
		get
		{
			return new PlayerSettings
			{
				GravityScale = 0.8f,
				WalkSpeed = 173.5f,
				SprintSpeed = 260f,
				WallrunJumpInputDirSpeed = 80f,
				WallrunMaxSpeedHorizontal = 420f,
				WallrunAccelerationHorizontal = 1500f,
				WallrunGravityRampUpTime = 0.7f,
				WallrunSlipStartTime = 1.5f,
			};
		}
	}

	public static PlayerSettings GetPlayerSettings( PlayerTypes playerType )
	{
		return playerType switch
		{
			PlayerTypes.Faster => Faster,
			_ => Regular,
		};
	}

	[Hide] public bool IsDebug { get; private set; }
	[Group( "Controller" )] public float HullRadius { get; private set; } = 16f;
	[Group( "Controller" )] public float HullHeightStanding { get; private set; } = 72f;
	[Group( "Controller" )] public float HullHeightCrouching { get; private set; } = 47f;
	[Group( "Camera" )] public float ViewHeightStanding { get; private set; } = 60f;
	[Group( "Camera" )] public float ViewHeightCrouching { get; private set; } = 38f;
	[Group( "Controller" )] public float Gravity { get; private set; } = 750f;
	[Group( "Controller" )] public float GravityScale { get; private set; } = 0.75f;
	[Group( "Controller" )] public float StepHeightMax { get; private set; } = 18f;
	[Group( "Controller" )] public float StepHeightMin { get; private set; } = 4f;
	[Group( "Controller" )] public float GroundAngle { get; private set; } = 46f;
	[Group( "Controller" )] public float MaxNonJumpVelocity { get; private set; } = 140f;
	[Group( "Base Movement" )] public float Acceleration { get; private set; } = 2500f;
	[Group( "Base Movement" )] public float WalkSpeed { get; private set; } = 162.5f;
	[Group( "Base Movement" )] public float CrouchSpeed { get; private set; } = 80f;
	[Group( "Base Movement" )] public float SprintSpeed { get; private set; } = 243f;
	[Group( "Base Movement" )] public float AirSpeed { get; private set; } = 60f;
	[Group( "Base Movement" )] public float AirAcceleration { get; private set; } = 500f;

	/// <summary>
	/// Extra air acceleration given to players, even if they're already at max speed. Helps to start wall running
	/// </summary>
	[Group( "Base Movement" )]
	public float ExtraAirAcceleration { get; private set; } = 2f;

	[Group( "Camera" )] public float PitchMaxUp { get; private set; } = 85f;
	[Group( "Camera" )] public float PitchMaxDown { get; private set; } = 89f;

	/// <summary>
	/// How fast the player ducks
	/// </summary>
	[Group( "Camera" )]
	public float DuckSpeed { get; private set; } = 2f;

	/// <summary>
	/// How fast the player stands up
	/// </summary>

	[Group( "Camera" )]
	public float UnduckSpeed { get; private set; } = 5f;

	[Group( "Jump" )] public bool CanJumpWhileUnducking { get; private set; } = true;

	[Group( "Jump" )] public float JumpHeight { get; private set; } = 60f;

	/// <summary>
	/// Extra time during which a player can jump after falling off a ledge
	/// </summary>
	[Group( "Jump" )]
	public float JumpGracePeriod { get; private set; } = 0.2f;

	/// <summary>
	/// Time allowed to buffer a jump input. If we land or wallrun within this time, we will jump
	/// </summary>
	[Group( "Jump" )]
	public float JumpBufferTime { get; private set; } = 1 / 128f;

	/// <summary>
	/// Extra time during which a player can change their direction with keyboard input after jumping (at full strength)
	/// </summary>
	[Group( "Jump" )]
	public float JumpKeyboardGracePeriodMin { get; private set; } = 0.2f;

	/// <summary>
	/// Extra time during which a player can change their direction with keyboard input after jumping (fades to 0 strength at this time)
	/// </summary>
	[Group( "Jump" )]
	public float JumpKeyboardGracePeriodMax { get; private set; } = 0.5f;

	/// <summary>
	/// Amount of velocity change allowed during JumpKeyboardGracePeriod, as a fraction of sprinting speed
	/// </summary>
	[Group( "Jump" )]
	public float JumpKeyboardGraceMax { get; private set; } = 0.7f;


	/// <summary>
	/// Fraction of change toward the new direction when pressing a direction during JumpKeyboardGracePeriod
	/// </summary>
	[Group( "Jump" )]
	public float JumpKeyboardGraceStrength { get; private set; } = 0.7f;

	[Group( "Jump" )] public float AirJumpHeight { get; private set; } = 60f;

	/// <summary>
	/// Horizontal movement speed when air jumping while pushing in a direction
	/// </summary>
	[Group( "Jump" )]
	public float AirJumpHorizontalSpeed { get; private set; } = 180f;

	/// <summary>
	/// Minimum fraction of desired superjump height that is acheived, even if already moving quickly upwards
	/// </summary>
	[Group( "Jump" )]
	public float AirJumpMinHeightFraction { get; private set; } = 0.25f;

	[Group( "Jump" )] public int AirJumpMaxJumps { get; private set; } = 1;

	/// <summary>
	/// Cosine of max angle from forward that you can sprint
	/// </summary>
	[Group( "Sprint" )]
	public float SprintMaxAngleDot { get; private set; } = 0.6f;

	/// <summary>
	/// Acceleration of sprint view tilt fraction
	/// </summary>
	[Group( "Sprint" )]
	public float SprintTiltAccel { get; private set; } = 35f;


	/// <summary>
	/// Maximum speed of sprint view tilt
	/// </summary>
	[Group( "Sprint" )]
	public float SprintTiltMaxVel { get; private set; } = 2f;

	/// <summary>
	/// Max turn rate that creates view tilt when sprinting
	/// </summary>
	[Group( "Sprint" )]
	public float SprintTiltTurnRange { get; private set; } = 120f;

	[Group( "Sprint" )] public float SprintTiltMaxRoll { get; private set; } = 2f;

	/// <summary>
	/// How long to wait before transitioning into SprintViewOffset
	/// </summary>
	[Group( "Sprint" )]
	public float SprintOffsetStartDelay { get; private set; } = 0.2f;

	/// <summary>
	/// How long to take to transition into SprintViewOffset
	/// </summary>
	[Group( "Sprint" )]
	public float SprintOffsetStartDuration { get; private set; } = 0.8f;

	/// <summary>
	/// How long to take to transition into SprintViewOffset after landing from a sprinting jump
	/// </summary>
	[Group( "Sprint" )]
	public float SprintOffsetStartFastDuration { get; private set; } = 0.2f;

	/// <summary>
	/// How long to wait before transitioning into sprintViewOffset
	/// </summary>
	[Group( "Sprint" )]
	public float SprintOffsetEndDuration { get; private set; } = 0.15f;

	/// <summary>
	/// How far to slide the player's down as they enter a sprinting state.
	/// </summary>
	[Group( "Sprint" )]
	public float SprintViewOffset { get; private set; } = -6f;

	/// <summary>
	/// How much to reduce the sprint bob after sprinting for SprintBobSmoothingTime
	/// </summary>
	[Group( "Sprint" )]
	public float SprintBobSmoothingReductionFactor { get; private set; } = 0.2f;

	/// <summary>
	/// Sprint bob is smoothed over this time
	/// </summary>
	[Group( "Sprint" )]
	public float SprintBobSmoothingTime { get; private set; } = 3f;

	[Group( "Sprint" )] public float SprintBobCycleTime { get; private set; } = 0.55f;

	/// <summary>
	/// How long until boosted slide is available again.
	/// </summary>
	[Group( "Slide" )]
	public float SlideBoostCooldown { get; private set; } = 2f;

	/// <summary>
	/// Velocity reduction when going up a step (is multiplied by step height)
	/// </summary>
	[Group( "Slide" )]
	public float SlideStepVelocityReduction { get; private set; } = 10f;

	/// <summary>
	/// Deceleration while sliding (fixed velocity lost per second)
	/// </summary>
	[Group( "Slide" )]
	public float SlideDecel { get; private set; } = 50f;

	/// <summary>
	/// Deceleration while sliding (percentage of velocity retained per second; has a stronger effect at faster speeds)
	/// </summary>
	[Group( "Slide" )]
	public float SlideVelocityDecay { get; private set; } = 0.7f;

	/// <summary>
	/// How much boost to apply when initiating a boosted slide
	/// </summary>
	[Group( "Slide" )]
	public float SlideSpeedBoost { get; private set; } = 150f;

	/// <summary>
	/// Slide boost may not bring player speed above this
	/// </summary>
	[Group( "Slide" )]
	public float SlideSpeedBoostCap { get; private set; } = 400f;

	/// <summary>
	/// Cosine of max angle from forward that you can slide when sprinting
	/// </summary>
	[Group( "Slide" )]
	public float SlideMaxAngleDot { get; private set; } = 0.6f;

	/// <summary>
	/// Speed required to start a slide
	/// </summary>
	[Group( "Slide" )]
	public float SlideRequiredStartSpeed { get; private set; } = 200f;

	[Group( "Slide" )] public float SlideJumpHeight { get; private set; } = 50f;

	/// <summary>
	/// Slide may not end above this speed
	/// </summary>
	[Group( "Slide" )]
	public float SlideForceSlideSpeed { get; private set; } = 350f;

	/// <summary>
	/// Deceleration while sliding when the player is trying to stand up
	/// </summary>
	[Group( "Slide" )]
	public float SlideForceSlideUnduckDecel { get; private set; } = 350f;

	/// <summary>
	/// Slide ends when below this speed
	/// </summary>
	[Group( "Slide" )]
	public float SlideEndSpeed { get; private set; } = 125f;

	/// <summary>
	/// FOV is lerped in over this time
	/// </summary>
	[Group( "Slide" )]
	public float SlideFovLerpInTime { get; private set; } = 0.25f;

	/// <summary>
	/// FOV is lerped out over this time
	/// </summary>
	[Group( "Slide" )]
	public float SlideFovLerpOutTime { get; private set; } = 0.25f;

	/// <summary>
	/// FOV is scaled by this amount while sliding
	/// </summary>
	[Group( "Slide" )]
	public float SlideFovScale { get; private set; } = 1.1f;

	/// <summary>
	/// Speed at which view tilt is full while sliding
	/// </summary>
	[Group( "Slide" )]
	public float SlideTiltPlayerSpeed { get; private set; } = 400f;

	/// <summary>
	/// Speed at which view tilt fraction increases
	/// </summary>
	[Group( "Slide" )]
	public float SlideTiltIncreaseSpeed { get; private set; } = 5f;

	/// <summary>
	/// Speed at which view tilt fraction decreases
	/// </summary>
	[Group( "Slide" )]
	public float SlideTiltDecreaseSpeed { get; private set; } = 2.5f;

	/// <summary>
	/// View tilt when looking to the side while sliding in degrees
	/// </summary>
	[Group( "Slide" )]
	public float SlideTiltMaxRoll { get; private set; } = 15f;

	/// <summary>
	/// Time after landing that is considered "skipping" if the player jumps again
	/// </summary>
	[Group( "Jump" )]
	public float SkipTime { get; private set; } = 0.1f;

	/// <summary>
	/// Speed loss doesn’t go below this
	/// </summary>
	[Group( "Jump" )]
	public float SkipSpeedRetain { get; private set; } = 450f;

	/// <summary>
	/// Speed lost when skipping
	/// </summary>
	[Group( "Jump" )]
	public float SkipSpeedReduce { get; private set; } = 12f;

	/// <summary>
	/// Jump height fraction when skipping
	/// </summary>
	[Group( "Jump" )]
	public float SkipJumpHeightFraction { get; private set; } = 0.75f;

	/// <summary>
	/// Bigger number increases the speed at which the view corrects.
	/// </summary>
	[Group( "View Punch" )]
	public float ViewPunchSpringConstant { get; private set; } = 65f;

	/// <summary>
	/// Bigger number makes the response more damped.
	/// </summary>
	[Group( "View Punch" )]
	public float ViewPunchSpringDamping { get; private set; } = 9f;

	[Group( "View Punch" )] public float ViewPunchJumpPitchMin { get; private set; } = 2.2f;
	[Group( "View Punch" )] public float ViewPunchJumpPitchMax { get; private set; } = 3.4f;
	[Group( "View Punch" )] public float ViewPunchJumpYawMin { get; private set; } = -0.7f;
	[Group( "View Punch" )] public float ViewPunchJumpYawMax { get; private set; } = 0.7f;
	[Group( "View Punch" )] public float ViewPunchJumpRollMin { get; private set; } = 0.85f;
	[Group( "View Punch" )] public float ViewPunchJumpRollMax { get; private set; } = 1.5f;
	[Group( "View Punch" )] public float ViewPunchAirJumpPitchMin { get; private set; } = 5.58f;
	[Group( "View Punch" )] public float ViewPunchAirJumpPitchMax { get; private set; } = 6.88f;
	[Group( "View Punch" )] public float ViewPunchAirJumpYawMin { get; private set; } = -0.7f;
	[Group( "View Punch" )] public float ViewPunchAirJumpYawMax { get; private set; } = 0.7f;
	[Group( "View Punch" )] public float ViewPunchAirJumpRollMin { get; private set; } = -1.2f;
	[Group( "View Punch" )] public float ViewPunchAirJumpRollMax { get; private set; } = 0.9f;
	[Group( "View Punch" )] public float ViewPunchFallPitchMin { get; private set; } = 2.2f;
	[Group( "View Punch" )] public float ViewPunchFallPitchMax { get; private set; } = 3.4f;
	[Group( "View Punch" )] public float ViewPunchFallYawMin { get; private set; } = 0f;
	[Group( "View Punch" )] public float ViewPunchFallYawMax { get; private set; } = 0f;
	[Group( "View Punch" )] public float ViewPunchFallRollMin { get; private set; } = -0.62f;
	[Group( "View Punch" )] public float ViewPunchFallRollMax { get; private set; } = 0.62f;
	[Group( "View Punch" )] public float ViewPunchFallDistMin { get; private set; } = 10f;
	[Group( "View Punch" )] public float ViewPunchFallDistMax { get; private set; } = 70f;
	[Group( "View Punch" )] public float ViewPunchFallDistMaxScale { get; private set; } = 12f;
	[Group( "View Punch" )] public float ViewPunchWallrunStartPitchMin { get; private set; } = -1.93f;
	[Group( "View Punch" )] public float ViewPunchWallrunStartPitchMax { get; private set; } = -2.42f;
	[Group( "View Punch" )] public float ViewPunchWallrunStartYawMin { get; private set; } = -0.33f;
	[Group( "View Punch" )] public float ViewPunchWallrunStartYawMax { get; private set; } = 0.33f;
	[Group( "View Punch" )] public float ViewPunchWallrunStartRollMin { get; private set; } = 6.2f;
	[Group( "View Punch" )] public float ViewPunchWallrunStartRollMax { get; private set; } = 6.67f;
	[Group( "Camera" )] public float StepSmoothingOffsetCorrectSpeed { get; private set; } = 80f;
	[ToggleGroup( "Wallrun" )] public bool Wallrun { get; private set; } = true;

	[ToggleGroup( "Wallrun" )] public float WallrunTimeLimit { get; private set; } = 1.75f;

	[ToggleGroup( "Wallrun" )] public float WallrunGravityRampUpTime { get; private set; } = 1f;

	[ToggleGroup( "Wallrun" )] public float WallrunUpWallBoost { get; private set; } = 250f;

	[ToggleGroup( "Wallrun" )] public float WallrunJumpOutwardSpeed { get; private set; } = 205f;

	[ToggleGroup( "Wallrun" )] public float WallrunJumpUpSpeed { get; private set; } = 230f;

	[ToggleGroup( "Wallrun" )] public float WallrunJumpInputDirSpeed { get; private set; } = 75f;

	[ToggleGroup( "Wallrun" )] public float WallrunFallAwaySpeed { get; private set; } = 70f;

	[ToggleGroup( "Wallrun" )] public float WallrunMaxSpeedHorizontal { get; private set; } = 340f;

	[ToggleGroup( "Wallrun" )] public float WallrunMaxSpeedBackwards { get; private set; } = 50f;

	[ToggleGroup( "Wallrun" )] public float WallrunMaxSpeedVertical { get; private set; } = 225f;

	[ToggleGroup( "Wallrun" )] public float WallrunAccelerationHorizontal { get; private set; } = 1400f;

	[ToggleGroup( "Wallrun" )] public float WallrunAccelerationVertical { get; private set; } = 360f;

	[ToggleGroup( "Wallrun" )] public float WallrunAvoidTopWallDecel { get; private set; } = 3000f;

	[ToggleGroup( "Wallrun" )] public float WallrunFriction { get; private set; } = 4f;

	/// <summary>
	/// Time wall running before you start to slip down
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunSlipStartTime { get; private set; } = 2f;

	/// <summary>
	/// Time it takes for slipping to become completely gravity based
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunSlipDuration { get; private set; } = 1f;

	/// <summary>
	/// Min fraction of slip behavior when not pushing in any direction (applies more gravity)
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunNoInputSlipFrac { get; private set; } = 0.7f;

	/// <summary>
	/// Pushing away from the wall for this many seconds causes you to fall off
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunPushAwayFallOffTime { get; private set; } = 0.05f;

	/// <summary>
	/// The amount of automatic up-the-wall input applied when the player pushes
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunUpwardAutoPush { get; private set; } = 0.65f;

	/// <summary>
	/// Dot product threshold for preventing wall running on the same wall twice
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunSameWallDot { get; private set; } = 0.9f;

	/// <summary>
	/// Height, relative to where the player last touched the wall, above which he cannot reattach
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunSameWallHeight { get; private set; } = 0f;

	/// <summary>
	/// Distance to the side that feet may be from the wall
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunAllowedWallDist { get; private set; } = 13f;

	/// <summary>
	/// Cosine of maximum angle the wall can change away from you without falling off
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunAngleChangeMinCos { get; private set; } = 0.8f;

	/// <summary>
	/// Maximum rotation speed around a wall in radians per second; avoids sticking to walls that do tight curves
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunRotateMaxRate { get; private set; } = 3f;

	/// <summary>
	/// Speed at which the view tilts while wall running
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunTiltSpeed { get; private set; } = 4f;

	/// <summary>
	/// Speed at which view tilts back to normal before running out of time
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunTiltEndSpeed { get; private set; } = 1.33f;

	/// <summary>
	/// Time before you start wall running where your view starts tilting. Predicts upcoming wall running
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunTiltPredictTime { get; private set; } = 0.25f;

	/// <summary>
	/// Amount of roll applied to the view in degrees while wall running
	/// </summary>
	[ToggleGroup( "Wallrun" )]
	public float WallrunTiltMaxRoll { get; private set; } = 15f;

	[ToggleGroup( "Wallrun" )] public float WallrunViewYawOffset { get; private set; } = 80f;

	[ToggleGroup( "Wallrun" )] public float WallrunViewPitchOffsetMin { get; private set; } = 30f;

	[ToggleGroup( "Wallrun" )] public float WallrunViewPitchOffsetMax { get; private set; } = 50f;

	[ToggleGroup( "Wallrun" )] public float WallrunViewPitchOffsetCorrectSpeed { get; private set; } = 35f;

	[Group( "Footsteps" )] public float FootStepWalkInterval { get; private set; } = 0.4f;
	[Group( "Footsteps" )] public float FootStepSprintInterval { get; private set; } = 0.3f;
	[Group( "Footsteps" )] public float FootStepDuckIntervalAdd { get; private set; } = 0.1f;
	[Group( "Footsteps" )] public float FootStepDuckWalkSpeed { get; private set; } = 80f;
	[Group( "Footsteps" )] public float FootStepDuckRunSpeed { get; private set; } = 120f;
	[Group( "Footsteps" )] public float FootStepNormalWalkSpeed { get; private set; } = 30f;
	[Group( "Footsteps" )] public float FootStepNormalRunSpeed { get; private set; } = 220f;
	[Group( "View Punch" )] public float HardFallDist { get; private set; } = 20f;
}
