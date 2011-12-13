using System;

namespace LinqToDB.TypeBuilder
{
	///<summary>
	/// Indicates that a field, property or method can be treated as a value setter.
	///</summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public sealed class SetValueAttribute : Attribute
	{
	}
}
