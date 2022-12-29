using System;

namespace LinqToDB.DataProvider.Oracle
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="AlternativeBulkCopy">
	/// Specify AlternativeBulkCopy used by Oracle Provider.
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
