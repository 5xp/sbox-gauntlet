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

  public static string StatVersion => "v0.1";

  /// <summary>
  /// Called when the game starts. Fetches the leaderboard entries for the current level, so our entry gets cached.
  /// </summary>
  private async Task GetStats()
  {
    Task t1 = LeaderboardManager.Instance.FetchLeaderboardEntries( Common.GetTimeStatIdent( Scene.Title, 1 ) );
    Task t2 = LeaderboardManager.Instance.FetchLeaderboardEntries( Common.GetTimeStatIdent( Scene.Title, 2 ) );

    await Task.WhenAll( t1, t2 );

    SetStats();
  }

  private void SetStats()
  {
    if ( LeaderboardManager.Instance.MyEntryCache.TryGetValue( Common.GetTimeStatIdent( Scene.Title, 1 ), out var entry ) )
    {
      BestFirstLoopTime = Convert.ToInt32( entry.Value );
    }

    if ( LeaderboardManager.Instance.MyEntryCache.TryGetValue( Common.GetTimeStatIdent( Scene.Title, 2 ), out var entry2 ) )
    {
      BestLoopTime = Convert.ToInt32( entry2.Value );
    }
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
    string stat = Common.GetTimeStatIdent( Scene.Title, CurrentLoop );

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
}