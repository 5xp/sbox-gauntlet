namespace Tf;

public partial class CrouchMechanic : BasePlayerControllerMechanic
{
	public bool ForceDuck { get; set; } = false;

	public override int Priority => 10;

	public override bool ShouldBecomeActive()
	{
		if ( ForceDuck ) return true;

		if ( ShouldStayDucked() ) return true;

		if ( !Input.Down( "Duck" ) ) return false;

		return true;
	}

	private bool ShouldStayDucked()
	{
		if ( !HasTag( "slide" ) ) return false;

		float forceSpeed = PlayerSettings.SlideForceSlideSpeed;

		if ( HorzVelocity.LengthSquared <= forceSpeed * forceSpeed ) return false;

		return true;
	}

	public override void Simulate()
	{
		ForceDuck = !Controller.CanUnduck();
	}

	public override IEnumerable<string> GetTags()
	{
		yield return "crouch";
	}

	public override float? GetSpeed()
	{
		return PlayerSettings.CrouchSpeed;
	}

	public override float? GetHullHeight()
	{
		return PlayerSettings.HullHeightCrouching;
	}
}
