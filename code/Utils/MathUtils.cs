using System.Numerics;

namespace Gauntlet.Utils;

public static class MathUtils
{
	/// <summary>
	/// Moves the current vector closer to a target vector by a certain distance.
	/// </summary>
	/// <param name="from">Initial Vector3 position</param>
	/// <param name="to">Target Vector3 position</param>
	/// <param name="delta">Amount to travel towards target</param>
	/// <returns>
	/// If the distance between current vector and target vector is less than delta,
	/// returns the delta. Otherwise, returns a vector that is closer to the target
	/// vector by the given distance.
	/// </returns>
	public static Vector3 ApproachVector( this Vector3 from, Vector3 to, float delta )
	{
		Vector3 diff = to - from;
		float distanceSquared = diff.LengthSquared;

		if ( distanceSquared <= delta * delta )
		{
			return to;
		}

		diff = diff.ClampLength( delta );
		return from + diff;
	}

	public static float LerpExp( this float from, float to, float delta, bool clamp = true )
	{
		return from.LerpTo( to, 1 - MathF.Exp( -delta ), clamp );
	}

	/// <summary>
	/// Rotates the current unit vector closer to a target unit vector by a certain angle.
	/// </summary>
	/// <param name="from">Initial unit Vector3 position</param>
	/// <param name="to">Target unit Vector3 position</param>
	/// <param name="angleDelta">Angle to rotate towards target (in radians)</param>
	/// <returns>
	/// If the angle between current vector and target vector is less than angleDelta,
	/// returns the target vector. Otherwise, returns a vector that is closer to the target
	/// vector by the given angle.
	/// </returns>
	public static Vector3 RotateTowards( this Vector3 from, Vector3 to, float angleDelta )
	{
		float dot = Vector3.Dot( from, to );
		float angle = MathF.Acos( dot );

		if ( angle <= angleDelta )
		{
			return to;
		}

		Vector3 axis = Vector3.Cross( from, to ).Normal;

		Quaternion rotationIncrement = Quaternion.CreateFromAxisAngle( axis, angleDelta );

		return from * rotationIncrement;
	}

	/// <summary>
	/// Clamps the length of the current vector to a maximum value.
	/// </summary>
	/// <param name="input">The input vector to be clamped.</param>
	/// <param name="axis">The axis to clamp the length on.</param>
	/// <param name="maxLength">The maximum length of the vector.</param>
	/// <returns>The clamped vector.</returns>
	public static Vector3 ClampLengthOnAxis( this Vector3 input, Vector3 axis, float maxLength )
	{
		Vector3 projection = input.ProjectOnNormal( axis );

		if ( projection.LengthSquared > maxLength * maxLength )
		{
			return axis.Normal * maxLength + (input - projection);
		}

		return input;
	}


	/// <summary>
	/// Smooth step interpolation function.
	/// </summary>
	/// <param name="t">The input value to be interpolated.</param>
	/// <param name="clamp">Whether the result should be clamped between 0 and 1. Default is true.</param>
	/// <returns>The interpolated smooth step value.</returns>
	public static float SmoothStep( float t, bool clamp = true )
	{
		float result = t * t * (3 - 2 * t);

		if ( clamp )
		{
			result = result.Clamp( 0f, 1f );
		}

		return result;
	}

	/// <summary>
	/// Ease out sine function.
	/// </summary>
	/// <param name="t">The input value to be interpolated.</param>
	/// <param name="clamp">Whether the result should be clamped between 0 and 1. Default is true.</param>
	/// <returns>The interpolated value.</returns>
	public static float EaseOutSine( float t, bool clamp = true )
	{
		float result = MathF.Sin( t * MathF.PI * 0.5f );

		if ( clamp )
		{
			result = result.Clamp( 0f, 1f );
		}

		return result;
	}

	/// <summary>
	/// Ease out cubic function.
	/// </summary>
	/// <param name="t">The input value to be interpolated.</param>
	/// <param name="clamp">Whether the result should be clamped between 0 and 1. Default is true.</param>
	/// <returns>The interpolated value.</returns>
	public static float EaseOutCubic( float t, bool clamp = true )
	{
		if ( clamp )
		{
			t = t.Clamp( 0f, 1f );
		}

		return 1 - (1 - t) * (1 - t) * (1 - t);
	}

	/// <summary>
	/// Finds the vector distance between two angles.
	/// </summary>
	/// <param name="a">The first angle.</param>
	/// <param name="b">The second angle.</param>
	/// <returns>The distance between the two angles.</returns>
	public static float Distance( this Angles a, Angles b )
	{
		return a.Forward.Distance( b.Forward );
	}
}
