using System.Collections.Immutable;

namespace Gauntlet.Player;

public partial class PlayerController
{
	private ImmutableArray<string> _tags = ImmutableArray.Create<string>();

	/// <summary>
	/// Do we have a tag?
	/// </summary>
	/// <param name="tag"></param>
	/// <returns></returns>
	public bool HasTag( string tag )
	{
		return _tags.Contains( tag );
	}

	/// <summary>
	/// Do we have any tag?
	/// </summary>
	/// <param name="tags"></param>
	/// <returns></returns>
	public bool HasAnyTag( params string[] tags )
	{
		return tags.Any( HasTag );
	}

	/// <summary>
	/// Do we have all tags?
	/// </summary>
	/// <param name="tags"></param>
	/// <returns></returns>
	public bool HasAllTags( params string[] tags )
	{
		return tags.All( HasTag );
	}
}
