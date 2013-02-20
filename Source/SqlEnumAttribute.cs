using System;

namespace LinqToDB
{
	[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
	public sealed class SqlEnumAttribute : Attribute
	{
	}
}
