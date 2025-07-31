using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// YDB data provider configuration options.
	/// </summary>
	/// <param name="BulkCopyType">
	/// Default bulk copy mode for YDB.
	/// Default value: <c><see cref="BulkCopyType.ProviderSpecific"/></c>.
	/// </param>
	/// <param name="UseServerSideUpsert">
	/// Enables server-side UPSERT optimization for YDB.
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="AutoConvertDateTimeToUtc">
	/// Automatically convert DateTime values to UTC when working with YDB timestamps.
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="UseLegacyPagination">
	/// Use legacy OFFSET/LIMIT pagination instead of newer syntax.
	/// Default value: <c>false</c>.
	/// </param>
	public sealed record YdbOptions(
		BulkCopyType BulkCopyType = BulkCopyType.ProviderSpecific,
		bool UseServerSideUpsert = true,
		bool AutoConvertDateTimeToUtc = true,
		bool UseLegacyPagination = false,
		YdbIdentifierQuoteMode IdentifierQuoteMode = YdbIdentifierQuoteMode.Auto
	) : DataProviderOptions<YdbOptions>(BulkCopyType)
	{
		public YdbOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		private YdbOptions(YdbOptions original) : base(original)
		{
			UseServerSideUpsert = original.UseServerSideUpsert;
			AutoConvertDateTimeToUtc = original.AutoConvertDateTimeToUtc;
			UseLegacyPagination = original.UseLegacyPagination;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(UseServerSideUpsert)
			.Add(AutoConvertDateTimeToUtc)
			.Add(UseLegacyPagination);

		#region IEquatable implementation

		public bool Equals(YdbOptions? other)
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
