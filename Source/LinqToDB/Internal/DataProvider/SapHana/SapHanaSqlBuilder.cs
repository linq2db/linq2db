using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SapHana;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SapHana
{
	public partial class SapHanaSqlBuilder : BasicSqlBuilder
	{
		public SapHanaSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected SapHanaSqlBuilder(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SapHanaSqlBuilder(this);
		}

		public override int CommandCount(SqlStatement statement)
		{
			return statement.NeedsIdentity ? 2 : 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			var insertClause = Statement.InsertClause;
			if (insertClause != null)
			{
				var identityField = insertClause.Into!.GetIdentityField();
				var table = insertClause.Into;

				if (identityField == null || table == null)
					throw new LinqToDBException($"Identity field must be defined for '{insertClause.Into.NameForLogging}'.");

				StringBuilder.Append("SELECT CURRENT_IDENTITY_VALUE() FROM DUMMY");
			}
		}

		protected override string LimitFormat(SelectQuery selectQuery)
		{
			return "LIMIT {0}";
		}

		protected override string OffsetFormat(SelectQuery selectQuery)
		{
			return selectQuery.Select.TakeValue == null ? "LIMIT 4200000000 OFFSET {0}" : "OFFSET {0}";
		}

		public override bool IsNestedJoinParenthesisRequired => true;

		protected override void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null)
			{
				AppendIndent().Append("CREATE COLUMN TABLE ");
				BuildPhysicalTable(createTable.Table, null);
			}
			else
			{
				var name = WithStringBuilder(
					static ctx =>
					{
						ctx.this_.BuildPhysicalTable(ctx.createTable.Table, null);
					}, (this_: this, createTable));

				AppendIndent().AppendFormat(CultureInfo.InvariantCulture, createTable.StatementHeader, name);
			}
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Int32         :
				case DataType.UInt16        :
					StringBuilder.Append("Integer");
					return;
				case DataType.Double:
					StringBuilder.Append("Double");
					return;
				case DataType.DateTime2     :
				case DataType.DateTime      :
					StringBuilder.Append("Timestamp");
					return;
				case DataType.Time:
					StringBuilder.Append("Time");
					return;
				case DataType.SmallDateTime :
					StringBuilder.Append("SecondDate");
					return;
				case DataType.Boolean       :
					StringBuilder.Append("TinyInt");
					return;
				case DataType.Image:
					StringBuilder.Append("Blob");
					return;
				case DataType.Xml:
					StringBuilder.Append("Clob");
					return;
				case DataType.Guid:
					StringBuilder.Append("Char (36)");
					return;
				case DataType.SmallDecFloat:
					StringBuilder.Append("SMALLDECIMAL");
					return;
				case DataType.DecFloat:
					StringBuilder.Append("DECIMAL");
					return;
				case DataType.Vector32:
					StringBuilder.Append("REAL_VECTOR");
					if (type.Length != null)
						StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");
					return;
				case DataType.NVarChar:
				case DataType.VarChar:
				case DataType.VarBinary:
					if (type.Length is null or > 5000 or < 1)
					{
						StringBuilder.Append(CultureInfo.InvariantCulture, $"{type.DataType}(5000)");

						return;
					}

					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
		}

		protected override void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0)
				StringBuilder.Append("FROM DUMMY").AppendLine();
			else
				base.BuildFromClause(statement, selectQuery);
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
					return sb.Append(':')
						.Append('"').Append(value).Append('"');

				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
				case ConvertType.SprocParameterToName:
					return sb.Append(value);

				case ConvertType.NameToQueryField     :
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
				case ConvertType.NameToServer         :
				case ConvertType.NameToDatabase       :
				case ConvertType.NameToSchema         :
				case ConvertType.NameToPackage        :
				case ConvertType.NameToQueryTable     :
				case ConvertType.NameToCteName        :
				case ConvertType.NameToProcedure      :
				{
					return value switch
					{
						['"', ..] => sb.Append(value),
						_ => sb.Append('"').Append(value).Append('"'),
					};
				}
			}

			return sb.Append(value);
		}

		protected override void BuildCreateTableIdentityAttribute1(SqlField field)
		{
			StringBuilder.Append("GENERATED BY DEFAULT AS IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("PRIMARY KEY (");
			StringBuilder.AppendJoinStrings(InlineComma, fieldNames);
			StringBuilder.Append(')');
		}

		protected override bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			switch (join.JoinType)
			{
				case JoinType.CrossApply:
					// join with function implies lateral keyword
					if (join.Table.SqlTableType == SqlTableType.Function)
						StringBuilder.Append("INNER JOIN ");
					else
						StringBuilder.Append("INNER JOIN LATERAL ");
					return true;
				case JoinType.OuterApply:
					// join with function implies lateral keyword
					if (join.Table.SqlTableType == SqlTableType.Function)
						StringBuilder.Append("LEFT JOIN ");
					else
						StringBuilder.Append("LEFT JOIN LATERAL ");
					return true;
			}

			return base.BuildJoinType(join, condition);
		}

		public override StringBuilder BuildObjectName(
			StringBuilder sb,
			SqlObjectName name,
			ConvertType objectType = ConvertType.NameToQueryTable,
			bool escape = true,
			TableOptions tableOptions = TableOptions.NotSet,
			bool withoutSuffix = false
		)
		{
			// <table_name> ::= [[<linked_server_name>.]<schema_name>.][library_name:]<identifier>
			if (name.Server != null && name.Schema == null)
				throw new LinqToDBException("You must specify schema name for linked server queries.");

			if (name.Server != null)
			{
				(escape ? Convert(sb, name.Server, ConvertType.NameToServer) : sb.Append(name.Server))
					.Append('.');
			}

			if (name.Schema != null)
			{
				(escape ? Convert(sb, name.Schema, ConvertType.NameToSchema) : sb.Append(name.Schema))
					.Append('.');
			}

			if (name.Package != null)
			{
				(escape ? Convert(sb, name.Package, ConvertType.NameToPackage) : sb.Append(name.Package))
					.Append(':');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override void BuildCreateTableCommand(SqlTable table)
		{
			var command = table.TableOptions.TemporaryOptionValue switch
			{
				TableOptions.IsTemporary                                                                              or
				TableOptions.IsTemporary |                                          TableOptions.IsLocalTemporaryData or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure                                     or
				TableOptions.IsTemporary | TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData or
																					TableOptions.IsLocalTemporaryData or
										   TableOptions.IsLocalTemporaryStructure                                     or
										   TableOptions.IsLocalTemporaryStructure | TableOptions.IsLocalTemporaryData =>
					"CREATE LOCAL TEMPORARY TABLE ",

				TableOptions.IsGlobalTemporaryStructure                                                               or
				TableOptions.IsGlobalTemporaryStructure | TableOptions.IsLocalTemporaryData                           =>
					"CREATE GLOBAL TEMPORARY TABLE ",

				0 =>
					"CREATE TABLE ",

				var value =>
					throw new InvalidOperationException($"Incompatible table options '{value}'"),
			};

			StringBuilder.Append(command);
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr) => BuildIsDistinctPredicateFallback(expr);

		protected override string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			if (DataProvider is SapHanaDataProvider provider)
			{
				var param = provider.TryGetProviderParameter(dataContext, parameter);
				if (param != null)
				{
					return provider.Provider switch
					{
						SapHanaProvider.ODBC => provider.Adapter.GetOdbcDbType!(param).ToString(),
						_ => provider.Adapter.GetDbType!(param).ToString(),
					};
				}
			}

			return base.GetProviderTypeName(dataContext, parameter);
		}

		protected override bool IsSqlValuesTableValueTypeRequired(SqlValuesTable source, IReadOnlyList<List<ISqlExpression>> rows, int row, int column)
		{
			return row == 0
				&& rows[0][column] is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal,
				};
		}
	}
}
