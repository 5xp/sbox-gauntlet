namespace Tf;

[Icon( "stop_circle" )]
public partial class EndZone : BaseZone
{
  public override void OnTriggerEnter( Collider other )
  {
    if ( !other.Components.TryGet<Timer>( out var timer ) ) return;

    timer.EndTimer();
  }

  public override void OnTriggerExit( Collider other )
  {
    // Do nothing
  }
}