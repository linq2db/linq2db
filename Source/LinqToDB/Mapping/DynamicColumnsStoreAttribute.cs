using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Marks target member as dynamic columns store.
	/// </summary>
	/// <seealso cref="System.Attribute" />
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DynamicColumnsStoreAttribute : Attribute
    {
	    /// <summary>
	    /// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
	    /// <see cref="ProviderName"/> for standard names.
	    /// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
	    /// </summary>
	    public string Configuration { get; set; }
	}
}
