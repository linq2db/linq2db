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
	/// <param name="UseParametrizedDecimal">
	/// Use Decimal(p, s) type name instead of Decimal.
	/// Default value: <see langword="true"/>.
	/// </param>
	public sealed record YdbOptions(
		BulkCopyType BulkCopyType     = BulkCopyType.ProviderSpecific,
		bool UseParametrizedDecimal   = true
	) : DataProviderOptions<YdbOptions>(BulkCopyType)
	{
		public YdbOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		private YdbOptions(YdbOptions original) : base(original)
		{
			UseParametrizedDecimal = original.UseParametrizedDecimal;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(UseParametrizedDecimal)
			;

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
