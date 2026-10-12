using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Options;

namespace LinqToDB.DataProvider.SQLite
{
	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for SQLite by <see cref="DataContextExtensions.BulkCopy{T}(IDataContext, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="AlwaysCheckDbNull">
	/// Enables null-value checks during database data mapping even if SQLite reports that column cannot be <c>NULL</c> to
	/// avoid <see cref="NullReferenceException"/> on mapping when database reports nullability incorrectly.
	/// Default value: <see langword="true"/>.
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
		/// <summary>
		/// When enabled, System.Data.SQLite parameters of type <see cref="DateTimeOffset"/> use invariant
		/// <c>yyyy-MM-dd HH:mm:ss.fffzzz</c> text instead of the driver's culture-dependent format.
		/// Default value: <see langword="false"/>.
		/// </summary>
		/// <remarks>
		/// Sub-millisecond precision is truncated. Inline literals and default bulk-copy literals already
		/// use this format; Microsoft.Data.Sqlite parameters are unaffected. Existing rows are not migrated:
		/// normalize culture-formatted data before enabling this option to preserve text-key matching.
		/// For remote queries, configure the option on the server's data connection.
		/// </remarks>
		public bool UseInvariantDateTimeOffsetFormat { get; init; }

		public SQLiteOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		SQLiteOptions(SQLiteOptions original) : base(original)
		{
			AlwaysCheckDbNull                = original.AlwaysCheckDbNull;
			UseInvariantDateTimeOffsetFormat = original.UseInvariantDateTimeOffsetFormat;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(AlwaysCheckDbNull)
			.Add(UseInvariantDateTimeOffsetFormat)
			;

		#region IEquatable implementation

		public bool Equals([NotNullWhen(true)] SQLiteOptions? other)
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
