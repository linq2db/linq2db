using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.Scaffold;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlQuery;

namespace LinqToDB.Schema
{
	// Because linq2db schema API is quite an abomination, created to leverage T4 functionality and includes not
	// only schema but also names generation for generated code and type mapping of database types to .net types.
	// To not work with this API directly, we introduced schema provider interface and perform conversion of data
	// we get from linq2db API to new API format.
	// Later we plan to introduce new cleaner schema API, based on ISchemaProvider
	/// <summary>
	/// Default schema provider implementation over existing <see cref="SchemaProvider.ISchemaProvider.GetSchema(DataConnection, GetSchemaOptions?)"/> API.
	/// </summary>
	public sealed class LegacySchemaProvider : ISchemaProvider, ITypeMappingProvider
	{
		// cached results of linq2db API results conversion to schema models
		private readonly List<Table>             _tables             = new ();
		private readonly List<View>              _views              = new ();
		private readonly List<ForeignKey>        _foreignKeys        = new ();
		private readonly List<StoredProcedure>   _procedures         = new ();
		private readonly List<TableFunction>     _tableFunctions     = new ();
		private readonly List<ScalarFunction>    _scalarFunctions    = new ();
		private readonly List<AggregateFunction> _aggregateFunctions = new ();

		private string? _databaseName;
		private string? _serverVersion;
		private string? _dataSource;

		// we don't perform type mapping here as legacy API already does it
		// we just populate mappings from legacy API
		private readonly Dictionary<DatabaseType, TypeMapping> _typeMappings = new ();

		private readonly ILanguageProvider _languageProvider;
		private readonly SchemaOptions     _options;
		private readonly ISet<string>      _defaultSchemas = new HashSet<string>();

		// database provider name, used to workaround provider-specific issues with legacy API...
		private readonly string _providerName;
		private readonly bool   _isPostgreSql;
		private readonly bool   _isMySqlOrMariaDB;
		private readonly bool   _isAccessOleDb;
		private readonly bool   _isAccessOdbc;
		private readonly bool   _isSystemDataSqlite;
		private readonly bool   _isSqlServer;
		private readonly bool   _isYdb;

		public LegacySchemaProvider(DataConnection connection, SchemaOptions options, ILanguageProvider languageProvider)
		{
			_options           = options;
			_languageProvider  = languageProvider;
			var schemaProvider = connection.DataProvider.GetSchemaProvider();
			_providerName      = connection.DataProvider.Name;

			_isPostgreSql       = _providerName.Contains(ProviderName.PostgreSQL);
			_isMySqlOrMariaDB   = _providerName is ProviderName.MariaDB10MySqlConnector or ProviderName.MySql80MySqlConnector or ProviderName.MySql57MySqlConnector;
			_isSystemDataSqlite = _providerName == "SQLite.Classic";
			_isAccessOleDb      = _providerName is ProviderName.AccessJetOleDb or ProviderName.AccessAceOleDb;
			_isAccessOdbc       = _providerName is ProviderName.AccessJetOdbc or ProviderName.AccessAceOdbc;
			_isSqlServer        = _providerName.Contains(ProviderName.SqlServer);
			_isYdb              = _providerName.Contains(ProviderName.Ydb);

			// load schema from legacy API and convrt it into new model
			ParseSchema(
				schemaProvider.GetSchema(connection, CreateSchemaOptions(_options)),
				_options.LoadedObjects);
		}

		/// <summary>
		/// Convert legacy API schema into new schema model.
		/// </summary>
		/// <param name="schema">Legacy schema.</param>
		/// <param name="loadedObjects">Schema objects to load.</param>
		private void ParseSchema(DatabaseSchema schema, SchemaObjects loadedObjects)
		{
			// basic properties
			_databaseName  = string.IsNullOrWhiteSpace(schema.Database     ) ? null : schema.Database;
			_serverVersion = string.IsNullOrWhiteSpace(schema.ServerVersion) ? null : schema.ServerVersion;
			_dataSource    = string.IsNullOrWhiteSpace(schema.DataSource   ) ? null : schema.DataSource;

			// create table/view models
			if (loadedObjects.HasFlag(SchemaObjects.Table) || loadedObjects.HasFlag(SchemaObjects.View))
			{
				foreach (var table in schema.Tables)
					ParseTable(table, loadedObjects);
			}

			// TODO: Legacy API bug
			// pgsql schema provider returns table function column descriptors as output parameters
			var skipNonInputParameters = _isPostgreSql;

			// create procedures/functions models
			if (   loadedObjects.HasFlag(SchemaObjects.StoredProcedure)
				|| loadedObjects.HasFlag(SchemaObjects.TableFunction)
				|| loadedObjects.HasFlag(SchemaObjects.ScalarFunction)
				|| loadedObjects.HasFlag(SchemaObjects.AggregateFunction))
			{
				foreach (var proc in schema.Procedures)
					ParseCallable(proc, loadedObjects, skipNonInputParameters && proc.IsTableFunction);
			}
		}

