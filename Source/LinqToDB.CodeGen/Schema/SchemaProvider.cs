using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.CodeGen.Model;
using LinqToDB.Data;
using LinqToDB.SchemaProvider;

namespace LinqToDB.CodeGen.Schema
{
	/// <summary>
	/// Default schema provider implementation over existing <see cref="LinqToDB.SchemaProvider.ISchemaProvider.GetSchema(DataConnection, GetSchemaOptions?)"/> API.
	/// </summary>
	public class SchemaProvider : ISchemaProvider, ITypeMappingProvider
	{
		private readonly List<Table> _tables = new ();
		private readonly List<View> _views = new ();

		private readonly List<ForeignKey> _foreignKeys = new ();

		private readonly List<StoredProcedure> _procedures = new ();
		private readonly List<TableFunction> _tableFunctions = new ();
		private readonly List<ScalarFunction> _scalarFunctions = new ();
		private readonly List<AggregateFunction> _aggregateFunctions = new ();

		private string? _databaseName;
		private string? _serverVersion;
		private string? _dataSource;

		private readonly Dictionary<DatabaseType, (IType clrType, DataType? dataType)> _typeMappings = new ();

		private readonly ILanguageProvider _languageProvider;
		private readonly SchemaSettings _settings;
		private readonly ISet<string> _defaultSchemas = new HashSet<string>();

		// TODO: remove, used for debugging
		private readonly string _providerName;

		public SchemaProvider(DataConnection connection, SchemaSettings settings, ILanguageProvider languageProvider)
		{
			_settings = settings;
			_languageProvider = languageProvider;

			var schemaProvider = connection.DataProvider.GetSchemaProvider();
			_providerName = connection.DataProvider.Name;
			var schema = schemaProvider.GetSchema(connection, CreateSchemaOptions(settings));

			ParseSchema(schema, settings.Objects);
		}

		//[MemberNotNull(nameof(_databaseName), nameof(_serverVersion))]
		private void ParseSchema(DatabaseSchema schema, SchemaObjects loadedObjects)
		{
			_databaseName = string.IsNullOrWhiteSpace(schema.Database) ? null : schema.Database;
			_serverVersion = string.IsNullOrWhiteSpace(schema.ServerVersion) ? null : schema.ServerVersion;
			_dataSource = string.IsNullOrWhiteSpace(schema.DataSource) ? null : schema.DataSource;

			if (loadedObjects.HasFlag(SchemaObjects.Table) || loadedObjects.HasFlag(SchemaObjects.View))
			{
				foreach (var table in schema.Tables)
					ParseTable(table, loadedObjects);
			}

			// TODO: pgsql schema provider returns table function column descriptors as output parameters
			var skipNonInputParameters = _providerName.Contains(ProviderName.PostgreSQL);

			if (loadedObjects.HasFlag(SchemaObjects.StoredProcedure)
				|| loadedObjects.HasFlag(SchemaObjects.TableFunction)
				|| loadedObjects.HasFlag(SchemaObjects.ScalarFunction)
				|| loadedObjects.HasFlag(SchemaObjects.AggregateFunction))
				foreach (var proc in schema.Procedures)
					ParseCallable(proc, loadedObjects, skipNonInputParameters && proc.IsTableFunction);
		}

