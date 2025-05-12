using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

#if NETFRAMEWORK || NETSTANDARD2_0
using System.Text;
#endif

using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider
{
	public class BasicBulkCopy
	{
		protected virtual int? MaxMultipleRows => null;
		protected virtual int  MaxParameters   => 999;
		protected virtual int  MaxSqlLength    => 100000;

		protected virtual bool CastFirstRowLiteralOnUnionAll    => false;
		protected virtual bool CastFirstRowParametersOnUnionAll => false;
		protected virtual bool CastAllRowsParametersOnUnionAll  => false;

		protected virtual bool CastLiteral(ColumnDescriptor column) => false;

		public virtual BulkCopyRowsCopied BulkCopy<T>(BulkCopyType bulkCopyType, ITable<T> table, DataOptions options, IEnumerable<T> source)
			where T : notnull
		{
			return bulkCopyType switch
			{
				BulkCopyType.MultipleRows => MultipleRowsCopy    (table, options, source),
				BulkCopyType.RowByRow     => RowByRowCopy        (table, options, source),
				_                         => ProviderSpecificCopy(table, options, source),
			};
		}

		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			BulkCopyType bulkCopyType, ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return bulkCopyType switch
			{
				BulkCopyType.MultipleRows => MultipleRowsCopyAsync    (table, options, source, cancellationToken),
				BulkCopyType.RowByRow     => RowByRowCopyAsync        (table, options, source, cancellationToken),
				_                         => ProviderSpecificCopyAsync(table, options, source, cancellationToken),
			};
		}

		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			BulkCopyType bulkCopyType, ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return bulkCopyType switch
			{
				BulkCopyType.MultipleRows => MultipleRowsCopyAsync    (table, options, source, cancellationToken),
				BulkCopyType.RowByRow     => RowByRowCopyAsync        (table, options, source, cancellationToken),
				_                         => ProviderSpecificCopyAsync(table, options, source, cancellationToken),
			};
		}

		protected virtual BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
			where T : notnull
		{
			return MultipleRowsCopy(table, options, source);
		}

		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

		protected virtual BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source)
			where T : notnull
		{
			return RowByRowCopy(table, options, source);
		}

		protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return RowByRowCopyAsync(table, options, source, cancellationToken);
		}

		protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return RowByRowCopyAsync(table, options, source, cancellationToken);
		}

		protected BulkCopyRowsCopied RowByRowCopy<T>(ITable<T> table, DataOptions dataOptions, IEnumerable<T> source)
			where T : notnull
		{
			var options = dataOptions.BulkCopyOptions;

			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			foreach (var item in source)
			{
				table.DataContext.Insert(
					item,
					options.TableName    ?? table.TableName,
					options.DatabaseName ?? table.DatabaseName,
					options.SchemaName   ?? table.SchemaName,
					options.ServerName   ?? table.ServerName,
					options.TableOptions.Or(table.TableOptions));

				rowsCopied.RowsCopied++;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
				{
					options.RowsCopiedCallback(rowsCopied);

					if (rowsCopied.Abort)
						break;
				}
			}

			return rowsCopied;
		}

		protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(
			ITable<T> table, DataOptions dataOptions, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			var options = dataOptions.BulkCopyOptions;

			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			foreach (var item in source)
			{
				await table.DataContext
					.InsertAsync(
						item,
						options.TableName    ?? table.TableName,
						options.DatabaseName ?? table.DatabaseName,
						options.SchemaName   ?? table.SchemaName,
						options.ServerName   ?? table.ServerName,
						options.TableOptions.Or(table.TableOptions),
						cancellationToken)
					.ConfigureAwait(false);

				rowsCopied.RowsCopied++;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
				{
					options.RowsCopiedCallback(rowsCopied);

					if (rowsCopied.Abort)
						break;
				}
			}

			return rowsCopied;
		}

		protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(
			ITable<T> table, DataOptions dataOptions, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			var options = dataOptions.BulkCopyOptions;

			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			await foreach (var item in source.ConfigureAwait(false).WithCancellation(cancellationToken))
			{
				await table.DataContext
					.InsertAsync(item, options.TableName ?? table.TableName, options.DatabaseName ?? table.DatabaseName, options.SchemaName ?? table.SchemaName, options.ServerName ?? table.ServerName, options.TableOptions.Or(table.TableOptions), cancellationToken)
					.ConfigureAwait(false);
				rowsCopied.RowsCopied++;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
				{
					options.RowsCopiedCallback(rowsCopied);

					if (rowsCopied.Abort)
						break;
				}
			}

			return rowsCopied;
		}

		protected internal static string GetTableName<T>(ISqlBuilder sqlBuilder, BulkCopyOptions options, ITable<T> table, bool escaped = true)
			where T : notnull
		{
			var tableName = new SqlObjectName(
				           options.TableName    ?? table.TableName,
				Server   : options.ServerName   ?? table.ServerName,
				Database : options.DatabaseName ?? table.DatabaseName,
				Schema   : options.SchemaName   ?? table.SchemaName);

			var sqlTable = new SqlTable(typeof(T), null, tableName)
			{
				TableOptions = options.TableOptions.Or(table.TableOptions)
			};

			return sqlBuilder
				.BuildObjectName(new (), sqlTable.TableName, escape: escaped, tableOptions: sqlTable.TableOptions)
				.ToString();
		}

		protected struct ProviderConnections
		{
			public DataConnection DataConnection;
			public DbConnection   ProviderConnection;
			public DbTransaction? ProviderTransaction;
		}

		#region ProviderSpecific Support

		protected void TraceAction(DataConnection dataConnection, Func<string> commandText, Func<int> action)
		{
			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute, TraceOperation.BulkCopy, false)
				{
					TraceLevel     = TraceLevel.Info,
					CommandText    = commandText(),
					StartTime      = now,
				});
			}

			try
			{
				var count = action();

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute, TraceOperation.BulkCopy, false)
					{
						TraceLevel      = TraceLevel.Info,
						CommandText     = commandText(),
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = count,
					});
				}
			}
			catch (Exception ex)
			{
				if (dataConnection.TraceSwitchConnection.TraceError)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.Error, TraceOperation.BulkCopy, false)
					{
						TraceLevel     = TraceLevel.Error,
						CommandText    = commandText(),
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		protected async Task TraceActionAsync(DataConnection dataConnection, Func<string> commandText, Func<Task<int>> action)
		{
			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute, TraceOperation.BulkCopy, true)
				{
					TraceLevel     = TraceLevel.Info,
					CommandText    = commandText(),
					StartTime      = now,
				});
			}

			try
			{
				var count = await action().ConfigureAwait(false);

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute, TraceOperation.BulkCopy, true)
					{
						TraceLevel      = TraceLevel.Info,
						CommandText     = commandText(),
						StartTime       = now,
						ExecutionTime   = sw.Elapsed,
						RecordsAffected = count,
					});
				}
			}
			catch (Exception ex)
			{
				if (dataConnection.TraceSwitchConnection.TraceError)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.Error, TraceOperation.BulkCopy, true)
					{
						TraceLevel     = TraceLevel.Error,
						CommandText    = commandText(),
						StartTime      = now,
						ExecutionTime  = sw.Elapsed,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region MultipleRows Support

		protected static BulkCopyRowsCopied MultipleRowsCopyHelper(
			MultipleRowsHelper                        helper,
			IEnumerable                               source,
			string?                                   from,
			Action<MultipleRowsHelper>                prepFunction,
			Action<MultipleRowsHelper,object,string?> addFunction,
			Action<MultipleRowsHelper>                finishFunction,
			int                                       maxParameters,
			int                                       maxSqlLength)
		{
			var adjustedBatchSize = helper.Options.BulkCopyOptions.UseParameters
				? Math.Min(helper.BatchSize,
					helper.Options.BulkCopyOptions.MaxParametersForBatch.GetValueOrDefault(maxParameters) / helper.Columns.Length)
				: helper.BatchSize;

			prepFunction(helper);

			foreach (var item in source)
			{
				helper.LastRowParameterIndex = helper.ParameterIndex;
				helper.LastRowStringIndex    = helper.StringBuilder.Length;
				addFunction(helper, item!, from);
				var needRemove = helper.Parameters.Count > maxParameters ||
				                 helper.StringBuilder.Length > maxSqlLength;
				var isSingle = helper.CurrentCount == 1;
				if (helper.CurrentCount >= adjustedBatchSize || needRemove)
				{
					if (needRemove && !isSingle)
					{
						helper.Parameters.RemoveRange(helper.LastRowParameterIndex, helper.ParameterIndex-helper.LastRowParameterIndex);
						helper.StringBuilder.Length = helper.LastRowStringIndex;
						helper.RowsCopied.RowsCopied--;
					}

					finishFunction(helper);

					if (!helper.Execute())
					{
						if (!helper.SuppressCloseAfterUse && helper.OriginalContext.CloseAfterUse)
							helper.OriginalContext.Close();

						return helper.RowsCopied;
					}

					if (needRemove && !isSingle)
					{
						addFunction(helper, item!, from);
					}
				}
			}

			if (helper.CurrentCount > 0)
			{
				finishFunction(helper);
				helper.Execute();
			}

			if (!helper.SuppressCloseAfterUse && helper.OriginalContext.CloseAfterUse)
				helper.OriginalContext.Close();

			return helper.RowsCopied;
		}

		protected static async Task<BulkCopyRowsCopied> MultipleRowsCopyHelperAsync(
			MultipleRowsHelper                          helper,
			IEnumerable                                 source,
			string?                                     from,
			Action<MultipleRowsHelper>                  prepFunction,
			Action<MultipleRowsHelper, object, string?> addFunction,
			Action<MultipleRowsHelper>                  finishFunction,
			CancellationToken                           cancellationToken,
			int                                         maxParameters,
			int                                         maxSqlLength)
		{
			var adjustedBatchSize = helper.Options.BulkCopyOptions.UseParameters
				? Math.Min(helper.BatchSize, helper.Options.BulkCopyOptions.MaxParametersForBatch.GetValueOrDefault(maxParameters) / helper.Columns.Length)
				: helper.BatchSize;

			prepFunction(helper);

			foreach (var item in source)
			{
				helper.LastRowParameterIndex = helper.ParameterIndex;
				helper.LastRowStringIndex    = helper.StringBuilder.Length;
				addFunction(helper, item!, from);

				var needRemove = helper.Parameters.Count     > maxParameters ||
				                 helper.StringBuilder.Length > maxSqlLength;
				var isSingle = helper.CurrentCount == 1;
				if (helper.CurrentCount >= adjustedBatchSize || needRemove)
				{
					if (needRemove && !isSingle)
					{
						helper.Parameters.RemoveRange(helper.LastRowParameterIndex, helper.ParameterIndex-helper.LastRowParameterIndex);
						helper.StringBuilder.Length = helper.LastRowStringIndex;
						helper.RowsCopied.RowsCopied--;
					}

					finishFunction(helper);
					if (!await helper.ExecuteAsync(cancellationToken).ConfigureAwait(false))
					{
						if (!helper.SuppressCloseAfterUse && helper.OriginalContext.CloseAfterUse)
							await helper.OriginalContext.CloseAsync().ConfigureAwait(false);

						return helper.RowsCopied;
					}

					if (needRemove && !isSingle)
					{
						addFunction(helper, item!, from);
					}
				}
			}

			if (helper.CurrentCount > 0)
			{
				finishFunction(helper);
				await helper.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			}

			if (!helper.SuppressCloseAfterUse && helper.OriginalContext.CloseAfterUse)
				await helper.OriginalContext.CloseAsync().ConfigureAwait(false);

			return helper.RowsCopied;
		}

		protected static async Task<BulkCopyRowsCopied> MultipleRowsCopyHelperAsync<T>(
			MultipleRowsHelper                          helper,
			IAsyncEnumerable<T>                         source,
			string?                                     from,
			Action<MultipleRowsHelper>                  prepFunction,
			Action<MultipleRowsHelper, object, string?> addFunction,
			Action<MultipleRowsHelper>                  finishFunction,
			CancellationToken                           cancellationToken,
			int                                         maxParameters,
			int                                         maxSqlLength)
		{
			var adjustedBatchSize = helper.Options.BulkCopyOptions.UseParameters
				? Math.Min(helper.BatchSize, helper.Options.BulkCopyOptions.MaxParametersForBatch.GetValueOrDefault(maxParameters) / helper.Columns.Length)
				: helper.BatchSize;

			prepFunction(helper);

			await foreach (var item in source.ConfigureAwait(false).WithCancellation(cancellationToken))
			{
				helper.LastRowParameterIndex = helper.ParameterIndex;
				helper.LastRowStringIndex    = helper.StringBuilder.Length;
				addFunction(helper, item!, from);

				var needRemove = helper.Parameters.Count     > maxParameters ||
				                 helper.StringBuilder.Length > maxSqlLength;
				var isSingle = helper.CurrentCount == 1;
				if (helper.CurrentCount >= adjustedBatchSize || needRemove)
				{
					if (needRemove && !isSingle)
					{
						helper.Parameters.RemoveRange(helper.LastRowParameterIndex, helper.ParameterIndex-helper.LastRowParameterIndex);
						helper.StringBuilder.Length = helper.LastRowStringIndex;
						helper.RowsCopied.RowsCopied--;
					}

					finishFunction(helper);
					if (!await helper.ExecuteAsync(cancellationToken).ConfigureAwait(false))
					{
						if (!helper.SuppressCloseAfterUse && helper.OriginalContext.CloseAfterUse)
							await helper.OriginalContext.CloseAsync().ConfigureAwait(false);

						return helper.RowsCopied;
					}

					if (needRemove && !isSingle)
					{
						addFunction(helper, item!, from);
					}
				}
			}

			if (helper.CurrentCount > 0)
			{
				finishFunction(helper);
				await helper.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			}

			if (!helper.SuppressCloseAfterUse && helper.OriginalContext.CloseAfterUse)
				await helper.OriginalContext.CloseAsync().ConfigureAwait(false);

			return helper.RowsCopied;
		}

		protected BulkCopyRowsCopied MultipleRowsCopy1<T>(ITable<T> table, DataOptions options, IEnumerable<T> source)
			where T : notnull
		{
			if (MaxMultipleRows != null
				&& (options.BulkCopyOptions.MaxBatchSize == null
					|| options.BulkCopyOptions.MaxBatchSize > MaxMultipleRows))
			{
				options = options.UseBulkCopyMaxBatchSize(MaxMultipleRows);
			}

			return MultipleRowsCopy1(new MultipleRowsHelper<T>(table, options), source);
		}

		protected BulkCopyRowsCopied MultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, MultipleRowsCopy1Prep, MultipleRowsCopy1Add, MultipleRowsCopy1Finish,MaxParameters, MaxSqlLength);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(ITable<T> table, DataOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			if (MaxMultipleRows != null
				&& (options.BulkCopyOptions.MaxBatchSize == null
					|| options.BulkCopyOptions.MaxBatchSize > MaxMultipleRows))
			{
				options = options.UseBulkCopyMaxBatchSize(MaxMultipleRows);
			}

			return MultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
		}

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, MultipleRowsCopy1Prep, MultipleRowsCopy1Add, MultipleRowsCopy1Finish, cancellationToken, MaxParameters, MaxSqlLength);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		where T : notnull
		{
			if (MaxMultipleRows != null
				&& (options.BulkCopyOptions.MaxBatchSize == null
					|| options.BulkCopyOptions.MaxBatchSize > MaxMultipleRows))
			{
				options = options.UseBulkCopyMaxBatchSize(MaxMultipleRows);
			}

			return MultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);
		}

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopyHelperAsync(helper, source, null, MultipleRowsCopy1Prep, MultipleRowsCopy1Add, MultipleRowsCopy1Finish, cancellationToken, MaxParameters, MaxSqlLength);

		private void MultipleRowsCopy1Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendLine(CultureInfo.InvariantCulture, $"INSERT INTO {helper.TableName}")
				.Append('(');

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append('\t');
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(',');
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.Append(')');

			helper.StringBuilder
				.AppendLine()
				.Append("VALUES");

			helper.SetHeader();
		}

		private void MultipleRowsCopy1Add(MultipleRowsHelper helper, object item, string? from)
		{
			helper.StringBuilder
				.AppendLine()
				.Append('(');
			helper.BuildColumns(item);
			helper.StringBuilder.Append("),");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		private void MultipleRowsCopy1Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.Length--;
		}

		protected BulkCopyRowsCopied MultipleRowsCopy2<T>(ITable<T> table, DataOptions options, IEnumerable<T> source, string from)
			where T : notnull
		{
			if (MaxMultipleRows != null
				&& (options.BulkCopyOptions.MaxBatchSize == null
					|| options.BulkCopyOptions.MaxBatchSize > MaxMultipleRows))
			{
				options = options.UseBulkCopyMaxBatchSize(MaxMultipleRows);
			}

			return MultipleRowsCopy2(new MultipleRowsHelper<T>(table, options), source, from);
		}

		protected BulkCopyRowsCopied MultipleRowsCopy2(MultipleRowsHelper helper, IEnumerable source, string from)
			=> MultipleRowsCopyHelper(helper, source, from, MultipleRowsCopy2Prep, MultipleRowsCopy2Add, MultipleRowsCopy2Finish, MaxParameters, MaxSqlLength);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(ITable<T> table, DataOptions options, IEnumerable<T> source, string from, CancellationToken cancellationToken)
			where T : notnull
		{
			if (MaxMultipleRows != null
				&& (options.BulkCopyOptions.MaxBatchSize == null
					|| options.BulkCopyOptions.MaxBatchSize > MaxMultipleRows))
			{
				options = options.UseBulkCopyMaxBatchSize(MaxMultipleRows);
			}

			return MultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, from, cancellationToken);
		}

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async(MultipleRowsHelper helper, IEnumerable source, string from, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy2Prep, MultipleRowsCopy2Add, MultipleRowsCopy2Finish, cancellationToken, MaxParameters, MaxSqlLength);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(ITable<T> table, DataOptions options, IAsyncEnumerable<T> source, string from, CancellationToken cancellationToken)
		where T : notnull
		{
			if (MaxMultipleRows != null
				&& (options.BulkCopyOptions.MaxBatchSize == null
					|| options.BulkCopyOptions.MaxBatchSize > MaxMultipleRows))
			{
				options = options.UseBulkCopyMaxBatchSize(MaxMultipleRows);
			}

			return MultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, from, cancellationToken);
		}

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, string from, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy2Prep, MultipleRowsCopy2Add, MultipleRowsCopy2Finish, cancellationToken, MaxParameters, MaxSqlLength);

		private void MultipleRowsCopy2Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendLine(CultureInfo.InvariantCulture, $"INSERT INTO {helper.TableName}")
				.Append('(');

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append('\t');
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(',');
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.Append(')');

			helper.SetHeader();
		}

		private void MultipleRowsCopy2Add(MultipleRowsHelper helper, object item, string? from)
		{
			helper.StringBuilder
				.AppendLine()
				.Append("SELECT ");
			helper.BuildColumns(item, castParameters: CastFirstRowParametersOnUnionAll, castAllRows: CastAllRowsParametersOnUnionAll, castFirstRowLiteralOnUnionAll: CastFirstRowLiteralOnUnionAll, castLiteral: CastLiteral);
			helper.StringBuilder.Append(from);
			helper.StringBuilder.Append(" UNION ALL");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		private void MultipleRowsCopy2Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.Length -= " UNION ALL".Length;
		}

		protected BulkCopyRowsCopied MultipleRowsCopy3(MultipleRowsHelper helper, BulkCopyOptions options, IEnumerable source, string from)
			=> MultipleRowsCopyHelper(helper, source, from, MultipleRowsCopy3Prep, MultipleRowsCopy3Add, MultipleRowsCopy3Finish, MaxParameters, MaxSqlLength);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy3Async(MultipleRowsHelper helper, BulkCopyOptions options, IEnumerable source, string from, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy3Prep, MultipleRowsCopy3Add, MultipleRowsCopy3Finish, cancellationToken, MaxParameters, MaxSqlLength);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy3Async<T>(MultipleRowsHelper helper, BulkCopyOptions options, IAsyncEnumerable<T> source, string from, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy3Prep, MultipleRowsCopy3Add, MultipleRowsCopy3Finish, cancellationToken, MaxParameters, MaxSqlLength);

		private void MultipleRowsCopy3Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendLine(CultureInfo.InvariantCulture, $"INSERT INTO {helper.TableName}")
				.Append('(');

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append('\t');
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(',');
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.AppendLine(")")
				.AppendLine("SELECT * FROM")
				.Append('(');

			helper.SetHeader();
		}

		private void MultipleRowsCopy3Add(MultipleRowsHelper helper, object item, string? from)
		{
			helper.StringBuilder
				.AppendLine()
				.Append("\tSELECT ");
			helper.BuildColumns(item, castParameters: CastFirstRowParametersOnUnionAll, castAllRows : CastAllRowsParametersOnUnionAll, castFirstRowLiteralOnUnionAll: CastFirstRowLiteralOnUnionAll, castLiteral: CastLiteral);
			helper.StringBuilder.Append(from);
			helper.StringBuilder.Append(" UNION ALL");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		private void MultipleRowsCopy3Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.Length -= " UNION ALL".Length;
			helper.StringBuilder
				.AppendLine()
				.Append(')');
		}

		#endregion
	}
}
