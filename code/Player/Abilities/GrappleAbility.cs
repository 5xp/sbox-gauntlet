using Gauntlet.Utils;

namespace Gauntlet.Player.Abilities;

public class GrappleAbility : BaseAbility
{
	private bool _canAttach;

	/// <summary>
	/// Generally, we can only attach when we're shooting or retracting, but in some cases we can attach when we're force
	/// retracting.
	/// </summary>
	private bool CanAttach
	{
		get =>
			State switch
			{
				GrappleState.Shooting or GrappleState.Retracting or GrappleState.Attached => true,
				GrappleState.ForcedRetracting => _canAttach,
				_ => false
			};

		set => _canAttach = value;
	}

	/// <summary>
	/// Contains the positions of the grapple points.
	/// The first point is the grapple hook.
	/// The last point is the only point that is in view of the player. It is also the point we accelerate towards.
	/// </summary>
	[Property, ReadOnly] private List<Vector3> _grapplePoints;

	/// <summary>
	/// Where the grapple hook wants to go. If we're shooting, this is the player's target.
	/// If we're retracting, this is the next point in the list.
	/// If there is no next point in the list, this is null (which represents the player's eye position).
	/// If we're not shooting or retracting, this is null.
	/// </summary>
	private Vector3 _wishHookPoint;

	private Vector3? WishHookPoint
	{
		get =>
			State switch
			{
				GrappleState.Shooting => _wishHookPoint,
				GrappleState.Idle or GrappleState.Attached => null,
				_ => _grapplePoints.Count > 1 ? _grapplePoints[1] : Controller.CameraPosition
			};
		set
		{
			if ( State is GrappleState.Shooting && value.HasValue )
			{
				_wishHookPoint = value.Value;
			}
		}
	}

	/// <summary>
	/// Once we're attached, detach if our grapple length exceeds this value.
	/// </summary>
	private float DetachLength => PlayerSettings.GrappleLength + PlayerSettings.GrappleExtraDetachLength;

	/// <summary>
	/// Tracks how long we've been moving under <see cref="PlayerSettings.GrappleDetachLowSpeedThreshold" />.
	/// If this exceeds <see cref="PlayerSettings.GrappleDetachLowSpeedTime"/>, then we detach.
	/// </summary>
	private TimeSince LowSpeedTime { get; set; }

	private bool HasAttached { get; set; }

	private TimeSince TimeSinceFirstAttach { get; set; }

	[Property, ReadOnly] public bool Pulling { get; set; }

	/// <summary>
	/// Our current state of the grapple.
	/// </summary>
	[Property]
	[ReadOnly]
	private GrappleState State { get; set; } = GrappleState.Idle;

	/// <summary>
	/// Our current tilt fraction. Our view tilts while we are attached.
	/// </summary>
	private float TiltFraction { get; set; }

	private SceneTraceResult? LastSafeTraceResult { get; set; }

	protected override void OnAwake()
	{
		base.OnAwake();

		_grapplePoints = new List<Vector3>( PlayerSettings.GrappleMaxGrapplePoints );
	}

	public override float? GetAcceleration()
	{
		return Pulling ? PlayerSettings.GrappleAirAcceleration : null;
	}

	public override float? GetSpeed()
	{
		return Pulling ? PlayerSettings.GrappleAirMaxSpeed : null;
	}

	public override float? GetGravityScale()
	{
		// TODO
		return null;
	}

	/// <summary>
	/// Here I'm using active to mean whenever the grapple is not idle.
	/// </summary>
	/// <returns></returns>
	public override bool ShouldBecomeActive()
	{
		if ( !IsActive && Input.Pressed( "Ability" ) )
		{
			return true;
		}

		if ( State is GrappleState.Idle )
		{
			return false;
		}

		return true;
	}

	protected override void OnActiveChanged( bool before, bool after )
	{
		_grapplePoints.Clear();
		LastSafeTraceResult = null;
		HasAttached = false;
		Pulling = false;

		if ( !after )
		{
			return;
		}

		State = GrappleState.Shooting;
		CanAttach = true;

		Ray aimRay = Controller.AimRay;

		WishHookPoint = aimRay.Position + aimRay.Forward * PlayerSettings.GrappleLength;

		_grapplePoints.Add( Controller.CameraPosition );
	}

