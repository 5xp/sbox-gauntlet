using Gauntlet.Player.Abilities;

namespace Gauntlet.Player;

public partial class PlayerController
{
	private IEnumerable<BaseAbility> Abilities =>
		Components.GetAll<BaseAbility>( FindMode.EnabledInSelfAndDescendants );

	BaseAbility[] ActiveAbilities;

	private void OnUpdateAbilities()
	{
		var lastUpdate = ActiveAbilities;
		var sortedAbilities = Abilities.Where( x => x.ShouldBecomeActive() );
		var activeAbilities = new List<BaseAbility>();

		foreach ( var ability in Abilities )
		{
			ability.Simulate();
		}

		foreach ( var ability in sortedAbilities )
		{
			ability.IsActive = true;
			ability.OnActiveUpdate();
			activeAbilities.Add( ability );
		}

		ActiveAbilities = activeAbilities.ToArray();

		if ( lastUpdate is not null )
		{
			foreach ( var ability in lastUpdate?.Except( ActiveAbilities ) )
			{
				ability.IsActive = false;
			}
		}
	}

	public T GetAbility<T>() where T : BaseAbility
	{
		return Components.Get<T>( FindMode.EverythingInChildren );
	}
}
