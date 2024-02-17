namespace Tf;

public partial class AirMoveMechanic : BasePlayerControllerMechanic
{
	public override bool ShouldBecomeActive() => !Controller.IsGrounded && !Controller.HasTag( "wallrun" );
	public override float? GetSpeed() => PlayerSettings.AirSpeed;

	public override IEnumerable<string> GetTags()
	{
		yield return "airmove";
	}

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

		Vector3 wishDir = Controller.BuildWishDir();
		float wishSpeed = GetSpeed().Value;

		Accelerate( wishDir, wishSpeed, PlayerSettings.AirAcceleration, PlayerSettings.ExtraAirAcceleration );
		ctrl.Move();
		Velocity += halfGravity;
	}

	public void Accelerate( Vector3 wishDir, float wishSpeed, float acceleration, float extraAcceleration = 0f )
	{
		float currentSpeed = HorzVelocity.Dot( wishDir );
		float addSpeed = MathF.Max( extraAcceleration * Time.Delta, wishSpeed - currentSpeed );

		float accelSpeed = MathF.Min( acceleration * Time.Delta, addSpeed );

		Velocity += wishDir * accelSpeed;
	}
}
