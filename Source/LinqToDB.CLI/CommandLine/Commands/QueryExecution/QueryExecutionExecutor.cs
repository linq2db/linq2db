using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using FirebirdSql.Data.Types;

using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.MySql;
using LinqToDB.Internal.DataProvider.PostgreSQL;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SqlServer;

using Microsoft.Data.SqlTypes;
using Microsoft.SqlServer.Types;

using NpgsqlTypes;

using Oracle.ManagedDataAccess.Types;

using QueryActualFieldType = LinqToDB.CommandLine.Commands.QueryExecution.QueryValueFormatter.QueryActualFieldType;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Query command execution logic.
	/// </summary>
	public sealed class QueryExecutionExecutor
	{
		sealed record QueryOutputColumn(int Ordinal, string Name, Type FieldType, string FieldTypeName, string ProviderSpecificFieldType, string DataTypeName, QueryActualFieldType ActualFieldType);

		readonly record struct QueryRowReadResult(string?[]? Row)
		{
			public bool IsTruncated => Row == null;
		}

		abstract class QueryRowReader
		{
			public abstract ValueTask<QueryRowReadResult> ReadRow(DbDataReader reader, QueryOutputColumn[] columns, CancellationToken cancellationToken);
		}

		sealed class StreamingQueryRowReader : QueryRowReader
		{
			public override async ValueTask<QueryRowReadResult> ReadRow(DbDataReader reader, QueryOutputColumn[] columns, CancellationToken cancellationToken)
			{
				var row = new string?[columns.Length];

				for (var i = 0; i < columns.Length; i++)
				{
					switch (columns[i].ActualFieldType)
					{
						// Oracle BFILE is an external file locator. Even IsDBNull can trigger a file/LOB
						// operation, so avoid reader value APIs for it.
						//
						case QueryActualFieldType.OracleBFile:
							row[i] = QueryValueFormatter.OracleBFilePlaceholder;
							continue;

						// MySQL wide DECIMAL values can overflow inside regular reader null checks.
						// The native GetMySqlDecimal path does its own best-effort null handling.
						//
						case QueryActualFieldType.MySqlDecimal:
							row[i] = ReadFieldAsString(reader, columns[i].ActualFieldType, i);
							continue;
					}

					if (await reader.IsDBNullAsync(i, cancellationToken))
						continue;

					row[i] = ReadFieldAsString(reader, columns[i].ActualFieldType, i);
				}

				return new QueryRowReadResult(row);
			}
		}

		sealed class BoundedQueryRowReader(int maxOutputBytes) : QueryRowReader
		{
			const int BufferSize = 8192;

			public override async ValueTask<QueryRowReadResult> ReadRow(DbDataReader reader, QueryOutputColumn[] columns, CancellationToken cancellationToken)
			{
				var row            = new string?[columns.Length];
				var remainingBytes = maxOutputBytes;

				for (var i = 0; i < columns.Length; i++)
				{
					var column = columns[i];

					switch (column.ActualFieldType)
					{
						case QueryActualFieldType.OracleBFile:
							row[i] = QueryValueFormatter.OracleBFilePlaceholder;
							break;
						case QueryActualFieldType.MySqlDecimal:
							row[i] = ReadMySqlDecimalAsString(reader, i);
							break;
						default:
							if (await reader.IsDBNullAsync(i, cancellationToken))
								continue;

							if (IsBinaryField(column))
							{
								var binary = await ReadBinary(reader, i, GetBinarySourceLimit(column.ActualFieldType, remainingBytes), cancellationToken);

								if (binary == null)
									return default;

								row[i] = QueryValueFormatter.Format(binary, column.DataTypeName, column.ActualFieldType);
							}
							else if (IsTextField(column))
							{
								row[i] = await ReadText(reader, i, remainingBytes, cancellationToken);

								if (row[i] == null)
									return default;
							}
							else
								row[i] = ReadFieldAsString(reader, column.ActualFieldType, i);
							break;
					}

					if (row[i] is { } fieldValue)
					{
						remainingBytes -= Encoding.UTF8.GetByteCount(fieldValue);

						if (remainingBytes < 0)
							return default;
					}
				}

				return new QueryRowReadResult(row);

				static bool IsBinaryField(QueryOutputColumn column)
				{
					return column.FieldType == typeof(byte[])
						|| typeof(Stream).IsAssignableFrom(column.FieldType)
						|| column.ActualFieldType is
							QueryActualFieldType.Bytes
							or QueryActualFieldType.ByteArray
							or QueryActualFieldType.SqlBinary
							or QueryActualFieldType.SqlBytes
							or QueryActualFieldType.OracleBinary
							or QueryActualFieldType.OracleBlob
							or QueryActualFieldType.DB2Binary
							or QueryActualFieldType.DB2Blob;
				}

				static bool IsTextField(QueryOutputColumn column)
				{
					return column.FieldType == typeof(string)
						|| typeof(TextReader).IsAssignableFrom(column.FieldType)
						|| column.ActualFieldType is
							QueryActualFieldType.SqlChars
							or QueryActualFieldType.SqlString
							or QueryActualFieldType.SqlXml
							or QueryActualFieldType.OracleClob
							or QueryActualFieldType.OracleXmlType
							or QueryActualFieldType.DB2Clob
							or QueryActualFieldType.DB2Xml;
				}

				static int GetBinarySourceLimit(QueryActualFieldType actualFieldType, int remainingBytes)
				{
					return actualFieldType == QueryActualFieldType.ByteArray
						? remainingBytes / 4
						: Math.Max(0, (remainingBytes - 2) / 2);
				}

				static async Task<byte[]?> ReadBinary(DbDataReader reader, int ordinal, int sourceLimit, CancellationToken cancellationToken)
				{
					try
					{
						await using var stream = reader.GetStream(ordinal);

						return await ReadStream(stream, sourceLimit, cancellationToken);
					}
					catch (Exception exception) when (IsUnsupportedSequentialRead(exception))
					{
						try
						{
							return await ReadReader(reader, ordinal, sourceLimit, cancellationToken);
						}
						catch (Exception fallbackException) when (IsUnsupportedSequentialRead(fallbackException))
						{
							return null;
						}
					}

					static async Task<byte[]?> ReadStream(Stream stream, int sourceLimit, CancellationToken cancellationToken)
					{
						using var result = new MemoryStream(Math.Min(sourceLimit, BufferSize));
						var buffer = new byte[Math.Min(sourceLimit, BufferSize - 1) + 1];

						while (true)
						{
							var readSize = (int)Math.Min(buffer.Length, (long)sourceLimit - result.Length + 1);
							var read     = await stream.ReadAsync(buffer.AsMemory(0, readSize), cancellationToken);

							if (read == 0)
								return result.ToArray();

							if (result.Length + read > sourceLimit)
								return null;

							await result.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
						}
					}

					static async Task<byte[]?> ReadReader(DbDataReader reader, int ordinal, int sourceLimit, CancellationToken cancellationToken)
					{
						using var result = new MemoryStream(Math.Min(sourceLimit, BufferSize));
						var buffer = new byte[Math.Min(sourceLimit, BufferSize - 1) + 1];
						var offset = 0L;

						while (true)
						{
							cancellationToken.ThrowIfCancellationRequested();

							var readSize = (int)Math.Min(buffer.Length, (long)sourceLimit - result.Length + 1);
							var read     = reader.GetBytes(ordinal, offset, buffer, 0, readSize);

							if (read == 0)
								return result.ToArray();

							if (result.Length + read > sourceLimit)
								return null;

							await result.WriteAsync(buffer.AsMemory(0, (int)read), cancellationToken);
							offset += read;
						}
					}
				}

				static async Task<string?> ReadText(DbDataReader reader, int ordinal, int byteLimit, CancellationToken cancellationToken)
				{
					try
					{
						using var textReader = reader.GetTextReader(ordinal);

						return await ReadTextReader(textReader, byteLimit, cancellationToken);
					}
					catch (Exception exception) when (IsUnsupportedSequentialRead(exception))
					{
						try
						{
							return await ReadReader(reader, ordinal, byteLimit, cancellationToken);
						}
						catch (Exception fallbackException) when (IsUnsupportedSequentialRead(fallbackException))
						{
							return null;
						}
					}

					static async Task<string?> ReadTextReader(TextReader reader, int byteLimit, CancellationToken cancellationToken)
					{
						var result = new StringBuilder(Math.Min(byteLimit, BufferSize));
						var buffer = new char[Math.Min(byteLimit, BufferSize - 1) + 1];
						var bytes  = 0;

						while (true)
						{
							var read = await reader.ReadAsync(buffer.AsMemory(), cancellationToken);

							if (read == 0)
								return result.ToString();

							bytes += Encoding.UTF8.GetByteCount(buffer, 0, read);

							if (bytes > byteLimit)
								return null;

							result.Append(buffer, 0, read);
						}
					}

					static async Task<string?> ReadReader(DbDataReader reader, int ordinal, int byteLimit, CancellationToken cancellationToken)
					{
						var result = new StringBuilder(Math.Min(byteLimit, BufferSize));
						var buffer = new char[Math.Min(byteLimit, BufferSize - 1) + 1];
						var offset = 0L;
						var bytes  = 0;

						while (true)
						{
							cancellationToken.ThrowIfCancellationRequested();

							var read = reader.GetChars(ordinal, offset, buffer, 0, buffer.Length);

							if (read == 0)
								return result.ToString();

							bytes += Encoding.UTF8.GetByteCount(buffer, 0, (int)read);

							if (bytes > byteLimit)
								return null;

							result.Append(buffer, 0, (int)read);
							offset += read;
						}
					}
				}

				static bool IsUnsupportedSequentialRead(Exception exception)
				{
					return exception is NotSupportedException or InvalidCastException;
				}
			}
		}

		abstract class QueryOutputSegmentWriter(TextWriter outputWriter)
		{
			protected TextWriter OutputWriter { get; } = outputWriter;

			public abstract Task<bool> Write(Func<TextWriter, Task> write, int reservedBytes, CancellationToken cancellationToken);
		}

		sealed class StreamingQueryOutputSegmentWriter(TextWriter outputWriter) : QueryOutputSegmentWriter(outputWriter)
		{
			public override async Task<bool> Write(Func<TextWriter, Task> write, int reservedBytes, CancellationToken cancellationToken)
			{
				await write(OutputWriter);
				return true;
			}
		}

		sealed class BoundedQueryOutputSegmentWriter(TextWriter outputWriter, int maxOutputBytes) : QueryOutputSegmentWriter(outputWriter)
		{
			int _outputBytes;

			public override async Task<bool> Write(Func<TextWriter, Task> write, int reservedBytes, CancellationToken cancellationToken)
			{
				using var segmentWriter = new StringWriter(CultureInfo.InvariantCulture);

				await write(segmentWriter);

				var segment      = segmentWriter.ToString();
				var segmentBytes = Encoding.UTF8.GetByteCount(segment);

				if ((long)_outputBytes + segmentBytes + reservedBytes > maxOutputBytes)
					return false;

				await OutputWriter.WriteAsync(segment.AsMemory(), cancellationToken);
				_outputBytes += segmentBytes;

				return true;
			}
		}

		readonly QueryExecutionSettings _settings;

		internal QueryExecutionExecutor(QueryExecutionSettings settings)
		{
			_settings = settings;
		}

		internal async ValueTask<QueryExecutionResult> Execute(TextWriter outputWriter, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var sql = _settings.Sql;

			try
			{
				var result = await ConnectionExecution.RunAsync(
					new ConnectionSettings(
						_settings.Profile,
						_settings.Provider,
						_settings.ProviderLocation,
						_settings.User,
						_settings.Password,
						_settings.ConnectionString,
						_settings.CommandTimeout,
						_settings.LockTimeout,
						null,
						_settings.Impersonate,
						_settings.ImpersonateMode,
						null),
					(dataOptions, dataProvider, token) => ExecuteValidatedDatabaseLoop(dataOptions, dataProvider, sql, outputWriter, token),
					cancellationToken);

				if (result.Error != null)
					return new QueryExecutionResult(result.StatusCode, result.Error, false);

				return result.Value!;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				return new QueryExecutionResult(StatusCodes.EXPECTED_ERROR, $"SQL execution failed: {ex.Message}", false);
			}
		}

		async Task<QueryExecutionResult> ExecuteValidatedDatabaseLoop(DataOptions dataOptions, IDataProvider dataProvider, string sql, TextWriter outputWriter, CancellationToken cancellationToken)
		{
			var singleStatementResult = ReadOnlySqlGuard.ValidateSingleStatement(dataProvider, sql);

			if (!singleStatementResult.IsAllowed)
				return new QueryExecutionResult(StatusCodes.EXPECTED_ERROR, singleStatementResult.Error, false);

			if (_settings.Mode == QueryExecutionMode.Query)
			{
				var guardResult = ReadOnlySqlGuard.Validate(dataProvider, sql);

				if (!guardResult.IsAllowed)
					return new QueryExecutionResult(StatusCodes.EXPECTED_ERROR, guardResult.Error, false);
			}
			else
			{
				await _settings.DiagnosticWriter.WriteLineAsync(
					string.Create(
						CultureInfo.InvariantCulture,
						$"Executing write-capable SQL because profile '{_settings.Profile}' has enableExecute=true. Provider: {_settings.Provider}."));
			}

			return await ExecuteDatabaseLoop(dataOptions, dataProvider, sql, outputWriter, cancellationToken);
		}

		async Task<QueryExecutionResult> ExecuteDatabaseLoop(DataOptions dataOptions, IDataProvider dataProvider, string sql, TextWriter outputWriter, CancellationToken cancellationToken)
		{
			// Open a connection and apply optional provider-specific session setup before user SQL execution.
			//
			var dataConnection = new DataConnection(dataOptions);
			DataReaderAsync? dataReader = null;

			try
			{
				var lockTimeoutCommand = GetLockTimeoutCommand(dataProvider, _settings.LockTimeout);

				if (lockTimeoutCommand != null)
					await dataConnection.ExecuteAsync(lockTimeoutCommand, cancellationToken);

				// Execute user-provided SQL and get a data reader for the result set.
				//
				dataReader = await dataConnection.ExecuteReaderAsync(sql, cancellationToken);

				var reader = dataReader.Reader;

				if (reader == null)
					return new QueryExecutionResult(StatusCodes.EXPECTED_ERROR, "SQL execution didn't return a data reader.", false);

				// Read the column metadata from the data reader and create output column definitions.
				//
				var columns = ReadOutputColumns(reader);

				// Validate that the output format is compatible with the column metadata.
				//
				if (string.Equals(_settings.Output, "json", StringComparison.OrdinalIgnoreCase))
				{
					var duplicateColumnName = GetDuplicateColumnName(columns);

					if (duplicateColumnName != null)
						return new QueryExecutionResult(StatusCodes.EXPECTED_ERROR, $"JSON output requires unique column names. Duplicate column name '{duplicateColumnName}' found. Use explicit SQL aliases for duplicate columns or switch to json-table output when duplicate-safe column metadata is needed.", false);
				}

				var rowCount  = 0;
				var truncated = false;
				QueryTruncationReason? truncationReason = null;

				var footerReserveBytes = 0;
				var rowReader          = _settings.MaxOutputBytes is { } maxOutputBytes
					? (QueryRowReader)new BoundedQueryRowReader(maxOutputBytes)
					: new StreamingQueryRowReader();
				var segmentWriter      = _settings.MaxOutputBytes is { } outputLimit
					? (QueryOutputSegmentWriter)new BoundedQueryOutputSegmentWriter(outputWriter, outputLimit)
					: new StreamingQueryOutputSegmentWriter(outputWriter);

				if (_settings.MaxOutputBytes != null)
				{
					using var footerWriter = new StringWriter(CultureInfo.InvariantCulture);

					await (_settings.Output switch
					{
						"json-table" => WriteJsonTableEnd(
							footerWriter,
							int.MaxValue,
							true,
							QueryTruncationReason.MaxOutputBytes,
							_settings.MaxOutputBytes,
							int.MinValue,
							cancellationToken),
						"csv"        => Task.CompletedTask,
						_            => footerWriter.WriteAsync("]".AsMemory(), cancellationToken),
					});

					footerReserveBytes = Encoding.UTF8.GetByteCount(footerWriter.ToString());
				}

				// Write the output header based on the specified output format.
				//
				var headerWritten = await (_settings.Output switch
				{
					"csv"        => segmentWriter.Write(
						writer => WriteCsvHeader(writer, columns, cancellationToken),
						footerReserveBytes,
						cancellationToken),
					"json-table" => segmentWriter.Write(
						writer => WriteJsonTableStart(writer, columns, cancellationToken),
						footerReserveBytes,
						cancellationToken),
					_            => segmentWriter.Write(
						writer => writer.WriteAsync("[".AsMemory(), cancellationToken),
						footerReserveBytes,
						cancellationToken),
				});

				if (!headerWritten)
					return new QueryExecutionResult(
						StatusCodes.EXPECTED_ERROR,
						string.Create(CultureInfo.InvariantCulture, $"Query output metadata exceeds the configured maximum output size of {_settings.MaxOutputBytes} bytes."),
						false);

				while (await reader.ReadAsync(cancellationToken))
				{
					// Stop reading after the configured row limit and report truncation.
					//
					if (_settings.MaxRows > 0 && rowCount >= _settings.MaxRows)
					{
						truncated        = true;
						truncationReason = QueryTruncationReason.MaxRows;
						break;
					}

					// Read one result row as normalized string values.
					//
					var rowResult = await rowReader.ReadRow(reader, columns, cancellationToken);

					if (rowResult.IsTruncated)
					{
						truncated        = true;
						truncationReason = QueryTruncationReason.MaxOutputBytes;
						break;
					}

					var row = rowResult.Row!;

					// Write the row using the selected output format.
					//
					var rowWritten = _settings.Output switch
					{
						"csv"        => await segmentWriter.Write(writer => WriteCsvRow      (writer, row,                    cancellationToken), footerReserveBytes, cancellationToken),
						"json-table" => await segmentWriter.Write(writer => WriteJsonTableRow(writer, row,          rowCount, cancellationToken), footerReserveBytes, cancellationToken),
						_            => await segmentWriter.Write(writer => WriteJsonRow     (writer, columns, row, rowCount, cancellationToken), footerReserveBytes, cancellationToken),
					};

					if (!rowWritten)
					{
						truncated        = true;
						truncationReason = QueryTruncationReason.MaxOutputBytes;
						break;
					}

					rowCount++;
				}

				var recordsAffected = GetRecordsAffected(reader);

				// Close the selected output format.
				//
				var footerWritten = await (_settings.Output switch
				{
					"json-table" => segmentWriter.Write(
						writer => WriteJsonTableEnd(writer, rowCount, truncated, truncationReason, _settings.MaxOutputBytes, recordsAffected, cancellationToken),
						0,
						cancellationToken),
					"csv"        => Task.FromResult(true),
					_            => segmentWriter.Write(
						writer => writer.WriteAsync("]".AsMemory(), cancellationToken),
						0,
						cancellationToken),
				});

				if (!footerWritten)
					return new QueryExecutionResult(
						StatusCodes.EXPECTED_ERROR,
						string.Create(CultureInfo.InvariantCulture, $"Query output footer exceeds the configured maximum output size of {_settings.MaxOutputBytes} bytes."),
						false);

				await outputWriter.FlushAsync(cancellationToken);

				return new QueryExecutionResult(StatusCodes.SUCCESS, null, truncated, rowCount, truncationReason);
			}
			finally
			{
				try
				{
					if (dataReader != null)
						await dataReader.DisposeAsync();
				}
				finally
				{
					await dataConnection.DisposeAsync();
				}
			}
		}

		QueryOutputColumn[] ReadOutputColumns(DbDataReader reader)
		{
			var columns = new QueryOutputColumn[reader.FieldCount];

			for (var i = 0; i < columns.Length; i++)
			{
				Type? providerSpecificType;

				try
				{
					providerSpecificType = reader.GetProviderSpecificFieldType(i);
				}
				catch (NotSupportedException)
				{
					providerSpecificType = reader.GetFieldType(i);
				}

				providerSpecificType ??= reader.GetFieldType(i) ?? typeof(object);

				columns[i] = CreateOutputColumn(reader, i, providerSpecificType);
			}

			return columns;
		}

		static string? GetLockTimeoutCommand(IDataProvider dataProvider, int? timeout)
		{
			if (timeout is null or <= 0)
				return null;

			return dataProvider switch
			{
				SqlServerDataProvider  => string.Create(CultureInfo.InvariantCulture, $"SET LOCK_TIMEOUT {(long)timeout.Value * 1000}"),
				PostgreSQLDataProvider => string.Create(CultureInfo.InvariantCulture, $"SET lock_timeout = '{timeout.Value}s'"),
				MySqlDataProvider      => string.Create(CultureInfo.InvariantCulture, $"SET SESSION innodb_lock_wait_timeout = {timeout.Value}"),
				SQLiteDataProvider     => string.Create(CultureInfo.InvariantCulture, $"PRAGMA busy_timeout = {(long)timeout.Value * 1000}"),
				_                      => null,
			};
		}

		static string? GetDuplicateColumnName(QueryOutputColumn[] columns)
		{
			var columnNames = new HashSet<string>(StringComparer.Ordinal);

			foreach (var column in columns)
			{
				if (!columnNames.Add(column.Name))
					return column.Name;
			}

			return null;
		}

		static int? GetRecordsAffected(DbDataReader reader)
		{
			try
			{
				var recordsAffected = reader.RecordsAffected;

				return recordsAffected == -1 ? null : recordsAffected;
			}
			catch (NotSupportedException)
			{
				return null;
			}
		}

		static async Task WriteJsonRow(TextWriter output, QueryOutputColumn[] columns, string?[] row, int rowIndex, CancellationToken cancellationToken)
		{
			if (rowIndex > 0)
				await output.WriteAsync(",".AsMemory(), cancellationToken);

			await output.WriteAsync("{".AsMemory(), cancellationToken);

			for (var i = 0; i < columns.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken);

				await WriteJsonString(output, columns[i].Name, cancellationToken);
				await output.WriteAsync(":".AsMemory(), cancellationToken);
				await WriteJsonValue(output, row[i], cancellationToken);
			}

			await output.WriteAsync("}".AsMemory(), cancellationToken);
		}

		static async Task WriteJsonTableStart(TextWriter output, QueryOutputColumn[] columns, CancellationToken cancellationToken)
		{
			await output.WriteAsync("{\"columns\":[".AsMemory(), cancellationToken);

			for (var i = 0; i < columns.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken);

				await output.WriteAsync("{\"ordinal\":".AsMemory(),   cancellationToken);
				await output.WriteAsync(columns[i].Ordinal.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken);
				await output.WriteAsync(",\"name\":".AsMemory(),      cancellationToken);
				await WriteJsonString  (output, columns[i].Name,      cancellationToken);
				await output.WriteAsync(",\"fieldType\":".AsMemory(), cancellationToken);
				await WriteJsonString  (output, columns[i].FieldTypeName, cancellationToken);
				await output.WriteAsync(",\"providerSpecificFieldType\":".AsMemory(), cancellationToken);
				await WriteJsonString  (output, columns[i].ProviderSpecificFieldType, cancellationToken);
				await output.WriteAsync(",\"dataTypeName\":".AsMemory(), cancellationToken);
				await WriteJsonString  (output, columns[i].DataTypeName, cancellationToken);
				await output.WriteAsync("}".AsMemory(), cancellationToken);
			}

			await output.WriteAsync("],\"rows\":[".AsMemory(), cancellationToken);
		}

		static async Task WriteJsonTableRow(TextWriter output, string?[] row, int rowIndex, CancellationToken cancellationToken)
		{
			if (rowIndex > 0)
				await output.WriteAsync(",".AsMemory(), cancellationToken);

			await output.WriteAsync("[".AsMemory(), cancellationToken);

			for (var i = 0; i < row.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken);

				await WriteJsonValue(output, row[i], cancellationToken);
			}

			await output.WriteAsync("]".AsMemory(), cancellationToken);
		}

		static async Task WriteJsonTableEnd(
			TextWriter             output,
			int                    rowCount,
			bool                   truncated,
			QueryTruncationReason? truncationReason,
			int?                   maxOutputBytes,
			int?                   recordsAffected,
			CancellationToken      cancellationToken)
		{
			await output.WriteAsync("],\"rowCount\":".AsMemory(), cancellationToken);
			await output.WriteAsync(rowCount.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken);
			await output.WriteAsync(",\"truncated\":".AsMemory(), cancellationToken);
			await output.WriteAsync((truncated ? "true" : "false").AsMemory(), cancellationToken);

			if (truncationReason != null)
			{
				await output.WriteAsync(",\"truncationReason\":".AsMemory(), cancellationToken);
				await WriteJsonString(
					output,
					truncationReason == QueryTruncationReason.MaxRows ? "maxRows" : "maxOutputBytes",
					cancellationToken);
			}

			if (truncationReason == QueryTruncationReason.MaxOutputBytes && maxOutputBytes != null)
			{
				await output.WriteAsync(",\"maxOutputBytes\":".AsMemory(), cancellationToken);
				await output.WriteAsync(maxOutputBytes.Value.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken);
			}

			if (recordsAffected != null)
			{
				await output.WriteAsync(",\"recordsAffected\":".AsMemory(), cancellationToken);
				await output.WriteAsync(recordsAffected.Value.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken);
			}

			await output.WriteAsync("}".AsMemory(), cancellationToken);
		}

		static async Task WriteCsvHeader(TextWriter output, QueryOutputColumn[] columns, CancellationToken cancellationToken)
		{
			for (var i = 0; i < columns.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken);

				await WriteCsvValue(output, columns[i].Name, cancellationToken);
			}

			await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken);
		}

		static async Task WriteCsvRow(TextWriter output, string?[] row, CancellationToken cancellationToken)
		{
			for (var i = 0; i < row.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken);

				if (row[i] != null)
					await WriteCsvValue(output, row[i]!, cancellationToken);
			}

			await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken);
		}

		static QueryOutputColumn CreateOutputColumn(DbDataReader reader, int ordinal, Type providerSpecificType)
		{
			var fieldType    = reader.GetFieldType(ordinal) ?? typeof(object);
			var dataTypeName = reader.GetDataTypeName(ordinal);

			var actualFieldType = providerSpecificType switch
			{
				_ when providerSpecificType == typeof(bool)               => QueryActualFieldType.Boolean,
				_ when providerSpecificType == typeof(SqlBoolean)         => QueryActualFieldType.Boolean,
				_ when providerSpecificType == typeof(double)             => QueryActualFieldType.Double,
				_ when providerSpecificType == typeof(SqlDouble)          => QueryActualFieldType.Double,
				_ when providerSpecificType == typeof(float)              => QueryActualFieldType.Single,
				_ when providerSpecificType == typeof(SqlSingle)          => QueryActualFieldType.Single,
				_ when providerSpecificType == typeof(DateTime) && IsDateDataType(dataTypeName) => QueryActualFieldType.Date,
				_ when providerSpecificType == typeof(DateOnly)           => QueryActualFieldType.Date,
				_ when providerSpecificType == typeof(DateTime)           => QueryActualFieldType.DateTime,
				_ when providerSpecificType == typeof(SqlDateTime)        => QueryActualFieldType.DateTime,
				_ when providerSpecificType == typeof(DateTimeOffset)     => QueryActualFieldType.DateTimeOffset,
				_ when providerSpecificType == typeof(TimeSpan)           => QueryActualFieldType.TimeSpan,
				_ when providerSpecificType == typeof(Guid)               => QueryActualFieldType.Guid,
				_ when providerSpecificType == typeof(SqlGuid)            => QueryActualFieldType.Guid,
				_ when providerSpecificType == typeof(byte[]) && dataTypeName.StartsWith("Array(", StringComparison.OrdinalIgnoreCase) => QueryActualFieldType.ByteArray,
				_ when providerSpecificType == typeof(byte[])             => QueryActualFieldType.Bytes,
				_ when providerSpecificType == typeof(SqlBinary)          => QueryActualFieldType.SqlBinary,
				_ when providerSpecificType == typeof(SqlBytes)           => QueryActualFieldType.SqlBytes,
				_ when providerSpecificType == typeof(SqlChars)           => QueryActualFieldType.SqlChars,
				_ when providerSpecificType == typeof(SqlString)          => QueryActualFieldType.SqlString,
				_ when providerSpecificType == typeof(SqlXml)             => QueryActualFieldType.SqlXml,
				_ when providerSpecificType == typeof(SqlVector<float>)   => QueryActualFieldType.SqlVectorFloat,
				_ when providerSpecificType == typeof(SqlVector<Half>)    => QueryActualFieldType.SqlVectorHalf,
				_ when providerSpecificType == typeof(SqlHierarchyId)     => QueryActualFieldType.SqlHierarchyId,
				_ when providerSpecificType == typeof(SqlGeometry)        => QueryActualFieldType.SqlGeometry,
				_ when providerSpecificType == typeof(SqlGeography)       => QueryActualFieldType.SqlGeography,
				_ when providerSpecificType == typeof(OracleBinary)       => QueryActualFieldType.OracleBinary,
				_ when providerSpecificType == typeof(OracleBlob)         => QueryActualFieldType.OracleBlob,
				_ when providerSpecificType == typeof(OracleBFile)        => QueryActualFieldType.OracleBFile,
				_ when providerSpecificType == typeof(OracleClob)         => QueryActualFieldType.OracleClob,
				_ when providerSpecificType == typeof(OracleXmlType)      => QueryActualFieldType.OracleXmlType,
				_ when providerSpecificType == typeof(OracleDate)         => QueryActualFieldType.OracleDate,
				_ when providerSpecificType == typeof(OracleTimeStamp)    => QueryActualFieldType.OracleTimeStamp,
				_ when providerSpecificType == typeof(OracleTimeStampTZ)  => QueryActualFieldType.OracleTimeStampTZ,
				_ when providerSpecificType == typeof(OracleTimeStampLTZ) => QueryActualFieldType.OracleTimeStampLTZ,
				_ when providerSpecificType == typeof(FbDecFloat)         => QueryActualFieldType.FirebirdDecFloat,
				_ when providerSpecificType == typeof(FbZonedDateTime)    => QueryActualFieldType.FirebirdZonedDateTime,
				_ when providerSpecificType == typeof(FbZonedTime)        => QueryActualFieldType.FirebirdZonedTime,
				_ when providerSpecificType.IsGenericType && providerSpecificType.GetGenericTypeDefinition() == typeof(NpgsqlRange<>) => QueryActualFieldType.NpgsqlRange,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Binary")    => QueryActualFieldType.DB2Binary,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Blob")      => QueryActualFieldType.DB2Blob,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Clob")      => QueryActualFieldType.DB2Clob,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Date")      => QueryActualFieldType.DB2Date,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Time")      => QueryActualFieldType.DB2Time,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2TimeStamp") => QueryActualFieldType.DB2TimeStamp,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Xml")       => QueryActualFieldType.DB2Xml,
				_ when IsMySqlDecimalDataType(dataTypeName) && HasProviderSpecificReaderMethod(reader, "GetMySqlDecimal") => QueryActualFieldType.MySqlDecimal,
				_                                                         => QueryActualFieldType.None,
			};

			return new QueryOutputColumn(
				ordinal,
				reader.GetName(ordinal),
				fieldType,
				fieldType.FullName ?? fieldType.Name,
				providerSpecificType.FullName ?? providerSpecificType.Name,
				dataTypeName,
				actualFieldType);
		}

		/// <summary>
		/// Reads and formats a field value for query output.
		/// </summary>
		/// <param name="reader">Data reader.</param>
		/// <param name="actualFieldType">Field value conversion mode.</param>
		/// <param name="ordinal">Zero-based column ordinal.</param>
		/// <returns>Formatted field value, or <see langword="null"/> for a database null.</returns>
		public static string? ReadFieldAsString(DbDataReader reader, QueryActualFieldType actualFieldType, int ordinal)
		{
			object value;

			switch (actualFieldType)
			{
				case QueryActualFieldType.OracleBFile : return QueryValueFormatter.OracleBFilePlaceholder;
				case QueryActualFieldType.MySqlDecimal: return ReadMySqlDecimalAsString(reader, ordinal);
			}

			try
			{
				value = reader.GetProviderSpecificValue(ordinal);
			}
			catch (Exception providerSpecificException) when (providerSpecificException is not OperationCanceledException)
			{
				try
				{
					value = reader.GetValue(ordinal);
				}
				catch (Exception getValueException) when (getValueException is not OperationCanceledException)
				{
					ExceptionDispatchInfo.Capture(providerSpecificException).Throw();
					throw;
				}
			}

			return QueryValueFormatter.Format(value, reader.GetDataTypeName(ordinal), actualFieldType);
		}

		static bool IsDateDataType(string dataTypeName)
		{
			return string.Equals(dataTypeName, "Date",             StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Date32",           StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date)",   StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date32)", StringComparison.OrdinalIgnoreCase);
		}

		static bool IsMySqlDecimalDataType(string dataTypeName)
		{
			return string.Equals(dataTypeName, "Decimal",    StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "NewDecimal", StringComparison.OrdinalIgnoreCase);
		}

		static bool HasProviderSpecificReaderMethod(DbDataReader reader, string methodName)
		{
			return reader.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: [typeof(int)], modifiers: null) != null;
		}

		static object GetProviderSpecificReaderMethodValue(DbDataReader reader, int ordinal, string methodName)
		{
			var method = reader.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: [typeof(int)], modifiers: null)
				?? throw new InvalidOperationException($"Provider-specific reader type '{reader.GetType().FullName}' doesn't contain '{methodName}' method.");

			return method.InvokeExt(reader, [ordinal])
				?? throw new InvalidOperationException($"Provider-specific reader type '{reader.GetType().FullName}' method '{methodName}' returned null.");
		}

		static bool IsProviderSpecificType(Type type, string fullName)
		{
			return string.Equals(type.FullName, fullName, StringComparison.Ordinal);
		}

		static string? ReadMySqlDecimalAsString(DbDataReader reader, int ordinal)
		{
			try
			{
				if (reader.IsDBNull(ordinal))
					return null;
			}
			catch (Exception exception) when (exception is not OperationCanceledException)
			{
				// Wide MySQL DECIMAL values can overflow during the regular null check.
				// GetMySqlDecimal below performs its own best-effort null handling.
			}

			return FormatMySqlDecimal(GetProviderSpecificReaderMethodValue(reader, ordinal, "GetMySqlDecimal"));
		}

		static string? FormatMySqlDecimal(object value)
		{
			if (value is DBNull)
				return null;

			var isNullProperty = value.GetType().GetProperty("IsNull", BindingFlags.Public | BindingFlags.Instance);

			if (isNullProperty != null && isNullProperty.GetValue(value) is true)
				return null;

			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		static Task WriteJsonString(TextWriter output, string value, CancellationToken cancellationToken)
		{
			return output.WriteAsync(JsonSerializer.Serialize(value).AsMemory(), cancellationToken);
		}

		static Task WriteJsonValue(TextWriter output, string? value, CancellationToken cancellationToken)
		{
			return value == null
				? output.WriteAsync("null".AsMemory(), cancellationToken)
				: WriteJsonString(output, value, cancellationToken);
		}

		static async Task WriteCsvValue(TextWriter output, string value, CancellationToken cancellationToken)
		{
			if (value.Length > 0 && value.IndexOfAny([',', '"', '\r', '\n']) < 0)
			{
				await output.WriteAsync(value.AsMemory(), cancellationToken);
				return;
			}

			await output.WriteAsync("\"".AsMemory(), cancellationToken);
			await output.WriteAsync(value.Replace("\"", "\"\"", StringComparison.Ordinal).AsMemory(), cancellationToken);
			await output.WriteAsync("\"".AsMemory(), cancellationToken);
		}
	}
}
