using System;

namespace LinqToDB.TypeBuilder
{
	[AttributeUsage(AttributeTargets.ReturnValue)]
	public sealed class ReturnIfFalseAttribute : ReturnIfZeroAttribute
	{
	}
}
