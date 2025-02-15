using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Data;

namespace LinqToDB.DataProvider.SapHana
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for SapHana by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	public sealed record SapHanaOptions
	(
		BulkCopyType BulkCopyType = BulkCopyType.MultipleRows
		// If you add another parameter here, don't forget to update
		// SapHanaOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<SapHanaOptions>(BulkCopyType)
	{
		public SapHanaOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		SapHanaOptions(SapHanaOptions original) : base(original)
		{
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder;

		#region IEquatable implementation

		public bool Equals(SapHanaOptions? other)
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
