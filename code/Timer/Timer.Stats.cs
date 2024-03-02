using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sandbox.Services;

namespace Tf;

public sealed partial class Timer
{
  public int NumCompletionsThisSession { get; private set; }
  public float TopSpeed { get; private set; }
  public float BestTopSpeed { get; private set; }
  public int? BestFirstLoopTime { get; private set; }
  public int? BestLoopTime { get; private set; }

  public static string StatVersion => "0.1";

  private void GetStats()
  {
    // var stats = Stats.GetLocalPlayerStats( "cflo.gauntlet" );

    // bool success1 = stats.TryGet( GetTimeStatIdent( 1 ), out var bestFirstLoopTime );
    // bool success2 = stats.TryGet( GetTimeStatIdent( 2 ), out var bestLoopTime );

    // if ( success1 && success2 && bestFirstLoopTime.Value != 0 && bestLoopTime.Value != 0 )
    // {
    //   BestFirstLoopTime = Convert.ToInt32( bestFirstLoopTime.Value );
    //   BestLoopTime = Convert.ToInt32( bestLoopTime.Value );
    // }
  }

  private void UpdateStats()
  {
    if ( Player.HorzVelocity.LengthSquared > TopSpeed * TopSpeed )
    {
      TopSpeed = Player.HorzVelocity.Length;
    }

    if ( TopSpeed > BestTopSpeed )
    {
      BestTopSpeed = TopSpeed;
    }
  }

  private bool UpdateBestTime( int ticks )
  {
    string stat = GetTimeStatIdent( CurrentLoop );

    if ( CurrentLoop == 1 && (!BestFirstLoopTime.HasValue || ticks < BestFirstLoopTime.Value) )
    {
      Stats.SetValue( stat, ticks );
      BestFirstLoopTime = ticks;
      return true;
    }

    if ( CurrentLoop > 1 && (!BestLoopTime.HasValue || ticks < BestLoopTime.Value) )
    {
      Stats.SetValue( stat, ticks );
      BestLoopTime = ticks;
      return true;
    }

    return false;
  }

  private string GetTimeStatIdent( int loopNum = 1 )
  {
    string title = Scene.Title;
    string levelNumber;
    Match match = Regex.Match( title, @"\d+" );
    if ( match.Success )
    {
      levelNumber = match.Value;
    }
    else
    {
      throw new Exception( "Could not parse level number from title" );
    }

    return $"v{StatVersion}-{levelNumber}.{(loopNum > 1 ? 2 : 1)}.time";
  }
}