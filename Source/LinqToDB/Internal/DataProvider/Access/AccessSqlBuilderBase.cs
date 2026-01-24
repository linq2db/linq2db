using System;
using System.Collections.Generic;
using System.Text;

using LinqToDB.DataProvider;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access
{
	public abstract class AccessSqlBuilderBase : BasicSqlBuilder
	{
		protected AccessSqlBuilderBase(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(provider, mappingSchema, dataOptions, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected AccessSqlBuilderBase(BasicSqlBuilder parentBuilder) : base(parentBuilder)
		{
		}

		public override int CommandCount(SqlStatement statement)
		{
			return statement switch
			{
				SqlTruncateTableStatement trun => trun.ResetIdentity ? 1 + trun.Table!.IdentityFields.Count : 1,
				_ => statement.NeedsIdentity ? 2 : 1,
			};
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				var field = trun.Table!.IdentityFields[commandNumber - 1];

				StringBuilder.Append("ALTER TABLE ");
				BuildObjectName(StringBuilder, trun.Table.TableName, ConvertType.NameToQueryTable, true, trun.Table.TableOptions);
				StringBuilder.Append(" ALTER COLUMN ");
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				StringBuilder.AppendLine(" COUNTER(1, 1)");
			}
			else
			{
				StringBuilder.AppendLine("SELECT @@IDENTITY");
			}
		}

		public override    bool IsNestedJoinSupported         => false;
		public override    bool WrapJoinCondition             => true;
		protected override bool IsValuesSyntaxSupported       => false;
		protected override bool SupportsColumnAliasesInSource => false;

		#region Skip / Take Support

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "TOP {0}";
		}

		#endregion

		protected override bool ParenthesizeJoin(List<SqlJoinedTable> joins) => true;

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias,
			ref bool                                               addAlias)
		{
			if (expr is SqlValue { Value: null } sqlValue)
			{
				// NULL value typization. Critical for UNION, UNION ALL queries.
				//
				var type = sqlValue.ValueType.SystemType.UnwrapNullableType();

				object? defaultValue;
				if (type == typeof(string))
					defaultValue = "";
				else
					defaultValue = DefaultValue.GetValue(type);

				if (defaultValue != null)
				{
					StringBuilder.Append("IIF(False, ");
					BuildValue(sqlValue.ValueType, defaultValue);
					StringBuilder.Append(", NULL)");
					return;
				}
			}

			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
		}

		protected override void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			StringBuilder.Append("IIF(");
			BuildExpression(Precedence.Comparison, expr.Expr1);
			StringBuilder.Append(" = ");
			BuildExpression(Precedence.Comparison, expr.Expr2);
			StringBuilder.Append(" OR ");
			BuildExpression(Precedence.Comparison, expr.Expr1);
			StringBuilder.Append(" IS NULL AND ");
			BuildExpression(Precedence.Comparison, expr.Expr2);
			StringBuilder
				.Append(" IS NULL, 0, 1) = ")
				.Append(expr.IsNot ? '0' : '1');
		}

		protected override void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery,
			SqlUpdateClause                                    updateClause)
		{
			base.BuildFromClause(statement, selectQuery);
			StringBuilder.Remove(0, 4).Insert(0, "UPDATE");
			base.BuildUpdateSet(selectQuery, updateClause);
		}

		protected override void BuildSqlCaseExpression(SqlCaseExpression caseExpression)
		{
			BuildExpression(ConvertCaseToConditions(caseExpression, 0));
		}

		protected override void BuildSqlConditionExpression(SqlConditionExpression conditionExpression)
		{
			BuildSqlConditionExpressionAsFunction("IIF", conditionExpression);
		}

		protected override void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.DateTime2 : StringBuilder.Append("timestamp");                               break;
				default                 : base.BuildDataTypeFromDataType(type, forCreateTable, canBeNull); break;
			}
		}

		public override StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			// https://learn.microsoft.com/en-us/office/troubleshoot/access/error-using-special-characters
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return sb.Append('@').Append(value);

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 0 && value[0] == '[')
							return sb.Append(value);

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.NameToDatabase  :
				case ConvertType.NameToSchema    :
				case ConvertType.NameToQueryTable:
				case ConvertType.NameToProcedure :
					if (value.Length > 0 && value[0] == '[')
							return sb.Append(value);

					return sb.Append('[').Append(value).Append(']');

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'
						? sb.Append(value.AsSpan(1))
						: sb.Append(value);
			}

			return sb.Append(value);
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.AppendJoinStrings(InlineComma, fieldNames);
			StringBuilder.Append(')');
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
			if (name.Database != null)
			{
				(escape ? Convert(sb, name.Database, ConvertType.NameToDatabase) : sb.Append(name.Database))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected override void BuildMergeStatement(SqlMergeStatement merge)
		{
			throw new LinqToDBException($"{Name} provider doesn't support SQL MERGE statement");
		}

		protected override StringBuilder BuildSqlComment(StringBuilder sb, SqlComment comment)
		{
			// comments not supported by Access
			return sb;
		}

		protected override void BuildSubQueryExtensions(SqlStatement statement)
		{
			if (statement.SelectQuery?.SqlQueryExtensions is not null)
			{
				var len = StringBuilder.Length;

				AppendIndent();

				var prefix = Environment.NewLine;

				if (StringBuilder.Length > len)
				{
					var buffer = new char[StringBuilder.Length - len];

					StringBuilder.CopyTo(len, buffer, 0, StringBuilder.Length - len);

					prefix += new string(buffer);
				}

				BuildQueryExtensions(StringBuilder, statement.SelectQuery!.SqlQueryExtensions, null, prefix, Environment.NewLine, Sql.QueryExtensionScope.SubQueryHint);
			}
		}

		protected override void BuildQueryExtensions(SqlStatement statement)
		{
			if (statement.SqlQueryExtensions is not null)
				BuildQueryExtensions(StringBuilder, statement.SqlQueryExtensions, null, " ", null, Sql.QueryExtensionScope.QueryHint);
		}

		protected override void StartStatementQueryExtensions(SelectQuery? selectQuery)
		{
		}

		protected override void BuildParameter(SqlParameter parameter)
		{
			if (parameter.NeedsCast && BuildStep != Step.TypedExpression)
			{
				var paramValue = parameter.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues);

				var saveStep = BuildStep;
				BuildStep = Step.TypedExpression;

				// 1. Single parameter loose precision when used with CVar
				// 2. Only CVar accepts NULL
				if (paramValue.ProviderValue != null && parameter.Type.DataType is DataType.Single)
					StringBuilder.Append("CSng(");
				else
					StringBuilder.Append("CVar(");

				base.BuildParameter(parameter);
				StringBuilder.Append(')');
				BuildStep = saveStep;

				return;
			}

			base.BuildParameter(parameter);
		}

		protected override bool TryConvertParameterToSql(SqlParameterValue paramValue)
		{
			return paramValue.ProviderValue switch
			{
				// Access literals doesn't support less than second precision
				DateTime { Millisecond: not 0 } => false,
				_ => base.TryConvertParameterToSql(paramValue),
			};
		}

		protected override void BuildValue(DbDataType? dataType, object? value)
		{
			// Access literals doesn't support less than second precision
			if (value is DateTime dt && dt.Millisecond != 0)
			{
				BuildParameter(new SqlParameter(dataType ?? MappingSchema.GetDbDataType(typeof(DateTime)), "value", value));
				return;
			}

			base.BuildValue(dataType, value);
		}
	}
}
