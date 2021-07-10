using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LinqToDB.CodeGen.CodeModel;
using LinqToDB.CodeGen.Metadata;
using LinqToDB.SqlProvider;

namespace LinqToDB.CodeGen.ContextModel
{
	public static class ContextModelBuilder
	{
		public static DataContextModel Build(
			ISqlBuilder sqlBuilder,
			DataModel dataModel,
			ContextModelSettings settings,
			ILanguageServices langServices,
			INameConverterProvider pluralizationProvider)
		{
			var className = NormalizeIdentifier(
				settings.DataContextClassNameNormalization,
				settings.ContextClassName ?? dataModel.DatabaseName ?? throw new InvalidOperationException("TODO"),
				langServices,
				pluralizationProvider);

			var baseClassName = settings.BaseContextClassName ?? "LinqToDB.Data.DataConnection";

			var context = new DataContextClass(className, baseClassName);

			var model = new DataContextModel(context, "DataModel");
			model.ServerVersion = dataModel.ServerVersion;
			model.DataSource = dataModel.DataSource;
			model.DatabaseName = dataModel.DatabaseName;

			var schemaContexts = new Dictionary<string, SchemaContextModel>();
			if (settings.GenerateSchemaAsType)
			{
				foreach (var schema in dataModel.Schemas)
				{
					if (!dataModel.DefaultSchemas.Contains(schema))
					{
						var baseTypeName = schema;
						if (settings.SchemaMap.TryGetValue(schema, out var replacement))
							baseTypeName = replacement;
						var ctx = new SchemaContextModel(baseTypeName);
						schemaContexts.Add(schema, ctx);
						model.SchemaContexts.Add(ctx);
					}
				}
			}

			var entities = LoadTables(dataModel, model, schemaContexts, settings, langServices, pluralizationProvider);
			LoadFunctions(sqlBuilder, dataModel, model, schemaContexts, settings, langServices, pluralizationProvider, entities);

			return model;
		}

