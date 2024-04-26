using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for Firebird by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="IdentifierQuoteMode">
	/// Specifies how identifiers like table and field names should be quoted.
	/// <remarks>
	/// Default value: <see cref="FirebirdIdentifierQuoteMode.Auto"/>.
	/// </remarks>
	/// </param>
	/// <param name="IsLiteralEncodingSupported">
	/// Specifies that Firebird supports literal encoding. Available from version 2.5 and could be used for Dialect 1 databases.
	/// </param>
	public sealed record FirebirdOptions
	(
		BulkCopyType                BulkCopyType               = BulkCopyType.MultipleRows,
		FirebirdIdentifierQuoteMode IdentifierQuoteMode        = FirebirdIdentifierQuoteMode.Auto,
		bool                        IsLiteralEncodingSupported = true
		// If you add another parameter here, don't forget to update
		// FirebirdOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<FirebirdOptions>(BulkCopyType)
	{
		public FirebirdOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		FirebirdOptions(FirebirdOptions original) : base(original)
		{
			IdentifierQuoteMode        = original.IdentifierQuoteMode;
			IsLiteralEncodingSupported = original.IsLiteralEncodingSupported;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(IdentifierQuoteMode)
			.Add(IsLiteralEncodingSupported)
			;

		#region IEquatable implementation

		public bool Equals(FirebirdOptions? other)
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
