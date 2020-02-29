using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using SqlQuery;
	using SqlProvider;
	using LinqToDB.Mapping;

	partial class SybaseSqlBuilder : BasicSqlBuilder
	{
		private readonly SybaseDataProvider? _provider;

		public SybaseSqlBuilder(
			SybaseDataProvider? provider,
			MappingSchema       mappingSchema,
			ISqlOptimizer       sqlOptimizer,
			SqlProviderFlags    sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider = provider;
		}

		// remote context
		public SybaseSqlBuilder(
			MappingSchema    mappingSchema,
			ISqlOptimizer    sqlOptimizer,
			SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override void BuildGetIdentity(SqlInsertClause insertClause)
		{
			StringBuilder
				.AppendLine()
				.AppendLine("SELECT @@IDENTITY");
		}

		protected override string FirstFormat(SelectQuery selectQuery)
		{
			return "TOP {0}";
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		private  bool _isSelect;
		readonly bool _skipAliases;

		SybaseSqlBuilder(SybaseDataProvider? provider, bool skipAliases, MappingSchema mappingSchema, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(mappingSchema, sqlOptimizer, sqlProviderFlags)
		{
			_provider    = provider;
			_skipAliases = skipAliases;
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			_isSelect = true;
			base.BuildSelectClause(selectQuery);
			_isSelect = false;
		}

		protected override void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
					wrap = expr is SqlExpression ex && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");

			if (_skipAliases) addAlias = false;
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SybaseSqlBuilder(_provider, _isSelect, MappingSchema, SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.DataType)
			{
				case DataType.DateTime2 : StringBuilder.Append("DateTime");       return;
				case DataType.NVarChar:
					// yep, 5461...
					if (type.Length == null || type.Length > 5461 || type.Length < 1)
					{
						StringBuilder
							.Append(type.DataType)
							.Append("(5461)");
						return;
					}
					break;
			}

			base.BuildDataTypeFromDataType(type, forCreateTable);
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

		protected override void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			if (predicate.Expr2 is SqlValue)
			{
				var value = ((SqlValue)predicate.Expr2).Value;

				if (value != null)
				{
					var text  = ((SqlValue)predicate.Expr2).Value.ToString();
					var ntext = predicate.IsSqlLike ? text :  DataTools.EscapeUnterminatedBracket(text);

					if (text != ntext)
						predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, new SqlValue(ntext), predicate.Escape, predicate.IsSqlLike);
				}
			}
			else if (predicate.Expr2 is SqlParameter)
			{
				var p = ((SqlParameter)predicate.Expr2);
				p.ReplaceLike = true;
			}

			base.BuildLikePredicate(predicate);
		}

		protected override void BuildUpdateTableName(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			if (updateClause.Table != null && updateClause.Table != selectQuery.From.Tables[0].Source)
				BuildPhysicalTable(updateClause.Table, null);
			else
				BuildTableName(selectQuery.From.Tables[0], true, false);
		}

		public override string Convert(string value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					value = "@" + value;

					if (value.Length > 27)
						value = value.Substring(0, 27);

					return value;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					if (value.Length > 28 || value.Length > 0 && value[0] == '[')
						return value;

					// https://github.com/linq2db/linq2db/issues/1064
					if (convertType == ConvertType.NameToQueryField && Name.Length > 0 && value[0] == '#')
						return value;

					return "[" + value + "]";

				case ConvertType.NameToDatabase:
				case ConvertType.NameToSchema:
				case ConvertType.NameToQueryTable:
					if (value.Length > 28 || value.Length > 0 && (value[0] == '[' || value[0] == '#'))
						return value;

					if (value.IndexOf('.') > 0)
						value = string.Join("].[", value.Split('.'));

					return "[" + value + "]";

				case ConvertType.SprocParameterToName:
					return value.Length > 0 && value[0] == '@'? value.Substring(1): value;
			}

			return value;
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
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		protected override string? GetProviderTypeName(IDbDataParameter parameter)
		{
			if (_provider != null)
			{
				var param = _provider.TryGetProviderParameter(parameter, MappingSchema);
				if (param != null)
					return _provider.Adapter.GetDbType(param).ToString();
			}

			return base.GetProviderTypeName(parameter);
		}

		protected override void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			StringBuilder.Append("TRUNCATE TABLE ");
		}

		public override int CommandCount(SqlStatement statement)
		{
			if (statement is SqlTruncateTableStatement trun)
				return trun.ResetIdentity && trun.Table.Fields.Values.Any(f => f.IsIdentity) ? 2 : 1;

			return 1;
		}

		protected override void BuildCommand(SqlStatement statement, int commandNumber)
		{
			if (statement is SqlTruncateTableStatement trun)
			{
				StringBuilder.Append("sp_chgattribute ");
				ConvertTableName(StringBuilder, trun.Table.Server, trun.Table.Database, trun.Table.Schema, trun.Table.PhysicalName);
				StringBuilder.AppendLine(", 'identity_burn_max', 0, '0'");
			}
		}

		protected void BuildIdentityInsert(SqlTableSource table, bool enable)
		{
			StringBuilder.Append($"SET IDENTITY_INSERT ");
			BuildTableName(table, true, false);
			StringBuilder.AppendLine(enable ? " ON" : " OFF");
		}
	}
}
