using System.Collections.Immutable;

namespace Tf;

public partial class PlayerController
{
	/// <summary>
	/// Maintains a list of mechanics that are associated with this player controller.
	/// </summary>
	public IEnumerable<BasePlayerControllerMechanic> Mechanics => Components.GetAll<BasePlayerControllerMechanic>( FindMode.EnabledInSelfAndDescendants ).OrderBy( x => x.Priority );

	public float? CurrentSpeedOverride;
	public float? CurrentEyeHeightOverride;
	public float? CurrentHullHeightOverride;
	public float? CurrentFrictionOverride;
	public float? CurrentAccelerationOverride;

	BasePlayerControllerMechanic[] ActiveMechanics;

	/// <summary>
	/// Called on <see cref="OnUpdate"/>.
	/// </summary>
	protected void OnUpdateMechanics()
	{
		var lastUpdate = ActiveMechanics;
		var sortedMechanics = Mechanics.Where( x => x.ShouldBecomeActive() ).ToArray();

		// Copy the previous update's tags so we can compare / send tag changed events later.
		var previousUpdateTags = tags;

		// Clear the current tags
		var currentTags = new List<string>();

		float? speedOverride = null;
		float? eyeHeightOverride = null;
		float? hullHeightOverride = null;
		float? frictionOverride = null;
		float? accelerationOverride = null;

		foreach ( var mechanic in Mechanics )
		{
			mechanic.Simulate();
		}

		foreach ( var mechanic in sortedMechanics )
		{
			mechanic.IsActive = true;
			mechanic.OnActiveUpdate();

			// Add tags where we can
			currentTags.AddRange( mechanic.GetTags() );

			var eyeHeight = mechanic.GetEyeHeight();
			var hullHeight = mechanic.GetHullHeight();
			var speed = mechanic.GetSpeed();
			var acceleration = mechanic.GetAcceleration();

			mechanic.BuildWishInput( ref WishMove );

			if ( speed is not null ) speedOverride = speed;
			if ( eyeHeight is not null ) eyeHeightOverride = eyeHeight;
			if ( hullHeight is not null ) hullHeightOverride = hullHeight;
			if ( acceleration is not null ) accelerationOverride = acceleration;
		}

		ActiveMechanics = sortedMechanics;

		if ( lastUpdate is not null )
		{
			foreach ( var mechanic in lastUpdate?.Except( sortedMechanics ) )
			{
				// This mechanic shouldn't be active anymore
				mechanic.IsActive = false;
			}
		}

		CurrentSpeedOverride = speedOverride;
		CurrentEyeHeightOverride = eyeHeightOverride;
		CurrentHullHeightOverride = hullHeightOverride;
		CurrentFrictionOverride = frictionOverride;
		CurrentAccelerationOverride = accelerationOverride;

		tags = currentTags.ToImmutableArray();
	}

	public T GetMechanic<T>() where T : BasePlayerControllerMechanic
	{
		foreach ( var mechanic in Mechanics )
		{
			if ( mechanic is T val ) return val;
		}

		return null;
	}

	public bool IsMechanicActive<T>() where T : BasePlayerControllerMechanic
	{
		return GetMechanic<T>()?.IsActive ?? false;
	}
}
