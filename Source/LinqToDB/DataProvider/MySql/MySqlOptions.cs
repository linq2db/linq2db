using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.MySql
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for MySql by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	public sealed record MySqlOptions
	(
		BulkCopyType BulkCopyType = BulkCopyType.MultipleRows
		// If you add another parameter here, don't forget to update
		// MySqlOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<MySqlOptions>(BulkCopyType)
	{
		public MySqlOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		MySqlOptions(MySqlOptions original) : base(original)
		{
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder;

		#region IEquatable implementation

		public bool Equals(MySqlOptions? other)
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
