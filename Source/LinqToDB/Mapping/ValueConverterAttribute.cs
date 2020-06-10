using System;
using LinqToDB.Common;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
	public class ValueConverterAttribute : Attribute
	{
		public IValueConverter? ValueConverter { get; set; }
		
		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string? Configuration { get; set; }
	}
}
