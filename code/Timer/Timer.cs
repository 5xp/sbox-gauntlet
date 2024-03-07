namespace Tf;

[Icon( "timer" )]
public sealed partial class Timer : Component
{
	[Property] public PlayerController Player { get; set; }
	[Property, ReadOnly] public float Time { get; set; }
	public int Ticks { get; set; }

	/// <summary>
	/// The player must be moving at this speed to not reset the loop counter.
	/// </summary>
	private float LoopMaintainSpeedThreshold { get; set; } = 270f;

	/// <summary>
	/// Our current loop. Will get reset when re-entering the start zone or when the player is in the start zone and below
	/// the loop maintain speed threshold.
	/// </summary>
	public int CurrentLoop { get; private set; } = 1;

	/// <summary>
	/// This represents the last loop we were in before exiting the start zone. If we're not in the start zone, this will
	/// be the same as CurrentLoop.
	/// </summary>
	public int LastOrCurrentLoop { get; private set; } = 1;
	public bool InStartZone { get; private set; }
	public bool BlockNextReset { get; private set; }

	/// <summary>
	/// Invoked when the timer starts -- when we exit the start zone.
	/// </summary>
	public Action OnTimerStart;

	/// <summary>
	/// Invoked when the timer restarts -- when we re-enter the start zone.
	/// </summary>
	public Action OnTimerRestart;

	/// <summary>
	/// Invoked when the timer ends -- when we enter the end zone.
	/// </summary>
	public Action<float, bool, int> OnTimerEnd;

	protected override void OnStart()
	{
		_ = GetStats();
	}

	protected override void OnFixedUpdate()
	{
		if ( InStartZone )
		{
			if ( Player.HorzVelocity.LengthSquared < LoopMaintainSpeedThreshold * LoopMaintainSpeedThreshold )
			{
				CurrentLoop = 1;
			}

			LeaderboardManager.Instance.StartPolling( this );

			return;
		}

		Time += Scene.FixedDelta;
		Ticks++;

		UpdateStats();
	}

	public void ResetTimer()
	{
		if ( BlockNextReset )
		{
			BlockNextReset = false;
			return;
		}

		Time = 0;
		Ticks = 0;
		CurrentLoop = 1;
		LastOrCurrentLoop = 1;
		InStartZone = true;

		OnTimerRestart?.Invoke();
	}

	public void StartTimer()
	{
		Time = 0;
		Ticks = 0;
		TopSpeed = 0;
		LastOrCurrentLoop = CurrentLoop;

		InStartZone = false;
		OnTimerStart?.Invoke();
	}

	public void EndTimer()
	{
		InStartZone = true;
		BlockNextReset = true;
		bool newBestTime = UpdateBestTime( Ticks );
		NumCompletionsThisSession++;
		LastOrCurrentLoop = CurrentLoop;
		CurrentLoop++;
		OnTimerEnd?.Invoke( Time, newBestTime, CurrentLoop - 1 );
	}

	public override string ToString()
	{
		return Common.TimeToString( Time );
	}

	public string BestTimeString( int loop )
	{
		int time = loop > 1 ? BestLoopTime ?? 0 : BestFirstLoopTime ?? 0;
		return time == 0 ? "N/A" : Common.TimeToString( time * Scene.FixedDelta );
	}
}