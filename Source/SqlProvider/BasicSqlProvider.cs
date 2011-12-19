using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.SqlProvider
{
	using Extensions;
	using Mapping;
	using Data.Linq;
	using SqlBuilder;

	public abstract class BasicSqlProvider : ISqlProvider
	{
		#region Init

		private SqlQuery _sqlQuery;
		public  SqlQuery  SqlQuery
		{
			get { return _sqlQuery;  }
			set { _sqlQuery = value; }
		}

		private int _indent;
		public  int  Indent
		{
			get { return _indent;  }
			set { _indent = value; }
		}

		private int _nextNesting = 1;
		private int _nesting;
		public  int  Nesting
		{
			get { return _nesting; }
		}

		bool _skipAlias;

		private Step _buildStep;
		public  Step  BuildStep
		{
			get { return _buildStep;  }
			set { _buildStep = value; }
		}

		#endregion

		#region Support Flags

		public virtual bool SkipAcceptsParameter            { get { return true;  } }
		public virtual bool TakeAcceptsParameter            { get { return true;  } }
		public virtual bool IsTakeSupported                 { get { return true;  } }
		public virtual bool IsSkipSupported                 { get { return true;  } }
		public virtual bool IsSubQueryTakeSupported         { get { return true;  } }
		public virtual bool IsSubQueryColumnSupported       { get { return true;  } }
		public virtual bool IsCountSubQuerySupported        { get { return true;  } }
		public virtual bool IsNestedJoinSupported           { get { return true;  } }
		public virtual bool IsNestedJoinParenthesisRequired { get { return false; } }
		public virtual bool IsIdentityParameterRequired     { get { return false; } }
		public virtual bool IsApplyJoinSupported            { get { return false; } }
		public virtual bool IsInsertOrUpdateSupported       { get { return true;  } }
		public virtual bool CanCombineParameters            { get { return true;  } }
		public virtual bool IsGroupByExpressionSupported    { get { return true;  } }

		public virtual bool ConvertCountSubQuery(SqlQuery subQuery)
		{
			return true;
		}

		#endregion

		#region CommandCount

		public virtual int CommandCount(SqlQuery sqlQuery)
		{
			return 1;
		}

		#endregion

		#region BuildSql

		public virtual int BuildSql(int commandNumber, SqlQuery sqlQuery, StringBuilder sb, int indent, int nesting, bool skipAlias)
		{
			_sqlQuery    = sqlQuery;
			_indent      = indent;
			_nesting     = nesting;
			_nextNesting = _nesting + 1;
			_skipAlias   = skipAlias;

			if (commandNumber == 0)
			{
				BuildSql(sb);

				if (sqlQuery.HasUnion)
				{
					foreach (var union in sqlQuery.Unions)
					{
						AppendIndent(sb);
						sb.Append("UNION");
						if (union.IsAll) sb.Append(" ALL");
						sb.AppendLine();

						CreateSqlProvider().BuildSql(commandNumber, union.SqlQuery, sb, indent, nesting, skipAlias);
					}
				}
			}
			else
			{
				BuildCommand(commandNumber, sb);
			}

			return _nextNesting;
		}

		protected virtual void BuildCommand(int commandNumber, StringBuilder sb)
		{
		}

		#endregion

		#region Overrides

		protected virtual int BuildSqlBuilder(SqlQuery sqlQuery, StringBuilder sb, int indent, int nesting, bool skipAlias)
		{
			if (!IsSkipSupported && sqlQuery.Select.SkipValue != null)
				throw new SqlException("Skip for subqueries is not supported by the '{0}' provider.", Name);

			if (!IsTakeSupported && sqlQuery.Select.TakeValue != null)
				throw new SqlException("Take for subqueries is not supported by the '{0}' provider.", Name);

			return CreateSqlProvider().BuildSql(0, sqlQuery, sb, indent, nesting, skipAlias);
		}

		protected abstract ISqlProvider CreateSqlProvider();

		protected virtual bool ParenthesizeJoin()
		{
			return false;
		}

		protected virtual void BuildSql(StringBuilder sb)
		{
			switch (_sqlQuery.QueryType)
			{
				case QueryType.Select         : BuildSelectQuery        (sb); break;
				case QueryType.Delete         : BuildDeleteQuery        (sb); break;
				case QueryType.Update         : BuildUpdateQuery        (sb); break;
				case QueryType.Insert         : BuildInsertQuery        (sb); break;
				case QueryType.InsertOrUpdate : BuildInsertOrUpdateQuery(sb); break;
				default                       : BuildUnknownQuery       (sb); break;
			}
		}

		protected virtual void BuildDeleteQuery(StringBuilder sb)
		{
			_buildStep = Step.DeleteClause;  BuildDeleteClause (sb);
			_buildStep = Step.FromClause;    BuildFromClause   (sb);
			_buildStep = Step.WhereClause;   BuildWhereClause  (sb);
			_buildStep = Step.GroupByClause; BuildGroupByClause(sb);
			_buildStep = Step.HavingClause;  BuildHavingClause (sb);
			_buildStep = Step.OrderByClause; BuildOrderByClause(sb);
			_buildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
		}

		protected virtual void BuildUpdateQuery(StringBuilder sb)
		{
			_buildStep = Step.UpdateClause;  BuildUpdateClause (sb);
			_buildStep = Step.FromClause;    BuildFromClause   (sb);
			_buildStep = Step.WhereClause;   BuildWhereClause  (sb);
			_buildStep = Step.GroupByClause; BuildGroupByClause(sb);
			_buildStep = Step.HavingClause;  BuildHavingClause (sb);
			_buildStep = Step.OrderByClause; BuildOrderByClause(sb);
			_buildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
		}

		protected virtual void BuildSelectQuery(StringBuilder sb)
		{
			_buildStep = Step.SelectClause;  BuildSelectClause (sb);
			_buildStep = Step.FromClause;    BuildFromClause   (sb);
			_buildStep = Step.WhereClause;   BuildWhereClause  (sb);
			_buildStep = Step.GroupByClause; BuildGroupByClause(sb);
			_buildStep = Step.HavingClause;  BuildHavingClause (sb);
			_buildStep = Step.OrderByClause; BuildOrderByClause(sb);
			_buildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
		}

		protected virtual void BuildInsertQuery(StringBuilder sb)
		{
			_buildStep = Step.InsertClause; BuildInsertClause(sb);

			if (_sqlQuery.QueryType == QueryType.Insert && _sqlQuery.From.Tables.Count != 0)
			{
				_buildStep = Step.SelectClause;  BuildSelectClause (sb);
				_buildStep = Step.FromClause;    BuildFromClause   (sb);
				_buildStep = Step.WhereClause;   BuildWhereClause  (sb);
				_buildStep = Step.GroupByClause; BuildGroupByClause(sb);
				_buildStep = Step.HavingClause;  BuildHavingClause (sb);
				_buildStep = Step.OrderByClause; BuildOrderByClause(sb);
				_buildStep = Step.OffsetLimit;   BuildOffsetLimit  (sb);
			}

			if (SqlQuery.Insert.WithIdentity)
				BuildGetIdentity(sb);
		}

		protected virtual void BuildUnknownQuery(StringBuilder sb)
		{
			throw new SqlException("Unknown query type '{0}'.", _sqlQuery.QueryType);
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

			if (SqlQuery.Select.IsDistinct)
				sb.Append(" DISTINCT");

			BuildSkipFirst(sb);

			sb.AppendLine();
			BuildColumns(sb);
		}

		protected virtual IEnumerable<SqlQuery.Column> GetSelectedColumns()
		{
			return _sqlQuery.Select.Columns;
		}

		protected virtual void BuildColumns(StringBuilder sb)
		{
			_indent++;

			var first = true;

			foreach (var col in GetSelectedColumns())
			{
				if (!first)
					sb.Append(',').AppendLine();
				first = false;

				var addAlias = true;

				AppendIndent(sb);
				BuildColumn(sb, col, ref addAlias);

				if (!_skipAlias && addAlias && col.Alias != null)
					sb.Append(" as ").Append(Convert(col.Alias, ConvertType.NameToQueryFieldAlias));
			}

			if (first)
				AppendIndent(sb).Append("*");

			_indent--;

			sb.AppendLine();
		}

		protected virtual void BuildColumn(StringBuilder sb, SqlQuery.Column col, ref bool addAlias)
		{
			BuildExpression(sb, col.Expression, true, true, col.Alias, ref addAlias);
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
			if (SqlQuery.Update.Table != null && SqlQuery.Update.Table != SqlQuery.From.Tables[0].Source)
				BuildPhysicalTable(sb, SqlQuery.Update.Table, null);
			else
				BuildTableName(sb, SqlQuery.From.Tables[0], true, true);
		}

		protected virtual void BuildUpdateSet(StringBuilder sb)
		{
			AppendIndent(sb)
				.AppendLine("SET");

			_indent++;

			var first = true;

			foreach (var expr in _sqlQuery.Update.Items)
			{
				if (!first)
					sb.Append(',').AppendLine();
				first = false;

				AppendIndent(sb);
				BuildExpression(sb, expr.Column, false, true);
				sb.Append(" = ");
				BuildExpression(sb, expr.Expression);
			}

			_indent--;

			sb.AppendLine();
		}

		#endregion

		#region Build Insert

		protected void BuildInsertClause(StringBuilder sb)
		{
			BuildInsertClause(sb, "INSERT INTO ", true);
		}

		protected virtual void BuildInsertClause(StringBuilder sb, string insertText, bool appendTableName)
		{
			AppendIndent(sb).Append(insertText);
			if (appendTableName)
				BuildPhysicalTable(sb, SqlQuery.Insert.Into, null);
			sb.AppendLine(" ");

			AppendIndent(sb).AppendLine("(");

			_indent++;

			var first = true;

			foreach (var expr in _sqlQuery.Insert.Items)
			{
				if (!first)
					sb.Append(',').AppendLine();
				first = false;

				AppendIndent(sb);
				BuildExpression(sb, expr.Column, false, true);
			}

			_indent--;

			sb.AppendLine();
			AppendIndent(sb).AppendLine(")");

			if (_sqlQuery.QueryType == QueryType.InsertOrUpdate || _sqlQuery.From.Tables.Count == 0)
			{
				AppendIndent(sb).AppendLine("VALUES");
				AppendIndent(sb).AppendLine("(");

				_indent++;

				first = true;

				foreach (var expr in _sqlQuery.Insert.Items)
				{
					if (!first)
						sb.Append(',').AppendLine();
					first = false;

					AppendIndent(sb);
					BuildExpression(sb, expr.Expression);
				}

				_indent--;

				sb.AppendLine();
				AppendIndent(sb).AppendLine(")");
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
			var table       = SqlQuery.Insert.Into;
			var targetAlias = Convert(SqlQuery.From.Tables[0].Alias, ConvertType.NameToQueryTableAlias).ToString();
			var sourceAlias = Convert(GetTempAliases(1, "s")[0],     ConvertType.NameToQueryTableAlias).ToString();
			var keys        = SqlQuery.Update.Keys;

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

			var alias = Convert(SqlQuery.From.Tables[0].Alias, ConvertType.NameToQueryTableAlias).ToString();
			var exprs = SqlQuery.Update.Keys;

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

		#region Build From

		protected virtual void BuildFromClause(StringBuilder sb)
		{
			if (_sqlQuery.From.Tables.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("FROM").AppendLine();

			_indent++;
			AppendIndent(sb);

			var first = true;

			foreach (var ts in _sqlQuery.From.Tables)
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

			_indent--;

			sb.AppendLine();
		}

		protected void BuildPhysicalTable(StringBuilder sb, ISqlTableSource table, string alias)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable    :
				case QueryElementType.TableSource :
					sb.Append(GetTablePhysicalName(table, alias));
					break;

				case QueryElementType.SqlQuery    :
					sb.Append("(").AppendLine();
					_nextNesting = BuildSqlBuilder((SqlQuery)table, sb, _indent + 1, _nextNesting, false);
					AppendIndent(sb).Append(")");

					break;

				default:
					throw new InvalidOperationException();
			}
		}

		protected void BuildTableName(StringBuilder sb, SqlQuery.TableSource ts, bool buildName, bool buildAlias)
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

		void BuildJoinTable(StringBuilder sb, SqlQuery.JoinedTable join, ref int joinCounter)
		{
			sb.AppendLine();
			_indent++;
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

			_indent--;
		}

		protected virtual bool BuildJoinType(StringBuilder sb, SqlQuery.JoinedTable join)
		{
			switch (join.JoinType)
			{
				case SqlQuery.JoinType.Inner      : sb.Append("INNER JOIN ");  return true;
				case SqlQuery.JoinType.Left       : sb.Append("LEFT JOIN ");   return true;
				case SqlQuery.JoinType.CrossApply : sb.Append("CROSS APPLY "); return false;
				case SqlQuery.JoinType.OuterApply : sb.Append("OUTER APPLY "); return false;
				default: throw new InvalidOperationException();
			}
		}

		#endregion

		#region Where Clause

		protected virtual bool BuildWhere()
		{
			return _sqlQuery.Where.SearchCondition.Conditions.Count != 0;
		}

		protected virtual void BuildWhereClause(StringBuilder sb)
		{
			if (!BuildWhere())
				return;

			AppendIndent(sb);

			sb.Append("WHERE").AppendLine();

			_indent++;
			AppendIndent(sb);
			BuildWhereSearchCondition(sb, _sqlQuery.Where.SearchCondition);
			_indent--;

			sb.AppendLine();
		}

		#endregion

		#region GroupBy Clause

		protected virtual void BuildGroupByClause(StringBuilder sb)
		{
			if (_sqlQuery.GroupBy.Items.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("GROUP BY").AppendLine();

			_indent++;

			for (var i = 0; i < _sqlQuery.GroupBy.Items.Count; i++)
			{
				AppendIndent(sb);

				BuildExpression(sb, _sqlQuery.GroupBy.Items[i]);

				if (i + 1 < _sqlQuery.GroupBy.Items.Count)
					sb.Append(',');

				sb.AppendLine();
			}

			_indent--;
		}

		#endregion

		#region Having Clause

		protected virtual void BuildHavingClause(StringBuilder sb)
		{
			if (_sqlQuery.Having.SearchCondition.Conditions.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("HAVING").AppendLine();

			_indent++;
			AppendIndent(sb);
			BuildWhereSearchCondition(sb, _sqlQuery.Having.SearchCondition);
			_indent--;

			sb.AppendLine();
		}

		#endregion

		#region OrderBy Clause

		protected virtual void BuildOrderByClause(StringBuilder sb)
		{
			if (_sqlQuery.OrderBy.Items.Count == 0)
				return;

			AppendIndent(sb);

			sb.Append("ORDER BY").AppendLine();

			_indent++;

			for (var i = 0; i < _sqlQuery.OrderBy.Items.Count; i++)
			{
				AppendIndent(sb);

				var item = _sqlQuery.OrderBy.Items[i];

				BuildExpression(sb, item.Expression);

				if (item.IsDescending)
					sb.Append(" DESC");

				if (i + 1 < _sqlQuery.OrderBy.Items.Count)
					sb.Append(',');

				sb.AppendLine();
			}

			_indent--;
		}

		#endregion

		#region Skip/Take

		protected virtual bool   SkipFirst    { get { return true;  } }
		protected virtual string SkipFormat   { get { return null;  } }
		protected virtual string FirstFormat  { get { return null;  } }
		protected virtual string LimitFormat  { get { return null;  } }
		protected virtual string OffsetFormat { get { return null;  } }
		protected virtual bool   OffsetFirst  { get { return false; } }

		protected bool NeedSkip { get { return SqlQuery.Select.SkipValue != null && IsSkipSupported; } }
		protected bool NeedTake { get { return SqlQuery.Select.TakeValue != null && IsTakeSupported; } }

		protected virtual void BuildSkipFirst(StringBuilder sb)
		{
			if (SkipFirst && NeedSkip && SkipFormat != null)
				sb.Append(' ').AppendFormat(SkipFormat,  BuildExpression(new StringBuilder(), SqlQuery.Select.SkipValue));

			if (NeedTake && FirstFormat != null)
				sb.Append(' ').AppendFormat(FirstFormat, BuildExpression(new StringBuilder(), SqlQuery.Select.TakeValue));

			if (!SkipFirst && NeedSkip && SkipFormat != null)
				sb.Append(' ').AppendFormat(SkipFormat,  BuildExpression(new StringBuilder(), SqlQuery.Select.SkipValue));
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
					sb.AppendFormat(OffsetFormat, BuildExpression(new StringBuilder(), SqlQuery.Select.SkipValue));

					if (doTake)
						sb.Append(' ');
				}

				if (doTake)
				{
					sb.AppendFormat(LimitFormat, BuildExpression(new StringBuilder(), SqlQuery.Select.TakeValue));

					if (doSkip)
						sb.Append(' ');
				}

				if (doSkip && !OffsetFirst)
					sb.AppendFormat(OffsetFormat, BuildExpression(new StringBuilder(), SqlQuery.Select.SkipValue));

				sb.AppendLine();
			}
		}

		#endregion

		#region Builders

		#region BuildSearchCondition

		protected virtual void BuildWhereSearchCondition(StringBuilder sb, SqlQuery.SearchCondition condition)
		{
			BuildSearchCondition(sb, Precedence.Unknown, condition);
		}

		protected virtual void BuildSearchCondition(StringBuilder sb, SqlQuery.SearchCondition condition)
		{
			var isOr = (bool?)null;
			var len  = sb.Length;
			var parentPrecedence = condition.Precedence + 1;

			foreach (var cond in condition.Conditions)
			{
				if (isOr != null)
				{
					sb.Append(isOr.Value ? " OR" : " AND");

					if (condition.Conditions.Count < 4 && sb.Length - len < 50 || condition != _sqlQuery.Where.SearchCondition)
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

		protected virtual void BuildSearchCondition(StringBuilder sb, int parentPrecedence, SqlQuery.SearchCondition condition)
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
						var expr = (SqlQuery.Predicate.ExprExpr)predicate;

						switch (expr.Operator)
						{
							case SqlQuery.Predicate.Operator.Equal :
							case SqlQuery.Predicate.Operator.NotEqual :
								{
									ISqlExpression e = null;

									if (expr.Expr1 is SqlValue && ((SqlValue)expr.Expr1).Value == null)
										e = expr.Expr2;
									else if (expr.Expr2 is SqlValue && ((SqlValue)expr.Expr2).Value == null)
										e = expr.Expr1;

									if (e != null)
									{
										BuildExpression(sb, GetPrecedence(expr), e);
										sb.Append(expr.Operator == SqlQuery.Predicate.Operator.Equal ? " IS NULL" : " IS NOT NULL");
										return;
									}

									break;
								}
						}

						BuildExpression(sb, GetPrecedence(expr), expr.Expr1);

						switch (expr.Operator)
						{
							case SqlQuery.Predicate.Operator.Equal          : sb.Append(" = ");  break;
							case SqlQuery.Predicate.Operator.NotEqual       : sb.Append(" <> "); break;
							case SqlQuery.Predicate.Operator.Greater        : sb.Append(" > ");  break;
							case SqlQuery.Predicate.Operator.GreaterOrEqual : sb.Append(" >= "); break;
							case SqlQuery.Predicate.Operator.NotGreater     : sb.Append(" !> "); break;
							case SqlQuery.Predicate.Operator.Less           : sb.Append(" < ");  break;
							case SqlQuery.Predicate.Operator.LessOrEqual    : sb.Append(" <= "); break;
							case SqlQuery.Predicate.Operator.NotLess        : sb.Append(" !< "); break;
						}

						BuildExpression(sb, GetPrecedence(expr), expr.Expr2);
					}

					break;

				case QueryElementType.LikePredicate :
					BuildLikePredicate(sb, (SqlQuery.Predicate.Like)predicate);
					break;

				case QueryElementType.BetweenPredicate :
					{
						var p = (SqlQuery.Predicate.Between)predicate;
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
						var p = (SqlQuery.Predicate.IsNull)predicate;
						BuildExpression(sb, GetPrecedence(p), p.Expr1);
						sb.Append(p.IsNot ? " IS NOT NULL" : " IS NULL");
					}

					break;

				case QueryElementType.InSubQueryPredicate :
					{
						var p = (SqlQuery.Predicate.InSubQuery)predicate;
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
						var f = (SqlQuery.Predicate.FuncLike)predicate;
						BuildExpression(sb, f.Function.Precedence, f.Function);
					}

					break;

				case QueryElementType.SearchCondition :
					BuildSearchCondition(sb, predicate.Precedence, (SqlQuery.SearchCondition)predicate);
					break;

				case QueryElementType.NotExprPredicate :
					{
						var p = (SqlQuery.Predicate.NotExpr)predicate;

						if (p.IsNot)
							sb.Append("NOT ");

						BuildExpression(sb, p.IsNot ? Precedence.LogicalNegation : GetPrecedence(p), p.Expr1);
					}

					break;

				case QueryElementType.ExprPredicate :
					{
						var p = (SqlQuery.Predicate.Expr)predicate;

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
				case QueryElementType.Column  : return GetUnderlayingField(((SqlQuery.Column)expr).Expression);
			}

			throw new InvalidOperationException();
		}

		void BuildInListPredicate(ISqlPredicate predicate, StringBuilder sb)
		{
			var p = (SqlQuery.Predicate.InList)predicate;

			if (p.Values == null || p.Values.Count == 0)
			{
				BuildPredicate(sb, new SqlQuery.Predicate.Expr(new SqlValue(false)));
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
						BuildPredicate(sb, new SqlQuery.Predicate.Expr(new SqlValue(false)));
						return;
					}

					if (pr.Value is IEnumerable)
					{
						var items      = (IEnumerable)pr.Value;
						var firstValue = true;

						if (p.Expr1 is ISqlTableSource)
						{
							var table = (ISqlTableSource)p.Expr1;
							var keys  = table.GetKeys(true);

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
									var value = field.MemberMapper.GetValue(item);

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
										var value = field.MemberMapper.GetValue(item);

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
										rem = 5 + _indent;
									}
								}

								if (!firstValue)
									sb.Remove(sb.Length - rem, rem);
							}
						}
						else
							foreach (var item in items)
							{
								if (firstValue)
								{
									firstValue = false;
									BuildExpression(sb, GetPrecedence(p), p.Expr1);
									sb.Append(p.IsNot ? " NOT IN (" : " IN (");
								}

								if (item is ISqlExpression)
									BuildExpression(sb, (ISqlExpression)item);
								else
									BuildValue(sb, item);

								sb.Append(", ");
							}

						if (firstValue)
							BuildPredicate(sb, new SqlQuery.Predicate.Expr(new SqlValue(p.IsNot)));
						else
							sb.Remove(sb.Length - 2, 2).Append(')');

						return;
					}
				}

				BuildExpression(sb, GetPrecedence(p), p.Expr1);
				sb.Append(p.IsNot ? " NOT IN (" : " IN (");

				foreach (var value in values)
				{
					if (value is ISqlExpression)
						BuildExpression(sb, (ISqlExpression)value);
					else
						BuildValue(sb, value);

					sb.Append(", ");
				}

				sb.Remove(sb.Length - 2, 2).Append(')');
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

		protected virtual void BuildLikePredicate(StringBuilder sb, SqlQuery.Predicate.Like predicate)
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
			expr = ConvertExpression(expr);

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
								var ts = _sqlQuery.GetTableSource(field.Table);

								if (ts == null)
								{
#if DEBUG
									_sqlQuery.GetTableSource(field.Table);
#endif

									throw new SqlException(string.Format("Table {0} not found.", field.Table));
								}

								var table = GetTableAlias(ts);

								table = table == null ?
									GetTablePhysicalName(field.Table, null) :
									Convert(table, ConvertType.NameToQueryTableAlias).ToString();

								if (string.IsNullOrEmpty(table))
									throw new SqlException(string.Format("Table {0} should have an alias.", field.Table));

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
						var column = (SqlQuery.Column)expr;

#if DEBUG
						//if (column.ToString() == "t8.ParentID")
						//{
						//    column.ToString();
						//}

						var sql = _sqlQuery.SqlText;
#endif

						var table = _sqlQuery.GetTableSource(column.Parent);

						if (table == null)
						{
#if DEBUG
							table = _sqlQuery.GetTableSource(column.Parent);
#endif

							throw new SqlException(string.Format("Table not found for '{0}'.", column));
						}

						var tableAlias = GetTableAlias(table) ?? GetTablePhysicalName(column.Parent, null);

						if (string.IsNullOrEmpty(tableAlias))
							throw new SqlException(string.Format("Table {0} should have an alias.", column.Parent));

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

						_nextNesting = BuildSqlBuilder((SqlQuery)expr, sb, _indent + 1, _nextNesting, _buildStep != Step.FromClause);

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
					BuildSearchCondition(sb, expr.Precedence, (SqlQuery.SearchCondition)expr);
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
			if      (value == null)                   sb.Append("NULL");
			else if (value is string)                 BuildString(sb, value.ToString());
			else if (value is char || value is char?) sb.Append('\'').Append(value.ToString().Replace("'", "''")).Append('\'');
			else if (value is bool || value is bool?) sb.Append((bool)value ? "1" : "0");
			else if (value is DateTime)               sb.AppendFormat("'{0:yyyy-MM-dd HH:mm:ss.fffffff}'", value);
			else if (value is Guid)                   sb.Append('\'').Append(value).Append('\'');
			else if (value is decimal)                sb.Append(((decimal)value).ToString(NumberFormatInfo));
			else if (value is double)                 sb.Append(((double) value).ToString(NumberFormatInfo));
			else if (value is float)                  sb.Append(((float)  value).ToString(NumberFormatInfo));
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

				if (type.IsEnum)
				{
					value = Map.DefaultSchema.MapEnumToValue(value);

					if (value != null && !value.GetType().IsEnum)
						BuildValue(sb, value);
					else
						sb.Append(value);
				}
				else
					sb.Append(value);
			}
		}

		protected virtual void BuildString(StringBuilder sb, string value)
		{
			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] > 127)
				{
					BuildUnicodeString(sb, value);
					return;
				}
			}

			sb
				.Append('\'')
				.Append(value.Replace("'", "''"))
				.Append('\'');
		}

		protected virtual void BuildUnicodeString(StringBuilder sb, string value)
		{
			sb
				.Append('\'')
				.Append(value.Replace("'", "''"))
				.Append("\'");
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

				_indent++;

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

				_indent--;

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
	
		protected virtual void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			sb.Append(type.SqlDbType.ToString());

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

		protected virtual void BuildAliases(StringBuilder sb, string table, List<SqlQuery.Column> columns, string postfix)
		{
			_indent++;

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

			_indent--;

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
				_indent++;

				AppendIndent(sb).Append("SELECT").AppendLine();

				_indent++;
				AppendIndent(sb).AppendFormat("{0}.*,", aliases[0]).AppendLine();
				AppendIndent(sb).Append("ROW_NUMBER() OVER");

				if (!SqlQuery.OrderBy.IsEmpty && !implementOrderBy)
					sb.Append("()");
				else
				{
					sb.AppendLine();
					AppendIndent(sb).Append("(").AppendLine();

					_indent++;

					if (SqlQuery.OrderBy.IsEmpty)
					{
						AppendIndent(sb).Append("ORDER BY").AppendLine();
						BuildAliases(sb, aliases[0], SqlQuery.Select.Columns.Take(1).ToList(), null);
					}
					else
						BuildAlternativeOrderBy(sb, true);

					_indent--;
					AppendIndent(sb).Append(")");
				}

				sb.Append(" as ").Append(rnaliase).AppendLine();
				_indent--;

				AppendIndent(sb).Append("FROM").AppendLine();
				AppendIndent(sb).Append("(").AppendLine();

				_indent++;
				buildSql(sb);
				_indent--;

				AppendIndent(sb).AppendFormat(") {0}", aliases[0]).AppendLine();

				_indent--;

				AppendIndent(sb).AppendFormat(") {0}", aliases[1]).AppendLine();
				AppendIndent(sb).Append("WHERE").AppendLine();

				_indent++;

				if (NeedTake)
				{
					var expr1 = Add(SqlQuery.Select.SkipValue, 1);
					var expr2 = Add<int>(SqlQuery.Select.SkipValue, SqlQuery.Select.TakeValue);

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
					BuildExpression(sb, SqlQuery.Select.SkipValue);
				}

				sb.AppendLine();
				_indent--;
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
			_indent++;

			AppendIndent(sb).Append("SELECT TOP ");
			BuildExpression(sb, SqlQuery.Select.TakeValue);
			sb.Append(" *").AppendLine();
			AppendIndent(sb).Append("FROM").AppendLine();
			AppendIndent(sb).Append("(")   .AppendLine();
			_indent++;

			if (SqlQuery.OrderBy.IsEmpty)
			{
				AppendIndent(sb).Append("SELECT TOP ");

				var p = SqlQuery.Select.SkipValue as SqlParameter;

				if (p != null && !p.IsQueryParameter && SqlQuery.Select.TakeValue is SqlValue)
					BuildValue(sb, (int)p.Value + (int)((SqlValue)(SqlQuery.Select.TakeValue)).Value);
				else
					BuildExpression(sb, Add<int>(SqlQuery.Select.SkipValue, SqlQuery.Select.TakeValue));

				sb.Append(" *").AppendLine();
				AppendIndent(sb).Append("FROM").AppendLine();
				AppendIndent(sb).Append("(")   .AppendLine();
				_indent++;
			}

			buildSql(sb);

			if (SqlQuery.OrderBy.IsEmpty)
			{
				_indent--;
				AppendIndent(sb).AppendFormat(") {0}", aliases[2]).AppendLine();
				AppendIndent(sb).Append("ORDER BY").AppendLine();
				BuildAliases(sb, aliases[2], SqlQuery.Select.Columns, null);
			}

			_indent--;
			AppendIndent(sb).AppendFormat(") {0}", aliases[1]).AppendLine();

			if (SqlQuery.OrderBy.IsEmpty)
			{
				AppendIndent(sb).Append("ORDER BY").AppendLine();
				BuildAliases(sb, aliases[1], SqlQuery.Select.Columns, " DESC");
			}
			else
			{
				BuildAlternativeOrderBy(sb, false);
			}

			_indent--;
			AppendIndent(sb).AppendFormat(") {0}", aliases[0]).AppendLine();

			if (SqlQuery.OrderBy.IsEmpty)
			{
				AppendIndent(sb).Append("ORDER BY").AppendLine();
				BuildAliases(sb, aliases[0], SqlQuery.Select.Columns, null);
			}
			else
			{
				BuildAlternativeOrderBy(sb, true);
			}
		}

		protected void BuildAlternativeOrderBy(StringBuilder sb, bool ascending)
		{
			AppendIndent(sb).Append("ORDER BY").AppendLine();

			var obys = GetTempAliases(SqlQuery.OrderBy.Items.Count, "oby");

			_indent++;

			for (var i = 0; i < obys.Length; i++)
			{
				AppendIndent(sb).Append(obys[i]);

				if ( ascending &&  SqlQuery.OrderBy.Items[i].IsDescending ||
					!ascending && !SqlQuery.OrderBy.Items[i].IsDescending)
					sb.Append(" DESC");

				if (i + 1 < obys.Length)
					sb.Append(',');

				sb.AppendLine();
			}

			_indent--;
		}

		protected delegate IEnumerable<SqlQuery.Column> ColumnSelector();

		protected IEnumerable<SqlQuery.Column> AlternativeGetSelectedColumns(ColumnSelector columnSelector)
		{
			foreach (var col in columnSelector())
				yield return col;

			var obys = GetTempAliases(SqlQuery.OrderBy.Items.Count, "oby");

			for (var i = 0; i < obys.Length; i++)
				yield return new SqlQuery.Column(SqlQuery, SqlQuery.OrderBy.Items[i].Expression, obys[i]);
		}

		protected bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType   : return ((SqlDataType)  expr).SqlDbType == SqlDbType.Date;
				case QueryElementType.SqlExpression : return ((SqlExpression)expr).Expr      == dateName;
			}

			return false;
		}

		protected bool IsTimeDataType(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlDataType   : return ((SqlDataType)expr).  SqlDbType == SqlDbType.Time;
				case QueryElementType.SqlExpression : return ((SqlExpression)expr).Expr      == "Time";
			}

			return false;
		}

		protected ISqlExpression FloorBeforeConvert(SqlFunction func)
		{
			var par1 = func.Parameters[1];

			return par1.SystemType.IsFloatType() && func.SystemType.IsIntegerType() ?
				new SqlFunction(func.SystemType, "Floor", par1) : par1;
		}

		protected ISqlExpression AlternativeConvertToBoolean(SqlFunction func, int paramNumber)
		{
			var par = func.Parameters[paramNumber];

			if (par.SystemType.IsFloatType() || par.SystemType.IsIntegerType())
			{
				var sc = new SqlQuery.SearchCondition();

				sc.Conditions.Add(
					new SqlQuery.Condition(false, new SqlQuery.Predicate.ExprExpr(par, SqlQuery.Predicate.Operator.Equal, new SqlValue(0))));

				return ConvertExpression(new SqlFunction(func.SystemType, "CASE", sc, new SqlValue(false), new SqlValue(true)));
			}

			return null;
		}

		protected SqlQuery GetAlternativeDelete(SqlQuery sqlQuery)
		{
			if (sqlQuery.IsDelete && 
				(sqlQuery.From.Tables.Count > 1 || sqlQuery.From.Tables[0].Joins.Count > 0) && 
				sqlQuery.From.Tables[0].Source is SqlTable)
			{
				var sql = new SqlQuery { QueryType = QueryType.Delete };

				sqlQuery.ParentSql = sql;
				sqlQuery.QueryType = QueryType.Select;

				var table = (SqlTable)sqlQuery.From.Tables[0].Source;
				var copy  = new SqlTable(table) { Alias = null };

				var tableKeys = table.GetKeys(true);
				var copyKeys  = copy. GetKeys(true);

				for (var i = 0; i < tableKeys.Count; i++)
					sqlQuery.Where
						.Expr(copyKeys[i]).Equal.Expr(tableKeys[i]);

				sql.From.Table(copy).Where.Exists(sqlQuery);
				sql.Parameters.AddRange(sqlQuery.Parameters);

				sqlQuery.Parameters.Clear();

				sqlQuery = sql;
			}

			return sqlQuery;
		}

		protected SqlQuery GetAlternativeUpdate(SqlQuery sqlQuery)
		{
			if (sqlQuery.IsUpdate && sqlQuery.From.Tables[0].Source is SqlTable)
			{
				if (sqlQuery.From.Tables.Count > 1 || sqlQuery.From.Tables[0].Joins.Count > 0)
				{
					var sql = new SqlQuery { QueryType = QueryType.Update };

					sqlQuery.ParentSql = sql;
					sqlQuery.QueryType = QueryType.Select;

					var table = (SqlTable)sqlQuery.From.Tables[0].Source;
					var copy  = new SqlTable(table);

					var tableKeys = table.GetKeys(true);
					var copyKeys  = copy. GetKeys(true);

					for (var i = 0; i < tableKeys.Count; i++)
						sqlQuery.Where
							.Expr(copyKeys[i]).Equal.Expr(tableKeys[i]);

					sql.From.Table(copy).Where.Exists(sqlQuery);

					var map = new Dictionary<SqlField, SqlField>(table.Fields.Count);

					foreach (var field in table.Fields.Values)
						map.Add(field, copy[field.Name]);

					foreach (var item in sqlQuery.Update.Items)
					{
						((ISqlExpressionWalkable)item).Walk(false, expr =>
						{
							var fld = expr as SqlField;
							return fld != null && map.TryGetValue(fld, out fld) ? fld : expr;
						});

						sql.Update.Items.Add(item);
					}

					sql.Parameters.AddRange(sqlQuery.Parameters);

					sqlQuery.Parameters.Clear();
					sqlQuery.Update.Items.Clear();

					sqlQuery = sql;
				}

				sqlQuery.From.Tables[0].Alias = "$";
			}

			return sqlQuery;
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

			if (attrs == null)
				if (throwException)
					throw new SqlException("Sequence name can not be retrieved for the '{0}' table.", table.Name);
				else
					return null;

			SequenceNameAttribute defaultAttr = null;

			foreach (var attr in attrs)
			{
				if (attr.ProviderName == Name)
					return attr;

				if (defaultAttr == null && attr.ProviderName == null)
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

		protected void CheckAliases(SqlQuery sqlQuery, int maxLen)
		{
			new QueryVisitor().Visit(sqlQuery, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField     : ((SqlField)            e).Alias = SetAlias(((SqlField)            e).Alias, maxLen); break;
					case QueryElementType.SqlParameter : ((SqlParameter)        e).Name  = SetAlias(((SqlParameter)        e).Name,  maxLen); break;
					case QueryElementType.SqlTable     : ((SqlTable)            e).Alias = SetAlias(((SqlTable)            e).Alias, maxLen); break;
					case QueryElementType.Join         : ((Join)                e).Alias = SetAlias(((Join)                e).Alias, maxLen); break;
					case QueryElementType.Column       : ((SqlQuery.Column)     e).Alias = SetAlias(((SqlQuery.Column)     e).Alias, maxLen); break;
					case QueryElementType.TableSource  : ((SqlQuery.TableSource)e).Alias = SetAlias(((SqlQuery.TableSource)e).Alias, maxLen); break;
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
			return SqlQuery.GetTempAliases(n, defaultAlias + (Nesting == 0? "": "n" + Nesting));
		}

		protected static string GetTableAlias(ISqlTableSource table)
		{
			switch (table.ElementType)
			{
				case QueryElementType.TableSource :
					var ts    = (SqlQuery.TableSource)table;
					var alias = string.IsNullOrEmpty(ts.Alias) ? GetTableAlias(ts.Source) : ts.Alias;
					return alias != "$" ? alias : null;

				case QueryElementType.SqlTable :
					return ((SqlTable)table).Alias;

				default :
					throw new InvalidOperationException();
			}
		}

		string GetTablePhysicalName(ISqlTableSource table, string alias)
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
					return GetTablePhysicalName(((SqlQuery.TableSource)table).Source, alias);

				default :
					throw new InvalidOperationException();
			}
		}

		protected StringBuilder AppendIndent(StringBuilder sb)
		{
			if (_indent > 0)
				sb.Append('\t', _indent);

			return sb;
		}

		public ISqlExpression Add(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "+", expr2, Precedence.Additive));
		}

		public ISqlExpression Add<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Add(expr1, expr2, typeof(T));
		}

		public ISqlExpression Add(ISqlExpression expr1, int value)
		{
			return Add<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Inc(ISqlExpression expr1)
		{
			return Add(expr1, 1);
		}

		public ISqlExpression Sub(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "-", expr2, Precedence.Subtraction));
		}

		public ISqlExpression Sub<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Sub(expr1, expr2, typeof(T));
		}

		public ISqlExpression Sub(ISqlExpression expr1, int value)
		{
			return Sub<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Dec(ISqlExpression expr1)
		{
			return Sub(expr1, 1);
		}

		public ISqlExpression Mul(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "*", expr2, Precedence.Multiplicative));
		}

		public ISqlExpression Mul<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Mul(expr1, expr2, typeof(T));
		}

		public ISqlExpression Mul(ISqlExpression expr1, int value)
		{
			return Mul<int>(expr1, new SqlValue(value));
		}

		public ISqlExpression Div(ISqlExpression expr1, ISqlExpression expr2, Type type)
		{
			return ConvertExpression(new SqlBinaryExpression(type, expr1, "/", expr2, Precedence.Multiplicative));
		}

		public ISqlExpression Div<T>(ISqlExpression expr1, ISqlExpression expr2)
		{
			return Div(expr1, expr2, typeof(T));
		}

		public ISqlExpression Div(ISqlExpression expr1, int value)
		{
			return Div<int>(expr1, new SqlValue(value));
		}

		#endregion

		#region DataTypes

		protected virtual int GetMaxLength     (SqlDataType type) { return SqlDataType.GetMaxLength     (type.SqlDbType); }
		protected virtual int GetMaxPrecision  (SqlDataType type) { return SqlDataType.GetMaxPrecision  (type.SqlDbType); }
		protected virtual int GetMaxScale      (SqlDataType type) { return SqlDataType.GetMaxScale      (type.SqlDbType); }
		protected virtual int GetMaxDisplaySize(SqlDataType type) { return SqlDataType.GetMaxDisplaySize(type.SqlDbType); }

		protected virtual ISqlExpression ConvertConvertion(SqlFunction func)
		{
			var from = (SqlDataType)func.Parameters[1];
			var to   = (SqlDataType)func.Parameters[0];

			if (to.Type == typeof(object))
				return func.Parameters[2];

			if (to.Precision > 0)
			{
				var maxPrecision = GetMaxPrecision(from);
				var maxScale     = GetMaxScale    (from);
				var newPrecision = maxPrecision >= 0 ? Math.Min(to.Precision, maxPrecision) : to.Precision;
				var newScale     = maxScale     >= 0 ? Math.Min(to.Scale,     maxScale)     : to.Scale;

				if (to.Precision != newPrecision || to.Scale != newScale)
					to = new SqlDataType(to.SqlDbType, to.Type, newPrecision, newScale);
			}
			else if (to.Length > 0)
			{
				var maxLength = to.Type == typeof(string) ? GetMaxDisplaySize(from) : GetMaxLength(from);
				var newLength = maxLength >= 0 ? Math.Min(to.Length, maxLength) : to.Length;

				if (to.Length != newLength)
					to = new SqlDataType(to.SqlDbType, to.Type, newLength);
			}
			else if (from.Type == typeof(short) && to.Type == typeof(int))
				return func.Parameters[2];

			return ConvertExpression(new SqlFunction(func.SystemType, "Convert", to, func.Parameters[2]));
		}

		#endregion

		#region ISqlProvider Members

		public virtual ISqlExpression ConvertExpression(ISqlExpression expression)
		{
			switch (expression.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:

					#region SqlBinaryExpression

					{
						var be = (SqlBinaryExpression)expression;

						switch (be.Operation)
						{
							case "+":
								if (be.Expr1 is SqlValue)
								{
									var v1 = (SqlValue)be.Expr1;
									if (v1.Value is int    && (int)   v1.Value == 0 ||
										v1.Value is string && (string)v1.Value == "") return be.Expr2;
								}

								if (be.Expr2 is SqlValue)
								{
									var v2 = (SqlValue) be.Expr2;

									if (v2.Value is int)
									{
										if ((int)v2.Value == 0) return be.Expr1;

										if (be.Expr1 is SqlBinaryExpression)
										{
											var be1 = (SqlBinaryExpression) be.Expr1;

											if (be1.Expr2 is SqlValue)
											{
												var be1v2 = (SqlValue)be1.Expr2;

												if (be1v2.Value is int)
												{
													switch (be1.Operation)
													{
														case "+":
															{
																var value = (int)be1v2.Value + (int)v2.Value;
																var oper  = be1.Operation;

																if (value < 0)
																{
																	value = - value;
																	oper  = "-";
																}

																return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
															}

														case "-":
															{
																var value = (int)be1v2.Value - (int)v2.Value;
																var oper  = be1.Operation;

																if (value < 0)
																{
																	value = - value;
																	oper  = "+";
																}

																return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
															}
													}
												}
											}
										}
									}
									else if (v2.Value is string)
									{
										if ((string)v2.Value == "") return be.Expr1;

										if (be.Expr1 is SqlBinaryExpression)
										{
											var be1 = (SqlBinaryExpression)be.Expr1;

											if (be1.Expr2 is SqlValue)
											{
												var value = ((SqlValue)be1.Expr2).Value;

												if (value is string)
													return new SqlBinaryExpression(
														be1.SystemType,
														be1.Expr1,
														be1.Operation,
														new SqlValue(string.Concat(value, v2.Value)));
											}
										}
									}
								}

								if (be.Expr1 is SqlValue && be.Expr2 is SqlValue)
								{
									var v1 = (SqlValue)be.Expr1;
									var v2 = (SqlValue)be.Expr2;
									if (v1.Value is int    && v2.Value is int)    return new SqlValue((int)v1.Value + (int)v2.Value);
									if (v1.Value is string || v2.Value is string) return new SqlValue(v1.Value.ToString() + v2.Value);
								}

								if (be.Expr1.SystemType == typeof(string) && be.Expr2.SystemType != typeof(string))
								{
									var len = be.Expr2.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr2.SystemType).SqlDbType);

									if (len <= 0)
										len = 100;

									return new SqlBinaryExpression(
										be.SystemType,
										be.Expr1,
										be.Operation,
										ConvertExpression(new SqlFunction(typeof(string), "Convert", new SqlDataType(SqlDbType.VarChar, len), be.Expr2)),
										be.Precedence);
								}

								if (be.Expr1.SystemType != typeof(string) && be.Expr2.SystemType == typeof(string))
								{
									var len = be.Expr1.SystemType == null ? 100 : SqlDataType.GetMaxDisplaySize(SqlDataType.GetDataType(be.Expr1.SystemType).SqlDbType);

									if (len <= 0)
										len = 100;

									return new SqlBinaryExpression(
										be.SystemType,
										ConvertExpression(new SqlFunction(typeof(string), "Convert", new SqlDataType(SqlDbType.VarChar, len), be.Expr1)),
										be.Operation,
										be.Expr2,
										be.Precedence);
								}

								break;

							case "-":
								if (be.Expr2 is SqlValue)
								{
									var v2 = (SqlValue) be.Expr2;

									if (v2.Value is int)
									{
										if ((int)v2.Value == 0) return be.Expr1;

										if (be.Expr1 is SqlBinaryExpression)
										{
											var be1 = (SqlBinaryExpression)be.Expr1;

											if (be1.Expr2 is SqlValue)
											{
												var be1v2 = (SqlValue)be1.Expr2;

												if (be1v2.Value is int)
												{
													switch (be1.Operation)
													{
														case "+":
															{
																var value = (int)be1v2.Value - (int)v2.Value;
																var oper  = be1.Operation;

																if (value < 0)
																{
																	value = -value;
																	oper  = "-";
																}

																return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
															}

														case "-":
															{
																var value = (int)be1v2.Value + (int)v2.Value;
																var oper  = be1.Operation;

																if (value < 0)
																{
																	value = -value;
																	oper  = "+";
																}

																return new SqlBinaryExpression(be.SystemType, be1.Expr1, oper, new SqlValue(value), be.Precedence);
															}
													}
												}
											}
										}
									}
								}

								if (be.Expr1 is SqlValue && be.Expr2 is SqlValue)
								{
									var v1 = (SqlValue)be.Expr1;
									var v2 = (SqlValue)be.Expr2;
									if (v1.Value is int && v2.Value is int) return new SqlValue((int)v1.Value - (int)v2.Value);
								}

								break;

							case "*":
								if (be.Expr1 is SqlValue)
								{
									var v1 = (SqlValue)be.Expr1;

									if (v1.Value is int)
									{
										var v1v = (int)v1.Value;

										switch (v1v)
										{
											case  0 : return new SqlValue(0);
											case  1 : return be.Expr2;
											default :
												{
													var be2 = be.Expr2 as SqlBinaryExpression;

													if (be2 != null && be2.Operation == "*" && be2.Expr1 is SqlValue)
													{
														var be2v1 = be2.Expr1 as SqlValue;

														if (be2v1.Value is int)
															return ConvertExpression(
																new SqlBinaryExpression(be2.SystemType, new SqlValue(v1v * (int)be2v1.Value), "*", be2.Expr2));
													}

													break;
												}

										}
									}
								}

								if (be.Expr2 is SqlValue)
								{
									var v2 = (SqlValue)be.Expr2;
									if (v2.Value is int && (int)v2.Value == 1) return be.Expr1;
									if (v2.Value is int && (int)v2.Value == 0) return new SqlValue(0);
								}

								if (be.Expr1 is SqlValue && be.Expr2 is SqlValue)
								{
									var v1 = (SqlValue)be.Expr1;
									var v2 = (SqlValue)be.Expr2;

									if (v1.Value is int)
									{
										if (v2.Value is int)    return new SqlValue((int)   v1.Value * (int)   v2.Value);
										if (v2.Value is double) return new SqlValue((int)   v1.Value * (double)v2.Value);
									}
									else if (v1.Value is double)
									{
										if (v2.Value is int)    return new SqlValue((double)v1.Value * (int)   v2.Value);
										if (v2.Value is double) return new SqlValue((double)v1.Value * (double)v2.Value);
									}
								}

								break;
						}
					}

					#endregion

					break;

				case QueryElementType.SqlFunction:

					#region SqlFunction

					{
						var func = (SqlFunction)expression;

						switch (func.Name)
						{
							case "ConvertToCaseCompareTo":
								return ConvertExpression(new SqlFunction(func.SystemType, "CASE",
									new SqlQuery.SearchCondition().Expr(func.Parameters[0]). Greater .Expr(func.Parameters[1]).ToExpr(), new SqlValue(1),
									new SqlQuery.SearchCondition().Expr(func.Parameters[0]). Equal   .Expr(func.Parameters[1]).ToExpr(), new SqlValue(0),
									new SqlValue(-1)));

							case "$Convert$": return ConvertConvertion(func);
							case "Average"  : return new SqlFunction(func.SystemType, "Avg", func.Parameters);
							case "Max"      :
							case "Min"      :
								{
									if (func.SystemType == typeof(bool) || func.SystemType == typeof(bool?))
									{
										return new SqlFunction(typeof(int), func.Name,
											new SqlFunction(func.SystemType, "CASE", func.Parameters[0], new SqlValue(1), new SqlValue(0)));
									}

									break;
								}

							case "CASE"     :
								{
									var parms = func.Parameters;
									var len   = parms.Length;

									for (var i = 0; i < parms.Length - 1; i += 2)
									{
										var value = parms[i] as SqlValue;

										if (value != null)
										{
											if ((bool)value.Value == false)
											{
												var newParms = new ISqlExpression[parms.Length - 2];

												if (i != 0)
													Array.Copy(parms, 0, newParms, 0, i);

												Array.Copy(parms, i + 2, newParms, i, parms.Length - i - 2);

												parms = newParms;
												i -= 2;
											}
											else
											{
												var newParms = new ISqlExpression[i + 1];

												if (i != 0)
													Array.Copy(parms, 0, newParms, 0, i);

												newParms[i] = parms[i + 1];

												parms = newParms;
												break;
											}
										}
									}

									if (parms.Length == 1)
										return parms[0];

									if (parms.Length != len)
										return new SqlFunction(func.SystemType, func.Name, func.Precedence, parms);
								}

								break;

							case "Convert":
								{
									var from  = func.Parameters[1] as SqlFunction;
									var typef = func.SystemType.GetUnderlyingType();

									if (from != null && from.Name == "Convert" && from.Parameters[1].SystemType.GetUnderlyingType() == typef)
										return from.Parameters[1];

									var fe = func.Parameters[1] as SqlExpression;

									if (fe != null && fe.Expr == "Cast({0} as {1})" && fe.Parameters[0].SystemType.GetUnderlyingType() == typef)
										return fe.Parameters[0];
								}

								break;
						}
					}

					#endregion

					break;

				case QueryElementType.SearchCondition :
					SqlQuery.OptimizeSearchCondition((SqlQuery.SearchCondition)expression);
					break;

				case QueryElementType.SqlExpression   :
					{
						var se = (SqlExpression)expression;

						if (se.Expr == "{0}" && se.Parameters.Length == 1 && se.Parameters[0] != null)
							return se.Parameters[0];
					}

					break;
			}

			return expression;
		}

		public virtual ISqlPredicate ConvertPredicate(ISqlPredicate predicate)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					{
						var expr = (SqlQuery.Predicate.ExprExpr)predicate;

						if (expr.Operator == SqlQuery.Predicate.Operator.Equal && expr.Expr1 is SqlValue && expr.Expr2 is SqlValue)
						{
							var value = Equals(((SqlValue)expr.Expr1).Value, ((SqlValue)expr.Expr2).Value);
							return new SqlQuery.Predicate.Expr(new SqlValue(value), Precedence.Comparison);
						}

						switch (expr.Operator)
						{
							case SqlQuery.Predicate.Operator.Equal:
							case SqlQuery.Predicate.Operator.Greater:
							case SqlQuery.Predicate.Operator.Less :
								predicate = OptimizeCase(expr);
								break;
						}

						if (predicate is SqlQuery.Predicate.ExprExpr)
						{
							expr = (SqlQuery.Predicate.ExprExpr)predicate;

							switch (expr.Operator)
							{
								case SqlQuery.Predicate.Operator.Equal :
								case SqlQuery.Predicate.Operator.NotEqual :
									var expr1 = expr.Expr1;
									var expr2 = expr.Expr2;

									if (expr1.CanBeNull() && expr2.CanBeNull())
									{
										if (expr1 is SqlParameter || expr2 is SqlParameter)
											SqlQuery.ParameterDependent = true;
										else
											if (expr1 is SqlQuery.Column || expr1 is SqlField)
											if (expr2 is SqlQuery.Column || expr2 is SqlField)
												predicate = ConvertEqualPredicate(expr);
									}

									break;
							}
						}
					}

					break;

				case QueryElementType.NotExprPredicate:
					{
						var expr = (SqlQuery.Predicate.NotExpr)predicate;

						if (expr.IsNot && expr.Expr1 is SqlQuery.SearchCondition)
						{
							var sc = (SqlQuery.SearchCondition)expr.Expr1;

							if (sc.Conditions.Count == 1)
							{
								var cond = sc.Conditions[0];

								if (cond.IsNot)
									return cond.Predicate;

								if (cond.Predicate is SqlQuery.Predicate.ExprExpr)
								{
									var ee = (SqlQuery.Predicate.ExprExpr)cond.Predicate;

									if (ee.Operator == SqlQuery.Predicate.Operator.Equal)
										return new SqlQuery.Predicate.ExprExpr(ee.Expr1, SqlQuery.Predicate.Operator.NotEqual, ee.Expr2);

									if (ee.Operator == SqlQuery.Predicate.Operator.NotEqual)
										return new SqlQuery.Predicate.ExprExpr(ee.Expr1, SqlQuery.Predicate.Operator.Equal, ee.Expr2);
								}
							}
						}
					}

					break;
			}

			return predicate;
		}

		protected ISqlPredicate ConvertEqualPredicate(SqlQuery.Predicate.ExprExpr expr)
		{
			var expr1 = expr.Expr1;
			var expr2 = expr.Expr2;
			var cond  = new SqlQuery.SearchCondition();

			if (expr.Operator == SqlQuery.Predicate.Operator.Equal)
				cond
					.Expr(expr1).IsNull.    And .Expr(expr2).IsNull. Or
					.Expr(expr1).IsNotNull. And .Expr(expr2).IsNotNull. And .Expr(expr1).Equal.Expr(expr2);
			else
				cond
					.Expr(expr1).IsNull.    And .Expr(expr2).IsNotNull. Or
					.Expr(expr1).IsNotNull. And .Expr(expr2).IsNull.    Or
					.Expr(expr1).NotEqual.Expr(expr2);

			return cond;
		}

		ISqlPredicate OptimizeCase(SqlQuery.Predicate.ExprExpr expr)
		{
			var value = expr.Expr1 as SqlValue;
			var func  = expr.Expr2 as SqlFunction;
			var valueFirst = false;

			if (value != null && func != null)
			{
				valueFirst = true;
			}
			else
			{
				value = expr.Expr2 as SqlValue;
				func  = expr.Expr1 as SqlFunction;
			}

			if (value != null && func != null && func.Name == "CASE")
			{
				if (value.Value is int && func.Parameters.Length == 5)
				{
					var c1 = func.Parameters[0] as SqlQuery.SearchCondition;
					var v1 = func.Parameters[1] as SqlValue;
					var c2 = func.Parameters[2] as SqlQuery.SearchCondition;
					var v2 = func.Parameters[3] as SqlValue;
					var v3 = func.Parameters[4] as SqlValue;

					if (c1 != null && c1.Conditions.Count == 1 && v1 != null && v1.Value is int &&
					    c2 != null && c2.Conditions.Count == 1 && v2 != null && v2.Value is int && v3 != null && v3.Value is int)
					{
						var ee1 = c1.Conditions[0].Predicate as SqlQuery.Predicate.ExprExpr;
						var ee2 = c2.Conditions[0].Predicate as SqlQuery.Predicate.ExprExpr;

						if (ee1 != null && ee2 != null && ee1.Expr1.Equals(ee2.Expr1) && ee1.Expr2.Equals(ee2.Expr2))
						{
							int e = 0, g = 0, l = 0;

							if (ee1.Operator == SqlQuery.Predicate.Operator.Equal   || ee2.Operator == SqlQuery.Predicate.Operator.Equal)   e = 1;
							if (ee1.Operator == SqlQuery.Predicate.Operator.Greater || ee2.Operator == SqlQuery.Predicate.Operator.Greater) g = 1;
							if (ee1.Operator == SqlQuery.Predicate.Operator.Less    || ee2.Operator == SqlQuery.Predicate.Operator.Less)    l = 1;

							if (e + g + l == 2)
							{
								var n  = (int)value.Value;
								var i1 = (int)v1.Value;
								var i2 = (int)v2.Value;
								var i3 = (int)v3.Value;

								var n1 = Compare(valueFirst ? n : i1, valueFirst ? i1 : n, expr.Operator) ? 1 : 0;
								var n2 = Compare(valueFirst ? n : i2, valueFirst ? i2 : n, expr.Operator) ? 1 : 0;
								var n3 = Compare(valueFirst ? n : i3, valueFirst ? i3 : n, expr.Operator) ? 1 : 0;

								if (n1 + n2 + n3 == 1)
								{
									if (n1 == 1) return ee1;
									if (n2 == 1) return ee2;

									return ConvertPredicate(new SqlQuery.Predicate.ExprExpr(
										ee1.Expr1,
										e == 0 ? SqlQuery.Predicate.Operator.Equal :
										g == 0 ? SqlQuery.Predicate.Operator.Greater :
												 SqlQuery.Predicate.Operator.Less,
										ee1.Expr2));
								}
							}

						}
					}
				}
				else if (value.Value is bool && func.Parameters.Length == 3)
				{
					var c1 = func.Parameters[0] as SqlQuery.SearchCondition;
					var v1 = func.Parameters[1] as SqlValue;
					var v2 = func.Parameters[2] as SqlValue;

					if (c1 != null && c1.Conditions.Count == 1 && v1 != null && v1.Value is bool && v2 != null && v2.Value is bool)
					{
						var bv  = (bool)value.Value;
						var bv1 = (bool)v1.Value;
						var bv2 = (bool)v2.Value;

						if (bv == bv1 && expr.Operator == SqlQuery.Predicate.Operator.Equal ||
						    bv != bv1 && expr.Operator == SqlQuery.Predicate.Operator.NotEqual)
						{
							return c1;
						}

						if (bv == bv2 && expr.Operator == SqlQuery.Predicate.Operator.NotEqual ||
						    bv != bv1 && expr.Operator == SqlQuery.Predicate.Operator.Equal)
						{
							var ee = c1.Conditions[0].Predicate as SqlQuery.Predicate.ExprExpr;

							if (ee != null)
							{
								SqlQuery.Predicate.Operator op;

								switch (ee.Operator)
								{
									case SqlQuery.Predicate.Operator.Equal          : op = SqlQuery.Predicate.Operator.NotEqual;       break;
									case SqlQuery.Predicate.Operator.NotEqual       : op = SqlQuery.Predicate.Operator.Equal;          break;
									case SqlQuery.Predicate.Operator.Greater        : op = SqlQuery.Predicate.Operator.LessOrEqual;    break;
									case SqlQuery.Predicate.Operator.NotLess        :
									case SqlQuery.Predicate.Operator.GreaterOrEqual : op = SqlQuery.Predicate.Operator.Less;           break;
									case SqlQuery.Predicate.Operator.Less           : op = SqlQuery.Predicate.Operator.GreaterOrEqual; break;
									case SqlQuery.Predicate.Operator.NotGreater     :
									case SqlQuery.Predicate.Operator.LessOrEqual    : op = SqlQuery.Predicate.Operator.Greater;        break;
									default: throw new InvalidOperationException();
								}

								return new SqlQuery.Predicate.ExprExpr(ee.Expr1, op, ee.Expr2);
							}

							var sc = new SqlQuery.SearchCondition();

							sc.Conditions.Add(new SqlQuery.Condition(true, c1));

							return sc;
						}
					}
				}
				else if (expr.Operator == SqlQuery.Predicate.Operator.Equal && func.Parameters.Length == 3)
				{
					var sc = func.Parameters[0] as SqlQuery.SearchCondition;
					var v1 = func.Parameters[1] as SqlValue;
					var v2 = func.Parameters[2] as SqlValue;

					if (sc != null && v1 != null && v2 != null)
					{
						if (Equals(value.Value, v1.Value))
							return sc;

						if (Equals(value.Value, v2.Value) && !sc.CanBeNull())
							return ConvertPredicate(new SqlQuery.Predicate.NotExpr(sc, true, Precedence.LogicalNegation));
					}
				}
			}

			return expr;
		}

		static bool Compare(int v1, int v2, SqlQuery.Predicate.Operator op)
		{
			switch (op)
			{
				case SqlQuery.Predicate.Operator.Equal:           return v1 == v2;
				case SqlQuery.Predicate.Operator.NotEqual:        return v1 != v2;
				case SqlQuery.Predicate.Operator.Greater:         return v1 >  v2;
				case SqlQuery.Predicate.Operator.NotLess:
				case SqlQuery.Predicate.Operator.GreaterOrEqual:  return v1 >= v2;
				case SqlQuery.Predicate.Operator.Less:            return v1 <  v2;
				case SqlQuery.Predicate.Operator.NotGreater:
				case SqlQuery.Predicate.Operator.LessOrEqual:     return v1 <= v2;
			}

			throw new InvalidOperationException();
		}

		public virtual SqlQuery Finalize(SqlQuery sqlQuery)
		{
			sqlQuery.FinalizeAndValidate(IsApplyJoinSupported, IsGroupByExpressionSupported);

			if (!IsCountSubQuerySupported)  sqlQuery = MoveCountSubQuery (sqlQuery);
			if (!IsSubQueryColumnSupported) sqlQuery = MoveSubQueryColumn(sqlQuery);

			if (!IsCountSubQuerySupported || !IsSubQueryColumnSupported)
				sqlQuery.FinalizeAndValidate(IsApplyJoinSupported, IsGroupByExpressionSupported);

			return sqlQuery;
		}

		SqlQuery MoveCountSubQuery(SqlQuery sqlQuery)
		{
			new QueryVisitor().Visit(sqlQuery, MoveCountSubQuery);
			return sqlQuery;
		}

		void MoveCountSubQuery(IQueryElement element)
		{
			if (element.ElementType != QueryElementType.SqlQuery)
				return;

			var query = (SqlQuery)element;

			for (var i = 0; i < query.Select.Columns.Count; i++)
			{
				var col = query.Select.Columns[i];

				// The column is a subquery.
				//
				if (col.Expression.ElementType == QueryElementType.SqlQuery)
				{
					var subQuery = (SqlQuery)col.Expression;
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
					SqlQuery.OptimizeSearchCondition(subQuery.Where.SearchCondition);

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
							case QueryElementType.Column   : return !allTables.Contains(((SqlQuery.Column)e).Parent);
						}
						return false;
					};

					var join = SqlQuery.LeftJoin(subQuery);

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
										break;
									}

									break;

								case QueryElementType.Column   :
									if (replaced.TryGetValue(e, out ne))
										return ne;

									if (levelTables.Contains(((SqlQuery.Column)e).Parent))
									{
										subQuery.GroupBy.Expr((SqlQuery.Column)e);
										ne = subQuery.Select.Columns[subQuery.Select.Add((SqlQuery.Column)e)];
										break;
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

		SqlQuery MoveSubQueryColumn(SqlQuery sqlQuery)
		{
			var dic = new Dictionary<IQueryElement,IQueryElement>();

			new QueryVisitor().Visit(sqlQuery, element =>
			{
				if (element.ElementType != QueryElementType.SqlQuery)
					return;

				var query = (SqlQuery)element;

				for (var i = 0; i < query.Select.Columns.Count; i++)
				{
					var col = query.Select.Columns[i];

					if (col.Expression.ElementType == QueryElementType.SqlQuery)
					{
						var subQuery    = (SqlQuery)col.Expression;
						var allTables   = new HashSet<ISqlTableSource>();
						var levelTables = new HashSet<ISqlTableSource>();

						Func<IQueryElement,bool> checkTable = e =>
						{
							switch (e.ElementType)
							{
								case QueryElementType.SqlField : return !allTables.Contains(((SqlField)e).Table);
								case QueryElementType.Column   : return !allTables.Contains(((SqlQuery.Column)e).Parent);
							}
							return false;
						};

						new QueryVisitor().Visit(subQuery, e =>
						{
							if (e is ISqlTableSource /*&& subQuery.From.IsChild((ISqlTableSource)e)*/)
								allTables.Add((ISqlTableSource)e);
						});

						new QueryVisitor().Visit(subQuery, e =>
						{
							if (e is ISqlTableSource && subQuery.From.IsChild((ISqlTableSource)e))
								levelTables.Add((ISqlTableSource)e);
						});

						if (IsSubQueryColumnSupported && new QueryVisitor().Find(subQuery, checkTable) == null)
							continue;

						var join = SqlQuery.LeftJoin(subQuery);

						query.From.Tables[0].Joins.Add(join.JoinedTable);

						SqlQuery.OptimizeSearchCondition(subQuery.Where.SearchCondition);

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

						if (IsSubQueryColumnSupported && !isCount)
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
											break;
										}

										break;

									case QueryElementType.Column   :
										if (replaced.TryGetValue(e, out ne))
											return ne;

										if (levelTables.Contains(((SqlQuery.Column)e).Parent))
										{
											if (isAggregated)
												subQuery.GroupBy.Expr((SqlQuery.Column)e);
											ne = subQuery.Select.Columns[subQuery.Select.Add((SqlQuery.Column)e)];
											break;
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

								query.Select.Columns[i] = new SqlQuery.Column(
									query,
									new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[0]));
							}
							else if (isAggregated && !query.GroupBy.IsEmpty)
							{
								var oldFunc = (SqlFunction)subQuery.Select.Columns[0].Expression;

								subQuery.Select.Columns.RemoveAt(0);

								var idx = subQuery.Select.Add(oldFunc.Parameters[0]);

								query.Select.Columns[i] = new SqlQuery.Column(
									query,
									new SqlFunction(oldFunc.SystemType, oldFunc.Name, subQuery.Select.Columns[idx]));
							}
							else
							{
								query.Select.Columns[i] = new SqlQuery.Column(query, subQuery.Select.Columns[0]);
							}

							dic.Add(col, query.Select.Columns[i]);
						}
					}
				}
			});

			sqlQuery = new QueryVisitor().Convert(sqlQuery, e =>
			{
				IQueryElement ne;
				return dic.TryGetValue(e, out ne) ? ne : e;
			});

			return sqlQuery;
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

		#region Linq Support

		public virtual LambdaExpression ConvertMember(MemberInfo mi)
		{
			return Expressions.ConvertMember(Name, mi);
		}

		#endregion
	}
}