		private static IDictionary<TableBase, EntityModel> LoadTables(
			DataModel dataModel,
			DataContextModel context,
			Dictionary<string, SchemaContextModel> schemaContexts,
			ContextModelSettings settings,
			ILanguageServices langServices,
			INameConverterProvider pluralizationProvider)
		{
			var tablesAndViews = dataModel.Tables.Cast<TableBase>().Concat(dataModel.Views).OrderBy(t => t.Name.Name);

			var tableMap = new Dictionary<TableBase, EntityModel>(ReferenceComparer<TableBase>.Instance);
			var columnMap = new Dictionary<Column, ColumnModel>(ReferenceComparer<Column>.Instance);

			foreach (var table in tablesAndViews.Where(t => !t.IsSystem))
			{
				var entity = new EntityModel(settings.BaseEntityClass);

				if (settings.GenerateSchemaAsType && table.Name.Schema != null && !dataModel.DefaultSchemas.Contains(table.Name.Schema))
					schemaContexts[table.Name.Schema].Entities.Add(entity);
				else
					context.Entities.Add(entity);

				tableMap.Add(table, entity);

				entity.Schema = table.Name.Schema == null || dataModel.DefaultSchemas.Contains(table.Name.Schema) ? null : table.Name.Schema;

				entity.IsView = table is View;
				entity.IsSystem = false;
				entity.Description = table.Description;

				entity.TableName = table.Name;
				if (!settings.GenerateDefaultSchema && table.Name.Schema != null && dataModel.DefaultSchemas.Contains(table.Name.Schema))
					entity.TableName = table.Name with { Schema = null };

				var customEntityPropertyName = settings.EntityContextPropertyNameProvider?.Invoke(table);
				entity.NameInContext = customEntityPropertyName != null
					? langServices.NormalizeIdentifier(customEntityPropertyName)
					: NormalizeIdentifier(
						settings.EntityContextPropertyNameNormalization,
						settings.EntityContextPropertyNameProvider?.Invoke(table) ?? table.Name.Name,
						langServices,
						pluralizationProvider);

				var customClassName = settings.EntityClassNameProvider?.Invoke(table);
				entity.ClassName = customClassName != null
					? langServices.NormalizeIdentifier(customClassName)
					: NormalizeIdentifier(
						settings.EntityClassNameNormalization,
						table.Name.Name,
						langServices,
						pluralizationProvider);
				if (customClassName == null && !settings.GenerateSchemaAsType && table.Name.Schema != null && !dataModel.DefaultSchemas.Contains(table.Name.Schema))
					entity.ClassName = langServices.NormalizeIdentifier(table.Name.Schema + "_" + entity.ClassName);

				foreach (var column in table.Columns)
				{
					var col = new ColumnModel();
					entity.Columns.Add(col);
					columnMap.Add(column, col);
					col.ColumnName = column.Name;
					col.Type = column.DbType;
					col.DataType = dataModel.TypeMap[column.DbType].dataType;
					col.Description = column.Description;
					col.IsPrimaryKey = table.PrimaryKey?.OrderedColumns.Contains(column) ?? false;
					col.PrimaryKeyOrdinal = table.PrimaryKey?.GetPosition(column);
					col.PropertyName = NormalizeIdentifier(
						settings.EntityColumnPropertyNameNormalization,
						column.Name,
						langServices,
						pluralizationProvider);
					col.CLRType = dataModel.TypeMap[column.DbType].type;
					col.ProviderType = dataModel.TypeMap[column.DbType].providerType;
					col.IsIdentity = column == table.Identity?.Column;

					col.CanInsert = column.Insertable;
					col.CanUpdate = column.Updateable;
				}
			}

			// database technically could have multiple same FKs with different names
			// duplicates have no value for mappings as there is no way to say (except maybe hints) which FK
			// to use
			var duplicateFKs = new Dictionary<(TableBase from, TableBase to), List<ISet<(Column from, Column to)>>>();

			foreach (var fk in dataModel.ForeignKeys)
			{
				var columnsSet = new HashSet<(Column, Column)>(fk.OrderedColumns);

				if (duplicateFKs.TryGetValue((fk.Source, fk.Target), out var list))
				{
					var isDuplicate = false;
					foreach (var knowKey in list)
					{
						if (knowKey.Count == columnsSet.Count)
						{
							var differ = false;
							foreach (var pair in columnsSet)
							{
								if (!knowKey.Contains(pair))
								{
									differ = true;
									break;
								}
							}

							if (!differ)
							{
								isDuplicate = true;
								break;
							}
						}
					}

					// skip duplicate key
					if (isDuplicate)
						continue;
				}
				else
				{
					duplicateFKs.Add((fk.Source, fk.Target), new List<ISet<(Column, Column)>>() { columnsSet });
				}

				var fkEntity = tableMap[fk.Source];
				var targetEntity = tableMap[fk.Target];

				var association = new Association();
				context.Associations.Add(association);

				association.SourceEntity = fkEntity;
				association.TargetEntity = targetEntity;

				association.SourceIsOptional = true;
				association.TargetIsOptional = true;

				var fromIsPK = true;
				//var toIsPK = true;

				foreach (var (from, to) in fk.OrderedColumns)
				{
					association.SourceColumns.Add(columnMap[from]);
					association.TargetColumns.Add(columnMap[to]);

					association.SourceIsOptional &= from.DbType.IsNullable;

					if (fromIsPK && fk.Source.PrimaryKey?.OrderedColumns.Contains(from) != true)
						fromIsPK = false;
					//if (toIsPK && fk.Target.PrimaryKey?.OrderedColumns.Contains(to) != true)
					//	toIsPK = false;
				}

				// TODO: speculation, e.g. could have unique constraint of some kind
				association.ManyToOne = !fromIsPK;

				association.SourceMemberName = TransformAssociationMemberName(fk, true, settings.SingularForeignKeyAssociationPropertyNameNormalization, langServices, pluralizationProvider, dataModel.DefaultSchemas);
				if (association.ManyToOne)
					association.TargetMemberName = TransformAssociationMemberName(fk, false, settings.MultiplePrimaryKeyAssociationPropertyNameNormalization, langServices, pluralizationProvider, dataModel.DefaultSchemas);
				else
					association.TargetMemberName = TransformAssociationMemberName(fk, false, settings.SingularPrimaryKeyAssociationPropertyNameNormalization, langServices, pluralizationProvider, dataModel.DefaultSchemas);

				association.KeyName = fk.Name;
			}

			return tableMap;
		}

		

