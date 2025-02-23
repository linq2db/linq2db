using System;

using LinqToDB.Internal.Common;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Defines to which type linq2db should map record based on discriminator value. You can apply this attribute to
	/// a base class or insterface, implemented by all child classes.
	/// Don't forget to define discriminator value storage column using <see cref="ColumnAttribute.IsDiscriminator"/>.
	/// </summary>
	/// <remarks>
	/// You cannot configure inheritance mapping using this attribute for discriminator types, not supported by .NET
	/// attributes. See <a href="https://github.com/dotnet/csharplang/blob/master/spec/attributes.md#attribute-parameter-types">document</a>
	/// for a list of supported types.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=true)]
	public class InheritanceMappingAttribute : MappingAttribute
	{
		/// <summary>
		/// Gets or sets discriminator value.
		/// </summary>
		public object? Code          { get; set; }

		/// <summary>
		/// Get or sets flag, that tells linq2db that current mapping should be used by default if suitable mapping type not found.
		/// </summary>
		public bool   IsDefault     { get; set; }

		/// <summary>
		/// Gets or sets type, to which record with current discriminator value should be mapped.
		/// </summary>
		public Type   Type          { get; set; } = null!;

		public override string GetObjectID()
		{
			var type = IdentifierBuilder.GetObjectID(Type);
			return $".{Configuration}.{Code}.{(IsDefault?'1':'0')}.{type}.";
		}
	}
}
