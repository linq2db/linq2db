using System;
using LinqToDB.Common;
using LinqToDB.Reflection;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
	public class ValueConverterAttribute : Attribute
	{
		private IValueConverter? _valueConverter;

		/// <summary>
		/// ValueConverter for mapping Database Values to Model values.
		/// </summary>
		public IValueConverter? ValueConverter
		{
			get
			{
				if (_valueConverter != null)
					return _valueConverter;

				if (ConverterType == null)
					return null;

				var dynamicConverter = (IValueConverter)TypeAccessor.GetAccessor(ConverterType).CreateInstance();
				return dynamicConverter;
			}

			set => _valueConverter = value;
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
