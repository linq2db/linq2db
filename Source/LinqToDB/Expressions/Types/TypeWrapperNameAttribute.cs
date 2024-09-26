using System;

namespace LinqToDB.Expressions.Types
{
	// could allow more targets later if needed
	[AttributeUsage(AttributeTargets.Method)]
	public class TypeWrapperNameAttribute : Attribute
	{
		public TypeWrapperNameAttribute(string name)
		{
			Name = name;
		}

		internal string Name { get; }
	}
}
