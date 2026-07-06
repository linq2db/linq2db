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

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.Firebird;
using LinqToDB.Internal.DataProvider.MySql;
using LinqToDB.Internal.DataProvider.PostgreSQL;
using LinqToDB.Internal.DataProvider.SQLite;
using LinqToDB.Internal.DataProvider.SqlServer;

using Microsoft.Data.SqlTypes;
using Microsoft.SqlServer.Types;

using Oracle.ManagedDataAccess.Types;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Query command execution logic.
	/// </summary>
	sealed class QueryCommandExecutor(ICliEnvironment environment, QueryCommandSettings settings)
	{
		sealed record QueryOutputColumn(int Ordinal, string Name, string FieldType, string ProviderSpecificFieldType, string DataTypeName, QueryActualFieldType ActualFieldType);

		enum QueryActualFieldType
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
		}

		readonly ICliEnvironment      _environment = environment;
		readonly QueryCommandSettings _settings    = settings;

		public async ValueTask<int> Execute(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			// Get user-provided SQL text from command line or file.
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
				if (!LoadExternalProvider())
					return StatusCodes.EXPECTED_ERROR;

				// Create data provider for the specified database provider and connection string.
				//
				var dataProvider = DataConnection.GetDataProvider(_settings.Provider, _settings.ConnectionString);

				if (dataProvider == null)
				{
					await _environment.Error.WriteLineAsync($"Cannot create database provider: {_settings.Provider}").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				// Validate that user-provided SQL contains a single statement.
				//
				var singleStatementResult = ReadOnlySqlGuard.ValidateSingleStatement(dataProvider, sql);

				if (!singleStatementResult.IsAllowed)
				{
					await _environment.Error.WriteLineAsync(singleStatementResult.Error).ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				// Validate that user-provided SQL is allowed by the configured unsafe SQL policy.
				//
				if (_settings.UnsafeSqlPolicy != UnsafeSqlPolicy.Allow)
				{
					var guardResult = ReadOnlySqlGuard.Validate(dataProvider, sql);

					if (!guardResult.IsAllowed && !(_settings.UnsafeSqlPolicy == UnsafeSqlPolicy.Confirm && _settings.AllowUnsafeSql))
					{
						if (_settings.UnsafeSqlPolicy == UnsafeSqlPolicy.Confirm)
							await _environment.Error.WriteLineAsync($"Unsafe SQL requires '--allow-unsafe-sql': {guardResult.Error}").ConfigureAwait(false);
						else
							await _environment.Error.WriteLineAsync(guardResult.Error).ConfigureAwait(false);

						return StatusCodes.EXPECTED_ERROR;
					}
				}

				// Check if the output file already exists and the overwrite option is not specified.
				//
				if (_settings is { OutputFile: not null, Overwrite: false } && _environment.FileExists(_settings.OutputFile))
				{
					await _environment.Error.WriteLineAsync($"Output file '{_settings.OutputFile}' already exists. Use '--overwrite' to replace it.").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}

				// Configure linq2db connection options for the resolved provider and connection string.
				//
				var dataOptions = new DataOptions().UseConnectionString(dataProvider, _settings.ConnectionString);

				if (_settings.CommandTimeout > 0)
					dataOptions = dataOptions.UseCommandTimeout(_settings.CommandTimeout);

				// Open a connection and apply optional provider-specific session setup before user SQL execution.
				//
				var dataConnection = new DataConnection(dataOptions);

				await using (dataConnection.ConfigureAwait(false))
				{
					var lockTimeoutCommand = GetLockTimeoutCommand(dataProvider, _settings.LockTimeout);

					if (lockTimeoutCommand != null)
						await dataConnection.ExecuteAsync(lockTimeoutCommand, cancellationToken).ConfigureAwait(false);

					// Execute user-provided SQL and get a data reader for the result set.
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

						// Validate that the output format is compatible with the column metadata.
						//
						if (string.Equals(_settings.Output, "json", StringComparison.OrdinalIgnoreCase))
						{
							var duplicateColumnName = GetDuplicateColumnName(columns);

							if (duplicateColumnName != null)
							{
								await _environment.Error.WriteLineAsync($"JSON output requires unique column names. Duplicate column name '{duplicateColumnName}' found. Use explicit SQL aliases for duplicate columns or switch to json-table output when duplicate-safe column metadata is needed.").ConfigureAwait(false);
								return StatusCodes.EXPECTED_ERROR;
							}
						}

						// Read rows from the data reader and write them directly to the output stream.
						//
						var outputWriter        = _settings.OutputFile != null ? _environment.CreateTextWriter(_settings.OutputFile) : _environment.Out;
						var disposeOutputWriter = _settings.OutputFile != null;
						var rowCount            = 0;
						var truncated           = false;

						try
						{
							// Write the output header based on the specified output format.
							//
							switch (_settings.Output)
							{
								case "csv":
									// CSV output starts with a header row.
									//
									await WriteCsvHeader(outputWriter, columns, cancellationToken).ConfigureAwait(false);
									break;
								case "json-table":
									// JSON table output starts with column metadata and then streams rows.
									//
									await WriteJsonTableStart(outputWriter, columns, cancellationToken).ConfigureAwait(false);
									break;
								default:
									// Regular JSON output is an array of row objects.
									//
									await outputWriter.WriteAsync("[".AsMemory(), cancellationToken).ConfigureAwait(false);
									break;
							}

							while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
							{
								// Stop reading after the configured row limit and report truncation.
								//
								if (_settings.MaxRows > 0 && rowCount >= _settings.MaxRows)
								{
									truncated = true;
									break;
								}

								// Read one result row as normalized string values.
								//
								var row = new string?[columns.Length];

								for (var i = 0; i < columns.Length; i++)
								{
									// Oracle BFILE is an external file locator. Even IsDBNull can trigger a file/LOB
									// operation, so avoid reader value APIs for it.
									//
									if (columns[i].ActualFieldType == QueryActualFieldType.OracleBFile)
									{
										row[i] = "BFILE";
										continue;
									}

									if (await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false))
										continue;

									row[i] = ReadFieldAsString(reader, columns[i].ActualFieldType, i);
								}

								// Write the row using the selected output format.
								//
								switch (_settings.Output)
								{
									case "csv":
										await WriteCsvRow(outputWriter, row, cancellationToken).ConfigureAwait(false);
										break;
									case "json-table":
										await WriteJsonTableRow(outputWriter, row, rowCount, cancellationToken).ConfigureAwait(false);
										break;
									default:
										await WriteJsonRow(outputWriter, columns, row, rowCount, cancellationToken).ConfigureAwait(false);
										break;
								}

								rowCount++;
							}

							// Close the selected output format.
							//
							switch (_settings.Output)
							{
								case "json-table":
									await WriteJsonTableEnd(outputWriter, rowCount, truncated, cancellationToken).ConfigureAwait(false);
									break;
								case "csv":
									break;
								default:
									await outputWriter.WriteAsync("]".AsMemory(), cancellationToken).ConfigureAwait(false);
									break;
							}

							await outputWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
						}
						finally
						{
							if (disposeOutputWriter)
								await outputWriter.DisposeAsync().ConfigureAwait(false);
						}

						if (truncated && !string.Equals(_settings.Output, "json-table", StringComparison.OrdinalIgnoreCase))
						{
							// JSON table carries truncation in-band; other formats report it through stderr.
							//
							await _environment.Error.WriteLineAsync($"Query result truncated to {_settings.MaxRows.ToString(CultureInfo.InvariantCulture)} row(s). Use '--max-rows' to change the limit.").ConfigureAwait(false);
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

		bool LoadExternalProvider()
		{
			if (_settings.ProviderLocation == null)
			{
				if (IsDB2Provider(_settings.Provider))
				{
					_environment.Error.WriteLine(@"Cannot locate IBM.Data.Db2.dll provider assembly.
Due to huge size of it, we don't include Net.IBM.Data.Db2 provider into installation.
You need to install it manually and specify provider path using '--provider-location <path_to_assembly>' option.
Provider could be downloaded from:
- for Windows: https://www.nuget.org/packages/Net.IBM.Data.Db2
- for Linux: https://www.nuget.org/packages/Net.IBM.Data.Db2-lnx
- for macOS: https://www.nuget.org/packages/Net.IBM.Data.Db2-osx");
					return false;
				}

				return true;
			}

			if (!_environment.FileExists(_settings.ProviderLocation))
			{
				_environment.Error.WriteLine($"Provider assembly '{_settings.ProviderLocation}' not found.");
				return false;
			}

			var currentDirectory = Environment.CurrentDirectory;
			var providerDirectory = Path.GetDirectoryName(Path.GetFullPath(_settings.ProviderLocation));

			try
			{
				if (!string.IsNullOrEmpty(providerDirectory))
					Environment.CurrentDirectory = providerDirectory;

				var assembly = Assembly.LoadFrom(_settings.ProviderLocation);

				if (IsDB2Provider(_settings.Provider))
				{
					DB2Tools.AutoDetectProvider = true;

					var factory = FindProviderFactory(assembly, "DB2Factory");

					if (factory == null)
					{
						_environment.Error.WriteLine($"Provider assembly '{_settings.ProviderLocation}' doesn't contain DB2Factory type.");
						return false;
					}

					DbProviderFactories.RegisterFactory("IBM.Data.DB2", factory);
				}
			}
			finally
			{
				Environment.CurrentDirectory = currentDirectory;
			}

			return true;
		}

		static Type? FindProviderFactory(Assembly assembly, string factoryTypeName)
		{
			foreach (var type in assembly.GetTypes())
			{
				if (string.Equals(type.Name, factoryTypeName, StringComparison.Ordinal)
					&& typeof(DbProviderFactory).IsAssignableFrom(type))
				{
					return type;
				}
			}

			return null;
		}

		static bool IsDB2Provider(string provider)
		{
			return string.Equals(provider, ProviderName.DB2,     StringComparison.OrdinalIgnoreCase)
				|| string.Equals(provider, ProviderName.DB2LUW,  StringComparison.OrdinalIgnoreCase)
				|| string.Equals(provider, ProviderName.DB2zOS,  StringComparison.OrdinalIgnoreCase);
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
				FirebirdDataProvider   => string.Create(CultureInfo.InvariantCulture, $"SET LOCK TIMEOUT {timeout.Value}"),
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

		static async Task WriteJsonRow(TextWriter output, QueryOutputColumn[] columns, string?[] row, int rowIndex, CancellationToken cancellationToken)
		{
			if (rowIndex > 0)
				await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

			await output.WriteAsync("{".AsMemory(), cancellationToken).ConfigureAwait(false);

			for (var i = 0; i < columns.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

				await WriteJsonString(output, columns[i].Name, cancellationToken).ConfigureAwait(false);
				await output.WriteAsync(":".AsMemory(), cancellationToken).ConfigureAwait(false);
				await WriteJsonValue(output, row[i], cancellationToken).ConfigureAwait(false);
			}

			await output.WriteAsync("}".AsMemory(), cancellationToken).ConfigureAwait(false);
		}

		static async Task WriteJsonTableStart(TextWriter output, QueryOutputColumn[] columns, CancellationToken cancellationToken)
		{
			await output.WriteAsync("{\"columns\":[".AsMemory(), cancellationToken).ConfigureAwait(false);

			for (var i = 0; i < columns.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

				await output.WriteAsync("{\"ordinal\":".AsMemory(), cancellationToken).ConfigureAwait(false);
				await output.WriteAsync(columns[i].Ordinal.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken).ConfigureAwait(false);
				await output.WriteAsync(",\"name\":".AsMemory(), cancellationToken).ConfigureAwait(false);
				await WriteJsonString(output, columns[i].Name, cancellationToken).ConfigureAwait(false);
				await output.WriteAsync(",\"fieldType\":".AsMemory(), cancellationToken).ConfigureAwait(false);
				await WriteJsonString(output, columns[i].FieldType, cancellationToken).ConfigureAwait(false);
				await output.WriteAsync(",\"providerSpecificFieldType\":".AsMemory(), cancellationToken).ConfigureAwait(false);
				await WriteJsonString(output, columns[i].ProviderSpecificFieldType, cancellationToken).ConfigureAwait(false);
				await output.WriteAsync(",\"dataTypeName\":".AsMemory(), cancellationToken).ConfigureAwait(false);
				await WriteJsonString(output, columns[i].DataTypeName, cancellationToken).ConfigureAwait(false);
				await output.WriteAsync("}".AsMemory(), cancellationToken).ConfigureAwait(false);
			}

			await output.WriteAsync("],\"rows\":[".AsMemory(), cancellationToken).ConfigureAwait(false);
		}

		static async Task WriteJsonTableRow(TextWriter output, string?[] row, int rowIndex, CancellationToken cancellationToken)
		{
			if (rowIndex > 0)
				await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

			await output.WriteAsync("[".AsMemory(), cancellationToken).ConfigureAwait(false);

			for (var i = 0; i < row.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

				await WriteJsonValue(output, row[i], cancellationToken).ConfigureAwait(false);
			}

			await output.WriteAsync("]".AsMemory(), cancellationToken).ConfigureAwait(false);
		}

		static async Task WriteJsonTableEnd(TextWriter output, int rowCount, bool truncated, CancellationToken cancellationToken)
		{
			await output.WriteAsync("],\"rowCount\":".AsMemory(), cancellationToken).ConfigureAwait(false);
			await output.WriteAsync(rowCount.ToString(CultureInfo.InvariantCulture).AsMemory(), cancellationToken).ConfigureAwait(false);
			await output.WriteAsync(",\"truncated\":".AsMemory(), cancellationToken).ConfigureAwait(false);
			await output.WriteAsync((truncated ? "true" : "false").AsMemory(), cancellationToken).ConfigureAwait(false);
			await output.WriteAsync("}".AsMemory(), cancellationToken).ConfigureAwait(false);
		}

		static async Task WriteCsvHeader(TextWriter output, QueryOutputColumn[] columns, CancellationToken cancellationToken)
		{
			for (var i = 0; i < columns.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

				await WriteCsvValue(output, columns[i].Name, cancellationToken).ConfigureAwait(false);
			}

			await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken).ConfigureAwait(false);
		}

		static async Task WriteCsvRow(TextWriter output, string?[] row, CancellationToken cancellationToken)
		{
			for (var i = 0; i < row.Length; i++)
			{
				if (i > 0)
					await output.WriteAsync(",".AsMemory(), cancellationToken).ConfigureAwait(false);

				if (row[i] != null)
					await WriteCsvValue(output, row[i]!, cancellationToken).ConfigureAwait(false);
			}

			await output.WriteAsync(Environment.NewLine.AsMemory(), cancellationToken).ConfigureAwait(false);
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
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Binary")    => QueryActualFieldType.DB2Binary,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Blob")      => QueryActualFieldType.DB2Blob,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Clob")      => QueryActualFieldType.DB2Clob,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Date")      => QueryActualFieldType.DB2Date,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Time")      => QueryActualFieldType.DB2Time,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2TimeStamp") => QueryActualFieldType.DB2TimeStamp,
				_ when IsProviderSpecificType(providerSpecificType, "IBM.Data.DB2Types.DB2Xml")       => QueryActualFieldType.DB2Xml,
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

		static string? ReadFieldAsString(DbDataReader reader, QueryActualFieldType actualFieldType, int ordinal)
		{
			object value;

			if (actualFieldType == QueryActualFieldType.OracleBFile)
				return "<BFILE>";

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
				QueryActualFieldType.Boolean            => value is SqlBoolean sqlBoolean ? sqlBoolean.Value ? "true" : "false" : ((bool)value) ? "true" : "false",
				QueryActualFieldType.Double             => (value is SqlDouble sqlDouble ? sqlDouble.Value : (double)value).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.Single             => (value is SqlSingle sqlSingle ? sqlSingle.Value : (float)value).ToString("R", CultureInfo.InvariantCulture),
				QueryActualFieldType.Date               => FormatDate(value),
				QueryActualFieldType.DateTime           => (value is SqlDateTime sqlDateTime ? sqlDateTime.Value : (DateTime)value).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.DateTimeOffset     => ((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.TimeSpan           => ((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture),
				QueryActualFieldType.Guid               => (value is SqlGuid sqlGuid ? sqlGuid.Value : (Guid)value).ToString("D"),
				QueryActualFieldType.Bytes              => ConvertBytesToString((byte[])value),
				QueryActualFieldType.ByteArray          => ConvertByteArrayToString((byte[])value),
				QueryActualFieldType.SqlBinary          => ConvertBytesToString(((SqlBinary)value).Value),
				QueryActualFieldType.SqlBytes           => ConvertBytesToString(((SqlBytes)value).Value),
				QueryActualFieldType.SqlChars           => new string(((SqlChars)value).Value),
				QueryActualFieldType.SqlString          => ((SqlString)value).Value,
				QueryActualFieldType.SqlXml             => ((SqlXml)value).Value,
				QueryActualFieldType.SqlVectorFloat     => ConvertVectorToString(((SqlVector<float>)value).Memory.ToArray()),
				QueryActualFieldType.SqlVectorHalf      => ConvertVectorToString(((SqlVector<Half>) value).Memory.ToArray()),
				QueryActualFieldType.SqlHierarchyId     => ((SqlHierarchyId)value).ToString(),
				QueryActualFieldType.SqlGeometry        => ((SqlGeometry)value).ToString(),
				QueryActualFieldType.SqlGeography       => ((SqlGeography)value).ToString(),
				QueryActualFieldType.OracleBinary       => ConvertBytesToString(((OracleBinary)value).Value),
				QueryActualFieldType.OracleBlob         => ConvertBytesToString(((OracleBlob)value).Value),
				QueryActualFieldType.OracleClob         => ((OracleClob)value).Value,
				QueryActualFieldType.OracleXmlType      => ((OracleXmlType)value).Value,
				QueryActualFieldType.OracleDate         => FormatDate(((OracleDate)value).Value),
				QueryActualFieldType.OracleTimeStamp    => FormatOracleTimeStamp((OracleTimeStamp)value),
				QueryActualFieldType.OracleTimeStampTZ  => FormatOracleTimeStampTZ((OracleTimeStampTZ)value),
				QueryActualFieldType.OracleTimeStampLTZ => FormatOracleTimeStampLTZ((OracleTimeStampLTZ)value),
				QueryActualFieldType.DB2Binary          => ConvertBytesToString((byte[])GetProviderSpecificPropertyValue(value, "Value")),
				QueryActualFieldType.DB2Blob            => ConvertBytesToString((byte[])GetProviderSpecificPropertyValue(value, "Value")),
				QueryActualFieldType.DB2Clob            => (string)GetProviderSpecificPropertyValue(value, "Value"),
				QueryActualFieldType.DB2Date            => FormatDate((DateTime)GetProviderSpecificPropertyValue(value, "Value")),
				QueryActualFieldType.DB2Time            => ((TimeSpan)GetProviderSpecificPropertyValue(value, "Value")).ToString("c", CultureInfo.InvariantCulture),
				QueryActualFieldType.DB2TimeStamp       => ((DateTime)GetProviderSpecificPropertyValue(value, "Value")).ToString("O", CultureInfo.InvariantCulture),
				QueryActualFieldType.DB2Xml             => (string)GetProviderSpecificMethodValue(value, "GetString"),
				_                                       => ConvertValueToString(value),
			};
		}

		static string? ConvertValueToString(object? value)
		{
			return value switch
			{
				null                  => null,
				string stringValue    => stringValue,
				byte[] bytes          => ConvertBytesToString(bytes),
				DateOnly date         => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				TimeOnly time         => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
				ITuple tuple         => ConvertTupleToString(tuple),
				IEnumerable sequence => ConvertSequenceToString(sequence),
				_                     => Convert.ToString(value, CultureInfo.InvariantCulture),
			};
		}

		static string ConvertBytesToString(byte[] bytes)
		{
			return "0x" + Convert.ToHexString(bytes);
		}

		static bool IsDateDataType(string dataTypeName)
		{
			return string.Equals(dataTypeName, "Date", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Date32", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date)", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(dataTypeName, "Nullable(Date32)", StringComparison.OrdinalIgnoreCase);
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
				DateOnly date     => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
				_                 => Convert.ToString(value, CultureInfo.InvariantCulture)!,
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
				await output.WriteAsync(value.AsMemory(), cancellationToken).ConfigureAwait(false);
				return;
			}

			await output.WriteAsync("\"".AsMemory(), cancellationToken).ConfigureAwait(false);
			await output.WriteAsync(value.Replace("\"", "\"\"", StringComparison.Ordinal).AsMemory(), cancellationToken).ConfigureAwait(false);
			await output.WriteAsync("\"".AsMemory(), cancellationToken).ConfigureAwait(false);
		}
	}
}
