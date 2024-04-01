namespace Gauntlet.Player.Abilities;

public abstract class BaseAbility : Component
{
	[Property, Category( "Base" )] protected PlayerController Controller { get; set; }

	protected TimeSince TimeSinceActiveChanged { get; set; }

	public TimeSince TimeSinceStart { get; protected set; }

	public TimeSince TimeSinceLastStart { get; protected set; }

	public TimeSince TimeSinceStop { get; protected set; }

	protected Vector3 Position
	{
		get => Controller.Position;
		set => Controller.Position = value;
	}

	protected Vector3 Velocity
	{
		get => Controller.Velocity;
		set => Controller.Velocity = value;
	}

	protected Vector3 HorzVelocity
	{
		get => Controller.HorzVelocity;
	}

	protected PlayerSettings PlayerSettings
	{
		get => Controller.PlayerSettings;
	}

	private bool _isActive;

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

	protected virtual void OnActiveChanged( bool before, bool after )
	{
		//
	}

	public virtual void OnActiveUpdate()
	{
		//
	}

	public virtual void Simulate()
	{
		//
	}

	public virtual void FrameSimulate()
	{
		//
	}

	public virtual bool ShouldBecomeActive()
	{
		return false;
	}
}
