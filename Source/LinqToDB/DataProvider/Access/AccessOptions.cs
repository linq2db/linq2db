using System.Collections.Generic;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.Access
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for Access by <see cref="DataContextExtensions.BulkCopy{T}(IDataContext, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	public sealed record AccessOptions
	(
		BulkCopyType BulkCopyType = BulkCopyType.MultipleRows
		// If you add another parameter here, don't forget to update
		// AccessOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<AccessOptions>(BulkCopyType)
	{
		public AccessOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		AccessOptions(AccessOptions original) : base(original)
		{
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder;

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
