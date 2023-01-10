﻿using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.PostgreSQL
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for PostgreSQL by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="NormalizeTimestampData">
	/// Enables normalization of <see cref="DateTime"/> and <see cref="DateTimeOffset"/> data, passed to query
	/// as parameter or passed to <see cref="DataConnectionExtensions.BulkCopy{T}(ITable{T}, IEnumerable{T})"/> APIs,
	/// to comform with Npgsql 6 requerements:
	/// <list type="bullet">
	/// <item>convert <see cref="DateTimeOffset"/> value to UTC value with zero <see cref="DateTimeOffset.Offset"/></item>
	/// <item>Use <see cref="DateTimeKind.Utc"/> for <see cref="DateTime"/> timestamptz values</item>
	/// <item>Use <see cref="DateTimeKind.Unspecified"/> for <see cref="DateTime"/> timestamp values with <see cref="DateTimeKind.Utc"/> kind</item>
	/// </list>
	/// Default value: <c>true</c>.
	/// </param>
	/// <param name="IdentifierQuoteMode">
	/// Specify identifiers quotation logic for SQL generation.
	/// Default value: <see cref="PostgreSQLIdentifierQuoteMode.Auto"/>.
	/// </param>
	public sealed record PostgreSQLOptions
	(
		BulkCopyType                  BulkCopyType           = BulkCopyType.MultipleRows,
		bool                          NormalizeTimestampData = true,
		PostgreSQLIdentifierQuoteMode IdentifierQuoteMode    = PostgreSQLIdentifierQuoteMode.Auto
	)
		: DataProviderOptions<PostgreSQLOptions>(BulkCopyType)
	{
		public PostgreSQLOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		PostgreSQLOptions(PostgreSQLOptions original) : base(original)
		{
			NormalizeTimestampData = original.NormalizeTimestampData;
			IdentifierQuoteMode    = original.IdentifierQuoteMode;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(NormalizeTimestampData)
			.Add(IdentifierQuoteMode)
			;

		#region IEquatable implementation

		public bool Equals(PostgreSQLOptions? other)
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
