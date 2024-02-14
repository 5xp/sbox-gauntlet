﻿namespace Tf;

/// <summary>
/// Player's viewpunch mechanic.
/// Responsible for punching the view when landing, jumping, airjumping, and wallrunning.
/// </summary>
public partial class ViewPunchMechanic : BasePlayerControllerMechanic
{
	public DampedSpring Spring { get; set; }

	public override int Priority => 500;

	public override bool ShouldBecomeActive() => true;

	protected override void OnAwake()
	{
		base.OnAwake();
		Spring = new( PlayerSettings.ViewPunchSpringConstant, PlayerSettings.ViewPunchSpringDamping );
	}

	public override void OnActiveUpdate()
	{
		float beforeVel = Spring.Velocity.x;


		DebugViewPunch( beforeVel, Spring.Velocity.x );

		if ( HasAnyTag( "jump", "walljump" ) )
		{
			DoJumpPunch();
		}

		if ( HasTag( "airjump" ) )
		{
			DoAirJumpPunch();
		}

		if ( Controller.TimeSinceLastLanding == 0f )
		{
			DoFallPunch();
		}

		if ( Controller.GetMechanic<WallrunMechanic>().TimeSinceStart == 0f )
		{
			DoFallPunch();
			DoWallrunStartPunch();
		}
	}

	public override void FrameSimulate()
	{
		if ( IsActive )
			Spring.Update( Time.Delta );
	}

	private void DoJumpPunch()
	{
		var (min, max) = GetViewPunchJumpVectors();
		Spring.AddRandomVelocity( min, max );
	}

	private void DoAirJumpPunch()
	{
		var (min, max) = GetViewPunchAirJumpVectors();
		Spring.AddRandomVelocity( min, max );
	}

	private void DoFallPunch()
	{
		float fallDist = Controller.ApexHeight - Position.z;

		if ( fallDist <= 0f )
			return;

		float fallFraction = GetFallFraction( fallDist );

		var (min, max) = GetViewPunchFallVectors();

		min *= fallFraction;
		max *= fallFraction;

		Spring.AddRandomVelocity( min, max );
	}

	private void DoWallrunStartPunch()
	{
		WallrunMechanic wallrun = Controller.GetMechanic<WallrunMechanic>();
		Vector3 wallHorizontal = wallrun.LastWallNormal.Cross( Vector3.Up );
		float forwardFraction = -1f * Controller.EyeAngles.WithPitch( 0f ).Forward.Dot( wallHorizontal );

		var (min, max) = GetViewPunchWallrunStartVectors( forwardFraction );
		Spring.AddRandomVelocity( min, max );
	}

	private float GetFallFraction( float fallDist )
	{
		if ( fallDist <= PlayerSettings.ViewPunchFallDistMin )
		{
			return MathUtils.EaseOutSine( fallDist / PlayerSettings.ViewPunchFallDistMin );
		}
		else
		{
			float t = fallDist.LerpInverse( PlayerSettings.ViewPunchFallDistMin, PlayerSettings.ViewPunchFallDistMax );
			return MathX.Lerp( 1f, PlayerSettings.ViewPunchFallDistMaxScale, t );
		}
	}

	private (Vector3 min, Vector3 max) GetViewPunchJumpVectors()
	{
		float pitchMin = PlayerSettings.ViewPunchJumpPitchMin;
		float pitchMax = PlayerSettings.ViewPunchJumpPitchMax;
		float yawMin = PlayerSettings.ViewPunchJumpYawMin;
		float yawMax = PlayerSettings.ViewPunchJumpYawMax;
		float rollMin = PlayerSettings.ViewPunchJumpRollMin;
		float rollMax = PlayerSettings.ViewPunchJumpRollMax;

		Vector3 minPunch = new( pitchMin, yawMin, rollMin );
		Vector3 maxPunch = new( pitchMax, yawMax, rollMax );

		return (minPunch, maxPunch);
	}

	private (Vector3 min, Vector3 max) GetViewPunchAirJumpVectors()
	{
		float pitchMin = PlayerSettings.ViewPunchAirJumpPitchMin;
		float pitchMax = PlayerSettings.ViewPunchAirJumpPitchMax;
		float yawMin = PlayerSettings.ViewPunchAirJumpYawMin;
		float yawMax = PlayerSettings.ViewPunchAirJumpYawMax;
		float rollMin = PlayerSettings.ViewPunchAirJumpRollMin;
		float rollMax = PlayerSettings.ViewPunchAirJumpRollMax;

		Vector3 minPunch = new( pitchMin, yawMin, rollMin );
		Vector3 maxPunch = new( pitchMax, yawMax, rollMax );

		return (minPunch, maxPunch);
	}

	private (Vector3 min, Vector3 max) GetViewPunchFallVectors()
	{
		float pitchMin = PlayerSettings.ViewPunchFallPitchMin;
		float pitchMax = PlayerSettings.ViewPunchFallPitchMax;
		float yawMin = PlayerSettings.ViewPunchFallYawMin;
		float yawMax = PlayerSettings.ViewPunchFallYawMax;
		float rollMin = PlayerSettings.ViewPunchFallRollMin;
		float rollMax = PlayerSettings.ViewPunchFallRollMax;

		Vector3 minPunch = new( pitchMin, yawMin, rollMin );
		Vector3 maxPunch = new( pitchMax, yawMax, rollMax );

		return (minPunch, maxPunch);
	}

	private (Vector3 min, Vector3 max) GetViewPunchWallrunStartVectors( float forwardFraction )
	{
		float pitchMin = PlayerSettings.ViewPunchWallrunStartPitchMin;
		float pitchMax = PlayerSettings.ViewPunchWallrunStartPitchMax;
		float yawMin = PlayerSettings.ViewPunchWallrunStartYawMin * forwardFraction;
		float yawMax = PlayerSettings.ViewPunchWallrunStartYawMax * forwardFraction;
		float rollMin = PlayerSettings.ViewPunchWallrunStartRollMin * forwardFraction;
		float rollMax = PlayerSettings.ViewPunchWallrunStartRollMax * forwardFraction;

		Vector3 minPunch = new( pitchMin, yawMin, rollMin );
		Vector3 maxPunch = new( pitchMax, yawMax, rollMax );

		return (minPunch, maxPunch);
	}

	private void DebugViewPunch( float beforeSpeed, float afterSpeed )
	{
		if ( !PlayerController.DebugViewPunch )
		{
			return;
		}

		if ( !Spring.Position.x.AlmostEqual( 0f, 0.1f ) && (afterSpeed.AlmostEqual( 0f ) || (afterSpeed < 0f && beforeSpeed > 0f)) )
		{
			Log.Info( $"Pitch {Spring.Position.x}, Yaw: {Spring.Position.y}, Roll: {Spring.Position.z}" );
		}
	}
}