using System.Collections.Generic;

namespace LinqToDB.DataProvider.Firebird
{
	using Data;
	using System.Threading;
	using System.Threading.Tasks;

	class FirebirdBulkCopy : BasicBulkCopy
	{
		// TODO: Firebird 2.5 has 64k limit, Firebird 3.0+ 10MB. Add Compat Switch
		protected override int MaxSqlLength => 65535;

		protected override BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			// firebird doesn't have built-in identity management, it must be implemented by user using generators and triggers
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2(table, options, source, " FROM rdb$database");
		}

		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
		{
			// firebird doesn't have built-in identity management, it must be implemented by user using generators and triggers
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2Async(table, options, source, " FROM rdb$database", cancellationToken);
		}

#if NATIVE_ASYNC
		protected override Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		{
			// firebird doesn't have built-in identity management, it must be implemented by user using generators and triggers
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by Firebird provider. If you use generators with triggers, you should disable triggers during BulkCopy execution manually.");

			return MultipleRowsCopy2Async(table, options, source, " FROM rdb$database", cancellationToken);
		}
#endif
	}
}
