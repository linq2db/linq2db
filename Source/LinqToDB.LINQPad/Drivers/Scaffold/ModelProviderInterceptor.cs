#pragma warning disable CA1002 // Do not expose generic lists
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LINQPad.Extensibility.DataContext;

using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Scaffold;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

namespace LinqToDB.LINQPad;

/// <summary>
/// Scaffold interceptor used to populate generated data model for dynamic context (with proper type/member identifiers).
/// </summary>
internal sealed class ModelProviderInterceptor(ConnectionSettings settings, ISqlBuilder sqlBuilder) : ScaffoldInterceptors
{
	private readonly bool        _replaceClickHouseFixedString = string.Equals(settings.Connection.Database, ProviderName.ClickHouse, StringComparison.Ordinal) && settings.Scaffold.ClickHouseFixedStringAsString;

	// stores populated model information:
	// - FK associations
	// - schema-scoped objects (views, tables, routines)
	private readonly List<AssociationData>          _associations = new ();
	private readonly Dictionary<string, SchemaData> _schemaItems  = new (StringComparer.Ordinal);

	#region model DTOs

	private          record PackageData                  (List<TableData> Tables, List<TableData> Views, List<ProcedureData> Procedures, List<TableFunctionData> TableFunctions, List<ScalarOrAggregateFunctionData> ScalarFunctions, List<ScalarOrAggregateFunctionData> AggregateFunctions);
	private sealed   record SchemaData                   (List<TableData> Tables, List<TableData> Views, List<ProcedureData> Procedures, List<TableFunctionData> TableFunctions, List<ScalarOrAggregateFunctionData> ScalarFunctions, List<ScalarOrAggregateFunctionData> AggregateFunctions, Dictionary<string, PackageData> Packages) : PackageData(Tables, Views, Procedures, TableFunctions, ScalarFunctions, AggregateFunctions);
	private sealed   record TableData                    (string ContextName, IType ContextType, string DbName, List<ColumnData> Columns);
	private sealed   record ColumnData                   (string MemberName, IType Type, string DbName, bool IsPrimaryKey, bool IsIdentity, DataType? DataType, DatabaseType DbType);
	private sealed   record AssociationData              (string MemberName, IType Type, bool FromSide, bool OneToMany, string KeyName, TableData Source, TableData Target);
	private sealed   record ResultColumnData             (string MemberName, IType Type, string DbName, DataType? DataType, DatabaseType DbType);
	private sealed   record ParameterData                (string Name, IType Type, ParameterDirection Direction);
	private abstract record FunctionBaseData             (string MethodName, string DbName, IReadOnlyList<ParameterData> Parameters);
	private sealed   record ProcedureData                (string MethodName, string DbName, IReadOnlyList<ParameterData> Parameters, IReadOnlyList<ResultColumnData>? Result) : FunctionBaseData(MethodName, DbName, Parameters);
	private sealed   record TableFunctionData            (string MethodName, string DbName, IReadOnlyList<ParameterData> Parameters, IReadOnlyList<ResultColumnData>? Result) : FunctionBaseData(MethodName, DbName, Parameters);
	private sealed   record ScalarOrAggregateFunctionData(string MethodName, string DbName, IReadOnlyList<ParameterData> Parameters, IType ResultType) : FunctionBaseData(MethodName, DbName, Parameters);

	public enum ParameterDirection
	{
		None,
		Ref,
		Out,
	}

	#endregion

