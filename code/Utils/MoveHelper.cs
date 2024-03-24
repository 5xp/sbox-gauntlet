namespace Gauntlet.Utils;

public struct MoveHelper
{
	//
	// Inputs and Outputs
	//
	public Vector3 Position;
	public Vector3 Velocity;
	public bool HitWall;
	public Vector3? HitNormal;
	public Vector3? WallPosition;

	//
	// Config
	//
	public float GroundBounce;
	public float WallBounce;
	public float MaxStandableAngle;
	public SceneTrace Trace;

	/// <summary>
	/// Create the movehelper and initialize it with the default settings.
	/// You can change Trace and MaxStandableAngle after creation.
	/// </summary>
	/// <example>
	/// var move = new MoveHelper( Position, Velocity )
	/// </example>
	public MoveHelper( Vector3 position, Vector3 velocity, params string[] solidTags ) : this()
	{
		Velocity = velocity;
		Position = position;
		GroundBounce = 0.0f;
		WallBounce = 0.0f;
		MaxStandableAngle = 10.0f;

		Trace = Trace.Ray( 0, 0 ).WithAnyTags( solidTags );
	}

	/// <summary>
	/// Create the movehelper and initialize it with the default settings.
	/// You can change Trace and MaxStandableAngle after creation.
	/// </summary>
	/// <example>
	/// var move = new MoveHelper( Position, Velocity )
	/// </example>
	public MoveHelper( Vector3 position, Vector3 velocity ) : this( position, velocity, "solid", "playerclip", "passbullets", "player" )
	{

	}

	/// <summary>
	/// Trace this from one position to another
	/// </summary>
	public readonly SceneTraceResult TraceFromTo( Vector3 start, Vector3 end )
	{
		return Trace.FromTo( start, end ).Run();
	}

	/// <summary>
	/// Trace this from its current Position to a delta
	/// </summary>
	public SceneTraceResult TraceDirection( Vector3 down )
	{
		return TraceFromTo( Position, Position + down );
	}


	/// <summary>
	/// Try to move to the position. Will return the fraction of the desired velocity that we traveled.
	/// Position and Velocity will be what we recommend using.
	/// </summary>
	public float TryMove( float timestep, Vector3 up )
	{
		var timeLeft = timestep;
		float travelFraction = 0;
		HitWall = false;

		using var moveplanes = new VelocityClipPlanes( Velocity );

		for ( int bump = 0; bump < moveplanes.Max; bump++ )
		{
			if ( Velocity.Length.AlmostEqual( 0.0f ) )
				break;

			var pm = TraceFromTo( Position, Position + Velocity * timeLeft );

			travelFraction += pm.Fraction;

			if ( pm.Hit )
			{
				// There's a bug with sweeping where sometimes the end position is starting in solid, so we get stuck.
				// Push back by a small margin so this should never happen.
				Position = pm.EndPosition + pm.Normal * 0.03125f;
			}
			else
			{
				Position = pm.EndPosition;

				break;
			}

			moveplanes.StartBump( Velocity );

			bool isFloor = IsFloor( pm, up );

			if ( bump == 0 && !isFloor )
			{
				HitWall = true;
				HitNormal = pm.Normal;
				WallPosition = pm.HitPosition;
			}


			timeLeft -= timeLeft * pm.Fraction;

			if ( !moveplanes.TryAdd( pm.Normal, ref Velocity, isFloor ? GroundBounce : WallBounce ) )
				break;
		}

		if ( travelFraction == 0 )
			Velocity = 0;

		return travelFraction;
	}

	/// <summary>
	/// Return true if this is the trace is a floor. Checks hit and normal angle.
	/// </summary>
	private bool IsFloor( SceneTraceResult tr, Vector3 up )
	{
		if ( !tr.Hit ) return false;
		return tr.Normal.Angle( up ) < MaxStandableAngle;
	}

	/// <summary>
	/// Move our position by this delta using trace. If we hit something we'll stop,
	/// we won't slide across it nicely like TryMove does.
	/// </summary>
	private SceneTraceResult TraceMove( Vector3 delta )
	{
		var tr = TraceFromTo( Position, Position + delta );
		Position = tr.EndPosition;
		return tr;
	}

	/// <summary>
	/// Like TryMove but will also try to step up if it hits a wall
	/// </summary>
	public float TryMoveWithStep( float timeDelta, float stepsize, Vector3 up, out float stepAmount )
	{
		var startPosition = Position;
		stepAmount = 0f;

		// Make a copy of us to stepMove
		var stepMove = this;

		// Do a regular move
		var fraction = TryMove( timeDelta, up );

		// If it got all the way then that's cool, use it
		//if ( fraction.AlmostEqual( 0 ) )
		//	return fraction;

		// Move up (as much as we can)
		stepMove.TraceMove( up * stepsize );

		// Move across (using existing velocity)
		var stepFraction = stepMove.TryMove( timeDelta, up );

		// Move back down
		var tr = stepMove.TraceMove( -up * stepsize );

		// if we didn't land on something, return
		if ( !tr.Hit ) return fraction;

		// If we landed on a wall then this is no good
		if ( tr.Normal.Angle( up ) > MaxStandableAngle )
			return fraction;

		// if the original non stepped attempt moved further use that
		//if ( startPosition.Distance( Position.WithZ( startPosition.z ) ) > startPosition.Distance( stepMove.Position.WithZ( startPosition.z ) ) )
		//	return fraction;
		float regularDistance = Vector3.VectorPlaneProject( startPosition, up ).Distance( Vector3.VectorPlaneProject( Position, up ) );
		float stepMoveDistance = Vector3.VectorPlaneProject( startPosition, up ).Distance( Vector3.VectorPlaneProject( stepMove.Position, up ) );

		if ( regularDistance > stepMoveDistance )
			return fraction;

		// step move moved further, copy its data to us
		Position = stepMove.Position;
		Velocity = stepMove.Velocity;
		HitWall = stepMove.HitWall;
		HitNormal = stepMove.HitNormal;
		stepAmount = Vector3.Dot( stepMove.Position - startPosition, up );

		return stepFraction;
	}

	/// <summary>
	/// Test whether we're stuck, and if we are then unstuck us
	/// </summary>
	public bool TryUnstuck()
	{
		var tr = TraceFromTo( Position, Position );
		return !tr.StartedSolid || Unstuck();
	}

	/// <summary>
	/// We're inside something solid, lets try to get out of it.
	/// </summary>
	private bool Unstuck()
	{

		//
		// Try going straight up first, people are most of the time stuck in the floor
		//
		for ( int i = 1; i < 20; i++ )
		{
			var tryPos = Position + Vector3.Up * i;

			var tr = TraceFromTo( tryPos, Position );
			if ( !tr.StartedSolid )
			{
				Position = tryPos + tr.Direction.Normal * (tr.Distance - 0.5f);
				Velocity = 0;
				return true;
			}
		}

		//
		// Then fuck it, we got to get unstuck some how, try random shit
		//
		for ( int i = 1; i < 100; i++ )
		{
			var tryPos = Position + Vector3.Random * i;

			var tr = TraceFromTo( tryPos, Position );
			if ( tr.StartedSolid )
			{
				continue;
			}

			Position = tryPos + tr.Direction.Normal * (tr.Distance - 0.5f);
			Velocity = 0;
			return true;
		}

		return false;
	}
}