		private static string TransformAssociationMemberName(
			ForeignKey fk,
			bool fromMember,
			ObjectNormalizationSettings settings,
			ILanguageServices langServices,
			INameConverterProvider pluralizationProvider,
			ISet<string> defaultSchemas)
		{
			var name = fromMember ? fk.Target.Name.Name : fk.Source.Name.Name;
			if (settings.Transformation == NameTransformation.T4Association)
			{
				// port of SetForeignKeyMemberName method (approximate)

				string? newName = null;

				// TODO: interceptors
				//if (schemaOptions.GetAssociationMemberName != null)
				//{
				//	newName = schemaOptions.GetAssociationMemberName(key);

				//	if (newName != null)
				//		name = ToValidName(newName);
				//}

				//if (newName == null)
				{
					newName = fk.Name;

					if (fromMember && fk.OrderedColumns.Count == 1 && fk.OrderedColumns[0].sourceColumn.Name.ToLower().EndsWith("id"))
					{
						newName = fk.OrderedColumns[0].sourceColumn.Name;
						newName = newName.Substring(0, newName.Length - "id".Length).TrimEnd('_');
					}
					else
					{
						if (newName.StartsWith("FK_"))
							newName = newName.Substring(3);
						//if (newName.EndsWith("_BackReference"))
							//newName = newName.Substring(0, newName.Length - "_BackReference".Length);

						var tableName = fromMember ? fk.Source.Name : fk.Target.Name;

						newName = string.Concat(newName
							.Split('_')
							.Where(_ =>
								_.Length > 0 && _ != tableName.Name &&
								(tableName.Schema == null || defaultSchemas.Contains(tableName.Schema) || _ != tableName.Schema)));

						var skip = true;
						newName = string.Concat(newName.EnumerateCharacters().Reverse().Select(_ =>
						{
							if (skip)
							{
								if (_.category == UnicodeCategory.DecimalDigitNumber)
									return string.Empty;
								else
									skip = false;
							}

							return _.character;
						}).Reverse());
					}

					if (string.IsNullOrEmpty(newName))
						newName = fk.Source != fk.Target ? (fromMember ? fk.Target.Name.Name : fk.Source.Name.Name) : fk.Name;
				}

				name = newName;
			}
			return NormalizeIdentifier(settings, name, langServices, pluralizationProvider);
		}

		public static string NormalizeIdentifier(
			ObjectNormalizationSettings settings,
			string name,
			ILanguageServices langServices,
			INameConverterProvider pluralizationProvider)
		{
			//if (name == string.Empty)
			//	name = settings.DefaultValue ?? name;

			var mixedCase = name.ToUpper() != name;
			// skip normalization for ALLCAPS names
			if (!settings.DontCaseAllCaps || name.EnumerateCharacters().Any(c => c.category != UnicodeCategory.UppercaseLetter))
			{
				// first split identifier into text/non-text fragments, optionally treat underscore as discarded separator
				var words = name.SplitIntoWords(settings.Transformation == NameTransformation.SplitByUnderscore
					|| settings.Transformation == NameTransformation.T4Association);

				// find last word to apply pluralization to it (if configured)
				var lastTextIndex = -1;
				if (settings.Pluralization != Pluralization.None)
				{
					for (var i = words.Count; i > 0; i--)
					{
						if (words[i - 1].isText)
						{
							lastTextIndex = i - 1;
							break;
						}
					}
				}

				if (settings.PluralizeOnlyIfLastWordIsText && lastTextIndex != words.Count - 1)
					lastTextIndex = -1;

				// recreate identifier from words applying casing rules and pluralization
				var identifier = new StringBuilder();
				var firstWord = true;
				for (var i = 0; i < words.Count; i++)
				{
					var (word, isText) = words[i];

					if (!isText)
						identifier.Append(word);
					else
					{
						// apply pluralization (to lowercased form to not confuse pluralizer)
						if (lastTextIndex == i)
						{
							var normalized = word.ToLower();
							var toUpperCase = settings.Casing == NameCasing.T4CompatNonPluralized && normalized != word && word == word.ToUpper();
							word = normalized.Pluralize(settings.Pluralization, pluralizationProvider);
							if (toUpperCase)
								word = word.ToUpper();
						}

						// apply casing rules
						if (isText)
							word = word.ApplyCasing(settings.Casing, firstWord, i == words.Count - 1, mixedCase);

						// append to whole identifier with casing-specific separator (only before text fragment)
						if (isText && settings.Casing == NameCasing.SnakeCase && identifier.Length > 0)
							identifier.Append('_');
						identifier.Append(word);

						firstWord = false;
					}
				}

				name = identifier.ToString();
			}

			// apply fixed prefix/suffix (ignores other options like casing)
			name = settings.Prefix + name + settings.Suffix;

			// fix final identifier to only contain characters, allowed by target language
			name = langServices.NormalizeIdentifier(name);

			return name;
		}

