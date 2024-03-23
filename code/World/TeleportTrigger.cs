namespace Gauntlet;

public sealed class TeleportTrigger : Component, Component.ITriggerListener
{
	[Property] public GameObject Destination { get; set; }

	public void OnTriggerEnter( Collider other )
	{
		bool isPlayer = other.Components.TryGet<PlayerController>( out var player );
		if ( !isPlayer ) return;

		Teleport( player );
	}

	private void Teleport( PlayerController player )
	{
		if ( Destination is null ) return;
		Vector3 relativePosition = player.Transform.World.Position - Transform.World.Position;
		player.Position = Destination.Transform.World.Position + relativePosition;
		player.Transform.ClearLerp();
	}

	public void OnTriggerExit( Collider other )
	{
	}
}