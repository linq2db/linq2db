using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// <para>
	/// Defines bidirectional mapping between enum field value, used on client and database value, stored in database and
	/// used in queries.
	/// Enumeration field could have multiple <see cref="MapValueAttribute"/> attributes.
	/// </para>
	/// <para>
	/// Mapping from database value to enumeration performed when you load data from database. Linq2db will search for
	/// enumeration field with <see cref="MapValueAttribute"/> with required value. If attribute with such value is not
	/// found, you will receive <see cref="LinqToDBException"/> error. If you cannot specify all possible values using
	/// <see cref="MapValueAttribute"/>, you can specify custom mapping using methods like
	/// <see cref="MappingSchema.SetConvertExpression{TFrom, TTo}(System.Linq.Expressions.Expression{Func{TFrom, TTo}}, bool)"/>.
	/// </para>
	/// <para>
	/// Mapping from enumeration value performed when you save it to database or use in query. If your enum field has
	/// multiple <see cref="MapValueAttribute"/> attributes, you should mark one of them as default using <see cref="IsDefault"/> property.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple=true)]
	public class MapValueAttribute : Attribute
	{
		/// <summary>
		/// Adds <see cref="MapValueAttribute"/> mapping to enum field. If you don't specify <see cref="Value"/> property,
		/// <code>null</code> value will be used.
		/// </summary>
		public MapValueAttribute()
		{
		}

		/// <summary>
		/// Adds <see cref="MapValueAttribute"/> to enum field.
		/// </summary>
		/// <param name="value">Database value, mapped to current enumeration field.</param>
		public MapValueAttribute(object value)
		{
			Value = value;
		}

		/// <summary>
		/// Adds <see cref="MapValueAttribute"/> to enum field.
		/// </summary>
		/// <param name="configuration">Name of configuration, for which this attribute instance will be used.</param>
		/// <param name="value">Database value, mapped to current enumeration field.</param>
		public MapValueAttribute(string configuration, object value)
		{
			Configuration = configuration;
			Value         = value;
		}

		/// <summary>
		/// Adds <see cref="MapValueAttribute"/> to enum field.
		/// </summary>
		/// <param name="value">Database value, mapped to current enumeration field.</param>
		/// <param name="isDefault">If <code>true</code>, database value from this attribute will be used for mapping
		/// to database value.</param>
		public MapValueAttribute(object value, bool isDefault)
		{
			Value     = value;
			IsDefault = isDefault;
		}

		/// <summary>
		/// Adds <see cref="MapValueAttribute"/> to enum field.
		/// </summary>
		/// <param name="configuration">Name of configuration, for which this attribute instance will be used.</param>
		/// <param name="value">Database value, mapped to current enumeration field.</param>
		/// <param name="isDefault">If <code>true</code>, database value from this attribute will be used for mapping
		/// to database value.</param>
		public MapValueAttribute(string configuration, object value, bool isDefault)
		{
			Configuration = configuration;
			Value         = value;
			IsDefault     = isDefault;
		}

		/// <summary>
		/// Mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Database value, to which current enumeration field will be mapped when used in query or saved to database.
		/// This value, when loaded from database, will be converted to current enumeration field.
		/// </summary>
		public object Value         { get; set; }

		/// <summary>
		/// If <code>true</code>, <see cref="Value"/> property value will be used for conversion from enumeration to
		/// database value.
		/// </summary>
		public bool   IsDefault     { get; set; }
	}
}
