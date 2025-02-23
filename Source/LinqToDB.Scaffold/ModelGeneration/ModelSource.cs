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

	/// <summary>
	/// For internal use.
	/// </summary>
	public abstract class ModelSource<TModel,TNamespace> : IModelSource
	where TModel     : ModelSource<TModel,TNamespace>
	where TNamespace : Namespace<TNamespace>, new()
	{
		public int CurrentNamespace;

		public HashSet<string>  Usings { get; set; } = [ "System" ];

		public List<INamespace> Namespaces { get; } = [ new TNamespace() ];

		public INamespace       Namespace => Namespaces[CurrentNamespace];
		public List<ITypeBase>  Types     => Namespaces[CurrentNamespace].Types;

		public virtual void Render(ModelGenerator tt)
		{
			tt.RenderUsings(Usings);
			tt.WriteLine("");

			foreach (var nm in Namespaces)
			{
				nm.Render(tt);
				tt.WriteLine("");
			}

			tt.Trim();
		}

		public ITree?             Parent     { get; set; }
		public IEnumerable<ITree> GetNodes() { return Namespaces; }

		public void SetTree()
		{
			foreach (var ch in GetNodes())
			{
				ch.Parent = this;
				ch.SetTree();
			}
		}
	}
}
