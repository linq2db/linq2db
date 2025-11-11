using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.ClickHouse
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode.
	/// Default value: <c><see cref="BulkCopyType.ProviderSpecific"/></c>.
	/// </param>
	/// <param name="UseStandardCompatibleAggregates">
	/// Enables -OrNull combinator for Min, Max, Sum and Avg aggregation functions to support SQL standard-compatible behavior.
	/// Default value: <c>false</c>.
	/// </param>
	/// <param name="HttpClient">
	/// Optional, specify the HttpClient instance to use for making requests.
	/// </param>
	public sealed record ClickHouseOptions
	(
		BulkCopyType BulkCopyType                    = BulkCopyType.ProviderSpecific,
		bool         UseStandardCompatibleAggregates = default,
		object?      HttpClient                      = null
		// If you add another parameter here, don't forget to update
		// ClickHouseOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<ClickHouseOptions>(BulkCopyType)
	{
		public ClickHouseOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		ClickHouseOptions(ClickHouseOptions original) : base(original)
		{
			UseStandardCompatibleAggregates = original.UseStandardCompatibleAggregates;
			HttpClient                      = original.HttpClient;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(UseStandardCompatibleAggregates)
			.Add(HttpClient)
			;

		#region IEquatable implementation

		public bool Equals(ClickHouseOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