		private void ParseCallable(ProcedureSchema proc, SchemaObjects loadedObjects, bool onlyInputParameters)
		{
			var load = !proc.IsFunction && loadedObjects.HasFlag(SchemaObjects.StoredProcedure)
				|| proc.IsTableFunction && loadedObjects.HasFlag(SchemaObjects.TableFunction)
				|| proc.IsAggregateFunction && loadedObjects.HasFlag(SchemaObjects.AggregateFunction)
				|| proc.IsFunction && !proc.IsAggregateFunction && !proc.IsTableFunction && loadedObjects.HasFlag(SchemaObjects.ScalarFunction);

			if (!load)
				return;

			// TODO: debug exceptions
			if (proc.IsResultDynamic)
				throw new InvalidOperationException($"IsResultDynamic provided by schema");
			if (proc.IsLoaded && !(!proc.IsFunction || proc.IsTableFunction))
				throw new InvalidOperationException($"IsLoaded set for scalar/aggregate function");
			if (proc.ResultTable != null && !(!proc.IsFunction || proc.IsTableFunction))
				throw new InvalidOperationException($"ResultTable set for scalar/aggregate function");
			if (proc.ResultTable != null && !proc.IsLoaded)
				throw new InvalidOperationException($"ResultTable specified but IsLoaded not set");

			if (proc.IsDefaultSchema && !string.IsNullOrWhiteSpace(proc.SchemaName))
				_defaultSchemas.Add(proc.SchemaName!);

			var name = new ObjectName(
				null,
				string.IsNullOrWhiteSpace(proc.CatalogName) ? null : proc.CatalogName,
				string.IsNullOrWhiteSpace(proc.SchemaName) ? null : proc.SchemaName,
				proc.ProcedureName);
			var description = string.IsNullOrWhiteSpace(proc.Description) ? null : proc.Description;

			Exception? schemaError = null;
			if (proc.ResultException != null)
			{
				if (!proc.IsFunction || proc.IsTableFunction)
					schemaError = proc.ResultException;
				else
					throw new InvalidOperationException($"ResultException set for scalar/aggregate function");
			}

			var resultSet = ParseResultSet(proc);
			var (parameters, result) = ParseParameters(proc, onlyInputParameters);

			// information not provided by schema API
			var isSystem = false;

			if (!proc.IsFunction)
				_procedures.Add(new StoredProcedure(name, description, isSystem, parameters, schemaError, resultSet != null ? new List<IReadOnlyCollection<ResultColumn>>() { resultSet } : null, result));
			else if (proc.IsTableFunction)
				_tableFunctions.Add(new TableFunction(name, description, isSystem, parameters, schemaError, resultSet));
			else if (proc.IsAggregateFunction)
			{
				if (result is not ScalarResult scalarResult)
					throw new InvalidOperationException("Unsupported result type for aggregate function");
				_aggregateFunctions.Add(new AggregateFunction(name, description, isSystem, parameters, scalarResult));
			}
			else
				_scalarFunctions.Add(new ScalarFunction(name, description, isSystem, parameters, result));

			///// <summary>
			///// Gets list of procedure parameters.
			///// </summary>
			//public List<ParameterSchema> Parameters { get; set; } = null!;
		}

		private (List<Parameter> parameters, Result result) ParseParameters(ProcedureSchema proc, bool onlyInputs)
		{
			// scalar pgsql function
			var detectTuples = proc.IsFunction && !proc.IsTableFunction && !proc.IsAggregateFunction
				&& _providerName.Contains(ProviderName.PostgreSQL);
			List<ScalarResult>? tupleColumns = null;

			var parameters = new List<Parameter>();
			Result? result = null;
			foreach (var param in proc.Parameters)
			{
				if (onlyInputs && (!param.IsIn || param.IsOut || param.IsResult))
					continue;

				if (string.IsNullOrWhiteSpace(param.SchemaType))
					throw new InvalidOperationException("Parameter missing SchemaType");

				var name = param.ParameterName;
				var description = string.IsNullOrWhiteSpace(param.Description) ? null : param.Description;

				// precison/scale not provided by API
				var type = new DatabaseType(param.SchemaType!, param.Size, null, null);
				RegisterType(type, param.DataType, param.SystemType, param.ProviderSpecificType);

				if (param.IsResult)
				{
					if (result != null)
						throw new InvalidOperationException("Multiple result parameters found");
					if (description != null)
						throw new InvalidOperationException("Result parameter has Description");

					result = new ScalarResult(name, type, param.IsNullable);
				}
				else
				{
					if (string.IsNullOrWhiteSpace(name))
						throw new InvalidOperationException("Parameter name missing");

					if (detectTuples && param.IsOut)
					{
						(tupleColumns ??= new()).Add(new ScalarResult(name, type, param.IsNullable));
						if (param.IsIn)
							param.IsOut = false;
						else
							continue;
					}

					var direction = !param.IsIn
						? ParameterDirection.Output
						: (!param.IsOut ? ParameterDirection.Input : ParameterDirection.InputOutput);

					parameters.Add(new Parameter(name, description, type, param.IsNullable, direction));
				}
			}

			if (result == null)
			{
				if (tupleColumns != null)
				{
					if (tupleColumns.Count == 1)
						result = tupleColumns[0];
					else
						// always nullable?
						result = new TupleResult(tupleColumns, true);
				}
				else
					result = new VoidResult();
			}

			return (parameters, result);
		}

