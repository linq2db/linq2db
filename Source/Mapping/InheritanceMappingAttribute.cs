using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Defines to which type linq2db should map record based on discriminator value. You can apply this attribute to
	/// a base class or insterface, implemented by all child classes.
	/// Don't forget to define discriminator value storage column using <see cref="ColumnAttribute.IsDiscriminator"/>.
	/// </summary>
	/// <remarks>
	/// You cannot configure inheritance mapping using this attribute for discriminator types, not supported by .NET
	/// attributes. See <see cref="https://github.com/dotnet/csharplang/blob/master/spec/attributes.md#attribute-parameter-types"/>
	/// for a list of supported types.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=true)]
	public class InheritanceMappingAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets discriminator value.
		/// </summary>
		public object Code          { get; set; }

		/// <summary>
		/// Get or sets flag, that tells linq2db that current mapping should be used by default if suitable mapping type not found.
		/// </summary>
		public bool   IsDefault     { get; set; }

		/// <summary>
		/// Gets or sets type, to which record with current discriminator value should be mapped.
		/// </summary>
		public Type   Type          { get; set; }
	}
}
