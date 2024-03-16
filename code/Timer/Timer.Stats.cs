using System.Threading.Tasks;
using Sandbox.Services;
using Sandbox.Utility;

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
    string stat1 = Common.GetTimeStatIdent( Scene.Title, 1 );
    string stat2 = Common.GetTimeStatIdent( Scene.Title, 2 );

    if ( LeaderboardManager.Instance.MyEntryCache.TryGetValue( stat1, out var entry ) )
    {
      BestFirstLoopTime = entry.TimeTicks;
    }

    if ( LeaderboardManager.Instance.MyEntryCache.TryGetValue( stat2, out var entry2 ) )
    {
      BestLoopTime = entry2.TimeTicks;
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

    GauntletLeaderboardEntry entry = new()
    {
      DisplayName = Steam.PersonaName,
      SteamId = Game.SteamId,
      TimeTicks = ticks
    };

    if ( CurrentLoop == 1 && (!BestFirstLoopTime.HasValue || ticks < BestFirstLoopTime.Value) )
    {
      Stats.SetValue( stat, ticks );
      LeaderboardManager.Instance.AddOrUpdateEntry( stat, entry, true );
      BestFirstLoopTime = ticks;
      return true;
    }

    if ( CurrentLoop > 1 && (!BestLoopTime.HasValue || ticks < BestLoopTime.Value) )
    {
      Stats.SetValue( stat, ticks );
      LeaderboardManager.Instance.AddOrUpdateEntry( stat, entry, true );
      BestLoopTime = ticks;
      return true;
    }

    return false;
  }
}