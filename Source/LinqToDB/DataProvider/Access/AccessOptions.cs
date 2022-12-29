using System;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Common.Internal;
	using Data;

	// <param name="AlternativeBulkCopy">
	// Specify AlternativeBulkCopy used by Oracle Provider.
	// </param>
	public sealed record AccessOptions
	(
		BulkCopyType        BulkCopyType//,
		//AlternativeBulkCopy AlternativeBulkCopy
	)
		: DataProviderOptions<AccessOptions>(BulkCopyType)
	{
		public AccessOptions() : this(BulkCopyType.MultipleRows/*, AlternativeBulkCopy.InsertAll*/)
		{
		}

		AccessOptions(AccessOptions original) : base(original)
		{
			//AlternativeBulkCopy = original.AlternativeBulkCopy;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			//.Add(AlternativeBulkCopy)
			;

		#region IEquatable implementation

		public bool Equals(AccessOptions? other)
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
