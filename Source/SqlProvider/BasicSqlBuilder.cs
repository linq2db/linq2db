using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

#if !SILVERLIGHT && !NETFX_CORE
using System.Data.SqlTypes;
#endif

namespace LinqToDB.SqlProvider
{
	using Common;
	using Mapping;
	using SqlQuery;

	public abstract class BasicSqlBuilder : ISqlBuilder
	{
		#region Init

		protected BasicSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
		{
			SqlOptimizer = sqlOptimizer;
			SqlProviderFlags = sqlProviderFlags;
			ValueToSqlConverter = valueToSqlConverter;
		}

		protected SelectQuery SelectQuery;
		protected int Indent;
		protected Step BuildStep;
		protected ISqlOptimizer SqlOptimizer;
		protected SqlProviderFlags SqlProviderFlags;
		protected ValueToSqlConverter ValueToSqlConverter;
		protected StringBuilder StringBuilder;
		protected bool SkipAlias;

		#endregion

		#region Support Flags

		public virtual bool IsNestedJoinSupported { get { return true; } }
		public virtual bool IsNestedJoinParenthesisRequired { get { return false; } }

		#endregion

		#region CommandCount

		public virtual int CommandCount(SelectQuery selectQuery)
		{
			return 1;
		}

		#endregion

		#region BuildSql

		public void BuildSql(int commandNumber, SelectQuery selectQuery, StringBuilder sb)
		{
			BuildSql(commandNumber, selectQuery, sb, 0, false);
		}

		protected virtual void BuildSql(int commandNumber, SelectQuery selectQuery, StringBuilder sb, int indent, bool skipAlias)
		{
			SelectQuery = selectQuery;
			StringBuilder = sb;
			Indent = indent;
			SkipAlias = skipAlias;

			if (commandNumber == 0)
			{
				BuildSql();

				if (selectQuery.HasUnion)
				{
					foreach (var union in selectQuery.Unions)
					{
						AppendIndent();
						sb.Append("UNION");
						if (union.IsAll) sb.Append(" ALL");
						sb.AppendLine();

						((BasicSqlBuilder)CreateSqlBuilder()).BuildSql(commandNumber, union.SelectQuery, sb, indent, skipAlias);
					}
				}
			}
			else
			{
				BuildCommand(commandNumber);
			}
		}

		protected virtual void BuildCommand(int commandNumber)
		{
		}

		#endregion

		#region Overrides

		protected virtual void BuildSqlBuilder(SelectQuery selectQuery, int indent, bool skipAlias)
		{
			if (!SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery)
				&& selectQuery.Select.SkipValue != null)
				throw new SqlException("Skip for subqueries is not supported by the '{0}' provider.", Name);

			if (!SqlProviderFlags.IsTakeSupported && selectQuery.Select.TakeValue != null)
				throw new SqlException("Take for subqueries is not supported by the '{0}' provider.", Name);

			((BasicSqlBuilder)CreateSqlBuilder()).BuildSql(0, selectQuery, StringBuilder, indent, skipAlias);
		}

		protected abstract ISqlBuilder CreateSqlBuilder();

		protected T WithStringBuilder<T>(StringBuilder sb, Func<T> func)
		{
			var current = StringBuilder;

			StringBuilder = sb;

			var ret = func();

			StringBuilder = current;

			return ret;
		}

		void WithStringBuilder(StringBuilder sb, Action func)
		{
			var current = StringBuilder;

			StringBuilder = sb;

			func();

			StringBuilder = current;
		}

		protected virtual bool ParenthesizeJoin()
		{
			return false;
		}

		protected virtual void BuildSql()
		{
			switch (SelectQuery.QueryType)
			{
				case QueryType.Select: BuildSelectQuery(); break;
				case QueryType.Delete: BuildDeleteQuery(); break;
				case QueryType.Update: BuildUpdateQuery(); break;
				case QueryType.Insert: BuildInsertQuery(); break;
				case QueryType.InsertOrUpdate: BuildInsertOrUpdateQuery(); break;
				case QueryType.CreateTable:
					if (SelectQuery.CreateTable.IsDrop)
						BuildDropTableStatement();
					else
						BuildCreateTableStatement();
					break;
				default: BuildUnknownQuery(); break;
			}
		}

		protected virtual void BuildDeleteQuery()
		{
			BuildStep = Step.DeleteClause; BuildDeleteClause();
			BuildStep = Step.FromClause; BuildFromClause();
			BuildStep = Step.WhereClause; BuildWhereClause();
			BuildStep = Step.GroupByClause; BuildGroupByClause();
			BuildStep = Step.HavingClause; BuildHavingClause();
			BuildStep = Step.OrderByClause; BuildOrderByClause();
			BuildStep = Step.OffsetLimit; BuildOffsetLimit();
		}

		protected virtual void BuildUpdateQuery()
		{
			BuildStep = Step.UpdateClause; BuildUpdateClause();
			BuildStep = Step.FromClause; BuildFromClause();
			BuildStep = Step.WhereClause; BuildWhereClause();
			BuildStep = Step.GroupByClause; BuildGroupByClause();
			BuildStep = Step.HavingClause; BuildHavingClause();
			BuildStep = Step.OrderByClause; BuildOrderByClause();
			BuildStep = Step.OffsetLimit; BuildOffsetLimit();
		}

		protected virtual void BuildSelectQuery()
		{
			BuildStep = Step.SelectClause; BuildSelectClause();
			BuildStep = Step.FromClause; BuildFromClause();
			BuildStep = Step.WhereClause; BuildWhereClause();
			BuildStep = Step.GroupByClause; BuildGroupByClause();
			BuildStep = Step.HavingClause; BuildHavingClause();
			BuildStep = Step.OrderByClause; BuildOrderByClause();
			BuildStep = Step.OffsetLimit; BuildOffsetLimit();
		}

		protected virtual void BuildInsertQuery()
		{
			BuildStep = Step.InsertClause; BuildInsertClause();

			if (SelectQuery.QueryType == QueryType.Insert && SelectQuery.From.Tables.Count != 0)
			{
				BuildStep = Step.SelectClause; BuildSelectClause();
				BuildStep = Step.FromClause; BuildFromClause();
				BuildStep = Step.WhereClause; BuildWhereClause();
				BuildStep = Step.GroupByClause; BuildGroupByClause();
				BuildStep = Step.HavingClause; BuildHavingClause();
				BuildStep = Step.OrderByClause; BuildOrderByClause();
				BuildStep = Step.OffsetLimit; BuildOffsetLimit();
			}

			if (SelectQuery.Insert.WithIdentity)
				BuildGetIdentity();
		}

		protected virtual void BuildUnknownQuery()
		{
			throw new SqlException("Unknown query type '{0}'.", SelectQuery.QueryType);
		}

		public virtual StringBuilder ConvertTableName(StringBuilder sb, string database, string owner, string table)
		{
			if (database != null) database = Convert(database, ConvertType.NameToDatabase).ToString();
			if (owner != null) owner = Convert(owner, ConvertType.NameToOwner).ToString();
			if (table != null) table = Convert(table, ConvertType.NameToQueryTable).ToString();

			return BuildTableName(sb, database, owner, table);
		}

		public virtual StringBuilder BuildTableName(StringBuilder sb, string database, string owner, string table)
		{
			if (database != null)
			{
				if (owner == null) sb.Append(database).Append("..");
				else sb.Append(database).Append(".").Append(owner).Append(".");
			}
			else if (owner != null) sb.Append(owner).Append(".");

			return sb.Append(table);
		}

		public virtual object Convert(object value, ConvertType convertType)
		{
			return value;
		}

		#endregion

		#region Build Select

		protected virtual void BuildSelectClause()
		{
			AppendIndent();
			StringBuilder.Append("SELECT");

			if (SelectQuery.Select.IsDistinct)
				StringBuilder.Append(" DISTINCT");

			BuildSkipFirst();

			StringBuilder.AppendLine();
			BuildColumns();
		}

		protected virtual IEnumerable<SelectQuery.Column> GetSelectedColumns()
		{
			return SelectQuery.Select.Columns;
		}

		protected virtual void BuildColumns()
		{
			Indent++;

			var first = true;

			foreach (var col in GetSelectedColumns())
			{
				if (!first)
					StringBuilder.Append(',').AppendLine();
				first = false;

				var addAlias = true;

				AppendIndent();
				BuildColumnExpression(col.Expression, col.Alias, ref addAlias);

				if (!SkipAlias && addAlias && col.Alias != null)
					StringBuilder.Append(" as ").Append(Convert(col.Alias, ConvertType.NameToQueryFieldAlias));
			}

			if (first)
				AppendIndent().Append("*");

			Indent--;

			StringBuilder.AppendLine();
		}

		protected virtual void BuildColumnExpression(ISqlExpression expr, string alias, ref bool addAlias)
		{
			BuildExpression(expr, true, true, alias, ref addAlias, true);
		}