		private static void LoadFunctions(
			ISqlBuilder sqlBuilder,
			DataModel dataModel,
			DataContextModel model,
			Dictionary<string, SchemaContextModel> schemaContexts,
			ContextModelSettings settings,
			ILanguageServices langServices,
			INameConverterProvider pluralizationProvider,
			IDictionary<TableBase, EntityModel> entities)
		{
			var procedures = dataModel.StoredProcedures.Cast<FunctionBase>()
					.Concat(dataModel.TableFunctions)
					.Concat(dataModel.ScalarFunctions)
					.Concat(dataModel.Aggregates);
			foreach (var func in procedures)
			{
				var dbName = func.Name;
				if (!settings.GenerateDefaultSchema && dbName.Schema != null && dataModel.DefaultSchemas.Contains(dbName.Schema))
					dbName = dbName with { Schema = null };

				FunctionModelBase funcModel;
				if (func is StoredProcedure sp)
				{
					var spm = new StoredProcedureModel();
					if (settings.GenerateSchemaAsType && func.Name.Schema != null && !dataModel.DefaultSchemas.Contains(func.Name.Schema))
						schemaContexts[func.Name.Schema].StoredProcedures.Add(spm);
					else
						model.StoredProcedures.Add(spm);
					funcModel = spm;

					if (sp.ReturnParameter != null)
					{
						spm.Return = new ReturnParameter()
						{
							Type = sp.ReturnParameter.Type,
							DbName = sp.ReturnParameter.DbName,
							DataType = dataModel.TypeMap[sp.ReturnParameter.Type].dataType,
							CLRType = dataModel.TypeMap[sp.ReturnParameter.Type].type,
							ProviderType = dataModel.TypeMap[sp.ReturnParameter.Type].providerType,
							ParameterName = NormalizeIdentifier(
								settings.ProcedureParameterNameNormalization,
								sp.ReturnParameter.DbName ?? "return",
								langServices,
								pluralizationProvider)
						};
					}

					// TODO: unfortunatelly we still miss sproc API, which works with unprepared names
					spm.FullName = sqlBuilder.BuildTableName(
						new StringBuilder(),
						dbName.Server == null ? null : sqlBuilder.ConvertInline(dbName.Server, ConvertType.NameToServer),
						dbName.Database == null ? null : sqlBuilder.ConvertInline(dbName.Database, ConvertType.NameToDatabase),
						dbName.Schema == null ? null : sqlBuilder.ConvertInline(dbName.Schema, ConvertType.NameToSchema),
													  sqlBuilder.ConvertInline(dbName.Name, ConvertType.NameToQueryTable),
						TableOptions.NotSet
					).ToString();

					if (sp.ResultSets.Count > 1)
					{
						spm.ResultSetClassName = NormalizeIdentifier(
							settings.ProcedureResultSetClassNameNormalization,
							sp.Name.Name!,
							langServices,
							pluralizationProvider);
						var idx = 1;
						foreach (var rs in sp.ResultSets)
						{
							spm.Results.Add(PrepareResultSetModel(dataModel, sp.Name, rs, settings, langServices, pluralizationProvider, idx, entities));
							idx++;
						}
					}
					else if (sp.ResultSets.Count == 1)
					{
						spm.Results.Add(PrepareResultSetModel(dataModel, sp.Name, sp.ResultSets[0], settings, langServices, pluralizationProvider, null, entities));
					}

					//public List<ResultTableModel> Results { get; set; } = new();
				}
				else if (func is TableFunction tf)
				{
					var tfm = new TableFunctionModel();
					if (settings.GenerateSchemaAsType && func.Name.Schema != null && !dataModel.DefaultSchemas.Contains(func.Name.Schema))
						schemaContexts[func.Name.Schema].TableFunctions.Add(tfm);
					else
						model.TableFunctions.Add(tfm);
					funcModel = tfm;

					var (rs, entity) = PrepareResultSetModel(dataModel, tf.Name, tf.Table, settings, langServices, pluralizationProvider, null, entities);
					tfm.CustomResult = rs;
					tfm.EntityResult = entity;
				}
				else if (func is ScalarFunction sf)
				{
					var sfm = new ScalarFunctionModel();
					if (settings.GenerateSchemaAsType && func.Name.Schema != null && !dataModel.DefaultSchemas.Contains(func.Name.Schema))
						schemaContexts[func.Name.Schema].ScalarFunctions.Add(sfm);
					else
						model.ScalarFunctions.Add(sfm);
					funcModel = sfm;

					//sfm.IsDynamic = sf.IsDynamicResult;
					// TODO: unfortunatelly we miss function mapping, which works with raw names
					sfm.FullName = sqlBuilder.BuildTableName(
						new StringBuilder(),
						dbName.Server == null ? null : sqlBuilder.ConvertInline(dbName.Server, ConvertType.NameToServer),
						dbName.Database == null ? null : sqlBuilder.ConvertInline(dbName.Database, ConvertType.NameToDatabase),
						dbName.Schema == null ? null : sqlBuilder.ConvertInline(dbName.Schema, ConvertType.NameToSchema),
													  sqlBuilder.ConvertInline(dbName.Name, ConvertType.NameToQueryTable),
						TableOptions.NotSet
					).ToString();

					if (sf.Result.Length > 1)
					{
						sfm.TupleTypeName = NormalizeIdentifier(
							settings.FunctionTupleResultClassName,
							sf.Name.Name!,
							langServices,
							pluralizationProvider);

						sfm.Type = sf.Result.Select(t => (new TypeModel()
						{
							Type = t.Type,
							DataType = dataModel.TypeMap[t.Type].dataType,
							CLRType = dataModel.TypeMap[t.Type].type,
							ProviderType = dataModel.TypeMap[t.Type].providerType
						}, (string?)NormalizeIdentifier(
							settings.FunctionTupleResultPropertyName,
							t.DbName ?? string.Empty,
							langServices,
							pluralizationProvider))).ToArray();
					}
					else
					{
						sfm.Type = new[]
						{
							(new TypeModel()
							{
								Type = sf.Result[0].Type,
								DataType = dataModel.TypeMap[sf.Result[0].Type].dataType,
								CLRType = dataModel.TypeMap[sf.Result[0].Type].type,
								ProviderType = dataModel.TypeMap[sf.Result[0].Type].providerType
							}, (string?)null)
						};
					}
				}
				else if (func is Aggregate agg)
				{
					var aggm = new AggregateModel();

					aggm.FullName = sqlBuilder.BuildTableName(
						new StringBuilder(),
						dbName.Server == null ? null : sqlBuilder.ConvertInline(dbName.Server, ConvertType.NameToServer),
						dbName.Database == null ? null : sqlBuilder.ConvertInline(dbName.Database, ConvertType.NameToDatabase),
						dbName.Schema == null ? null : sqlBuilder.ConvertInline(dbName.Schema, ConvertType.NameToSchema),
													  sqlBuilder.ConvertInline(dbName.Name, ConvertType.NameToQueryTable),
						TableOptions.NotSet).ToString();

					if (settings.GenerateSchemaAsType && func.Name.Schema != null && !dataModel.DefaultSchemas.Contains(func.Name.Schema))
						schemaContexts[func.Name.Schema].Aggregates.Add(aggm);
					else
						model.Aggregates.Add(aggm);
					funcModel = aggm;

					aggm.Type = new TypeModel()
					{
						Type = agg.Result.Type,
						DataType = dataModel.TypeMap[agg.Result.Type].dataType,
						CLRType = dataModel.TypeMap[agg.Result.Type].type,
						ProviderType = dataModel.TypeMap[agg.Result.Type].providerType
					};
				}
				else
					throw new InvalidOperationException();

					
				if (func is TableFunctionBase tfb && tfb.SchemaError != null)
				{
					((TableFunctionModelBase)funcModel).SchemaError = tfb.SchemaError.Message;
				}

				funcModel.DbName = dbName;

				funcModel.Description = func.Description;
				funcModel.MethodName = NormalizeIdentifier(
						settings.ProcedureNameNormalization,
						func.Name.Name,
						langServices,
						pluralizationProvider);

				funcModel.MethodInfoName = NormalizeIdentifier(
					settings.ProcedureMethodInfoFieldNameNormalization,
					func.Name.Name,
					langServices,
					pluralizationProvider);

				foreach (var param in func.Parameters)
				{
					var p = new ParameterModel();
					funcModel.Parameters.Add(p);

					p.DbName = param.Name;
					p.Description = param.Description;
					p.ParameterName = NormalizeIdentifier(
						settings.ProcedureParameterNameNormalization,
						param.Name,
						langServices,
						pluralizationProvider);
					p.Type = param.Type;
					p.DataType = dataModel.TypeMap[param.Type].dataType;
					p.CLRType = dataModel.TypeMap[param.Type].type;
					p.ProviderType = dataModel.TypeMap[param.Type].providerType;
					p.Direction = (func is not StoredProcedure) && param.Direction == ParameterDirection.InOut ? ParameterDirection.In : param.Direction;
				}
			}
		}

