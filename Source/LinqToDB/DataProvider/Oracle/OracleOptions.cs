using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for oracle by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="AlternativeBulkCopy">
	/// Defines type of multi-row INSERT operation to generate for <see cref="BulkCopyType.RowByRow"/> bulk copy mode.
	/// </param>
	/// <param name="DontEscapeLowercaseIdentifiers">
	/// Gets or sets flag to tell LinqToDB to quote identifiers, if they contain lowercase letters.
	/// Default value: <c>false</c>.
	/// This flag is added for backward compatibility and not recommended for use with new applications.
	/// </param>
	public sealed record OracleOptions
	(
		BulkCopyType        BulkCopyType                   = BulkCopyType.MultipleRows,
		AlternativeBulkCopy AlternativeBulkCopy            = AlternativeBulkCopy.InsertAll,
		bool                DontEscapeLowercaseIdentifiers = false
		// If you add another parameter here, don't forget to update
		// OracleOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<OracleOptions>(BulkCopyType)
	{
		public OracleOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		OracleOptions(OracleOptions original) : base(original)
		{
			AlternativeBulkCopy            = original.AlternativeBulkCopy;
			DontEscapeLowercaseIdentifiers = original.DontEscapeLowercaseIdentifiers;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(AlternativeBulkCopy)
			.Add(DontEscapeLowercaseIdentifiers)
			;

		#region IEquatable implementation

		public bool Equals(OracleOptions? other)
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
