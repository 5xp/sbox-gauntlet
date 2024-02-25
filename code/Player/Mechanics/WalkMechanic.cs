namespace Tf;

/// <summary>
/// A walking mechanic.
/// </summary>
public partial class WalkMechanic : BasePlayerControllerMechanic
{
	public override int Priority => 5;

	public override bool ShouldBecomeActive()
	{
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
}
