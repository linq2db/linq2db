using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using LinqToDB.SqlQuery;

namespace LinqToDB.DataProvider
{
	using Data;
	using SqlProvider;
	using System.Data;
	using System.Threading;
	using System.Threading.Tasks;

	public class BasicBulkCopy
	{
		public virtual BulkCopyRowsCopied BulkCopy<T>(BulkCopyType bulkCopyType, ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
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
			BulkCopyType bulkCopyType, ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return bulkCopyType switch
			{
				BulkCopyType.MultipleRows => MultipleRowsCopyAsync    (table, options, source, cancellationToken),
				BulkCopyType.RowByRow     => RowByRowCopyAsync        (table, options, source, cancellationToken),
				_                         => ProviderSpecificCopyAsync(table, options, source, cancellationToken),
			};
		}

#if !NETFRAMEWORK
		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(
			BulkCopyType bulkCopyType, ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return bulkCopyType switch
			{
				BulkCopyType.MultipleRows => MultipleRowsCopyAsync    (table, options, source, cancellationToken),
				BulkCopyType.RowByRow     => RowByRowCopyAsync        (table, options, source, cancellationToken),
				_                         => ProviderSpecificCopyAsync(table, options, source, cancellationToken),
			};
		}
#endif

		protected virtual BulkCopyRowsCopied ProviderSpecificCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			where T : notnull
		{
			return MultipleRowsCopy(table, options, source);
		}

		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}

#if !NETFRAMEWORK
		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return MultipleRowsCopyAsync(table, options, source, cancellationToken);
		}
#endif

		protected virtual BulkCopyRowsCopied MultipleRowsCopy<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			where T : notnull
		{
			return RowByRowCopy(table, options, source);
		}

		protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
			return RowByRowCopyAsync(table, options, source, cancellationToken);
		}

#if !NETFRAMEWORK
		protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			return RowByRowCopyAsync(table, options, source, cancellationToken);
		}