	public override void OnActiveUpdate()
	{
		TraceToGrapplePoint();
		CheckLinesOfSight();
		UpdateHookPoint();

		if ( State is GrappleState.Idle )
		{
			return;
		}

		if ( Controller.Velocity.LengthSquared >= PlayerSettings.GrappleDetachLowSpeedThreshold *
		    PlayerSettings.GrappleDetachLowSpeedThreshold )
		{
			LowSpeedTime = 0;
		}

		if ( State is not GrappleState.ForcedRetracting && ShouldDetach() )
		{
			ForceRetractGrapple();
		}
		else
		{
			Pulling = HasAttached && TimeSinceFirstAttach >= PlayerSettings.GrapplePullDellay;
		}

		if ( Pulling )
		{
			GrappleAttachedUpdate();
		}
	}

	public override void FrameSimulate()
	{
		ApplyTilt();
		DrawGizmos();
	}

	private void GrappleAttachedUpdate()
	{
		Vector3 grapplePoint = _grapplePoints.Last() + Vector3.Up * PlayerSettings.GrappleLift;

		Vector3 grappleDir = (grapplePoint - Controller.CameraPosition).Normal;

		Accelerate( grappleDir, GetGrappleMaxSpeed(), PlayerSettings.GrappleAcceleration );
	}

	private void Accelerate( Vector3 wishDir, float wishSpeed, float acceleration )
	{
		float currentSpeed = Velocity.Dot( wishDir );
		float addSpeed = MathF.Max( 0f, wishSpeed - currentSpeed );

		float accelSpeed = MathF.Min( acceleration * Time.Delta, addSpeed );

		Velocity += wishDir * accelSpeed;
	}

	private float GetGrappleMaxSpeed()
	{
		float t = ((float)TimeSinceFirstAttach).LerpInverse( 0f, PlayerSettings.GrappleSpeedRampTime );
		return PlayerSettings.GrappleSpeedRampMin.LerpTo( PlayerSettings.GrappleSpeedRampMax, t );
	}

	/// <summary>
	/// Move the hook point towards the wish hook point.
	/// </summary>
	private void UpdateHookPoint()
	{
		if ( !WishHookPoint.HasValue )
		{
			return;
		}

		Vector3 hookPoint = _grapplePoints.FirstOrDefault();

		if ( State is GrappleState.Retracting && _grapplePoints.Count > 1 )
		{
			State = GrappleState.ForcedRetracting;
		}

		float speed = GetHookSpeed();

		if ( State is GrappleState.Retracting )
		{
			hookPoint += Vector3.Down * PlayerSettings.GrappleRetractFallSpeed * Time.Delta;
		}

		Vector3 nextPosition = hookPoint.ApproachVector( WishHookPoint.Value, speed * Time.Delta );

		SceneTraceResult tr = Scene.Trace.Ray( hookPoint, nextPosition )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit && State is GrappleState.Shooting )
		{
			nextPosition = tr.HitPosition;
			SetAttached();
			_grapplePoints[0] = nextPosition;
			return;
		}

		_grapplePoints[0] = nextPosition;

