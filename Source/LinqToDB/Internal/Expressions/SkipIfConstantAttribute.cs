using System;

namespace LinqToDB.Internal.Expressions
{
	/// <summary>
	/// Used to tell query expression comparer to skip method call argument comparison if it is constant.
	/// Method parameter parameterization should be also implemented in method builder.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class SkipIfConstantAttribute : Attribute
	{
	}
}
