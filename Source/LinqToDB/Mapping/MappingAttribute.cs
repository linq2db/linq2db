using System;

namespace LinqToDB.Mapping
{
	public abstract class MappingAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string? Configuration { get; set; }

		/// <summary>
		/// Returns mapping attribute id, based on all attribute options.
		/// </summary>
		public abstract string GetObjectID();
	}
}
