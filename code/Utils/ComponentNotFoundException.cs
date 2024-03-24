namespace Gauntlet.Utils;

public partial class ComponentNotFoundException : Exception
{
	private string CustomMessage { get; set; } = null;

	public override string Message => string.IsNullOrEmpty( CustomMessage ) ? "Couldn't find a component." : CustomMessage;

	public ComponentNotFoundException( string message = null )
	{
		CustomMessage = message;
	}
}
