using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	sealed partial class SybaseSqlBuilder : BasicSqlBuilder
	{
		public SybaseSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		SybaseSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SybaseSqlBuilder(this) { _skipAliases = _isSelect };
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("SELECT @@IDENTITY");
		}

		protected override bool SupportsColumnAliasesInSource => true;

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "TOP {0}";
		}

		bool _isSelect;
		bool _skipAliases;

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			_isSelect = true;
			base.BuildSelectClause(selectQuery);
			_isSelect = false;
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias,
			ref bool                                               addAlias)
		{
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);

			if (_skipAliases) addAlias = false;
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Guid      : StringBuilder.Append("VARCHAR(36)"); return;
				case DataType.DateTime2 : StringBuilder.Append("DateTime");    return;
				case DataType.Money     : StringBuilder.Append("MONEY");       return;
				case DataType.SmallMoney: StringBuilder.Append("SMALLMONEY");  return;
				case DataType.NVarChar  :
					// yep, 5461...
					if (type.Length == null || type.Length > 5461 || type.Length < 1)
					{
						StringBuilder.Append("NVarChar(5461)");
						return;
					}

					break;
				case DataType.Decimal:
					// default is ASE is 18,0
					if (type.Scale != null)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"DECIMAL({type.Precision ?? 18}{InlineComma}{type.Scale})");
					else if (type.Precision != null)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"DECIMAL({type.Precision})");
					else
						StringBuilder.Append("DECIMAL");
					return;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}

		protected override void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			// BIT cannot be nullable in ASE
			if (field.CanBeNull && field.Type.DataType == DataType.Boolean)
				return;

			base.BuildCreateTableNullAttribute(field, defaultNullable);
		}

		protected override void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			var selectQuery = deleteStatement.SelectQuery;

			AppendIndent();
			StringBuilder.Append("DELETE");
			BuildSkipFirst(selectQuery);
			StringBuilder.Append(" FROM ");

			ISqlTableSource table;
			ISqlTableSource source;

			if (deleteStatement.Table != null)
				table = source = deleteStatement.Table;
			else
			{
				table  = selectQuery.From.Tables[0];
				source = selectQuery.From.Tables[0].Source;
			}

			var alias = GetTableAlias(table);
			BuildPhysicalTable(source, alias);

			StringBuilder.AppendLine();
		}

		protected override void BuildUpdateTableName(SelectQuery selectQuery,
			SqlUpdateClause                                      updateClause)
		{
			if (updateClause.Table != null && (selectQuery.From.Tables.Count == 0 || updateClause.Table != selectQuery.From.Tables[0].Source))
				BuildPhysicalTable(updateClause.Table, null);
			else
				BuildTableName(selectQuery.From.Tables[0], true, false);
		}

		bool _skipBrackets;

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:

					if (value.Length > 26)
						value = value.Substring(0, 26);

					if (value.Length == 0 || value[0] != '@')
						sb.Append('@');

					return sb.Append(value);

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (_skipBrackets || value.Length > 28 || value.Length > 0 && value[0] == '[')
						return sb.Append(value);

					// https://github.com/linq2db/linq2db/issues/1064
					if (convertType == ConvertType.NameToQueryField && Name.Length > 0 && value[0] == '#')
						return sb.Append(value);

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToProcedure :
					if (_skipBrackets || value.Length > 28 || value.Length > 0 && (value[0] == '[' || value[0] == '#'))
						return sb.Append(value);

					if (value.IndexOf('.') > 0)
						value = string.Join("].[", value.Split('.'));

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.Substring(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsUpdateInsert(insertOrUpdate);
		}

		protected override void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.AppendLine("VALUES ()");
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(string.Join(InlineComma, fieldNames));
			StringBuilder.Append(')');
		}

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is SybaseDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
					return provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			StringBuilder.Append("TRUNCATE TABLE ");
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity && trun.Table!.IdentityFields.Count > 0 ? 2 : 1;

			return 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				StringBuilder.Append("sp_chgattribute ");
				BuildObjectName(StringBuilder, trun.Table!.TableName, ConvertType.NameToQueryTable, true, trun.Table.TableOptions);
				StringBuilder.AppendLine(", 'identity_burn_max', 0, '0'");
			}
		}

		private void BuildIdentityInsert(NullabilityContext nullability, SqlTableSource table, bool enable)
		{
			StringBuilder.Append("SET IDENTITY_INSERT ");
			BuildTableName(table, true, false);
			StringBuilder.AppendLine(enable ? " ON" : " OFF");
		}

		private static string GetTablePhysicalName(string physicalName, TableOptions tableOptions)
		{
			if (physicalName.StartsWith("#") || !tableOptions.IsTemporaryOptionSet())
				return physicalName;

			switch (tableOptions & TableOptions.IsTemporaryOptionSet)
			{
				case TableOptions.IsTemporary                                                                              :
				case TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData :
				case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     :
				case TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
				case                                                                     TableOptions.IsLocalTemporaryData :
				case                            TableOptions.IsLocalTemporaryStructure                                     :
				case                            TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData :
					return $"#{physicalName}";
				case TableOptions.IsGlobalTemporaryStructure                                                               :
				case TableOptions.IsGlobalTemporaryStructure | TableOptions.IsGlobalTemporaryData                          :
					return $"##{physicalName}";
				case var value :
					throw new InvalidOperationException($"Incompatible table options '{value}'");
			}
		}

		public override StringBuilder BuildObjectName(StringBuilder sb, SqlObjectName name, ConvertType objectType, bool escape, TableOptions tableOptions, bool withoutSuffix = false)
		{
			if (name.Database != null && IsTemporary(name.Name, tableOptions))
				name = name with { Database = null };

			name = name with { Name = GetTablePhysicalName(name.Name, tableOptions) };

			return base.BuildObjectName(sb, name, objectType, escape, tableOptions, withoutSuffix: withoutSuffix);
		}

		protected override void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			var nullability = NullabilityContext.NonQuery;
			var table       = dropTable.Table!;

			BuildTag(dropTable);

			if (dropTable.Table.TableOptions.HasDropIfExists())
			{
				_skipBrackets = true;
				StringBuilder.Append("IF (OBJECT_ID(N'");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine("') IS NOT NULL)");
				_skipBrackets = false;

				Indent++;
			}

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(table, null);

			if (dropTable.Table.TableOptions.HasDropIfExists())
				Indent--;
		}

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			var nullability = NullabilityContext.NonQuery;

			if (createTable.StatementHeader == null && createTable.Table!.TableOptions.HasCreateIfNotExists())
			{
				var table = createTable.Table;

				var isTemporary = IsTemporary(table.TableName.Name, table.TableOptions);

				_skipBrackets = true;
				StringBuilder.Append("IF (OBJECT_ID(N'");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine("') IS NULL)");
				_skipBrackets = false;

				if (!isTemporary)
				{
					Indent++;
					AppendIndent().AppendLine("EXECUTE('");
				}

				Indent++;
			}

			base.BuildStartCreateTableStatement(createTable);
		}

		protected override void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			base.BuildEndCreateTableStatement(createTable);

			if (createTable.StatementHeader == null && createTable.Table!.TableOptions.HasCreateIfNotExists())
			{
				if (!IsTemporary(createTable.Table.TableName.Name, createTable.Table.TableOptions))
				{
					Indent--;
					AppendIndent().AppendLine("')");
				}

				Indent--;
			}
		}

		static bool IsTemporary(string tableName, TableOptions tableOptions)
		{
			return tableOptions.IsTemporaryOptionSet() || tableName.StartsWith("#");
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(expr);

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows, int row, int column)
		{
			if (row == 0)
			{
				if (rows[0][column] is SqlValue
					{
						Value: uint or long or ulong or float or double or decimal or null
					})
				{
					return true;
				}
			}

			return false;
		}

		protected override void BuildTypedExpression(DbDataType dataType, ISqlExpression value)
		{
			if (value is SqlValue { Value: decimal val })
			{
				var precision = DecimalHelper.GetPrecision(val);
				var scale = DecimalHelper.GetScale(val);
				if (precision == 0 && scale == 0)
					precision = 1;
				dataType = dataType.WithPrecision(precision).WithScale(scale);
			}
			else if (value is SqlParameter param)
			{
				var paramValue = param.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues);

				if (paramValue.ProviderValue is decimal decValue)
				{
					var precision = DecimalHelper.GetPrecision(decValue);
					var scale = DecimalHelper.GetScale(decValue);
					if (precision == 0 && scale == 0)
						precision = 1;
					dataType = dataType.WithPrecision(precision).WithScale(scale);
				}
			}

			base.BuildTypedExpression(dataType, value);
		}
	}
}
