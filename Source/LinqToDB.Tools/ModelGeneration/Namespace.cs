using System;
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

	/// <summary>
	/// For internal use.
	/// </summary>
	public class Namespace<T> : INamespace
		where T : Namespace<T>
	{
		public string?         Name   { get; set; }
		public List<ITypeBase> Types  { get; set; } = [];
		public HashSet<string> Usings { get; set; } = [];

		public virtual void Render(ModelGenerator tt)
		{
			if (!string.IsNullOrEmpty(Name))
			{
				tt.WriteBeginNamespace(Name!);
				tt.PushIndent("\t");
			}

			tt.RenderUsings(Usings);

			foreach (var t in Types)
			{
				t.Render(tt);
				tt.WriteLine("");
			}

			tt.Trim();

			if (!string.IsNullOrEmpty(Name))
			{
				tt.PopIndent();
				tt.WriteEndNamespace();
			}
		}

		public ITree?             Parent     { get; set; }
		public IEnumerable<ITree> GetNodes() { return Types; }

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
