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
	public class NullableAttribute : MappingAttribute
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
		/// Gets or sets nullability flag for current column.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool   CanBeNull     { get; set; }

		public override string GetObjectID()
		{
			return FormattableString.Invariant($".{Configuration}.{(CanBeNull ? 1 : 0)}.");
		}
	}
}
