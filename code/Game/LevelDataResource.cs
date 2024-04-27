namespace Gauntlet;

[GameResource( "Level Data", "lvl", "A resource containing information about a level.", IconBgColor = "#5877E0",
	Icon = "filter_hdr" )]
public class LevelDataResource : GameResource
{
	public static HashSet<LevelDataResource> All { get; set; } = new();

	public string Name { get; set; } = "Level";

	/// <summary>
	/// The identifier of the level for the leaderboard.
	/// </summary>
	public string StatName { get; set; } = "1";

	/// <summary>
	/// The version of <see cref="StatName"/> to use.
	/// </summary>
	public string StatVersion { get; set; } = "0.1";

	public string GetStatId( int loop )
	{
		loop = Math.Clamp( loop, 1, 2 );
		return $"v{StatVersion}-{StatName}.{loop}.time";
	}

	protected override void PostLoad()
	{
		if ( !All.Add( this ) )
		{
			Log.Warning( "Tried to add a level data resource that already exists" );
		}
	}
}
