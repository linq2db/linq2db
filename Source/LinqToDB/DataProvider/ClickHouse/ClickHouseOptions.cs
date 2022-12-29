using System;

namespace LinqToDB.DataProvider.ClickHouse
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode.
	/// Default value: <c>BulkCopyType.ProviderSpecific</c>.
	/// </param>
	public sealed record ClickHouseOptions
	(
		BulkCopyType BulkCopyType = BulkCopyType.ProviderSpecific
	)
		: DataProviderOptions<ClickHouseOptions>(BulkCopyType)
	{
		public ClickHouseOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		ClickHouseOptions(ClickHouseOptions original) : base(original)
		{
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
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
