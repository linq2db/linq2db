using System;
using System.Collections.Generic;

using LinqToDB.Common;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for SQLite by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="AlwaysCheckDbNull">
	/// Enables null-value checks during database data mapping even if SQLite reports that column cannot be <c>NULL</c> to
	/// avoid <see cref="NullReferenceException"/> on mapping when database reports nullability incorrectly.
	/// Default value: <c>true</c>.
	/// </param>
	public sealed record SQLiteOptions
	(
		BulkCopyType BulkCopyType      = BulkCopyType.MultipleRows,
		bool         AlwaysCheckDbNull = true
		// If you add another parameter here, don't forget to update
		// SQLiteOptions copy constructor and CreateID method.
	)
		: DataProviderOptions<SQLiteOptions>(BulkCopyType)
	{
		public SQLiteOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		SQLiteOptions(SQLiteOptions original) : base(original)
		{
			AlwaysCheckDbNull = original.AlwaysCheckDbNull;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(AlwaysCheckDbNull)
			;

		#region IEquatable implementation

		public bool Equals(SQLiteOptions? other)
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
