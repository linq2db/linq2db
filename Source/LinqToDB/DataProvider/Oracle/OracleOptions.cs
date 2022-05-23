using System;

namespace LinqToDB.DataProvider.Oracle
{
	using Common.Internal;
	using Data;
	using Infrastructure;

	/// <summary>
	/// This is internal API and is not intended for use by LinqToDB applications.
	/// It may change or be removed without further notice.
	/// </summary>	/// <param name="BulkCopyType">
	/// BulkCopyType used by Oracle Provider by default.
	/// </param>
	/// <param name="AlternativeBulkCopy">
	/// Specify AlternativeBulkCopy used by Oracle Provider.
	/// </param>
	public sealed record OracleOptions
	(
		BulkCopyType        BulkCopyType,
		AlternativeBulkCopy AlternativeBulkCopy
	)
		: IOptionSet
	{
		public OracleOptions() : this(BulkCopyType.MultipleRows, AlternativeBulkCopy.InsertAll)
		{
		}

		OracleOptions(OracleOptions original)
		{
			BulkCopyType        = original.BulkCopyType;
			AlternativeBulkCopy = original.AlternativeBulkCopy;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(BulkCopyType)
			.Add(AlternativeBulkCopy)
			.CreateID();

		public static OracleOptions Default { get; set; } = new();

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
