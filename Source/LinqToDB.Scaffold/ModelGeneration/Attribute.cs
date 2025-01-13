using System;
using System.Collections.Generic;

namespace LinqToDB.Tools.ModelGeneration
{
	/// <summary>
	/// For internal use.
	/// </summary>
	public interface IAttribute
	{
		string?      Name        { get; }
		List<string> Parameters  { get; }
		string?      Conditional { get; }
		bool         IsSeparated { get; }

		void Render(ModelGenerator tt);
	}

	/// <summary>
	/// For internal use.
	/// </summary>
	public class Attribute<T> : IAttribute
		where T : Attribute<T>
	{
		public string?      Name        { get; set; }
		public List<string> Parameters  { get; set; } = [];
		public string?      Conditional { get; set; }
		public bool         IsSeparated { get; set; }
		public object?      Tag         { get; set; }

		public Attribute()
		{
		}

		public Attribute(string name, params string[] ps)
		{
			Name = name;
			Parameters.AddRange(ps);
		}

		public virtual void Render(ModelGenerator tt)
		{
			tt.WriteAttribute(this);
		}
	}
}
