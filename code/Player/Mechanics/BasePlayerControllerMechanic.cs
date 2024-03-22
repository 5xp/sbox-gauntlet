namespace Gauntlet;

/// <summary>
/// A base for a player controller mechanic.
/// </summary>
public abstract partial class BasePlayerControllerMechanic : Component
{
	[Property, Category( "Base" )] public PlayerController Controller { get; set; }

	/// <summary>
	/// A priority for the controller mechanic.
	/// </summary>
	[Property, Category( "Base" )] public virtual int Priority { get; set; } = 0;

	/// <summary>
	/// How long since <see cref="IsActive"/> changed.
	/// </summary>
	[Property, Category( "Base" ), ReadOnly] public TimeSince TimeSinceActiveChanged { get; protected set; }

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


	public Vector3 Position
	{
		get => Controller.Position;
		set => Controller.Position = value;
	}

	public Vector3 Velocity
	{
		get => Controller.Velocity;
		set => Controller.Velocity = value;
	}

	public Vector3 HorzVelocity
	{
		get => Controller.HorzVelocity;
	}

	public PlayerSettings PlayerSettings
	{
		get => Controller.PlayerSettings;
	}

	private bool isActive;

	/// <summary>
	/// Is this mechanic active?
	/// </summary>
	[Property, Category( "Base" ), ReadOnly]
	public bool IsActive
	{
		get => isActive;
		set
		{
			var before = isActive;
			isActive = value;

			if ( isActive != before )
			{
				TimeSinceActiveChanged = 0;
				if ( isActive ) TimeSinceStart = 0;
				if ( !isActive ) TimeSinceStop = 0;
				OnActiveChanged( before, isActive );
				if ( isActive ) TimeSinceLastStart = 0;
			}
		}
	}

	protected override void OnAwake()
	{
		// If we don't have the player controller defined, let's have a look for it
		if ( !Controller.IsValid() )
		{
			Controller = Components.Get<PlayerController>( FindMode.EverythingInSelfAndAncestors );
		}
	}

	/// <summary>
	/// Return a list of tags to be used by the player controller / other mechanics.
	/// </summary>
	/// <returns></returns>
	public virtual IEnumerable<string> GetTags()
	{
		return Enumerable.Empty<string>();
	}

	/// <summary>
	/// An accessor to see if the player controller has a tag.
	/// </summary>
	/// <param name="tag"></param>
	/// <returns></returns>
	public bool HasTag( string tag ) => Controller.HasTag( tag );

	/// <summary>
	/// An accessor to see if the player controller has all matched tags.
	/// </summary>
	/// <param name="tags"></param>
	/// <returns></returns>
	public bool HasAllTags( params string[] tags ) => Controller.HasAllTags( tags );

	/// <summary>
	/// An accessor to see if the player controller has any tag.
	/// </summary>
	/// <param name="tags"></param>
	/// <returns></returns>
	public bool HasAnyTag( params string[] tags ) => Controller.HasAnyTag( tags );

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
	/// Should we be inactive?
	/// </summary>
	/// <returns></returns>
	public virtual bool ShouldBecomeInactive()
	{
		return !ShouldBecomeActive();
	}

	/// <summary>
	/// Mechanics can override the player's movement speed.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetSpeed()
	{
		return null;
	}

	/// <summary>
	/// Mechanics can override the player's eye height.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetEyeHeight()
	{
		return null;
	}

	/// <summary>
	/// Mechanics can override the player's hull height.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetHullHeight()
	{
		return null;
	}

	/// <summary>
	/// Mechanics can override the player's ground friction.
	/// </summary>
	public virtual float? GetGroundFriction()
	{
		return null;
	}

	/// <summary>
	/// Mechanics can override the player's acceleration.
	/// </summary>
	/// <returns></returns>
	public virtual float? GetAcceleration()
	{
		return null;
	}

	/// <summary>
	/// Mechanics can override the player's wish input direction.
	/// </summary>
	public virtual void BuildWishInput( ref Vector3 wish )
	{
	}
}