	#region Type Mapping
	public override TypeMapping? GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping? defaultMapping)
	{
		if (_replaceClickHouseFixedString && databaseType.Name?.StartsWith("FixedString(", StringComparison.Ordinal) == true)
			return new TypeMapping(WellKnownTypes.System.String, DataType.NChar);

		return base.GetTypeMapping(databaseType, typeParser, defaultMapping);
	}
	#endregion

	#region Model Population

	public override void AfterSourceCodeGenerated(FinalDataModel model)
	{
		// tables lookup for association model population
		var tablesLookup = new Dictionary<EntityModel, TableData>();

		foreach (var entity      in model.Entities          ) ProcessEntity           (entity, tablesLookup);
		foreach (var association in model.Associations      ) ProcessAssociation      (association, tablesLookup);
		foreach (var proc        in model.StoredProcedures  ) ProcessStoredProcedure  (proc);
		foreach (var func        in model.TableFunctions    ) ProcessTableFunction    (func);
		foreach (var func        in model.ScalarFunctions   ) ProcessScalarFunction   (func);
		foreach (var func        in model.AggregateFunctions) ProcessAggregateFunction(func);
	}

	private PackageData GetSchemaOrPackage(string? schemaName, string? packageName)
	{
		if (!_schemaItems.TryGetValue(schemaName ?? string.Empty, out var schema))
			_schemaItems.Add(schemaName ?? string.Empty, schema = new SchemaData(new(), new(), new(), new(), new(), new(), new(StringComparer.Ordinal)));

		if (packageName != null)
		{
			if (!schema.Packages.TryGetValue(packageName, out var package))
				schema.Packages.Add(packageName, package = new(new(), new(), new(), new(), new(), new()));
			return package;
		}

		return schema;
	}

	private void ProcessEntity(EntityModel entityModel, Dictionary<EntityModel, TableData> tablesLookup)
	{
		var schemaName = entityModel.Metadata.Name!.Value.Schema;
		var schema     = GetSchemaOrPackage(schemaName, entityModel.Metadata.Name!.Value.Package);
		var columns    = new List<ColumnData>();

		foreach (var column in entityModel.Columns)
		{
			columns.Add(new ColumnData(
				column.Property.Name,
				column.Property.Type!,
				column.Metadata.Name!,
				column.Metadata.IsPrimaryKey,
				column.Metadata.IsIdentity,
				column.Metadata.DataType,
				column.Metadata.DbType!));
		}

		var table = new TableData(
				entityModel.ContextProperty!.Name,
				entityModel.ContextProperty.Type!,
				GetDbName(entityModel.Metadata.Name!.Value.Name, schemaName),
				columns);

		tablesLookup.Add(entityModel, table);

		if (entityModel.Metadata.IsView)
			schema.Views.Add(table);
		else
			schema.Tables.Add(table);
	}

	private void ProcessAssociation(AssociationModel associationModel, Dictionary<EntityModel, TableData> tablesLookup)
	{
		if (   tablesLookup.TryGetValue(associationModel.Source, out var fromTable)
			&& tablesLookup.TryGetValue(associationModel.Target, out var toTable))
		{
			_associations.Add(new AssociationData(
				associationModel.Property!.Name,
				associationModel.Property.Type!,
				true,
				associationModel.ManyToOne,
				associationModel.ForeignKeyName!,
				fromTable,
				toTable));

			_associations.Add(new AssociationData(
				associationModel.BackreferenceProperty!.Name,
				associationModel.BackreferenceProperty.Type!,
				false,
				associationModel.ManyToOne,
				associationModel.ForeignKeyName!,
				toTable,
				fromTable));
		}
	}

	private void ProcessStoredProcedure(StoredProcedureModel procedureModel)
	{
		var schema     = GetSchemaOrPackage(procedureModel.Name.Schema, procedureModel.Name.Package);
		var parameters = CollectParameters(procedureModel.Parameters);

		if (procedureModel.Return != null)
			parameters.Add(new ParameterData(procedureModel.Return.Parameter.Name, procedureModel.Return.Parameter.Type, ParameterDirection.Out));

		List<ResultColumnData>? result = null;

		if (procedureModel.Results.Count > 0)
			result = CollectResultData(procedureModel.Results[0]);

		schema.Procedures.Add(new ProcedureData(
			procedureModel.Method.Name,
			GetDbName(procedureModel.Name.Name, procedureModel.Name.Schema),
			parameters,
			result));
	}

	private void ProcessTableFunction(TableFunctionModel functionModel)
	{
		var schema     = GetSchemaOrPackage(functionModel.Name.Schema, functionModel.Name.Package);
		var parameters = CollectParameters(functionModel.Parameters);
		var result     = CollectResultData(functionModel.Result!);

		schema.TableFunctions.Add(new TableFunctionData(
			functionModel.Method.Name,
			GetDbName(functionModel.Name.Name, functionModel.Name.Schema),
			parameters,
			result));
	}

	private void ProcessAggregateFunction(AggregateFunctionModel functionModel)
	{
		var schema     = GetSchemaOrPackage(functionModel.Name.Schema, functionModel.Name.Package);
		var parameters = CollectParameters(functionModel.Parameters);

		schema.AggregateFunctions.Add(new ScalarOrAggregateFunctionData(
			functionModel.Method.Name,
			GetDbName(functionModel.Name.Name, functionModel.Name.Schema),
			parameters,
			functionModel.ReturnType));
	}

	private void ProcessScalarFunction(ScalarFunctionModel functionModel)
	{
		var schema     = GetSchemaOrPackage(functionModel.Name.Schema, functionModel.Name.Package);
		var parameters = CollectParameters(functionModel.Parameters);

		schema.ScalarFunctions.Add(new ScalarOrAggregateFunctionData(
			functionModel.Method.Name,
			GetDbName(functionModel.Name.Name, functionModel.Name.Schema),
			parameters,
			functionModel.Return!));
	}

	private static List<ResultColumnData>? CollectResultData(FunctionResult procedureModel)
	{
		var table  = procedureModel.CustomTable;
		if (table == null)
			return null;

		var result = new List<ResultColumnData>(table.Columns.Count);

		foreach (var column in table.Columns)
		{
			result.Add(new ResultColumnData(
				column.Property.Name,
				column.Property.Type!,
				column.Metadata.Name!,
				column.Metadata.DataType,
				column.Metadata.DbType!));
		}

		return result;
	}

	private static List<ParameterData> CollectParameters(List<FunctionParameterModel> parameters)
	{
		var parametersData = new List<ParameterData>(parameters.Count);

		foreach (var param in parameters)
		{
			var direction = param.Direction switch
			{
				System.Data.ParameterDirection.InputOutput        => ParameterDirection.Ref,
				System.Data.ParameterDirection.Output
					or System.Data.ParameterDirection.ReturnValue => ParameterDirection.Out,
				_                                                 => ParameterDirection.None,
			};

			parametersData.Add(new ParameterData(
				param.Parameter.Name,
				param.Parameter.Type,
				direction));
		}

		return parametersData;
	}

	#endregion

	#region Convert data model to LINQPad tree model

	public List<ExplorerItem> GetTree()
	{
		var tablesLookup = new Dictionary<TableData, ExplorerItem>();

		// don't create schema node for single schema without name (default schema)
		if (_schemaItems.Count == 1 && _schemaItems.ContainsKey(string.Empty))
		{
			var result = PopulateSchemaMembers(string.Empty, tablesLookup);
			PopulateAssociations(_associations, tablesLookup);
			return result;
		}

		var model = new List<ExplorerItem>();

		foreach (var schema in _schemaItems.Keys.OrderBy(static _ => _, StringComparer.Ordinal))
		{
			// for cases when default (empty) schema exists in model with named schemas
			if (schema.Length == 0)
				model.Add(new ExplorerItem("<default>", ExplorerItemKind.Schema, ExplorerIcon.Schema)
				{
					ToolTipText = "default schema",
					SqlName     = string.Empty,
					Children    = PopulateSchemaMembers(schema, tablesLookup),
				});
			else
				model.Add(new ExplorerItem(schema, ExplorerItemKind.Schema, ExplorerIcon.Schema)
				{
					ToolTipText = $"schema: {schema}",
					SqlName     = GetDbName(schema),
					Children    = PopulateSchemaMembers(schema, tablesLookup),
				});
		}

		// associations need references to table nodes and could define cross-schema references, so we must create them after all table nodes created
		// for all schemas
		PopulateAssociations(_associations, tablesLookup);

		return model;
	}

	private List<ExplorerItem> PopulateSchemaMembers(string schemaName, Dictionary<TableData, ExplorerItem> tablesLookup)
	{
		var items = new List<ExplorerItem>();
		var data  = _schemaItems[schemaName];

		if (data.Packages.Count > 0)
		{
			foreach (var package in data.Packages.Keys.OrderBy(_ => _, StringComparer.Ordinal))
			{
				var children = new List<ExplorerItem>();
				PopulateSchemaOrPackageMembers(tablesLookup, children, data.Packages[package]);
				items.Add(new ExplorerItem(package, ExplorerItemKind.Schema, ExplorerIcon.Schema)
				{
					ToolTipText = $"package: {package}",
					SqlName     = GetDbName(package),
					Children    = children,
				});
			}
		}

		PopulateSchemaOrPackageMembers(tablesLookup, items, data);

		return items;
	}

	private void PopulateSchemaOrPackageMembers(Dictionary<TableData, ExplorerItem> tablesLookup, List<ExplorerItem> items, PackageData data)
	{
		if (data.Tables.Count > 0)
			items.Add(PopulateTables(data.Tables, "Tables", ExplorerIcon.Table, tablesLookup));

		if (data.Views.Count > 0)
			items.Add(PopulateTables(data.Views, "Views", ExplorerIcon.View, tablesLookup));

		if (data.Procedures.Count > 0)
			items.Add(PopulateStoredProcedures(data.Procedures));

		if (data.TableFunctions.Count > 0)
			items.Add(PopulateTableFunctions(data.TableFunctions));

		if (data.ScalarFunctions.Count > 0)
			items.Add(PopulateScalarFunctions(data.ScalarFunctions, "Scalar Functions"));

		if (data.AggregateFunctions.Count > 0)
			items.Add(PopulateScalarFunctions(data.AggregateFunctions, "Aggregate Functions"));
	}

	private ExplorerItem PopulateStoredProcedures(List<ProcedureData> procedures)
	{
		var items = new List<ExplorerItem>(procedures.Count);

		foreach (var func in procedures.OrderBy(static f => f.MethodName, StringComparer.Ordinal))
		{
			List<ExplorerItem>? children = null;
			var size = func.Parameters.Count + (func.Result != null ? 1 : 0);

			if (size > 0)
			{
				children = new List<ExplorerItem>(size);
				AddParameters(func.Parameters, children);

				if (func.Result != null)
					AddResultTable(func.Result, children);
			}

			items.Add(new ExplorerItem(func.MethodName, ExplorerItemKind.QueryableObject, ExplorerIcon.StoredProc)
			{
				DragText     = $"this.{CSharpUtils.EscapeIdentifier(func.MethodName)}({string.Join(", ", func.Parameters.Select(GetParameterNameEscaped))})",
				Children     = children,
				IsEnumerable = func.Result != null,
				SqlName      = func.DbName,
			});
		}

		return new ExplorerItem("Stored Procedures", ExplorerItemKind.Category, ExplorerIcon.StoredProc)
		{
			Children = items,
		};
	}

	private void AddResultTable(IReadOnlyList<ResultColumnData> resultColumns, List<ExplorerItem> children)
	{
		var columns = new List<ExplorerItem>(resultColumns.Count);

		foreach (var column in resultColumns)
		{
			var dbName = GetDbName(column.DbName);
			var dbType = $"{GetTypeName(column.DataType, column.DbType)} {(column.Type.IsNullable ? "NULL" : "NOT NULL")}";

			columns.Add(new ExplorerItem($"{column.MemberName} : {SimpleBuildTypeName(column.Type)}", ExplorerItemKind.Property, ExplorerIcon.Column)
			{
				ToolTipText        = $"{dbName} {dbType}",
				DragText           = CSharpUtils.EscapeIdentifier(column.MemberName),
				SqlName            = dbName,
				SqlTypeDeclaration = dbType,
			});
		}

		children.Add(new ExplorerItem("Result Table", ExplorerItemKind.Category, ExplorerIcon.Table)
		{
			Children = columns,
		});
	}

	private void AddParameters(IReadOnlyList<ParameterData> parameters, List<ExplorerItem> children)
	{
		foreach (var param in parameters)
			children.Add(new ExplorerItem($"{GetParameterName(param)} : {SimpleBuildTypeName(param.Type)}", ExplorerItemKind.Parameter, ExplorerIcon.Parameter));
	}

	private static string GetParameterName(ParameterData param)
	{
		return $"{(param.Direction == ParameterDirection.Out ? "out " : param.Direction == ParameterDirection.Ref ? "ref " : null)}{param.Name}";
	}

	private static string GetParameterNameEscaped(ParameterData param)
	{
		return $"{(param.Direction == ParameterDirection.Out ? "out " : param.Direction == ParameterDirection.Ref ? "ref " : null)}{CSharpUtils.EscapeIdentifier(param.Name)}";
	}

	private ExplorerItem PopulateTableFunctions(List<TableFunctionData> functions)
	{
		var items = new List<ExplorerItem>(functions.Count);

		foreach (var func in functions.OrderBy(static f => f.MethodName, StringComparer.Ordinal))
		{
			var children = new List<ExplorerItem>(func.Parameters.Count + 1);

			AddParameters(func.Parameters, children);
			if (func.Result != null)
				AddResultTable(func.Result, children);

			items.Add(new ExplorerItem(func.MethodName, ExplorerItemKind.QueryableObject, ExplorerIcon.TableFunction)
			{
				DragText     = $"{CSharpUtils.EscapeIdentifier(func.MethodName)}({string.Join(", ", func.Parameters.Select(GetParameterNameEscaped))})",
				Children     = children,
				IsEnumerable = true,
				SqlName      = func.DbName,
			});
		}

		return new ExplorerItem("Table Functions", ExplorerItemKind.Category, ExplorerIcon.TableFunction)
		{
			Children = items,
		};
	}

	private ExplorerItem PopulateScalarFunctions(List<ScalarOrAggregateFunctionData> functions, string categoryName)
	{
		var items = new List<ExplorerItem>(functions.Count);

		foreach (var func in functions.OrderBy(static f => f.MethodName, StringComparer.Ordinal))
		{
			List<ExplorerItem>? children = null;
			if (func.Parameters.Count > 0)
			{
				children = new List<ExplorerItem>(func.Parameters.Count);
				AddParameters(func.Parameters, children);
			}

			items.Add(new ExplorerItem(func.MethodName, ExplorerItemKind.QueryableObject, ExplorerIcon.TableFunction)
			{
				DragText     = $"ExtensionMethods.{CSharpUtils.EscapeIdentifier(func.MethodName)}({string.Join(", ", func.Parameters.Select(GetParameterNameEscaped))})",
				Children     = children,
				IsEnumerable = false,
				SqlName      = func.DbName,
			});
		}

		return new ExplorerItem(categoryName, ExplorerItemKind.Category, ExplorerIcon.TableFunction)
		{
			Children = items,
		};
	}

	private ExplorerItem PopulateTables(List<TableData> tables, string category, ExplorerIcon icon, Dictionary<TableData, ExplorerItem> tablesLookup)
	{
		var children = new List<ExplorerItem>(tables.Count);

		foreach (var table in tables.OrderBy(static t => t.ContextName, StringComparer.Ordinal))
		{
			var tableChildren = new List<ExplorerItem>(table.Columns.Count);

			foreach (var column in table.Columns)
			{
				var dbName = GetDbName(column.DbName);
				var dbType = $"{GetTypeName(column.DataType, column.DbType)} {(column.Type.IsNullable ? "NULL" : "NOT NULL")}{(column.IsIdentity ? " IDENTITY" : string.Empty)}";

				tableChildren.Add(
					new ExplorerItem(
						$"{column.MemberName} : {SimpleBuildTypeName(column.Type)}",
						ExplorerItemKind.Property,
						column.IsPrimaryKey ? ExplorerIcon.Key : ExplorerIcon.Column)
					{
						ToolTipText        = $"{dbName} {dbType}",
						DragText           = CSharpUtils.EscapeIdentifier(column.MemberName),
						SqlName            = dbName,
						SqlTypeDeclaration = dbType,
					});
			}

			var tableNode = new ExplorerItem(table.ContextName, ExplorerItemKind.QueryableObject, icon)
			{
				DragText     = CSharpUtils.EscapeIdentifier(table.ContextName),
				ToolTipText  = SimpleBuildTypeName(table.ContextType),
				SqlName      = table.DbName,
				IsEnumerable = true,
				// we don't sort columns/associations and render associations after columns intentionally
				Children     = tableChildren,
			};

			tablesLookup.Add(table, tableNode);

			children.Add(tableNode);
		}

		return new ExplorerItem(category, ExplorerItemKind.Category, icon)
		{
			Children = children,
		};
	}

	private void PopulateAssociations(List<AssociationData> associations, Dictionary<TableData, ExplorerItem> tablesLookup)
	{
		foreach (var association in associations)
		{
			if (tablesLookup.TryGetValue(association.Source, out var sourceNode)
				&& tablesLookup.TryGetValue(association.Target, out var targetNode))
			{
				sourceNode.Children.Add(
					new ExplorerItem(
							association.MemberName,
							association.OneToMany && association.FromSide
								? ExplorerItemKind.CollectionLink
								: ExplorerItemKind.ReferenceLink,
							association.OneToMany && association.FromSide
								? ExplorerIcon.OneToMany
								: association.OneToMany && !association.FromSide
									? ExplorerIcon.ManyToOne
									: ExplorerIcon.OneToOne)
					{
						DragText        = CSharpUtils.EscapeIdentifier(association.MemberName),
						ToolTipText     = $"{SimpleBuildTypeName(association.Type)}{(!association.FromSide ? " // Back Reference" : null)}",
						SqlName         = association.KeyName,
						IsEnumerable    = association.OneToMany && association.FromSide,
						HyperlinkTarget = targetNode,
					});
			}
		}
	}

	#endregion

	private string GetDbName(string name, string? schema = null)
	{
		return sqlBuilder.BuildObjectName(
				new StringBuilder(),
				new SqlObjectName(Name: name, Schema: schema),
				tableOptions: TableOptions.NotSet)
			.ToString();
	}

	private string GetTypeName(DataType? dataType, DatabaseType type)
	{
		return sqlBuilder.BuildDataType(
				new StringBuilder(),
				new DbDataType(typeof(object),
					dataType : dataType ?? DataType.Undefined,
					dbType   : type.Name,
					length   : type.Length,
					precision: type.Precision,
					scale    : type.Scale))
			.ToString();
	}

	private readonly Dictionary<IType, string> _typeNameCache = new();

	// we use this method as we don't have type-only generation logic in scaffold framework
	// and actually we don't need such logic - simple C# type name generator below is enough for us
	private string SimpleBuildTypeName(IType type)
	{
		if (!_typeNameCache.TryGetValue(type, out var typeName))
		{
			typeName = type.Kind switch
			{
				TypeKind.Regular
					or TypeKind.TypeArgument => type.Name!.Name,
				TypeKind.Array               => $"{SimpleBuildTypeName(type.ArrayElementType!)}[]",
				TypeKind.Dynamic             => "dynamic",
				TypeKind.Generic             => $"{type.Name!.Name}<{string.Join(", ", type.TypeArguments!.Select(SimpleBuildTypeName))}>",
				TypeKind.OpenGeneric         => $"{type.Name!.Name}<{string.Join(", ", type.TypeArguments!.Select(static _ => string.Empty))}>",
				_                            => throw new InvalidOperationException($"Unsupported type kind: {type.Kind}"),
			};

			if (type.IsNullable)
				typeName += "?";

			_typeNameCache.Add(type, typeName);
		}

		return typeName;
	}
}