		#endregion

		#region Build Delete

		protected virtual void BuildDeleteClause()
		{
			AppendIndent();
			StringBuilder.Append("DELETE");
			BuildSkipFirst();
			StringBuilder.Append(" ");
		}

		#endregion

		#region Build Update

		protected virtual void BuildUpdateClause()
		{
			BuildUpdateTable();
			BuildUpdateSet();
		}

		protected virtual void BuildUpdateTable()
		{
			AppendIndent().Append("UPDATE");

			BuildSkipFirst();

			StringBuilder.AppendLine().Append('\t');
			BuildUpdateTableName();
			StringBuilder.AppendLine();
		}

		protected virtual void BuildUpdateTableName()
		{
			if (SelectQuery.Update.Table != null && SelectQuery.Update.Table != SelectQuery.From.Tables[0].Source)
			{
				BuildPhysicalTable(SelectQuery.Update.Table, null);
			}
			else
			{
				if (SelectQuery.From.Tables[0].Source is SelectQuery)
					StringBuilder.Length--;

				BuildTableName(SelectQuery.From.Tables[0], true, true);
			}
		}

		protected virtual void BuildUpdateSet()
		{
			AppendIndent()
				.AppendLine("SET");

			Indent++;

			var first = true;

			foreach (var expr in SelectQuery.Update.Items)
			{
				if (!first)
					StringBuilder.Append(',').AppendLine();
				first = false;

				AppendIndent();

				BuildExpression(expr.Column, SqlProviderFlags.IsUpdateSetTableAliasSupported, true, false);
				StringBuilder.Append(" = ");

				var addAlias = false;

				BuildColumnExpression(expr.Expression, null, ref addAlias);
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region Build Insert

		protected void BuildInsertClause()
		{
			BuildInsertClause("INSERT INTO ", true);
		}

		protected virtual void BuildEmptyInsert()
		{
			StringBuilder.AppendLine("DEFAULT VALUES");
		}

		protected virtual void BuildOutputSubclause()
		{
		}

		protected virtual void BuildInsertClause(string insertText, bool appendTableName)
		{
			AppendIndent().Append(insertText);

			if (appendTableName)
				BuildPhysicalTable(SelectQuery.Insert.Into, null);

			if (SelectQuery.Insert.Items.Count == 0)
			{
				StringBuilder.Append(' ');

				BuildOutputSubclause();

				BuildEmptyInsert();
			}
			else
			{
				StringBuilder.AppendLine();

				AppendIndent().AppendLine("(");

				Indent++;

				var first = true;

				foreach (var expr in SelectQuery.Insert.Items)
				{
					if (!first)
						StringBuilder.Append(',').AppendLine();
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
				}

				Indent--;

				StringBuilder.AppendLine();
				AppendIndent().AppendLine(")");

				BuildOutputSubclause();

				if (SelectQuery.QueryType == QueryType.InsertOrUpdate || SelectQuery.From.Tables.Count == 0)
				{
					AppendIndent().AppendLine("VALUES");
					AppendIndent().AppendLine("(");

					Indent++;

					first = true;

					foreach (var expr in SelectQuery.Insert.Items)
					{
						if (!first)
							StringBuilder.Append(',').AppendLine();
						first = false;

						AppendIndent();
						BuildExpression(expr.Expression);
					}

					Indent--;

					StringBuilder.AppendLine();
					AppendIndent().AppendLine(")");
				}
			}
		}

		protected virtual void BuildGetIdentity()
		{
			//throw new SqlException("Insert with identity is not supported by the '{0}' sql provider.", Name);
		}

		#endregion

		#region Build InsertOrUpdate

		protected virtual void BuildInsertOrUpdateQuery()
		{
			throw new SqlException("InsertOrUpdate query type is not supported by {0} provider.", Name);
		}


		protected virtual void BuildInsertOrUpdateQueryAsMerge(string fromDummyTable)
		{
			var table = SelectQuery.Insert.Into;
			var targetAlias = Convert(SelectQuery.From.Tables[0].Alias, ConvertType.NameToQueryTableAlias).ToString();
			var sourceAlias = Convert(GetTempAliases(1, "s")[0], ConvertType.NameToQueryTableAlias).ToString();
			var keys = SelectQuery.Update.Keys;

			AppendIndent().Append("MERGE INTO ");
			BuildPhysicalTable(table, null);
			StringBuilder.Append(' ').AppendLine(targetAlias);

			AppendIndent().Append("USING (SELECT ");

			for (var i = 0; i < keys.Count; i++)
			{
				BuildExpression(keys[i].Expression, false, false);
				StringBuilder.Append(" AS ");
				BuildExpression(keys[i].Column, false, false);

				if (i + 1 < keys.Count)
					StringBuilder.Append(", ");
			}

			if (!string.IsNullOrEmpty(fromDummyTable))
				StringBuilder.Append(' ').Append(fromDummyTable);

			StringBuilder.Append(") ").Append(sourceAlias).AppendLine(" ON");

			AppendIndent().AppendLine("(");

			Indent++;

			for (var i = 0; i < keys.Count; i++)
			{
				var key = keys[i];

				AppendIndent();

				StringBuilder.Append(targetAlias).Append('.');
				BuildExpression(key.Column, false, false);

				StringBuilder.Append(" = ").Append(sourceAlias).Append('.');
				BuildExpression(key.Column, false, false);

				if (i + 1 < keys.Count)
					StringBuilder.Append(" AND");

				StringBuilder.AppendLine();
			}

			Indent--;

			AppendIndent().AppendLine(")");
			AppendIndent().AppendLine("WHEN MATCHED THEN");

			Indent++;
			AppendIndent().AppendLine("UPDATE ");
			BuildUpdateSet();
			Indent--;

			AppendIndent().AppendLine("WHEN NOT MATCHED THEN");

			Indent++;
			BuildInsertClause("INSERT", false);
			Indent--;

			while (_endLine.Contains(StringBuilder[StringBuilder.Length - 1]))
				StringBuilder.Length--;
		}

		protected static readonly char[] _endLine = { ' ', '\r', '\n' };

		protected void BuildInsertOrUpdateQueryAsUpdateInsert()
		{
			AppendIndent().AppendLine("BEGIN TRAN").AppendLine();

			BuildUpdateQuery();

			AppendIndent().AppendLine("WHERE");

			var alias = Convert(SelectQuery.From.Tables[0].Alias, ConvertType.NameToQueryTableAlias).ToString();
			var exprs = SelectQuery.Update.Keys;

			Indent++;

			for (var i = 0; i < exprs.Count; i++)
			{
				var expr = exprs[i];

				AppendIndent();

				StringBuilder.Append(alias).Append('.');
				BuildExpression(expr.Column, false, false);

				StringBuilder.Append(" = ");
				BuildExpression(Precedence.Comparison, expr.Expression);

				if (i + 1 < exprs.Count)
					StringBuilder.Append(" AND");

				StringBuilder.AppendLine();
			}

			Indent--;

			StringBuilder.AppendLine();
			AppendIndent().AppendLine("IF @@ROWCOUNT = 0");
			AppendIndent().AppendLine("BEGIN");

			Indent++;

			BuildInsertQuery();

			Indent--;

			AppendIndent().AppendLine("END");

			StringBuilder.AppendLine();
			AppendIndent().AppendLine("COMMIT");
		}

		#endregion

		#region Build DDL

		protected virtual void BuildDropTableStatement()
		{
			var table = SelectQuery.CreateTable.Table;

			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(table, null);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildStartCreateTableStatement(SelectQuery.CreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null)
			{
				AppendIndent().Append("CREATE TABLE ");
				BuildPhysicalTable(createTable.Table, null);
			}
			else
			{
				var name = WithStringBuilder(
					new StringBuilder(),
					() =>
					{
						BuildPhysicalTable(createTable.Table, null);
						return StringBuilder.ToString();
					});

				AppendIndent().AppendFormat(createTable.StatementHeader, name);
			}
		}

		protected virtual void BuildEndCreateTableStatement(SelectQuery.CreateTableStatement createTable)
		{
			if (createTable.StatementFooter != null)
				AppendIndent().Append(createTable.StatementFooter);
		}

		class CreateFieldInfo
		{
			public SqlField Field;
			public StringBuilder StringBuilder;
			public string Name;
			public string Type;
			public string Identity;
			public string Null;
		}

		protected virtual void BuildCreateTableStatement()
		{
			var table = SelectQuery.CreateTable.Table;

			BuildStartCreateTableStatement(SelectQuery.CreateTable);

			StringBuilder.AppendLine();
			AppendIndent().Append("(");
			Indent++;

			var fields = table.Fields.Select(f => new CreateFieldInfo { Field = f.Value, StringBuilder = new StringBuilder() }).ToList();
			var maxlen = 0;

			Action<bool> appendToMax = addCreateFormat =>
			{
				foreach (var field in fields)
					if (addCreateFormat || field.Field.CreateFormat == null)
						while (maxlen > field.StringBuilder.Length)
							field.StringBuilder.Append(' ');
			};

			var isAnyCreateFormat = false;

			// Build field name.
			//
			foreach (var field in fields)
			{
				field.StringBuilder.Append(Convert(field.Field.PhysicalName, ConvertType.NameToQueryField));

				if (maxlen < field.StringBuilder.Length)
					maxlen = field.StringBuilder.Length;

				if (field.Field.CreateFormat != null)
					isAnyCreateFormat = true;
			}

			appendToMax(true);

			if (isAnyCreateFormat)
				foreach (var field in fields)
					if (field.Field.CreateFormat != null)
						field.Name = field.StringBuilder.ToString() + ' ';

			// Build field type.
			//
			foreach (var field in fields)
			{
				field.StringBuilder.Append(' ');

				if (!string.IsNullOrEmpty(field.Field.DbType))
					field.StringBuilder.Append(field.Field.DbType);
				else
				{
					var sb = StringBuilder;
					StringBuilder = field.StringBuilder;

					BuildCreateTableFieldType(field.Field);

					StringBuilder = sb;
				}

				if (maxlen < field.StringBuilder.Length)
					maxlen = field.StringBuilder.Length;
			}

			appendToMax(true);

			if (isAnyCreateFormat)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat != null)
					{
						var sb = field.StringBuilder;

						field.Type = sb.ToString().Substring(field.Name.Length) + ' ';
						sb.Length = 0;
					}
				}
			}

