using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Marks target member as dynamic columns store.
	/// </summary>
	/// <seealso cref="Attribute" />
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public class DynamicColumnsStoreAttribute : MappingAttribute
	{
		public override string GetObjectID() => Configuration ?? string.Empty;
	}
}
