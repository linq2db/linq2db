using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	abstract class SqlServerSqlBuilder : BasicSqlBuilder
	{
		protected SqlServerSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
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

		protected override void BuildOutputSubclause()
		{
			if (SelectQuery.Insert.WithIdentity)
			{
				var identityField = SelectQuery.Insert.Into.GetIdentityField();

				if (identityField != null && (identityField.DataType == DataType.Guid || SqlServerConfiguration.GenerateScopeIdentity == false))
				{
					StringBuilder
						.Append("OUTPUT [INSERTED].[")
						.Append(identityField.PhysicalName)
						.Append("]")
						.AppendLine();
				}
			}
		}

		protected override void BuildGetIdentity()
		{
			if (SqlServerConfiguration.GenerateScopeIdentity)
			{
				var identityField = SelectQuery.Insert.Into.GetIdentityField();

				if (identityField == null || identityField.DataType != DataType.Guid)
				{
					StringBuilder
						.AppendLine()
						.AppendLine("SELECT SCOPE_IDENTITY()");
				}
			}
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

		protected override void BuildLikePredicate(SelectQuery.Predicate.Like predicate)
		{
			if (predicate.Expr2 is SqlValue)
			{
				var value = ((SqlValue)predicate.Expr2).Value;

				if (value != null)
				{
					var text  = ((SqlValue)predicate.Expr2).Value.ToString();
					var ntext = text.Replace("[", "[[]");

					if (text != ntext)
						predicate = new SelectQuery.Predicate.Like(predicate.Expr1, predicate.IsNot, new SqlValue(ntext), predicate.Escape);
				}
			}
			else if (predicate.Expr2 is SqlParameter)
			{
				var p = ((SqlParameter)predicate.Expr2);
				p.ReplaceLike = true;
			}

			base.BuildLikePredicate(predicate);
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

//						if (name.IndexOf('.') > 0)
//							value = string.Join("].[", name.Split('.'));

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

		protected override void BuildCreateTableIdentityAttribute2(SqlField field)
		{
			StringBuilder.Append("IDENTITY");
		}

		protected override void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();

			if (!pkName.StartsWith("[PK_#"))
				StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(' ');

			StringBuilder.Append("PRIMARY KEY CLUSTERED (");
			StringBuilder.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		protected override void BuildDropTableStatement()
		{
			var table = SelectQuery.CreateTable.Table;

			if (table.PhysicalName.StartsWith("#"))
			{
				AppendIndent().Append("DROP TABLE ");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine();
			}
			else
			{
				StringBuilder.Append("IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine("') AND type in (N'U'))");

				AppendIndent().Append("BEGIN DROP TABLE ");
				BuildPhysicalTable(table, null);
				StringBuilder.AppendLine(" END");
			}
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Guid      : StringBuilder.Append("UniqueIdentifier"); return;
				case DataType.Variant   : StringBuilder.Append("Sql_Variant");      return;
				case DataType.NVarChar  :
				case DataType.VarChar   :
				case DataType.VarBinary :

					if (type.Length == int.MaxValue || type.Length < 0)
					{
						StringBuilder
							.Append(type.DataType)
							.Append("(Max)");
						return;
					}

					break;
			}

			base.BuildDataType(type, createDbType);
		}

#if !NETFX_CORE && !SILVERLIGHT

#if !MONO
		protected override string GetTypeName(IDbDataParameter parameter)
		{
			return ((System.Data.SqlClient.SqlParameter)parameter).TypeName;
		}
#endif

		protected override string GetUdtTypeName(IDbDataParameter parameter)
		{
			return ((System.Data.SqlClient.SqlParameter)parameter).UdtTypeName;
		}

		protected override string GetProviderTypeName(IDbDataParameter parameter)
		{
			return ((System.Data.SqlClient.SqlParameter)parameter).SqlDbType.ToString();
		}

#endif
	}
}