		private List<ResultColumn>? ParseResultSet(ProcedureSchema proc)
		{
			List<ResultColumn>? resultSet = null;
			if (proc.ResultTable != null)
			{
				resultSet = new();

				if (proc.ResultTable.CatalogName != null)
					throw new InvalidOperationException($"Result set CatalogName set by schema");
				if (proc.ResultTable.SchemaName != null)
					throw new InvalidOperationException($"Result set SchemaName set by schema");
				if (proc.ResultTable.TableName != null)
					throw new InvalidOperationException($"Result set TableName set by schema");
				if (proc.ResultTable.Description != null)
					throw new InvalidOperationException($"Result set Description set by schema");
				if (proc.ResultTable.IsDefaultSchema)
					throw new InvalidOperationException($"Result set IsDefaultSchema set by schema");
				if (proc.ResultTable.IsView)
					throw new InvalidOperationException($"Result set IsView set by schema");
				if (proc.ResultTable.IsProviderSpecific)
					throw new InvalidOperationException($"Result set IsProviderSpecific set by schema");
				if (!proc.ResultTable.IsProcedureResult)
					throw new InvalidOperationException($"Result set IsProcedureResult not set by schema");
				if (proc.ResultTable.ForeignKeys.Count > 0)
					throw new InvalidOperationException($"Result set ForeignKeys set by schema");

				foreach (var column in proc.ResultTable.Columns)
					resultSet.Add(ParseProcedureColumn(column));
			}

			return resultSet;
		}

		private ResultColumn ParseProcedureColumn(ColumnSchema column)
		{
			if (string.IsNullOrWhiteSpace(column.ColumnType))
				throw new InvalidOperationException($"ColumnType not provided by schema");
			if (!string.IsNullOrWhiteSpace(column.Description))
				throw new InvalidOperationException($"Result set column Description provided by schema");
			if (column.SkipOnInsert)
				throw new InvalidOperationException($"Result set column SkipOnInsert is set");
			if (column.SkipOnUpdate)
				throw new InvalidOperationException($"Result set column SkipOnInsert is set");
			if (column.IsPrimaryKey)
				throw new InvalidOperationException($"Result set column IsPrimaryKey is set");
			if (column.PrimaryKeyOrder != 0)
				throw new InvalidOperationException($"Result set column PrimaryKeyOrder is set");

			var type = new DatabaseType(column.ColumnType!, column.Length, column.Precision, column.Scale);

			RegisterType(type, column.DataType, column.SystemType, column.ProviderSpecificType);

			return new ResultColumn(string.IsNullOrEmpty(column.ColumnName) ? null : column.ColumnName, type, column.IsNullable);
		}

