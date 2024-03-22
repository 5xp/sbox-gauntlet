using Sandbox.Network;

namespace Gauntlet;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject SpawnPoint { get; set; }

	/// <summary>
	/// Is this game multiplayer?
	/// </summary>
	[Property] public bool IsMultiplayer { get; set; } = false;

	protected override void OnStart()
	{
		LeaderboardManager.Instance.StopPolling();
		LeaderboardManager.Instance.ResetSubscriptions();

		if ( Common.TryGetLeaderboardIdent( Scene.Title, 1, out string stat1 ) )
		{
			_ = LeaderboardManager.Instance.FetchLeaderboardEntries( stat1, true );
		}

		if ( Common.TryGetLeaderboardIdent( Scene.Title, 2, out string stat2 ) )
		{
			_ = LeaderboardManager.Instance.FetchLeaderboardEntries( stat2, true );
		}

		PlayerPreferences.Load();

		if ( !IsMultiplayer )
		{
			SpawnPlayer();
			return;
		}

		//
		// Create a lobby if we're not connected
		//
		if ( !GameNetworkSystem.IsActive )
		{
			GameNetworkSystem.CreateLobby();
		}
	}

	public void OnActive( Connection channel )
	{
		if ( !IsMultiplayer ) return;

		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );

		var player = SpawnPlayer();

		var cl = player.Components.Create<Client>();
		cl.Setup( channel );

		player.NetworkSpawn( channel );
	}

	private GameObject SpawnPlayer()
	{
		Transform spawnTransform;

		if ( SpawnPoint is null )
		{
			var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

			if ( spawnPoints.Length == 0 )
			{
				Log.Error( "No spawn points found" );
				return null;
			}

			spawnTransform = Game.Random.FromArray( spawnPoints ).GameObject.Transform.World;
		}
		else
		{
			spawnTransform = SpawnPoint.Transform.World;
		}

		Angles angles = spawnTransform.Rotation.Angles();
		spawnTransform = spawnTransform.WithRotation( Rotation.Identity );
		var player = PlayerPrefab.Clone( spawnTransform, name: Connection.Local.DisplayName );
		player.BreakFromPrefab();
		player.Components.GetInChildrenOrSelf<PlayerController>().EyeAngles = angles;
		return player;
	}
}
