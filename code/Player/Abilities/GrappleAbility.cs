using Gauntlet.Utils;

namespace Gauntlet.Player.Abilities;

public class GrappleAbility : BaseAbility
{
	/// <summary>
	/// Our current state of the grapple.
	/// </summary>
	[Property, ReadOnly]
	private GrappleState State { get; set; } = GrappleState.Idle;

	/// <summary>
	/// Contains the positions of the grapple points.
	/// The first point is the grapple hook.t
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
		get
		{
			return State switch
			{
				GrappleState.Shooting => _wishHookPoint,
				GrappleState.Idle or GrappleState.Attached => null,
				_ => _grapplePoints.Count > 1 ? _grapplePoints[1] : Controller.CameraPosition
			};
		}
		set
		{
			if ( State is GrappleState.Shooting && value.HasValue )
			{
				_wishHookPoint = value.Value;
			}
		}
	}

	protected override void OnAwake()
	{
		base.OnAwake();

		_grapplePoints = new List<Vector3>( PlayerSettings.GrappleMaxGrapplePoints );
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

		if ( !after )
		{
			return;
		}

		State = GrappleState.Shooting;

		Ray aimRay = Controller.AimRay;

		WishHookPoint = aimRay.Position + aimRay.Forward * PlayerSettings.GrappleLength;

		_grapplePoints.Add( Controller.CameraPosition );
	}

	public override void OnActiveUpdate()
	{
		TraceToGrapplePoint();
		CheckLinesOfSight();
		UpdateHookPoint();
	}

	public override void FrameSimulate()
	{
		DrawGizmos();
	}

	private new void DrawGizmos()
	{
		using ( Gizmo.Scope( "grapple" ) )
		{
			Gizmo.Transform = global::Transform.Zero;

			for ( int i = 0; i < _grapplePoints.Count - 1; i++ )
			{
				Gizmo.Draw.Color = Color.White;
				Gizmo.Draw.Line( _grapplePoints[i], _grapplePoints[i + 1] );

				Gizmo.Draw.Color = Color.Blue;
				Gizmo.Draw.SolidSphere( _grapplePoints[i], 5f );
				Gizmo.Draw.SolidSphere( _grapplePoints[i + 1], 5f );
			}

			if ( _grapplePoints.Count == 0 )
			{
				return;
			}

			Gizmo.Draw.Color = Color.White;

			Gizmo.Draw.Line( Controller.CameraPosition + Vector3.Down * 5, _grapplePoints.Last() );
		}
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

		float speed = GetHookSpeed();

		if ( State is GrappleState.Retracting )
		{
			hookPoint += Vector3.Down * PlayerSettings.GrappleRetractFallSpeed * Time.Delta;
		}

		hookPoint = hookPoint.ApproachVector( WishHookPoint.Value, speed * Time.Delta );

		_grapplePoints[0] = hookPoint;

		if ( hookPoint.AlmostEqual( WishHookPoint.Value ) )
		{
			OnHookPointReached();
		}
	}

	/// <summary>
	/// Called when the hook reaches its target.
	/// If it's shooting, begin retracting.
	/// If it's retracting, remove the first point. If there are no points left, it was moving to the player.
	/// </summary>
	/// 
	/// <exception cref="ArgumentOutOfRangeException">
	/// Thrown when the state is not shooting or retracting. This should never happen.
	/// </exception>
	private void OnHookPointReached()
	{
		Log.Info( "Hook point reached." );
		switch ( State )
		{
			case GrappleState.Shooting:
				State = GrappleState.Retracting;
				break;
			case GrappleState.Retracting:
				if ( _grapplePoints.Count == 1 )
				{
					OnGrappleFinishedRetracting();
				}
				else
				{
					State = GrappleState.Attached;
				}

				break;
			case GrappleState.ForcedRetracting:
				// This was our only grapple point, so it was moving to the player.
				if ( _grapplePoints.Count == 1 )
				{
					OnGrappleFinishedRetracting();
				}
				else
				{
					_grapplePoints.RemoveAt( 0 );
				}

				break;
			case GrappleState.Attached:
			case GrappleState.Idle:
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private void ForceRetractGrapple()
	{
		if ( State is GrappleState.ForcedRetracting )
		{
			return;
		}

		State = GrappleState.ForcedRetracting;
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
		SceneTraceResult tr = Scene.Trace.Ray( Controller.CameraPosition, _grapplePoints.Last() )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !tr.Hit )
		{
			return;
		}

		_grapplePoints.Add( tr.EndPosition );

		if ( _grapplePoints.Count <= PlayerSettings.GrappleMaxGrapplePoints )
		{
			return;
		}

		if ( State is GrappleState.Retracting )
		{
			_grapplePoints.RemoveAt( 0 );
		}
		else
		{
			State = GrappleState.Retracting;
		}
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

		SceneTraceResult tr = Scene.Trace.Ray( Controller.CameraPosition, _grapplePoints[^2] )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( tr.Hit )
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

	private enum GrappleState
	{
		Idle,
		Shooting,
		Attached,
		Retracting,
		ForcedRetracting,
	}
}
