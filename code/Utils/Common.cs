using System.Text.RegularExpressions;

namespace Tf;

public static class Common
{
  public static float HUPSToKPH( float hups ) => hups * 0.091392f;

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

    switch ( num % 10 )
    {
      case 1:
        return num + "st";
      case 2:
        return num + "nd";
      case 3:
        return num + "rd";
      default:
        return num + "th";
    }
  }

  public static string TimeToString( float time, bool includeUnit = false )
  {
    TimeSpan timeSpan = TimeSpan.FromSeconds( time );

    if ( timeSpan.Minutes >= 1 )
    {
      return timeSpan.ToString( @"m\:ss" ) + (includeUnit ? "m" : "");
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

  public static string GetTimeStatIdent( string title, int loopNum = 1 )
  {
    string levelNumber;
    Match match = Regex.Match( title, @"\d+" );
    if ( match.Success )
    {
      levelNumber = match.Value;
    }
    else
    {
      throw new Exception( "Could not parse level number from scene title" );
    }

    return $"{Timer.StatVersion}-{levelNumber}.{(loopNum > 1 ? 2 : 1)}.time";
  }
}