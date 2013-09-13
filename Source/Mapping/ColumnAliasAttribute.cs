using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Associates a property with another column property in a class.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property| AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true, Inherited = true)]
	public class ColumnAliasAttribute : Attribute
	{
		public ColumnAliasAttribute()
		{
		}

		public ColumnAliasAttribute(string memberName) : this()
		{
			MemberName = memberName;
		}

		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets the name of an associated member name.
		/// </summary>
		public string MemberName { get; set; }
	}
}
