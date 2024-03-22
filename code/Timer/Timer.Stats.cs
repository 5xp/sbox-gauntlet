using System.Threading.Tasks;
using Sandbox.Services;
using Sandbox.Utility;

namespace Gauntlet;

public sealed partial class Timer
{
  public int NumCompletionsThisSession { get; private set; }
  public float TopSpeed { get; private set; }
  public float BestTopSpeed { get; private set; }
  public int? BestFirstLoopTime { get; private set; }
  public int? BestLoopTime { get; private set; }
  public bool HasJumped { get; private set; }
  public bool HasSlid { get; private set; }
  public bool HasWallran { get; private set; }

  public static string StatVersion => "v0.2";

  /// <summary>
  /// Called when the game starts. Fetches the leaderboard entries for the current level, so our entry gets cached.
  /// </summary>
  private async Task GetStats()
  {

    Common.TryGetLeaderboardIdent( Scene.Title, 1, out string id1 );
    Common.TryGetLeaderboardIdent( Scene.Title, 2, out string id2 );

    if ( id1 is null || id2 is null )
    {
      return;
    }

    Task t1 = LeaderboardManager.Instance.FetchLeaderboardEntries( id1 );
    Task t2 = LeaderboardManager.Instance.FetchLeaderboardEntries( id2 );

    await Task.WhenAll( t1, t2 );

    SetStats();
  }

  private void SetStats()
  {
    string stat1 = Common.ParseLeaderboardIdent( Scene.Title, 1 );
    string stat2 = Common.ParseLeaderboardIdent( Scene.Title, 2 );

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

    if ( Player.HasAnyTag( "jump", "walljump", "airjump" ) )
    {
      HasJumped = true;
    }

    if ( Player.HasTag( "slide" ) )
    {
      HasSlid = true;
    }

    if ( Player.HasTag( "wallrun" ) )
    {
      HasWallran = true;
    }
  }

  /// <summary>
  /// Checks if the given time is the best time for the current loop, and if so, updates the stats and leaderboard.
  /// </summary>
  /// <param name="ticks">How many ticks it took to complete</param>
  /// <returns>Whether the time is the best time for the current loop.</returns>
  private bool CheckBestTime( int ticks )
  {
    string stat = Common.ParseLeaderboardIdent( Scene.Title, CurrentLoop );

    GauntletLeaderboardEntry entry = new()
    {
      DisplayName = Steam.PersonaName,
      SteamId = Game.SteamId,
      TimeTicks = ticks
    };

    if ( CurrentLoop == 1 && (!BestFirstLoopTime.HasValue || ticks < BestFirstLoopTime.Value) )
    {
      if ( !DebugConVars.DebugDisableTimeSubmission )
      {
        Stats.SetValue( stat, ticks );
      }
      LeaderboardManager.Instance.AddOrUpdateEntry( stat, entry, true );
      BestFirstLoopTime = ticks;
      return true;
    }

    if ( CurrentLoop > 1 && (!BestLoopTime.HasValue || ticks < BestLoopTime.Value) )
    {
      if ( !DebugConVars.DebugDisableTimeSubmission )
      {
        Stats.SetValue( stat, ticks );
      }
      LeaderboardManager.Instance.AddOrUpdateEntry( stat, entry, true );
      BestLoopTime = ticks;
      return true;
    }

    return false;
  }

  private bool IsRunValid()
  {
    if ( !HasJumped )
    {
      return false;
    }

    if ( !HasWallran && !HasSlid )
    {
      return false;
    }

    if ( TopSpeed < Player.PlayerSettings.WalkSpeed )
    {
      return false;
    }

    return true;
  }
}