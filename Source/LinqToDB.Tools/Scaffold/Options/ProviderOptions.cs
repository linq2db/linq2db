using System;
using System.Collections.Generic;

using LinqToDB.Configuration;

namespace LinqToDB.Scaffold
{
	/// <summary>
	/// General code-generation options, not related to data model directly.
	/// </summary>
	public sealed class ProviderOptions
	{
		internal ProviderOptions() { }

		/// <summary>
		/// Provider Name.
		/// <list type="bullet">
		/// <item>Unique name of Provider</item>
		/// </list>
		/// </summary>
		public string ProviderName { get; set; } = null!;
		public string? ProviderLocation { get; set; }
		public string? ProviderDetectorClass { get; set; }
		public string? ProviderDetectorMethod { get; set; }


	}
}
