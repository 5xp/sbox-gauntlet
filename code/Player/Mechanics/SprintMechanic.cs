namespace Tf;

/// <summary>
/// A sprinting mechanic.
/// </summary>
public partial class SprintMechanic : BasePlayerControllerMechanic
{
	public bool SprintToggled { get; set; } = false;
	public bool SprintCanToggleOff { get; set; } = false;
	public float SprintTiltFraction { get; set; }
	public float SprintTiltVelocity { get; set; }
	public float SprintViewOffsetFraction { get; set; }

	public override int Priority => 70;

	public override IEnumerable<string> GetTags()
	{
		yield return "sprint";
	}

	public override float? GetSpeed()
	{
		return PlayerSettings.SprintSpeed;
	}

	public override bool ShouldBecomeActive()
	{
		if ( HasTag( "crouch" ) ) return false;

		if ( !Input.Down( "Run" ) && !SprintToggled ) return false;

		if ( !Controller.IsGrounded ) return false;

		Vector3 wish = Controller.WishMove;

		if ( wish.Dot( Vector3.Forward ) < PlayerSettings.SprintMaxAngleDot ) return false;

		return true;
	}

	public override void Simulate()
	{
		CheckSprintToggled();
		UpdateViewOffset();
	}

	public override void FrameSimulate()
	{
		ApplySprintTilt();
		ApplyViewOffset();
	}

	private void UpdateViewOffset()
	{
		if ( IsActive )
		{
			bool startedFast = Controller.TimeSinceLastLanding - TimeSinceStart <= Time.Delta;

			float duration = startedFast ? PlayerSettings.SprintOffsetStartFastDuration : PlayerSettings.SprintOffsetStartDuration;

			if ( !startedFast && TimeSinceStart <= PlayerSettings.SprintOffsetStartDelay )
			{
				return;
			}

			SprintViewOffsetFraction = SprintViewOffsetFraction.Approach( 1f, 1f / duration * Time.Delta );
		}
		else
		{
			SprintViewOffsetFraction = SprintViewOffsetFraction.Approach( 0f, 1f / PlayerSettings.SprintOffsetEndDuration * Time.Delta );
		}
	}

	private void ApplyViewOffset()
	{
		float offsetTime = MathF.Sin( SprintViewOffsetFraction * MathF.PI * 0.5f );
		float offsetAmount = 0f.LerpTo( PlayerSettings.SprintViewOffset, offsetTime );
		Controller.CurrentCameraOffset += Vector3.Up * offsetAmount;
	}

	/// <summary>
	/// Apply camera tilt when looking to the side
	/// </summary>
	private void ApplySprintTilt()
	{
		float angleDelta = 0f;

		if ( IsActive )
		{
			angleDelta = PlayerController.GetInputDelta().x;
		}

		float turnRate = angleDelta / Time.Delta;
		float turnRateFrac = turnRate / PlayerSettings.SprintTiltTurnRange;
		turnRateFrac = turnRateFrac.Clamp( -1f, 1f );

		float deltaFrac = turnRateFrac - SprintTiltFraction;

		SprintTiltFraction = SprintTiltFraction.Approach( turnRateFrac, MathF.Abs( SprintTiltVelocity * Time.Delta ) );
		SprintTiltVelocity += Math.Sign( deltaFrac ) * PlayerSettings.SprintTiltAccel * Time.Delta;
		SprintTiltVelocity = SprintTiltVelocity.Clamp( -PlayerSettings.SprintTiltMaxVel, PlayerSettings.SprintTiltMaxVel );

		Controller.EyeAngles += new Angles( 0f, 0f, SprintTiltFraction * PlayerSettings.SprintTiltMaxRoll );
	}

	/// <summary>
	/// If we press sprint, keep sprint on until we try moving backwards or crouch
	/// </summary>
	private void CheckSprintToggled()
	{
		bool movingForward = Vector3.Dot( Controller.WishMove, Vector3.Forward ) >= 0f && !Controller.WishMove.AlmostEqual( 0f );

		if ( SprintToggled && movingForward )
		{
			SprintCanToggleOff = true;
		}

		if ( Input.Pressed( "Duck" ) || SprintCanToggleOff && !movingForward )
		{
			SprintToggled = false;
		}

		if ( Input.Pressed( "Run" ) )
		{
			SprintCanToggleOff = false;
			SprintToggled = true;
		}
	}
}