		private void ParseTable(TableSchema table, SchemaObjects loadedObjects)
		{
			var load = table.IsView && loadedObjects.HasFlag(SchemaObjects.View)
				|| !table.IsView && loadedObjects.HasFlag(SchemaObjects.Table);

			if (!load)
				return;

			// TODO: debug exceptions
			if (string.IsNullOrWhiteSpace(table.TableName))
				throw new InvalidOperationException($"TableName not provided by schema");
			if (table.IsProcedureResult)
				throw new InvalidOperationException($"IsProcedureResult set by schema");
			if (table.IsProviderSpecific)
			{
				// TODO: don't load system tables without request
				if (_providerName == "Access" || _providerName == "Access.Odbc")
					return;
				throw new InvalidOperationException($"IsProviderSpecific set by schema");
			}

			if (table.IsDefaultSchema && !string.IsNullOrWhiteSpace(table.SchemaName))
				_defaultSchemas.Add(table.SchemaName!);

			var tableName = GetTableName(table);

			var description = string.IsNullOrWhiteSpace(table.Description) ? null : table.Description;

			var columns = new List<Column>();
			PrimaryKey? primaryKey = null;
			Identity? identity = null;
			bool hasPrimaryKey = false;

			foreach (var column in table.Columns)
			{
				var columnSchema = ParseColumn(column);
				columns.Add(columnSchema);

				// TODO: fix access oldedb schema provider to not report identity for all non-nullable INT columns
				if (column.IsIdentity && _providerName != "Access")
				{
					if (identity != null)
					{
						throw new InvalidOperationException($"Duplicate identity found on table {tableName}");
					}

					identity = new Identity(column.ColumnName, null);
				}

				if (column.IsPrimaryKey)
				{
					if (table.IsView)
						throw new InvalidOperationException($"Primary key found on view {tableName}");

					hasPrimaryKey = true;
				}
			}

			if (hasPrimaryKey)
			{
				var pkColumns = table.Columns.Where(c => c.IsPrimaryKey);
				if (pkColumns.Count() != pkColumns.Select(c => c.PrimaryKeyOrder).Distinct().Count())
					throw new InvalidOperationException($"Primary key columns have duplicate ordinals");

				primaryKey = new PrimaryKey(null, table.Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.PrimaryKeyOrder).Select(c => c.ColumnName).ToList());
			}

			if (loadedObjects.HasFlag(SchemaObjects.ForeignKey))
				foreach (var fk in table.ForeignKeys)
					ParseForeignKey(fk);

			// data not provided by API
			var isSystem = false;

			if (table.IsView)
				_views.Add(new View(tableName, description, isSystem, columns, identity));
			else
				_tables.Add(new Table(tableName, description, isSystem, columns, identity, primaryKey));
		}

		private static ObjectName GetTableName(TableSchema table)
		{
			return new ObjectName(
				null,
				string.IsNullOrWhiteSpace(table.CatalogName) ? null : table.CatalogName,
				string.IsNullOrWhiteSpace(table.SchemaName) ? null : table.SchemaName,
				table.TableName!);
		}

		private void ParseForeignKey(ForeignKeySchema fk)
		{
			// exclude fake backreference FK
			// detected by not set ThisTable property
			if (fk.ThisTable == null)
				return;


			if (fk.ThisColumns.Count != fk.OtherColumns.Count)
				throw new InvalidOperationException($"Foreign key has different number of columns on both sides");
			if (fk.ThisColumns.Count == 0)
				throw new InvalidOperationException($"Foreign key has no columns");

			var relation = new (string SourceColumn, string TargetColumn)[fk.ThisColumns.Count];
			for (var i = 0; i < fk.ThisColumns.Count; i++)
				relation[i] = (fk.ThisColumns[i].ColumnName, fk.OtherColumns[i].ColumnName);

			_foreignKeys.Add(new ForeignKey(fk.KeyName, GetTableName(fk.ThisTable), GetTableName(fk.OtherTable), relation));
		}

		private Column ParseColumn(ColumnSchema column)
		{
			if (string.IsNullOrWhiteSpace(column.ColumnName))
				throw new InvalidOperationException($"ColumnName not provided by schema");
			if (string.IsNullOrWhiteSpace(column.ColumnType))
			{
				if (_providerName == "SQLite.Classic")
				{
					// TODO: generate exception/log message?
					// numeric is default column type affinity for sqlite 3 (still, it is wrong for specific case)
					column.ColumnType = "NUMERIC";
				}
				else
					throw new InvalidOperationException($"ColumnType not provided by schema");
			}

			var type = new DatabaseType(column.ColumnType!, column.Length, column.Precision, column.Scale);
			
			RegisterType(type, column.DataType, column.SystemType, column.ProviderSpecificType);

			return new Column(
				column.ColumnName,
				string.IsNullOrWhiteSpace(column.Description) ? null : column.Description,
				type,
				column.IsNullable,
				!column.SkipOnInsert,
				!column.SkipOnUpdate);
		}

