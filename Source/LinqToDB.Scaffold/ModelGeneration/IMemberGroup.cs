using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IMemberGroup : IMemberBase
	{
		bool               IsCompact       { get; set; }
		bool               IsPropertyGroup { get; set; }
		List<IClassMember> Members         { get; set; }
	}
}
