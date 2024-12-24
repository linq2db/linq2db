using System;

namespace LinqToDB.Internals.Expressions.Types
{
	[AttributeUsage(AttributeTargets.ReturnValue)]
	public class CustomMapperAttribute : Attribute
	{
		public CustomMapperAttribute(Type mapper)
		{
			Mapper = mapper;
		}

		internal Type Mapper { get; }
	}
}
