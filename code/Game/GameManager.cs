using Sandbox.Network;

namespace Gauntlet;

public sealed class GameManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// The spawn point to use for the player. If this is null, we try to get a random spawn point from the scene.
	/// </summary>
	[Property] public GameObject SpawnPoint { get; set; }

	/// <summary>
	/// Is this game multiplayer?
	/// </summary>
	[Property] public bool IsMultiplayer { get; set; } = false;

	public bool ShouldRespawn { get; set; }

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

	protected override void OnFixedUpdate()
	{
		if ( ShouldRespawn )
		{
			RespawnPlayer();
			ShouldRespawn = false;
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

		GameObject spawnPoint = GetSpawnPoint();

		if ( spawnPoint is null )
		{
			Log.Error( "No spawn points found" );
			return null;
		}

		spawnTransform = spawnPoint.Transform.World;
		var player = PlayerPrefab.Clone();
		player.BreakFromPrefab();
		SetPlayerTransform( player, spawnTransform );
		player.Name = Connection.Local.DisplayName;
		return player;
	}

	public void RespawnPlayer( GameObject player )
	{
		GameObject spawnPoint = GetSpawnPoint();

		if ( spawnPoint is null )
		{
			Log.Error( "No spawn points found" );
			return;
		}

		SetPlayerTransform( player, spawnPoint.Transform.World );
	}

	public void RespawnPlayer()
	{
		GameObject player = Scene.Directory.FindByName( Connection.Local.DisplayName ).First();

		if ( player is null )
		{
			Log.Error( "Player not found" );
			return;
		}

		RespawnPlayer( player );
	}

	private void SetPlayerTransform( GameObject player, Transform transform )
	{
		Angles angles = transform.Rotation.Angles();
		player.Transform.World = transform;

		PlayerController controller = player.Components.GetInChildrenOrSelf<PlayerController>();
		controller.EyeAngles = angles;
		controller.Velocity = 0;
	}

	private GameObject GetSpawnPoint()
	{
		if ( SpawnPoint is not null )
		{
			return SpawnPoint;
		}

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		if ( spawnPoints.Length == 0 )
		{
			return null;
		}

		return Game.Random.FromArray( spawnPoints ).GameObject;
	}
}
