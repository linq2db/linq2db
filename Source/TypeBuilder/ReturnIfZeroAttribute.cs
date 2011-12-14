using System;

namespace LinqToDB.TypeBuilder
{
	[AttributeUsage(AttributeTargets.ReturnValue)]
	public class ReturnIfZeroAttribute : Attribute
	{
	}
}
