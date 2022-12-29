using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.DB2
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for DB2 by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	public sealed record DB2Options
	(
		BulkCopyType           BulkCopyType,
		DB2IdentifierQuoteMode IdentifierQuoteMode
	)
		: DataProviderOptions<DB2Options>(BulkCopyType)
	{
		public DB2Options() : this(BulkCopyType.MultipleRows, DB2IdentifierQuoteMode.Auto)
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
