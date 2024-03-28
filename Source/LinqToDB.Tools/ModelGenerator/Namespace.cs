using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGenerator
{
	public class Namespace<T> : ITree
	where T : Namespace<T>
	{
		public string?        Name;
		public List<TypeBase> Types  = [];
		public List<string>   Usings = [];

		public virtual void Render(CodeTemplateGenerator tt)
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