		/// <summary>
		/// Create procedure/function model from legacy model.
		/// </summary>
		/// <param name="proc">Legacy procedure/function model.</param>
		/// <param name="loadedObjects">Schema objects to load.</param>
		/// <param name="onlyInputParameters">Ignore non-input parameters.</param>
		private void ParseCallable(ProcedureSchema proc, SchemaObjects loadedObjects, bool onlyInputParameters)
		{
			var load =    !proc.IsFunction          && loadedObjects.HasFlag(SchemaObjects.StoredProcedure)
						|| proc.IsTableFunction     && loadedObjects.HasFlag(SchemaObjects.TableFunction)
						|| proc.IsAggregateFunction && loadedObjects.HasFlag(SchemaObjects.AggregateFunction)
						|| proc.IsFunction          && !proc.IsAggregateFunction && !proc.IsTableFunction && loadedObjects.HasFlag(SchemaObjects.ScalarFunction);

			if (!load)
				return;

			var dbName = new SqlObjectName(
				proc.ProcedureName,
				Database: !_options.LoadDatabaseName || string.IsNullOrWhiteSpace(proc.CatalogName) ? null : proc.CatalogName,
				Schema  :                               string.IsNullOrWhiteSpace(proc.SchemaName ) ? null : proc.SchemaName,
				Package :                               string.IsNullOrWhiteSpace(proc.PackageName) ? null : proc.PackageName);

			var description = string.IsNullOrWhiteSpace(proc.Description) ? null : proc.Description;

			// API debug asserts for unexpected/unsupported inputs
			if (proc.IsResultDynamic)
				throw new InvalidOperationException($"IsResultDynamic set for function {dbName}");
			if (proc.IsLoaded && !(!proc.IsFunction || proc.IsTableFunction))
				throw new InvalidOperationException($"IsLoaded set for scalar/aggregate function {dbName}");
			if (proc.ResultTable != null && !(!proc.IsFunction || proc.IsTableFunction))
				throw new InvalidOperationException($"ResultTable set for scalar/aggregate function {dbName}");
			if (proc.ResultTable != null && !proc.IsLoaded)
				throw new InvalidOperationException($"ResultTable specified but IsLoaded not set for function {dbName}");

			// collect default schemas, reported by API
			if (proc.IsDefaultSchema && !string.IsNullOrWhiteSpace(proc.SchemaName))
				_defaultSchemas.Add(proc.SchemaName!);

			Exception? schemaError = null;
			if (proc.ResultException != null)
			{
				if (!proc.IsFunction || proc.IsTableFunction)
					schemaError = proc.ResultException;
				else
					throw new InvalidOperationException($"ResultException set for scalar/aggregate function {dbName}");
			}

			var resultSet            = ParseResultSet (dbName, proc);
			var (parameters, result) = ParseParameters(dbName, proc, onlyInputParameters);

			if (!proc.IsFunction)
			{
				if (_options.LoadStoredProcedure(new SqlObjectName(proc.ProcedureName, Schema: proc.SchemaName, Package: proc.PackageName)))
					_procedures.Add(new StoredProcedure(dbName, description, parameters, schemaError, resultSet != null ? new [] { resultSet } : null, result));
			}
			else if (proc.IsTableFunction)
			{
				if (_options.LoadTableFunction(new SqlObjectName(proc.ProcedureName, Schema: proc.SchemaName, Package: proc.PackageName)))
					_tableFunctions.Add(new TableFunction(dbName, description, parameters, schemaError, resultSet));
			}
			else if (proc.IsAggregateFunction)
			{
				if (result is not ScalarResult scalarResult)
					throw new InvalidOperationException($"Unsupported result type for aggregate function {dbName}");

				if (_options.LoadAggregateFunction(new SqlObjectName(proc.ProcedureName, Schema: proc.SchemaName, Package: proc.PackageName)))
					_aggregateFunctions.Add(new AggregateFunction(dbName, description, parameters, scalarResult));
			}
			else
			{
				if (_options.LoadScalarFunction(new SqlObjectName(proc.ProcedureName, Schema: proc.SchemaName, Package: proc.PackageName)))
					_scalarFunctions.Add(new ScalarFunction(dbName, description, parameters, result));
			}
		}

