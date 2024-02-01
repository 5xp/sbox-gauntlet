using Sandbox.Network;

namespace Tf;

public sealed class GameNetworkManager : Component, Component.INetworkListener
{
	[Property] public GameObject PlayerPrefab { get; set; }
	[Property] public GameObject SpawnPoint { get; set; }

	/// <summary>
	/// Is this game multiplayer?
	/// </summary>
	[Property] public bool IsMultiplayer { get; set; } = true;

	protected override void OnStart()
	{
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
		var player = PlayerPrefab.Clone(SpawnPoint.Transform.World);
		player.BreakFromPrefab();
		return player;
	}
}
