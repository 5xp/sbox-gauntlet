using System.Collections.Concurrent;
using System.Threading.Tasks;
using Gauntlet.Utils;
using Sandbox.Services;

namespace Gauntlet;

public struct GauntletLeaderboardEntry
{
	public string DisplayName { get; init; }
	public long SteamId { get; init; }
	public int TimeTicks { get; init; }
	public int Rank { get; set; }
	public bool Me { get; set; }
}

internal class TimeComparer : IComparer<GauntletLeaderboardEntry>
{
	public int Compare( GauntletLeaderboardEntry x, GauntletLeaderboardEntry y )
	{
		return x.TimeTicks.CompareTo( y.TimeTicks );
	}
}

public sealed class LeaderboardManager
{
	private static LeaderboardManager _instance;

	public static LeaderboardManager Instance
	{
		get
		{
			_instance ??= new LeaderboardManager();
			return _instance;
		}
	}

	private LeaderboardManager()
	{
	}

	private bool ShouldPoll { get; set; }

	public List<long> BlacklistedSteamIds { get; } = new()
	{
		76561198356798272,
		76561198927823324,
		76561198987213809,
		76561198203150213,
		76561198167838587,
		76561199401217985,
	};

	/// <summary>
	/// A dictionary of cached leaderboard entries, indexed by the leaderboard identifier.
	/// </summary>
	private readonly Dictionary<string, SortedSet<GauntletLeaderboardEntry>> _leaderboardCache = new();

	/// <summary>
	/// A dictionary of ongoing fetches, indexed by the leaderboard identifier.
	/// </summary>
	private readonly ConcurrentDictionary<string, Task<SortedSet<GauntletLeaderboardEntry>>> _ongoingFetches = new();

	/// <summary>
	/// A dictionary of the player's leaderboard entries, indexed by the leaderboard identifier.
	/// </summary>
	public Dictionary<string, GauntletLeaderboardEntry> MyEntryCache { get; private set; } = new();

	public Action OnLeaderboardRefresh;

	public void ResetSubscriptions() => OnLeaderboardRefresh = null;

	/// <summary>
	/// Fetches the leaderboard entries for the given leaderboard identifier.
	/// </summary>
	/// <param name="ident">The leaderboard identifier in the backend.</param>
	/// <param name="force">Whether to force a refresh of the leaderboard entries, even if they are already cached.</param>
	/// <returns>A tuple containing the top 10 entries and the entries around the player.</returns>
	public async Task<SortedSet<GauntletLeaderboardEntry>> FetchLeaderboardEntries( string ident, bool force = false )
	{
		try
		{
			if ( !force && _leaderboardCache.TryGetValue( ident, out var entries ) )
			{
				return entries;
			}

			// If a fetch is already in progress for this key, wait for it to complete and return its result.
			if ( _ongoingFetches.TryGetValue( ident, out var ongoingFetch ) )
			{
				return await ongoingFetch;
			}

			// No fetch in progress, so start a new one.
			var fetchTask = FetchFromLeaderboard( ident );

			// Store the task in the dictionary so other callers can await it.
			_ongoingFetches[ident] = fetchTask;

			var result = await fetchTask;

			// Once the fetch is complete, remove it from the dictionary.
			_ongoingFetches.TryRemove( ident, out _ );

			return result;
		}
		catch ( Exception e )
		{
			Log.Error( e );
			return new SortedSet<GauntletLeaderboardEntry>();
		}
	}

	private async Task<SortedSet<GauntletLeaderboardEntry>> FetchFromLeaderboard( string key )
	{
		Leaderboards.Board board = Leaderboards.Get( key );

		board.MaxEntries = 999999999;
		await board.Refresh();
		var entries = board.Entries;

		if ( !_leaderboardCache.TryGetValue( key, out SortedSet<GauntletLeaderboardEntry> value ) )
		{
			value = new SortedSet<GauntletLeaderboardEntry>( new TimeComparer() );
			_leaderboardCache[key] = value;
		}

		foreach ( var entry in entries )
		{
			AddOrUpdateEntry( key, ConvertEntry( entry ) );
		}

		OnLeaderboardRefresh?.Invoke();

		return value;
	}

	public async Task<List<GauntletLeaderboardEntry>> GetTopTen( string key, bool force = false )
	{
		var entries = (await FetchLeaderboardEntries( key, force )).Take( 10 ).ToList();

		entries = entries.Select( ( entry, index ) =>
		{
			entry.Rank = index + 1;
			return entry;
		} ).ToList();

		return entries;
	}

