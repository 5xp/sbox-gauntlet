namespace Tf;

public partial class CrouchMechanic : BasePlayerControllerMechanic
{
	public bool ForceDuck { get; set; } = false;

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

		if ( Controller.IsGrounded ) return false;

		return true;
	}

	public override IEnumerable<string> GetTags()
	{
		yield return "crouch";
	}

	public override float? GetEyeHeight()
	{
		return PlayerSettings.ViewHeightCrouching;
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
