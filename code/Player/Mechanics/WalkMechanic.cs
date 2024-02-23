﻿namespace Tf;

/// <summary>
/// A walking mechanic.
/// </summary>
public partial class WalkMechanic : BasePlayerControllerMechanic
{
	public override bool ShouldBecomeActive()
	{
		JumpMechanic jump = Controller.GetMechanic<JumpMechanic>();

		if ( !jump.IsActive && jump.ShouldBecomeActive() )
			return false;

		return true;

	}

	public override IEnumerable<string> GetTags()
	{
		yield return "walk";
	}

	public override void OnActiveUpdate()
	{
		if ( Controller.IsGrounded )
			WalkMove();

		CategorizePosition( Controller.IsGrounded );
	}

	private void WalkMove()
	{
		Vector3 wishDir = Controller.BuildWishDir();
		float wishSpeed = Controller.GetWishSpeed();

		float startZ = Position.z;

		Velocity = HorzVelocity;

		float accel = Controller.CurrentAccelerationOverride ?? PlayerSettings.Acceleration;
		Decelerate( wishDir, wishSpeed, accel * 0.6f );
		Accelerate( wishDir, wishSpeed, accel );

		if ( Velocity.LengthSquared < 1f )
		{
			Velocity = Vector3.Zero;
			return;
		}

		Vector3 dest = (Position + Velocity * Time.Delta).WithZ( Position.z );
		SceneTraceResult tr = Controller.TraceBBox( Position, dest );

		if ( tr.Fraction == 1f )
		{
			Position = tr.EndPosition;
			StayOnGround();
			OnStep( Position.z - startZ );
			return;
		}

		Controller.StepMove( PlayerSettings.GroundAngle, PlayerSettings.StepHeightMax );
		StayOnGround();
		OnStep( Position.z - startZ );
	}

	private void StayOnGround()
	{
		Vector3 start = Position + Vector3.Up * 2f;
		Vector3 end = Position + Vector3.Down * PlayerSettings.StepHeightMax;

		SceneTraceResult tr = Controller.TraceBBox( Controller.Position, start );
		start = tr.EndPosition;

		tr = Controller.TraceBBox( start, end );

		if ( tr.Fraction <= 0f ) return;
		if ( tr.Fraction >= 1 ) return;
		if ( tr.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up, tr.Normal ) > PlayerSettings.GroundAngle ) return;

		Position = tr.EndPosition;
	}

	private void OnStep( float stepAmount )
	{
		ApplySlideStepVelocityReduction( stepAmount );
		Controller.AddStepOffset( Vector3.Down * stepAmount );
	}

	private void ApplySlideStepVelocityReduction( float stepAmount )
	{
		if ( !HasTag( "slide" ) || stepAmount < PlayerSettings.StepHeightMin )
			return;

		Velocity = Velocity.Approach( 0f, stepAmount * PlayerSettings.SlideStepVelocityReduction );
	}

	private void Accelerate( Vector3 wishDir, float wishSpeed, float acceleration )
	{
		float currentSpeed = Velocity.Dot( wishDir );
		float addSpeed = MathF.Max( 0f, wishSpeed - currentSpeed );
		float accelSpeed = MathF.Min( acceleration * Time.Delta, addSpeed );

		float magnitudeSquared = MathF.Max( wishSpeed * wishSpeed, Velocity.LengthSquared );

		Velocity += wishDir * accelSpeed;

		Velocity = Velocity.ClampLength( MathF.Sqrt( magnitudeSquared ) );
	}

	private void Decelerate( Vector3 wishDir, float wishSpeed, float deceleration )
	{
		float projSpeed = MathF.Min( wishSpeed, Velocity.Dot( wishDir ) );
		Vector3 proj = wishDir * projSpeed;
		Vector3 decel = proj - Velocity;

		decel = decel.ClampLength( deceleration * Time.Delta );

		Velocity += decel;
	}

	public void SetGroundObject( GameObject groundObject )
	{
		Controller.LastGroundObject = Controller.GroundObject;
		Controller.GroundObject = groundObject;

		if ( groundObject is not null )
		{
			Velocity = HorzVelocity;
			Controller.TimeSinceLastOnGround = 0;
			Controller.OnLanded?.Invoke();
		}

		if ( Controller.LastGroundObject is null && groundObject is not null )
		{
			Controller.TimeSinceLastLanding = 0;
		}
	}

	private void CategorizePosition( bool stayOnGround )
	{
		Vector3 point = Position + Vector3.Down * 2f;
		Vector3 bumpOrigin = Position;
		bool movingUpRapidly = Velocity.z > PlayerSettings.MaxNonJumpVelocity;
		bool moveToEndPos = false;

		if ( movingUpRapidly )
		{
			Controller.ClearGroundObject();
			return;
		}

		if ( Controller.IsGrounded )
		{
			moveToEndPos = true;
			point.z -= PlayerSettings.StepHeightMax;
		}
		else if ( stayOnGround )
		{
			moveToEndPos = true;
			point.z -= PlayerSettings.StepHeightMax;
		}

		SceneTraceResult tr = Controller.TraceBBox( bumpOrigin, point, 4.0f );
		float angle = Vector3.GetAngle( Vector3.Up, tr.Normal );

		if ( tr.GameObject is null || angle > PlayerSettings.GroundAngle )
		{
			Controller.ClearGroundObject();
			moveToEndPos = false;
		}
		else
		{
			UpdateGroundObject( tr );
		}

		if ( moveToEndPos && !tr.StartedSolid && tr.Fraction > 0f && tr.Fraction < 1f )
		{
			Position = tr.EndPosition;
		}
	}

	private void UpdateGroundObject( SceneTraceResult tr )
	{
		Controller.GroundNormal = tr.Normal;
		SetGroundObject( tr.GameObject );
	}
}
