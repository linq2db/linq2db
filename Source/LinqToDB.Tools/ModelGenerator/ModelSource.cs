using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGenerator
{
	public interface IModelSource : ITree
	{
		public List<string> Usings { get; }

		void Render(CodeTemplateGenerator tt);
	}

	public abstract class ModelSource<TModel,TNamespace> : IModelSource
	where TModel     : ModelSource<TModel,TNamespace>
	where TNamespace : Namespace<TNamespace>, new()
	{
		public int CurrentNamespace;

		public List<string> Usings { get; set; } = [ "System" ];

		public List<TNamespace> Namespaces = [ new TNamespace() ];

		public TNamespace     Namespace => Namespaces[CurrentNamespace];
		public List<TypeBase> Types     => Namespaces[CurrentNamespace].Types;

		public virtual void Render(CodeTemplateGenerator tt)
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
