﻿using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for SqlServer by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.ProviderSpecific"/>.
	/// </param>
	/// <param name="GenerateScopeIdentity">
	/// Enables identity selection using SCOPE_IDENTITY function for insert with identity APIs.
	/// Default value: <c>true</c>.
	/// </param>
	public sealed record SqlServerOptions
	(
		BulkCopyType BulkCopyType          = BulkCopyType.ProviderSpecific,
		bool         GenerateScopeIdentity = true
	)
		: DataProviderOptions<SqlServerOptions>(BulkCopyType)
	{
		public SqlServerOptions() : this(BulkCopyType.ProviderSpecific)
		{
		}

		SqlServerOptions(SqlServerOptions original) : base(original)
		{
			GenerateScopeIdentity = original.GenerateScopeIdentity;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(GenerateScopeIdentity)
			;

		#region IEquatable implementation

		public bool Equals(SqlServerOptions? other)
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
