﻿using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Common.Internal;
	using Data;

	/// <param name="BulkCopyType">
	/// Default bulk copy mode, used for SqlCe by <see cref="DataConnectionExtensions.BulkCopy{T}(DataConnection, IEnumerable{T})"/>
	/// methods, if mode is not specified explicitly.
	/// Default value: <see cref="BulkCopyType.MultipleRows"/>.
	/// </param>
	/// <param name="InlineFunctionParameters">
	/// Enables force inlining of function parameters to support SQL CE 3.0.
	/// Default value: <c>false</c>.
	/// </param>
	public sealed record SqlCeOptions
	(
		BulkCopyType BulkCopyType             = BulkCopyType.MultipleRows,
		bool         InlineFunctionParameters = default
	)
		: DataProviderOptions<SqlCeOptions>(BulkCopyType)
	{
		public SqlCeOptions() : this(BulkCopyType.MultipleRows)
		{
		}

		SqlCeOptions(SqlCeOptions original) : base(original)
		{
			InlineFunctionParameters = original.InlineFunctionParameters;
		}

		protected override IdentifierBuilder CreateID(IdentifierBuilder builder) => builder
			.Add(InlineFunctionParameters)
			;

		#region IEquatable implementation

		public bool Equals(SqlCeOptions? other)
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