		if ( nextPosition.AlmostEqual( WishHookPoint.Value ) )
		{
			OnHookPointReached();
		}
	}

	/// <summary>
	/// Called when the hook reaches its target.
	/// If it's shooting, begin retracting.
	/// If it's retracting, remove the first point. If there are no points left, it was moving to the player.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the state is not shooting or retracting. This should never happen.
	/// </exception>
	private void OnHookPointReached()
	{
		switch ( State )
		{
			case GrappleState.Shooting:
				State = GrappleState.Retracting;
				break;
			case GrappleState.Retracting:
			case GrappleState.ForcedRetracting:
				// This was our only grapple point, so it was moving to the player.
				if ( _grapplePoints.Count == 1 )
				{
					OnGrappleFinishedRetracting();
				}
				// The hook retracted to the next point.
				else if ( State is GrappleState.ForcedRetracting )
				{
					_grapplePoints.RemoveAt( 0 );

					// If we're force retracting before the grapple has finished or been canceled
					if ( CanAttach )
					{
						SetAttached();
					}
				}

				break;
			case GrappleState.Attached:
			case GrappleState.Idle:
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void SetAttached()
	{
		if ( State is GrappleState.Attached )
		{
			return;
		}

		State = GrappleState.Attached;
		LowSpeedTime = 0;

		if ( HasAttached )
		{
			return;
		}

		HasAttached = true;
		OnFirstAttach();
	}

	private void OnFirstAttach()
	{
		TimeSinceFirstAttach = 0;
		Controller.ClearGroundObject();

		float newSpeed = MathF.Max( Velocity.z, PlayerSettings.GrappleAttachVerticalBoost );
		Velocity = Velocity.WithZ( newSpeed );
	}

	private void ForceRetractGrapple()
	{
		if ( State is GrappleState.ForcedRetracting )
		{
			return;
		}

		State = GrappleState.ForcedRetracting;
		CanAttach = false;
		Pulling = false;
	}

	private void OnGrappleFinishedRetracting()
	{
		State = GrappleState.Idle;
	}

	/// <summary>
	/// Trace from our view to the last grapple point. If it hits then our view is obstructed and we add a new grapple point.
	/// </summary>
	private void TraceToGrapplePoint()
	{
		SceneTraceResult tr = TraceWithBackoff( Controller.CameraPosition, _grapplePoints.Last() );

		if ( !tr.Hit )
		{
			LastSafeTraceResult = tr;
			return;
		}

		var endpos = tr.EndPosition;

		if ( LastSafeTraceResult.HasValue )
		{
			endpos = SearchForEdge( LastSafeTraceResult.Value.StartPosition, tr.StartPosition,
				_grapplePoints.Last() );
		}

		_grapplePoints.Add( endpos );

		if ( _grapplePoints.Count <= PlayerSettings.GrappleMaxGrapplePoints )
		{
			return;
		}

		if ( State is GrappleState.ForcedRetracting )
		{
			_grapplePoints.RemoveAt( 0 );
		}
		else
		{
			State = GrappleState.ForcedRetracting;
		}
	}

	private Vector3 SearchForEdge( Vector3 safePosition, Vector3 unsafePosition, Vector3 target, int depth = 3 )
	{
		SceneTraceResult lastUnsafeTraceResult = TraceWithBackoff( unsafePosition, target );

		while ( true )
		{
			Vector3 midpoint = safePosition + (unsafePosition - safePosition) / 2;
			SceneTraceResult tr = TraceWithBackoff( midpoint, target );

			if ( tr.Hit )
			{
				unsafePosition = midpoint;
				lastUnsafeTraceResult = tr;
			}
			else
			{
				safePosition = midpoint;
			}

			if ( depth-- <= 0 )
			{
				return lastUnsafeTraceResult.EndPosition;
			}
		}
	}

	/// <summary>
	/// This is used for line of sight checks but with a backoff so we don't hit the wall.
	/// </summary>
	private SceneTraceResult TraceWithBackoff( Vector3 start, Vector3 end, float backoff = 0.01f )
	{
		Vector3 dir = (end - start).Normal;
		end -= dir * backoff;

		return Scene.Trace.Ray( start, end )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();
	}

	/// <summary>
	/// This is called when we have more than two grapple points.
	/// For each point (starting with the player's eye position), we check if we can see the grapple point two points ahead.
	/// For example, if we have points 1, 2, 3, 4, check if 4 can see 2 and 3 can see 1.
	/// If 4 can see 2, we can remove 3 and 2. If 3 can see 1, we can remove 2.
	/// TODO: Is this even necessary? Maybe I just need to check from the camera to the 2nd to last point.
	/// </summary>
	private void CheckLinesOfSight()
	{
		// List<Vector3> points = _grapplePoints.ToList();
		// points.Add( Controller.CameraPosition );
		//
		// for ( int i = points.Count - 1; i >= 2; i-- )
		// {
		// 	SceneTraceResult tr = Scene.Trace.Ray( points[i], points[i - 2] )
		// 		.IgnoreGameObject( GameObject )
		// 		.Run();
		//
		// 	if ( tr.Hit )
		// 	{
		// 		continue;
		// 	}
		//
		// 	_grapplePoints.RemoveRange( i - 1, 1 );
		// }

		if ( _grapplePoints.Count < 2 )
		{
			return;
		}

		SceneTraceResult tr = TraceWithBackoff( Controller.CameraPosition, _grapplePoints[^2] );

		if ( tr.Hit )
		{
			return;
		}

		Vector3 midPoint = Controller.CameraPosition + (_grapplePoints[^2] - Controller.CameraPosition) / 2;
		SceneTraceResult midTr = TraceWithBackoff( _grapplePoints.Last(), midPoint );

		if ( midTr.Hit )
		{
			return;
		}

		_grapplePoints.RemoveAt( _grapplePoints.Count - 1 );
	}

	private float GetHookSpeed()
	{
		return State switch
		{
			GrappleState.Shooting => PlayerSettings.GrappleShootVel,
			GrappleState.Retracting => PlayerSettings.GrappleRetractVel,
			GrappleState.ForcedRetracting => PlayerSettings.GrappleForcedRetractVel,
			_ => 0
		};
	}

	private bool ShouldDetach()
	{
		// Player wants to detach
		if ( Input.Pressed( "Ability" ) && TimeSinceStart >= PlayerSettings.GrappleDetachOnGrappleDebounceTime )
		{
			return true;
		}

		float grappleLength = GetGrappleLength();

		if ( State is not GrappleState.Attached )
		{
			return false;
		}

		// Grapple length is too long or too short
		if ( grappleLength > DetachLength || grappleLength < PlayerSettings.GrappleDetachLengthMin )
		{
			return true;
		}

		// We're moving away from the grapple point too fast
		float movingAwaySpeed = Vector3.Dot( Controller.Velocity,
			(_grapplePoints.Last() - Controller.CameraPosition).Normal );

		if ( movingAwaySpeed < -PlayerSettings.GrappleDetachAwaySpeed )
		{
			return true;
		}

		// We're moving too slow for too long
		if ( LowSpeedTime > PlayerSettings.GrappleDetachLowSpeedTime )
		{
			return true;
		}

		// We looked more than 90 degrees away from the grapple point
		Angles eyeAngles = Controller.AimAngles;
		Angles toGrapplePoint = (_grapplePoints.Last() - Controller.CameraPosition).Normal.EulerAngles;
		Angles diff = eyeAngles - toGrapplePoint;
		if ( MathF.Abs( diff.Normal.yaw ) > 90 )
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Applies view tilt based on the angle between the player's view and the hook point.
	/// </summary>
	private void ApplyTilt()
	{
		float tiltFrac = 0f;

		if ( State is not GrappleState.Idle )
		{
			Angles eyeAngles = Controller.AimAngles;
			Angles toHookPoint = (_grapplePoints.First() - Controller.CameraPosition).Normal.EulerAngles;
			Angles diff = eyeAngles - toHookPoint;
			float yawDiff = diff.Normal.yaw;

			float angleFrac =
				MathF.Abs( yawDiff ).LerpInverse( PlayerSettings.GrappleRollViewAngleMin,
					PlayerSettings.GrappleRollViewAngleMax ) * MathF.Sign( yawDiff );

			float distanceToHook = Vector3.DistanceBetween( Controller.CameraPosition, _grapplePoints.First() );

			float distanceFrac = distanceToHook.LerpInverse( PlayerSettings.GrappleRollDistanceMin,
				PlayerSettings.GrappleRollDistanceMax );

			tiltFrac = angleFrac * distanceFrac;
		}

		float tiltAmount = TiltFraction.LerpTo( tiltFrac, 3.5f * Time.Delta );

		TiltFraction = TiltFraction.Approach( tiltAmount, 85f * Time.Delta );

		Controller.InputAngles += new Angles( 0f, 0f, TiltFraction * PlayerSettings.GrappleMaxRoll );
	}

	/// <summary>
	/// Sums the distances between each grapple point and the last point to the player's eye position.
	/// </summary>
	/// <returns>The summed current grapple length.</returns>
	private float GetGrappleLength()
	{
		float grappleDistance = _grapplePoints
			.Zip( _grapplePoints.Skip( 1 ), ( a, b ) => Vector3.DistanceBetween( a, b ) ).Sum();

		grappleDistance += _grapplePoints.Last().Distance( Controller.CameraPosition );

		return grappleDistance;
	}

	private enum GrappleState
	{
		Idle,
		Shooting,
		Attached,
		Retracting,
		ForcedRetracting
	}

	private new void DrawGizmos()
	{
		using ( Gizmo.Scope( "grapple" ) )
		{
			Gizmo.Transform = global::Transform.Zero;

			for ( int i = 0; i < _grapplePoints.Count - 1; i++ )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.SolidCapsule( _grapplePoints[i], _grapplePoints[i + 1], 0.5f, 5, 5 );
			}


			for ( int i = 1; i < _grapplePoints.Count; i++ )
			{
				Gizmo.Draw.Color = Color.Blue;
				// Gizmo.Draw.SolidSphere( _grapplePoints[i], 5f );
			}

			if ( _grapplePoints.Count == 0 )
			{
				return;
			}

			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.SolidSphere( _grapplePoints.First(), 5f );

			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.SolidCapsule( Controller.CameraPosition + Vector3.Down * 6, _grapplePoints.Last(), 0.5f, 5, 5 );
		}
	}
}
