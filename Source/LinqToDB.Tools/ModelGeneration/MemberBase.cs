using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IMemberBase : IClassMember
	{
		string?          ID                   { get; set; }
		AccessModifier   AccessModifier       { get; set; }
		string?          Name                 { get; set; }
		Func<string?>?   TypeBuilder          { get; set; }
		List<string>     Comment              { get; set; }
		string?          EndLineComment       { get; set; }
		List<IAttribute> Attributes           { get; set; }
		bool             InsertBlankLineAfter { get; set; }
		string?          Conditional          { get; set; }

		int AccessModifierLen { get; set; }
		int ModifierLen       { get; set; }
		int TypeLen           { get; set; }
		int NameLen           { get; set; }
		int ParamLen          { get; set; }
		int BodyLen           { get; set; }

		public string? Type { get; set; }

		string? BuildType();
	}

	/// <summary>
	/// For internal use.
	/// </summary>
	public abstract class MemberBase : IMemberBase
	{
		public string?          ID                   { get; set; }
		public AccessModifier   AccessModifier       { get; set; } = AccessModifier.Public;
		public string?          Name                 { get; set; }
		public Func<string?>?   TypeBuilder          { get; set; }
		public List<string>     Comment              { get; set; } = [];
		public string?          EndLineComment       { get; set; }
		public List<IAttribute> Attributes           { get; set; } = [];
		public bool             InsertBlankLineAfter { get; set; } = true;
		public string?          Conditional          { get; set; }

		public int AccessModifierLen { get; set; }
		public int ModifierLen       { get; set; }
		public int TypeLen           { get; set; }
		public int NameLen           { get; set; }
		public int ParamLen          { get; set; }
		public int BodyLen           { get; set; }

		public string? Type
		{
			get => TypeBuilder?.Invoke();
			set => TypeBuilder = () => value;
		}

		public string? BuildType() { return TypeBuilder?.Invoke(); }

		public virtual  int  CalcModifierLen() { return 0; }
		public abstract int  CalcBodyLen    ();
		public virtual  int  CalcParamLen   () { return 0; }
		public abstract void Render         (ModelGenerator tt, bool isCompact);

		public virtual void BeginConditional(ModelGenerator tt, bool isCompact)
		{
			if (Conditional != null)
			{
				tt.RemoveSpace();
				tt.WriteLine($"#if {Conditional}");
				if (!isCompact)
					tt.WriteLine("");
			}
		}

		public virtual void EndConditional(ModelGenerator tt, bool isCompact)
		{
			if (Conditional != null)
			{
				if (!isCompact)
				{
					tt.Trim();
					tt.WriteLine("");
				}

				tt.RemoveSpace();
				tt.WriteLine("#endif");

				if (!isCompact)
					tt.WriteLine("");
			}
		}

		public         ITree?             Parent     { get; set; }
		public virtual IEnumerable<ITree> GetNodes() { return Enumerable.Empty<ITree>(); }
		public virtual void               SetTree () {}
	}
}
