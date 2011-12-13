using System;

namespace LinqToDB.TypeBuilder
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class NoInstanceAttribute : Attribute
	{
	}
}
