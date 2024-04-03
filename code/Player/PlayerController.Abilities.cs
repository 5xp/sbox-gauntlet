using Gauntlet.Player.Abilities;

namespace Gauntlet.Player;

public partial class PlayerController
{
	private IEnumerable<BaseAbility> Abilities =>
		Components.GetAll<BaseAbility>( FindMode.EnabledInSelfAndDescendants );

	BaseAbility[] ActiveAbilities;

	public float? AbilitiesAccelerationOverride;
	public float? AbilitiesGravityScaleOverride = 1f;
	public float? AbilitiesSpeedOverride;

	private void OnUpdateAbilities()
	{
		var lastUpdate = ActiveAbilities;
		var sortedAbilities = Abilities.Where( x => x.ShouldBecomeActive() );
		var activeAbilities = new List<BaseAbility>();

		float? accelerationOverride = null;
		float? gravityScaleOverride = null;
		float? speedOverride = null;

		foreach ( var ability in Abilities )
		{
			ability.Simulate();
		}

		foreach ( var ability in sortedAbilities )
		{
			ability.IsActive = true;
			ability.OnActiveUpdate();
			activeAbilities.Add( ability );

			var acceleration = ability.GetAcceleration();
			var gravityScale = ability.GetGravityScale();
			var speed = ability.GetSpeed();

			if ( acceleration is not null ) accelerationOverride = acceleration;
			if ( gravityScale is not null ) gravityScaleOverride = gravityScale;
			if ( speed is not null ) speedOverride = speed;
		}

		ActiveAbilities = activeAbilities.ToArray();

		if ( lastUpdate is not null )
		{
			foreach ( var ability in lastUpdate?.Except( ActiveAbilities ) )
			{
				ability.IsActive = false;
			}
		}

		AbilitiesAccelerationOverride = accelerationOverride;
		AbilitiesGravityScaleOverride = gravityScaleOverride;
		AbilitiesSpeedOverride = speedOverride;
	}

	public T GetAbility<T>() where T : BaseAbility
	{
		return Components.Get<T>( FindMode.EverythingInChildren );
	}
}
