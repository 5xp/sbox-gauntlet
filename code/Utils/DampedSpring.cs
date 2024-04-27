namespace Gauntlet.Utils;

/// <summary>
/// A damped spring system. Used for viewpunch/viewkick/recoil.
/// </summary>
public class DampedSpring
{
	private Vector3 SpringConstant { get; set; }
	private Vector3 Damping { get; set; }
	public Vector3 TargetPosition { get; set; }
	public Vector3 Position { get; private set; }
	public Vector3 Velocity { get; private set; }

	public DampedSpring( Vector3 springConstant, Vector3 damping )
	{
		SpringConstant = springConstant;
		Damping = damping;
	}

	public DampedSpring()
	{
		SpringConstant = 65f;
		Damping = 9f;
	}

	public void Update( float timeDelta )
	{
		Vector3 springForce = -SpringConstant * (Position - TargetPosition);

		Vector3 damping = Vector3.One - (Damping * timeDelta);

		damping = Vector3.Max( damping, Vector3.Zero );

		Velocity *= damping;

		Velocity += springForce * (1f - float.Exp( -timeDelta ));

		Position += Velocity * timeDelta;
	}

	public Angles ToAngles()
	{
		return new Angles( Position.x, Position.y, Position.z );
	}

	public Rotation ToRotation()
	{
		return Rotation.From( Position.x, Position.y, Position.z );
	}

	public void AddRandomVelocity( Vector3 min, Vector3 max )
	{
		float x = Game.Random.Float( min.x, max.x );
		float y = Game.Random.Float( min.y, max.y );
		float z = Game.Random.Float( min.z, max.z );

		Velocity += new Vector3( x, y, z );
	}
}
