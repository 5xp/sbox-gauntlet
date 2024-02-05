namespace Tf;

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
}
