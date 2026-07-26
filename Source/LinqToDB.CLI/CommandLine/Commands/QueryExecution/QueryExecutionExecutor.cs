using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DuckDB.NET.Native;

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

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Query command execution logic.
	/// </summary>
	public sealed class QueryExecutionExecutor
	{
		sealed record QueryOutputColumn(int Ordinal, string Name, string FieldType, string ProviderSpecificFieldType, string DataTypeName, QueryActualFieldType ActualFieldType);

		/// <summary>
		/// Provider-specific field value conversion mode.
		/// </summary>
		public enum QueryActualFieldType
		{
			None = 0,
			Boolean,
			Double,
			Single,
			Date,
			DateTime,
			DateTimeOffset,
			TimeSpan,
			Guid,
			Bytes,
			ByteArray,
			SqlBinary,
			SqlBytes,
			SqlChars,
			SqlString,
			SqlXml,
			SqlVectorFloat,
			SqlVectorHalf,
			SqlHierarchyId,
			SqlGeometry,
			SqlGeography,
			OracleBinary,
			OracleBlob,
			OracleBFile,
			OracleClob,
			OracleXmlType,
			OracleDate,
			OracleTimeStamp,
			OracleTimeStampTZ,
			OracleTimeStampLTZ,
			DB2Binary,
			DB2Blob,
			DB2Clob,
			DB2Date,
			DB2Time,
			DB2TimeStamp,
			DB2Xml,
			MySqlDecimal,
			FirebirdDecFloat,
			FirebirdZonedDateTime,
			FirebirdZonedTime,
			NpgsqlRange,
		}

		readonly QueryExecutionSettings _settings;

		const string OracleBFilePlaceholder = "<BFILE>";

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
					CreateConnectionSettings(),
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

		ConnectionSettings CreateConnectionSettings()
		{
			return new ConnectionSettings(
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
				null);
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

				var outputBytes       = 0;
				var footerReserveBytes = 0;

				async Task<bool> WriteOutputSegment(Func<TextWriter, Task> write, int reservedBytes)
				{
					if (_settings.MaxOutputBytes == null)
					{
						await write(outputWriter);
						return true;
					}

					using var segmentWriter = new StringWriter(CultureInfo.InvariantCulture);

					await write(segmentWriter);

					var segment      = segmentWriter.ToString();
					var segmentBytes = Encoding.UTF8.GetByteCount(segment);

					if ((long)outputBytes + segmentBytes + reservedBytes > _settings.MaxOutputBytes.Value)
						return false;

					await outputWriter.WriteAsync(segment.AsMemory(), cancellationToken);
					outputBytes += segmentBytes;

					return true;
				}

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
					"csv"        => WriteOutputSegment(
						writer => WriteCsvHeader(writer, columns, cancellationToken),
						footerReserveBytes),
					"json-table" => WriteOutputSegment(
						writer => WriteJsonTableStart(writer, columns, cancellationToken),
						footerReserveBytes),
					_            => WriteOutputSegment(
						writer => writer.WriteAsync("[".AsMemory(), cancellationToken),
						footerReserveBytes),
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
					var row = await ReadRow(reader, columns, cancellationToken);

					// Write the row using the selected output format.
					//
					var rowWritten = _settings.Output switch
					{
						"csv"        => await WriteOutputSegment(writer => WriteCsvRow      (writer, row,                    cancellationToken), footerReserveBytes),
						"json-table" => await WriteOutputSegment(writer => WriteJsonTableRow(writer, row,          rowCount, cancellationToken), footerReserveBytes),
						_            => await WriteOutputSegment(writer => WriteJsonRow     (writer, columns, row, rowCount, cancellationToken), footerReserveBytes),
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
					"json-table" => WriteOutputSegment(
						writer => WriteJsonTableEnd(writer, rowCount, truncated, truncationReason, _settings.MaxOutputBytes, recordsAffected, cancellationToken),
						0),
					"csv"        => Task.FromResult(true),
					_            => WriteOutputSegment(
						writer => writer.WriteAsync("]".AsMemory(), cancellationToken),
						0),
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
				if (dataReader != null)
					await dataReader.DisposeAsync().AsTask();

				await dataConnection.DisposeAsync().AsTask();
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

		static async Task<string?[]> ReadRow(DbDataReader reader, QueryOutputColumn[] columns, CancellationToken cancellationToken)
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
						row[i] = OracleBFilePlaceholder;
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

			return row;
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
				await WriteJsonString  (output, columns[i].FieldType, cancellationToken);
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
			TextWriter              output,
			int                     rowCount,
			bool                    truncated,
			QueryTruncationReason?  truncationReason,
			int?                    maxOutputBytes,
			int?                    recordsAffected,
			CancellationToken       cancellationToken)
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
				case QueryActualFieldType.OracleBFile : return OracleBFilePlaceholder;
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

			return actualFieldType switch
			{
				QueryActualFieldType.Boolean               => value is SqlBoolean sqlBoolean ? sqlBoolean.Value ? "true" : "false" : ((bool)value) ? "true" : "false",
				QueryActualFieldType.Double                => (value is SqlDouble sqlDouble ? sqlDouble.Value : (double)value).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.Single                => (value is SqlSingle sqlSingle ? sqlSingle.Value : (float)value).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.Date                  => FormatDate(value),
				QueryActualFieldType.DateTime              => (value is SqlDateTime sqlDateTime ? sqlDateTime.Value : (DateTime)value).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.DateTimeOffset        => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.TimeSpan              => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture),
				QueryActualFieldType.Guid                  => (value is SqlGuid sqlGuid ? sqlGuid.Value : (Guid)value).ToString("D"),
				QueryActualFieldType.Bytes                 => ConvertBytesToString((byte[])value),
				QueryActualFieldType.ByteArray             => ConvertByteArrayToString((byte[])value),
				QueryActualFieldType.SqlBinary             => ConvertBytesToString(((SqlBinary)value).Value),
				QueryActualFieldType.SqlBytes              => ConvertBytesToString(((SqlBytes)value).Value),
				QueryActualFieldType.SqlChars              => new string(((SqlChars)value).Value),
				QueryActualFieldType.SqlString             => ((SqlString)value).Value,
				QueryActualFieldType.SqlXml                => ((SqlXml)value).Value,
				QueryActualFieldType.SqlVectorFloat        => ConvertVectorToString(((SqlVector<float>)value).Memory.ToArray()),
				QueryActualFieldType.SqlVectorHalf         => ConvertVectorToString(((SqlVector<Half>) value).Memory.ToArray()),
				QueryActualFieldType.SqlHierarchyId        => ((SqlHierarchyId)value).ToString(),
				QueryActualFieldType.SqlGeometry           => ((SqlGeometry)value).ToString(),
				QueryActualFieldType.SqlGeography          => ((SqlGeography)value).ToString(),
				QueryActualFieldType.OracleBinary          => ConvertBytesToString(((OracleBinary)value).Value),
				QueryActualFieldType.OracleBlob            => ConvertBytesToString(((OracleBlob)value).Value),
				QueryActualFieldType.OracleClob            => ((OracleClob)value).Value,
				QueryActualFieldType.OracleXmlType         => ((OracleXmlType)value).Value,
				QueryActualFieldType.OracleDate            => FormatDate(((OracleDate)value).Value),
				QueryActualFieldType.OracleTimeStamp       => FormatOracleTimeStamp((OracleTimeStamp)value),
				QueryActualFieldType.OracleTimeStampTZ     => FormatOracleTimeStampTZ((OracleTimeStampTZ)value),
				QueryActualFieldType.OracleTimeStampLTZ    => FormatOracleTimeStampLTZ((OracleTimeStampLTZ)value),
				QueryActualFieldType.DB2Binary             => ConvertBytesToString((byte[])GetProviderSpecificPropertyValue(value, "Value")),
				QueryActualFieldType.DB2Blob               => ConvertBytesToString((byte[])GetProviderSpecificPropertyValue(value, "Value")),
				QueryActualFieldType.DB2Clob               => (string)GetProviderSpecificPropertyValue(value, "Value"),
				QueryActualFieldType.DB2Date               => FormatDate((DateTime)GetProviderSpecificPropertyValue(value, "Value")),
				QueryActualFieldType.DB2Time               => ((TimeSpan)GetProviderSpecificPropertyValue(value, "Value")).ToString("c", CultureInfo.InvariantCulture),
				QueryActualFieldType.DB2TimeStamp          => ((DateTime)GetProviderSpecificPropertyValue(value, "Value")).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.DB2Xml                => (string)GetProviderSpecificMethodValue(value, "GetString"),
				QueryActualFieldType.MySqlDecimal          => Convert.ToString(value, CultureInfo.InvariantCulture),
				QueryActualFieldType.FirebirdDecFloat      => FormatFirebirdDecFloat((FbDecFloat)value),
				QueryActualFieldType.FirebirdZonedDateTime => FormatFirebirdZonedDateTime((FbZonedDateTime)value),
				QueryActualFieldType.FirebirdZonedTime     => FormatFirebirdZonedTime((FbZonedTime)value),
				QueryActualFieldType.NpgsqlRange           => FormatNpgsqlRange(value),
				_                                          => ConvertValueToString(value),
			};
		}

		static string? ConvertValueToString(object? value)
		{
			return value switch
			{
				null                 => null,
				string stringValue   => stringValue,
				byte[] bytes         => ConvertBytesToString(bytes),
				Stream stream        => ConvertStreamToString(stream),
				DuckDBDateOnly date  => FormatDuckDBDateOnly(date),
				DuckDBTimeOnly time  => FormatDuckDBTimeOnly(time),
				DuckDBTimestamp time => FormatDuckDBTimestamp(time),
				DateOnly date        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				TimeOnly time        => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
				ITuple tuple         => ConvertTupleToString(tuple),
				IEnumerable sequence => ConvertSequenceToString(sequence),
				_                    => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static string ConvertBytesToString(byte[] bytes)
		{
			return "0x" + Convert.ToHexString(bytes);
		}

		static string ConvertStreamToString(Stream stream)
		{
			if (stream.CanSeek)
				stream.Position = 0;

			using var memory = new MemoryStream();

			stream.CopyTo(memory);

			return ConvertBytesToString(memory.ToArray());
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

		static object GetProviderSpecificPropertyValue(object value, string propertyName)
		{
			var property = value.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
				?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' doesn't contain '{propertyName}' property.");

			return property.GetValue(value)
				?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' property '{propertyName}' returned null.");
		}

		static object GetProviderSpecificMethodValue(object value, string methodName)
		{
			var method = value.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, binder: null, types: Type.EmptyTypes, modifiers: null)
				?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' doesn't contain '{methodName}' method.");

			return method.InvokeExt(value, null)
				?? throw new InvalidOperationException($"Provider-specific type '{value.GetType().FullName}' method '{methodName}' returned null.");
		}

		static string FormatDate(object value)
		{
			return value switch
			{
				DateOnly date     => date.    ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				_                 => Convert. ToString(value,        CultureInfo.InvariantCulture)!,
			};
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

		static string FormatFirebirdDecFloat(FbDecFloat value)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{value.Coefficient}E{value.Exponent}");
		}

		static string FormatFirebirdZonedDateTime(FbZonedDateTime value)
		{
			return value.DateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture) + " " + FormatFirebirdTimeZone(value.TimeZone, value.Offset);
		}

		static string FormatFirebirdZonedTime(FbZonedTime value)
		{
			return value.Time.ToString("c", CultureInfo.InvariantCulture) + " " + FormatFirebirdTimeZone(value.TimeZone, value.Offset);
		}

		static string FormatFirebirdTimeZone(string timeZone, TimeSpan? offset)
		{
			return offset?.ToString("c", CultureInfo.InvariantCulture) ?? timeZone;
		}

		static string FormatDuckDBDateOnly(DuckDBDateOnly value)
		{
			if (value.IsPositiveInfinity) return "infinity";
			if (value.IsNegativeInfinity) return "-infinity";

			return string.Create(CultureInfo.InvariantCulture, $"{value.Year:D4}-{value.Month:D2}-{value.Day:D2}");
		}

		static string FormatDuckDBTimeOnly(DuckDBTimeOnly value)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{value.Hour:D2}:{value.Min:D2}:{value.Sec:D2}.{value.Microsecond:D6}0");
		}

		static string FormatDuckDBTimestamp(DuckDBTimestamp value)
		{
			if (value.IsPositiveInfinity) return "infinity";
			if (value.IsNegativeInfinity) return "-infinity";

			return FormatDuckDBDateOnly(value.Date) + "T" + FormatDuckDBTimeOnly(value.Time);
		}

		static string FormatNpgsqlRange(object value)
		{
			var type    = value.GetType();
			var isEmpty = (bool)GetProviderSpecificPropertyValue(value, "IsEmpty");

			if (isEmpty)
				return "empty";

			var lowerBoundInfinite    = (bool)GetProviderSpecificPropertyValue(value, "LowerBoundInfinite");
			var upperBoundInfinite    = (bool)GetProviderSpecificPropertyValue(value, "UpperBoundInfinite");
			var lowerBoundIsInclusive = (bool)GetProviderSpecificPropertyValue(value, "LowerBoundIsInclusive");
			var upperBoundIsInclusive = (bool)GetProviderSpecificPropertyValue(value, "UpperBoundIsInclusive");
			var lowerBound            = lowerBoundInfinite ? null : type.GetProperty("LowerBound", BindingFlags.Public | BindingFlags.Instance)!.GetValue(value);
			var upperBound            = upperBoundInfinite ? null : type.GetProperty("UpperBound", BindingFlags.Public | BindingFlags.Instance)!.GetValue(value);
			var output                = new StringBuilder();

			output.Append(lowerBoundIsInclusive ? '[' : '(');

			if (!lowerBoundInfinite)
				output.Append(ConvertRangeBoundToString(lowerBound));

			output.Append(',');

			if (!upperBoundInfinite)
				output.Append(ConvertRangeBoundToString(upperBound));

			output.Append(upperBoundIsInclusive ? ']' : ')');
			return output.ToString();
		}

		static string? ConvertRangeBoundToString(object? value)
		{
			return value switch
			{
				null                    => null,
				DateOnly date           => date.    ToString("yyyy-MM-dd",       CultureInfo.InvariantCulture),
				DateTime dateTime       => dateTime.ToString("O",                CultureInfo.InvariantCulture),
				DateTimeOffset dateTime => dateTime.ToString("O",                CultureInfo.InvariantCulture),
				TimeOnly time           => time.    ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
				TimeSpan time           => time.    ToString("c",                CultureInfo.InvariantCulture),
				_                       => Convert. ToString(value,              CultureInfo.InvariantCulture),
			};
		}

		static string ConvertByteArrayToString(byte[] bytes)
		{
			var output = new StringBuilder();

			output.Append('[');

			for (var i = 0; i < bytes.Length; i++)
			{
				if (i > 0)
					output.Append(',');

				output.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
			}

			output.Append(']');

			return output.ToString();
		}

		static string ConvertTupleToString(ITuple tuple)
		{
			var output = new StringBuilder();

			output.Append('(');

			for (var i = 0; i < tuple.Length; i++)
			{
				if (i > 0)
					output.Append(',');

				output.Append(ConvertValueToString(tuple[i]));
			}

			output.Append(')');

			return output.ToString();
		}

		static string ConvertSequenceToString(IEnumerable sequence)
		{
			var output       = new StringBuilder();
			var first        = true;
			var map          = false;
			var openBracket  = '[';
			var closeBracket = ']';

			foreach (var item in sequence)
			{
				if (first)
				{
					map = IsKeyValuePair(item);

					if (map)
					{
						openBracket  = '{';
						closeBracket = '}';
					}

					output.Append(openBracket);
					first = false;
				}

				if (output.Length > 1)
					output.Append(',');

				if (map)
					AppendKeyValuePair(output, item);
				else
					output.Append(ConvertValueToString(item));
			}

			if (first)
				output.Append(openBracket);

			output.Append(closeBracket);

			return output.ToString();
		}

		static bool IsKeyValuePair(object? value)
		{
			return value != null
				&& value.GetType().IsGenericType
				&& value.GetType().GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
		}

		static void AppendKeyValuePair(StringBuilder output, object? value)
		{
			if (value == null)
			{
				output.Append(':');
				return;
			}

			var type  = value.GetType();
			var key   = type.GetProperty("Key")!.GetValue(value);
			var item  = type.GetProperty("Value")!.GetValue(value);

			output.Append(ConvertValueToString(key));
			output.Append(':');
			output.Append(ConvertValueToString(item));
		}

		static string FormatOracleTimeStamp(OracleTimeStamp value)
		{
			return FormatOracleTimeStamp(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Nanosecond);
		}

		static string FormatOracleTimeStampTZ(OracleTimeStampTZ value)
		{
			return FormatOracleTimeStamp(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Nanosecond) + value.TimeZone;
		}

		static string FormatOracleTimeStampLTZ(OracleTimeStampLTZ value)
		{
			return FormatOracleTimeStamp(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Nanosecond);
		}

		static string FormatOracleTimeStamp(int year, int month, int day, int hour, int minute, int second, int nanosecond)
		{
			return string.Create(CultureInfo.InvariantCulture, $"{year:D4}-{month:D2}-{day:D2}T{hour:D2}:{minute:D2}:{second:D2}.{nanosecond:D9}");
		}

		static string ConvertVectorToString<T>(T[] vector)
		{
			var output = new StringBuilder();

			output.Append('[');

			for (var i = 0; i < vector.Length; i++)
			{
				if (i > 0)
					output.Append(',');

				output.Append(Convert.ToString(vector[i], CultureInfo.InvariantCulture));
			}

			output.Append(']');

			return output.ToString();
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
