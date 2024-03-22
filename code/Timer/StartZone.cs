namespace Gauntlet;

[Icon( "play_circle" )]
public partial class StartZone : BaseZone
{
  public override void OnTriggerEnter( Collider other )
  {
    if ( !other.Components.TryGet<Timer>( out var timer ) ) return;

    timer.ResetTimer();
  }

  public override void OnTriggerExit( Collider other )
  {
    if ( !other.Components.TryGet<Timer>( out var timer ) ) return;

    timer.StartTimer();
  }
}