			var hasIdentity = fields.Any(f => f.Field.IsIdentity);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat == null)
						field.StringBuilder.Append(' ');

					if (field.Field.IsIdentity)
						WithStringBuilder(field.StringBuilder, () => BuildCreateTableIdentityAttribute1(field.Field));

					if (field.Field.CreateFormat != null)
					{
						field.Identity = field.StringBuilder.ToString();

						if (field.Identity.Length != 0)
							field.Identity += ' ';

						field.StringBuilder.Length = 0;
					}
					else if (maxlen < field.StringBuilder.Length)
					{
						maxlen = field.StringBuilder.Length;
					}
				}

				appendToMax(false);
			}

			// Build nullable attribute.
			//
			foreach (var field in fields)
			{
				if (field.Field.CreateFormat == null)
					field.StringBuilder.Append(' ');

				WithStringBuilder(
					field.StringBuilder,
					() => BuildCreateTableNullAttribute(field.Field, SelectQuery.CreateTable.DefaulNullable));

				if (field.Field.CreateFormat != null)
				{
					field.Null = field.StringBuilder.ToString() + ' ';
					field.StringBuilder.Length = 0;
				}
				else if (maxlen < field.StringBuilder.Length)
				{
					maxlen = field.StringBuilder.Length;
				}
			}

			appendToMax(false);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat == null)
						field.StringBuilder.Append(' ');

					if (field.Field.IsIdentity)
						WithStringBuilder(field.StringBuilder, () => BuildCreateTableIdentityAttribute2(field.Field));

					if (field.Field.CreateFormat != null)
					{
						if (field.Field.CreateFormat != null && field.Identity.Length == 0)
						{
							field.Identity = field.StringBuilder.ToString() + ' ';
							field.StringBuilder.Length = 0;
						}
					}
					else if (maxlen < field.StringBuilder.Length)
					{
						maxlen = field.StringBuilder.Length;
					}
				}

				appendToMax(false);
			}

			// Build fields.
			//
			for (var i = 0; i < fields.Count; i++)
			{
				while (fields[i].StringBuilder.Length > 0 && fields[i].StringBuilder[fields[i].StringBuilder.Length - 1] == ' ')
					fields[i].StringBuilder.Length--;

				StringBuilder.AppendLine(i == 0 ? "" : ",");
				AppendIndent();

				var field = fields[i];

				if (field.Field.CreateFormat != null)
				{
					StringBuilder.AppendFormat(field.Field.CreateFormat, field.Name, field.Type, field.Null, field.Identity);

					while (StringBuilder.Length > 0 && StringBuilder[StringBuilder.Length - 1] == ' ')
						StringBuilder.Length--;
				}
				else
				{
					StringBuilder.Append(field.StringBuilder);
				}
			}

			var pk =
			(
				from f in fields
				where f.Field.IsPrimaryKey
				orderby f.Field.PrimaryKeyOrder
				select f
			).ToList();

			if (pk.Count > 0)
			{
				StringBuilder.AppendLine(",").AppendLine();

				BuildCreateTablePrimaryKey(
					Convert("PK_" + SelectQuery.CreateTable.Table.PhysicalName, ConvertType.NameToQueryTable).ToString(),
					pk.Select(f => Convert(f.Field.PhysicalName, ConvertType.NameToQueryField).ToString()));
			}

			Indent--;
			StringBuilder.AppendLine();
			AppendIndent().AppendLine(")");

			BuildEndCreateTableStatement(SelectQuery.CreateTable);
		}

		protected virtual void BuildCreateTableFieldType(SqlField field)
		{
			BuildDataType(new SqlDataType(
				field.DataType,
				field.SystemType,
				field.Length,
				field.Precision,
				field.Scale),
				createDbType: true);
		}

		protected virtual void BuildCreateTableNullAttribute(SqlField field, DefaulNullable defaulNullable)
		{
			if (defaulNullable == DefaulNullable.Null && field.CanBeNull)
				return;

			if (defaulNullable == DefaulNullable.NotNull && !field.CanBeNull)
				return;

			StringBuilder.Append(field.CanBeNull ? "    NULL" : "NOT NULL");
		}

		protected virtual void BuildCreateTableIdentityAttribute1(SqlField field)
		{
		}

		protected virtual void BuildCreateTableIdentityAttribute2(SqlField field)
		{
		}

		protected virtual void BuildCreateTablePrimaryKey(string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			StringBuilder.Append(fieldNames.Aggregate((f1, f2) => f1 + ", " + f2));
			StringBuilder.Append(")");
		}

		#endregion

		#region Build From

		protected virtual void BuildFromClause()
		{
			if (SelectQuery.From.Tables.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("FROM").AppendLine();

			Indent++;
			AppendIndent();

			var first = true;

			foreach (var ts in SelectQuery.From.Tables)
			{
				if (!first)
				{
					StringBuilder.AppendLine(",");
					AppendIndent();
				}

				first = false;

				var jn = ParenthesizeJoin() ? ts.GetJoinNumber() : 0;

				if (jn > 0)
				{
					jn--;
					for (var i = 0; i < jn; i++)
						StringBuilder.Append("(");
				}

				BuildTableName(ts, true, true);

				foreach (var jt in ts.Joins)
					BuildJoinTable(jt, ref jn);
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		protected void BuildPhysicalTable(ISqlTableSource table, string alias)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable:
				case QueryElementType.TableSource:
					StringBuilder.Append(GetPhysicalTableName(table, alias));
					break;

				case QueryElementType.SqlQuery:
					StringBuilder.Append("(").AppendLine();
					BuildSqlBuilder((SelectQuery)table, Indent + 1, false);
					AppendIndent().Append(")");

					break;

				default:
					throw new InvalidOperationException();
			}
		}

		protected void BuildTableName(SelectQuery.TableSource ts, bool buildName, bool buildAlias)
		{
			if (buildName)
			{
				var alias = GetTableAlias(ts);
				BuildPhysicalTable(ts.Source, alias);
			}

			if (buildAlias)
			{
				if (ts.SqlTableType != SqlTableType.Expression)
				{
					var alias = GetTableAlias(ts);

					if (!string.IsNullOrEmpty(alias))
					{
						if (buildName)
							StringBuilder.Append(" ");
						StringBuilder.Append(Convert(alias, ConvertType.NameToQueryTableAlias));
					}
				}
			}
		}

		void BuildJoinTable(SelectQuery.JoinedTable join, ref int joinCounter)
		{
			StringBuilder.AppendLine();
			Indent++;
			AppendIndent();

			var buildOn = BuildJoinType(join);

			if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
				StringBuilder.Append('(');

			BuildTableName(join.Table, true, true);

			if (IsNestedJoinSupported && join.Table.Joins.Count != 0)
			{
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(jt, ref joinCounter);

				if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
					StringBuilder.Append(')');

				if (buildOn)
				{
					StringBuilder.AppendLine();
					AppendIndent();
					StringBuilder.Append("ON ");
				}
			}
			else if (buildOn)
				StringBuilder.Append(" ON ");

			if (buildOn)
			{
				if (join.Condition.Conditions.Count != 0)
					BuildSearchCondition(Precedence.Unknown, join.Condition);
				else
					StringBuilder.Append("1=1");
			}

			if (joinCounter > 0)
			{
				joinCounter--;
				StringBuilder.Append(")");
			}

			if (!IsNestedJoinSupported)
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(jt, ref joinCounter);

			Indent--;
		}

		protected virtual bool BuildJoinType(SelectQuery.JoinedTable join)
		{
			switch (join.JoinType)
			{
				case SelectQuery.JoinType.Inner: StringBuilder.Append("INNER JOIN "); return true;
				case SelectQuery.JoinType.Left: StringBuilder.Append("LEFT JOIN "); return true;
				case SelectQuery.JoinType.CrossApply: StringBuilder.Append("CROSS APPLY "); return false;
				case SelectQuery.JoinType.OuterApply: StringBuilder.Append("OUTER APPLY "); return false;
				default: throw new InvalidOperationException();
			}
		}

		#endregion

		#region Where Clause

		protected virtual bool BuildWhere()
		{
			return SelectQuery.Where.SearchCondition.Conditions.Count != 0;
		}

		protected virtual void BuildWhereClause()
		{
			if (!BuildWhere())
				return;

			AppendIndent();

			StringBuilder.Append("WHERE").AppendLine();

			Indent++;
			AppendIndent();
			BuildWhereSearchCondition(SelectQuery.Where.SearchCondition);
			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region GroupBy Clause

		protected virtual void BuildGroupByClause()
		{
			if (SelectQuery.GroupBy.Items.Count == 0)
				return;

			var items = SelectQuery.GroupBy.Items.Where(i => !(i is SqlValue || i is SqlParameter)).ToList();

			if (items.Count == 0)
				return;

			//			if (SelectQuery.GroupBy.Items.Count == 1)
			//			{
			//				var item = SelectQuery.GroupBy.Items[0];
			//
			//				if (item is SqlValue || item is SqlParameter)
			//				{
			//					var value = ((SqlValue)item).Value;
			//
			//					if (value is Sql.GroupBy || value is int)
			//						return;
			//				}
			//			}

			AppendIndent();

			StringBuilder.Append("GROUP BY").AppendLine();

			Indent++;

			for (var i = 0; i < items.Count; i++)
			{
				AppendIndent();

				BuildExpression(items[i]);

				if (i + 1 < items.Count)
					StringBuilder.Append(',');

				StringBuilder.AppendLine();
			}

			Indent--;
		}

		#endregion

		#region Having Clause

		protected virtual void BuildHavingClause()
		{
			if (SelectQuery.Having.SearchCondition.Conditions.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("HAVING").AppendLine();

			Indent++;
			AppendIndent();
			BuildWhereSearchCondition(SelectQuery.Having.SearchCondition);
			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region OrderBy Clause

		protected virtual void BuildOrderByClause()
		{
			if (SelectQuery.OrderBy.Items.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("ORDER BY").AppendLine();

			Indent++;

			for (var i = 0; i < SelectQuery.OrderBy.Items.Count; i++)
			{
				AppendIndent();

				var item = SelectQuery.OrderBy.Items[i];

				BuildExpression(item.Expression);

				if (item.IsDescending)
					StringBuilder.Append(" DESC");

				if (i + 1 < SelectQuery.OrderBy.Items.Count)
					StringBuilder.Append(',');

				StringBuilder.AppendLine();
			}

			Indent--;
		}

		#endregion

		#region Skip/Take

		protected virtual bool SkipFirst { get { return true; } }
		protected virtual string SkipFormat { get { return null; } }
		protected virtual string FirstFormat { get { return null; } }
		protected virtual string LimitFormat { get { return null; } }
		protected virtual string OffsetFormat { get { return null; } }
		protected virtual bool OffsetFirst { get { return false; } }

		protected bool NeedSkip { get { return SelectQuery.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(SelectQuery); } }
		protected bool NeedTake { get { return SelectQuery.Select.TakeValue != null && SqlProviderFlags.IsTakeSupported; } }

		protected virtual void BuildSkipFirst()
		{
			if (SkipFirst && NeedSkip && SkipFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					SkipFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.SkipValue)));

			if (NeedTake && FirstFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					FirstFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.TakeValue)));

			if (!SkipFirst && NeedSkip && SkipFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					SkipFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.SkipValue)));
		}

		protected virtual void BuildOffsetLimit()
		{
			var doSkip = NeedSkip && OffsetFormat != null;
			var doTake = NeedTake && LimitFormat != null;

			if (doSkip || doTake)
			{
				AppendIndent();

				if (doSkip && OffsetFirst)
				{
					StringBuilder.AppendFormat(
						OffsetFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.SkipValue)));

					if (doTake)
						StringBuilder.Append(' ');
				}

				if (doTake)
				{
					StringBuilder.AppendFormat(
						LimitFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.TakeValue)));

					if (doSkip)
						StringBuilder.Append(' ');
				}

				if (doSkip && !OffsetFirst)
					StringBuilder.AppendFormat(
						OffsetFormat, WithStringBuilder(new StringBuilder(), () => BuildExpression(SelectQuery.Select.SkipValue)));

				StringBuilder.AppendLine();
			}
		}

		#endregion

		#region Builders

		#region BuildSearchCondition

		protected virtual void BuildWhereSearchCondition(SelectQuery.SearchCondition condition)
		{
			BuildSearchCondition(Precedence.Unknown, condition);
		}

		protected virtual void BuildSearchCondition(SelectQuery.SearchCondition condition)
		{
			var isOr = (bool?)null;
			var len = StringBuilder.Length;
			var parentPrecedence = condition.Precedence + 1;

			foreach (var cond in condition.Conditions)
			{
				if (isOr != null)
				{
					StringBuilder.Append(isOr.Value ? " OR" : " AND");

					if (condition.Conditions.Count < 4 && StringBuilder.Length - len < 50 || condition != SelectQuery.Where.SearchCondition)
					{
						StringBuilder.Append(' ');
					}
					else
					{
						StringBuilder.AppendLine();
						AppendIndent();
						len = StringBuilder.Length;
					}
				}

				if (cond.IsNot)
					StringBuilder.Append("NOT ");

				var precedence = GetPrecedence(cond.Predicate);

				BuildPredicate(cond.IsNot ? Precedence.LogicalNegation : parentPrecedence, precedence, cond.Predicate);

				isOr = cond.IsOr;
			}
		}

		protected virtual void BuildSearchCondition(int parentPrecedence, SelectQuery.SearchCondition condition)
		{
			var wrap = Wrap(GetPrecedence(condition as ISqlExpression), parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildSearchCondition(condition);
			if (wrap) StringBuilder.Append(')');
		}

		#endregion

		#region BuildPredicate

		protected virtual void BuildPredicate(ISqlPredicate predicate)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					{
						var expr = (SelectQuery.Predicate.ExprExpr)predicate;

						switch (expr.Operator)
						{
							case SelectQuery.Predicate.Operator.Equal:
							case SelectQuery.Predicate.Operator.NotEqual:
								{
									ISqlExpression e = null;

									if (expr.Expr1 is IValueContainer && ((IValueContainer)expr.Expr1).Value == null)
										e = expr.Expr2;
									else if (expr.Expr2 is IValueContainer && ((IValueContainer)expr.Expr2).Value == null)
										e = expr.Expr1;

									if (e != null)
									{
										BuildExpression(GetPrecedence(expr), e);
										StringBuilder.Append(expr.Operator == SelectQuery.Predicate.Operator.Equal ? " IS NULL" : " IS NOT NULL");
										return;
									}

									break;
								}
						}

						BuildExpression(GetPrecedence(expr), expr.Expr1);

						switch (expr.Operator)
						{
							case SelectQuery.Predicate.Operator.Equal: StringBuilder.Append(" = "); break;
							case SelectQuery.Predicate.Operator.NotEqual: StringBuilder.Append(" <> "); break;
							case SelectQuery.Predicate.Operator.Greater: StringBuilder.Append(" > "); break;
							case SelectQuery.Predicate.Operator.GreaterOrEqual: StringBuilder.Append(" >= "); break;
							case SelectQuery.Predicate.Operator.NotGreater: StringBuilder.Append(" !> "); break;
							case SelectQuery.Predicate.Operator.Less: StringBuilder.Append(" < "); break;
							case SelectQuery.Predicate.Operator.LessOrEqual: StringBuilder.Append(" <= "); break;
							case SelectQuery.Predicate.Operator.NotLess: StringBuilder.Append(" !< "); break;
						}

						BuildExpression(GetPrecedence(expr), expr.Expr2);
					}

					break;

				case QueryElementType.LikePredicate:
					BuildLikePredicate((SelectQuery.Predicate.Like)predicate);
					break;

				case QueryElementType.BetweenPredicate:
					{
						var p = (SelectQuery.Predicate.Between)predicate;
						BuildExpression(GetPrecedence(p), p.Expr1);
						if (p.IsNot) StringBuilder.Append(" NOT");
						StringBuilder.Append(" BETWEEN ");
						BuildExpression(GetPrecedence(p), p.Expr2);
						StringBuilder.Append(" AND ");
						BuildExpression(GetPrecedence(p), p.Expr3);
					}

					break;

				case QueryElementType.IsNullPredicate:
					{
						var p = (SelectQuery.Predicate.IsNull)predicate;
						BuildExpression(GetPrecedence(p), p.Expr1);
						StringBuilder.Append(p.IsNot ? " IS NOT NULL" : " IS NULL");
					}

					break;

				case QueryElementType.InSubQueryPredicate:
					{
						var p = (SelectQuery.Predicate.InSubQuery)predicate;
						BuildExpression(GetPrecedence(p), p.Expr1);
						StringBuilder.Append(p.IsNot ? " NOT IN " : " IN ");
						BuildExpression(GetPrecedence(p), p.SubQuery);
					}

					break;

				case QueryElementType.InListPredicate:
					BuildInListPredicate(predicate);
					break;

				case QueryElementType.FuncLikePredicate:
					{
						var f = (SelectQuery.Predicate.FuncLike)predicate;
						BuildExpression(f.Function.Precedence, f.Function);
					}

					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(predicate.Precedence, (SelectQuery.SearchCondition)predicate);
					break;

				case QueryElementType.NotExprPredicate:
					{
						var p = (SelectQuery.Predicate.NotExpr)predicate;

						if (p.IsNot)
							StringBuilder.Append("NOT ");

						BuildExpression(p.IsNot ? Precedence.LogicalNegation : GetPrecedence(p), p.Expr1);
					}

					break;

				case QueryElementType.ExprPredicate:
					{
						var p = (SelectQuery.Predicate.Expr)predicate;

						if (p.Expr1 is SqlValue)
						{
							var value = ((SqlValue)p.Expr1).Value;

							if (value is bool)
							{
								StringBuilder.Append((bool)value ? "1 = 1" : "1 = 0");
								return;
							}
						}

						BuildExpression(GetPrecedence(p), p.Expr1);
					}

					break;

				default:
					throw new InvalidOperationException();
			}
		}

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column: return GetUnderlayingField(((SelectQuery.Column)expr).Expression);
			}

			throw new InvalidOperationException();
		}

		void BuildInListPredicate(ISqlPredicate predicate)
		{
			var p = (SelectQuery.Predicate.InList)predicate;

			if (p.Values == null || p.Values.Count == 0)
			{
				BuildPredicate(new SelectQuery.Predicate.Expr(new SqlValue(false)));
			}
			else
			{
				ICollection values = p.Values;

				if (p.Values.Count == 1 && p.Values[0] is SqlParameter &&
					!(p.Expr1.SystemType == typeof(string) && ((SqlParameter)p.Values[0]).Value is string))
				{
					var pr = (SqlParameter)p.Values[0];

					if (pr.Value == null)
					{
						BuildPredicate(new SelectQuery.Predicate.Expr(new SqlValue(false)));
						return;
					}

					if (pr.Value is IEnumerable)
					{
						var items = (IEnumerable)pr.Value;

						if (p.Expr1 is ISqlTableSource)
						{
							var firstValue = true;
							var table = (ISqlTableSource)p.Expr1;
							var keys = table.GetKeys(true);

							if (keys == null || keys.Count == 0)
								throw new SqlException("Cannot create IN expression.");

							if (keys.Count == 1)
							{
								foreach (var item in items)
								{
									if (firstValue)
									{
										firstValue = false;
										BuildExpression(GetPrecedence(p), keys[0]);
										StringBuilder.Append(p.IsNot ? " NOT IN (" : " IN (");
									}

									var field = GetUnderlayingField(keys[0]);
									var value = field.ColumnDescriptor.MemberAccessor.GetValue(item);

									if (value is ISqlExpression)
										BuildExpression((ISqlExpression)value);
									else
										BuildValue(
											new SqlDataType(
												field.DataType,
												field.SystemType,
												field.Length,
												field.Precision,
												field.Scale),
											value);

									StringBuilder.Append(", ");
								}
							}
							else
							{
								var len = StringBuilder.Length;
								var rem = 1;

								foreach (var item in items)
								{
									if (firstValue)
									{
										firstValue = false;
										StringBuilder.Append('(');
									}

									foreach (var key in keys)
									{
										var field = GetUnderlayingField(key);
										var value = field.ColumnDescriptor.MemberAccessor.GetValue(item);

										BuildExpression(GetPrecedence(p), key);

										if (value == null)
										{
											StringBuilder.Append(" IS NULL");
										}
										else
										{
											StringBuilder.Append(" = ");
											BuildValue(
												new SqlDataType(
													field.DataType,
													field.SystemType,
													field.Length,
													field.Precision,
													field.Scale),
												value);
										}

										StringBuilder.Append(" AND ");
									}

									StringBuilder.Remove(StringBuilder.Length - 4, 4).Append("OR ");

									if (StringBuilder.Length - len >= 50)
									{
										StringBuilder.AppendLine();
										AppendIndent();
										StringBuilder.Append(' ');
										len = StringBuilder.Length;
										rem = 5 + Indent;
									}
								}

								if (!firstValue)
									StringBuilder.Remove(StringBuilder.Length - rem, rem);
							}

							if (firstValue)
								BuildPredicate(new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot)));
							else
								StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');
						}
						else
						{
							BuildInListValues(p, items);
						}

						return;
					}
				}

				BuildInListValues(p, values);
			}
		}

		void BuildInListValues(SelectQuery.Predicate.InList predicate, IEnumerable values)
		{
			var firstValue = true;
			var len = StringBuilder.Length;
			var hasNull = false;
			var count = 0;
			var longList = false;

			SqlDataType sqlDataType = null;

			foreach (var value in values)
			{
				if (count++ >= SqlProviderFlags.MaxInListValuesCount)
				{
					count = 1;
					longList = true;

					// start building next bucked
					firstValue = true;
					StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');
					StringBuilder.Append(" OR ");
				}

				var val = value;

				if (val is IValueContainer)
					val = ((IValueContainer)value).Value;

				if (val == null)
				{
					hasNull = true;
					continue;
				}

				if (firstValue)
				{
					firstValue = false;
					BuildExpression(GetPrecedence(predicate), predicate.Expr1);
					StringBuilder.Append(predicate.IsNot ? " NOT IN (" : " IN (");

					switch (predicate.Expr1.ElementType)
					{
						case QueryElementType.SqlField:
							{
								var field = (SqlField)predicate.Expr1;

								sqlDataType = new SqlDataType(
									field.DataType,
									field.SystemType,
									field.Length,
									field.Precision,
									field.Scale);
							}
							break;

						case QueryElementType.SqlParameter:
							{
								var p = (SqlParameter)predicate.Expr1;
								sqlDataType = new SqlDataType(p.DataType, p.SystemType, 0, 0, 0);
							}

							break;
					}
				}

				if (value is ISqlExpression)
					BuildExpression((ISqlExpression)value);
				else
					BuildValue(sqlDataType, value);

				StringBuilder.Append(", ");
			}

			if (firstValue)
			{
				BuildPredicate(
					hasNull ?
						new SelectQuery.Predicate.IsNull(predicate.Expr1, predicate.IsNot) :
						new SelectQuery.Predicate.Expr(new SqlValue(predicate.IsNot)));
			}
			else
			{
				StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');

				if (hasNull)
				{
					StringBuilder.Insert(len, "(");
					StringBuilder.Append(" OR ");
					BuildPredicate(new SelectQuery.Predicate.IsNull(predicate.Expr1, predicate.IsNot));
					StringBuilder.Append(")");
				}
			}

			if (longList && !hasNull)
			{
				StringBuilder.Insert(len, "(");
				StringBuilder.Append(")");
			}
		}

		protected void BuildPredicate(int parentPrecedence, ISqlPredicate predicate)
		{
			BuildPredicate(parentPrecedence, GetPrecedence(predicate), predicate);
		}

		protected void BuildPredicate(int parentPrecedence, int precedence, ISqlPredicate predicate)
		{
			var wrap = Wrap(precedence, parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildPredicate(predicate);
			if (wrap) StringBuilder.Append(')');
		}

		protected virtual void BuildLikePredicate(SelectQuery.Predicate.Like predicate)
		{
			var precedence = GetPrecedence(predicate);

			BuildExpression(precedence, predicate.Expr1);
			StringBuilder.Append(predicate.IsNot ? " NOT LIKE " : " LIKE ");
			BuildExpression(precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(predicate.Escape);
			}
		}

		#endregion

		#region BuildExpression

		protected virtual StringBuilder BuildExpression(
			ISqlExpression expr,
			bool buildTableName,
			bool checkParentheses,
			string alias,
			ref bool addAlias,
			bool throwExceptionIfTableNotFound = true)
		{
			// TODO: check the necessity.
			//
			expr = SqlOptimizer.ConvertExpression(expr);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlField:
					{
						var field = (SqlField)expr;

						if (buildTableName)
						{
							var ts = SelectQuery.GetTableSource(field.Table);

							if (ts == null)
							{
								if (field != field.Table.All)
								{
#if DEBUG
									//SqlQuery.GetTableSource(field.Table);
#endif

									if (throwExceptionIfTableNotFound)
										throw new SqlException("Table '{0}' not found.", field.Table);
								}
							}
							else
							{
								var table = GetTableAlias(ts);

								table = table == null ?
									GetPhysicalTableName(field.Table, null) :
									Convert(table, ConvertType.NameToQueryTableAlias).ToString();

								if (string.IsNullOrEmpty(table))
									throw new SqlException("Table {0} should have an alias.", field.Table);

								addAlias = alias != field.PhysicalName;

								StringBuilder
									.Append(table)
									.Append('.');
							}
						}

						if (field == field.Table.All)
						{
							StringBuilder.Append("*");
						}
						else
						{
							StringBuilder.Append(Convert(field.PhysicalName, ConvertType.NameToQueryField));
						}
					}

					break;

				case QueryElementType.Column:
					{
						var column = (SelectQuery.Column)expr;

#if DEBUG
						var sql = SelectQuery.SqlText;
#endif

						var table = SelectQuery.GetTableSource(column.Parent);

						if (table == null)
						{
#if DEBUG
							table = SelectQuery.GetTableSource(column.Parent);
#endif

							throw new SqlException("Table not found for '{0}'.", column);
						}

						var tableAlias = GetTableAlias(table) ?? GetPhysicalTableName(column.Parent, null);

						if (string.IsNullOrEmpty(tableAlias))
							throw new SqlException("Table {0} should have an alias.", column.Parent);

						addAlias = alias != column.Alias;

						StringBuilder
							.Append(Convert(tableAlias, ConvertType.NameToQueryTableAlias))
							.Append('.')
							.Append(Convert(column.Alias, ConvertType.NameToQueryField));
					}

					break;

				case QueryElementType.SqlQuery:
					{
						var hasParentheses = checkParentheses && StringBuilder[StringBuilder.Length - 1] == '(';

						if (!hasParentheses)
							StringBuilder.Append("(");
						StringBuilder.AppendLine();

						BuildSqlBuilder((SelectQuery)expr, Indent + 1, BuildStep != Step.FromClause);

						AppendIndent();

						if (!hasParentheses)
							StringBuilder.Append(")");
					}

					break;

				case QueryElementType.SqlValue:
					var sqlval = (SqlValue)expr;
					var dt = sqlval.SystemType == null ? null : new SqlDataType(sqlval.SystemType);
					BuildValue(dt, sqlval.Value);
					break;

				case QueryElementType.SqlExpression:
					{
						var e = (SqlExpression)expr;
						var s = new StringBuilder();

						if (e.Parameters == null || e.Parameters.Length == 0)
							StringBuilder.Append(e.Expr);
						else
						{
							var values = new object[e.Parameters.Length];

							for (var i = 0; i < values.Length; i++)
							{
								var value = e.Parameters[i];

								s.Length = 0;
								WithStringBuilder(s, () => BuildExpression(GetPrecedence(e), value));
								values[i] = s.ToString();
							}

							StringBuilder.AppendFormat(e.Expr, values);
						}
					}

					break;

				case QueryElementType.SqlBinaryExpression:
					BuildBinaryExpression((SqlBinaryExpression)expr);
					break;

				case QueryElementType.SqlFunction:
					BuildFunction((SqlFunction)expr);
					break;

				case QueryElementType.SqlParameter:
					{
						var parm = (SqlParameter)expr;

						if (parm.IsQueryParameter)
						{
							var name = Convert(parm.Name, ConvertType.NameToQueryParameter);

							StringBuilder.Append(name);
						}
						else
						{
							BuildValue(new SqlDataType(parm.DataType, parm.SystemType, 0, 0, 0), parm.Value);
						}
					}

					break;

				case QueryElementType.SqlDataType:
					BuildDataType((SqlDataType)expr);
					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(expr.Precedence, (SelectQuery.SearchCondition)expr);
					break;

				default:
					throw new InvalidOperationException();
			}

			return StringBuilder;
		}

		void BuildExpression(int parentPrecedence, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = Wrap(GetPrecedence(expr), parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildExpression(expr, true, true, alias, ref addAlias);
			if (wrap) StringBuilder.Append(')');
		}

		protected StringBuilder BuildExpression(ISqlExpression expr)
		{
			var dummy = false;
			return BuildExpression(expr, true, true, null, ref dummy);
		}

		protected void BuildExpression(ISqlExpression expr, bool buildTableName, bool checkParentheses, bool throwExceptionIfTableNotFound = true)
		{
			var dummy = false;
			BuildExpression(expr, buildTableName, checkParentheses, null, ref dummy, throwExceptionIfTableNotFound);
		}

		protected void BuildExpression(int precedence, ISqlExpression expr)
		{
			var dummy = false;
			BuildExpression(precedence, expr, null, ref dummy);
		}

		#endregion

		#region BuildValue

		protected void BuildValue(SqlDataType dataType, object value)
		{
			if (dataType != null)
				ValueToSqlConverter.Convert(StringBuilder, dataType, value);
			else
				ValueToSqlConverter.Convert(StringBuilder, value);
		}

		#endregion

		#region BuildBinaryExpression

		protected virtual void BuildBinaryExpression(SqlBinaryExpression expr)
		{
			BuildBinaryExpression(expr.Operation, expr);
		}

		void BuildBinaryExpression(string op, SqlBinaryExpression expr)
		{
			if (expr.Operation == "*" && expr.Expr1 is SqlValue)
			{
				var value = (SqlValue)expr.Expr1;

				if (value.Value is int && (int)value.Value == -1)
				{
					StringBuilder.Append('-');
					BuildExpression(GetPrecedence(expr), expr.Expr2);
					return;
				}
			}

			BuildExpression(GetPrecedence(expr), expr.Expr1);
			StringBuilder.Append(' ').Append(op).Append(' ');
			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		#endregion

		#region BuildFunction

		protected virtual void BuildFunction(SqlFunction func)
		{
			if (func.Name == "CASE")
			{
				StringBuilder.Append(func.Name).AppendLine();

				Indent++;

				var i = 0;

				for (; i < func.Parameters.Length - 1; i += 2)
				{
					AppendIndent().Append("WHEN ");

					var len = StringBuilder.Length;

					BuildExpression(func.Parameters[i]);

					if (SqlExpression.NeedsEqual(func.Parameters[i]))
					{
						StringBuilder.Append(" = ");
						BuildValue(null, true);
					}

					if (StringBuilder.Length - len > 20)
					{
						StringBuilder.AppendLine();
						AppendIndent().Append("\tTHEN ");
					}
					else
						StringBuilder.Append(" THEN ");

					BuildExpression(func.Parameters[i + 1]);
					StringBuilder.AppendLine();
				}

				if (i < func.Parameters.Length)
				{
					AppendIndent().Append("ELSE ");
					BuildExpression(func.Parameters[i]);
					StringBuilder.AppendLine();
				}

				Indent--;

				AppendIndent().Append("END");
			}
			else
				BuildFunction(func.Name, func.Parameters);
		}

		void BuildFunction(string name, ISqlExpression[] exprs)
		{
			StringBuilder.Append(name).Append('(');

			var first = true;

			foreach (var parameter in exprs)
			{
				if (!first)
					StringBuilder.Append(", ");

				BuildExpression(parameter, true, !first || name == "EXISTS");

				first = false;
			}

			StringBuilder.Append(')');
		}

		#endregion

		#region BuildDataType

		protected virtual void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Double: StringBuilder.Append("Float"); return;
				case DataType.Single: StringBuilder.Append("Real"); return;
				case DataType.SByte: StringBuilder.Append("TinyInt"); return;
				case DataType.UInt16: StringBuilder.Append("Int"); return;
				case DataType.UInt32: StringBuilder.Append("BigInt"); return;
				case DataType.UInt64: StringBuilder.Append("Decimal"); return;
				case DataType.Byte: StringBuilder.Append("TinyInt"); return;
				case DataType.Int16: StringBuilder.Append("SmallInt"); return;
				case DataType.Int32: StringBuilder.Append("Int"); return;
				case DataType.Int64: StringBuilder.Append("BigInt"); return;
				case DataType.Boolean: StringBuilder.Append("Bit"); return;
			}

			StringBuilder.Append(type.DataType);

			if (type.Length > 0)
				StringBuilder.Append('(').Append(type.Length).Append(')');

			if (type.Precision > 0)
				StringBuilder.Append('(').Append(type.Precision).Append(',').Append(type.Scale).Append(')');
		}

		#endregion

		#region GetPrecedence

		static int GetPrecedence(ISqlExpression expr)
		{
			return expr.Precedence;
		}

		protected static int GetPrecedence(ISqlPredicate predicate)
		{
			return predicate.Precedence;
		}

		#endregion

		#endregion

		#region Internal Types

		protected enum Step
		{
			SelectClause,
			DeleteClause,
			UpdateClause,
			InsertClause,
			FromClause,
			WhereClause,
			GroupByClause,
			HavingClause,
			OrderByClause,
			OffsetLimit
		}

		#endregion

		#region Alternative Builders

		void BuildAliases(string table, List<SelectQuery.Column> columns, string postfix)
		{
			Indent++;

			var first = true;

			foreach (var col in columns)
			{
				if (!first)
					StringBuilder.Append(',').AppendLine();
				first = false;

				AppendIndent().AppendFormat("{0}.{1}", table, Convert(col.Alias, ConvertType.NameToQueryFieldAlias));

				if (postfix != null)
					StringBuilder.Append(postfix);
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		protected void AlternativeBuildSql(bool implementOrderBy, Action buildSql)
		{
			if (NeedSkip)
			{
				var aliases = GetTempAliases(2, "t");
				var rnaliase = GetTempAliases(1, "rn")[0];

				AppendIndent().Append("SELECT *").AppendLine();
				AppendIndent().Append("FROM").AppendLine();
				AppendIndent().Append("(").AppendLine();
				Indent++;

				AppendIndent().Append("SELECT").AppendLine();

				Indent++;
				AppendIndent().AppendFormat("{0}.*,", aliases[0]).AppendLine();
				AppendIndent().Append("ROW_NUMBER() OVER");

				if (!SelectQuery.OrderBy.IsEmpty && !implementOrderBy)
					StringBuilder.Append("()");
				else
				{
					StringBuilder.AppendLine();
					AppendIndent().Append("(").AppendLine();

					Indent++;

					if (SelectQuery.OrderBy.IsEmpty)
					{
						AppendIndent().Append("ORDER BY").AppendLine();
						BuildAliases(aliases[0], SelectQuery.Select.Columns.Take(1).ToList(), null);
					}
					else
						BuildAlternativeOrderBy(true);

					Indent--;
					AppendIndent().Append(")");
				}

				StringBuilder.Append(" as ").Append(rnaliase).AppendLine();
				Indent--;

				AppendIndent().Append("FROM").AppendLine();
				AppendIndent().Append("(").AppendLine();

				Indent++;
				buildSql();
				Indent--;

				AppendIndent().AppendFormat(") {0}", aliases[0]).AppendLine();

				Indent--;

				AppendIndent().AppendFormat(") {0}", aliases[1]).AppendLine();
				AppendIndent().Append("WHERE").AppendLine();

				Indent++;

				if (NeedTake)
				{
					var expr1 = Add(SelectQuery.Select.SkipValue, 1);
					var expr2 = Add<int>(SelectQuery.Select.SkipValue, SelectQuery.Select.TakeValue);

					if (expr1 is SqlValue && expr2 is SqlValue && Equals(((SqlValue)expr1).Value, ((SqlValue)expr2).Value))
					{
						AppendIndent().AppendFormat("{0}.{1} = ", aliases[1], rnaliase);
						BuildExpression(expr1);
					}
					else
					{
						AppendIndent().AppendFormat("{0}.{1} BETWEEN ", aliases[1], rnaliase);
						BuildExpression(expr1);
						StringBuilder.Append(" AND ");
						BuildExpression(expr2);
					}
				}
				else
				{
					AppendIndent().AppendFormat("{0}.{1} > ", aliases[1], rnaliase);
					BuildExpression(SelectQuery.Select.SkipValue);
				}

				StringBuilder.AppendLine();
				Indent--;
			}
			else
				buildSql();
		}

		protected void AlternativeBuildSql2(Action buildSql)
		{
			var aliases = GetTempAliases(3, "t");

			AppendIndent().Append("SELECT *").AppendLine();
			AppendIndent().Append("FROM").AppendLine();
			AppendIndent().Append("(").AppendLine();
			Indent++;

			AppendIndent().Append("SELECT TOP ");
			BuildExpression(SelectQuery.Select.TakeValue);
			StringBuilder.Append(" *").AppendLine();
			AppendIndent().Append("FROM").AppendLine();
			AppendIndent().Append("(").AppendLine();
			Indent++;

			if (SelectQuery.OrderBy.IsEmpty)
			{
				AppendIndent().Append("SELECT TOP ");

				var p = SelectQuery.Select.SkipValue as SqlParameter;

				if (p != null && !p.IsQueryParameter && SelectQuery.Select.TakeValue is SqlValue)
					BuildValue(null, (int)p.Value + (int)((SqlValue)(SelectQuery.Select.TakeValue)).Value);
				else
					BuildExpression(Add<int>(SelectQuery.Select.SkipValue, SelectQuery.Select.TakeValue));

				StringBuilder.Append(" *").AppendLine();
				AppendIndent().Append("FROM").AppendLine();
				AppendIndent().Append("(").AppendLine();
				Indent++;
			}

			buildSql();

			if (SelectQuery.OrderBy.IsEmpty)
			{
				Indent--;
				AppendIndent().AppendFormat(") {0}", aliases[2]).AppendLine();
				AppendIndent().Append("ORDER BY").AppendLine();
				BuildAliases(aliases[2], SelectQuery.Select.Columns, null);
			}

			Indent--;
			AppendIndent().AppendFormat(") {0}", aliases[1]).AppendLine();

			if (SelectQuery.OrderBy.IsEmpty)
			{
				AppendIndent().Append("ORDER BY").AppendLine();
				BuildAliases(aliases[1], SelectQuery.Select.Columns, " DESC");
			}
			else
			{
				BuildAlternativeOrderBy(false);
			}

			Indent--;
			AppendIndent().AppendFormat(") {0}", aliases[0]).AppendLine();

			if (SelectQuery.OrderBy.IsEmpty)
			{
				AppendIndent().Append("ORDER BY").AppendLine();
				BuildAliases(aliases[0], SelectQuery.Select.Columns, null);
			}
			else
			{
				BuildAlternativeOrderBy(true);
			}
		}

		void BuildAlternativeOrderBy(bool ascending)
		{
			AppendIndent().Append("ORDER BY").AppendLine();

			var obys = GetTempAliases(SelectQuery.OrderBy.Items.Count, "oby");

			Indent++;

			for (var i = 0; i < obys.Length; i++)
			{
				AppendIndent().Append(obys[i]);

				if (ascending && SelectQuery.OrderBy.Items[i].IsDescending ||
					!ascending && !SelectQuery.OrderBy.Items[i].IsDescending)
					StringBuilder.Append(" DESC");

				if (i + 1 < obys.Length)
					StringBuilder.Append(',');

				StringBuilder.AppendLine();
			}

			Indent--;
		}

		protected delegate IEnumerable<SelectQuery.Column> ColumnSelector();

		protected IEnumerable<SelectQuery.Column> AlternativeGetSelectedColumns(ColumnSelector columnSelector)
		{
			foreach (var col in columnSelector())
				yield return col;

			var obys = GetTempAliases(SelectQuery.OrderBy.Items.Count, "oby");

			for (var i = 0; i < obys.Length; i++)
				yield return new SelectQuery.Column(SelectQuery, SelectQuery.OrderBy.Items[i].Expression, obys[i]);
		}

		protected static bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType: return ((SqlDataType)expr).DataType == DataType.Date;
				case QueryElementType.SqlExpression: return ((SqlExpression)expr).Expr == dateName;
			}

			return false;
		}

		protected static bool IsTimeDataType(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType: return ((SqlDataType)expr).DataType == DataType.Time;
				case QueryElementType.SqlExpression: return ((SqlExpression)expr).Expr == "Time";
			}

			return false;
		}

		static bool IsBooleanParameter(ISqlExpression expr, int count, int i)
		{
			if ((i % 2 == 1 || i == count - 1) && expr.SystemType == typeof(bool) || expr.SystemType == typeof(bool?))
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SearchCondition: return true;
				}
			}

			return false;
		}

		protected SqlFunction ConvertFunctionParameters(SqlFunction func)
		{
			if (func.Name == "CASE" &&
				func.Parameters.Select((p, i) => new { p, i }).Any(p => IsBooleanParameter(p.p, func.Parameters.Length, p.i)))
			{
				return new SqlFunction(
					func.SystemType,
					func.Name,
					func.Precedence,
					func.Parameters.Select((p, i) =>
						IsBooleanParameter(p, func.Parameters.Length, i) ?
							SqlOptimizer.ConvertExpression(new SqlFunction(typeof(bool), "CASE", p, new SqlValue(true), new SqlValue(false))) :
							p
					).ToArray());
			}

			return func;
		}

		#endregion

		#region Helpers

		protected SequenceNameAttribute GetSequenceNameAttribute(SqlTable table, bool throwException)
		{
			var identityField = table.GetIdentityField();

			if (identityField == null)
				if (throwException)
					throw new SqlException("Identity field must be defined for '{0}'.", table.Name);
				else
					return null;

			if (table.ObjectType == null)
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			var attrs = table.SequenceAttributes;

			if (attrs.IsNullOrEmpty())
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			SequenceNameAttribute defaultAttr = null;

			foreach (var attr in attrs)
			{
				if (attr.Configuration == Name)
					return attr;

				if (defaultAttr == null && attr.Configuration == null)
					defaultAttr = attr;
			}

			if (defaultAttr == null)
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			return defaultAttr;
		}

		static bool Wrap(int precedence, int parentPrecedence)
		{
			return
				precedence == 0 ||
				/* maybe it will be no harm to put "<=" here? */
				precedence < parentPrecedence ||
				(precedence == parentPrecedence &&
					(parentPrecedence == Precedence.Subtraction ||
					 parentPrecedence == Precedence.Multiplicative ||
					 parentPrecedence == Precedence.LogicalNegation));
		}

		protected string[] GetTempAliases(int n, string defaultAlias)
		{
			return SelectQuery.GetTempAliases(n, defaultAlias);
		}

		protected static string GetTableAlias(ISqlTableSource table)
		{
			switch (table.ElementType)
			{
				case QueryElementType.TableSource:
					var ts = (SelectQuery.TableSource)table;
					var alias = string.IsNullOrEmpty(ts.Alias) ? GetTableAlias(ts.Source) : ts.Alias;
					return alias != "$" ? alias : null;

				case QueryElementType.SqlTable:
					return ((SqlTable)table).Alias;

				default:
					throw new InvalidOperationException();
			}
		}

		protected virtual string GetTableDatabaseName(SqlTable table)
		{
			return table.Database == null ? null : Convert(table.Database, ConvertType.NameToDatabase).ToString();
		}

		protected virtual string GetTableOwnerName(SqlTable table)
		{
			return table.Owner == null ? null : Convert(table.Owner, ConvertType.NameToOwner).ToString();
		}

		protected virtual string GetTablePhysicalName(SqlTable table)
		{
			return table.PhysicalName == null ? null : Convert(table.PhysicalName, ConvertType.NameToQueryTable).ToString();
		}

		string GetPhysicalTableName(ISqlTableSource table, string alias)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable:
					{
						var tbl = (SqlTable)table;

						var database = GetTableDatabaseName(tbl);
						var owner = GetTableOwnerName(tbl);
						var physicalName = GetTablePhysicalName(tbl);

						var sb = new StringBuilder();

						BuildTableName(sb, database, owner, physicalName);

						if (tbl.SqlTableType == SqlTableType.Expression)
						{
							var values = new object[2 + (tbl.TableArguments == null ? 0 : tbl.TableArguments.Length)];

							values[0] = sb.ToString();
							values[1] = Convert(alias, ConvertType.NameToQueryTableAlias);

							for (var i = 2; i < values.Length; i++)
							{
								var value = tbl.TableArguments[i - 2];

								sb.Length = 0;
								WithStringBuilder(sb, () => BuildExpression(Precedence.Primary, value));
								values[i] = sb.ToString();
							}

							sb.Length = 0;
							sb.AppendFormat(tbl.Name, values);
						}

						if (tbl.SqlTableType == SqlTableType.Function)
						{
							sb.Append('(');

							if (tbl.TableArguments != null && tbl.TableArguments.Length > 0)
							{
								var first = true;

								foreach (var arg in tbl.TableArguments)
								{
									if (!first)
										sb.Append(", ");

									WithStringBuilder(sb, () => BuildExpression(arg, true, !first));

									first = false;
								}
							}

							sb.Append(')');
						}

						return sb.ToString();
					}

				case QueryElementType.TableSource:
					return GetPhysicalTableName(((SelectQuery.TableSource)table).Source, alias);

				default:
					throw new InvalidOperationException();
			}
		}

		protected StringBuilder AppendIndent()
		{
			if (Indent > 0)
				StringBuilder.Append('\t', Indent);

			return StringBuilder;
		}

		ISqlExpression Add(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return SqlOptimizer.ConvertExpression(new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive));
		}

		protected ISqlExpression Add<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		ISqlExpression Add(ISqlExpression expr1, int value)
		{
			return Add<int>(expr1, new SqlValue(value));
		}

		protected virtual bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word);
		}

		#endregion

		#region ISqlProvider Members

		public virtual ISqlExpression GetIdentityExpression(SqlTable table)
		{
			return null;
		}

		protected virtual void PrintParameterName(StringBuilder sb, IDbDataParameter parameter)
		{
			if (!parameter.ParameterName.StartsWith("@"))
				sb.Append('@');
			sb.Append(parameter.ParameterName);
		}

		protected virtual string GetTypeName(IDbDataParameter parameter)
		{
			return null;
		}

		protected virtual string GetUdtTypeName(IDbDataParameter parameter)
		{
			return null;
		}

		protected virtual string GetProviderTypeName(IDbDataParameter parameter)
		{
			switch (parameter.DbType)
			{
				case DbType.AnsiString: return "VarChar";
				case DbType.AnsiStringFixedLength: return "Char";
				case DbType.String: return "NVarChar";
				case DbType.StringFixedLength: return "NChar";
				case DbType.Decimal: return "Decimal";
				case DbType.Binary: return "Binary";
			}

			return null;
		}

		protected virtual void PrintParameterType(StringBuilder sb, IDbDataParameter parameter)
		{
			var typeName = GetTypeName(parameter);
			if (!string.IsNullOrEmpty(typeName))
				sb.Append(typeName).Append(" -- ");

			var udtTypeName = GetUdtTypeName(parameter);
			if (!string.IsNullOrEmpty(udtTypeName))
				sb.Append(udtTypeName).Append(" -- ");

			var t1 = GetProviderTypeName(parameter);
			var t2 = parameter.DbType.ToString();

			sb.Append(t1);

			if (t1 != null)
			{
				if (parameter.Size != 0)
				{
					if (t1.IndexOf('(') < 0)
						sb.Append('(').Append(parameter.Size).Append(')');
				}
				else if (parameter.Precision != 0)
				{
					if (t1.IndexOf('(') < 0)
						sb.Append('(').Append(parameter.Precision).Append(',').Append(parameter.Scale).Append(')');
				}
				else
				{
					switch (parameter.DbType)
					{
						case DbType.AnsiString:
						case DbType.AnsiStringFixedLength:
						case DbType.String:
						case DbType.StringFixedLength:
							{
								var value = parameter.Value as string;

								if (!string.IsNullOrEmpty(value))
									sb.Append('(').Append(value.Length).Append(')');

								break;
							}
#if !SILVERLIGHT && !NETFX_CORE
						case DbType.Decimal:
							{
								var value = parameter.Value;

								if (value is decimal)
								{
									var d = new SqlDecimal((decimal)value);
									sb.Append('(').Append(d.Precision).Append(',').Append(d.Scale).Append(')');
								}

								break;
							}
#endif
						case DbType.Binary:
							{
								var value = parameter.Value as byte[];

								if (value != null)
									sb.Append('(').Append(value.Length).Append(')');

								break;
							}
					}
				}
			}

			if (t1 != t2)
				sb.Append(" -- ").Append(t2);
		}

		protected virtual void PrintParameterValue(StringBuilder sb, IDbDataParameter parameter)
		{
			ValueToSqlConverter.Convert(sb, parameter.Value);
		}

		public virtual StringBuilder PrintParameters(StringBuilder sb, IDbDataParameter[] parameters)
		{
			if (parameters != null && parameters.Length > 0)
			{
				foreach (var p in parameters)
				{
					sb.Append("DECLARE ");
					PrintParameterName(sb, p);
					sb.Append(' ');
					PrintParameterType(sb, p);
					sb.AppendLine();

					sb.Append("SET     ");
					PrintParameterName(sb, p);
					sb.Append(" = ");
					ValueToSqlConverter.Convert(sb, p.Value);
					sb.AppendLine();
				}

				sb.AppendLine();
			}

			return sb;
		}

		public string ApplyQueryHints(string sql, List<string> queryHints)
		{
			var sb = new StringBuilder(sql);

			foreach (var hint in queryHints)
				sb.AppendLine(hint);

			return sb.ToString();
		}

		private string _name;

		public virtual string Name
		{
			get { return _name ?? (_name = GetType().Name.Replace("SqlBuilder", "")); }
		}
		#endregion
	}
}
