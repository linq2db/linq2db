using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Internal.Common;

namespace LinqToDB.DataProvider.Sybase
{
	/// <param name="BulkCopyType">
	/// Using <see cref="BulkCopyType.ProviderSpecific"/> mode with bit and identity fields could lead to following errors:
	/// - bit: <c>false</c> inserted into bit field for first record even if <c>true</c> provided;
	/// - identity: bulk copy operation fail with exception: "Bulk insert failed. Null value is not allowed in not null column.".
	/// Those are provider bugs and could be fixed in latest versions.
	/// </param>
	public sealed record SybaseOptions
	(
		// don't set ProviderSpecific as default type while SAP not fix incorrect bit field value
		// insert for first record
		BulkCopyType BulkCopyType = BulkCopyType.MultipleRows
		// If you add another parameter here, don't forget to update
		// SybaseOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<SybaseOptions>(BulkCopyType)
	{
		public SybaseOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		SybaseOptions(SybaseOptions original) : base(original)
		{
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			;

		#region IEquatable implementation

		public bool Equals(SybaseOptions? other)
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