		private void RegisterType(DatabaseType dbType, DataType dataType, Type systemType, string? providerSpecificType)
		{
			IType type;
			if (_settings.PreferProviderSpecificTypes && !string.IsNullOrWhiteSpace(providerSpecificType))
			{
				// TODO: cache types and sync with schema provider implementations
				switch (providerSpecificType)
				{
					case "MySqlDecimal": type = _languageProvider.TypeParser.Parse("MySql.Data.Types.MySqlDecimal", true); break;
					case "MySqlDateTime": type = _languageProvider.TypeParser.Parse("MySql.Data.Types.MySqlDateTime", true); break;
					case "MySqlGeometry": type = _languageProvider.TypeParser.Parse("MySql.Data.Types.MySqlGeometry", true); break;

					case "FbZonedDateTime": type = _languageProvider.TypeParser.Parse("FirebirdSql.Data.Types.FbZonedDateTime", true); break;
					case "FbZonedTime": type = _languageProvider.TypeParser.Parse("FirebirdSql.Data.Types.FbZonedTime", true); break;
					case "FbDecFloat": type = _languageProvider.TypeParser.Parse("FirebirdSql.Data.Types.FbDecFloat", true); break;

					case "NpgsqlDateTime": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlDateTime", true); break;
					case "NpgsqlDate": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlDate", true); break;
					case "NpgsqlPoint": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlPoint", true); break;
					case "NpgsqlLSeg": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlLSeg", true); break;
					case "NpgsqlBox": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlBox", true); break;
					case "NpgsqlPath": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlPath", true); break;
					case "NpgsqlPolygon": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlPolygon", true); break;
					case "NpgsqlCircle": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlCircle", true); break;
					case "NpgsqlLine": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlLine", true); break;
					case "NpgsqlInet": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlInet", true); break;

					case "Microsoft.SqlServer.Types.SqlHierarchyId": type = WellKnownTypes.Microsoft.SqlServer.Types.SqlHierarchyId; break;
					case "Microsoft.SqlServer.Types.SqlGeography": type = _languageProvider.TypeParser.Parse("Microsoft.SqlServer.Types.SqlGeography", false); break;
					case "Microsoft.SqlServer.Types.SqlGeometry": type = _languageProvider.TypeParser.Parse("Microsoft.SqlServer.Types.SqlGeometry", false); break;

					case "SqlString": type = WellKnownTypes.System.Data.SqlTypes.SqlString; break;
					case "SqlByte": type = WellKnownTypes.System.Data.SqlTypes.SqlByte; break;
					case "SqlInt16": type = WellKnownTypes.System.Data.SqlTypes.SqlInt16; break;
					case "SqlInt32": type = WellKnownTypes.System.Data.SqlTypes.SqlInt32; break;
					case "SqlInt64": type = WellKnownTypes.System.Data.SqlTypes.SqlInt64; break;
					case "SqlDecimal": type = WellKnownTypes.System.Data.SqlTypes.SqlDecimal; break;
					case "SqlMoney": type = WellKnownTypes.System.Data.SqlTypes.SqlMoney; break;
					case "SqlSingle": type = WellKnownTypes.System.Data.SqlTypes.SqlSingle; break;
					case "SqlDouble": type = WellKnownTypes.System.Data.SqlTypes.SqlDouble; break;
					case "SqlBoolean": type = WellKnownTypes.System.Data.SqlTypes.SqlBoolean; break;
					case "SqlDateTime": type = WellKnownTypes.System.Data.SqlTypes.SqlDateTime; break;
					case "SqlBinary": type = WellKnownTypes.System.Data.SqlTypes.SqlBinary; break;
					case "SqlGuid": type = WellKnownTypes.System.Data.SqlTypes.SqlGuid; break;
					case "SqlXml": type = WellKnownTypes.System.Data.SqlTypes.SqlXml; break;

					case "DB2Binary": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Binary", true); break;
					case "DB2Blob": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Blob", false); break;
					case "DB2Clob": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Clob", false); break;
					case "DB2Date": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Date", true); break;
					case "DB2DateTime": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2DateTime", true); break;
					case "DB2Decimal": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Decimal", true); break;
					case "DB2DecimalFloat": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2DecimalFloat", true); break;
					case "DB2Double": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Double", true); break;
					case "DB2Int16": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int16", true); break;
					case "DB2Int32": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int32", true); break;
					case "DB2Int64": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int64", true); break;
					case "DB2Real": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Real", true); break;
					case "DB2Real370": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Real370", true); break;
					case "DB2RowId": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2RowId", true); break;
					case "DB2String": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2String", true); break;
					case "DB2Time": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Time", true); break;
					case "DB2TimeStamp": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2TimeStamp", true); break;
					case "DB2Xml": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Xml", false); break;
					case "DB2TimeSpan": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2TimeSpan", true); break;

						// TODO: ODP.NET provider use other namespace
					case "OracleBFile": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleBFile", false); break;
					case "OracleBinary": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleBinary", true); break;
					case "OracleBlob": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleBlob", false); break;
					case "OracleClob": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleClob", false); break;
					case "OracleDate": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleDate", true); break;
					case "OracleDecimal": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleDecimal", true); break;
					case "OracleIntervalDS": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleIntervalDS", true); break;
					case "OracleIntervalYM": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleIntervalYM", true); break;
					case "OracleString": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleString", true); break;
					case "OracleTimeStamp": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleTimeStamp", true); break;
					case "OracleTimeStampLTZ": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleTimeStampLTZ", true); break;
					case "OracleTimeStampTZ": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleTimeStampTZ", true); break;
					case "OracleXmlType": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleXmlType", false); break;
					case "OracleXmlStream": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleXmlStream", false); break;
					case "OracleRefCursor": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleRefCursor", false); break;
					case "OracleRef": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleRef", true); break;

					default:
						throw new InvalidOperationException($"Unknown provider-specific type {providerSpecificType}");
				}
			}
			else
				type = _languageProvider.TypeParser.Parse(systemType);

			if (type.IsNullable)
				throw new InvalidOperationException("Nullability specified on type");

			var dt = dataType == DataType.Undefined ? (DataType?)null : dataType;

			if (_typeMappings.TryGetValue(dbType, out var registeredType))
			{
				// validate that there is no conflicting mappings
				// validate that there is no conflicting mappings
				if (!_languageProvider.TypeEqualityComparerWithNRT.Equals(registeredType.clrType, type))
				{
					if (_providerName == "MySql.Official" || _providerName == "MySqlConnector")
					{
						// TODO: fix Mysql schema provider to resolve types properly
						// now it can return different types for column vs parameter, e.g. loose UNSIGNED modifier
					}
					else
						throw new InvalidOperationException($"Type {dbType} mapped to multiple types: ({registeredType.clrType}, {registeredType.dataType}) vs ({type}, {dt})");
				}
				if (registeredType.dataType != dt)
				{
					if (_providerName == "SQLite.Classic")
					{
						if (registeredType.dataType == DataType.Char && dt == DataType.NChar)
							_typeMappings[dbType] = (type, dt);
						else if (registeredType.dataType == DataType.NChar && dt == DataType.Char)
						{
						}
						else
							throw new InvalidOperationException($"Type {dbType} mapped to multiple types: ({registeredType.clrType}, {registeredType.dataType}) vs ({type}, {dt})");
					}
					else if (_providerName == "MySql.Official" || _providerName == "MySqlConnector")
					{
						// TODO: fix Mysql schema provider to resolve types properly
						// now it can return different types for column vs parameter, e.g. loose UNSIGNED modifier
					}
					else
						throw new InvalidOperationException($"Type {dbType} mapped to multiple types: ({registeredType.clrType}, {registeredType.dataType}) vs ({type}, {dt})");
				}
			}
			else
				_typeMappings.Add(dbType, (type, dt));
		}

