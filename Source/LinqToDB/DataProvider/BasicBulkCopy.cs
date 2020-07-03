using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Data;
	using SqlProvider;
	using System.Threading.Tasks;

	public class BasicBulkCopy
	{
		public virtual BulkCopyRowsCopied BulkCopy<T>(BulkCopyType bulkCopyType, ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			switch (bulkCopyType)
			{
				case BulkCopyType.MultipleRows : return MultipleRowsCopy    (table, options, source);
				case BulkCopyType.RowByRow     : return RowByRowCopy        (table, options, source);
				default                        : return ProviderSpecificCopy(table, options, source);
			}
		}

		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(BulkCopyType bulkCopyType, ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			switch (bulkCopyType)
			{
				case BulkCopyType.MultipleRows : return MultipleRowsCopyAsync    (table, options, source);
				case BulkCopyType.RowByRow     : return RowByRowCopyAsync        (table, options, source);
				default                        : return ProviderSpecificCopyAsync(table, options, source);
			}
		}

#if !NET45 && !NET46
		public virtual Task<BulkCopyRowsCopied> BulkCopyAsync<T>(BulkCopyType bulkCopyType, ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			switch (bulkCopyType)
			{
				case BulkCopyType.MultipleRows: return MultipleRowsCopyAsync(table, options, source);
				case BulkCopyType.RowByRow: return RowByRowCopyAsync(table, options, source);
				default: return ProviderSpecificCopyAsync(table, options, source);
			}
		}
#endif

		protected virtual BulkCopyRowsCopied ProviderSpecificCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy(table, options, source);
		}

		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopyAsync(table, options, source);
		}

#if !NET45 && !NET46
		protected virtual Task<BulkCopyRowsCopied> ProviderSpecificCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			return MultipleRowsCopyAsync(table, options, source);
		}
#endif

		protected virtual BulkCopyRowsCopied MultipleRowsCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return RowByRowCopy(table, options, source);
		}

		protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			return RowByRowCopyAsync(table, options, source);
		}

#if !NET45 && !NET46
		protected virtual Task<BulkCopyRowsCopied> MultipleRowsCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			return RowByRowCopyAsync(table, options, source);
		}
