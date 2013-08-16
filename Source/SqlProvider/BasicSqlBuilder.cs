using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using Common;
	using Mapping;
	using SqlQuery;

	public abstract class BasicSqlBuilder : ISqlBuilder
	{
		#region Init

		protected BasicSqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
		{
			SqlOptimizer     = sqlOptimizer;
			SqlProviderFlags = sqlProviderFlags;
		}

		public SelectQuery SelectQuery { get; set; }
		public int         Indent      { get; set; }
		public Step        BuildStep   { get; set; }

		bool _skipAlias;

		public ISqlOptimizer    SqlOptimizer     { get; private set; }
		public SqlProviderFlags SqlProviderFlags { get; private set; }

		#endregion

		#region Support Flags

		public virtual bool IsNestedJoinSupported           { get { return true;  } }
		public virtual bool IsNestedJoinParenthesisRequired { get { return false; } }
		public virtual bool IsGroupByExpressionSupported    { get { return true;  } }

		public virtual bool ConvertCountSubQuery(SelectQuery subQuery)
		{
			return true;
		}

		#endregion

		#region CommandCount

		public virtual int CommandCount(SelectQuery selectQuery)
		{
			return 1;
		}

		#endregion

		#region BuildSql

		public virtual void BuildSql(int commandNumber, SelectQuery selectQuery, StringBuilder sb, int indent, bool skipAlias)
		{
			SelectQuery = selectQuery;
			Indent      = indent;
			_skipAlias  = skipAlias;

			if (commandNumber == 0)
			{
				BuildSql(sb);

				if (selectQuery.HasUnion)
				{
					foreach (var union in selectQuery.Unions)
					{
						AppendIndent(sb);
						sb.Append("UNION");
						if (union.IsAll) sb.Append(" ALL");
						sb.AppendLine();

						CreateSqlProvider().BuildSql(commandNumber, union.SelectQuery, sb, indent, skipAlias);
					}
				}
			}
			else
			{
				BuildCommand(commandNumber, sb);
			}
		}

		protected virtual void BuildCommand(int commandNumber, StringBuilder sb)
		{
		}

		#endregion

		#region Overrides

		protected virtual void BuildSqlBuilder(SelectQuery selectQuery, StringBuilder sb, int indent, bool skipAlias)
		{
			if (!SqlProviderFlags.GetIsSkipSupportedFlag(selectQuery)
				&& selectQuery.Select.SkipValue != null)
				throw new SqlException("Skip for subqueries is not supported by the '{0}' provider.", Name);

			if (!SqlProviderFlags.IsTakeSupported && selectQuery.Select.TakeValue != null)
				throw new SqlException("Take for subqueries is not supported by the '{0}' provider.", Name);

			CreateSqlProvider().BuildSql(0, selectQuery, sb, indent, skipAlias);
		}

		protected abstract ISqlBuilder CreateSqlProvider();

		protected virtual bool ParenthesizeJoin()
		{
			return false;
		}

		protected virtual void BuildSql(StringBuilder sb)
		{
			switch (SelectQuery.QueryType)
			{
				case QueryType.Select         : BuildSelectQuery         (sb); break;
				case QueryType.Delete         : BuildDeleteQuery         (sb); break;
				case QueryType.Update         : BuildUpdateQuery         (sb); break;
				case QueryType.Insert         : BuildInsertQuery         (sb); break;
				case QueryType.InsertOrUpdate : BuildInsertOrUpdateQuery (sb); break;
				case QueryType.CreateTable    :
					if (SelectQuery.CreateTable.IsDrop)
						BuildDropTableStatement(sb);
					else
						BuildCreateTableStatement(sb);
					break;
				default                       : BuildUnknownQuery        (sb); break;
			}
		}

		protected virtual void BuildDeleteQuery(StringBuilder sb)
		{
			BuildStep = Step.DeleteClause;  BuildDeleteClause (sb);
			BuildStep = Step.FromClause;    BuildFromClause   (sb);
			BuildStep = Step.WhereClause;   BuildWhereClause  (sb);
			BuildStep = Step.GroupByClause; BuildGroupByClause(sb);
			BuildStep = Step.HavingClause;  BuildHavingClause (sb);
			BuildStep = Step.OrderByClause; BuildOrderByClause(sb);
			BuildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
		}

		protected virtual void BuildUpdateQuery(StringBuilder sb)
		{
			BuildStep = Step.UpdateClause;  BuildUpdateClause (sb);
			BuildStep = Step.FromClause;    BuildFromClause   (sb);
			BuildStep = Step.WhereClause;   BuildWhereClause  (sb);
			BuildStep = Step.GroupByClause; BuildGroupByClause(sb);
			BuildStep = Step.HavingClause;  BuildHavingClause (sb);
			BuildStep = Step.OrderByClause; BuildOrderByClause(sb);
			BuildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
		}

		protected virtual void BuildSelectQuery(StringBuilder sb)
		{
			BuildStep = Step.SelectClause;  BuildSelectClause (sb);
			BuildStep = Step.FromClause;    BuildFromClause   (sb);
			BuildStep = Step.WhereClause;   BuildWhereClause  (sb);
			BuildStep = Step.GroupByClause; BuildGroupByClause(sb);
			BuildStep = Step.HavingClause;  BuildHavingClause (sb);
			BuildStep = Step.OrderByClause; BuildOrderByClause(sb);
			BuildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
		}

		protected virtual void BuildInsertQuery(StringBuilder sb)
		{
			BuildStep = Step.InsertClause; BuildInsertClause(sb);

			if (SelectQuery.QueryType == QueryType.Insert && SelectQuery.From.Tables.Count != 0)
			{
				BuildStep = Step.SelectClause;  BuildSelectClause (sb);
				BuildStep = Step.FromClause;    BuildFromClause   (sb);
				BuildStep = Step.WhereClause;   BuildWhereClause  (sb);
				BuildStep = Step.GroupByClause; BuildGroupByClause(sb);
				BuildStep = Step.HavingClause;  BuildHavingClause (sb);
				BuildStep = Step.OrderByClause; BuildOrderByClause(sb);
				BuildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
			}

			if (SelectQuery.Insert.WithIdentity)
				BuildGetIdentity(sb);
		}

		protected virtual void BuildUnknownQuery(StringBuilder sb)
		{
			throw new SqlException("Unknown query type '{0}'.", SelectQuery.QueryType);
		}

		public virtual StringBuilder BuildTableName(StringBuilder sb, string database, string owner, string table)
		{
			if (database != null)
			{
				if (owner == null)  sb.Append(database).Append("..");
				else                sb.Append(database).Append(".").Append(owner).Append(".");
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

		protected virtual void BuildSelectClause(StringBuilder sb)
		{
			AppendIndent(sb);
			sb.Append("SELECT");

			if (SelectQuery.Select.IsDistinct)
				sb.Append(" DISTINCT");

			BuildSkipFirst(sb);

			sb.AppendLine();
			BuildColumns(sb);
		}

		protected virtual IEnumerable<SelectQuery.Column> GetSelectedColumns()
		{
			return SelectQuery.Select.Columns;
		}

		protected virtual void BuildColumns(StringBuilder sb)
		{
			Indent++;

			var first = true;

			foreach (var col in GetSelectedColumns())
			{
				if (!first)
					sb.Append(',').AppendLine();
				first = false;

				var addAlias = true;

				AppendIndent(sb);
				BuildColumnExpression(sb, col.Expression, col.Alias, ref addAlias);

				if (!_skipAlias && addAlias && col.Alias != null)
					sb.Append(" as ").Append(Convert(col.Alias, ConvertType.NameToQueryFieldAlias));
			}

			if (first)
				AppendIndent(sb).Append("*");

			Indent--;

			sb.AppendLine();
		}

		protected virtual void BuildColumnExpression(StringBuilder sb, ISqlExpression expr, string alias, ref bool addAlias)
		{
			BuildExpression(sb, expr, true, true, alias, ref addAlias);
		}

		#endregion

		#region Build Delete

		protected virtual void BuildDeleteClause(StringBuilder sb)
		{
			AppendIndent(sb);
			sb.Append("DELETE ");
		}

		#endregion

		#region Build Update

		protected virtual void BuildUpdateClause(StringBuilder sb)
		{
			BuildUpdateTable(sb);
			BuildUpdateSet  (sb);
		}

		protected virtual void BuildUpdateTable(StringBuilder sb)
		{
			AppendIndent(sb)
				.AppendLine("UPDATE")
				.Append('\t');
			BuildUpdateTableName(sb);
			sb.AppendLine();
		}

		protected virtual void BuildUpdateTableName(StringBuilder sb)
		{
			if (SelectQuery.Update.Table != null && SelectQuery.Update.Table != SelectQuery.From.Tables[0].Source)
				BuildPhysicalTable(sb, SelectQuery.Update.Table, null);
			else
				BuildTableName(sb, SelectQuery.From.Tables[0], true, true);
		}

		protected virtual void BuildUpdateSet(StringBuilder sb)
		{
			AppendIndent(sb)
				.AppendLine("SET");

			Indent++;

			var first = true;

			foreach (var expr in SelectQuery.Update.Items)
			{
				if (!first)
					sb.Append(',').AppendLine();
				first = false;

				AppendIndent(sb);
				BuildExpression(sb, expr.Column, false, true);
				sb.Append(" = ");

				var addAlias = false;

				BuildColumnExpression(sb, expr.Expression, null, ref addAlias);
			}

			Indent--;

			sb.AppendLine();
		}

		#endregion

		#region Build Insert

		protected void BuildInsertClause(StringBuilder sb)
		{
			BuildInsertClause(sb, "INSERT INTO ", true);
		}

		protected virtual void BuildEmptyInsert(StringBuilder sb)
		{
			sb.AppendLine("DEFAULT VALUES");
		}

		protected virtual void BuildInsertClause(StringBuilder sb, string insertText, bool appendTableName)
		{
			AppendIndent(sb).Append(insertText);

			if (appendTableName)
				BuildPhysicalTable(sb, SelectQuery.Insert.Into, null);

			if (SelectQuery.Insert.Items.Count == 0)
			{
				sb.Append(' ');
				BuildEmptyInsert(sb);
			}
			else
			{
				sb.AppendLine();

				AppendIndent(sb).AppendLine("(");

				Indent++;

				var first = true;

				foreach (var expr in SelectQuery.Insert.Items)
				{
					if (!first)
						sb.Append(',').AppendLine();
					first = false;

					AppendIndent(sb);
					BuildExpression(sb, expr.Column, false, true);
				}

				Indent--;

				sb.AppendLine();
				AppendIndent(sb).AppendLine(")");

				if (SelectQuery.QueryType == QueryType.InsertOrUpdate || SelectQuery.From.Tables.Count == 0)
				{
					AppendIndent(sb).AppendLine("VALUES");
					AppendIndent(sb).AppendLine("(");

					Indent++;

					first = true;

					foreach (var expr in SelectQuery.Insert.Items)
					{
						if (!first)
							sb.Append(',').AppendLine();
						first = false;

						AppendIndent(sb);
						BuildExpression(sb, expr.Expression);
					}

					Indent--;

					sb.AppendLine();
					AppendIndent(sb).AppendLine(")");
				}
			}
		}

		protected virtual void BuildGetIdentity(StringBuilder sb)
		{
			//throw new SqlException("Insert with identity is not supported by the '{0}' sql provider.", Name);
		}

		#endregion

		#region Build InsertOrUpdate

		protected virtual void BuildInsertOrUpdateQuery(StringBuilder sb)
		{
			throw new SqlException("InsertOrUpdate query type is not supported by {0} provider.", Name);
		}

		protected void BuildInsertOrUpdateQueryAsMerge(StringBuilder sb, string fromDummyTable)
		{
			var table       = SelectQuery.Insert.Into;
			var targetAlias = Convert(SelectQuery.From.Tables[0].Alias, ConvertType.NameToQueryTableAlias).ToString();
			var sourceAlias = Convert(GetTempAliases(1, "s")[0],        ConvertType.NameToQueryTableAlias).ToString();
			var keys        = SelectQuery.Update.Keys;

			AppendIndent(sb).Append("MERGE INTO ");
			BuildPhysicalTable(sb, table, null);
			sb.Append(' ').AppendLine(targetAlias);

			AppendIndent(sb).Append("USING (SELECT ");

			for (var i = 0; i < keys.Count; i++)
			{
				BuildExpression(sb, keys[i].Expression, false, false);
				sb.Append(" AS ");
				BuildExpression(sb, keys[i].Column, false, false);

				if (i + 1 < keys.Count)
					sb.Append(", ");
			}

			if (!string.IsNullOrEmpty(fromDummyTable))
				sb.Append(' ').Append(fromDummyTable);

			sb.Append(") ").Append(sourceAlias).AppendLine(" ON");

			AppendIndent(sb).AppendLine("(");

			Indent++;

			for (var i = 0; i < keys.Count; i++)
			{
				var key = keys[i];

				AppendIndent(sb);

				sb.Append(targetAlias).Append('.');
				BuildExpression(sb, key.Column, false, false);

				sb.Append(" = ").Append(sourceAlias).Append('.');
				BuildExpression(sb, key.Column, false, false);

				if (i + 1 < keys.Count)
					sb.Append(" AND");

				sb.AppendLine();
			}

			Indent--;

			AppendIndent(sb).AppendLine(")");
			AppendIndent(sb).AppendLine("WHEN MATCHED THEN");

			Indent++;
			AppendIndent(sb).AppendLine("UPDATE ");
			BuildUpdateSet(sb);
			Indent--;

			AppendIndent(sb).AppendLine("WHEN NOT MATCHED THEN");

			Indent++;
			BuildInsertClause(sb, "INSERT", false);
			Indent--;

			while (_endLine.Contains(sb[sb.Length - 1]))
				sb.Length--;
		}

		static readonly char[] _endLine = new[] { ' ', '\r', '\n' };

		protected void BuildInsertOrUpdateQueryAsUpdateInsert(StringBuilder sb)
		{
			AppendIndent(sb).AppendLine("BEGIN TRAN").AppendLine();

			BuildUpdateQuery(sb);

			AppendIndent(sb).AppendLine("WHERE");

			var alias = Convert(SelectQuery.From.Tables[0].Alias, ConvertType.NameToQueryTableAlias).ToString();
			var exprs = SelectQuery.Update.Keys;

			Indent++;

			for (var i = 0; i < exprs.Count; i++)
			{
				var expr = exprs[i];

				AppendIndent(sb);

				sb.Append(alias).Append('.');
				BuildExpression(sb, expr.Column, false, false);

				sb.Append(" = ");
				BuildExpression(sb, Precedence.Comparison, expr.Expression);

				if (i + 1 < exprs.Count)
					sb.Append(" AND");

				sb.AppendLine();
			}

			Indent--;

			sb.AppendLine();
			AppendIndent(sb).AppendLine("IF @@ROWCOUNT = 0");
			AppendIndent(sb).AppendLine("BEGIN");

			Indent++;

			BuildInsertQuery(sb);

			Indent--;

			AppendIndent(sb).AppendLine("END");

			sb.AppendLine();
			AppendIndent(sb).AppendLine("COMMIT");
		}

		#endregion

		#region Build DDL

		protected virtual void BuildDropTableStatement(StringBuilder sb)
		{
			var table = SelectQuery.CreateTable.Table;

			AppendIndent(sb).Append("DROP TABLE ");
			BuildPhysicalTable(sb, table, null);
			sb.AppendLine();
		}

		protected virtual void BuildCreateTableStatement(StringBuilder sb)
		{
			var table = SelectQuery.CreateTable.Table;

			AppendIndent(sb).Append("CREATE TABLE ");

			BuildPhysicalTable(sb, table, null);
			sb.AppendLine();
			AppendIndent(sb).Append("(");
			Indent++;

			var fields = table.Fields.Select(f => new { field = f.Value, sb = new StringBuilder() }).ToList();
			var maxlen = 0;

			Action appendToMax = () =>
			{
				foreach (var field in fields)
					while (maxlen > field.sb.Length)
						field.sb.Append(' ');
			};

			// Build field name.
			//
			foreach (var field in fields)
			{
				field.sb.Append(Convert(field.field.PhysicalName, ConvertType.NameToQueryField));

				if (maxlen < field.sb.Length)
					maxlen = field.sb.Length;
			}

			appendToMax();

			// Build field type.
			//
			foreach (var field in fields)
			{
				field.sb.Append(' ');

				if (!string.IsNullOrEmpty(field.field.DbType))
					field.sb.Append(field.field.DbType);
				else
					BuildCreateTableFieldType(field.sb, field.field);

				if (maxlen < field.sb.Length)
					maxlen = field.sb.Length;
			}

			appendToMax();

			var hasIdentity = fields.Any(f => f.field.IsIdentity);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					field.sb.Append(' ');

					if (field.field.IsIdentity)
						BuildCreateTableIdentityAttribute1(field.sb, field.field);

					if (maxlen < field.sb.Length)
						maxlen = field.sb.Length;
				}

				appendToMax();
			}

			// Build nullable attribute.
			//
			foreach (var field in fields)
			{
				field.sb.Append(' ');
				BuildCreateTableNullAttribute(field.sb, field.field);

				if (maxlen < field.sb.Length)
					maxlen = field.sb.Length;
			}

			appendToMax();

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					field.sb.Append(' ');

					if (field.field.IsIdentity)
						BuildCreateTableIdentityAttribute2(field.sb, field.field);

					if (maxlen < field.sb.Length)
						maxlen = field.sb.Length;
				}

				appendToMax();
			}

			// Build fields.
			//
			for (var i = 0; i < fields.Count; i++)
			{
				while (fields[i].sb.Length > 0 && fields[i].sb[fields[i].sb.Length - 1] == ' ')
					fields[i].sb.Length--;

				sb.AppendLine(i == 0 ? "" : ",");
				AppendIndent(sb);
				sb.Append(fields[i].sb);
			}

			var pk =
			(
				from f in fields
				where f.field.IsPrimaryKey
				orderby f.field.PrimaryKeyOrder
				select f
			).ToList();

			if (pk.Count > 0)
			{
				sb.AppendLine(",").AppendLine();

				BuildCreateTablePrimaryKey(sb,
					Convert("PK_" + SelectQuery.CreateTable.Table.PhysicalName, ConvertType.NameToQueryTable).ToString(),
					pk.Select(f => Convert(f.field.PhysicalName, ConvertType.NameToQueryField).ToString()));
			}

			Indent--;
			sb.AppendLine();
			AppendIndent(sb).AppendLine(")");
		}

		protected virtual void BuildCreateTableFieldType(StringBuilder sb, SqlField field)
		{
			BuildDataType(sb, new SqlDataType(
				field.DataType,
				field.SystemType,
				field.Length,
				field.Precision,
				field.Scale),
				createDbType : true);
		}

		protected virtual void BuildCreateTableNullAttribute(StringBuilder sb, SqlField field)
		{
			sb.Append(field.Nullable ? "    NULL" : "NOT NULL");
		}

		protected virtual void BuildCreateTableIdentityAttribute1(StringBuilder sb, SqlField field)
		{
		}

		protected virtual void BuildCreateTableIdentityAttribute2(StringBuilder sb, SqlField field)
		{
		}

		protected virtual void BuildCreateTablePrimaryKey(StringBuilder sb, string pkName, IEnumerable<string> fieldNames)
		{
			sb.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			sb.Append(fieldNames.Aggregate((f1,f2) => f1 + ", " + f2));
			sb.Append(")");
		}

		#endregion

		#region Build From

		protected virtual void BuildFromClause(StringBuilder sb)
		{
			if (SelectQuery.From.Tables.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("FROM").AppendLine();

			Indent++;
			AppendIndent(sb);

			var first = true;

			foreach (var ts in SelectQuery.From.Tables)
			{
				if (!first)
				{
					sb.AppendLine(",");
					AppendIndent(sb);
				}

				first = false;

				var jn = ParenthesizeJoin() ? ts.GetJoinNumber() : 0;

				if (jn > 0)
				{
					jn--;
					for (var i = 0; i < jn; i++)
						sb.Append("(");
				}

				BuildTableName(sb, ts, true, true);

				foreach (var jt in ts.Joins)
					BuildJoinTable(sb, jt, ref jn);
			}

			Indent--;

			sb.AppendLine();
		}

		protected void BuildPhysicalTable(StringBuilder sb, ISqlTableSource table, string alias)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable    :
				case QueryElementType.TableSource :
					sb.Append(GetPhysicalTableName(table, alias));
					break;

				case QueryElementType.SqlQuery    :
					sb.Append("(").AppendLine();
					BuildSqlBuilder((SelectQuery)table, sb, Indent + 1, false);
					AppendIndent(sb).Append(")");

					break;

				default:
					throw new InvalidOperationException();
			}
		}

		protected void BuildTableName(StringBuilder sb, SelectQuery.TableSource ts, bool buildName, bool buildAlias)
		{
			if (buildName)
			{
				var alias = GetTableAlias(ts);
				BuildPhysicalTable(sb, ts.Source, alias);
			}

			if (buildAlias)
			{
				if (ts.SqlTableType != SqlTableType.Expression)
				{
					var alias = GetTableAlias(ts);

					if (!string.IsNullOrEmpty(alias))
					{
						if (buildName)
							sb.Append(" ");
						sb.Append(Convert(alias, ConvertType.NameToQueryTableAlias));
					}
					
				}
			}
		}

		void BuildJoinTable(StringBuilder sb, SelectQuery.JoinedTable join, ref int joinCounter)
		{
			sb.AppendLine();
			Indent++;
			AppendIndent(sb);

			var buildOn = BuildJoinType(sb, join);

			if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
				sb.Append('(');

			BuildTableName(sb, join.Table, true, true);

			if (IsNestedJoinSupported && join.Table.Joins.Count != 0)
			{
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(sb, jt, ref joinCounter);

				if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
					sb.Append(')');

				if (buildOn)
				{
					sb.AppendLine();
					AppendIndent(sb);
					sb.Append("ON ");
				}
			}
			else if (buildOn)
				sb.Append(" ON ");

			if (buildOn)
			{
				if (join.Condition.Conditions.Count != 0)
					BuildSearchCondition(sb, Precedence.Unknown, join.Condition);
				else
					sb.Append("1=1");
			}

			if (joinCounter > 0)
			{
				joinCounter--;
				sb.Append(")");
			}

			if (!IsNestedJoinSupported)
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(sb, jt, ref joinCounter);

			Indent--;
		}

		protected virtual bool BuildJoinType(StringBuilder sb, SelectQuery.JoinedTable join)
		{
			switch (join.JoinType)
			{
				case SelectQuery.JoinType.Inner      : sb.Append("INNER JOIN ");  return true;
				case SelectQuery.JoinType.Left       : sb.Append("LEFT JOIN ");   return true;
				case SelectQuery.JoinType.CrossApply : sb.Append("CROSS APPLY "); return false;
				case SelectQuery.JoinType.OuterApply : sb.Append("OUTER APPLY "); return false;
				default: throw new InvalidOperationException();
			}
		}

		#endregion

		#region Where Clause

		protected virtual bool BuildWhere()
		{
			return SelectQuery.Where.SearchCondition.Conditions.Count != 0;
		}

		protected virtual void BuildWhereClause(StringBuilder sb)
		{
			if (!BuildWhere())
				return;

			AppendIndent(sb);

			sb.Append("WHERE").AppendLine();

			Indent++;
			AppendIndent(sb);
			BuildWhereSearchCondition(sb, SelectQuery.Where.SearchCondition);
			Indent--;

			sb.AppendLine();
		}

		#endregion

		#region GroupBy Clause

		protected virtual void BuildGroupByClause(StringBuilder sb)
		{
			if (SelectQuery.GroupBy.Items.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("GROUP BY").AppendLine();

			Indent++;

			for (var i = 0; i < SelectQuery.GroupBy.Items.Count; i++)
			{
				AppendIndent(sb);

				BuildExpression(sb, SelectQuery.GroupBy.Items[i]);

				if (i + 1 < SelectQuery.GroupBy.Items.Count)
					sb.Append(',');

				sb.AppendLine();
			}

			Indent--;
		}

		#endregion

		#region Having Clause

		protected virtual void BuildHavingClause(StringBuilder sb)
		{
			if (SelectQuery.Having.SearchCondition.Conditions.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("HAVING").AppendLine();

			Indent++;
			AppendIndent(sb);
			BuildWhereSearchCondition(sb, SelectQuery.Having.SearchCondition);
			Indent--;

			sb.AppendLine();
		}

		#endregion

		#region OrderBy Clause

		protected virtual void BuildOrderByClause(StringBuilder sb)
		{
			if (SelectQuery.OrderBy.Items.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("ORDER BY").AppendLine();

			Indent++;

			for (var i = 0; i < SelectQuery.OrderBy.Items.Count; i++)
			{
				AppendIndent(sb);

				var item = SelectQuery.OrderBy.Items[i];

				BuildExpression(sb, item.Expression);

				if (item.IsDescending)
					sb.Append(" DESC");

				if (i + 1 < SelectQuery.OrderBy.Items.Count)
					sb.Append(',');

				sb.AppendLine();
			}

			Indent--;
		}

		#endregion

		#region Skip/Take

		protected virtual bool   SkipFirst    { get { return true;  } }
		protected virtual string SkipFormat   { get { return null;  } }
		protected virtual string FirstFormat  { get { return null;  } }
		protected virtual string LimitFormat  { get { return null;  } }
		protected virtual string OffsetFormat { get { return null;  } }
		protected virtual bool   OffsetFirst  { get { return false; } }

		protected bool NeedSkip { get { return SelectQuery.Select.SkipValue != null && SqlProviderFlags.GetIsSkipSupportedFlag(SelectQuery); } }
		protected bool NeedTake { get { return SelectQuery.Select.TakeValue != null && SqlProviderFlags.IsTakeSupported; } }

		protected virtual void BuildSkipFirst(StringBuilder sb)
		{
			if (SkipFirst && NeedSkip && SkipFormat != null)
				sb.Append(' ').AppendFormat(SkipFormat,  BuildExpression(new StringBuilder(), SelectQuery.Select.SkipValue));

			if (NeedTake && FirstFormat != null)
				sb.Append(' ').AppendFormat(FirstFormat, BuildExpression(new StringBuilder(), SelectQuery.Select.TakeValue));

			if (!SkipFirst && NeedSkip && SkipFormat != null)
				sb.Append(' ').AppendFormat(SkipFormat,  BuildExpression(new StringBuilder(), SelectQuery.Select.SkipValue));
		}

		protected virtual void BuildOffsetLimit(StringBuilder sb)
		{
			var doSkip = NeedSkip && OffsetFormat != null;
			var doTake = NeedTake && LimitFormat  != null;

			if (doSkip || doTake)
			{
				AppendIndent(sb);

				if (doSkip && OffsetFirst)
				{
					sb.AppendFormat(OffsetFormat, BuildExpression(new StringBuilder(), SelectQuery.Select.SkipValue));

					if (doTake)
						sb.Append(' ');
				}

				if (doTake)
				{
					sb.AppendFormat(LimitFormat, BuildExpression(new StringBuilder(), SelectQuery.Select.TakeValue));

					if (doSkip)
						sb.Append(' ');
				}

				if (doSkip && !OffsetFirst)
					sb.AppendFormat(OffsetFormat, BuildExpression(new StringBuilder(), SelectQuery.Select.SkipValue));

				sb.AppendLine();
			}
		}

		#endregion

		#region Builders

		#region BuildSearchCondition

		protected virtual void BuildWhereSearchCondition(StringBuilder sb, SelectQuery.SearchCondition condition)
		{
			BuildSearchCondition(sb, Precedence.Unknown, condition);
		}

		protected virtual void BuildSearchCondition(StringBuilder sb, SelectQuery.SearchCondition condition)
		{
			var isOr = (bool?)null;
			var len  = sb.Length;
			var parentPrecedence = condition.Precedence + 1;

			foreach (var cond in condition.Conditions)
			{
				if (isOr != null)
				{
					sb.Append(isOr.Value ? " OR" : " AND");

					if (condition.Conditions.Count < 4 && sb.Length - len < 50 || condition != SelectQuery.Where.SearchCondition)
					{
						sb.Append(' ');
					}
					else
					{
						sb.AppendLine();
						AppendIndent(sb);
						len = sb.Length;
					}
				}

				if (cond.IsNot)
					sb.Append("NOT ");

				var precedence = GetPrecedence(cond.Predicate);

				BuildPredicate(sb, cond.IsNot ? Precedence.LogicalNegation : parentPrecedence, precedence, cond.Predicate);

				isOr = cond.IsOr;
			}
		}

		protected virtual void BuildSearchCondition(StringBuilder sb, int parentPrecedence, SelectQuery.SearchCondition condition)
		{
			var wrap = Wrap(GetPrecedence(condition as ISqlExpression), parentPrecedence);

			if (wrap) sb.Append('(');
			BuildSearchCondition(sb, condition);
			if (wrap) sb.Append(')');
		}

		#endregion

		#region BuildPredicate

		protected virtual void BuildPredicate(StringBuilder sb, ISqlPredicate predicate)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate :
					{
						var expr = (SelectQuery.Predicate.ExprExpr)predicate;

						switch (expr.Operator)
						{
							case SelectQuery.Predicate.Operator.Equal :
							case SelectQuery.Predicate.Operator.NotEqual :
								{
									ISqlExpression e = null;

									if (expr.Expr1 is IValueContainer && ((IValueContainer)expr.Expr1).Value == null)
										e = expr.Expr2;
									else if (expr.Expr2 is IValueContainer && ((IValueContainer)expr.Expr2).Value == null)
										e = expr.Expr1;

									if (e != null)
									{
										BuildExpression(sb, GetPrecedence(expr), e);
										sb.Append(expr.Operator == SelectQuery.Predicate.Operator.Equal ? " IS NULL" : " IS NOT NULL");
										return;
									}

									break;
								}
						}

						BuildExpression(sb, GetPrecedence(expr), expr.Expr1);

						switch (expr.Operator)
						{
							case SelectQuery.Predicate.Operator.Equal          : sb.Append(" = ");  break;
							case SelectQuery.Predicate.Operator.NotEqual       : sb.Append(" <> "); break;
							case SelectQuery.Predicate.Operator.Greater        : sb.Append(" > ");  break;
							case SelectQuery.Predicate.Operator.GreaterOrEqual : sb.Append(" >= "); break;
							case SelectQuery.Predicate.Operator.NotGreater     : sb.Append(" !> "); break;
							case SelectQuery.Predicate.Operator.Less           : sb.Append(" < ");  break;
							case SelectQuery.Predicate.Operator.LessOrEqual    : sb.Append(" <= "); break;
							case SelectQuery.Predicate.Operator.NotLess        : sb.Append(" !< "); break;
						}

						BuildExpression(sb, GetPrecedence(expr), expr.Expr2);
					}

					break;

				case QueryElementType.LikePredicate :
					BuildLikePredicate(sb, (SelectQuery.Predicate.Like)predicate);
					break;

				case QueryElementType.BetweenPredicate :
					{
						var p = (SelectQuery.Predicate.Between)predicate;
						BuildExpression(sb, GetPrecedence(p), p.Expr1);
						if (p.IsNot) sb.Append(" NOT");
						sb.Append(" BETWEEN ");
						BuildExpression(sb, GetPrecedence(p), p.Expr2);
						sb.Append(" AND ");
						BuildExpression(sb, GetPrecedence(p), p.Expr3);
					}

					break;

				case QueryElementType.IsNullPredicate :
					{
						var p = (SelectQuery.Predicate.IsNull)predicate;
						BuildExpression(sb, GetPrecedence(p), p.Expr1);
						sb.Append(p.IsNot ? " IS NOT NULL" : " IS NULL");
					}

					break;

				case QueryElementType.InSubQueryPredicate :
					{
						var p = (SelectQuery.Predicate.InSubQuery)predicate;
						BuildExpression(sb, GetPrecedence(p), p.Expr1);
						sb.Append(p.IsNot ? " NOT IN " : " IN ");
						BuildExpression(sb, GetPrecedence(p), p.SubQuery);
					}

					break;

				case QueryElementType.InListPredicate :
					BuildInListPredicate(predicate, sb);
					break;

				case QueryElementType.FuncLikePredicate :
					{
						var f = (SelectQuery.Predicate.FuncLike)predicate;
						BuildExpression(sb, f.Function.Precedence, f.Function);
					}

					break;

				case QueryElementType.SearchCondition :
					BuildSearchCondition(sb, predicate.Precedence, (SelectQuery.SearchCondition)predicate);
					break;

				case QueryElementType.NotExprPredicate :
					{
						var p = (SelectQuery.Predicate.NotExpr)predicate;

						if (p.IsNot)
							sb.Append("NOT ");

						BuildExpression(sb, p.IsNot ? Precedence.LogicalNegation : GetPrecedence(p), p.Expr1);
					}

					break;

				case QueryElementType.ExprPredicate :
					{
						var p = (SelectQuery.Predicate.Expr)predicate;

						if (p.Expr1 is SqlValue)
						{
							var value = ((SqlValue)p.Expr1).Value;

							if (value is bool)
							{
								sb.Append((bool)value ? "1 = 1" : "1 = 0");
								return;
							}
						}

						BuildExpression(sb, GetPrecedence(p), p.Expr1);
					}

					break;

				default :
					throw new InvalidOperationException();
			}
		}

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column  : return GetUnderlayingField(((SelectQuery.Column)expr).Expression);
			}

			throw new InvalidOperationException();
		}

		void BuildInListPredicate(ISqlPredicate predicate, StringBuilder sb)
		{
			var p = (SelectQuery.Predicate.InList)predicate;

			if (p.Values == null || p.Values.Count == 0)
			{
				BuildPredicate(sb, new SelectQuery.Predicate.Expr(new SqlValue(false)));
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
						BuildPredicate(sb, new SelectQuery.Predicate.Expr(new SqlValue(false)));
						return;
					}

					if (pr.Value is IEnumerable)
					{
						var items = (IEnumerable)pr.Value;

						if (p.Expr1 is ISqlTableSource)
						{
							var firstValue = true;
							var table      = (ISqlTableSource)p.Expr1;
							var keys       = table.GetKeys(true);

							if (keys == null || keys.Count == 0)
								throw new SqlException("Cannot create IN expression.");

							if (keys.Count == 1)
							{
								foreach (var item in items)
								{
									if (firstValue)
									{
										firstValue = false;
										BuildExpression(sb, GetPrecedence(p), keys[0]);
										sb.Append(p.IsNot ? " NOT IN (" : " IN (");
									}

									var field = GetUnderlayingField(keys[0]);
									var value = field.ColumnDescriptor.MemberAccessor.GetValue(item);

									if (value is ISqlExpression)
										BuildExpression(sb, (ISqlExpression)value);
									else
										BuildValue(sb, value);

									sb.Append(", ");
								}
							}
							else
							{
								var len = sb.Length;
								var rem = 1;

								foreach (var item in items)
								{
									if (firstValue)
									{
										firstValue = false;
										sb.Append('(');
									}

									foreach (var key in keys)
									{
										var field = GetUnderlayingField(key);
										var value = field.ColumnDescriptor.MemberAccessor.GetValue(item);

										BuildExpression(sb, GetPrecedence(p), key);

										if (value == null)
										{
											sb.Append(" IS NULL");
										}
										else
										{
											sb.Append(" = ");
											BuildValue(sb, value);
										}

										sb.Append(" AND ");
									}

									sb.Remove(sb.Length - 4, 4).Append("OR ");

									if (sb.Length - len >= 50)
									{
										sb.AppendLine();
										AppendIndent(sb);
										sb.Append(' ');
										len = sb.Length;
										rem = 5 + Indent;
									}
								}

								if (!firstValue)
									sb.Remove(sb.Length - rem, rem);
							}

							if (firstValue)
								BuildPredicate(sb, new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot)));
							else
								sb.Remove(sb.Length - 2, 2).Append(')');
						}
						else
						{
							BuildInListValues(sb, p, items);
						}

						return;
					}
				}

				BuildInListValues(sb, p, values);
			}
		}

		void BuildInListValues(StringBuilder sb, SelectQuery.Predicate.InList predicate, IEnumerable values)
		{
			var firstValue = true;
			var len        = sb.Length;
			var hasNull    = false;
			var count      = 0;
			var longList   = false;

			foreach (var value in values)
			{
				if (count++ >= SqlProviderFlags.MaxInListValuesCount)
				{
					count    = 1;
					longList = true;

					// start building next bucked
					firstValue = true;
					sb.Remove(sb.Length - 2, 2).Append(')');
					sb.Append(" OR ");
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
					BuildExpression(sb, GetPrecedence(predicate), predicate.Expr1);
					sb.Append(predicate.IsNot ? " NOT IN (" : " IN (");
				}

				if (value is ISqlExpression)
					BuildExpression(sb, (ISqlExpression)value);
				else
					BuildValue(sb, value);

				sb.Append(", ");
			}

			if (firstValue)
			{
				BuildPredicate(sb,
					hasNull ?
						new SelectQuery.Predicate.IsNull(predicate.Expr1, predicate.IsNot) :
						new SelectQuery.Predicate.Expr(new SqlValue(predicate.IsNot)));
			}
			else
			{
				sb.Remove(sb.Length - 2, 2).Append(')');

				if (hasNull)
				{
					sb.Insert(len, "(");
					sb.Append(" OR ");
					BuildPredicate(sb, new SelectQuery.Predicate.IsNull(predicate.Expr1, predicate.IsNot));
					sb.Append(")");
				}
			}

			if (longList && !hasNull)
			{
				sb.Insert(len, "(");
				sb.Append(")");
			}
		}

		protected void BuildPredicate(StringBuilder sb, int parentPrecedence, ISqlPredicate predicate)
		{
			BuildPredicate(sb, parentPrecedence, GetPrecedence(predicate), predicate);
		}

		protected void BuildPredicate(StringBuilder sb, int parentPrecedence, int precedence, ISqlPredicate predicate)
		{
			var wrap = Wrap(precedence, parentPrecedence);

			if (wrap) sb.Append('(');
			BuildPredicate(sb, predicate);
			if (wrap) sb.Append(')');
		}

		protected virtual void BuildLikePredicate(StringBuilder sb, SelectQuery.Predicate.Like predicate)
		{
			var precedence = GetPrecedence(predicate);

			BuildExpression(sb, precedence, predicate.Expr1);
			sb.Append(predicate.IsNot? " NOT LIKE ": " LIKE ");
			BuildExpression(sb, precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				sb.Append(" ESCAPE ");
				BuildExpression(sb, predicate.Escape);
			}
		}

		#endregion

		#region BuildExpression

		protected virtual StringBuilder BuildExpression(
			StringBuilder  sb,
			ISqlExpression expr,
			bool           buildTableName,
			bool           checkParentheses,
			string         alias,
			ref bool       addAlias)
		{
			// TODO: check the necessity.
			//
			expr = SqlOptimizer.ConvertExpression(expr);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlField:
					{
						var field = (SqlField)expr;

						if (field == field.Table.All)
						{
							sb.Append("*");
						}
						else
						{
							if (buildTableName)
							{
								var ts = SelectQuery.GetTableSource(field.Table);

								if (ts == null)
								{
#if DEBUG
									//SqlQuery.GetTableSource(field.Table);
#endif

									throw new SqlException("Table '{0}' not found.", field.Table);
								}

								var table = GetTableAlias(ts);

								table = table == null ?
									GetPhysicalTableName(field.Table, null) :
									Convert(table, ConvertType.NameToQueryTableAlias).ToString();

								if (string.IsNullOrEmpty(table))
									throw new SqlException("Table {0} should have an alias.", field.Table);

								addAlias = alias != field.PhysicalName;

								sb
									.Append(table)
									.Append('.');
							}

							sb.Append(Convert(field.PhysicalName, ConvertType.NameToQueryField));
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

						sb
							.Append(Convert(tableAlias, ConvertType.NameToQueryTableAlias))
							.Append('.')
							.Append(Convert(column.Alias, ConvertType.NameToQueryField));
					}

					break;

				case QueryElementType.SqlQuery:
					{
						var hasParentheses = checkParentheses && sb[sb.Length - 1] == '(';

						if (!hasParentheses)
							sb.Append("(");
						sb.AppendLine();

						BuildSqlBuilder((SelectQuery)expr, sb, Indent + 1, BuildStep != Step.FromClause);

						AppendIndent(sb);

						if (!hasParentheses)
							sb.Append(")");
					}

					break;

				case QueryElementType.SqlValue:
					BuildValue(sb, ((SqlValue)expr).Value);
					break;

				case QueryElementType.SqlExpression:
					{
						var e = (SqlExpression)expr;
						var s = new StringBuilder();

						if (e.Parameters == null || e.Parameters.Length == 0)
							sb.Append(e.Expr);
						else
						{
							var values = new object[e.Parameters.Length];

							for (var i = 0; i < values.Length; i++)
							{
								var value = e.Parameters[i];

								s.Length = 0;
								BuildExpression(s, GetPrecedence(e), value);
								values[i] = s.ToString();
							}

							sb.AppendFormat(e.Expr, values);
						}
					}

					break;

				case QueryElementType.SqlBinaryExpression:
					BuildBinaryExpression(sb, (SqlBinaryExpression)expr);
					break;

				case QueryElementType.SqlFunction:
					BuildFunction(sb, (SqlFunction)expr);
					break;

				case QueryElementType.SqlParameter:
					{
						var parm = (SqlParameter)expr;

						if (parm.IsQueryParameter)
						{
							var name = Convert(parm.Name, ConvertType.NameToQueryParameter);
							sb.Append(name);
						}
						else
							BuildValue(sb, parm.Value);
					}

					break;

				case QueryElementType.SqlDataType:
					BuildDataType(sb, (SqlDataType)expr);
					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(sb, expr.Precedence, (SelectQuery.SearchCondition)expr);
					break;

				default:
					throw new InvalidOperationException();
			}

			return sb;
		}

		protected void BuildExpression(StringBuilder sb, int parentPrecedence, ISqlExpression expr, string alias, ref bool addAlias)
		{
			var wrap = Wrap(GetPrecedence(expr), parentPrecedence);

			if (wrap) sb.Append('(');
			BuildExpression(sb, expr, true, true, alias, ref addAlias);
			if (wrap) sb.Append(')');
		}

		protected StringBuilder BuildExpression(StringBuilder sb, ISqlExpression expr)
		{
			var dummy = false;
			return BuildExpression(sb, expr, true, true, null, ref dummy);
		}

		protected StringBuilder BuildExpression(StringBuilder sb, ISqlExpression expr, bool buildTableName, bool checkParentheses)
		{
			var dummy = false;
			return BuildExpression(sb, expr, buildTableName, checkParentheses, null, ref dummy);
		}

		protected void BuildExpression(StringBuilder sb, int precedence, ISqlExpression expr)
		{
			var dummy = false;
			BuildExpression(sb, precedence, expr, null, ref dummy);
		}

		#endregion

		#region BuildValue

		interface INullableValueReader
		{
			object GetValue(object value);
		}

		class NullableValueReader<T> : INullableValueReader where T : struct
		{
			public object GetValue(object value)
			{
				return ((T?)value).Value;
			}
		}

		static readonly Dictionary<Type,INullableValueReader> _nullableValueReader = new Dictionary<Type,INullableValueReader>();

		public NumberFormatInfo NumberFormatInfo = new NumberFormatInfo
		{
			CurrencyDecimalDigits    = NumberFormatInfo.InvariantInfo.CurrencyDecimalDigits,
			CurrencyDecimalSeparator = NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator,
			CurrencyGroupSeparator   = NumberFormatInfo.InvariantInfo.CurrencyGroupSeparator,
			CurrencyGroupSizes       = NumberFormatInfo.InvariantInfo.CurrencyGroupSizes,
			CurrencyNegativePattern  = NumberFormatInfo.InvariantInfo.CurrencyNegativePattern,
			CurrencyPositivePattern  = NumberFormatInfo.InvariantInfo.CurrencyPositivePattern,
			CurrencySymbol           = NumberFormatInfo.InvariantInfo.CurrencySymbol,
			NaNSymbol                = NumberFormatInfo.InvariantInfo.NaNSymbol,
			NegativeInfinitySymbol   = NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol,
			NegativeSign             = NumberFormatInfo.InvariantInfo.NegativeSign,
			NumberDecimalDigits      = NumberFormatInfo.InvariantInfo.NumberDecimalDigits,
			NumberDecimalSeparator   = ".",
			NumberGroupSeparator     = NumberFormatInfo.InvariantInfo.NumberGroupSeparator,
			NumberGroupSizes         = NumberFormatInfo.InvariantInfo.NumberGroupSizes,
			NumberNegativePattern    = NumberFormatInfo.InvariantInfo.NumberNegativePattern,
			PercentDecimalDigits     = NumberFormatInfo.InvariantInfo.PercentDecimalDigits,
			PercentDecimalSeparator  = ".",
			PercentGroupSeparator    = NumberFormatInfo.InvariantInfo.PercentGroupSeparator,
			PercentGroupSizes        = NumberFormatInfo.InvariantInfo.PercentGroupSizes,
			PercentNegativePattern   = NumberFormatInfo.InvariantInfo.PercentNegativePattern,
			PercentPositivePattern   = NumberFormatInfo.InvariantInfo.PercentPositivePattern,
			PercentSymbol            = NumberFormatInfo.InvariantInfo.PercentSymbol,
			PerMilleSymbol           = NumberFormatInfo.InvariantInfo.PerMilleSymbol,
			PositiveInfinitySymbol   = NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol,
			PositiveSign             = NumberFormatInfo.InvariantInfo.PositiveSign,
		};

		public virtual void BuildValue(StringBuilder sb, object value)
		{
			if      (value == null)     sb.Append("NULL");
			else if (value is string)   BuildString(sb, value.ToString());
			else if (value is char)     BuildChar  (sb, (char)value);
			else if (value is bool)     sb.Append((bool)value ? "1" : "0");
			else if (value is DateTime) BuildDateTime(sb, value);
			else if (value is Guid)     sb.Append('\'').Append(value).Append('\'');
			else if (value is decimal)  sb.Append(((decimal)value).ToString(NumberFormatInfo));
			else if (value is double)   sb.Append(((double) value).ToString(NumberFormatInfo));
			else if (value is float)    sb.Append(((float)  value).ToString(NumberFormatInfo));
			else
			{
				var type = value.GetType();

				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					type = type.GetGenericArguments()[0];

					if (type.IsEnum)
					{
						lock (_nullableValueReader)
						{
							INullableValueReader reader;

							if (_nullableValueReader.TryGetValue(type, out reader) == false)
							{
								reader = (INullableValueReader)Activator.CreateInstance(typeof(NullableValueReader<>).MakeGenericType(type));
								_nullableValueReader.Add(type, reader);
							}

							value = reader.GetValue(value);
						}
					}
				}

				sb.Append(value);
			}
		}

		protected virtual void BuildString(StringBuilder sb, string value)
		{
			sb
				.Append('\'')
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		protected virtual void BuildChar(StringBuilder sb, char value)
		{
			sb.Append('\'');

			if (value == '\'') sb.Append("''");
			else               sb.Append(value);

			sb.Append('\'');
		}

		protected virtual void BuildDateTime(StringBuilder sb, object value)
		{
			sb.Append(string.Format("'{0:yyyy-MM-dd HH:mm:ss.fff}'", value));
		}

		#endregion

		#region BuildBinaryExpression

		protected virtual void BuildBinaryExpression(StringBuilder sb, SqlBinaryExpression expr)
		{
			BuildBinaryExpression(sb, expr.Operation, expr);
		}

		protected void BuildFunction(StringBuilder sb, string name, SqlBinaryExpression expr)
		{
			sb.Append(name);
			sb.Append("(");
			BuildExpression(sb, expr.Expr1);
			sb.Append(", ");
			BuildExpression(sb, expr.Expr2);
			sb.Append(')');
		}

		protected void BuildBinaryExpression(StringBuilder sb, string op, SqlBinaryExpression expr)
		{
			if (expr.Operation == "*" && expr.Expr1 is SqlValue)
			{
				var value = (SqlValue)expr.Expr1;

				if (value.Value is int && (int)value.Value == -1)
				{
					sb.Append('-');
					BuildExpression(sb, GetPrecedence(expr), expr.Expr2);
					return;
				}
			}

			BuildExpression(sb, GetPrecedence(expr), expr.Expr1);
			sb.Append(' ').Append(op).Append(' ');
			BuildExpression(sb, GetPrecedence(expr), expr.Expr2);
		}

		#endregion

		#region BuildFunction

		protected virtual void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			if (func.Name == "CASE")
			{
				sb.Append(func.Name).AppendLine();

				Indent++;

				var i = 0;

				for (; i < func.Parameters.Length - 1; i += 2)
				{
					AppendIndent(sb).Append("WHEN ");

					var len = sb.Length;

					BuildExpression(sb, func.Parameters[i]);

					if (SqlExpression.NeedsEqual(func.Parameters[i]))
					{
						sb.Append(" = ");
						BuildValue(sb, true);
					}

					if (sb.Length - len > 20)
					{
						sb.AppendLine();
						AppendIndent(sb).Append("\tTHEN ");
					}
					else
						sb.Append(" THEN ");

					BuildExpression(sb, func.Parameters[i+1]);
					sb.AppendLine();
				}

				if (i < func.Parameters.Length)
				{
					AppendIndent(sb).Append("ELSE ");
					BuildExpression(sb, func.Parameters[i]);
					sb.AppendLine();
				}

				Indent--;

				AppendIndent(sb).Append("END");
			}
			else
				BuildFunction(sb, func.Name, func.Parameters);
		}

		protected void BuildFunction(StringBuilder sb, string name, ISqlExpression[] exprs)
		{
			sb.Append(name).Append('(');

			var first = true;

			foreach (var parameter in exprs)
			{
				if (!first)
					sb.Append(", ");

				BuildExpression(sb, parameter, true, !first || name == "EXISTS");

				first = false;
			}

			sb.Append(')');
		}

		#endregion

		#region BuildDataType
	
		protected virtual void BuildDataType(StringBuilder sb, SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.Double  : sb.Append("Float");    return;
				case DataType.Single  : sb.Append("Real");     return;
				case DataType.SByte   : sb.Append("TinyInt");  return;
				case DataType.UInt16  : sb.Append("Int");      return;
				case DataType.UInt32  : sb.Append("BigInt");   return;
				case DataType.UInt64  : sb.Append("Decimal");  return;
				case DataType.Byte    : sb.Append("TinyInt");  return;
				case DataType.Int16   : sb.Append("SmallInt"); return;
				case DataType.Int32   : sb.Append("Int");      return;
				case DataType.Int64   : sb.Append("BigInt");   return;
				case DataType.Boolean : sb.Append("Bit");      return;
			}

			sb.Append(type.DataType.ToString());

			if (type.Length > 0)
				sb.Append('(').Append(type.Length).Append(')');

			if (type.Precision > 0)
				sb.Append('(').Append(type.Precision).Append(',').Append(type.Scale).Append(')');
		}

		#endregion

		#region GetPrecedence

		protected virtual int GetPrecedence(ISqlExpression expr)
		{
			return expr.Precedence;
		}

		protected virtual int GetPrecedence(ISqlPredicate predicate)
		{
			return predicate.Precedence;
		}

		#endregion

		#endregion

		#region Internal Types

		public enum Step
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

		protected virtual void BuildAliases(StringBuilder sb, string table, List<SelectQuery.Column> columns, string postfix)
		{
			Indent++;

			var first = true;

			foreach (var col in columns)
			{
				if (!first)
					sb.Append(',').AppendLine();
				first = false;

				AppendIndent(sb).AppendFormat("{0}.{1}", table, Convert(col.Alias, ConvertType.NameToQueryFieldAlias));

				if (postfix != null)
					sb.Append(postfix);
			}

			Indent--;

			sb.AppendLine();
		}

		protected void AlternativeBuildSql(StringBuilder sb, bool implementOrderBy, Action<StringBuilder> buildSql)
		{
			if (NeedSkip)
			{
				var aliases  = GetTempAliases(2, "t");
				var rnaliase = GetTempAliases(1, "rn")[0];

				AppendIndent(sb).Append("SELECT *").AppendLine();
				AppendIndent(sb).Append("FROM").    AppendLine();
				AppendIndent(sb).Append("(").       AppendLine();
				Indent++;

				AppendIndent(sb).Append("SELECT").AppendLine();

				Indent++;
				AppendIndent(sb).AppendFormat("{0}.*,", aliases[0]).AppendLine();
				AppendIndent(sb).Append("ROW_NUMBER() OVER");

				if (!SelectQuery.OrderBy.IsEmpty && !implementOrderBy)
					sb.Append("()");
				else
				{
					sb.AppendLine();
					AppendIndent(sb).Append("(").AppendLine();

					Indent++;

					if (SelectQuery.OrderBy.IsEmpty)
					{
						AppendIndent(sb).Append("ORDER BY").AppendLine();
						BuildAliases(sb, aliases[0], SelectQuery.Select.Columns.Take(1).ToList(), null);
					}
					else
						BuildAlternativeOrderBy(sb, true);

					Indent--;
					AppendIndent(sb).Append(")");
				}

				sb.Append(" as ").Append(rnaliase).AppendLine();
				Indent--;

				AppendIndent(sb).Append("FROM").AppendLine();
				AppendIndent(sb).Append("(").AppendLine();

				Indent++;
				buildSql(sb);
				Indent--;

				AppendIndent(sb).AppendFormat(") {0}", aliases[0]).AppendLine();

				Indent--;

				AppendIndent(sb).AppendFormat(") {0}", aliases[1]).AppendLine();
				AppendIndent(sb).Append("WHERE").AppendLine();

				Indent++;

				if (NeedTake)
				{
					var expr1 = Add(SelectQuery.Select.SkipValue, 1);
					var expr2 = Add<int>(SelectQuery.Select.SkipValue, SelectQuery.Select.TakeValue);

					if (expr1 is SqlValue && expr2 is SqlValue && Equals(((SqlValue)expr1).Value, ((SqlValue)expr2).Value))
					{
						AppendIndent(sb).AppendFormat("{0}.{1} = ", aliases[1], rnaliase);
						BuildExpression(sb, expr1);
					}
					else
					{
						AppendIndent(sb).AppendFormat("{0}.{1} BETWEEN ", aliases[1], rnaliase);
						BuildExpression(sb, expr1);
						sb.Append(" AND ");
						BuildExpression(sb, expr2);
					}
				}
				else
				{
					AppendIndent(sb).AppendFormat("{0}.{1} > ", aliases[1], rnaliase);
					BuildExpression(sb, SelectQuery.Select.SkipValue);
				}

				sb.AppendLine();
				Indent--;
			}
			else
				buildSql(sb);
		}

		protected void AlternativeBuildSql2(StringBuilder sb, Action<StringBuilder> buildSql)
		{
			var aliases = GetTempAliases(3, "t");

			AppendIndent(sb).Append("SELECT *").AppendLine();
			AppendIndent(sb).Append("FROM")    .AppendLine();
			AppendIndent(sb).Append("(")       .AppendLine();
			Indent++;

			AppendIndent(sb).Append("SELECT TOP ");
			BuildExpression(sb, SelectQuery.Select.TakeValue);
			sb.Append(" *").AppendLine();
			AppendIndent(sb).Append("FROM").AppendLine();
			AppendIndent(sb).Append("(")   .AppendLine();
			Indent++;

			if (SelectQuery.OrderBy.IsEmpty)
			{
				AppendIndent(sb).Append("SELECT TOP ");

				var p = SelectQuery.Select.SkipValue as SqlParameter;

				if (p != null && !p.IsQueryParameter && SelectQuery.Select.TakeValue is SqlValue)
					BuildValue(sb, (int)p.Value + (int)((SqlValue)(SelectQuery.Select.TakeValue)).Value);
				else
					BuildExpression(sb, Add<int>(SelectQuery.Select.SkipValue, SelectQuery.Select.TakeValue));

				sb.Append(" *").AppendLine();
				AppendIndent(sb).Append("FROM").AppendLine();
				AppendIndent(sb).Append("(")   .AppendLine();
				Indent++;
			}

			buildSql(sb);

			if (SelectQuery.OrderBy.IsEmpty)
			{
				Indent--;
				AppendIndent(sb).AppendFormat(") {0}", aliases[2]).AppendLine();
				AppendIndent(sb).Append("ORDER BY").AppendLine();
				BuildAliases(sb, aliases[2], SelectQuery.Select.Columns, null);
			}

			Indent--;
			AppendIndent(sb).AppendFormat(") {0}", aliases[1]).AppendLine();

			if (SelectQuery.OrderBy.IsEmpty)
			{
				AppendIndent(sb).Append("ORDER BY").AppendLine();
				BuildAliases(sb, aliases[1], SelectQuery.Select.Columns, " DESC");
			}
			else
			{
				BuildAlternativeOrderBy(sb, false);
			}

			Indent--;
			AppendIndent(sb).AppendFormat(") {0}", aliases[0]).AppendLine();

			if (SelectQuery.OrderBy.IsEmpty)
			{
				AppendIndent(sb).Append("ORDER BY").AppendLine();
				BuildAliases(sb, aliases[0], SelectQuery.Select.Columns, null);
			}
			else
			{
				BuildAlternativeOrderBy(sb, true);
			}
		}

		protected void BuildAlternativeOrderBy(StringBuilder sb, bool ascending)
		{
			AppendIndent(sb).Append("ORDER BY").AppendLine();

			var obys = GetTempAliases(SelectQuery.OrderBy.Items.Count, "oby");

			Indent++;

			for (var i = 0; i < obys.Length; i++)
			{
				AppendIndent(sb).Append(obys[i]);

				if ( ascending &&  SelectQuery.OrderBy.Items[i].IsDescending ||
					!ascending && !SelectQuery.OrderBy.Items[i].IsDescending)
					sb.Append(" DESC");

				if (i + 1 < obys.Length)
					sb.Append(',');

				sb.AppendLine();
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

		protected bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType   : return ((SqlDataType)  expr).DataType == DataType.Date;
				case QueryElementType.SqlExpression : return ((SqlExpression)expr).Expr     == dateName;
			}

			return false;
		}

		protected bool IsTimeDataType(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType   : return ((SqlDataType)expr).  DataType == DataType.Time;
				case QueryElementType.SqlExpression : return ((SqlExpression)expr).Expr     == "Time";
			}

			return false;
		}

		protected SelectQuery GetAlternativeDelete(SelectQuery selectQuery)
		{
			if (selectQuery.IsDelete && 
				(selectQuery.From.Tables.Count > 1 || selectQuery.From.Tables[0].Joins.Count > 0) && 
				selectQuery.From.Tables[0].Source is SqlTable)
			{
				var sql = new SelectQuery { QueryType = QueryType.Delete };

				selectQuery.ParentSelect = sql;
				selectQuery.QueryType = QueryType.Select;

				var table = (SqlTable)selectQuery.From.Tables[0].Source;
				var copy  = new SqlTable(table) { Alias = null };

				var tableKeys = table.GetKeys(true);
				var copyKeys  = copy. GetKeys(true);

				if (selectQuery.Where.SearchCondition.Conditions.Any(c => c.IsOr))
				{
					var sc1 = new SelectQuery.SearchCondition(selectQuery.Where.SearchCondition.Conditions);
					var sc2 = new SelectQuery.SearchCondition();

					for (var i = 0; i < tableKeys.Count; i++)
					{
						sc2.Conditions.Add(new SelectQuery.Condition(
							false,
							new SelectQuery.Predicate.ExprExpr(copyKeys[i], SelectQuery.Predicate.Operator.Equal, tableKeys[i])));
					}

					selectQuery.Where.SearchCondition.Conditions.Clear();
					selectQuery.Where.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, sc1));
					selectQuery.Where.SearchCondition.Conditions.Add(new SelectQuery.Condition(false, sc2));
				}
				else
				{
					for (var i = 0; i < tableKeys.Count; i++)
						selectQuery.Where.Expr(copyKeys[i]).Equal.Expr(tableKeys[i]);
				}

				sql.From.Table(copy).Where.Exists(selectQuery);
				sql.Parameters.AddRange(selectQuery.Parameters);

				selectQuery.Parameters.Clear();

				selectQuery = sql;
			}

			return selectQuery;
		}

		protected SelectQuery GetAlternativeUpdate(SelectQuery selectQuery)
		{
			if (selectQuery.IsUpdate && (selectQuery.From.Tables[0].Source is SqlTable || selectQuery.Update.Table != null))
			{
				if (selectQuery.From.Tables.Count > 1 || selectQuery.From.Tables[0].Joins.Count > 0)
				{
					var sql = new SelectQuery { QueryType = QueryType.Update };

					selectQuery.ParentSelect = sql;
					selectQuery.QueryType = QueryType.Select;

					var table = selectQuery.Update.Table ?? (SqlTable)selectQuery.From.Tables[0].Source;

					if (selectQuery.Update.Table != null)
						if (new QueryVisitor().Find(selectQuery.From, t => t == table) == null)
							table = (SqlTable)new QueryVisitor().Find(selectQuery.From,
								ex => ex is SqlTable && ((SqlTable)ex).ObjectType == table.ObjectType) ?? table;

					var copy = new SqlTable(table);

					var tableKeys = table.GetKeys(true);
					var copyKeys  = copy. GetKeys(true);

					for (var i = 0; i < tableKeys.Count; i++)
						selectQuery.Where
							.Expr(copyKeys[i]).Equal.Expr(tableKeys[i]);

					sql.From.Table(copy).Where.Exists(selectQuery);

					var map = new Dictionary<SqlField,SqlField>(table.Fields.Count);

					foreach (var field in table.Fields.Values)
						map.Add(field, copy[field.Name]);

					foreach (var item in selectQuery.Update.Items)
					{
						var ex = new QueryVisitor().Convert(item, expr =>
						{
							var fld = expr as SqlField;
							return fld != null && map.TryGetValue(fld, out fld) ? fld : expr;
						});

						sql.Update.Items.Add(ex);
					}

					sql.Parameters.AddRange(selectQuery.Parameters);
					sql.Update.Table = selectQuery.Update.Table;

					selectQuery.Parameters.Clear();
					selectQuery.Update.Items.Clear();

					selectQuery = sql;
				}

				selectQuery.From.Tables[0].Alias = "$";
			}

			return selectQuery;
		}

		static bool IsBooleanParameter(ISqlExpression expr, int count, int i)
		{
			if ((i % 2 == 1 || i == count - 1) && expr.SystemType == typeof(bool) || expr.SystemType == typeof(bool?))
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SearchCondition : return true;
				}
			}

			return false;
		}

		protected SqlFunction ConvertFunctionParameters(SqlFunction func)
		{
			if (func.Name == "CASE" &&
				func.Parameters.Select((p,i) => new { p, i }).Any(p => IsBooleanParameter(p.p, func.Parameters.Length, p.i)))
			{
				return new SqlFunction(
					func.SystemType,
					func.Name,
					func.Precedence,
					func.Parameters.Select((p,i) =>
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

		static string SetAlias(string alias, int maxLen)
		{
			if (alias == null)
				return null;

			alias = alias.TrimStart('_');

			var cs      = alias.ToCharArray();
			var replace = false;

			for (var i = 0; i < cs.Length; i++)
			{
				var c = cs[i];

				if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')
					continue;

				cs[i] = ' ';
				replace = true;
			}

			if (replace)
				alias = new string(cs).Replace(" ", "");

			return alias.Length == 0 || alias.Length > maxLen ? null : alias;
		}

		protected void CheckAliases(SelectQuery selectQuery, int maxLen)
		{
			new QueryVisitor().Visit(selectQuery, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField     : ((SqlField)               e).Alias = SetAlias(((SqlField)            e).Alias, maxLen); break;
					case QueryElementType.SqlParameter : ((SqlParameter)           e).Name  = SetAlias(((SqlParameter)        e).Name,  maxLen); break;
					case QueryElementType.SqlTable     : ((SqlTable)               e).Alias = SetAlias(((SqlTable)            e).Alias, maxLen); break;
					case QueryElementType.Column       : ((SelectQuery.Column)     e).Alias = SetAlias(((SelectQuery.Column)     e).Alias, maxLen); break;
					case QueryElementType.TableSource  : ((SelectQuery.TableSource)e).Alias = SetAlias(((SelectQuery.TableSource)e).Alias, maxLen); break;
				}
			});
		}

		static bool Wrap(int precedence, int parentPrecedence)
		{
			return
				precedence == 0 ||
				precedence < parentPrecedence ||
				(precedence == parentPrecedence && 
					(parentPrecedence == Precedence.Subtraction ||
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
				case QueryElementType.TableSource :
					var ts    = (SelectQuery.TableSource)table;
					var alias = string.IsNullOrEmpty(ts.Alias) ? GetTableAlias(ts.Source) : ts.Alias;
					return alias != "$" ? alias : null;

				case QueryElementType.SqlTable :
					return ((SqlTable)table).Alias;

				default :
					throw new InvalidOperationException();
			}
		}

		string GetPhysicalTableName(ISqlTableSource table, string alias)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable :
					{
						var tbl = (SqlTable)table;

						var database     = tbl.Database     == null ? null : Convert(tbl.Database,     ConvertType.NameToDatabase).  ToString();
						var owner        = tbl.Owner        == null ? null : Convert(tbl.Owner,        ConvertType.NameToOwner).     ToString();
						var physicalName = tbl.PhysicalName == null ? null : Convert(tbl.PhysicalName, ConvertType.NameToQueryTable).ToString();

						var sb = new StringBuilder();

						if (tbl.SqlTableType == SqlTableType.Expression)
						{
							if (tbl.TableArguments == null)
								physicalName = tbl.PhysicalName;
							else
							{
								var values = new object[tbl.TableArguments.Length + 2];

								values[0] = physicalName;
								values[1] = Convert(alias, ConvertType.NameToQueryTableAlias);

								for (var i = 2; i < values.Length; i++)
								{
									var value = tbl.TableArguments[i - 2];

									sb.Length = 0;
									BuildExpression(sb, Precedence.Primary, value);
									values[i] = sb.ToString();
								}

								physicalName = string.Format(tbl.Name, values);

								sb.Length = 0;
							}
						}

						BuildTableName(sb, database, owner, physicalName);

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

									BuildExpression(sb, arg, true, !first);

									first = false;
								}
							}

							sb.Append(')');
						}

						return sb.ToString();
					}

				case QueryElementType.TableSource :
					return GetPhysicalTableName(((SelectQuery.TableSource)table).Source, alias);

				default :
					throw new InvalidOperationException();
			}
		}

		protected StringBuilder AppendIndent(StringBuilder sb)
		{
			if (Indent > 0)
				sb.Append('\t', Indent);

			return sb;
		}

		public ISqlExpression Add(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return SqlOptimizer.ConvertExpression(new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive));
		}

		public ISqlExpression Add<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		public ISqlExpression Add(ISqlExpression expr1, int value)
		{
			return Add<int>(expr1, new SqlValue(value));
		}

		#endregion

		#region DataTypes

		protected virtual int GetMaxLength     (SqlDataType type) { return SqlDataType.GetMaxLength     (type.DataType); }
		protected virtual int GetMaxPrecision  (SqlDataType type) { return SqlDataType.GetMaxPrecision  (type.DataType); }
		protected virtual int GetMaxScale      (SqlDataType type) { return SqlDataType.GetMaxScale      (type.DataType); }
		protected virtual int GetMaxDisplaySize(SqlDataType type) { return SqlDataType.GetMaxDisplaySize(type.DataType); }

		#endregion

		#region ISqlProvider Members

		public virtual SelectQuery Finalize(SelectQuery selectQuery)
		{
			selectQuery.FinalizeAndValidate(SqlProviderFlags.IsApplyJoinSupported, IsGroupByExpressionSupported);

			if (!SqlProviderFlags.IsCountSubQuerySupported)  selectQuery = MoveCountSubQuery (selectQuery);
			if (!SqlProviderFlags.IsSubQueryColumnSupported) selectQuery = MoveSubQueryColumn(selectQuery);

			if (!SqlProviderFlags.IsCountSubQuerySupported || !SqlProviderFlags.IsSubQueryColumnSupported)
				selectQuery.FinalizeAndValidate(SqlProviderFlags.IsApplyJoinSupported, IsGroupByExpressionSupported);

			return selectQuery;
		}

		SelectQuery MoveCountSubQuery(SelectQuery selectQuery)
		{
			new QueryVisitor().Visit(selectQuery, MoveCountSubQuery);
			return selectQuery;
		}

		void MoveCountSubQuery(IQueryElement element)
		{
			if (element.ElementType != QueryElementType.SqlQuery)
				return;

			var query = (SelectQuery)element;

			for (var i = 0; i < query.Select.Columns.Count; i++)
			{
				var col = query.Select.Columns[i];

				// The column is a subquery.
				//
				if (col.Expression.ElementType == QueryElementType.SqlQuery)
				{
					var subQuery = (SelectQuery)col.Expression;
					var isCount  = false;

					// Check if subquery is Count subquery.
					//
					if (subQuery.Select.Columns.Count == 1)
					{
						var subCol = subQuery.Select.Columns[0];

						if (subCol.Expression.ElementType == QueryElementType.SqlFunction)
							isCount = ((SqlFunction)subCol.Expression).Name == "Count";
					}

					if (!isCount)
						continue;

					// Check if subquery where clause does not have ORs.
					//
					SelectQuery.OptimizeSearchCondition(subQuery.Where.SearchCondition);

					var allAnd = true;

					for (var j = 0; allAnd && j < subQuery.Where.SearchCondition.Conditions.Count - 1; j++)
					{
						var cond = subQuery.Where.SearchCondition.Conditions[j];

						if (cond.IsOr)
							allAnd = false;
					}

					if (!allAnd || !ConvertCountSubQuery(subQuery))
						continue;

					// Collect tables.
					//
					var allTables   = new HashSet<ISqlTableSource>();
					var levelTables = new HashSet<ISqlTableSource>();

					new QueryVisitor().Visit(subQuery, e =>
					{
						if (e is ISqlTableSource)
							allTables.Add((ISqlTableSource)e);
					});

					new QueryVisitor().Visit(subQuery, e =>
					{
						if (e is ISqlTableSource)
							if (subQuery.From.IsChild((ISqlTableSource)e))
								levelTables.Add((ISqlTableSource)e);
					});

					Func<IQueryElement,bool> checkTable = e =>
					{
						switch (e.ElementType)
						{
							case QueryElementType.SqlField : return !allTables.Contains(((SqlField)       e).Table);
							case QueryElementType.Column   : return !allTables.Contains(((SelectQuery.Column)e).Parent);
						}
						return false;
					};

					var join = SelectQuery.LeftJoin(subQuery);

					query.From.Tables[0].Joins.Add(join.JoinedTable);

					for (var j = 0; j < subQuery.Where.SearchCondition.Conditions.Count; j++)
					{
						var cond = subQuery.Where.SearchCondition.Conditions[j];

						if (new QueryVisitor().Find(cond, checkTable) == null)
							continue;

						var replaced = new Dictionary<IQueryElement,IQueryElement>();

						var nc = new QueryVisitor().Convert(cond, e =>
						{
							var ne = e;

							switch (e.ElementType)
							{
								case QueryElementType.SqlField :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SqlField)e).Table))
									{
										subQuery.GroupBy.Expr((SqlField)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SqlField)e)];
									}

									break;

								case QueryElementType.Column   :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SelectQuery.Column)e).Parent))
									{
										subQuery.GroupBy.Expr((SelectQuery.Column)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SelectQuery.Column)e)];
									}

									break;
							}

							if (!ReferenceEquals(e, ne))
								replaced.Add(e, ne);

							return ne;
						});

						if (nc != null && !ReferenceEquals(nc, cond))
						{
							join.JoinedTable.Condition.Conditions.Add(nc);
							subQuery.Where.SearchCondition.Conditions.RemoveAt(j);
							j--;
						}
					}

					if (!query.GroupBy.IsEmpty/* && subQuery.Select.Columns.Count > 1*/)
					{
						var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

						subQuery.Select.Columns.RemoveAt(0);

						query.Select.Columns[i].Expression = 
							new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[0]);
					}
					else
					{
						query.Select.Columns[i].Expression = subQuery.Select.Columns[0];
					}
				}
			}
		}

		SelectQuery MoveSubQueryColumn(SelectQuery selectQuery)
		{
			var dic = new Dictionary<IQueryElement,IQueryElement>();

			new QueryVisitor().Visit(selectQuery, element =>
			{
				if (element.ElementType != QueryElementType.SqlQuery)
					return;

				var query = (SelectQuery)element;

				for (var i = 0; i < query.Select.Columns.Count; i++)
				{
					var col = query.Select.Columns[i];

					if (col.Expression.ElementType == QueryElementType.SqlQuery)
					{
						var subQuery    = (SelectQuery)col.Expression;
						var allTables   = new HashSet<ISqlTableSource>();
						var levelTables = new HashSet<ISqlTableSource>();

						Func<IQueryElement,bool> checkTable = e =>
						{
							switch (e.ElementType)
							{
								case QueryElementType.SqlField : return !allTables.Contains(((SqlField)e).Table);
								case QueryElementType.Column   : return !allTables.Contains(((SelectQuery.Column)e).Parent);
							}
							return false;
						};

						new QueryVisitor().Visit(subQuery, e =>
						{
							if (e is ISqlTableSource)
								allTables.Add((ISqlTableSource)e);
						});

						new QueryVisitor().Visit(subQuery, e =>
						{
							if (e is ISqlTableSource && subQuery.From.IsChild((ISqlTableSource)e))
								levelTables.Add((ISqlTableSource)e);
						});

						if (SqlProviderFlags.IsSubQueryColumnSupported && new QueryVisitor().Find(subQuery, checkTable) == null)
							continue;

						var join = SelectQuery.LeftJoin(subQuery);

						query.From.Tables[0].Joins.Add(join.JoinedTable);

						SelectQuery.OptimizeSearchCondition(subQuery.Where.SearchCondition);

						var isCount      = false;
						var isAggregated = false;
						
						if (subQuery.Select.Columns.Count == 1)
						{
							var subCol = subQuery.Select.Columns[0];

							if (subCol.Expression.ElementType == QueryElementType.SqlFunction)
							{
								switch (((SqlFunction)subCol.Expression).Name)
								{
									case "Min"     :
									case "Max"     :
									case "Sum"     :
									case "Average" : isAggregated = true;                 break;
									case "Count"   : isAggregated = true; isCount = true; break;
								}
							}
						}

						if (SqlProviderFlags.IsSubQueryColumnSupported && !isCount)
							continue;

						var allAnd = true;

						for (var j = 0; allAnd && j < subQuery.Where.SearchCondition.Conditions.Count - 1; j++)
						{
							var cond = subQuery.Where.SearchCondition.Conditions[j];

							if (cond.IsOr)
								allAnd = false;
						}

						if (!allAnd)
							continue;

						var modified = false;

						for (var j = 0; j < subQuery.Where.SearchCondition.Conditions.Count; j++)
						{
							var cond = subQuery.Where.SearchCondition.Conditions[j];

							if (new QueryVisitor().Find(cond, checkTable) == null)
								continue;

							var replaced = new Dictionary<IQueryElement,IQueryElement>();

							var nc = new QueryVisitor().Convert(cond, delegate(IQueryElement e)
							{
								var ne = e;

								switch (e.ElementType)
								{
									case QueryElementType.SqlField :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SqlField)e).Table))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SqlField)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SqlField)e)];
										}

										break;

									case QueryElementType.Column   :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SelectQuery.Column)e).Parent))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SelectQuery.Column)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SelectQuery.Column)e)];
										}

										break;
								}

								if (!ReferenceEquals(e, ne))
									replaced.Add(e, ne);

								return ne;
							});

							if (nc != null && !ReferenceEquals(nc, cond))
							{
								modified = true;

								join.JoinedTable.Condition.Conditions.Add(nc);
								subQuery.Where.SearchCondition.Conditions.RemoveAt(j);
								j--;
							}
						}

						if (modified || isAggregated)
						{
							if (isCount && !query.GroupBy.IsEmpty)
							{
								var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

								subQuery.Select.Columns.RemoveAt(0);

								query.Select.Columns[i] = new SelectQuery.Column(
									query,
									new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[0]));
							}
							else if (isAggregated && !query.GroupBy.IsEmpty)
							{
								var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

								subQuery.Select.Columns.RemoveAt(0);

								var idx = subQuery.Select.Add(oldFunc.Parameters[0]);

								query.Select.Columns[i] = new SelectQuery.Column(
									query,
									new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[idx]));
							}
							else
							{
								query.Select.Columns[i] = new SelectQuery.Column(query, subQuery.Select.Columns[0]);
							}

							dic.Add(col, query.Select.Columns[i]);
						}
					}
				}
			});

			selectQuery = new QueryVisitor().Convert(selectQuery, e =>
			{
				IQueryElement ne;
				return dic.TryGetValue(e, out ne) ? ne : e;
			});

			return selectQuery;
		}

		public virtual ISqlExpression GetIdentityExpression(SqlTable table, SqlField identityField, bool forReturning)
		{
			return null;
		}

		private        string _name;
		public virtual string  Name
		{
			get { return _name ?? (_name = GetType().Name.Replace("SqlProvider", "")); }
		}

		#endregion
	}
}
