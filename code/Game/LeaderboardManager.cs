using System.Collections.Concurrent;
using System.Threading.Tasks;
using Sandbox.Services;
namespace Tf;

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

	/// <summary>
	/// A dictionary of cached leaderboard entries, indexed by the leaderboard identifier.
	/// 
	/// The first item in the tuple is the top 10 entries, and the second item is the entries around the player.
	/// </summary>
	private readonly Dictionary<string, Tuple<List<Leaderboards.Entry>, List<Leaderboards.Entry>>> LeaderboardCache = new();

	/// <summary>
	/// A dictionary of ongoing fetches, indexed by the leaderboard identifier.
	/// </summary>
	private readonly ConcurrentDictionary<string, Task<Tuple<List<Leaderboards.Entry>, List<Leaderboards.Entry>>>> OngoingFetches = new();

	/// <summary>
	/// A dictionary of the player's leaderboard entries, indexed by the leaderboard identifier.
	/// </summary>
	public Dictionary<string, Leaderboards.Entry> MyEntryCache { get; private set; } = new();

	public Action OnLeaderboardRefresh;

	public void ResetSubscriptions() => OnLeaderboardRefresh = null;

	/// <summary>
	/// Fetches the leaderboard entries for the given leaderboard identifier.
	/// </summary>
	/// <param name="ident">The leaderboard identifier in the backend.</param>
	/// <param name="force">Whether to force a refresh of the leaderboard entries, even if they are already cached.</param>
	/// <returns>A tuple containing the top 10 entries and the entries around the player.</returns>
	public async Task<Tuple<List<Leaderboards.Entry>, List<Leaderboards.Entry>>> FetchLeaderboardEntries( string ident, bool force = false )
	{
		try
		{
			if ( !force && LeaderboardCache.TryGetValue( ident, out var entries ) )
			{
				return entries;
			}

			// If a fetch is already in progress for this key, wait for it to complete and return its result.
			if ( OngoingFetches.TryGetValue( ident, out var ongoingFetch ) )
			{
				return await ongoingFetch;
			}

			// No fetch in progress, so start a new one.
			var fetchTask = FetchFromLeaderboard( ident );

			// Store the task in the dictionary so other callers can await it.
			OngoingFetches[ident] = fetchTask;

			var result = await fetchTask;

			// Once the fetch is complete, remove it from the dictionary.
			OngoingFetches.TryRemove( ident, out _ );

			return result;
		}
		catch ( Exception e )
		{
			Log.Error( e );
			return Tuple.Create( new List<Leaderboards.Entry>(), new List<Leaderboards.Entry>() );
		}
	}

	private async Task<Tuple<List<Leaderboards.Entry>, List<Leaderboards.Entry>>> FetchFromLeaderboard( string key )
	{
		Leaderboards.Board board = Leaderboards.Get( key );
		List<Leaderboards.Entry> topTen = new();
		List<Leaderboards.Entry> aroundPlayer = new();

		// First get the 10 entries around the player
		board.MaxEntries = 10;
		await board.Refresh();
		aroundPlayer.AddRange( board.Entries );

		foreach ( var entry in aroundPlayer )
		{
			if ( entry.SteamId == Game.SteamId )
			{
				MyEntryCache[key] = entry;
			}
		}

		// HACK: The leaderboard API doesn't support getting the top 10 entries, so we have to get the total number of entries first
		board.MaxEntries = Convert.ToInt32( board.TotalEntries );
		await board.Refresh();
		topTen.AddRange( board.Entries.Take( 10 ) );

		LeaderboardCache[key] = new( topTen, aroundPlayer );

		OnLeaderboardRefresh?.Invoke();

		return LeaderboardCache[key];
	}

	public async Task<List<Leaderboards.Entry>> GetTopTen( string key, bool force = false )
	{
		return (await FetchLeaderboardEntries( key, force )).Item1;
	}

	public async Task<List<Leaderboards.Entry>> GetAroundPlayer( string key, bool force = false )
	{
		return (await FetchLeaderboardEntries( key, force )).Item2;
	}

	/// <summary>
	/// When we enter the start zone, we start polling the leaderboard entries for the current level.
	/// </summary>
	public void StartPolling( Timer timer )
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

					if ( timer.InStartZone && timer.Scene is not null )
					{
						await FetchLeaderboardEntries( Common.GetTimeStatIdent( timer.Scene.Title, timer.CurrentLoop ), true );
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
}