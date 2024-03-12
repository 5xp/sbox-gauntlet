namespace Tf;

public partial class PlayerController
{
  [Property, Category( "Sounds" )] public SoundEvent FootStepSoundWalk { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent FootStepSoundRun { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent FootStepSoundWallrun { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent JumpSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent AirJumpSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent SlideSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent SlideBoostSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent SlideEndSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent WallrunStartSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent WallrunImpactSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent LandSound { get; set; }
  [Property, Category( "Sounds" )] public SoundEvent HardLandSound { get; set; }
}