		/// <summary>
		/// Create procedure/function parameter models including return value model.
		/// </summary>
		/// <param name="functionName">Procedure/function database name.</param>
		/// <param name="proc">Procedure/function legacy model.</param>
		/// <param name="onlyInputs">Ignore non-input parameters (in/out parameters also ignored).</param>
		/// <returns>Returns procedure/function parameters and return value models.</returns>
		private (List<Parameter> parameters, Result result) ParseParameters(SqlObjectName functionName, ProcedureSchema proc, bool onlyInputs)
		{
			// scalar function can return tuple only for pgsql
			var detectTuples = proc.IsFunction && !proc.IsTableFunction && !proc.IsAggregateFunction && _isPostgreSql;
			List<ScalarResult>? tupleColumns = null;

			var parameters = new List<Parameter>();
			Result? result = null;

			foreach (var param in proc.Parameters)
			{
				if (onlyInputs && (!param.IsIn || param.IsOut || param.IsResult))
					continue;

				if (string.IsNullOrWhiteSpace(param.SchemaType))
					throw new InvalidOperationException($"Parameter {param.ParameterName} miss SchemaType value for function {functionName}");

				var name        = param.ParameterName;
				var description = string.IsNullOrWhiteSpace(param.Description) ? null : param.Description;

				// precison/scale not provided by API for parameters
				var type = new DatabaseType(param.SchemaType!, param.Size, null, null);
				RegisterType(type, param.DataType, param.SystemType, param.ProviderSpecificType);

				if (param.IsResult)
				{
					// debug asserts
					if (result != null)
						throw new InvalidOperationException($"Multiple result parameters found for function {functionName}");
					if (description != null)
						throw new InvalidOperationException($"Result parameter has Description for function {functionName}");

					result = new ScalarResult(name, type, param.IsNullable);
				}
				else
				{
					if (string.IsNullOrWhiteSpace(name))
						throw new InvalidOperationException($"Parameter name missing for function {functionName}");

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
						// tuple result nullability not provided by API, set to true
						result = new TupleResult(tupleColumns, true);
				}
				else
					result = new VoidResult();
			}

			// https://github.com/linq2db/linq2db/issues/1897
			if (_isSqlServer && !proc.IsFunction && _options.EnableSqlServerReturnValue)
			{
				// https://docs.microsoft.com/en-us/sql/t-sql/language-elements/return-transact-sql
				var type            = new DatabaseType("INT", 0, 0, 0);
				_typeMappings[type] = new TypeMapping(WellKnownTypes.System.Int32, DataType.Int32);
				parameters.Add(new Parameter("@return", null, type, false, ParameterDirection.Output));
			}

			return (parameters, result);
		}

		/// <summary>
		/// Create stored procedure or table function result set schema model.
		/// </summary>
		/// <param name="functionName">Procedure/function database name.</param>
		/// <param name="proc">Procedure/function legacy model.</param>
		/// <returns>Returns procedure/function result set schema model.</returns>
		private List<ResultColumn>? ParseResultSet(SqlObjectName functionName, ProcedureSchema proc)
		{
			List<ResultColumn>? resultSet = null;
			if (proc.ResultTable != null)
			{
				resultSet = new();

				// debug asserts
				if (proc.ResultTable.CatalogName != null)
					throw new InvalidOperationException($"Result set CatalogName set by schema for function {functionName}");
				if (proc.ResultTable.SchemaName != null)
					throw new InvalidOperationException($"Result set SchemaName set by schema for function {functionName}");
				if (proc.ResultTable.TableName != null)
					throw new InvalidOperationException($"Result set TableName set by schema for function {functionName}");
				if (proc.ResultTable.Description != null)
					throw new InvalidOperationException($"Result set Description set by schema for function {functionName}");
				if (proc.ResultTable.IsDefaultSchema)
					throw new InvalidOperationException($"Result set IsDefaultSchema set by schema for function {functionName}");
				if (proc.ResultTable.IsView)
					throw new InvalidOperationException($"Result set IsView set by schema for function {functionName}");
				if (proc.ResultTable.IsProviderSpecific)
					throw new InvalidOperationException($"Result set IsProviderSpecific set by schema for function {functionName}");
				if (!proc.ResultTable.IsProcedureResult)
					throw new InvalidOperationException($"Result set IsProcedureResult not set by schema for function {functionName}");
				if (proc.ResultTable.ForeignKeys.Count > 0)
					throw new InvalidOperationException($"Result set ForeignKeys set by schema for function {functionName}");

				foreach (var column in proc.ResultTable.Columns)
					resultSet.Add(ParseProcedureColumn(functionName, column));
			}

			return resultSet;
		}

