using System;

namespace LinqToDB.Internal.Expressions.Types
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
