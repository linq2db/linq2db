using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IModelSource : ITree
	{
		HashSet<string>  Usings     { get; }
		List<ITypeBase>  Types      { get; }
		List<INamespace> Namespaces { get; }
		INamespace       Namespace  { get; }

		void Render(ModelGenerator tt);
	}
}