		/// <summary>
		/// Creates stored procedure or table schema result set column model.
		/// </summary>
		/// <param name="functionName">Procedure/function database name.</param>
		/// <param name="column">Legacy column model.</param>
		/// <returns>Column model.</returns>
		private ResultColumn ParseProcedureColumn(SqlObjectName functionName, ColumnSchema column)
		{
			// debug asserts
			if (string.IsNullOrWhiteSpace(column.ColumnType))
				throw new InvalidOperationException($"ColumnType not provided by schema for column {column.ColumnName} in function {functionName}");
			if (!string.IsNullOrWhiteSpace(column.Description))
				throw new InvalidOperationException($"Result set column Description provided by schema for column {column.ColumnName} in function {functionName}");
			if (column.SkipOnInsert)
				throw new InvalidOperationException($"Result set column SkipOnInsert is set for column {column.ColumnName} in function {functionName}");
			if (column.SkipOnUpdate)
				throw new InvalidOperationException($"Result set column SkipOnInsert is set for column {column.ColumnName} in function {functionName}");
			if (column.IsPrimaryKey)
				throw new InvalidOperationException($"Result set column IsPrimaryKey is set for column {column.ColumnName} in function {functionName}");
			if (column.PrimaryKeyOrder != 0)
				throw new InvalidOperationException($"Result set column PrimaryKeyOrder is set for column {column.ColumnName} in function {functionName}");

			var type = new DatabaseType(column.ColumnType, column.Length, column.Precision, column.Scale);

			RegisterType(type, column.DataType, column.SystemType, column.ProviderSpecificType);

			return new ResultColumn(string.IsNullOrEmpty(column.ColumnName) ? null : column.ColumnName, type, column.IsNullable);
		}

		/// <summary>
		/// Creates table/view model from legacy model.
		/// </summary>
		/// <param name="table">Table or view legacy model.</param>
		/// <param name="loadedObjects">Database object load filter.</param>
		private void ParseTable(TableSchema table, SchemaObjects loadedObjects)
		{
			var load =  table.IsView && loadedObjects.HasFlag(SchemaObjects.View)
					|| !table.IsView && loadedObjects.HasFlag(SchemaObjects.Table);

			if (!load)
				return;

			var tableName = GetTableName(table);

			// debug asserts
			if (string.IsNullOrWhiteSpace(table.TableName))
				throw new InvalidOperationException($"TableName not provided by schema for table {tableName}");
			if (table.IsProcedureResult)
				throw new InvalidOperationException($"IsProcedureResult set by schema for table {tableName}");
			if (table.IsProviderSpecific)
			{
				// don't load system tables. Access schema provider returns them
				if (_isAccessOleDb || _isAccessOdbc)
					return;

				throw new InvalidOperationException($"IsProviderSpecific set by schema for table {tableName}");
			}

			if (table.IsDefaultSchema && !string.IsNullOrWhiteSpace(table.SchemaName))
				_defaultSchemas.Add(table.SchemaName!);

			var description = string.IsNullOrWhiteSpace(table.Description) ? null : table.Description;

			var columns            = new List<Column>();
			PrimaryKey? primaryKey = null;
			Identity? identity     = null;
			bool hasPrimaryKey     = false;

			foreach (var column in table.Columns)
			{
				var columnSchema = ParseColumn(tableName, column);
				columns.Add(columnSchema);

				// Access OleDb provider reports incorrect identity information
				// by marking all (?) non-nullable INT columns and identities
				if (column.IsIdentity && !_isAccessOleDb)
				{
					if (identity != null)
						throw new InvalidOperationException($"Duplicate identity found on table {tableName}");

					identity = new Identity(column.ColumnName, null);
				}

				if (column.IsPrimaryKey)
					hasPrimaryKey = true;
			}

			if (hasPrimaryKey)
			{
				var pkColumns = table.Columns.Where(c => c.IsPrimaryKey).ToList();

				if (pkColumns.Count != pkColumns.Select(c => c.PrimaryKeyOrder).Distinct().Count())
					throw new InvalidOperationException($"Primary key columns have duplicate ordinals on table {tableName}");

				primaryKey = new PrimaryKey(null, table.Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.PrimaryKeyOrder).Select(c => c.ColumnName).ToList());
			}

			// load table foreign keys
			if (loadedObjects.HasFlag(SchemaObjects.ForeignKey))
				foreach (var fk in table.ForeignKeys)
					ParseForeignKey(fk);

