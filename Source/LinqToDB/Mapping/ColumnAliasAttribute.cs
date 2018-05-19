using System;

namespace LinqToDB.Mapping
{
	// TODO: V2 - why it allows Class and Interface as target?
	// TODO: right now we can reference other aliases and create a loop, that will lead to stack overflow exception
	// We should detect loops or disalow aliases to aliases.
	/// <summary>
	/// Specifies that current field or property is just an alias to another property or field.
	/// Currently this attribute has several issues:
	/// - you can apply it to class or interface - such attribute will be ignored by linq2db;
	/// - it is possible to define attribute without setting  <see cref="MemberName"/> value;
	/// - you can define alias to another alias property or field and potentially create loop.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Field | AttributeTargets.Property| AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple = true, Inherited = true)]
	public class ColumnAliasAttribute : Attribute
	{
		/// <summary>
		/// Use <see cref="ColumnAliasAttribute.ColumnAliasAttribute(string)"/> constructor or specify <see cref="MemberName"/> value.
		/// </summary>
		public ColumnAliasAttribute()
		{
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="memberName">Name of target property or field.</param>
		public ColumnAliasAttribute(string memberName) : this()
		{
			MemberName = memberName;
		}

		/// <summary>
		/// Mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets the name of target property or field.
		/// </summary>
		public string MemberName { get; set; }
	}
}