		private static (ResultTableModel? customModel, EntityModel? entity) PrepareResultSetModel(
			DataModel dataModel,
			ObjectName name,
			IReadOnlyCollection<Column> columns,
			ContextModelSettings settings,
			ILanguageServices langServices,
			INameConverterProvider pluralizationProvider,
			int? resultIndex,
			IDictionary<TableBase, EntityModel> entities)
		{
			if (settings.MapProcedureResultToEntity)
			{
				var names = new Dictionary<string, DbType>();
				foreach (var column in columns)
				{
					if (column.Name == string.Empty || names.ContainsKey(column.Name))
					{
						break;
					}
					names.Add(column.Name, column.DbType);
				}
				if (names.Count == columns.Count)
				{
					foreach (var table in dataModel.Tables.Cast<TableBase>().Concat(dataModel.Views))
					{
						if (table.Columns.Count == names.Count)
						{
							var match = true;
							foreach (var column in table.Columns)
							{
								if (!names.TryGetValue(column.Name, out var type) || !type.Equals(column.DbType))
								{
									match = false;
									break;
								}
							}

							if (match)
								return (null, entities[table]);
						}
					}
				}
			}

			var model = new ResultTableModel();
			var classNameOptions = settings.ProcedureResultClassNameNormalization;
			if (resultIndex != null)
			{
				model.ResultSetPropertyName = NormalizeIdentifier(
					settings.ProcedureResultSetClassPropertyNameNormalization,
					name.Name,
					langServices,
					pluralizationProvider);

				classNameOptions = classNameOptions.Clone();
				classNameOptions.Suffix += resultIndex.Value.ToString(NumberFormatInfo.InvariantInfo);
			}

			model.ClassName = NormalizeIdentifier(
					classNameOptions,
					name.Name,
					langServices,
					pluralizationProvider);

			foreach (var col in columns)
			{
				var colModel = new ResultTableColumnModel();
				model.Columns.Add(colModel);

				colModel.ColumnName = col.Name;
				colModel.Type = col.DbType;
				colModel.DataType = dataModel.TypeMap[col.DbType].dataType;
				colModel.CLRType = dataModel.TypeMap[col.DbType].type;
				colModel.ProviderType = dataModel.TypeMap[col.DbType].providerType;

				colModel.PropertyName = NormalizeIdentifier(
					settings.ProcedureResultColumnPropertyNameNormalization,
					col.Name,
					langServices,
					pluralizationProvider);
			}

			return (model, null);
		}
	}
}
