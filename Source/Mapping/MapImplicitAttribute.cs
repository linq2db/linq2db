using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public abstract class MapImplicitAttribute : Attribute
	{
	}
}
