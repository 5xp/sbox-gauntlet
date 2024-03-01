namespace Tf;

public partial class MoveMechanic : BasePlayerControllerMechanic
{
  public override bool ShouldBecomeActive() => true;

  public override int Priority => 9;

  public override void OnActiveUpdate()
  {
    if ( Controller.IsGrounded )
    {
      WalkMechanic walk = Controller.GetMechanic<WalkMechanic>();
      walk.WalkMove();
      Controller.CategorizePosition( true );
    }
    else if ( Controller.GetMechanic<WallrunMechanic>() is WallrunMechanic wallrun && wallrun.WallNormal.HasValue )
    {
      wallrun.WallrunMove();
      wallrun.CategorizePosition();
    }
    else
    {
      AirMoveMechanic airMove = Controller.GetMechanic<AirMoveMechanic>();
      airMove.AirMove();
      Controller.CategorizePosition( Controller.IsGrounded );
    }
  }
}