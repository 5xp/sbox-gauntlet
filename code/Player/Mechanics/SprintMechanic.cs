namespace Tf;

/// <summary>
/// A sprinting mechanic.
/// </summary>
public partial class SprintMechanic : BasePlayerControllerMechanic
{
	private bool SprintToggled { get; set; } = false;
	private bool SprintCanToggleOff { get; set; } = false;
	private float SprintTiltFraction { get; set; }
	private float SprintTiltVelocity { get; set; }
	private float SprintViewOffsetFraction { get; set; }
	private float SprintBobFrequency => 2f * MathF.PI / PlayerSettings.SprintBobCycleTime;
	private float SprintBobFraction { get; set; }

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

		if ( !Input.Down( "Run" ) && !SprintToggled && !PlayerPreferences.Instance.AutoSprintEnabled ) return false;

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
		ApplySprintBob();
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
			angleDelta = PlayerController.GetInputDelta().yaw;
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
	/// Apply camera bob when sprinting
	/// </summary>
	private void ApplySprintBob()
	{
		float approachSpeed = IsActive ? 4f : 10f;

		SprintBobFraction = SprintBobFraction.Approach( IsActive ? 1f : 0f, approachSpeed * Time.Delta );

		float smoothingFrac = MathX.LerpInverse( TimeSinceStart, 0f, PlayerSettings.SprintBobSmoothingTime );
		float reductionFactor = MathX.LerpTo( 1f, PlayerSettings.SprintBobSmoothingReductionFactor, smoothingFrac );
		float sprintFrac = MathX.LerpInverse( Velocity.Length, 0f, PlayerSettings.SprintSpeed );

		Vector3 bob = new Vector3( GetPitchBob(), GetYawBob(), GetRollBob() ) * reductionFactor * sprintFrac * SprintBobFraction;
		bob *= PlayerPreferences.Instance.SprintBobScale;

		Controller.CurrentCameraRotationOffset *= Rotation.From( bob.x, bob.y, bob.z );
	}

	private float GetYawBob()
	{
		return 0.7f * MathF.Sin( TimeSinceStart * SprintBobFrequency + 0.82f ) - 0.159f;
	}

	private float GetPitchBob()
	{
		float t = TimeSinceStart;

		float c1 = -0.143f * MathF.Sin( t * SprintBobFrequency + 0.762f );
		float c2 = 0.3f * MathF.Sin( t * 2f * SprintBobFrequency - 1.532f );
		float c3 = -0.083f * MathF.Sin( t * 4f * SprintBobFrequency + 1.4f );

		return c1 + c2 + c3 + 0.044f;
	}

	private float GetRollBob()
	{
		float t = TimeSinceStart;

		float c1 = 0.55f * MathF.Sin( t * SprintBobFrequency - 1.26f );
		float c2 = -0.05f * MathF.Sin( t * 2f * SprintBobFrequency + 1.06f );
		float c3 = -0.08f * MathF.Sin( t * 3f * SprintBobFrequency + 4.38f );
		return c1 + c2 + c3 + 0.49f;
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
