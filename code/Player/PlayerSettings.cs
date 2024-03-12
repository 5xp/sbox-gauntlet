namespace Tf;

public sealed class PlayerSettings
{
	public enum PlayerTypes
	{
		Regular,
		Faster
	};

	public PlayerSettings() { }

	public static PlayerSettings Regular
	{
		get
		{
			if ( WallrunMechanic.DebugWallrunSettings )
			{
				return new PlayerSettings
				{
					WallrunTimeLimit = 9999f,
					WallrunGravityRampUpTime = 9999f,
					WallrunSlipStartTime = 9999f,
					WallrunPushAwayFallOffTime = 9999f,
					WallrunUpwardAutoPush = 0f,
					WallrunNoInputSlipFrac = 0f,
					WallrunTiltMaxRoll = 0f,
				};
			}

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

	public float HullRadius { get; set; } = 16f;
	public float HullHeightStanding { get; set; } = 72f;
	public float HullHeightCrouching { get; set; } = 47f;
	public float ViewHeightStanding { get; set; } = 60f;
	public float ViewHeightCrouching { get; set; } = 38f;
	public float Gravity { get; set; } = 750f;
	public float GravityScale { get; set; } = 0.75f;
	public float StepHeightMax { get; set; } = 18f;
	public float StepHeightMin { get; set; } = 4f;
	public float GroundAngle { get; set; } = 46f;
	public float MaxNonJumpVelocity { get; set; } = 140f;
	public float Acceleration { get; set; } = 2500f;
	public float WalkSpeed { get; set; } = 162.5f;
	public float CrouchSpeed { get; set; } = 80f;
	public float SprintSpeed { get; set; } = 243f;
	public float AirSpeed { get; set; } = 60f;
	public float AirAcceleration { get; set; } = 500f;
	public float ExtraAirAcceleration { get; set; } = 2f;
	public float PitchMaxUp { get; set; } = 85f;
	public float PitchMaxDown { get; set; } = 89f;
	public float DuckSpeed { get; set; } = 2f;
	public float UnduckSpeed { get; set; } = 5f;
	public bool CanJumpWhileUnducking { get; set; } = true;
	public float SprintMaxAngleDot { get; set; } = 0.6f;
	public float SprintTiltAccel { get; set; } = 35f;
	public float JumpHeight { get; set; } = 60f;
	public float JumpGracePeriod { get; set; } = 0.2f;
	public float JumpBufferTime { get; set; } = 1 / 128f;
	public float JumpKeyboardGracePeriodMin { get; set; } = 0.2f;
	public float JumpKeyboardGracePeriodMax { get; set; } = 0.5f;
	public float JumpKeyboardGraceMax { get; set; } = 0.7f;
	public float JumpKeyboardGraceStrength { get; set; } = 0.7f;
	public float AirJumpHeight { get; set; } = 60f;
	public float AirJumpHorizontalSpeed { get; set; } = 180f;
	public float AirJumpMinHeightFraction { get; set; } = 0.25f;
	public int AirJumpMaxJumps { get; set; } = 1;
	public float SprintTiltMaxVel { get; set; } = 2f;
	public float SprintTiltTurnRange { get; set; } = 120f;
	public float SprintTiltMaxRoll { get; set; } = 2f;
	public float SprintOffsetStartDelay { get; set; } = 0.2f;
	public float SprintOffsetStartDuration { get; set; } = 0.8f;
	public float SprintOffsetStartFastDuration { get; set; } = 0.2f;
	public float SprintOffsetEndDuration { get; set; } = 0.15f;
	public float SprintViewOffset { get; set; } = -6f;
	public float SlideBoostCooldown { get; set; } = 2f;
	public float SlideMaxAngleDot { get; set; } = 0.6f;
	public float SlideStepVelocityReduction { get; set; } = 10f;
	public float SlideDecel { get; set; } = 50f;
	public float SlideVelocityDecay { get; set; } = 0.7f;
	public float SlideSpeedBoost { get; set; } = 150f;
	public float SlideSpeedBoostCap { get; set; } = 400f;
	public float SlideRequiredStartSpeed { get; set; } = 200f;
	public float SlideJumpHeight { get; set; } = 50f;
	public float SlideForceSlideSpeed { get; set; } = 350f;
	public float SlideForceSlideUnduckDecel { get; set; } = 350f;
	public float SlideEndSpeed { get; set; } = 125f;
	public float SlideFOVLerpInTime { get; set; } = 0.25f;
	public float SlideFOVLerpOutTime { get; set; } = 0.25f;
	public float SlideFOVScale { get; set; } = 1.1f;
	public float SlideTiltPlayerSpeed { get; set; } = 400f;
	public float SlideTiltIncreaseSpeed { get; set; } = 5f;
	public float SlideTiltDecreaseSpeed { get; set; } = 2.5f;
	public float SlideTiltMaxRoll { get; set; } = 15f;
	public float SkipTime { get; set; } = 0.1f;
	public float SkipSpeedRetain { get; set; } = 450f;
	public float SkipSpeedReduce { get; set; } = 12f;
	public float SkipJumpHeightFraction { get; set; } = 0.75f;
	public float ViewPunchSpringConstant { get; set; } = 65f;
	public float ViewPunchSpringDamping { get; set; } = 9f;
	public float ViewPunchJumpPitchMin { get; set; } = 2.2f;
	public float ViewPunchJumpPitchMax { get; set; } = 3.4f;
	public float ViewPunchJumpYawMin { get; set; } = -0.7f;
	public float ViewPunchJumpYawMax { get; set; } = 0.7f;
	public float ViewPunchJumpRollMin { get; set; } = 0.85f;
	public float ViewPunchJumpRollMax { get; set; } = 1.5f;
	public float ViewPunchAirJumpPitchMin { get; set; } = 5.58f;
	public float ViewPunchAirJumpPitchMax { get; set; } = 6.88f;
	public float ViewPunchAirJumpYawMin { get; set; } = -0.7f;
	public float ViewPunchAirJumpYawMax { get; set; } = 0.7f;
	public float ViewPunchAirJumpRollMin { get; set; } = -1.2f;
	public float ViewPunchAirJumpRollMax { get; set; } = 0.9f;
	public float ViewPunchFallPitchMin { get; set; } = 2.2f;
	public float ViewPunchFallPitchMax { get; set; } = 3.4f;
	public float ViewPunchFallYawMin { get; set; } = 0f;
	public float ViewPunchFallYawMax { get; set; } = 0f;
	public float ViewPunchFallRollMin { get; set; } = -0.62f;
	public float ViewPunchFallRollMax { get; set; } = 0.62f;
	public float ViewPunchFallDistMin { get; set; } = 160f;
	public float ViewPunchFallDistMax { get; set; } = 1160;
	public float ViewPunchFallDistMaxScale { get; set; } = 12f;
	public float ViewPunchWallrunStartPitchMin { get; set; } = -1.93f;
	public float ViewPunchWallrunStartPitchMax { get; set; } = -2.42f;
	public float ViewPunchWallrunStartYawMin { get; set; } = -0.33f;
	public float ViewPunchWallrunStartYawMax { get; set; } = 0.33f;
	public float ViewPunchWallrunStartRollMin { get; set; } = 6.2f;
	public float ViewPunchWallrunStartRollMax { get; set; } = 6.67f;
	public float StepSmoothingOffsetCorrectSpeed { get; set; } = 80f;
	public bool WallrunEnable { get; set; } = true;
	public float WallrunTimeLimit { get; set; } = 1.75f;
	public float WallrunGravityRampUpTime { get; set; } = 1f;
	public float WallrunUpWallBoost { get; set; } = 250f;
	public float WallrunJumpOutwardSpeed { get; set; } = 205f;
	public float WallrunJumpUpSpeed { get; set; } = 230f;
	public float WallrunJumpInputDirSpeed { get; set; } = 75f;
	public float WallrunFallAwaySpeed { get; set; } = 70f;
	public float WallrunMaxSpeedHorizontal { get; set; } = 340f;
	public float WallrunMaxSpeedBackwards { get; set; } = 50f;
	public float WallrunMaxSpeedVertical { get; set; } = 225f;
	public float WallrunAccelerationHorizontal { get; set; } = 1400f;
	public float WallrunAccelerationVertical { get; set; } = 360f;
	public float WallrunAvoidTopWallDecel { get; set; } = 3000f;
	public float WallrunFriction { get; set; } = 4f;
	public float WallrunSlipStartTime { get; set; } = 2f;
	public float WallrunSlipDuration { get; set; } = 1f;
	public float WallrunNoInputSlipFrac { get; set; } = 0.7f;
	public float WallrunPushAwayFallOffTime { get; set; } = 0.05f;
	public float WallrunUpwardAutoPush { get; set; } = 0.65f;
	public float WallrunSameWallDot { get; set; } = 0.9f;
	public float WallrunSameWallHeight { get; set; } = 0f;
	public float WallrunAllowedWallDist { get; set; } = 13f;
	public float WallrunAngleChangeMinCos { get; set; } = 0.8f;
	public float WallrunRotateMaxRate { get; set; } = 3f;
	public float WallrunTiltSpeed { get; set; } = 4f;
	public float WallrunTiltEndSpeed { get; set; } = 1.33f;
	public float WallrunTiltPredictTime { get; set; } = 0.25f;
	public float WallrunTiltMaxRoll { get; set; } = 15f;
	public float WallrunViewYawOffset { get; set; } = 80f;
	public float WallrunViewPitchOffsetMin { get; set; } = 30f;
	public float WallrunViewPitchOffsetMax { get; set; } = 50f;
	public float WallrunViewYawCorrectSpeed { get; set; } = 85f;
	public float WallrunViewPitchOffsetCorrectSpeed { get; set; } = 35f;
	public float FootStepWalkInterval { get; set; } = 0.4f;
	public float FootStepSprintInterval { get; set; } = 0.3f;
	public float FootStepDuckIntervalAdd { get; set; } = 0.1f;
	public float FootStepDuckWalkSpeed { get; set; } = 80f;
	public float FootStepDuckRunSpeed { get; set; } = 120f;
	public float FootStepNormalWalkSpeed { get; set; } = 30f;
	public float FootStepNormalRunSpeed { get; set; } = 220f;
}
