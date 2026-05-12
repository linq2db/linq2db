using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.DuckDB
{
	/// <summary>
	/// DuckDB data provider configuration options.
	/// </summary>
	/// <param name="BulkCopyType">
	/// Default bulk copy mode for DuckDB.
	/// Default value: <c><see cref="BulkCopyType.ProviderSpecific"/></c>.
	/// Uses native DuckDB Appender for best performance with automatic fallback to MultipleRows
	/// when the table has unmapped columns or identity columns with nextval() defaults.
	/// </param>
	public sealed record DuckDBOptions(
		BulkCopyType BulkCopyType = BulkCopyType.ProviderSpecific
	) : DataProviderOptions<DuckDBOptions>(BulkCopyType)
	{
		public DuckDBOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		private DuckDBOptions(DuckDBOptions original) : base(original)
		{
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder;

		#region IEquatable implementation

		public bool Equals(DuckDBOptions? other)
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
