using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Specifies value generation sequence for mapped property of field.
	/// Currently it supported only for:
	/// - Firebird generators;
	/// - Oracle sequences.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class SequenceNameAttribute : Attribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="configuration">Mapping schema configuration name. See <see cref="Configuration"/>.</param>
		/// <param name="sequenceName">Sequence generator name.</param>
		public SequenceNameAttribute(string configuration, string sequenceName)
		{
			Configuration = configuration;
			SequenceName  = sequenceName;
		}

		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		/// <param name="sequenceName">Sequence generator name.</param>
		public SequenceNameAttribute(string sequenceName)
		{
			SequenceName = sequenceName;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string Configuration { get; set; }

		/// <summary>
		/// Gets or sets sequence generator name.
		/// </summary>
		public string SequenceName  { get; set; }
	}
}
