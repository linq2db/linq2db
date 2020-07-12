using System;

namespace LinqToDB.Expressions
{
	// could allow more targets later if needed
	[AttributeUsage(AttributeTargets.Method)]
	internal class TypeWrapperNameAttribute : Attribute
	{
		public TypeWrapperNameAttribute(string name)
		{
			Name = name;
		}

		public string Name { get; }
	}
}
