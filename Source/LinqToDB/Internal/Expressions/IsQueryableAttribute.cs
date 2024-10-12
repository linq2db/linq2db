using System;

namespace LinqToDB.Internal.Expressions
{
	/// <summary>
	/// Used to define queryable members.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class IsQueryableAttribute : Attribute
	{
	}
}
