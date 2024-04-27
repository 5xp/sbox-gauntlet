using System.Text.RegularExpressions;

namespace Gauntlet.Utils;

public static class Common
{
	public static float HupsToKph( float hups ) => hups * 0.09f;

	public static string ToOrdinal( this int num )
	{
		if ( num <= 0 ) return num.ToString();

		switch ( num % 100 )
		{
			case 11:
			case 12:
			case 13:
				return num + "th";
		}

		return (num % 10) switch
		{
			1 => num + "st",
			2 => num + "nd",
			3 => num + "rd",
			_ => num + "th"
		};
	}

	public static string TimeToString( float time, bool includeUnit = false )
	{
		TimeSpan timeSpan = TimeSpan.FromSeconds( time );

		if ( timeSpan.Minutes >= 1 )
		{
			return timeSpan.ToString( @"m\:ss" );
		}
		else
		{
			return timeSpan.ToString( @"s\.ff" ) + (includeUnit ? "s" : "");
		}
	}

	public static string TimeToString( int ticks, float timeDelta, bool includeUnit = false )
	{
		return TimeToString( ticks * timeDelta, includeUnit );
	}

	/// <summary>
	/// Fades the volume of a sound handle linearly. Stops the sound if the volume reaches 0.
	/// </summary>
	public static void FadeVolume( this SoundHandle sound, float amount )
	{
		if ( !sound.IsValid() )
		{
			return;
		}

		sound.Volume = sound.Volume.Approach( 0f, amount );

		if ( sound.Volume.AlmostEqual( 0f ) )
		{
			sound.Stop();
		}
	}

	public static bool EscapePressed => Input.EscapePressed;

	public static bool DuckDown()
	{
		return Input.Down( "Duck" ) || Input.Down( "Duck2" );
	}
}
