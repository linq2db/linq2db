using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGenerator
{
	public interface IAttribute
	{
		string?      Name        { get; }
		List<string> Parameters  { get; }
		string?      Conditional { get; }
		bool         IsSeparated { get; }

		void Render(CodeTemplateGenerator tt);
	}

	public class Attribute<T> : IAttribute
		where T : Attribute<T>
	{
		public string?      Name        { get; set; }
		public List<string> Parameters  { get; set; } = [];
		public string?      Conditional { get; set; }
		public bool         IsSeparated { get; set; }

		public Attribute()
		{
		}

		public Attribute(string name, params string[] ps)
		{
			Name = name;
			Parameters.AddRange(ps);
		}

		public virtual void Render(CodeTemplateGenerator tt)
		{
			tt.WriteAttribute(this);
		}
	}
}
