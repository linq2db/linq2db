using System;

namespace LinqToDB.Linq
{
	[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
	public sealed class SqlEnumAttribute : Attribute
	{
	}
}
