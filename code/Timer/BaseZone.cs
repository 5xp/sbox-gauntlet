namespace Gauntlet;

public abstract partial class BaseZone : Component, Component.ITriggerListener
{
	public abstract void OnTriggerEnter( Collider other );
	public abstract void OnTriggerExit( Collider other );
}