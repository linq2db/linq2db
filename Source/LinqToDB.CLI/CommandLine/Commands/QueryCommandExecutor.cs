using System;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Query command execution logic.
	/// </summary>
	internal sealed class QueryCommandExecutor
	{
		private readonly ICliEnvironment       _environment;
		private readonly QueryCommandSettings _settings;

		public QueryCommandExecutor(ICliEnvironment environment, QueryCommandSettings settings)
		{
			_environment = environment;
			_settings     = settings;
		}

		public async ValueTask<int> Execute(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (!TryGetSql(out var sql, out var error))
			{
				await _environment.Error.WriteLineAsync(error).ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}

			try
			{
				var dataProvider = DataConnection.GetDataProvider(_settings.Provider, _settings.ConnectionString);
				if (dataProvider == null)
				{
					await _environment.Error.WriteLineAsync($"Cannot create database provider: {_settings.Provider}").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
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

						var output = string.Equals(_settings.Output, "csv", StringComparison.OrdinalIgnoreCase)
							? await FormatCsv (reader, cancellationToken).ConfigureAwait(false)
							: await FormatJson(reader, cancellationToken).ConfigureAwait(false);

						await WriteOutput(output, cancellationToken).ConfigureAwait(false);
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

		private bool TryGetSql(out string sql, out string error)
		{
			if (_settings.Sql != null)
			{
				sql   = _settings.Sql;
				error = string.Empty;
				return true;
			}

			if (_settings.SqlFile != null)
			{
				if (!_environment.FileExists(_settings.SqlFile))
				{
					sql   = string.Empty;
					error = $"SQL file '{_settings.SqlFile}' not found.";
					return false;
				}

				sql   = _environment.ReadAllText(_settings.SqlFile);
				error = string.Empty;
				return true;
			}

			throw new InvalidOperationException("Either SQL text or SQL file must be specified.");
		}

		private async Task WriteOutput(string output, CancellationToken cancellationToken)
		{
			if (_settings.OutputFile != null)
			{
				_environment.WriteAllText(_settings.OutputFile, output);
			}
			else
			{
				await _environment.Out.WriteAsync(output.AsMemory(), cancellationToken).ConfigureAwait(false);
			}
		}

		private static async Task<string> FormatJson(DbDataReader reader, CancellationToken cancellationToken)
		{
			using var stream = new MemoryStream();
			using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

			writer.WriteStartArray();

			while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				writer.WriteStartObject();

				for (var i = 0; i < reader.FieldCount; i++)
				{
					writer.WritePropertyName(reader.GetName(i));

					if (await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false))
						writer.WriteNullValue();
					else
						JsonSerializer.Serialize(writer, reader.GetValue(i), reader.GetFieldType(i));
				}

				writer.WriteEndObject();
			}

			writer.WriteEndArray();

			return Encoding.UTF8.GetString(stream.ToArray());
		}

		private static async Task<string> FormatCsv(DbDataReader reader, CancellationToken cancellationToken)
		{
			var output = new StringBuilder();

			for (var i = 0; i < reader.FieldCount; i++)
			{
				if (i > 0)
					output.Append(',');

				AppendCsvValue(output, reader.GetName(i));
			}

			output.AppendLine();

			while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				for (var i = 0; i < reader.FieldCount; i++)
				{
					if (i > 0)
						output.Append(',');

					if (!await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false))
						AppendCsvValue(output, Convert.ToString(reader.GetValue(i), CultureInfo.InvariantCulture) ?? string.Empty);
				}

				output.AppendLine();
			}

			return output.ToString();
		}

		private static void AppendCsvValue(StringBuilder output, string value)
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