			if (table.IsView)
				_views.Add(new View(tableName, description, columns, identity, primaryKey));
			else
				_tables.Add(new Table(tableName, description, columns, identity, primaryKey));
		}

		/// <summary>
		/// Creates database object name from legacy table model.
		/// </summary>
		/// <param name="table">Table model.</param>
		/// <returns>Table name.</returns>
		private SqlObjectName GetTableName(TableSchema table)
		{
			return new SqlObjectName(
				table.TableName!,
				Database: !_options.LoadDatabaseName || string.IsNullOrWhiteSpace(table.CatalogName) ? null : table.CatalogName,
				Schema  : string.IsNullOrWhiteSpace(table.SchemaName ) ? null : table.SchemaName);
		}

		/// <summary>
		/// Creates foreign key model from legacy model.
		/// </summary>
		/// <param name="fk">Legacy foreign key model.</param>
		private void ParseForeignKey(ForeignKeySchema fk)
		{
			// legacy API creates fake back-reference foreign key, which we must ignore
			// this is detected by not set ThisTable property (implementation-specific hack)
			if (fk.ThisTable == null)
				return;

			// debug asserts
			if (fk.ThisColumns.Count != fk.OtherColumns.Count)
				throw new InvalidOperationException($"Foreign key {fk.KeyName} has different number of columns on both sides");
			if (fk.ThisColumns.Count == 0)
				throw new InvalidOperationException($"Foreign key {fk.KeyName} has no columns");

			var relation = new ForeignKeyColumnMapping[fk.ThisColumns.Count];
			for (var i = 0; i < fk.ThisColumns.Count; i++)
				relation[i] = new(fk.ThisColumns[i].ColumnName, fk.OtherColumns[i].ColumnName);

			_foreignKeys.Add(new ForeignKey(fk.KeyName, GetTableName(fk.ThisTable), GetTableName(fk.OtherTable), relation));
		}

		/// <summary>
		/// Creates column model for table or view from legacy model.
		/// </summary>
		/// <param name="tableName">Table or view name.</param>
		/// <param name="column">Legacy column model.</param>
		/// <returns></returns>
		private Column ParseColumn(SqlObjectName tableName, ColumnSchema column)
		{
			// debug asserts
			if (string.IsNullOrWhiteSpace(column.ColumnName))
				throw new InvalidOperationException($"ColumnName not provided by schema for column in table {tableName}");
			if (string.IsNullOrWhiteSpace(column.ColumnType))
			{
				// sqlite schema provider could return column without type
				if (_isSystemDataSqlite)
				{
					// TODO: generate exception/log message?
					// numeric is default column type affinity for sqlite 3 (still, it is wrong for specific case, that triggered this workaround)
					column.ColumnType = "NUMERIC";
				}
				else
					// TODO: use logger
					Console.Error.WriteLine($"ColumnType not provided by schema for column {tableName}.{column.ColumnName}");
			}

			var type = new DatabaseType(column.ColumnType, column.Length, column.Precision, column.Scale);

			RegisterType(type, column.DataType, column.SystemType, column.ProviderSpecificType);

			return new Column(
				column.ColumnName,
				string.IsNullOrWhiteSpace(column.Description) ? null : column.Description,
				type,
				column.IsNullable,
				!column.SkipOnInsert,
				!column.SkipOnUpdate,
				column.Ordinal);
		}

