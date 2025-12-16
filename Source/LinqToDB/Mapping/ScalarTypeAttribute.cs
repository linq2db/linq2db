using System;
using System.Globalization;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Overrides default scalar detection for target class or structure.
	/// By default linq2db treats primitives and structs as scalar types.
	/// This attribute allows you to mark class or struct as scalar type or mark struct as non-scalar type.
	/// Also see <seealso cref="Common.Configuration.IsStructIsScalarType"/>.
	/// Note that if you marks some type as scalar, you will need to define custom mapping logic between object of
	/// that type and data parameter using <seealso cref="MappingSchema.SetConvertExpression(Type, Type, System.Linq.Expressions.LambdaExpression, bool, ConversionType)"/> methods.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = true)]
	public class ScalarTypeAttribute : MappingAttribute
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
		/// Gets or sets scalar type flag.
		/// Default value: <see langword="true"/>.
		/// </summary>
		public bool   IsScalar      { get; set; }

		public override string GetObjectID()
		{
			return string.Create(CultureInfo.InvariantCulture, $".{Configuration}.{(IsScalar ? 1 : 0)}.");
		}
	}
}
