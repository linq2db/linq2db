using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Overrides default scalar detection for target class or structure.
	/// By default linq2db treats primitives and structs as scalar types.
	/// This attribute allows you to mark class or struct as scalar type or mark struct as non-scalar type.
	/// Also see <seealso cref="LinqToDB.Common.Configuration.IsStructIsScalarType"/>.
	/// Note that if you marks some type as scalar, you will need to define custom mapping logic between object of
	/// that type and data parameter using <seealso cref="MappingSchema.SetConvertExpression()"/> methods.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
	public class ScalarTypeAttribute : Attribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		public ScalarTypeAttribute()
		{
			IsScalar = true;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="isScalar">Should target type be treated as scalar type or not.</param>
		public ScalarTypeAttribute(bool isScalar)
		{
			IsScalar      = isScalar;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name. See <see cref="Configuration"/>.</param>
		public ScalarTypeAttribute(string configuration)
		{
			Configuration = configuration;
			IsScalar      = true;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name. See <see cref="Configuration"/>.</param>
		/// <param name="isScalar">Should target type be treated as scalar type or not.</param>
		public ScalarTypeAttribute(string configuration, bool isScalar)
		{
			Configuration = configuration;
			IsScalar      = isScalar;
		}

		/// <summary>
		/// Mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets scalar type flag.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool   IsScalar      { get; set; }
	}
}
