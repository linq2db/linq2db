using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Explicitly specifies that associated column could contain <c>NULL</c> values.
	/// Overrides default nullability flag from current mapping schema for property/field type.
	/// Has lower priority over <seealso cref="ColumnAttribute.CanBeNull"/>.
	/// Using this attribute, you can allow <c>NULL</c> values for identity columns.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=true)]
	public class NullableAttribute : Attribute
	{
		/// <summary>
		/// Creates attribute isntance.
		/// </summary>
		public NullableAttribute()
		{
			CanBeNull = true;
		}

		/// <summary>
		/// Creates attribute isntance.
		/// </summary>
		/// <param name="isNullable">Nullability flag for current column.</param>
		public NullableAttribute(bool isNullable)
		{
			CanBeNull = isNullable;
		}

		/// <summary>
		/// Creates attribute isntance.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name. See <see cref="Configuration"/>.</param>
		/// <param name="isNullable">Nullability flag for current column.</param>
		public NullableAttribute(string configuration, bool isNullable)
		{
			Configuration = configuration;
			CanBeNull     = isNullable;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets nullability flag for current column.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool   CanBeNull     { get; set; }
	}
}
