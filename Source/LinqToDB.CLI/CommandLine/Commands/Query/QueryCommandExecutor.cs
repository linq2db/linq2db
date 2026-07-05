using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.DataProvider.Firebird;
using LinqToDB.Internal.DataProvider.MySql;
using LinqToDB.Internal.DataProvider.PostgreSQL;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SqlServer;

using Microsoft.Data.SqlTypes;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Query command execution logic.
	/// </summary>
	sealed class QueryCommandExecutor(ICliEnvironment environment, QueryCommandSettings settings)
	{
		sealed record QueryOutputColumn(string Name, QueryActualFieldType ActualFieldType);

		sealed record QueryOutput(QueryOutputColumn[] Columns, List<string?[]> Rows);

		enum QueryActualFieldType
		{
			None = 0,
			Boolean,
			Double,
			Single,
			DateTime,
			DateTimeOffset,
			TimeSpan,
			Guid,
			Bytes,
			SqlBinary,
			SqlBytes,
			SqlChars,
			SqlString,
			SqlXml,
			SqlVectorFloat,
			SqlVectorHalf,
		}

		readonly ICliEnvironment      _environment = environment;
		readonly QueryCommandSettings _settings    = settings;

		public async ValueTask<int> Execute(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Get SQL text from command line or file.
			//
			string sql;

			if (_settings.Sql != null)
			{
				sql = _settings.Sql;
			}
			else if (_settings.SqlFile != null)
			{
				if (!_environment.FileExists(_settings.SqlFile))
				{
					await _environment.Error.WriteLineAsync($"SQL file '{_settings.SqlFile}' not found.").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				sql = _environment.ReadAllText(_settings.SqlFile);
			}
			else
			{
				throw new InvalidOperationException("Either SQL text or SQL file must be specified.");
			}

			try
			{
				// Create data provider for the specified database provider and connection string.
				//
				var dataProvider = DataConnection.GetDataProvider(_settings.Provider, _settings.ConnectionString);

				if (dataProvider == null)
				{
					await _environment.Error.WriteLineAsync($"Cannot create database provider: {_settings.Provider}").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				// Validate that the SQL query is a single statement and does not contain multiple statements or batch separators.
				//
				var singleStatementResult = QuerySafetyValidator.ValidateSingleStatement(dataProvider, sql);

				if (!singleStatementResult.IsSafe)
				{
					await _environment.Error.WriteLineAsync(singleStatementResult.Error).ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				// Validate that the SQL query is safe to execute based on the configured safety mode.
				//
				if (_settings.SqlSafety != QuerySqlSafetyMode.Allow)
				{
					var safetyResult = QuerySafetyValidator.Validate(dataProvider, sql);

					if (!safetyResult.IsSafe && !(_settings.SqlSafety == QuerySqlSafetyMode.Confirm && _settings.AllowUnsafeSql))
					{
						if (_settings.SqlSafety == QuerySqlSafetyMode.Confirm)
							await _environment.Error.WriteLineAsync($"Unsafe SQL requires '--allow-unsafe-sql': {safetyResult.Error}").ConfigureAwait(false);
						else
							await _environment.Error.WriteLineAsync(safetyResult.Error).ConfigureAwait(false);

						return StatusCodes.EXPECTED_ERROR;
					}
				}

				// Execute the SQL query and read the results.
				//
				var dataOptions = new DataOptions().UseConnectionString(dataProvider, _settings.ConnectionString);

				if (_settings.CommandTimeout != null)
					dataOptions = dataOptions.UseCommandTimeout(_settings.CommandTimeout);

				var dataConnection = new DataConnection(dataOptions);

				await using (dataConnection.ConfigureAwait(false))
				{
					var lockTimeoutCommand = GetLockTimeoutCommand(dataProvider, _settings.LockTimeout);

					if (lockTimeoutCommand != null)
						await dataConnection.ExecuteAsync(lockTimeoutCommand, cancellationToken).ConfigureAwait(false);

					// Execute the SQL query and get a data reader for the results.
					//
					var dataReader = await dataConnection.ExecuteReaderAsync(sql, cancellationToken).ConfigureAwait(false);

					await using (dataReader.ConfigureAwait(false))
					{
						var reader = dataReader.Reader;

						if (reader == null)
						{
							await _environment.Error.WriteLineAsync("Query didn't return data reader.").ConfigureAwait(false);
							return StatusCodes.EXPECTED_ERROR;
						}

						// Read the column metadata from the data reader and create output column definitions.
						//
						var columns = new QueryOutputColumn[reader.FieldCount];

						for (var i = 0; i < columns.Length; i++)
						{
							Type providerSpecificType;

							try
							{
								providerSpecificType = reader.GetProviderSpecificFieldType(i);
							}
							catch (NotSupportedException)
							{
								providerSpecificType = reader.GetFieldType(i);
							}

							columns[i] = CreateOutputColumn(reader, i, providerSpecificType);
						}

						if (string.Equals(_settings.Output, "json", StringComparison.OrdinalIgnoreCase))
						{
							var duplicateColumnName = GetDuplicateColumnName(columns);

							if (duplicateColumnName != null)
							{
								await _environment.Error.WriteLineAsync($"JSON output requires unique column names. Duplicate column name '{duplicateColumnName}' found. Use explicit SQL aliases for duplicate columns or switch to json-table output when duplicate-safe column metadata is needed.").ConfigureAwait(false);
								return StatusCodes.EXPECTED_ERROR;
							}
						}

						// Read the rows from the data reader and convert each field to a string representation.
						//
						var rows = new List<string?[]>();

						while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
						{
							var row = new string?[columns.Length];

							for (var i = 0; i < columns.Length; i++)
							{
								if (await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false))
									continue;

								row[i] = ReadFieldAsString(reader, columns[i].ActualFieldType, i);
							}

							rows.Add(row);
						}

						// Create a query output object containing the column definitions and rows.
						//
						var queryOutput = new QueryOutput(columns, rows);

						var output = string.Equals(_settings.Output, "csv", StringComparison.OrdinalIgnoreCase)
							? FormatCsv (queryOutput)
							: FormatJson(queryOutput);

						if (_settings.OutputFile != null)
						{
							_environment.WriteAllText(_settings.OutputFile, output);
						}
						else
						{
							await _environment.Out.WriteAsync(output.AsMemory(), cancellationToken).ConfigureAwait(false);
						}
					}
				}

				return StatusCodes.SUCCESS;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				await _environment.Error.WriteLineAsync($"Query execution failed: {ex.Message}").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}
		}

		static string? GetLockTimeoutCommand(IDataProvider dataProvider, int? timeout)
		{
			if (timeout == null)
				return null;

			if (dataProvider is SqlServerDataProvider)
				return string.Create(CultureInfo.InvariantCulture, $"SET LOCK_TIMEOUT {(long)timeout.Value * 1000}");

			if (dataProvider is PostgreSQLDataProvider)
				return string.Create(CultureInfo.InvariantCulture, $"SET lock_timeout = '{timeout.Value}s'");

			if (dataProvider is MySqlDataProvider)
				return string.Create(CultureInfo.InvariantCulture, $"SET SESSION innodb_lock_wait_timeout = {timeout.Value}");

			if (dataProvider is SQLiteDataProvider)
				return string.Create(CultureInfo.InvariantCulture, $"PRAGMA busy_timeout = {(long)timeout.Value * 1000}");

			if (dataProvider is FirebirdDataProvider)
				return string.Create(CultureInfo.InvariantCulture, $"SET LOCK TIMEOUT {timeout.Value}");

			return null;
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

		static string FormatJson(QueryOutput queryOutput)
		{
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

			writer.WriteStartArray();

			foreach (var row in queryOutput.Rows)
			{
				writer.WriteStartObject();

				for (var i = 0; i < queryOutput.Columns.Length; i++)
				{
					writer.WritePropertyName(queryOutput.Columns[i].Name);

					if (row[i] == null)
						writer.WriteNullValue();
					else
						writer.WriteStringValue(row[i]);
				}

				writer.WriteEndObject();
			}

			writer.WriteEndArray();
			writer.Flush();

			return Encoding.UTF8.GetString(stream.ToArray());
		}

		static string FormatCsv(QueryOutput queryOutput)
		{
			var text = new StringBuilder();

			for (var i = 0; i < queryOutput.Columns.Length; i++)
			{
				if (i > 0)
					text.Append(',');

				AppendCsvValue(text, queryOutput.Columns[i].Name);
			}

			text.AppendLine();

			foreach (var row in queryOutput.Rows)
			{
				for (var i = 0; i < queryOutput.Columns.Length; i++)
				{
					if (i > 0)
						text.Append(',');

					if (row[i] != null)
						AppendCsvValue(text, row[i]!);
				}

				text.AppendLine();
			}

			return text.ToString();
		}

		static QueryOutputColumn CreateOutputColumn(DbDataReader reader, int ordinal, Type providerSpecificType)
		{
			var actualFieldType = providerSpecificType switch
			{
				_ when providerSpecificType == typeof(bool)             => QueryActualFieldType.Boolean,
				_ when providerSpecificType == typeof(SqlBoolean)       => QueryActualFieldType.Boolean,
				_ when providerSpecificType == typeof(double)           => QueryActualFieldType.Double,
				_ when providerSpecificType == typeof(SqlDouble)        => QueryActualFieldType.Double,
				_ when providerSpecificType == typeof(float)            => QueryActualFieldType.Single,
				_ when providerSpecificType == typeof(SqlSingle)        => QueryActualFieldType.Single,
				_ when providerSpecificType == typeof(DateTime)         => QueryActualFieldType.DateTime,
				_ when providerSpecificType == typeof(SqlDateTime)      => QueryActualFieldType.DateTime,
				_ when providerSpecificType == typeof(DateTimeOffset)   => QueryActualFieldType.DateTimeOffset,
				_ when providerSpecificType == typeof(TimeSpan)         => QueryActualFieldType.TimeSpan,
				_ when providerSpecificType == typeof(Guid)             => QueryActualFieldType.Guid,
				_ when providerSpecificType == typeof(SqlGuid)          => QueryActualFieldType.Guid,
				_ when providerSpecificType == typeof(byte[])           => QueryActualFieldType.Bytes,
				_ when providerSpecificType == typeof(SqlBinary)        => QueryActualFieldType.SqlBinary,
				_ when providerSpecificType == typeof(SqlBytes)         => QueryActualFieldType.SqlBytes,
				_ when providerSpecificType == typeof(SqlChars)         => QueryActualFieldType.SqlChars,
				_ when providerSpecificType == typeof(SqlString)        => QueryActualFieldType.SqlString,
				_ when providerSpecificType == typeof(SqlXml)           => QueryActualFieldType.SqlXml,
				_ when providerSpecificType == typeof(SqlVector<float>) => QueryActualFieldType.SqlVectorFloat,
				_ when providerSpecificType == typeof(SqlVector<Half>)  => QueryActualFieldType.SqlVectorHalf,
				_                                                       => QueryActualFieldType.None,
			};

			return new QueryOutputColumn(reader.GetName(ordinal), actualFieldType);
		}

		static string? ReadFieldAsString(DbDataReader reader, QueryActualFieldType actualFieldType, int ordinal)
		{
			object value;

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
				QueryActualFieldType.Boolean        => value is SqlBoolean sqlBoolean ? sqlBoolean.Value ? "true" : "false" : ((bool)value) ? "true" : "false",
				QueryActualFieldType.Double         => (value is SqlDouble sqlDouble ? sqlDouble.Value : (double)value).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.Single         => (value is SqlSingle sqlSingle ? sqlSingle.Value : (float)value).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.DateTime       => (value is SqlDateTime sqlDateTime ? sqlDateTime.Value : (DateTime)value).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.DateTimeOffset => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.TimeSpan       => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture),
				QueryActualFieldType.Guid           => (value is SqlGuid sqlGuid ? sqlGuid.Value : (Guid)value).ToString("D"),
				QueryActualFieldType.Bytes          => Convert.ToBase64String((byte[])value),
				QueryActualFieldType.SqlBinary      => Convert.ToBase64String(((SqlBinary)value).Value),
				QueryActualFieldType.SqlBytes       => Convert.ToBase64String(((SqlBytes)value).Value),
				QueryActualFieldType.SqlChars       => new string(((SqlChars)value).Value),
				QueryActualFieldType.SqlString      => ((SqlString)value).Value,
				QueryActualFieldType.SqlXml         => ((SqlXml)value).Value,
				QueryActualFieldType.SqlVectorFloat => ConvertVectorToString(((SqlVector<float>)value).Memory.ToArray()),
				QueryActualFieldType.SqlVectorHalf  => ConvertVectorToString(((SqlVector<Half>) value).Memory.ToArray()),
				_                                   => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
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

		static void AppendCsvValue(StringBuilder output, string value)
		{
			if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
			{
				output.Append(value);
				return;
			}

			output.Append('"');
			output.Append(value.Replace("\"", "\"\"", StringComparison.Ordinal));
			output.Append('"');
		}
	}
}