		/// <summary>
		/// Register provider-specific types mappings for known types, returned by legacy API.
		/// </summary>
		/// <param name="dbType">Database type.</param>
		/// <param name="dataType"><see cref="DataType"/> hint enum.</param>
		/// <param name="systemType">CLR type.</param>
		/// <param name="providerSpecificType">Provider-specific type name.</param>
		private void RegisterType(DatabaseType dbType, DataType dataType, Type? systemType, string? providerSpecificType)
		{
			IType type;
			if ((_options.PreferProviderSpecificTypes || systemType == null) && !string.IsNullOrWhiteSpace(providerSpecificType))
			{
				switch (providerSpecificType)
				{
					// MySql.Data
					case "MySqlDecimal" : type = _languageProvider.TypeParser.Parse("MySql.Data.Types.MySqlDecimal" , true); break;
					case "MySqlDateTime": type = _languageProvider.TypeParser.Parse("MySql.Data.Types.MySqlDateTime", true); break;
					case "MySqlGeometry": type = _languageProvider.TypeParser.Parse("MySql.Data.Types.MySqlGeometry", true); break;

					// FirebirdClient
					case "FbZonedDateTime": type = _languageProvider.TypeParser.Parse("FirebirdSql.Data.Types.FbZonedDateTime", true); break;
					case "FbZonedTime"    : type = _languageProvider.TypeParser.Parse("FirebirdSql.Data.Types.FbZonedTime"    , true); break;
					case "FbDecFloat"     : type = _languageProvider.TypeParser.Parse("FirebirdSql.Data.Types.FbDecFloat"     , true); break;

					// Npgsql
					case "NpgsqlDateTime": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlDateTime", true); break;
					case "NpgsqlDate"    : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlDate"    , true); break;
					case "NpgsqlPoint"   : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlPoint"   , true); break;
					case "NpgsqlLSeg"    : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlLSeg"    , true); break;
					case "NpgsqlBox"     : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlBox"     , true); break;
					case "NpgsqlPath"    : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlPath"    , true); break;
					case "NpgsqlPolygon" : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlPolygon" , true); break;
					case "NpgsqlCircle"  : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlCircle"  , true); break;
					case "NpgsqlLine"    : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlLine"    , true); break;
					case "NpgsqlInet"    : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlInet"    , true); break;
					case "NpgsqlCidr"    : type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlCidr"    , true); break;
					case "NpgsqlInterval": type = _languageProvider.TypeParser.Parse("NpgsqlTypes.NpgsqlInterval", true); break;

					// SQL Server spatial types
					case "Microsoft.SqlServer.Types.SqlHierarchyId": type = WellKnownTypes.Microsoft.SqlServer.Types.SqlHierarchyId; break;
					case "Microsoft.SqlServer.Types.SqlGeography"  : type = _languageProvider.TypeParser.Parse("Microsoft.SqlServer.Types.SqlGeography", false); break;
					case "Microsoft.SqlServer.Types.SqlGeometry"   : type = _languageProvider.TypeParser.Parse("Microsoft.SqlServer.Types.SqlGeometry" , false); break;

					// SQL Server/SQL CE Sql* types
					case "SqlString"  : type = WellKnownTypes.System.Data.SqlTypes.SqlString  ; break;
					case "SqlByte"    : type = WellKnownTypes.System.Data.SqlTypes.SqlByte    ; break;
					case "SqlInt16"   : type = WellKnownTypes.System.Data.SqlTypes.SqlInt16   ; break;
					case "SqlInt32"   : type = WellKnownTypes.System.Data.SqlTypes.SqlInt32   ; break;
					case "SqlInt64"   : type = WellKnownTypes.System.Data.SqlTypes.SqlInt64   ; break;
					case "SqlDecimal" : type = WellKnownTypes.System.Data.SqlTypes.SqlDecimal ; break;
					case "SqlMoney"   : type = WellKnownTypes.System.Data.SqlTypes.SqlMoney   ; break;
					case "SqlSingle"  : type = WellKnownTypes.System.Data.SqlTypes.SqlSingle  ; break;
					case "SqlDouble"  : type = WellKnownTypes.System.Data.SqlTypes.SqlDouble  ; break;
					case "SqlBoolean" : type = WellKnownTypes.System.Data.SqlTypes.SqlBoolean ; break;
					case "SqlDateTime": type = WellKnownTypes.System.Data.SqlTypes.SqlDateTime; break;
					case "SqlBinary"  : type = WellKnownTypes.System.Data.SqlTypes.SqlBinary  ; break;
					case "SqlGuid"    : type = WellKnownTypes.System.Data.SqlTypes.SqlGuid    ; break;
					case "SqlXml"     : type = WellKnownTypes.System.Data.SqlTypes.SqlXml     ; break;

					// DB2 provider types
					case "DB2Binary"      : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Binary"      , true ); break;
					case "DB2Blob"        : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Blob"        , false); break;
					case "DB2Clob"        : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Clob"        , false); break;
					case "DB2Date"        : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Date"        , true ); break;
					case "DB2DateTime"    : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2DateTime"    , true ); break;
					case "DB2Decimal"     : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Decimal"     , true ); break;
					case "DB2DecimalFloat": type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2DecimalFloat", true ); break;
					case "DB2Double"      : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Double"      , true ); break;
					case "DB2Int16"       : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int16"       , true ); break;
					case "DB2Int32"       : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int32"       , true ); break;
					case "DB2Int64"       : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Int64"       , true ); break;
					case "DB2Real"        : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Real"        , true ); break;
					case "DB2Real370"     : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Real370"     , true ); break;
					case "DB2RowId"       : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2RowId"       , true ); break;
					case "DB2String"      : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2String"      , true ); break;
					case "DB2Time"        : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Time"        , true ); break;
					case "DB2TimeStamp"   : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2TimeStamp"   , true ); break;
					case "DB2Xml"         : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2Xml"         , false); break;
					case "DB2TimeSpan"    : type = _languageProvider.TypeParser.Parse("IBM.Data.DB2Types.DB2TimeSpan"    , true ); break;

					// TODO                  : ODP.NET provider use other namespace
					// Oracle mamanged client types
					case "OracleBFile"       : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleBFile"       , false); break;
					case "OracleBinary"      : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleBinary"      , true ); break;
					case "OracleBlob"        : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleBlob"        , false); break;
					case "OracleClob"        : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleClob"        , false); break;
					case "OracleDate"        : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleDate"        , true ); break;
					case "OracleDecimal"     : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleDecimal"     , true ); break;
					case "OracleIntervalDS"  : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleIntervalDS"  , true ); break;
					case "OracleIntervalYM"  : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleIntervalYM"  , true ); break;
					case "OracleString"      : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleString"      , true ); break;
					case "OracleTimeStamp"   : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleTimeStamp"   , true ); break;
					case "OracleTimeStampLTZ": type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleTimeStampLTZ", true ); break;
					case "OracleTimeStampTZ" : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleTimeStampTZ" , true ); break;
					case "OracleXmlType"     : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleXmlType"     , false); break;
					case "OracleXmlStream"   : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleXmlStream"   , false); break;
					case "OracleRefCursor"   : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleRefCursor"   , false); break;
					case "OracleRef"         : type = _languageProvider.TypeParser.Parse("Oracle.ManagedDataAccess.Types.OracleRef"         , true ); break;

					default:
						throw new InvalidOperationException($"Unknown provider-specific type {providerSpecificType}");
				}
			}
			else
			{
				if (systemType == null)
				{
					if (!_isYdb)
					{
						return;
					}

					var name = (dbType.Name ?? string.Empty).Trim('"').ToLowerInvariant();
					switch (name)
					{
						case "text":
						case "utf8":
							_typeMappings[dbType] = new TypeMapping(WellKnownTypes.System.String, DataType.Text);
							return;

						case "string":         // YDB "String" = byte array
							_typeMappings[dbType] = new TypeMapping(
								_languageProvider.TypeParser.Parse("System.Byte[]", true),
								DataType.VarBinary);

							return;

						case "json":
						    _typeMappings[dbType] = new TypeMapping(WellKnownTypes.System.String, DataType.Json);
						    return;
						case "jsondocument":
						    _typeMappings[dbType] = new TypeMapping(
							    _languageProvider.TypeParser.Parse("System.Byte[]", true),
							    DataType.BinaryJson);
						    return;
					}

					return;
				}

				type = _languageProvider.TypeParser.Parse(systemType);
			}

			if (type.IsNullable)
				throw new InvalidOperationException($"Nullability specified on type {type}");

			var dt = dataType == DataType.Undefined ? (DataType?)null : dataType;

			if (_typeMappings.TryGetValue(dbType, out var registeredType))
			{
				// validate that there is no conflicting mappings when same database type
				// mapped to different CLR types.
				// Usually means provider has different type parsing logic for different database objects,
				// e.g. column type parser and procedure parameter type parser return different types
				if (!_languageProvider.TypeEqualityComparerWithNRT.Equals(registeredType.CLRType, type))
				{
					// known exception (workaround)
					if (_isMySqlOrMariaDB)
					{
						// TODO: fix Mysql schema provider to resolve types properly
						// now it can return different types for column vs parameter, e.g. loose UNSIGNED modifier
					}
					else
						throw new InvalidOperationException($"Type {dbType} mapped to multiple types: ({registeredType.CLRType}, {registeredType.DataType}) vs ({type}, {dt})");
				}

				// same check for DataType enum
				if (registeredType.DataType != dt)
				{
					// known exceptions (workarounds)
					if (_isSystemDataSqlite)
					{
						if (registeredType.DataType == DataType.Char && dt == DataType.NChar)
							_typeMappings[dbType] = new(type, dt);
						else if (registeredType.DataType == DataType.NChar && dt == DataType.Char)
						{
						}
						else
							throw new InvalidOperationException($"Type {dbType} mapped to multiple types: ({registeredType.CLRType}, {registeredType.DataType}) vs ({type}, {dt})");
					}
					else if (_isMySqlOrMariaDB)
					{
						// TODO: fix Mysql schema provider to resolve types properly
						// now it can return different types for column vs parameter, e.g. loose UNSIGNED modifier
					}
					else
						throw new InvalidOperationException($"Type {dbType} mapped to multiple types: ({registeredType.CLRType}, {registeredType.DataType}) vs ({type}, {dt})");
				}
			}
			else
				_typeMappings.Add(dbType, new(type, dt));
		}

