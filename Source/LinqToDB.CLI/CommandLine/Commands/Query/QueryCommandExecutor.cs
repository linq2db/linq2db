using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
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
		}

		readonly ICliEnvironment      _environment = environment;
		readonly QueryCommandSettings _settings    = settings;

		public async ValueTask<int> Execute(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

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
				var dataProvider = DataConnection.GetDataProvider(_settings.Provider, _settings.ConnectionString);

				if (dataProvider == null)
				{
					await _environment.Error.WriteLineAsync($"Cannot create database provider: {_settings.Provider}").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				var singleStatementResult = QuerySafetyValidator.ValidateSingleStatement(dataProvider, sql);

				if (!singleStatementResult.IsSafe)
				{
					await _environment.Error.WriteLineAsync(singleStatementResult.Error).ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

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

				var dataConnection = new DataConnection(new DataOptions().UseConnectionString(dataProvider, _settings.ConnectionString));

				await using (dataConnection.ConfigureAwait(false))
				{
					var dataReader = await dataConnection.ExecuteReaderAsync(sql, cancellationToken).ConfigureAwait(false);

					await using (dataReader.ConfigureAwait(false))
					{
						var reader = dataReader.Reader;

						if (reader == null)
						{
							await _environment.Error.WriteLineAsync("Query didn't return data reader.").ConfigureAwait(false);
							return StatusCodes.EXPECTED_ERROR;
						}

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
				_ when providerSpecificType == typeof(bool)           => QueryActualFieldType.Boolean,
				_ when providerSpecificType == typeof(SqlBoolean)     => QueryActualFieldType.Boolean,
				_ when providerSpecificType == typeof(double)         => QueryActualFieldType.Double,
				_ when providerSpecificType == typeof(SqlDouble)      => QueryActualFieldType.Double,
				_ when providerSpecificType == typeof(float)          => QueryActualFieldType.Single,
				_ when providerSpecificType == typeof(SqlSingle)      => QueryActualFieldType.Single,
				_ when providerSpecificType == typeof(DateTime)       => QueryActualFieldType.DateTime,
				_ when providerSpecificType == typeof(SqlDateTime)    => QueryActualFieldType.DateTime,
				_ when providerSpecificType == typeof(DateTimeOffset) => QueryActualFieldType.DateTimeOffset,
				_ when providerSpecificType == typeof(TimeSpan)       => QueryActualFieldType.TimeSpan,
				_ when providerSpecificType == typeof(Guid)           => QueryActualFieldType.Guid,
				_ when providerSpecificType == typeof(SqlGuid)        => QueryActualFieldType.Guid,
				_ when providerSpecificType == typeof(byte[])         => QueryActualFieldType.Bytes,
				_ when providerSpecificType == typeof(SqlBinary)      => QueryActualFieldType.SqlBinary,
				_ when providerSpecificType == typeof(SqlBytes)       => QueryActualFieldType.SqlBytes,
				_ when providerSpecificType == typeof(SqlChars)       => QueryActualFieldType.SqlChars,
				_ when providerSpecificType == typeof(SqlString)      => QueryActualFieldType.SqlString,
				_ when providerSpecificType == typeof(SqlXml)         => QueryActualFieldType.SqlXml,
				_                                                     => QueryActualFieldType.None,
			};

			return new QueryOutputColumn(reader.GetName(ordinal), actualFieldType);
		}

		static string? ReadFieldAsString(DbDataReader reader, QueryActualFieldType actualFieldType, int ordinal)
		{
			object fieldValue;

			try
			{
				fieldValue = reader.GetProviderSpecificValue(ordinal);
			}
			catch (Exception providerSpecificException) when (providerSpecificException is not OperationCanceledException)
			{
				try
				{
					fieldValue = reader.GetValue(ordinal);
				}
				catch (Exception getValueException) when (getValueException is not OperationCanceledException)
				{
					ExceptionDispatchInfo.Capture(providerSpecificException).Throw();
					throw;
				}
			}

			return actualFieldType switch
			{
				QueryActualFieldType.Boolean        => fieldValue is SqlBoolean sqlBoolean ? sqlBoolean.Value ? "true" : "false" : ((bool)fieldValue) ? "true" : "false",
				QueryActualFieldType.Double         => (fieldValue is SqlDouble sqlDouble ? sqlDouble.Value : (double)fieldValue).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.Single         => (fieldValue is SqlSingle sqlSingle ? sqlSingle.Value : (float)fieldValue).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.DateTime       => (fieldValue is SqlDateTime sqlDateTime ? sqlDateTime.Value : (DateTime)fieldValue).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.DateTimeOffset => ((DateTimeOffset)fieldValue).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.TimeSpan       => ((TimeSpan)fieldValue).ToString("c", CultureInfo.InvariantCulture),
				QueryActualFieldType.Guid           => (fieldValue is SqlGuid sqlGuid ? sqlGuid.Value : (Guid)fieldValue).ToString("D"),
				QueryActualFieldType.Bytes          => Convert.ToBase64String((byte[])fieldValue),
				QueryActualFieldType.SqlBinary      => Convert.ToBase64String(((SqlBinary)fieldValue).Value),
				QueryActualFieldType.SqlBytes       => Convert.ToBase64String(((SqlBytes)fieldValue).Value),
				QueryActualFieldType.SqlChars       => new string(((SqlChars)fieldValue).Value),
				QueryActualFieldType.SqlString      => ((SqlString)fieldValue).Value,
				QueryActualFieldType.SqlXml         => ((SqlXml)fieldValue).Value,
				_ when fieldValue is SqlVector<float> vector => ConvertVectorToString(vector.Memory.ToArray()),
				_ when fieldValue is SqlVector<Half> vector  => ConvertVectorToString(vector.Memory.ToArray()),
				_                                   => Convert.ToString(fieldValue, CultureInfo.InvariantCulture),
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
