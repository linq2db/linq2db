using System;

namespace LinqToDB.Expressions
{
	[AttributeUsage(AttributeTargets.ReturnValue)]
	internal class CustomMapperAttribute : Attribute
	{
		public CustomMapperAttribute(Type mapper)
		{
			Mapper = mapper;
		}

		public Type Mapper { get; }
	}
}
