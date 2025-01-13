namespace LinqToDB.Schema
{
	/// <summary>
	/// Implements aggregated <see cref="ITypeMappingProvider"/> provider.
	/// </summary>
	public sealed class AggregateTypeMappingsProvider : ITypeMappingProvider
	{
		private readonly ITypeMappingProvider[] _providers;

		/// <summary>
		/// Creates instance of <see cref="AggregateTypeMappingsProvider"/>.
		/// </summary>
		/// <param name="providers">Aggregated providers.</param>
		public AggregateTypeMappingsProvider(params ITypeMappingProvider[] providers)
		{
			_providers = providers;
		}

		TypeMapping? ITypeMappingProvider.GetTypeMapping(DatabaseType databaseType)
		{
			foreach (var provider in _providers)
			{
				var mapping = provider.GetTypeMapping(databaseType);

				// first successful resolve taken
				if (mapping != null)
					return mapping;
			}

			return null;
		}
	}
}