#endif

		protected virtual BulkCopyRowsCopied RowByRowCopy<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			foreach (var item in source)
			{
				table.DataContext.Insert(item, options.TableName, options.DatabaseName, options.SchemaName, options.ServerName);
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

		protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
		{
			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			foreach (var item in source)
			{
				await table.DataContext.InsertAsync(item, options.TableName, options.DatabaseName, options.SchemaName, options.ServerName)
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

#if !NET45 && !NET46
		protected virtual async Task<BulkCopyRowsCopied> RowByRowCopyAsync<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
		{
			// This limitation could be lifted later for some providers that supports identity insert if we will get such request
			// It will require support from DataConnection.Insert
			if (options.KeepIdentity == true)
				throw new LinqToDBException($"{nameof(BulkCopyOptions)}.{nameof(BulkCopyOptions.KeepIdentity)} = true is not supported by {nameof(BulkCopyType)}.{nameof(BulkCopyType.RowByRow)} mode");

			var rowsCopied = new BulkCopyRowsCopied();

			await foreach (var item in source)
			{
				await table.DataContext.InsertAsync(item, options.TableName, options.DatabaseName, options.SchemaName, options.ServerName)
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
		{
			var serverName   = options.ServerName   ?? table.ServerName;
			var databaseName = options.DatabaseName ?? table.DatabaseName;
			var schemaName   = options.SchemaName   ?? table.SchemaName;
			var tableName    = options.TableName    ?? table.TableName;

			return sqlBuilder.BuildTableName(
				new StringBuilder(),
				serverName   == null ? null : escaped ? sqlBuilder.ConvertInline(serverName,   ConvertType.NameToServer)    : serverName,
				databaseName == null ? null : escaped ? sqlBuilder.ConvertInline(databaseName, ConvertType.NameToDatabase)  : databaseName,
				schemaName   == null ? null : escaped ? sqlBuilder.ConvertInline(schemaName,   ConvertType.NameToSchema)    : schemaName,
											  escaped ? sqlBuilder.ConvertInline(tableName,    ConvertType.NameToQueryTable): tableName)
			.ToString();
		}

		#region ProviderSpecific Support

		protected void TraceAction(DataConnection dataConnection, Func<string> commandText, Func<int> action)
		{
			_ = TraceActionAsync(dataConnection, commandText, () => Task.FromResult(action()));
		}

		protected async Task TraceActionAsync(DataConnection dataConnection, Func<string> commandText, Func<Task<int>> action)
		{
			var now = DateTime.UtcNow;
			var sw = Stopwatch.StartNew();

			if (dataConnection.TraceSwitchConnection.TraceInfo)
			{
				dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.BeforeExecute)
				{
					TraceLevel = TraceLevel.Info,
					CommandText = commandText(),
					StartTime = now,
				});
			}

			try
			{
				var count = await action().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);

				if (dataConnection.TraceSwitchConnection.TraceInfo)
				{
					dataConnection.OnTraceConnection(new TraceInfo(dataConnection, TraceInfoStep.AfterExecute)
					{
						TraceLevel = TraceLevel.Info,
						CommandText = commandText(),
						StartTime = now,
						ExecutionTime = sw.Elapsed,
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
						TraceLevel = TraceLevel.Error,
						CommandText = commandText(),
						StartTime = now,
						ExecutionTime = sw.Elapsed,
						Exception = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region MultipleRows Support

		protected BulkCopyRowsCopied MultipleRowsCopy1<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			=> MultipleRowsCopy1(new MultipleRowsHelper<T>(table, options), source);

		protected BulkCopyRowsCopied MultipleRowsCopy1(MultipleRowsHelper helper, IEnumerable source)
		{
			MultipleRowsCopy1Prep(helper);

			foreach (var item in source)
			{
				MultipleRowsCopy1Add(helper, item!);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy1Finish(helper);
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy1Finish(helper);
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source)
			=> MultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source);

		protected async Task<BulkCopyRowsCopied> MultipleRowsCopy1Async(MultipleRowsHelper helper, IEnumerable source)
		{
			MultipleRowsCopy1Prep(helper);

			foreach (var item in source)
			{
				MultipleRowsCopy1Add(helper, item!);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy1Finish(helper);
					if (!await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy1Finish(helper);
				await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}

#if !NET45 && !NET46
		protected Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source)
			=> MultipleRowsCopy1Async(new MultipleRowsHelper<T>(table, options), source);

		protected async Task<BulkCopyRowsCopied> MultipleRowsCopy1Async<T>(MultipleRowsHelper helper, IAsyncEnumerable<T> source)
		{
			MultipleRowsCopy1Prep(helper);

			await foreach (var item in source.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
			{
				MultipleRowsCopy1Add(helper, item!);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy1Finish(helper);
					if (!await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy1Finish(helper);
				await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}
#endif

		private void MultipleRowsCopy1Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("\t");
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(",");
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.Append(")");

			helper.StringBuilder
				.AppendLine()
				.Append("VALUES");

			helper.SetHeader();
		}

		private void MultipleRowsCopy1Add(MultipleRowsHelper helper, object item)
		{
			helper.StringBuilder
				.AppendLine()
				.Append("(");
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
			=> MultipleRowsCopy2(new MultipleRowsHelper<T>(table, options), source, from);

		protected BulkCopyRowsCopied MultipleRowsCopy2(
			MultipleRowsHelper helper, IEnumerable source, string from)
		{
			MultipleRowsCopy2Prep(helper);

			foreach (var item in source)
			{
				MultipleRowsCopy2Add(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy2Finish(helper);
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy2Finish(helper);
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(ITable<T> table, BulkCopyOptions options, IEnumerable<T> source, string from)
			=> MultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, from);

		protected async Task<BulkCopyRowsCopied> MultipleRowsCopy2Async(
			MultipleRowsHelper helper, IEnumerable source, string from)
		{
			MultipleRowsCopy2Prep(helper);

			foreach (var item in source)
			{
				MultipleRowsCopy2Add(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy2Finish(helper);
					if (!await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy2Finish(helper);
				await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}

#if !NET45 && !NET46
		protected Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(ITable<T> table, BulkCopyOptions options, IAsyncEnumerable<T> source, string from)
			=> MultipleRowsCopy2Async(new MultipleRowsHelper<T>(table, options), source, from);

		protected async Task<BulkCopyRowsCopied> MultipleRowsCopy2Async<T>(
			MultipleRowsHelper helper, IAsyncEnumerable<T> source, string from)
		{
			MultipleRowsCopy2Prep(helper);

			await foreach (var item in source)
			{
				MultipleRowsCopy2Add(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy2Finish(helper);
					if (!await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy2Finish(helper);
				await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}
#endif

		private void MultipleRowsCopy2Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("\t");
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(",");
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.Append(")");

			helper.SetHeader();
		}

		private void MultipleRowsCopy2Add(MultipleRowsHelper helper, object item, string from)
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

		protected BulkCopyRowsCopied MultipleRowsCopy3(
			MultipleRowsHelper helper, BulkCopyOptions options, IEnumerable source, string from)
		{
			MultipleRowsCopy3Prep(helper);

			foreach (var item in source)
			{
				MultipleRowsCopy3Add(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy3Finish(helper);
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy3Finish(helper);
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		protected async Task<BulkCopyRowsCopied> MultipleRowsCopy3Async(
			MultipleRowsHelper helper, BulkCopyOptions options, IEnumerable source, string from)
		{
			MultipleRowsCopy3Prep(helper);

			foreach (var item in source)
			{
				MultipleRowsCopy3Add(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy3Finish(helper);
					if (!await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy3Finish(helper);
				await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}

#if !NET45 && !NET46
		protected async Task<BulkCopyRowsCopied> MultipleRowsCopy3Async<T>(
			MultipleRowsHelper helper, BulkCopyOptions options, IAsyncEnumerable<T> source, string from)
		{
			MultipleRowsCopy3Prep(helper);

			await foreach (var item in source)
			{
				MultipleRowsCopy3Add(helper, item!, from);

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					MultipleRowsCopy3Finish(helper);
					if (!await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				MultipleRowsCopy3Finish(helper);
				await helper.ExecuteAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}

			return helper.RowsCopied;
		}
#endif

		private void MultipleRowsCopy3Prep(MultipleRowsHelper helper)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("\t");
				helper.SqlBuilder.Convert(helper.StringBuilder, column.ColumnName, ConvertType.NameToQueryField);
				helper.StringBuilder.Append(",");
			}

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.AppendLine(")")
				.AppendLine("SELECT * FROM")
				.Append("(");

			helper.SetHeader();
		}

		private void MultipleRowsCopy3Add(MultipleRowsHelper helper, object item, string from)
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
				.Append(")");
		}

		#endregion
	}
}
