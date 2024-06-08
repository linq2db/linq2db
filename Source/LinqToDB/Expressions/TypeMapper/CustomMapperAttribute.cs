using System;

namespace LinqToDB.Expressions
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
