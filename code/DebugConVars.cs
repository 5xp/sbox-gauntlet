namespace Tf;

public static class DebugConVars
{
  [ConVar( "gauntlet_debug_controller" )]
  public static bool DebugControllerGizmos { get; set; } = false;

  [ConVar( "gauntlet_debug_viewPunch" )]
  public static bool DebugViewPunch { get; set; } = false;

  [ConVar( "gauntlet_debug_wallrun_gizmos" )]
  public static bool DebugWallrunGizmos { get; set; } = false;

  [ConVar( "gauntlet_cheat_wallrun_settings" )]
  public static bool DebugWallrunSettings { get; set; } = false;

  [ConVar( "gauntlet_cheat_override_jump_buffer_ticks" )]
  public static int DebugOverrideJumpBufferTicks { get; set; } = -1;

  public static bool AnyCheatEnabled => DebugWallrunSettings || DebugOverrideJumpBufferTicks > 0;
}