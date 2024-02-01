namespace Tf;

/// <summary>
/// A jumping mechanic.
/// </summary>
public partial class JumpMechanic : BasePlayerControllerMechanic
{
	public override bool ShouldBecomeActive()
	{
		if ( !Input.Pressed( "Jump" ) ) return false;
		bool shouldJump = Controller.IsGrounded;

		if ( !shouldJump ) return false;
		return true;
	}
	public override IEnumerable<string> GetTags()
	{
		yield return "jump";
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		Controller.ClearGroundObject();

		float startZ = Velocity.z;

		float upSpeed = MathF.Sqrt( 2f * PlayerSettings.JumpHeight * PlayerSettings.Gravity );
		Velocity = Velocity.WithZ( startZ + upSpeed );

		Controller.BroadcastPlayerJumped();
	}
}
