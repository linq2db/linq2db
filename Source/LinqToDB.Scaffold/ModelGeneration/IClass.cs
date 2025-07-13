using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IClass : ITypeBase
	{
		string?            BaseClass        { get; set; }
		bool               IsStatic         { get; }
		bool               IsInterface      { get; set; }
		List<string>       GenericArguments { get; }
		List<string>       Interfaces       { get; }
		List<IClassMember> Members          { get; }
	}
}
