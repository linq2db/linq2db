﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;
	using Mapping;

	sealed class FirebirdBulkCopy : BasicBulkCopy
	{
		/// <remarks>
		/// Number based on http://www.firebirdfaq.org/faq197/
		/// TODO: Add Compat Switch. Firebird 2.5 has 64k limit, Firebird 3.0+ 10MB.
		/// </remarks>
		protected override int MaxSqlLength => 65535;

		/// <remarks>
		/// Based on https://github.com/FirebirdSQL/firebird/blob/799bca3ca5f9eb604433addc0f2b7cb3b6c07275/src/dsql/DsqlCompilerScratch.cpp#L528
		/// Max is 65536/2. We subtract one from that in case ADO provider uses parameter for statemnt.
		/// </remarks>
		protected override int MaxParameters => 32767;

		protected override bool CastFirstRowLiteralOnUnionAll    => true;
		protected override bool CastFirstRowParametersOnUnionAll => true;
		protected override bool CastAllRowsParametersOnUnionAll  => true;

		protected override bool CastLiteral(ColumnDescriptor column)
		{
			// if union column is not typed as varchar, it will use char(max_literal_length) type with values padded with spaces
			var dataType = column.DataType;
			if (dataType == DataType.Undefined)
				dataType = column.GetConvertedDbDataType().DataType;

			return dataType is DataType.VarChar or DataType.NVarChar;
		}

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
		{
			// firebird doesn't have built-in identity management, it must be implemented by user using generators and triggers
			if (options.BulkCopyOptions.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2(table, options, source, " FROM rdb$database");
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			// firebird doesn't have built-in identity management, it must be implemented by user using generators and triggers
			if (options.BulkCopyOptions.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2Async(table, options, source, " FROM rdb$database", cancellationToken);
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			// firebird doesn't have built-in identity management, it must be implemented by user using generators and triggers
			if (options.BulkCopyOptions.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2Async(table, options, source, " FROM rdb$database", cancellationToken);
		}
#endif
	}
}
