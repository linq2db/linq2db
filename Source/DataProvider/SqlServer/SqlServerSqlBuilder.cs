﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using SqlQuery;
	using SqlProvider;

	abstract class SqlServerSqlBuilder : BasicSqlBuilder
	{
		protected SqlServerSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		protected virtual  bool BuildAlternativeSql { get { return true; } }

		protected override string FirstFormat
		{
			get { return SelectQuery.Select.SkipValue == null ? "TOP ({0})" : null; }
		}

		protected override void BuildSql()
		{
			if (BuildAlternativeSql)
				AlternativeBuildSql(true, base.BuildSql);
			else
				base.BuildSql();
		}

		protected override void BuildGetIdentity()
		{
			StringBuilder
				.AppendLine()
				.AppendLine("SELECT SCOPE_IDENTITY()");
		}

		protected override void BuildOrderByClause()
		{
			if (!BuildAlternativeSql || !NeedSkip)
				base.BuildOrderByClause();
		}

		protected override IEnumerable<SelectQuery.Column> GetSelectedColumns()
		{
			if (BuildAlternativeSql && NeedSkip && !SelectQuery.OrderBy.IsEmpty)
				return AlternativeGetSelectedColumns(base.GetSelectedColumns);
			return base.GetSelectedColumns();
		}

		protected override void BuildDeleteClause()
		{
			var table = SelectQuery.Delete.Table != null ?
				(SelectQuery.From.FindTableSource(SelectQuery.Delete.Table) ?? SelectQuery.Delete.Table) :
				SelectQuery.From.Tables[0];

			AppendIndent()
				.Append("DELETE");

			BuildSkipFirst();

			StringBuilder
				.Append(" ")
				.Append(Convert(GetTableAlias(table), ConvertType.NameToQueryTableAlias))
				.AppendLine();
		}

		protected override void BuildUpdateTableName()
		{
			var table = SelectQuery.Update.Table != null ?
				(SelectQuery.From.FindTableSource(SelectQuery.Update.Table) ?? SelectQuery.Update.Table) :
				SelectQuery.From.Tables[0];

			if (table is SqlTable)
				BuildPhysicalTable(table, null);
			else
				StringBuilder.Append(Convert(GetTableAlias(table), ConvertType.NameToQueryTableAlias));
		}

		protected override void BuildString(string value)
		{
			foreach (var ch in value)
			{
				if (ch > 127)
				{
					StringBuilder.Append("N");
					break;
				}
			}

			base.BuildString(value);
		}

		protected override void BuildChar(char value)
		{
			if (value > 127)
				StringBuilder.Append("N");

			base.BuildChar(value);
		}

		protected override void BuildColumnExpression(ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = false;

			if (expr.SystemType == typeof(bool))
			{
				if (expr is SelectQuery.SearchCondition)
					wrap = true;
				else
				{
					var ex = expr as SqlExpression;
					wrap = ex != null && ex.Expr == "{0}" && ex.Parameters.Length == 1 && ex.Parameters[0] is SelectQuery.SearchCondition;
				}
			}

			if (wrap) StringBuilder.Append("CASE WHEN ");
			base.BuildColumnExpression(expr, alias, ref addAlias);
			if (wrap) StringBuilder.Append(" THEN 1 ELSE 0 END");
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.NameToQueryParameter:
				case ConvertType.NameToCommandParameter:
				case ConvertType.NameToSprocParameter:
					return "@" + value;

				case ConvertType.NameToQueryField:
				case ConvertType.NameToQueryFieldAlias:
				case ConvertType.NameToQueryTableAlias:
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
							return value;
					}

					return "[" + value + "]";

				case ConvertType.NameToDatabase:
				case ConvertType.NameToOwner:
				case ConvertType.NameToQueryTable:
					if (value != null)
					{
						var name = value.ToString();

						if (name.Length > 0 && name[0] == '[')
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

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertOrUpdateQueryAsUpdateInsert();
		}

		protected override void BuildDateTime(object value)
		{
			StringBuilder.Append("'{0:yyyy-MM-ddTHH:mm:ss.fff}'".Args(value));
		}

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}
	}
}
