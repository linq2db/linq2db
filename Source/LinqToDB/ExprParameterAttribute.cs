using System;

using JetBrains.Annotations;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	[MeansImplicitUse]
	public class ExprParameterAttribute : Attribute
	{
		public string?           Name              { get; set; }
		public ExprParameterKind ParameterKind     { get; set; }
		public bool              DoNotParameterize { get; set; }

		public ExprParameterAttribute(string name)
		{
			Name = name;
		}

		public ExprParameterAttribute()
		{
		}
	}
}
