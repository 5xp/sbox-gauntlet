using Gauntlet.Player;
using Gauntlet.Utils;
using Sandbox.Network;

namespace Gauntlet;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }

	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// The spawn point to use for the player. If this is null, we try to get a random spawn point from the scene.
	/// </summary>
	[Property]
	public GameObject SpawnPoint { get; set; }

	/// <summary>
	/// Is this game multiplayer?
	/// </summary>
	[Property]
	private bool IsMultiplayer { get; } = false;

	private bool ShouldRespawn { get; set; }

	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );

		GameObject player = SpawnPlayer();

		var cl = player.Components.Create<Client>();
		cl.Setup( channel );

		player.NetworkSpawn( channel );
	}

	protected override void OnStart()
	{
		Instance = this;

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
			OnActive( Connection.Local );
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
		if ( !ShouldRespawn && !Input.Pressed( "Restart" ) )
		{
			return;
		}

		RespawnPlayer();
		ShouldRespawn = false;
	}

	public void Respawn()
	{
		ShouldRespawn = true;
	}

	private GameObject SpawnPlayer()
	{
		GameObject spawnPoint = GetSpawnPoint();

		if ( spawnPoint is null )
		{
			Log.Error( "No spawn points found" );
			return null;
		}

		Transform spawnTransform = spawnPoint.Transform.World;
		GameObject player = PlayerPrefab.Clone();
		player.BreakFromPrefab();
		SetPlayerTransform( player, spawnTransform );
		player.Name = Connection.Local.DisplayName;
		return player;
	}

	private void RespawnPlayer( GameObject player )
	{
		GameObject spawnPoint = GetSpawnPoint();

		if ( spawnPoint is null )
		{
			Log.Error( "No spawn points found" );
			return;
		}

		SetPlayerTransform( player, spawnPoint.Transform.World );
		player.Transform.ClearLerp();
	}

	private void RespawnPlayer()
	{
		GameObject player = Client.Local.GameObject;

		if ( player is null )
		{
			Log.Error( "Player not found" );
			return;
		}

		RespawnPlayer( player );
	}

	private static void SetPlayerTransform( GameObject player, Transform transform )
	{
		Angles angles = transform.Rotation.Angles();
		player.Transform.World = transform;

		var controller = player.Components.GetInChildrenOrSelf<PlayerController>();
		controller.InputAngles = angles;
		controller.Velocity = 0;
	}

	private GameObject GetSpawnPoint()
	{
		if ( SpawnPoint is not null )
		{
			return SpawnPoint;
		}

		SpawnPoint[] spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		return spawnPoints.Length == 0 ? null : Game.Random.FromArray( spawnPoints ).GameObject;
	}
}
