using System;

namespace LinqToDB.Data.Linq
{
	[AttributeUsageAttribute(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
	public sealed class SqlEnumAttribute : Attribute
	{
	}
}
