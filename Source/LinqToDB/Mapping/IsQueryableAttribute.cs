using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Used to define queryable members.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class IsQueryableAttribute : Attribute
	{
	}
}
