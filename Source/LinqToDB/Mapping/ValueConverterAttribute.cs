using System;
using LinqToDB.Common;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
	public class ValueConverterAttribute : Attribute
	{
		/// <summary>
		/// ValueConverter for mapping Database Values to Model values.
		/// </summary>
		public IValueConverter? ValueConverter { get; set; }

		/// <summary>
		/// Returns <see cref="IValueConverter"/> for specific column.
		/// </summary>
		public virtual IValueConverter? GetValueConverter(ColumnDescriptor columnDescriptor)
		{
			if (ValueConverter != null)
				return ValueConverter;

			if (ConverterType == null)
				return null;

			var dynamicConverter = (IValueConverter)TypeAccessor.GetAccessor(ConverterType).CreateInstance();
			return dynamicConverter;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string? Configuration { get; set; }

		/// <summary>
		/// Gets or sets converter type. ConverterType should implement <see cref="IValueConverter"/> interface, should have public constructor with no parameters.
		/// </summary>
		public Type? ConverterType { get; set; }
	}
}
