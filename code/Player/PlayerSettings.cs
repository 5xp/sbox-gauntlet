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

	public bool IsDebug { get; private set; }
	public float HullRadius { get; private set; } = 16f;
	public float HullHeightStanding { get; private set; } = 72f;
	public float HullHeightCrouching { get; private set; } = 47f;
	public float ViewHeightStanding { get; private set; } = 60f;
	public float ViewHeightCrouching { get; private set; } = 38f;
	public float Gravity { get; private set; } = 750f;
	public float GravityScale { get; private set; } = 0.75f;
	public float StepHeightMax { get; private set; } = 18f;
	public float StepHeightMin { get; private set; } = 4f;
	public float GroundAngle { get; private set; } = 46f;
	public float MaxNonJumpVelocity { get; private set; } = 140f;
	public float Acceleration { get; private set; } = 2500f;
	public float WalkSpeed { get; private set; } = 162.5f;
	public float CrouchSpeed { get; private set; } = 80f;
	public float SprintSpeed { get; private set; } = 243f;
	public float AirSpeed { get; private set; } = 60f;
	public float AirAcceleration { get; private set; } = 500f;
	public float ExtraAirAcceleration { get; private set; } = 2f;
	public float PitchMaxUp { get; private set; } = 85f;
	public float PitchMaxDown { get; private set; } = 89f;
	public float DuckSpeed { get; private set; } = 2f;
	public float UnduckSpeed { get; private set; } = 5f;
	public bool CanJumpWhileUnducking { get; private set; } = true;
	public float JumpHeight { get; private set; } = 60f;
	public float JumpGracePeriod { get; private set; } = 0.2f;
	public float JumpBufferTime { get; private set; } = 1 / 128f;
	public float JumpKeyboardGracePeriodMin { get; private set; } = 0.2f;
	public float JumpKeyboardGracePeriodMax { get; private set; } = 0.5f;
	public float JumpKeyboardGraceMax { get; private set; } = 0.7f;
	public float JumpKeyboardGraceStrength { get; private set; } = 0.7f;
	public float AirJumpHeight { get; private set; } = 60f;
	public float AirJumpHorizontalSpeed { get; private set; } = 180f;
	public float AirJumpMinHeightFraction { get; private set; } = 0.25f;
	public int AirJumpMaxJumps { get; private set; } = 1;
	public float SprintMaxAngleDot { get; private set; } = 0.6f;
	public float SprintTiltAccel { get; private set; } = 35f;
	public float SprintTiltMaxVel { get; private set; } = 2f;
	public float SprintTiltTurnRange { get; private set; } = 120f;
	public float SprintTiltMaxRoll { get; private set; } = 2f;
	public float SprintOffsetStartDelay { get; private set; } = 0.2f;
	public float SprintOffsetStartDuration { get; private set; } = 0.8f;
	public float SprintOffsetStartFastDuration { get; private set; } = 0.2f;
	public float SprintOffsetEndDuration { get; private set; } = 0.15f;
	public float SprintViewOffset { get; private set; } = -6f;
	public float SprintBobSmoothingReductionFactor { get; private set; } = 0.2f;
	public float SprintBobSmoothingTime { get; private set; } = 3f;
	public float SprintBobCycleTime { get; private set; } = 0.55f;
	public float SlideBoostCooldown { get; private set; } = 2f;
	public float SlideMaxAngleDot { get; private set; } = 0.6f;
	public float SlideStepVelocityReduction { get; private set; } = 10f;
	public float SlideDecel { get; private set; } = 50f;
	public float SlideVelocityDecay { get; private set; } = 0.7f;
	public float SlideSpeedBoost { get; private set; } = 150f;
	public float SlideSpeedBoostCap { get; private set; } = 400f;
	public float SlideRequiredStartSpeed { get; private set; } = 200f;
	public float SlideJumpHeight { get; private set; } = 50f;
	public float SlideForceSlideSpeed { get; private set; } = 350f;
	public float SlideForceSlideUnduckDecel { get; private set; } = 350f;
	public float SlideEndSpeed { get; private set; } = 125f;
	public float SlideFovLerpInTime { get; private set; } = 0.25f;
	public float SlideFovLerpOutTime { get; private set; } = 0.25f;
	public float SlideFovScale { get; private set; } = 1.1f;
	public float SlideTiltPlayerSpeed { get; private set; } = 400f;
	public float SlideTiltIncreaseSpeed { get; private set; } = 5f;
	public float SlideTiltDecreaseSpeed { get; private set; } = 2.5f;
	public float SlideTiltMaxRoll { get; private set; } = 15f;
	public float SkipTime { get; private set; } = 0.1f;
	public float SkipSpeedRetain { get; private set; } = 450f;
	public float SkipSpeedReduce { get; private set; } = 12f;
	public float SkipJumpHeightFraction { get; private set; } = 0.75f;
	public float ViewPunchSpringConstant { get; private set; } = 65f;
	public float ViewPunchSpringDamping { get; private set; } = 9f;
	public float ViewPunchJumpPitchMin { get; private set; } = 2.2f;
	public float ViewPunchJumpPitchMax { get; private set; } = 3.4f;
	public float ViewPunchJumpYawMin { get; private set; } = -0.7f;
	public float ViewPunchJumpYawMax { get; private set; } = 0.7f;
	public float ViewPunchJumpRollMin { get; private set; } = 0.85f;
	public float ViewPunchJumpRollMax { get; private set; } = 1.5f;
	public float ViewPunchAirJumpPitchMin { get; private set; } = 5.58f;
	public float ViewPunchAirJumpPitchMax { get; private set; } = 6.88f;
	public float ViewPunchAirJumpYawMin { get; private set; } = -0.7f;
	public float ViewPunchAirJumpYawMax { get; private set; } = 0.7f;
	public float ViewPunchAirJumpRollMin { get; private set; } = -1.2f;
	public float ViewPunchAirJumpRollMax { get; private set; } = 0.9f;
	public float ViewPunchFallPitchMin { get; private set; } = 2.2f;
	public float ViewPunchFallPitchMax { get; private set; } = 3.4f;
	public float ViewPunchFallYawMin { get; private set; } = 0f;
	public float ViewPunchFallYawMax { get; private set; } = 0f;
	public float ViewPunchFallRollMin { get; private set; } = -0.62f;
	public float ViewPunchFallRollMax { get; private set; } = 0.62f;
	public float ViewPunchFallDistMin { get; private set; } = 10f;
	public float ViewPunchFallDistMax { get; private set; } = 70f;
	public float ViewPunchFallDistMaxScale { get; private set; } = 12f;
	public float ViewPunchWallrunStartPitchMin { get; private set; } = -1.93f;
	public float ViewPunchWallrunStartPitchMax { get; private set; } = -2.42f;
	public float ViewPunchWallrunStartYawMin { get; private set; } = -0.33f;
	public float ViewPunchWallrunStartYawMax { get; private set; } = 0.33f;
	public float ViewPunchWallrunStartRollMin { get; private set; } = 6.2f;
	public float ViewPunchWallrunStartRollMax { get; private set; } = 6.67f;
	public float StepSmoothingOffsetCorrectSpeed { get; private set; } = 80f;
	public bool WallrunEnable { get; private set; } = true;
	public float WallrunTimeLimit { get; private set; } = 1.75f;
	public float WallrunGravityRampUpTime { get; private set; } = 1f;
	public float WallrunUpWallBoost { get; private set; } = 250f;
	public float WallrunJumpOutwardSpeed { get; private set; } = 205f;
	public float WallrunJumpUpSpeed { get; private set; } = 230f;
	public float WallrunJumpInputDirSpeed { get; private set; } = 75f;
	public float WallrunFallAwaySpeed { get; private set; } = 70f;
	public float WallrunMaxSpeedHorizontal { get; private set; } = 340f;
	public float WallrunMaxSpeedBackwards { get; private set; } = 50f;
	public float WallrunMaxSpeedVertical { get; private set; } = 225f;
	public float WallrunAccelerationHorizontal { get; private set; } = 1400f;
	public float WallrunAccelerationVertical { get; private set; } = 360f;
	public float WallrunAvoidTopWallDecel { get; private set; } = 3000f;
	public float WallrunFriction { get; private set; } = 4f;
	public float WallrunSlipStartTime { get; private set; } = 2f;
	public float WallrunSlipDuration { get; private set; } = 1f;
	public float WallrunNoInputSlipFrac { get; private set; } = 0.7f;
	public float WallrunPushAwayFallOffTime { get; private set; } = 0.05f;
	public float WallrunUpwardAutoPush { get; private set; } = 0.65f;
	public float WallrunSameWallDot { get; private set; } = 0.9f;
	public float WallrunSameWallHeight { get; private set; } = 0f;
	public float WallrunAllowedWallDist { get; private set; } = 13f;
	public float WallrunAngleChangeMinCos { get; private set; } = 0.8f;
	public float WallrunRotateMaxRate { get; private set; } = 3f;
	public float WallrunTiltSpeed { get; private set; } = 4f;
	public float WallrunTiltEndSpeed { get; private set; } = 1.33f;
	public float WallrunTiltPredictTime { get; private set; } = 0.25f;
	public float WallrunTiltMaxRoll { get; private set; } = 15f;
	public float WallrunViewYawOffset { get; private set; } = 80f;
	public float WallrunViewPitchOffsetMin { get; private set; } = 30f;
	public float WallrunViewPitchOffsetMax { get; private set; } = 50f;
	public float WallrunViewYawCorrectSpeed { get; private set; } = 85f;
	public float WallrunViewPitchOffsetCorrectSpeed { get; private set; } = 35f;
	public float FootStepWalkInterval { get; private set; } = 0.4f;
	public float FootStepSprintInterval { get; private set; } = 0.3f;
	public float FootStepDuckIntervalAdd { get; private set; } = 0.1f;
	public float FootStepDuckWalkSpeed { get; private set; } = 80f;
	public float FootStepDuckRunSpeed { get; private set; } = 120f;
	public float FootStepNormalWalkSpeed { get; private set; } = 30f;
	public float FootStepNormalRunSpeed { get; private set; } = 220f;
	public float HardFallDist { get; private set; } = 20f;
	public float GrappleLength { get; private set; } = 850f;
	public float GrappleShootVel { get; private set; } = 2000f;
	public float GrappleForcedRetractVel { get; private set; } = 3000f;
	public float GrappleRetractVel { get; private set; } = 6000f;
	public float GrappleRetractFallSpeed { get; private set; } = 300f;
	public float GrappleAttachVerticalBoost { get; private set; } = 200f;
	public float GrapplePullDellay { get; private set; } = 0.2f;
	public float GrappleExtraDetachLength { get; private set; } = 256f;
	public int GrappleMaxGrapplePoints { get; private set; } = 3;
	public float GrappleLift { get; private set; } = 25f;
	public float GrappleAcceleration { get; private set; } = 1000f;
	public float GrappleSpeedRampMin { get; private set; } = 50f;
	public float GrappleSpeedRampMax { get; private set; } = 800f;
	public float GrappleSpeedRampTime { get; private set; } = 1.5f;
	public float GrappleAirMaxSpeed { get; private set; } = 420f;
	public float GrappleAirAcceleration { get; private set; } = 650f;
	public float GrappleDeceleration { get; private set; } = 425f;
	public float GrappleAroundObstacleAccel { get; private set; } = 1000f;
	public bool GrappleDontFightGravity { get; private set; } = true;
	public float GrappleLetGravityHelpCosAngle { get; private set; } = 0.8f;
	public float GrappleGravityFracMin { get; private set; } = 0.25f;
	public float GrappleGravityFracMax { get; private set; } = 0.7f;
	public float GrappleGravityPushUnderContributionScale { get; private set; } = 2f;
	public float GrappleRollDistanceMin { get; private set; } = 0f;
	public float GrappleRollDistanceMax { get; private set; } = 150f;
	public float GrappleRollViewAngleMin { get; private set; } = 0f;
	public float GrappleRollViewAngleMax { get; private set; } = 60f;
	public float GrappleMaxRoll { get; private set; } = 20f;
	public float GrappleDetachOnGrappleDebounceTime { get; private set; } = 0.2f;
	public float GrappleDetachLengthMin { get; private set; } = 33f;
	public float GrappleDetachAwaySpeed { get; private set; } = 500f;
	public float GrappleDetachLowSpeedThreshold { get; private set; } = 250f;
	public float GrappleDetachLowSpeedTime { get; private set; } = 1.5f;
	public float GrappleDetachSpeedLoss { get; private set; } = 300f;
	public float GrappleDeatchSpeedLossMin { get; private set; } = 450f;
}
