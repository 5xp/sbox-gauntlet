using System.Collections.Immutable;
using Gauntlet.Player.Mechanics;

namespace Gauntlet.Player;

public partial class PlayerController
{
	/// <summary>
	/// Maintains a list of mechanics that are associated with this player controller.
	/// </summary>
	private IEnumerable<BasePlayerControllerMechanic> Mechanics => Components.GetAll<BasePlayerControllerMechanic>( FindMode.EnabledInSelfAndDescendants ).OrderBy( x => x.Priority );

	private float? CurrentSpeedOverride;
	public float? CurrentAccelerationOverride;

	BasePlayerControllerMechanic[] ActiveMechanics;

	/// <summary>
	/// Called on <see cref="OnUpdate"/>.
	/// </summary>
	private void OnUpdateMechanics()
	{
		var lastUpdate = ActiveMechanics;
		var sortedMechanics = Mechanics.Where( x => x.ShouldBecomeActive() );
		var activeMechanics = new List<BasePlayerControllerMechanic>();

		// Copy the previous update's tags so we can compare / send tag changed events later.
		var previousUpdateTags = _tags;

		// Clear the current tags
		var currentTags = new List<string>();

		float? speedOverride = null;
		float? accelerationOverride = null;

		float previousZVel = Velocity.z;

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
			activeMechanics.Add( mechanic );

			var eyeHeight = mechanic.GetEyeHeight();
			var hullHeight = mechanic.GetHullHeight();
			var speed = mechanic.GetSpeed();
			var acceleration = mechanic.GetAcceleration();

			mechanic.BuildWishInput( ref WishMove );

			if ( speed is not null ) speedOverride = speed;
			if ( acceleration is not null ) accelerationOverride = acceleration;
		}

		ActiveMechanics = activeMechanics.ToArray();

		if ( lastUpdate is not null )
		{
			foreach ( var mechanic in lastUpdate?.Except( ActiveMechanics ) )
			{
				// This mechanic shouldn't be active anymore
				mechanic.IsActive = false;
			}
		}

		CurrentSpeedOverride = speedOverride;
		CurrentAccelerationOverride = accelerationOverride;

		_tags = currentTags.ToImmutableArray();

		if ( RecordWallJumpThisTick )
		{
			OnFastWallJump?.Invoke( AfterWallJumpSpeed - PreWallTouchSpeed, WallJumpTimeDiff );
		}

		RecordWallJumpThisTick = false;
	}

	public T GetMechanic<T>() where T : BasePlayerControllerMechanic
	{
		return Components.Get<T>( FindMode.EnabledInSelfAndChildren );
	}
}
