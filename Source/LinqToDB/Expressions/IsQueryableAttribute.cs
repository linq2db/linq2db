using System;

namespace LinqToDB.Expressions
{
	/// <summary>
	/// Used to define queryable members.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class IsQueryableAttribute : Attribute
	{
	}
}
