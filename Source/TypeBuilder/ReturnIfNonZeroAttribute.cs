using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.TypeBuilder
{
	[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
	[AttributeUsage(AttributeTargets.ReturnValue)]
	public class ReturnIfNonZeroAttribute : Attribute
	{
	}
}
