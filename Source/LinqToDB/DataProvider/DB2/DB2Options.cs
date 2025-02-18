using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;

namespace LinqToDB.DataProvider.DB2
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for DB2 by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="IdentifierQuoteMode">
	/// Identifier quotation logic for SQL generation.
	/// Default value: <see cref="DB2IdentifierQuoteMode.Auto"/>.
	/// </param>
	public sealed record DB2Options
	(
		BulkCopyType           BulkCopyType        = BulkCopyType.MultipleRows,
		DB2IdentifierQuoteMode IdentifierQuoteMode = DB2IdentifierQuoteMode.Auto
		// If you add another parameter here, don't forget to update
		// DB2Options copy constructor and CreateID method.
	)
		: DataProviderOptions<DB2Options>(BulkCopyType)
	{
		public DB2Options() : this(BulkCopyType.MultipleRows)
		{
		}

		DB2Options(DB2Options original) : base(original)
		{
			IdentifierQuoteMode = original.IdentifierQuoteMode;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(IdentifierQuoteMode)
			;

		#region IEquatable implementation

		public bool Equals(DB2Options? other)
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