	public async Task<List<GauntletLeaderboardEntry>> GetAroundPlayer( string key, long steamId, int take = 10,
		bool force = false )
	{
		var entries = await FetchLeaderboardEntries( key, force );
		var playerEntry = entries.FirstOrDefault( entry => entry.SteamId == steamId );

		if ( playerEntry.Equals( default(GauntletLeaderboardEntry) ) )
		{
			return new List<GauntletLeaderboardEntry>();
		}

		var playerIndex = entries.ToList().IndexOf( playerEntry );
		var startIndex = Math.Max( 0, playerIndex - take / 2 );
		var endIndex = Math.Min( entries.Count, startIndex + take );

		return entries.ToList().GetRange( startIndex, endIndex - startIndex ).Select( ( entry, index ) =>
		{
			entry.Rank = startIndex + index + 1;
			return entry;
		} ).ToList();
	}

	/// <summary>
	/// When we enter the start zone, we start polling the leaderboard entries for the current level.
	/// </summary>
	public void StartPolling( Timer.Timer timer )
	{
		if ( ShouldPoll )
		{
			return;
		}

		ShouldPoll = true;

		GameTask.RunInThreadAsync( async () =>
		{
			try
			{
				while ( ShouldPoll )
				{
					await GameTask.MainThread();

					if ( timer.InStartZone && timer.Scene is not null )
					{
						string stat = GameManager.Instance.LevelData.GetStatId( timer.CurrentLoop );
						await FetchLeaderboardEntries( stat, true );
					}
					else
					{
						StopPolling();
					}

					await Task.Delay( TimeSpan.FromSeconds( 5 ) );
				}
			}
			catch ( Exception e )
			{
				Log.Error( e );
				ShouldPoll = false;
			}
		} );
	}

	public void StopPolling()
	{
		ShouldPoll = false;
	}

	private static GauntletLeaderboardEntry ConvertEntry( Leaderboards.Entry entry )
	{
		return new GauntletLeaderboardEntry
		{
			DisplayName = entry.DisplayName, SteamId = entry.SteamId, TimeTicks = Convert.ToInt32( entry.Value )
		};
	}

	/// <summary>
	/// Adds or updates a leaderboard entry in the cache
	/// </summary>
	/// <param name="ident">The identifier of the leaderboard</param>
	/// <param name="entry">The leaderboard entry</param>
	/// <param name="updateSelf">Whether to add or update ourself. If called from the poll, this will be false
	/// If called from new PB, this will be true
	/// </param>
	public void AddOrUpdateEntry( string ident, GauntletLeaderboardEntry entry, bool updateSelf = false )
	{
		if ( BlacklistedSteamIds.Contains( entry.SteamId ) )
		{
			return;
		}

		if ( entry.SteamId == Game.SteamId )
		{
			entry.Me = true;

			if ( MyEntryCache.ContainsKey( ident ) && !updateSelf )
			{
				return;
			}

			MyEntryCache[ident] = entry;
		}

		var entries = _leaderboardCache[ident];

		entries.RemoveWhere( e => e.SteamId == entry.SteamId );
		entries.Add( entry );
	}

	/// <summary>
	/// Prints the leaderboard entries for the given identifier to the console.
	/// </summary>
	/// <param name="ident"></param>
	[ConCmd( "gauntlet_leaderboard_dump" )]
	public static void DumpLeaderboard( string ident )
	{
		if ( !Instance._leaderboardCache.TryGetValue( ident, out SortedSet<GauntletLeaderboardEntry> entries ) )
		{
			Log.Info( $"Leaderboard {ident} not found" );
			return;
		}

		Log.Info( $"Leaderboard {ident}:" );
		foreach ( var entry in entries )
		{
			Log.Info( $"{entry.Rank}: {entry.DisplayName} ({entry.SteamId}) - {entry.TimeTicks}" );
		}
	}

	/// <summary>
	/// Forces a refresh of the leaderboard entries for the given identifier.
	/// </summary>
	/// <param name="ident"></param>
	[ConCmd( "gauntlet_leaderboard_refresh" )]
	public static void RefreshLeaderboard( string ident )
	{
		_ = Instance.FetchLeaderboardEntries( ident, true );
	}
}
