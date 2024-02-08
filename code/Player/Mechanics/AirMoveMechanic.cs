namespace Tf;

public partial class AirMoveMechanic : BasePlayerControllerMechanic
{
	public override bool ShouldBecomeActive() => !Controller.IsGrounded;
	public override float? GetSpeed() => PlayerSettings.AirSpeed;

	public override void OnActiveUpdate()
	{
		bool groundedAtStart = Controller.IsGrounded;

		if ( groundedAtStart )
		{
			return;
		}

		PlayerController ctrl = Controller;

		Vector3 halfGravity = Vector3.Down * 0.5f * PlayerSettings.Gravity * PlayerSettings.GravityScale * Time.Delta;
		Velocity += halfGravity;

		Vector3 wishVel = Controller.BuildWishVelocity();
		Vector3 wishDir = wishVel.Normal;
		float wishSpeed = wishVel.Length;

		Controller.Accelerate( wishDir, wishSpeed, PlayerSettings.AirAcceleration, 2.0f );
		ctrl.Move();
		Velocity += halfGravity;

	}

	private void AirAccelerate( Vector3 wishDir, float wishSpeed, float acceleration, float extraAcceleration = 0f )
	{
		float currentSpeed = Velocity.Dot( wishDir );
		float addSpeed = MathF.Max( extraAcceleration * Time.Delta, wishSpeed - currentSpeed );

		float accelSpeed = MathF.Min( acceleration * Time.Delta, addSpeed );

		Velocity += wishDir * accelSpeed;
	}
}
