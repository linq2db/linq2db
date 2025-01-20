using System;
using System.Collections.Generic;
using System.Linq;

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

	/// <summary>
	/// For internal use.
	/// </summary>
	public class Class<T> : TypeBase, IClass
		where T : IClass
	{
		public string?            BaseClass        { get; set; }
		public bool               IsStatic         { get; set; }
		public List<string>       GenericArguments { get; set; } = [];
		public List<string>       Interfaces       { get; set; } = [];
		public List<IClassMember> Members          { get; set; } = [];
		public bool               IsInterface      { get; set; }

		public Class()
		{
		}

		string?               _classKeyword;
		public override string ClassKeyword
		{
			get => _classKeyword ?? (IsInterface ? "interface" : "class");
			set => _classKeyword = value;
		}

		public Class(string name, params IClassMember[] members)
		{
			Name = name;
			Members.AddRange(members);
		}

		public override void Render(ModelGenerator tt)
		{
			BeginConditional(tt);

			foreach (var c in Comment)
				tt.WriteLine("//" + c);

			if (Attributes.Count > 0)
			{
				var aa = Attributes.Where(a => !a.IsSeparated).ToList();

				if (aa.Count > 0)
				{
					tt.Write("[");

					for (var i = 0; i < aa.Count; i++)
					{
						if (i > 0) tt.SkipSpacesAndInsert(", ");
						aa[i].Render(tt);
					}

					tt.WriteLine("]");
				}

				aa = Attributes.Where(a => a.IsSeparated).ToList();

				foreach (var a in aa)
				{
					tt.Write("[");
					a.Render(tt);
					tt.WriteLine("]");
				}
			}

			tt.WriteBeginClass(this);
			tt.PushIndent("\t");

			foreach (var cm in Members)
			{
				if (cm is MemberBase m)
				{
					if (m is not IMemberGroup)
						m.BeginConditional(tt, false);

					foreach (var c in m.Comment)
						tt.WriteComment(c);

					if (m.Attributes.Count > 0)
					{
						var q =
							from a in m.Attributes
							group a by a.Conditional ?? "";

						foreach (var g in q)
						{
							if (g.Key.Length > 0)
							{
								tt.RemoveSpace();
								tt.WriteLine("#if " + g.Key);
							}

							var attrs = g.ToList();

							tt.Write("[");

							for (var i = 0; i < attrs.Count; i++)
							{
								if (i > 0) tt.SkipSpacesAndInsert(", ");
								attrs[i].Render(tt);
							}

							tt.WriteLine("]");

							if (g.Key.Length > 0)
							{
								tt.RemoveSpace();
								tt.WriteLine("#endif");
							}
						}
					}

					m.Render(tt, false);
					if (m.InsertBlankLineAfter)
						tt.WriteLine("");

					if (m is not IMemberGroup)
						m.EndConditional(tt, false);
				}
				else if (cm is TypeBase t)
				{
					t.Render(tt);
					tt.WriteLine("");
				}
			}

			tt.Trim();

			tt.PopIndent();
			tt.WriteEndClass();

			EndConditional(tt);
		}

		public override IEnumerable<ITree> GetNodes()
		{
			return Members;
		}

		public override void SetTree()
		{
			foreach (var ch in GetNodes())
			{
				ch.Parent = this;
				ch.SetTree();
			}
		}
	}
}
