using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGenerator
{
	public interface ITypeBase
	{
		AccessModifier   AccessModifier { get; }
		string?          Name           { get; }
		bool             IsPartial      { get; }
		List<string>     Comment        { get; }
		List<IAttribute> Attributes     { get; }
		string?          Conditional    { get; }
		string           ClassKeyword   { get; }
	}

	public abstract class TypeBase : IClassMember
	{
		public AccessModifier   AccessModifier { get; set; } = AccessModifier.Public;
		public string?          Name           { get; set; }
		public bool             IsPartial      { get; set; } = true;
		public List<string>     Comment        { get; set; } = [];
		public List<IAttribute> Attributes     { get; set; } = [];
		public string?          Conditional    { get; set; }
		public string           ClassKeyword   { get; set; } = "class";

		public abstract void Render(CodeTemplateGenerator tt);

		protected virtual void BeginConditional(CodeTemplateGenerator tt)
		{
			if (Conditional != null)
			{
				tt.RemoveSpace();
				tt.WriteLine("#if " + Conditional);
				tt.WriteLine("");
			}
		}

		protected virtual void EndConditional(CodeTemplateGenerator tt)
		{
			if (Conditional != null)
			{
				tt.RemoveSpace();
				tt.WriteLine("");
				tt.RemoveSpace();
				tt.WriteLine("#endif");
			}
		}

		public          ITree?             Parent { get; set; }
		public abstract IEnumerable<ITree> GetNodes();
		public abstract void               SetTree ();
	}
}
