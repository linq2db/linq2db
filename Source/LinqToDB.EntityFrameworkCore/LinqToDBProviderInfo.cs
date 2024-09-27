namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// Stores LINQ To DB database provider information.
	/// </summary>
	public sealed class LinqToDBProviderInfo
	{
		/// <summary>
		/// Server version. Currently is not used.
		/// </summary>
		public string? Version      { get; set; }

		/// <summary>
		/// Gets or sets LINQ To DB provider name.
		/// <see cref="LinqToDB.ProviderName"/> for available providers.
		/// </summary>
		public string? ProviderName { get; set; }

		/// <summary>
		/// Replaces <c>null</c> values in current instance with values from parameter.
		/// </summary>
		/// <param name="providerInfo">Provider information to merge into current object.</param>
		public void Merge(LinqToDBProviderInfo? providerInfo)
		{
			if (providerInfo != null)
			{
				Version      ??= providerInfo.Version;
				ProviderName ??= providerInfo.ProviderName;
			}
		}
	}
}