#endif

		protected virtual BulkCopyRowsCopied RowByRowCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			where T : notnull
		{
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
			ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
		{
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
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

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

#if !NETFRAMEWORK
		protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(
			ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
			where T: notnull
		{
			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			await foreach (var item in source.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext).WithCancellation(cancellationToken))
			{
				await table.DataContext
					.InsertAsync(item, options.TableName ?? table.TableName, options.DatabaseName ?? table.DatabaseName, options.SchemaName ?? table.SchemaName, options.ServerName ?? table.ServerName, options.TableOptions.Or(table.TableOptions), cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
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
#endif

		protected internal static string GetTableName<T>(ISqlBuilder sqlBuilder, BulkCopyOptions options, ITable<T> table, bool escaped = true)
			where T : notnull
		{
			var sqlTable = new SqlTable
			{
				ObjectType   = typeof(T),
				Server       = options.ServerName   ?? table.ServerName,
				Database     = options.DatabaseName ?? table.DatabaseName,
				Schema       = options.SchemaName   ?? table.SchemaName,
				PhysicalName = options.TableName    ?? table.TableName,
				TableOptions = options.TableOptions.Or(table.TableOptions)
			};

			return sqlBuilder
				.BuildTableName(
					new StringBuilder(),
					escaped ? sqlBuilder.GetTableServerName  (sqlTable)  : sqlTable.Server,
					escaped ? sqlBuilder.GetTableDatabaseName(sqlTable)  : sqlTable.Database,
					escaped ? sqlBuilder.GetTableSchemaName  (sqlTable)  : sqlTable.Schema,
					escaped ? sqlBuilder.GetTablePhysicalName(sqlTable)! : sqlTable.PhysicalName,
					sqlTable.TableOptions)
				.ToString();
		}

		protected struct ProviderConnections
		{
			public DataConnection  DataConnection;
			public IDbConnection   ProviderConnection;
			public IDbTransaction? ProviderTransaction;
		}

		#region ProviderSpecific Support

		protected void TraceAction(DataConnection dataConnection, Func<string> commandText, Func<int> action)
		{
			var task = TraceActionAsync(dataConnection, commandText, () => Task.FromResult(action()));
			// the following line of code should be completely redundant, as exceptions should bubble up, since there are no awaited tasks
			if (task.Status != TaskStatus.RanToCompletion) task.GetAwaiter().GetResult();
		}

		protected async Task TraceActionAsync(DataConnection dataConnection, Func<string> commandText, Func<Task<int>> action)
		{
			var now = DateTime.UtcNow;
			var sw  = Stopwatch.StartNew();

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute)
				{
					TraceLevel     = TraceLevel.Info,
					CommandText    = commandText(),
					StartTime      = now,
				});
			}

			try
			{
				var count = await action().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute)
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
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.Error)
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
			int                                       maxParameters = 10000,
			int                                       maxSqlLength  = 100000)
		{
			prepFunction(helper);

			foreach (var item in source)
			{
				addFunction(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > maxParameters || helper.StringBuilder.Length > maxSqlLength)
				{
					finishFunction(helper);
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				finishFunction(helper);
				helper.Execute();
			}

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
			int                                         maxParameters = 10000,
			int                                         maxSqlLength  = 100000)
		{
			prepFunction(helper);

			foreach (var item in source)
			{
				addFunction(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > maxParameters || helper.StringBuilder.Length > maxSqlLength)
				{
					finishFunction(helper);
					if (!await helper.ExecuteAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				finishFunction(helper);
				await helper.ExecuteAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}

#if !NETFRAMEWORK
		protected static async Task<BulkCopyRowsCopied> MultipleRowsCopyHelperAsync<T>(
			MultipleRowsHelper                          helper,
			IAsyncEnumerable<T>                         source,
			string?                                     from,
			Action<MultipleRowsHelper>                  prepFunction,
			Action<MultipleRowsHelper, object, string?> addFunction,
			Action<MultipleRowsHelper>                  finishFunction,
			CancellationToken                           cancellationToken,
			int                                         maxParameters = 10000,
			int                                         maxSqlLength  = 100000)
		{
			prepFunction(helper);

			await foreach (var item in source.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext).WithCancellation(cancellationToken))
			{
				addFunction(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > maxParameters || helper.StringBuilder.Length > maxSqlLength)
				{
					finishFunction(helper);
					if (!await helper.ExecuteAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				finishFunction(helper);
				await helper.ExecuteAsync(cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}
#endif

		protected BulkCopyRowsCopied MultipleRowsCopy1<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			where T : notnull
			=> MultipleRowsCopy1(new MultipleRowsHelper<T>(table, options), source);

		protected BulkCopyRowsCopied MultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
			=> MultipleRowsCopyHelper(helper, source, null, MultipleRowsCopy1Prep, MultipleRowsCopy1Add, MultipleRowsCopy1Finish);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, CancellationToken cancellationToken)
			where T : notnull
			=> MultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async(MultipleRowsHelper helper, IEnumerable source, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, null, MultipleRowsCopy1Prep, MultipleRowsCopy1Add, MultipleRowsCopy1Finish, cancellationToken);

#if !NETFRAMEWORK
		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source, cancellationToken);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopyHelperAsync(helper, source, null, MultipleRowsCopy1Prep, MultipleRowsCopy1Add, MultipleRowsCopy1Finish, cancellationToken);
#endif

		private void MultipleRowsCopy1Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
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
			helper.BuildColumns(item!);
			helper.StringBuilder.Append("),");

			helper.RowsCopied.RowsCopied++;
			helper.CurrentCount++;
		}

		private void MultipleRowsCopy1Finish(MultipleRowsHelper helper)
		{
			helper.StringBuilder.Length--;
		}

		protected BulkCopyRowsCopied MultipleRowsCopy2<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, string from)
			where T : notnull
			=> MultipleRowsCopy2(new MultipleRowsHelper<T>(table, options), source, from);

		protected BulkCopyRowsCopied MultipleRowsCopy2(MultipleRowsHelper helper, IEnumerable source, string from)
			=> MultipleRowsCopyHelper(helper, source, from, MultipleRowsCopy2Prep, MultipleRowsCopy2Add, MultipleRowsCopy2Finish);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, string from, CancellationToken cancellationToken)
			where T : notnull
			=> MultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, from, cancellationToken);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async(MultipleRowsHelper helper, IEnumerable source, string from, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy2Prep, MultipleRowsCopy2Add, MultipleRowsCopy2Finish, cancellationToken);

#if !NETFRAMEWORK
		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, string from, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, from, cancellationToken);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source, string from, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy2Prep, MultipleRowsCopy2Add, MultipleRowsCopy2Finish, cancellationToken);
#endif

		private void MultipleRowsCopy2Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
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
			helper.BuildColumns(item!);
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
			=> MultipleRowsCopyHelper(helper, source, from, MultipleRowsCopy3Prep, MultipleRowsCopy3Add, MultipleRowsCopy3Finish);

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy3Async(MultipleRowsHelper helper, BulkCopyOptions options, IEnumerable source, string from, CancellationToken cancellationToken)
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy3Prep, MultipleRowsCopy3Add, MultipleRowsCopy3Finish, cancellationToken);

#if !NETFRAMEWORK
		protected Task<BulkCopyRowsCopied> MultipleRowsCopy3Async<T>(MultipleRowsHelper helper, BulkCopyOptions options, IAsyncEnumerable<T> source, string from, CancellationToken cancellationToken)
		where T: notnull
			=> MultipleRowsCopyHelperAsync(helper, source, from, MultipleRowsCopy3Prep, MultipleRowsCopy3Add, MultipleRowsCopy3Finish, cancellationToken);
#endif

		private void MultipleRowsCopy3Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
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
			helper.BuildColumns(item);
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
