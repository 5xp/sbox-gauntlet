namespace Tf;

public sealed class PlayerSettings
{
	public enum PlayerTypes
	{
		Regular,
		Faster
	};

	public PlayerSettings() { }

	public static PlayerSettings Regular { get { return new PlayerSettings(); } }
	public static PlayerSettings Faster
	{
		get
		{
			return new PlayerSettings
			{
				GravityScale = 0.8f,
				WalkSpeed = 173.5f,
				SprintSpeed = 260f,
			};
		}
	}

	public static PlayerSettings GetPlayerSettings( PlayerTypes playerType )
	{
		return playerType switch
		{
			PlayerTypes.Faster => Faster,
			_ => Regular,
		};
	}

	public float HullRadius { get; set; } = 16f;
	public float HullHeightStanding { get; set; } = 72f;
	public float HullHeightCrouching { get; set; } = 47f;
	public float ViewHeightStanding { get; set; } = 60f;
	public float ViewHeightCrouching { get; set; } = 38f;
	public float Gravity { get; set; } = 750f;
	public float GravityScale { get; set; } = 0.75f;
	public float StepHeightMax { get; set; } = 18f;
	public float StepHeightMin { get; set; } = 2f;
	public float JumpHeight { get; set; } = 60f;
	public float GroundAngle { get; set; } = 46f;
	public float MaxNonJumpVelocity { get; set; } = 140f;
	public float Acceleration { get; set; } = 2500f;
	public float WalkSpeed { get; set; } = 162.5f;
	public float CrouchSpeed { get; set; } = 80f;
	public float SprintSpeed { get; set; } = 243f;
	public float AirSpeed { get; set; } = 60f;
	public float AirAcceleration { get; set; } = 500f;
	public float ExtraAirAcceleration { get; set; } = 2f;
	public float PitchMaxUp { get; set; } = 85f;
	public float PitchMaxDown { get; set; } = 89f;
	public float DuckSpeed { get; set; } = 2f;
	public float UnduckSpeed { get; set; } = 5f;
}