		/// <summary>
		/// Creates legacy API schema options object for API call.
		/// </summary>
		/// <param name="options">Schema load options.</param>
		/// <returns>Legacy schema request settings.</returns>
		private static GetSchemaOptions CreateSchemaOptions(SchemaOptions options)
		{
			var legacyOptions = new GetSchemaOptions();

			legacyOptions.PreferProviderSpecificTypes = options.PreferProviderSpecificTypes;

			// requires post-load filtering
			legacyOptions.GetTables      = options.LoadedObjects.HasFlag(SchemaObjects.Table) || options.LoadedObjects.HasFlag(SchemaObjects.View);
			legacyOptions.GetForeignKeys = options.LoadedObjects.HasFlag(SchemaObjects.ForeignKey) && options.LoadedObjects.HasFlag(SchemaObjects.Table);

			// requires post-load filtering
			legacyOptions.GetProcedures = options.LoadedObjects.HasFlag(SchemaObjects.StoredProcedure)
				|| options.LoadedObjects.HasFlag(SchemaObjects.ScalarFunction)
				|| options.LoadedObjects.HasFlag(SchemaObjects.TableFunction)
				|| options.LoadedObjects.HasFlag(SchemaObjects.AggregateFunction);

			if (options.Schemas.Count > 0)
			{
				if (options.IncludeSchemas)
					legacyOptions.IncludedSchemas = options.Schemas.ToArray();
				else
					legacyOptions.ExcludedSchemas = options.Schemas.ToArray();
			}

			if (options.Catalogs.Count > 0)
			{
				if (options.IncludeCatalogs)
					legacyOptions.IncludedCatalogs = options.Catalogs.ToArray();
				else
					legacyOptions.ExcludedCatalogs = options.Catalogs.ToArray();
			}

			legacyOptions.LoadProcedure = p =>
			{
				var name = new SqlObjectName(p.ProcedureName, Schema: p.SchemaName, Package: p.PackageName);
				if (!p.IsFunction)
					return options.LoadStoredProcedure(name) && options.LoadProceduresSchema && options.LoadProcedureSchema(name);
				else if (p.IsTableFunction)
					return options.LoadTableFunction(name);
				else if (p.IsAggregateFunction)
					return options.LoadAggregateFunction(name);
				else
					return options.LoadScalarFunction(name);

				throw new InvalidOperationException($"{nameof(GetSchemaOptions)}.{nameof(GetSchemaOptions.LoadProcedure)} called for non-table returning object {p.ProcedureName}");
			};

			legacyOptions.LoadTable                 = t => options.LoadTableOrView(new SqlObjectName(t.Name, Schema: t.Schema), t.IsView);
			legacyOptions.UseSchemaOnly             = options.UseSafeSchemaLoad;
			legacyOptions.IgnoreSystemHistoryTables = options.IgnoreSystemHistoryTables;

			return legacyOptions;
		}

