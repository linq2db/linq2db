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
	/// Default value: <c><see cref="BulkCopyType.MultipleRows"/></c>.
	/// </param>
	public sealed record DuckDBOptions(
		BulkCopyType BulkCopyType = BulkCopyType.MultipleRows
	) : DataProviderOptions<DuckDBOptions>(BulkCopyType)
	{
		public DuckDBOptions() : this(BulkCopyType.MultipleRows)
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
