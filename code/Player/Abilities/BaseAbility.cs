namespace Gauntlet.Player.Abilities;

/// <summary>
/// A base for a player controller mechanic.
/// </summary>
public abstract class BaseAbility : Component
{
	[Property, Category( "Base" )] protected PlayerController Controller { get; set; }

	/// <summary>
	/// How long since <see cref="IsActive"/> changed.
	/// </summary>
	protected TimeSince TimeSinceActiveChanged { get; set; }

	/// <summary>
	/// How long since <see cref="IsActive"/> was set to true
	/// </summary>
	public TimeSince TimeSinceStart { get; protected set; }

	/// <summary>
	/// How long since <see cref="IsActive"/> was last set to true
	/// </summary>
	public TimeSince TimeSinceLastStart { get; protected set; }

	/// <summary>
	/// How long since <see cref="IsActive"/> was set to false
	/// </summary>
	public TimeSince TimeSinceStop { get; protected set; }

	/// <summary>
	/// An accessor for the controller's position
	/// </summary>
	protected Vector3 Position
	{
		get => Controller.Position;
		set => Controller.Position = value;
	}

	/// <summary>
	/// An accessor for the controller's velocity
	/// </summary>
	protected Vector3 Velocity
	{
		get => Controller.Velocity;
		set => Controller.Velocity = value;
	}

	/// <summary>
	/// An accessor for the controller's horizontal velocity
	/// </summary>
	protected Vector3 HorzVelocity
	{
		get => Controller.HorzVelocity;
	}

	/// <summary>
	/// An accessor for the controller's player settings
	/// </summary>
	protected PlayerSettings PlayerSettings
	{
		get => Controller.PlayerSettings;
	}

	private bool _isActive;

	/// <summary>
	/// Is this mechanic active?
	/// </summary>
	[Property, Category( "Base" ), ReadOnly]
	public bool IsActive
	{
		get => _isActive;
		set
		{
			var before = _isActive;
			_isActive = value;

			if ( _isActive == before )
			{
				return;
			}

			TimeSinceActiveChanged = 0;
			switch ( _isActive )
			{
				case true:
					TimeSinceStart = 0;
					break;
				case false:
					TimeSinceStop = 0;
					break;
			}

			OnActiveChanged( before, _isActive );
			if ( _isActive ) TimeSinceLastStart = 0;
		}
	}

	public TimeUntil Cooldown { get; set; }

	protected override void OnAwake()
	{
		if ( !Controller.IsValid() )
		{
			Controller = Components.Get<PlayerController>( FindMode.EverythingInSelfAndAncestors );
		}
	}

	/// <summary>
	/// Called when <see cref="IsActive"/> changes.
	/// </summary>
	/// <param name="before"></param>
	/// <param name="after"></param>
	protected virtual void OnActiveChanged( bool before, bool after )
	{
		//
	}

	/// <summary>
	/// Called by <see cref="Controller"/>, treat this like a Tick/Update while the mechanic is active.
	/// </summary>
	public virtual void OnActiveUpdate()
	{
		//
	}

	/// <summary>
	/// Mechanics can simulate even if not active. Called before active update.
	/// </summary>
	public virtual void Simulate()
	{
		//
	}

	/// <summary>
	/// Mechanics can simulate every frame even if not active.
	/// </summary>
	public virtual void FrameSimulate()
	{
		//
	}

	/// <summary>
	/// Should we be ticking this mechanic at all?
	/// </summary>
	/// <returns></returns>
	public virtual bool ShouldBecomeActive()
	{
		return false;
	}

	/// <summary>
	/// Abilities can override the player's acceleration.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetAcceleration()
	{
		return null;
	}

	/// <summary>
	/// Abilities can override the player's gravity scale.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetGravityScale()
	{
		return null;
	}

	/// <summary>
	/// Abilities can override the player's speed.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetSpeed()
	{
		return null;
	}
}