		#region ISchemaProvider
		IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions(                                    ) => _aggregateFunctions;
		IEnumerable<StoredProcedure>   ISchemaProvider.GetProcedures        (bool withSchema, bool safeSchemaOnly) => _procedures;
		IEnumerable<ScalarFunction>    ISchemaProvider.GetScalarFunctions   (                                    ) => _scalarFunctions;
		IEnumerable<TableFunction>     ISchemaProvider.GetTableFunctions    (                                    ) => _tableFunctions;
		IEnumerable<Table>             ISchemaProvider.GetTables            (                                    ) => _tables;
		IEnumerable<View>              ISchemaProvider.GetViews             (                                    ) => _views;
		IEnumerable<ForeignKey>        ISchemaProvider.GetForeignKeys       (                                    ) => _foreignKeys;

		ISet<string> ISchemaProvider.GetDefaultSchemas() => _options.DefaultSchemas ??  _defaultSchemas;

		string?         ISchemaProvider.DatabaseName    => _databaseName;
		string?         ISchemaProvider.ServerVersion   => _serverVersion;
		string?         ISchemaProvider.DataSource      => _dataSource;
		DatabaseOptions ISchemaProvider.DatabaseOptions => _isSqlServer ? SqlServerDatabaseOptions.Instance : DatabaseOptions.Default;
		#endregion

		#region ITypeMappingProvider
		TypeMapping? ITypeMappingProvider.GetTypeMapping(DatabaseType databaseType)
		{
			if (_typeMappings.TryGetValue(databaseType, out var value))
				return value;

			return null;
		}
		#endregion
	}
}
