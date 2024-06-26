using System.Text.Json.Serialization;

namespace Gauntlet;

public sealed class PlayerPreferences
{
	private static PlayerPreferences _instance;

	public static PlayerPreferences Instance
	{
		get
		{
			_instance ??= new PlayerPreferences();
			return _instance;
		}
	}

	[JsonIgnore] private const string FileName = "GauntletPreferences.json";

	public static event Action OnPreferencesSavedOrLoaded;

	public float HudScale { get; set; } = 1f;
	public bool HudEnabled { get; set; } = true;
	public bool HudSwayEnabled { get; set; } = true;
	public bool KeyPressHudEnabled { get; set; } = true;
	public bool KeyPressShowJumpAndCrouch { get; set; } = false;
	public bool KeyPressHudCompactEnabled { get; set; } = false;
	public float KeyPressHudCompactPositionX { get; set; } = 0.5f;
	public float KeyPressHudCompactPositionY { get; set; } = 0.5f;
	public bool CrosshairHudEnabled { get; set; } = true;
	public bool SpeedometerHudEnabled { get; set; } = true;
	public bool SpeedometerHudCompactEnabled { get; set; } = false;
	public float SpeedometerHudCompactPositionX { get; set; } = 0.54f;
	public float SpeedometerHudCompactPositionY { get; set; } = 0.5f;
	public bool WallKickHudEnabled { get; set; } = true;
	public float SprintBobScale { get; set; } = 1f;
	public bool AutoSprintEnabled { get; set; } = false;

	public static void Save()
	{
		FileSystem.Data.WriteJson( FileName, Instance );
		OnPreferencesSavedOrLoaded?.Invoke();
	}

	public static void Load()
	{
		var preferences = FileSystem.Data.ReadJsonOrDefault<PlayerPreferences>( FileName );

		preferences ??= new PlayerPreferences();

		_instance = preferences;
		OnPreferencesSavedOrLoaded?.Invoke();
	}
}
