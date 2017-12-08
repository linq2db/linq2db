using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.Sybase
{
	using SqlQuery;
	using SqlProvider;

	class SybaseSqlBuilder : BasicSqlBuilder
	{
		public SybaseSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
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

		SybaseSqlBuilder(bool skipAliases, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
			_skipAliases = skipAliases;
		}

		protected override void BuildSelectClause(SelectQuery selectQuery)
		{
			_isSelect = true;
			base.BuildSelectClause(selectQuery);
			_isSelect = false;
		}

		protected override void BuildColumnExpression(SelectQuery selectQuery, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SqlSearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SqlSearchCondition;
				}
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(selectQuery, expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");

			if (_skipAliases) addAlias = false;
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SybaseSqlBuilder(_isSelect, SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType)
		{
			switch (type.DataType)
			{
				case DataType.DateTime2 : StringBuilder.Append("DateTime");       break;
				default                 : base.BuildDataType(type, createDbType); break;
			}
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
					var ntext = text.Replace("[", "[[]");

					if (text != ntext)
						predicate = new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, new SqlValue(ntext), predicate.Escape);
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

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					{
						var name = "@" + value;

						if (name.Length > 27)
							name = name.Substring(0, 27);

						return name;
					}

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();

						if (name.Length > 28 || name.Length > 0 && name[0] == '[')
							return value;
					}

					return "[" + value + "]";

				case ConvertType.NameToDatabase:
				case ConvertType.NameToOwner:
				case ConvertType.NameToQueryTable:
					if (value != null)
					{
						var name = value.ToString();

						if (name.Length > 28 || name.Length > 0 && (name[0] == '[' || name[0] == '#'))
							return value;

						if (name.IndexOf('.') > 0)
							value = string.Join("].[", name.Split('.'));

						return "[" + value + "]";
					}

					break;

				case ConvertType.SprocParameterToName:
					if (value != null)
					{
						var str = value.ToString();
						return str.Length > 0 && str[0] == '@'? str.Substring(1): str;
					}

					break;
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

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			dynamic p = parameter;
			return p.AseDbType.ToString();
		}
	}
}