		private static GetSchemaOptions CreateSchemaOptions(SchemaSettings settings)
		{
			var options = new GetSchemaOptions();

			options.PreferProviderSpecificTypes = settings.PreferProviderSpecificTypes;

			// requires post-load filtering
			options.GetTables = settings.Objects.HasFlag(SchemaObjects.Table) || settings.Objects.HasFlag(SchemaObjects.View);

			options.GetForeignKeys = settings.Objects.HasFlag(SchemaObjects.ForeignKey) && settings.Objects.HasFlag(SchemaObjects.Table);

			// requires post-load filtering
			options.GetProcedures = settings.Objects.HasFlag(SchemaObjects.StoredProcedure)
				|| settings.Objects.HasFlag(SchemaObjects.ScalarFunction)
				|| settings.Objects.HasFlag(SchemaObjects.TableFunction)
				|| settings.Objects.HasFlag(SchemaObjects.AggregateFunction);

			if (settings.IncludeSchemas)
				options.IncludedSchemas = settings.Schemas?.ToArray();
			else
				options.ExcludedSchemas = settings.Schemas?.ToArray();

			if (settings.IncludeCatalogs)
				options.IncludedCatalogs = settings.Catalogs?.ToArray();
			else
				options.ExcludedCatalogs = settings.Catalogs?.ToArray();

			options.LoadProcedure = p =>
			{
				var name = new ObjectName(null, null, p.SchemaName, p.ProcedureName);
				if (!p.IsFunction)
					return settings.LoadProceduresSchema && settings.LoadProcedureSchema(name);

				if (p.IsTableFunction)
					return settings.LoadTableFunctionsSchema && settings.LoadTableFunctionSchema(name);

				throw new InvalidOperationException($"{nameof(GetSchemaOptions)}.{nameof(GetSchemaOptions.LoadProcedure)} called for non-table returning object {p.ProcedureName}");
			};

			options.LoadTable = t =>
			{
				var name = new ObjectName(null, null, t.Schema, t.Name);

				if (t.IsView)
					return settings.LoadView(name);

				return settings.LoadTable(name);
			};

			options.UseSchemaOnly = settings.UseSafeSchemaLoad;

			// TODO: review unconfigured options
			//options.DefaultSchema = null;
			//options.GenerateChar1AsString;
			//options.StringComparer;
			//options.GetAssociationMemberName
			//options.ProcedureLoadingProgress

			return options;
		}

