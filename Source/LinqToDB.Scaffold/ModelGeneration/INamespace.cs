using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface INamespace : ITree
	{
		string?         Name   { get; set; }
		List<ITypeBase> Types  { get; set; }
		HashSet<string> Usings { get; set; }

		void Render(ModelGenerator tt);
	}
}
