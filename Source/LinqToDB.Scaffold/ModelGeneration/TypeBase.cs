using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface ITypeBase : IClassMember
	{
		AccessModifier   AccessModifier { get; set; }
		string?          Name           { get; set; }
		bool             IsPartial      { get; set; }
		List<string>     Comment        { get; set; }
		List<IAttribute> Attributes     { get; set; }
		string?          Conditional    { get; set; }
		string           ClassKeyword   { get; set; }

		void Render(ModelGenerator tt);
	}

	public record NameChangedArgs(string OldName, string? NewName);

	/// <summary>
	/// For internal use.
	/// </summary>
	public abstract class TypeBase : ITypeBase
	{
		public AccessModifier   AccessModifier { get; set; } = AccessModifier.Public;
		public bool             IsPartial      { get; set; } = true;
		public List<string>     Comment        { get; set; } = [];
		public List<IAttribute> Attributes     { get; set; } = [];
		public string?          Conditional    { get; set; }
		public virtual string   ClassKeyword   { get; set; } = "class";

		private string? _name;
		public  string?  Name
		{
			get => _name;
			set
			{
				var oldName = _name;

				_name = value;

				if (oldName != null && oldName != _name)
					OnNameChanged?.Invoke(this, new (oldName, _name));
			}
		}

		public delegate void               OnNameChangedHandler(object sender, NameChangedArgs e);
		public event OnNameChangedHandler? OnNameChanged;

		public abstract void Render(ModelGenerator     tt);

		protected virtual void BeginConditional(ModelGenerator tt)
		{
			if (Conditional != null)
			{
				tt.RemoveSpace();
				tt.WriteLine($"#if {Conditional}");
				tt.WriteLine("");
			}
		}

		protected virtual void EndConditional(ModelGenerator tt)
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
