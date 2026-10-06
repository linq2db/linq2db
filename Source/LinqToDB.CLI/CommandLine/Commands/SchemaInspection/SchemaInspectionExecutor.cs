using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.SchemaProvider;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Provider-aware schema inspection execution logic.
	/// </summary>
	internal sealed class SchemaInspectionExecutor(SchemaInspectionSettings settings)
	{
		sealed class SchemaOutputLimitExceededException(int maxOutputBytes) : Exception
		{
			public int MaxOutputBytes { get; } = maxOutputBytes;
		}

		sealed class BoundedMemoryStream(int maxOutputBytes) : MemoryStream
		{
			public override void Write(byte[] buffer, int offset, int count)
			{
				EnsureCapacity(count);
				base.Write(buffer, offset, count);
			}

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				EnsureCapacity(buffer.Length);
				base.Write(buffer);
			}

			public override void WriteByte(byte value)
			{
				EnsureCapacity(1);
				base.WriteByte(value);
			}

			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
			{
				EnsureCapacity(count);
				return base.WriteAsync(buffer, offset, count, cancellationToken);
			}

			public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
			{
				EnsureCapacity(buffer.Length);
				return base.WriteAsync(buffer, cancellationToken);
			}

			void EnsureCapacity(int count)
			{
				if (Position > maxOutputBytes - count)
					throw new SchemaOutputLimitExceededException(maxOutputBytes);
			}
		}

		static readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented        = false,
		};

		readonly SchemaInspectionSettings _settings = settings;

		public async ValueTask<SchemaInspectionResult> Execute(TextWriter outputWriter, CancellationToken cancellationToken)
		{
			try
			{
				var result = await ConnectionExecution.RunAsync(
					_settings.Connection,
					ExecuteSchemaRead,
					cancellationToken);

				if (result.Error != null)
					return new SchemaInspectionResult(result.StatusCode, $"Schema inspection failed: {result.Error}");

				if (_settings.MaxOutputBytes is { } maxOutputBytes)
				{
					using var outputStream = new BoundedMemoryStream(maxOutputBytes);

					JsonSerializer.Serialize(outputStream, result.Value, _jsonOptions);

					await outputWriter.WriteAsync(
						Encoding.UTF8.GetString(outputStream.GetBuffer(), 0, checked((int)outputStream.Length)).AsMemory(),
						cancellationToken);
				}
				else
				{
					await outputWriter.WriteAsync(JsonSerializer.Serialize(result.Value, _jsonOptions).AsMemory(), cancellationToken);
				}

				await outputWriter.FlushAsync(cancellationToken);

				return new SchemaInspectionResult(StatusCodes.SUCCESS, null);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (RegexMatchTimeoutException ex)
			{
				return new SchemaInspectionResult(StatusCodes.EXPECTED_ERROR, $"Schema inspection failed: Table filter regex '{ex.Pattern}' timed out after {((int)ex.MatchTimeout.TotalMilliseconds).ToString(CultureInfo.InvariantCulture)} ms.");
			}
			catch (SchemaOutputLimitExceededException ex)
			{
				return new SchemaInspectionResult(
					StatusCodes.EXPECTED_ERROR,
					$"Schema output exceeds the configured maximum response size of {ex.MaxOutputBytes.ToString(CultureInfo.InvariantCulture)} bytes. Use filterSchemas, filterCatalogs, or filterTables to request a smaller schema.");
			}
			catch (Exception ex)
			{
				return new SchemaInspectionResult(StatusCodes.EXPECTED_ERROR, $"Schema inspection failed: {ex.Message}");
			}
		}

		Task<object> ExecuteSchemaRead(DataOptions dataOptions, IDataProvider dataProvider, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			using var dataConnection = new DataConnection(dataOptions);
			var schemaOptions        = CreateSchemaOptions(_settings.Options);
			var schemaProvider       = dataProvider.GetSchemaProvider();
			var schema               = schemaProvider.GetSchema(dataConnection, schemaOptions);

			cancellationToken.ThrowIfCancellationRequested();

			return Task.FromResult(MapSchema(schema));
		}

		static GetSchemaOptions CreateSchemaOptions(SchemaInspectionEffectiveOptions options)
		{
			var tableNamePredicates = CreateTableNamePredicates(options.FilterTables);

			return new GetSchemaOptions
			{
				PreferProviderSpecificTypes = options.PreferProviderSpecificTypes,
				GetTables                   = options.GetTables,
				GetForeignKeys              = options.GetForeignKeys,
				GetProcedures               = false,
				GenerateChar1AsString       = options.GenerateChar1AsString,
				IgnoreSystemHistoryTables   = options.IgnoreSystemHistoryTables,
				DefaultSchema               = options.DefaultSchema,
				IncludedSchemas             = options.FilterSchemas.Length  == 0 ? null : options.FilterSchemas,
				IncludedCatalogs            = options.FilterCatalogs.Length == 0 ? null : options.FilterCatalogs,
				LoadProcedure               = _ => false,
				LoadTable                   = tableNamePredicates.Length == 0 ? null : table => ShouldLoadTable(table, tableNamePredicates),
			};

			static bool ShouldLoadTable(LoadTableData table, Func<string[], bool>[] tableNamePredicates)
			{
				var names = CreateTableNames(table);

				foreach (var predicate in tableNamePredicates)
					if (predicate(names))
						return true;

				return false;
			}

			static Func<string[], bool>[] CreateTableNamePredicates(string[] filters)
			{
				var predicates = new Func<string[], bool>[filters.Length];

				for (var i = 0; i < filters.Length; i++)
				{
					var filter       = filters[i];
					var regexPattern = GetRegexPattern(filter);

					if (regexPattern != null)
					{
						var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, SchemaFilterConstants.RegexTimeout);
						predicates[i] = names =>
						{
							foreach (var name in names)
								if (regex.IsMatch(name))
									return true;

							return false;
						};
					}
					else
					{
						predicates[i] = names =>
						{
							foreach (var name in names)
								if (string.Equals(name, filter, StringComparison.OrdinalIgnoreCase))
									return true;

							return false;
						};
					}
				}

				return predicates;

				static string? GetRegexPattern(string filter)
				{
					if (filter.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
						return filter.Substring("regex:".Length);

					if (filter.StartsWith("rx:", StringComparison.OrdinalIgnoreCase))
						return filter.Substring("rx:".Length);

					return null;
				}
			}

			static string[] CreateTableNames(LoadTableData table)
			{
				if (string.IsNullOrEmpty(table.Name))
					return [];

				if (!string.IsNullOrEmpty(table.Database) && !string.IsNullOrEmpty(table.Schema))
					return [table.Name, $"{table.Schema}.{table.Name}", $"{table.Database}.{table.Schema}.{table.Name}"];

				if (!string.IsNullOrEmpty(table.Schema))
					return [table.Name, $"{table.Schema}.{table.Name}"];

				return [table.Name];
			}
		}

		object MapSchema(DatabaseSchema schema)
		{
			var tables = schema.Tables
				.Where (t => !t.IsProcedureResult && !string.IsNullOrEmpty(t.TableName))
				.ToArray();

			if (string.Equals(_settings.Options.DetailLevel, SchemaInspectionSettingsResolver.NamesDetailLevel, StringComparison.Ordinal))
			{
				return new SchemaObjectNamesDto(
					_settings.Connection.Provider,
					ProviderDialectCatalog.GetDialect(_settings.Connection.Provider),
					schema.Database,
					_settings.Options,
					tables.Select(table => new SchemaObjectNameDto(
						string.IsNullOrEmpty(table.CatalogName) ? null : table.CatalogName,
						string.IsNullOrEmpty(table.SchemaName)  ? null : table.SchemaName,
						table.TableName!,
						GetTableKind(table))).ToArray(),
					[]);
			}

			return new SchemaInspectionDto(
				_settings.Connection.Provider,
				ProviderDialectCatalog.GetDialect(_settings.Connection.Provider),
				schema.Database,
				_settings.Options,
				tables.Select(MapTable).ToArray(),
				[]);
		}

		static SchemaTableDto MapTable(TableSchema table)
		{
			var columns    = table.Columns.Select(MapColumn).ToArray();
			var primaryKey = columns
				.Where  (c => c.PrimaryKey)
				.OrderBy(c => c.PrimaryKeyOrder ?? int.MaxValue)
				.Select (c => new SchemaPrimaryKeyColumnDto(c.Name, c.PrimaryKeyOrder ?? 0))
				.ToArray();

			return new SchemaTableDto(
				table.CatalogName,
				table.SchemaName,
				table.TableName,
				GetTableKind(table),
				table.Description,
				columns,
				primaryKey.Length == 0 ? null : new SchemaPrimaryKeyDto(primaryKey),
				table.ForeignKeys.Where(foreignKey => foreignKey.BackReference != null).Select(MapForeignKey).ToArray());

			static SchemaColumnDto MapColumn(ColumnSchema column)
			{
				return new SchemaColumnDto(
					column.ColumnName,
					column.Ordinal,
					column.ColumnType,
					column.DataType.ToString(),
					column.SystemType?.FullName,
					column.ProviderSpecificType,
					column.IsNullable,
					column.IsIdentity,
					column.IsPrimaryKey,
					column.IsPrimaryKey ? column.PrimaryKeyOrder : null,
					column.Length,
					column.Precision,
					column.Scale,
					column.Description);
			}

			static SchemaForeignKeyDto MapForeignKey(ForeignKeySchema foreignKey)
			{
				return new SchemaForeignKeyDto(
					foreignKey.KeyName,
					foreignKey.ThisColumns.Select(c => c.ColumnName).ToArray(),
					new SchemaObjectRefDto(
						foreignKey.OtherTable.CatalogName,
						foreignKey.OtherTable.SchemaName,
						foreignKey.OtherTable.TableName),
					foreignKey.OtherColumns.Select(c => c.ColumnName).ToArray(),
					foreignKey.CanBeNull);
			}
		}

		static string GetTableKind(TableSchema table)
		{
			if (table.IsView)
				return "view";

			if (table.IsProviderSpecific)
				return "provider-specific";

			return "table";
		}
	}
}