		#region ISchemaProvider
		IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions() => _aggregateFunctions;
		IEnumerable<StoredProcedure> ISchemaProvider.GetProcedures(bool withSchema, bool safeSchemaOnly) => _procedures;
		IEnumerable<ScalarFunction> ISchemaProvider.GetScalarFunctions() => _scalarFunctions;
		IEnumerable<TableFunction> ISchemaProvider.GetTableFunctions(bool withSchema, bool safeSchemaOnly) => _tableFunctions;

		IEnumerable<Table> ISchemaProvider.GetTables() => _tables;
		IEnumerable<View> ISchemaProvider.GetViews() => _views;

		IEnumerable<ForeignKey> ISchemaProvider.GetForeignKeys() => _foreignKeys;

		ISet<string> ISchemaProvider.GetDefaultSchemas() => _defaultSchemas;

		string? ISchemaProvider.DatabaseName => _databaseName;
		string? ISchemaProvider.ServerVersion => _serverVersion;
		string? ISchemaProvider.DataSource => _dataSource;
		#endregion

		#region ITypeMappingProvider
		(IType clrType, DataType? dataType)? ITypeMappingProvider.GetTypeMapping(DatabaseType databaseType)
		{
			if (_typeMappings.TryGetValue(databaseType, out var value))
				return value;

			return null;
		}

		#endregion
	}
}
