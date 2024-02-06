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

		shouldJump |= RecentlyLeftGround();

		if ( !shouldJump ) return false;
		return true;
	}
	public override IEnumerable<string> GetTags()
	{
		yield return "jump";
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		if ( !after ) return;

		if ( Controller.IsGrounded || RecentlyLeftGround() )
		{
			DoGroundJump();
		}
	}

	/// <summary>
	/// Returns true if we recently walked off a ledge within the allowed coyote time.
	/// Will not return true if we just jumped.
	/// </summary>
	/// <returns></returns>
	private bool RecentlyLeftGround()
	{
		return Controller.TimeSinceLastOnGround <= PlayerSettings.JumpGracePeriod && TimeSinceStart > Controller.TimeSinceLastOnGround;
	}

	private void DoGroundJump()
	{
		Controller.ClearGroundObject();

		float startZ = Velocity.z;

		float jumpHeight = HasTag( "slide" ) ? PlayerSettings.SlideJumpHeight : PlayerSettings.JumpHeight;

		bool isFullyDucked = Controller.DuckFraction.AlmostEqual( 1f );

		if ( !isFullyDucked && HasTag( "crouch" ) )
		{
			Controller.DuckFraction = 0f;

			// If we try jumping before we're fully crouching, refund our speed boost
			SlideMechanic slide = Controller.GetMechanic<SlideMechanic>();

			if ( slide.IsActive && slide.UsedBoost )
			{
				Velocity = Velocity.Normal * slide.StartSpeed;

				Controller.DuckFraction = 1f;
			}
		}

		float upSpeed = MathF.Sqrt( 2f * jumpHeight * PlayerSettings.Gravity );
		Velocity = Velocity.WithZ( startZ + upSpeed );

		Controller.BroadcastPlayerJumped();
	}
}
