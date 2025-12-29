using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using LinqToDB.DataProvider;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	public abstract partial class BasicSqlBuilder : ISqlBuilder
	{
		#region Init

		protected BasicSqlBuilder(IDataProvider? provider, MappingSchema mappingSchema, DataOptions dataOptions, ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
		{
			DataProvider       = provider;
			MappingSchema      = mappingSchema;
			DataOptions        = dataOptions;
			SqlOptimizer       = sqlOptimizer;
			SqlProviderFlags   = sqlProviderFlags;
			NullabilityContext = NullabilityContext.NonQuery;
		}

		protected BasicSqlBuilder(BasicSqlBuilder parentBuilder)
		{
			DataProvider       = parentBuilder.DataProvider;
			MappingSchema      = parentBuilder.MappingSchema;
			DataOptions        = parentBuilder.DataOptions;
			SqlOptimizer       = parentBuilder.SqlOptimizer;
			SqlProviderFlags   = parentBuilder.SqlProviderFlags;
			TablePath          = parentBuilder.TablePath;
			QueryName          = parentBuilder.QueryName;
			TableIDs           = parentBuilder.TableIDs ??= new(StringComparer.Ordinal);
			NullabilityContext = parentBuilder.NullabilityContext;
		}

		public AliasesContext      AliasesContext      { get; protected set; } = null!;
		public OptimizationContext OptimizationContext { get; protected set; } = null!;
		public MappingSchema       MappingSchema       { get;                }
		public StringBuilder       StringBuilder       { get; set;           } = null!;
		public SqlProviderFlags    SqlProviderFlags    { get;                }
		public DataOptions         DataOptions         { get;                }
		public NullabilityContext  NullabilityContext  { get; set; }

		protected IDataProvider?      DataProvider;
		protected ValueToSqlConverter ValueToSqlConverter => MappingSchema.ValueToSqlConverter;
		protected SqlStatement        Statement = null!;
		protected int                 Indent;
		protected Step                BuildStep;
		protected ISqlOptimizer       SqlOptimizer;
		protected bool                SkipAlias;

		#endregion

		#region Build Flags

		bool _disableAlias;
		int  _binaryOptimized;

		#endregion

		#region Support Flags

		public virtual bool IsNestedJoinSupported           => true;
		public virtual bool IsNestedJoinParenthesisRequired => false;
		/// <summary>
		/// Identifies CTE clause location:
		/// <list type="bullet">
		/// <item><c>CteFirst = true</c> (default): WITH clause goes first in query</item>
		/// <item><c>CteFirst = false</c>: WITH clause goes before SELECT</item>
		/// </list>
		/// </summary>
		public virtual bool CteFirst                        => true;

		/// <summary>
		/// True if it is needed to wrap join condition with ()
		/// </summary>
		/// <example>
		/// <code>
		/// INNER JOIN Table2 t2 ON (t1.Value = t2.Value)
		/// </code>
		/// </example>
		public virtual bool WrapJoinCondition => false;

		/// <summary>
		/// True if provider requires OVER() clause to be present in window function WITHIN GROUP.
		/// Currently only SQL Server
		/// </summary>
		protected virtual bool IsOverRequiredWithinGroup => false;

		protected virtual bool CanSkipRootAliases(SqlStatement statement) => true;

		#endregion

		#region CommandCount

		public virtual int CommandCount(SqlStatement statement)
		{
			return 1;
		}

		#endregion

		#region Formatting
		/// <summary>
		/// Inline comma separator.
		/// Default value: <code>", "</code>
		/// </summary>
		protected virtual string InlineComma => ", ";

		// some providers could define different separator, e.g. DB2 iSeries OleDb provider needs ", " as separator
		/// <summary>
		/// End-of-line comma separator.
		/// Default value: <code>","</code>
		/// </summary>
		protected virtual string Comma => ",";

		/// <summary>
		/// End-of-line open parentheses element.
		/// Default value: <code>"("</code>
		/// </summary>
		protected virtual string OpenParens => "(";

		protected StringBuilder RemoveInlineComma()
		{
			StringBuilder.Length -= InlineComma.Length;
			return StringBuilder;
		}

		#endregion

		#region Helpers

		[return: NotNullIfNotNull(nameof(element))]
		public T? ConvertElement<T>(T? element)
			where T : class, IQueryElement
		{
			return OptimizationContext.OptimizeAndConvert(element, NullabilityContext);
		}

		[return: NotNullIfNotNull(nameof(element))]
		public IQueryElement? Optimize(IQueryElement? element, bool reducePredicates)
		{
			if (element == null)
				return null;

			return OptimizationContext.Optimize(element, NullabilityContext, reducePredicates);
		}

		#endregion

		#region BuildSql

		public void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, AliasesContext aliases, NullabilityContext? nullabilityContext,
			int startIndent = 0)
		{
			AliasesContext = aliases;
			BuildSql(commandNumber, statement, sb, optimizationContext, startIndent, !DataOptions.SqlOptions.GenerateFinalAliases && CanSkipRootAliases(statement), nullabilityContext: nullabilityContext);
		}

		protected virtual void BuildSetOperation(SetOperation operation, StringBuilder sb)
		{
			switch (operation)
			{
				case SetOperation.Union       : sb.Append("UNION");         break;
				case SetOperation.UnionAll    : sb.Append("UNION ALL");     break;
				case SetOperation.Except      : sb.Append("EXCEPT");        break;
				case SetOperation.ExceptAll   : sb.Append("EXCEPT ALL");    break;
				case SetOperation.Intersect   : sb.Append("INTERSECT");     break;
				case SetOperation.IntersectAll: sb.Append("INTERSECT ALL"); break;
				default                       : throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
			}
		}

		protected virtual void BuildSql(int commandNumber, SqlStatement statement, StringBuilder sb, OptimizationContext optimizationContext, int indent, bool skipAlias, NullabilityContext? nullabilityContext)
		{
			Statement           = statement;
			StringBuilder       = sb;
			OptimizationContext = optimizationContext;
			Indent              = indent;
			SkipAlias           = skipAlias;

			if (commandNumber == 0)
			{
				NullabilityContext = nullabilityContext ?? NullabilityContext.GetContext(statement.SelectQuery);
				if (statement.SelectQuery != null)
					NullabilityContext = NullabilityContext.WithQuery(statement.SelectQuery);

				BuildSql();

				if (Statement.SelectQuery is { HasSetOperators: true })
				{
					foreach (var union in Statement.SelectQuery.SetOperators)
					{
						AppendIndent();
						BuildSetOperation(union.Operation, sb);
						sb.AppendLine();

						var sqlBuilder = ((BasicSqlBuilder)CreateSqlBuilder());
						sqlBuilder.BuildSql(commandNumber,
							new SqlSelectStatement(union.SelectQuery) { ParentStatement = statement }, sb,
							optimizationContext, indent, 
							skipAlias, NullabilityContext);
						MergeSqlBuilderData(sqlBuilder);
					}
				}

				switch (statement.QueryType)
				{
					case QueryType.Select :
					case QueryType.Delete :
					case QueryType.Update :
					case QueryType.Insert :
						BuildStep = Step.QueryExtensions;
						BuildQueryExtensions(statement);
						break;
				}

				FinalizeBuildQuery(statement);
			}
			else
			{
				BuildCommand(statement, commandNumber);
			}
		}

		protected virtual void MergeSqlBuilderData(BasicSqlBuilder sqlBuilder)
		{
		}

		protected virtual void BuildCommand(SqlStatement statement, int commandNumber)
		{
		}

		List<Action>? _finalBuilders;

		protected virtual void FinalizeBuildQuery(SqlStatement statement)
		{
			if (_finalBuilders != null)
				foreach (var builder in _finalBuilders)
					builder();
		}

		#endregion

		#region Overrides

		protected virtual void BuildSqlBuilder(SelectQuery selectQuery, int indent, bool skipAlias)
		{
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

			if (!SqlProviderFlags.GetIsSkipSupportedFlag(takeExpr)
				&& skipExpr != null)
				throw new LinqToDBException(ErrorHelper.Error_Skip_in_Subquery);

			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			sqlBuilder.BuildSql(0,
				new SqlSelectStatement(selectQuery) { ParentStatement = Statement }, StringBuilder, OptimizationContext, indent, skipAlias, NullabilityContext);
			MergeSqlBuilderData(sqlBuilder);
		}

		protected abstract ISqlBuilder CreateSqlBuilder();

		protected string WithStringBuilderBuildExpression(ISqlExpression expr)
		{
			using var sb = Pools.StringBuilder.Allocate();

			var current   = StringBuilder;
			StringBuilder = sb.Value;

			BuildExpression(expr);

			StringBuilder = current;

			return sb.Value.ToString();
		}

		protected string WithStringBuilderBuildExpression(int precedence, ISqlExpression expr)
		{
			using var sb = Pools.StringBuilder.Allocate();

			var current   = StringBuilder;
			StringBuilder = sb.Value;

			BuildExpression(precedence, expr);

			StringBuilder = current;

			return sb.Value.ToString();
		}

		protected string WithStringBuilder<TContext>(Action<TContext> func, TContext context)
		{
			using var sb = Pools.StringBuilder.Allocate();

			var current   = StringBuilder;
			StringBuilder = sb.Value;

			func(context);

			StringBuilder = current;

			return sb.Value.ToString();
		}

		void WithStringBuilder<TContext>(StringBuilder sb, Action<TContext> func, TContext context)
		{
			var current = StringBuilder;

			StringBuilder = sb;

			func(context);

			StringBuilder = current;
		}

		protected virtual bool ParenthesizeJoin(List<SqlJoinedTable> joins)
		{
			return false;
		}

		protected virtual void BuildSql()
		{
			BuildSqlImpl();
		}

		void BuildSqlImpl()
		{
			switch (Statement.QueryType)
			{
				case QueryType.Select        : BuildSelectQuery((SqlSelectStatement)Statement);                                                                 break;
				case QueryType.Delete        : BuildDeleteQuery((SqlDeleteStatement)Statement);                                                                 break;
				case QueryType.Update        : BuildUpdateQuery((SqlUpdateStatement)Statement, Statement.SelectQuery!, ((SqlUpdateStatement)Statement).Update); break;
				case QueryType.Insert        : BuildInsertQuery((SqlInsertStatement)Statement, ((SqlInsertStatement)Statement).Insert, false);                  break;
				case QueryType.InsertOrUpdate: BuildInsertOrUpdateQuery((SqlInsertOrUpdateStatement)Statement);                                                 break;
				case QueryType.CreateTable   : BuildCreateTableStatement((SqlCreateTableStatement)Statement);                                                   break;
				case QueryType.DropTable     : BuildDropTableStatement((SqlDropTableStatement)Statement);                                                       break;
				case QueryType.TruncateTable : BuildTruncateTableStatement((SqlTruncateTableStatement)Statement);                                               break;
				case QueryType.Merge         : BuildMergeStatement((SqlMergeStatement)Statement);                                                               break;
				case QueryType.MultiInsert   : BuildMultiInsertQuery((SqlMultiInsertStatement)Statement);                                                       break;
				default                      : BuildUnknownQuery();                                                                                             break;
			}
		}

		protected void BuildSqlForUnion()
		{
			if (Statement.SelectQuery?.SqlQueryExtensions is not null)
			{
				var isUnion =
					Statement.SelectQuery is {HasSetOperators: true} ||
					Statement.ParentStatement?.SelectQuery is {HasSetOperators: true} sq && sq.SetOperators.Exists(s => s.SelectQuery == Statement.SelectQuery);

				if (isUnion)
				{
					AppendIndent().AppendLine("(");
					Indent++;
					BuildSqlImpl();
					Indent--;
					AppendIndent().AppendLine(")");

					return;
				}
			}

			BuildSqlImpl();
		}

		protected virtual void BuildDeleteQuery(SqlDeleteStatement deleteStatement)
		{
			BuildStep = Step.Tag;               BuildTag(deleteStatement);
			BuildStep = Step.WithClause;        BuildWithClause(deleteStatement.With);
			BuildStep = Step.DeleteClause;      BuildDeleteClause(deleteStatement);
			BuildStep = Step.FromClause;        BuildDeleteFromClause(deleteStatement);
			BuildStep = Step.AlterDeleteClause; BuildAlterDeleteClause(deleteStatement);
			BuildStep = Step.WhereClause;       BuildWhereClause(deleteStatement.SelectQuery);
			BuildStep = Step.GroupByClause;     BuildGroupByClause(deleteStatement.SelectQuery);
			BuildStep = Step.HavingClause;      BuildHavingClause(deleteStatement.SelectQuery);
			BuildStep = Step.OrderByClause;     BuildOrderByClause(deleteStatement.SelectQuery);
			BuildStep = Step.OffsetLimit;       BuildOffsetLimit(deleteStatement.SelectQuery);
			BuildStep = Step.Output;            BuildOutputSubclause(deleteStatement.GetOutputClause());
			BuildStep = Step.QueryExtensions;   BuildSubQueryExtensions(deleteStatement);
		}

		protected void BuildDeleteQuery2(SqlDeleteStatement deleteStatement)
		{
			BuildStep = Step.Tag;          BuildTag(deleteStatement);
			BuildStep = Step.DeleteClause; BuildDeleteClause(deleteStatement);

			while (StringBuilder[StringBuilder.Length - 1] == ' ')
				StringBuilder.Length--;

			StringBuilder.AppendLine();
			AppendIndent().AppendLine(OpenParens);

			++Indent;

			var selectStatement = new SqlSelectStatement(deleteStatement.SelectQuery)
			{ ParentStatement = deleteStatement, With = deleteStatement.GetWithClause() };

			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			sqlBuilder.BuildSql(0, selectStatement, StringBuilder, OptimizationContext, AliasesContext, NullabilityContext, Indent);
			MergeSqlBuilderData(sqlBuilder);

			--Indent;

			AppendIndent().AppendLine(")");
		}

		protected virtual void BuildUpdateQuery(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			BuildStep = Step.Tag;          BuildTag(statement);
			BuildStep = Step.WithClause;   BuildWithClause(statement.GetWithClause());
			BuildStep = Step.UpdateClause; BuildUpdateClause(Statement, selectQuery, updateClause);

			if (SqlProviderFlags.IsUpdateFromSupported)
			{
				BuildStep = Step.FromClause; BuildFromClause(Statement, selectQuery);
			}

			BuildStep = Step.WhereClause;     BuildUpdateWhereClause(selectQuery);
			BuildStep = Step.GroupByClause;   BuildGroupByClause(selectQuery);
			BuildStep = Step.HavingClause;    BuildHavingClause(selectQuery);
			BuildStep = Step.OrderByClause;   BuildOrderByClause(selectQuery);
			BuildStep = Step.OffsetLimit;     BuildOffsetLimit(selectQuery);
			BuildStep = Step.Output;          BuildOutputSubclause(statement.GetOutputClause());
			BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(statement);
		}

		protected virtual void BuildSelectQuery(SqlSelectStatement selectStatement)
		{
			var queryName = QueryName;
			var tablePath = TablePath;

			if (selectStatement.SelectQuery.QueryName is not null && SqlProviderFlags.IsNamingQueryBlockSupported)
			{
				QueryName = selectStatement.SelectQuery.QueryName;
				TablePath = null;
			}

			BuildStep = Step.Tag;             BuildTag(selectStatement);
			BuildStep = Step.WithClause;      BuildWithClause(selectStatement.With);
			BuildStep = Step.SelectClause;    BuildSelectClause(selectStatement.SelectQuery);
			BuildStep = Step.FromClause;      BuildFromClause(selectStatement, selectStatement.SelectQuery);
			BuildStep = Step.WhereClause;     BuildWhereClause(selectStatement.SelectQuery);
			BuildStep = Step.GroupByClause;   BuildGroupByClause(selectStatement.SelectQuery);
			BuildStep = Step.HavingClause;    BuildHavingClause(selectStatement.SelectQuery);
			BuildStep = Step.OrderByClause;   BuildOrderByClause(selectStatement.SelectQuery);
			BuildStep = Step.OffsetLimit;     BuildOffsetLimit(selectStatement.SelectQuery);
			BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(selectStatement);

			TablePath = tablePath;
			QueryName = queryName;
		}

		protected virtual void BuildCteBody(SelectQuery selectQuery)
		{
			var sqlBuilder = (BasicSqlBuilder)CreateSqlBuilder();
			sqlBuilder.BuildSql(0, new SqlSelectStatement(selectQuery), StringBuilder, OptimizationContext, Indent, SkipAlias, NullabilityContext);
			MergeSqlBuilderData(sqlBuilder);
		}

		protected virtual void BuildInsertQuery(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			if (!CteFirst && statement is SqlStatementWithQueryBase withQuery && withQuery.With?.Clauses.Count > 0)
			{
				BuildInsertQuery2(statement, insertClause, addAlias);
				return;
			}

			BuildStep = Step.Tag;          BuildTag(statement);
			BuildStep = Step.WithClause;   BuildWithClause(statement.GetWithClause());
			BuildStep = Step.InsertClause; BuildInsertClause(statement, insertClause, addAlias);

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.SelectClause;    BuildSelectClause(statement.SelectQuery);
				BuildStep = Step.FromClause;      BuildFromClause(statement, statement.SelectQuery);
				BuildStep = Step.WhereClause;     BuildWhereClause(statement.SelectQuery);
				BuildStep = Step.GroupByClause;   BuildGroupByClause(statement.SelectQuery);
				BuildStep = Step.HavingClause;    BuildHavingClause(statement.SelectQuery);
				BuildStep = Step.OrderByClause;   BuildOrderByClause(statement.SelectQuery);
				BuildStep = Step.OffsetLimit;     BuildOffsetLimit(statement.SelectQuery);
				BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(statement);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
			{
				BuildStep = Step.Output;
				BuildOutputSubclause(statement.GetOutputClause());
			}
		}

		protected void BuildInsertQuery2(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			BuildStep = Step.Tag;          BuildTag(statement);
			BuildStep = Step.InsertClause; BuildInsertClause(statement, insertClause, addAlias);

			BuildStep = Step.WithClause;   BuildWithClause(statement.GetWithClause());

			if (statement.QueryType == QueryType.Insert && statement.SelectQuery!.From.Tables.Count != 0)
			{
				BuildStep = Step.SelectClause;    BuildSelectClause(statement.SelectQuery);
				BuildStep = Step.FromClause;      BuildFromClause(statement, statement.SelectQuery);
				BuildStep = Step.WhereClause;     BuildWhereClause(statement.SelectQuery);
				BuildStep = Step.GroupByClause;   BuildGroupByClause(statement.SelectQuery);
				BuildStep = Step.HavingClause;    BuildHavingClause(statement.SelectQuery);
				BuildStep = Step.OrderByClause;   BuildOrderByClause(statement.SelectQuery);
				BuildStep = Step.OffsetLimit;     BuildOffsetLimit(statement.SelectQuery);
				BuildStep = Step.QueryExtensions; BuildSubQueryExtensions(statement);
			}

			if (insertClause.WithIdentity)
				BuildGetIdentity(insertClause);
			else
				BuildOutputSubclause(statement.GetOutputClause());
		}

		protected virtual void BuildMultiInsertQuery(SqlMultiInsertStatement statement)
			=> throw new LinqToDBException(ErrorHelper.Error_MutiTable_Insert);

		protected virtual void BuildUnknownQuery()
		{
			throw new LinqToDBException($"Unknown query type '{Statement.QueryType}'.");
		}

		// Default implementation. Doesn't generate linked server and package name components.
		public virtual StringBuilder BuildObjectName(
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
				if (name.Schema == null)
					sb.Append('.');
			}

			if (name.Schema != null)
			{
				(escape ? Convert(sb, name.Schema, ConvertType.NameToSchema) : sb.Append(name.Schema))
					.Append('.');
			}

			return escape ? Convert(sb, name.Name, objectType) : sb.Append(name.Name);
		}

		protected virtual StringBuilder BuildObjectNameSuffix(StringBuilder sb, SqlObjectName name, bool escape)
		{
			return sb;
		}

		public string ConvertInline(string value, ConvertType convertType)
		{
			using var sb = Pools.StringBuilder.Allocate();
			return Convert(sb.Value, value, convertType).ToString();
		}

		public virtual StringBuilder Convert(StringBuilder sb, string value, ConvertType convertType)
		{
			sb.Append(value);
			return sb;
		}

		#endregion

		#region Build CTE

		protected virtual bool IsRecursiveCteKeywordRequired => false;
		protected virtual bool IsCteColumnListSupported      => true;

		protected virtual void BuildWithClause(SqlWithClause? with)
		{
			if (with == null || with.Clauses.Count == 0)
				return;

			var first = true;

			foreach (var cte in with.Clauses)
			{
				if (first)
				{
					AppendIndent();
					StringBuilder.Append("WITH ");

					if (IsRecursiveCteKeywordRequired && with.Clauses.Exists(c => c.IsRecursive))
						StringBuilder.Append("RECURSIVE ");

					first = false;
				}
				else
				{
					StringBuilder.AppendLine(Comma);
					AppendIndent();
				}

				BuildObjectName(StringBuilder, new (cte.Name!), ConvertType.NameToCteName, true, TableOptions.NotSet);

				if (IsCteColumnListSupported)
				{
					if (cte.Fields.Count > 3)
					{
						StringBuilder.AppendLine();
						AppendIndent(); StringBuilder.AppendLine(OpenParens);
						++Indent;

						var firstField = true;
						foreach (var field in cte.Fields)
						{
							if (!firstField)
								StringBuilder.AppendLine(Comma);
							firstField = false;
							AppendIndent();
							Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
						}

						--Indent;
						StringBuilder.AppendLine();
						AppendIndent(); StringBuilder.AppendLine(")");
					}
					else if (cte.Fields.Count > 0)
					{
						StringBuilder.Append(" (");

						var firstField = true;
						foreach (var field in cte.Fields)
						{
							if (!firstField)
								StringBuilder.Append(InlineComma);
							firstField = false;
							Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
						}

						StringBuilder.AppendLine(")");
					}
					else
						StringBuilder.Append(' ');
				}
				else
					StringBuilder.Append(' ');

				AppendIndent();
				StringBuilder.AppendLine("AS");
				AppendIndent();
				StringBuilder.AppendLine(OpenParens);

				Indent++;

				BuildCteBody(cte.Body!);

				Indent--;

				AppendIndent();
				StringBuilder.Append(')');
			}

			StringBuilder.AppendLine();
		}

		#endregion

		#region Build Select

		protected virtual void BuildSelectClause(SelectQuery selectQuery)
		{
			AppendIndent();
			StringBuilder.Append("SELECT");

			StartStatementQueryExtensions(selectQuery);

			if (selectQuery.Select.IsDistinct)
				StringBuilder.Append(" DISTINCT");

			BuildSkipFirst(selectQuery);

			StringBuilder.AppendLine();
			BuildColumns(selectQuery);
		}

		protected virtual void StartStatementQueryExtensions(SelectQuery? selectQuery)
		{
			if (selectQuery?.QueryName is {} queryName)
				StringBuilder
					.Append(" /* ")
					.Append(queryName)
					.Append(" */")
					;
		}

		protected virtual void BuildColumns(SelectQuery selectQuery)
		{
			Indent++;

			var first = true;

			var select = ConvertElement(selectQuery.Select);

			foreach (var col in select.Columns)
			{
				if (!first)
					StringBuilder.AppendLine(Comma);

				first = false;

				var addAlias = true;
				var expr     = (ISqlExpression)Optimize(col.Expression, reducePredicates: true);

				AppendIndent();
				BuildColumnExpression(selectQuery, expr, col.Alias, ref addAlias);

				if (!SkipAlias && addAlias && !string.IsNullOrEmpty(col.Alias))
				{
					StringBuilder.Append(" as ");
					Convert(StringBuilder, col.Alias!, ConvertType.NameToQueryFieldAlias);
				}
			}

			if (first)
				AppendIndent().Append('*');

			Indent--;

			StringBuilder.AppendLine();
		}

		protected virtual void BuildOutputColumnExpressions(IReadOnlyList<ISqlExpression> expressions)
		{
			Indent++;

			var first = true;

			foreach (var expr in expressions)
			{
				if (!first)
					StringBuilder.AppendLine(Comma);

				first = false;

				var addAlias  = true;

				AppendIndent();
				BuildColumnExpression(null, expr, null, ref addAlias);
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		protected virtual void BuildColumnExpression(SelectQuery? selectQuery, ISqlExpression expr, string? alias, ref bool addAlias)
		{
			BuildExpression(expr, true, true, alias, ref addAlias, true);
		}

		#endregion

		#region Build Delete

		protected virtual void BuildAlterDeleteClause(SqlDeleteStatement deleteStatement)
		{
		}

		protected virtual void BuildDeleteClause(SqlDeleteStatement deleteStatement)
		{
			AppendIndent();
			StringBuilder.Append("DELETE");
			StartStatementQueryExtensions(deleteStatement.SelectQuery);
			BuildSkipFirst(deleteStatement.SelectQuery);
			StringBuilder.Append(' ');
		}

		#endregion

		#region Build Update

		protected virtual void BuildUpdateWhereClause(SelectQuery selectQuery)
		{
			BuildWhereClause(selectQuery);
		}

		protected virtual void BuildUpdateClause(SqlStatement statement, SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			BuildUpdateTable(selectQuery, updateClause);
			BuildUpdateSet  (selectQuery, updateClause);
		}

		protected virtual void BuildUpdateTable(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			AppendIndent().Append(UpdateKeyword);

			StartStatementQueryExtensions(selectQuery);
			BuildSkipFirst(selectQuery);

			StringBuilder.AppendLine().Append('\t');
			BuildUpdateTableName(selectQuery, updateClause);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildUpdateTableName(SelectQuery selectQuery, SqlUpdateClause updateClause)
		{
			if (updateClause.Table != null && (selectQuery.From.Tables.Count == 0 || updateClause.Table != selectQuery.From.Tables[0].Source))
			{
				BuildPhysicalTable(updateClause.Table, null);
			}
			else
			{
				if (selectQuery.From.Tables[0].Source is SelectQuery)
					StringBuilder.Length--;

				BuildTableName(selectQuery.From.Tables[0], true, true);
			}
		}

		protected virtual string UpdateKeyword => "UPDATE";
		protected virtual string UpdateSetKeyword => "SET";

		protected virtual void BuildUpdateSet(SelectQuery? selectQuery, SqlUpdateClause updateClause)
		{
			AppendIndent()
				.AppendLine(UpdateSetKeyword);

			Indent++;

			var first = true;

			foreach (var expr in updateClause.Items)
			{
				if (!first)
					StringBuilder.AppendLine(Comma);

				first = false;

				AppendIndent();

				if (expr.Column is SqlRowExpression)
				{
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.Update))
						throw new LinqToDBException(ErrorHelper.Error_SqlRow_in_Update);
					if (!SqlProviderFlags.RowConstructorSupport.HasFlag(RowFeature.UpdateLiteral) && expr.Expression is not SelectQuery)
						throw new LinqToDBException(ErrorHelper.Error_SqlRow_in_Update_Value);
				}

				BuildExpression(expr.Column, updateClause.TableSource != null, true, false);

				var updateSet = ConvertElement(expr);

				if (updateSet.Expression != null)
				{
					var updateExpression = updateSet.Expression;

					StringBuilder.Append(" = ");

					var addAlias = false;

					BuildColumnExpression(selectQuery, updateExpression, null, ref addAlias);
				}
			}

			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region Build Insert
		protected virtual string OutputKeyword       => "RETURNING";
		// don't change case, override in specific db builder, if database needs other case
		protected virtual string DeletedOutputTable  => "OLD";
		protected virtual string InsertedOutputTable => "NEW";

		protected void BuildInsertClause(SqlStatement statement, SqlInsertClause insertClause, bool addAlias)
		{
			BuildInsertClause(statement, insertClause, "INSERT INTO ", true, addAlias);
		}

		protected virtual void BuildEmptyInsert(SqlInsertClause insertClause)
		{
			StringBuilder.AppendLine("DEFAULT VALUES");
		}

		protected virtual void BuildOutputSubclause(SqlStatement statement, SqlInsertClause insertClause)
		{
		}

		protected virtual void BuildInsertValuesOverrideClause(SqlStatement statement, SqlInsertClause insertClause)
		{
		}

		protected virtual void BuildOutputSubclause(SqlOutputClause? output)
		{
			if (output?.HasOutput == true)
			{
				output = ConvertElement(output);

				AppendIndent()
					.AppendLine(OutputKeyword);

				++Indent;

				var first = true;

				if (output.HasOutputItems)
				{
					foreach (var oi in output.OutputItems)
					{
						if (!first)
							StringBuilder.AppendLine(Comma);
						first = false;

						AppendIndent();

						BuildExpression(oi.Expression!);
					}

					StringBuilder
						.AppendLine();
				}

				--Indent;

				if (output.OutputColumns != null)
				{
					BuildOutputColumnExpressions(output.OutputColumns);
				}

				if (output.OutputTable != null)
				{
					AppendIndent()
						.Append("INTO ");
					BuildObjectName(StringBuilder, new(output.OutputTable.TableName.Name), ConvertType.NameToQueryTable, true, output.OutputTable.TableOptions);
					StringBuilder
						.AppendLine();

					AppendIndent()
						.AppendLine(OpenParens);

					++Indent;

					var firstColumn = true;
					if (output.HasOutputItems)
					{
						foreach (var oi in output.OutputItems)
						{
							if (!firstColumn)
								StringBuilder.AppendLine(Comma);
							firstColumn = false;

							AppendIndent();

							BuildExpression(oi.Column, false, true);
						}
					}

					StringBuilder
						.AppendLine();

					--Indent;

					AppendIndent()
						.AppendLine(")");
				}
			}
		}

		protected virtual void BuildInsertClause(SqlStatement statement, SqlInsertClause insertClause, string? insertText, bool appendTableName, bool addAlias)
		{
			AppendIndent().Append(insertText);

			StartStatementQueryExtensions(statement.SelectQuery);

			if (appendTableName)
			{
				if (insertClause.Into != null)
				{
					BuildPhysicalTable(insertClause.Into, null);

					if (addAlias)
					{
						var ts    = Statement.SelectQuery!.GetTableSource(insertClause.Into!);
						var alias = GetTableAlias(ts!);

						if (alias != null)
						{
							StringBuilder
								.Append(" AS ");
							Convert(StringBuilder, alias, ConvertType.NameToQueryTableAlias);
						}
					}
				}
			}

			if (insertClause.Items.Count == 0)
			{
				StringBuilder.Append(' ');

				BuildOutputSubclause(statement, insertClause);

				BuildInsertValuesOverrideClause(statement, insertClause);

				BuildEmptyInsert(insertClause);
			}
			else
			{
				StringBuilder.AppendLine();

				AppendIndent().AppendLine(OpenParens);

				Indent++;

				var first = true;

				foreach (var expr in insertClause.Items)
				{
					if (!first)
						StringBuilder.AppendLine(Comma);
					first = false;

					AppendIndent();
					BuildExpression(expr.Column, false, true);
				}

				Indent--;

				StringBuilder.AppendLine();
				AppendIndent().AppendLine(")");

				BuildOutputSubclause(statement, insertClause);

				BuildInsertValuesOverrideClause(statement, insertClause);

				if (statement.QueryType == QueryType.InsertOrUpdate ||
					statement.QueryType == QueryType.MultiInsert ||
					statement.EnsureQuery().From.Tables.Count == 0)
				{
					AppendIndent().AppendLine("VALUES");
					AppendIndent().AppendLine(OpenParens);

					Indent++;

					first = true;

					foreach (var expr in insertClause.Items)
					{
						if (!first)
							StringBuilder.AppendLine(Comma);
						first = false;

						AppendIndent();
						BuildExpression(ConvertElement(expr).Expression!);
					}

					Indent--;

					StringBuilder.AppendLine();
					AppendIndent().AppendLine(")");
				}
			}
		}

		protected virtual void BuildGetIdentity(SqlInsertClause insertClause)
		{
			//throw new LinqToDBException($"Insert with identity is not supported by the '{Name}' sql provider.");
		}

		#endregion

		#region Build InsertOrUpdate

		protected virtual void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			throw new LinqToDBException($"InsertOrUpdate query type is not supported by {Name} provider.");
		}

		protected void BuildInsertOrUpdateQueryAsOnConflictUpdateOrNothing(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertQuery(insertOrUpdate, insertOrUpdate.Insert, true);

			AppendIndent();
			StringBuilder.Append("ON CONFLICT (");

			var firstKey = true;
			foreach (var expr in insertOrUpdate.Update.Keys)
			{
				if (!firstKey)
					StringBuilder.Append(InlineComma);
				firstKey = false;

				BuildExpression(expr.Column, false, true);
			}

			if (insertOrUpdate.Update.Items.Count > 0)
			{
				StringBuilder.AppendLine(") DO UPDATE SET");

				Indent++;

				var first = true;

				foreach (var expr in insertOrUpdate.Update.Items)
				{
					if (!first)
						StringBuilder.AppendLine(Comma);
					first = false;

					var updateItem = ConvertElement(expr);

					AppendIndent();
					BuildExpression(updateItem.Column, false, true);
					StringBuilder.Append(" = ");
					BuildExpression(updateItem.Expression!, true, true);
				}

				Indent--;

				StringBuilder.AppendLine();
			}
			else
			{
				StringBuilder.AppendLine(") DO NOTHING");
			}
		}

		protected virtual void BuildInsertOrUpdateQueryAsMerge(SqlInsertOrUpdateStatement insertOrUpdate, string? fromDummyTable)
		{
			SkipAlias = false;

			var table       = insertOrUpdate.Insert.Into;
			var targetAlias = ConvertInline(insertOrUpdate.SelectQuery.From.Tables[0].Alias!, ConvertType.NameToQueryTableAlias);
			var sourceAlias = ConvertInline(GetTempAliases(1, "s")[0],        ConvertType.NameToQueryTableAlias);
			var keys        = insertOrUpdate.Update.Keys;

			BuildTag(insertOrUpdate);
			AppendIndent().Append("MERGE INTO ");
			BuildPhysicalTable(table!, null);
			StringBuilder.Append(' ').AppendLine(targetAlias);

			AppendIndent().Append("USING (SELECT ");

			for (var i = 0; i < keys.Count; i++)
			{
				BuildExpression(keys[i].Expression!, false, false);
				StringBuilder.Append(" AS ");
				BuildExpression(keys[i].Column, false, false);

				if (i + 1 < keys.Count)
					StringBuilder.Append(InlineComma);
			}

			if (!string.IsNullOrEmpty(fromDummyTable))
				StringBuilder.Append(' ').Append(fromDummyTable);

			StringBuilder.Append(") ").Append(sourceAlias).AppendLine(" ON");

			AppendIndent().AppendLine(OpenParens);

			Indent++;

			for (var i = 0; i < keys.Count; i++)
			{
				var key = keys[i];

				AppendIndent();

				if (key.Column.CanBeNullable(NullabilityContext))
				{
					StringBuilder.Append('(');

					StringBuilder.Append(targetAlias).Append('.');
					BuildExpression(key.Column, false, false);
					StringBuilder.Append(" IS NULL AND ");

					StringBuilder.Append(sourceAlias).Append('.');
					BuildExpression(key.Column, false, false);
					StringBuilder.Append(" IS NULL OR ");
				}

				StringBuilder.Append(targetAlias).Append('.');
				BuildExpression(key.Column, false, false);

				StringBuilder.Append(" = ").Append(sourceAlias).Append('.');
				BuildExpression(key.Column, false, false);

				if (key.Column.CanBeNullable(NullabilityContext))
					StringBuilder.Append(')');

				if (i + 1 < keys.Count)
					StringBuilder.Append(" AND");

				StringBuilder.AppendLine();
			}

			Indent--;

			AppendIndent().AppendLine(")");

			if (insertOrUpdate.Update.Items.Count > 0)
			{
				AppendIndent().AppendLine("WHEN MATCHED THEN");

				Indent++;
				AppendIndent().AppendLine("UPDATE ");
				BuildUpdateSet(insertOrUpdate.SelectQuery, insertOrUpdate.Update);
				Indent--;
			}

			AppendIndent().AppendLine("WHEN NOT MATCHED THEN");

			Indent++;
			BuildInsertClause(insertOrUpdate, insertOrUpdate.Insert, "INSERT", false, false);
			Indent--;

			while (EndLine.Contains(StringBuilder[StringBuilder.Length - 1]))
				StringBuilder.Length--;
		}

		protected static readonly char[] EndLine = { ' ', '\r', '\n' };

		protected void BuildInsertOrUpdateQueryAsUpdateInsert(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildTag(insertOrUpdate);

			var buildUpdate = insertOrUpdate.Update.Items.Count > 0;
			if (buildUpdate)
			{
				BuildUpdateQuery(insertOrUpdate, insertOrUpdate.SelectQuery, insertOrUpdate.Update);
			}
			else
			{
				AppendIndent().Append("IF NOT EXISTS").AppendLine(OpenParens);
				Indent++;
				AppendIndent().AppendLine("SELECT 1 ");
				BuildFromClause(insertOrUpdate, insertOrUpdate.SelectQuery);
			}

			AppendIndent().AppendLine("WHERE");

			var alias = ConvertInline(insertOrUpdate.SelectQuery.From.Tables[0].Alias!, ConvertType.NameToQueryTableAlias);
			var exprs = insertOrUpdate.Update.Keys;

			Indent++;

			for (var i = 0; i < exprs.Count; i++)
			{
				var expr = exprs[i];

				AppendIndent();

				if (expr.Column.CanBeNullable(NullabilityContext))
				{
					StringBuilder.Append('(');

					StringBuilder.Append(alias).Append('.');
					BuildExpression(expr.Column, false, false);
					StringBuilder.Append(" IS NULL OR ");
				}

				StringBuilder.Append(alias).Append('.');
				BuildExpression(expr.Column, false, false);

				StringBuilder.Append(" = ");
				BuildExpression(Precedence.Comparison, expr.Expression!);

				if (expr.Column.CanBeNullable(NullabilityContext))
					StringBuilder.Append(')');

				if (i + 1 < exprs.Count)
					StringBuilder.Append(" AND");

				StringBuilder.AppendLine();
			}

			Indent--;

			if (buildUpdate)
			{
				StringBuilder.AppendLine();
				AppendIndent().AppendLine("IF @@ROWCOUNT = 0");
			}
			else
			{
				Indent--;
				AppendIndent().AppendLine(")");
			}

			AppendIndent().AppendLine("BEGIN");

			Indent++;

			BuildInsertQuery(insertOrUpdate, insertOrUpdate.Insert, false);

			Indent--;

			AppendIndent().AppendLine("END");

			StringBuilder.AppendLine();
		}

		#endregion

		#region Build DDL

		protected virtual void BuildTruncateTableStatement(SqlTruncateTableStatement truncateTable)
		{
			BuildTag(truncateTable);

			var table = truncateTable.Table;

			AppendIndent();

			BuildTruncateTable(truncateTable);

			BuildPhysicalTable(table!, null);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildTruncateTable(SqlTruncateTableStatement truncateTable)
		{
			//StringBuilder.Append("TRUNCATE TABLE ");
			StringBuilder.Append("DELETE FROM ");
		}

		protected virtual void BuildDropTableStatement(SqlDropTableStatement dropTable)
		{
			BuildTag(dropTable);
			AppendIndent().Append("DROP TABLE ");
			BuildPhysicalTable(dropTable.Table!, null);
			StringBuilder.AppendLine();
		}

		protected void BuildDropTableStatementIfExists(SqlDropTableStatement dropTable)
		{
			BuildTag(dropTable);
			AppendIndent().Append("DROP TABLE ");

			if (dropTable.Table.TableOptions.HasDropIfExists())
				StringBuilder.Append("IF EXISTS ");

			BuildPhysicalTable(dropTable.Table!, null);
			StringBuilder.AppendLine();
		}

		protected virtual void BuildCreateTableCommand(SqlTable table)
		{
			StringBuilder
				.Append("CREATE TABLE ");
		}

		protected virtual void BuildStartCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementHeader == null)
			{
				AppendIndent();
				BuildCreateTableCommand(createTable.Table!);
				BuildPhysicalTable(createTable.Table!, null);
			}
			else
			{
				var name = WithStringBuilder(
					static ctx =>
					{
						ctx.this_.BuildPhysicalTable(ctx.createTable.Table!, null);
					}, (this_: this, createTable));

				AppendIndent().AppendFormat(CultureInfo.InvariantCulture, createTable.StatementHeader, name);
			}
		}

		protected virtual void BuildEndCreateTableStatement(SqlCreateTableStatement createTable)
		{
			if (createTable.StatementFooter != null)
				AppendIndent().Append(createTable.StatementFooter);
		}

		sealed class CreateFieldInfo
		{
			public SqlField      Field = null!;
			public StringBuilder StringBuilder = null!;
			public string        Name = null!;
			public string?       Type;
			public string        Identity = null!;
			public string?       Null;
		}

		protected virtual void BuildCreateTableStatement(SqlCreateTableStatement createTable)
		{
			var table = createTable.Table;

			BuildStartCreateTableStatement(createTable);

			StringBuilder.AppendLine();
			AppendIndent().Append(OpenParens);
			Indent++;

			// Order columns by the Order field. Positive first then negative.
			var orderedFields = table.Fields.OrderBy(_ => _.CreateOrder >= 0 ? 0 : (_.CreateOrder == null ? 1 : 2)).ThenBy(_ => _.CreateOrder);
			var fields = orderedFields.Select(f => new CreateFieldInfo { Field = f, StringBuilder = new StringBuilder() }).ToList();
			var maxlen = 0;

			var isAnyCreateFormat = false;

			// Build field name.
			//
			foreach (var field in fields)
			{
				Convert(field.StringBuilder, field.Field.PhysicalName, ConvertType.NameToQueryField);

				if (maxlen < field.StringBuilder.Length)
					maxlen = field.StringBuilder.Length;

				if (field.Field.CreateFormat != null)
					isAnyCreateFormat = true;
			}

			AppendToMax(fields, maxlen, true);

			if (isAnyCreateFormat)
				foreach (var field in fields)
					if (field.Field.CreateFormat != null)
						field.Name = field.StringBuilder.ToString() + ' ';

			// Build field type.
			//
			foreach (var field in fields)
			{
				field.StringBuilder.Append(' ');

				if (!string.IsNullOrEmpty(field.Field.Type.DbType))
					field.StringBuilder.Append(field.Field.Type.DbType);
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

			AppendToMax(fields, maxlen, true);

			if (isAnyCreateFormat)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat != null)
					{
						var sb = field.StringBuilder;
						sb.Remove(0, field.Name.Length);
						sb.Append(' ');

						field.Type = sb.ToString();
						sb.Length = 0;
					}
				}
			}

			var hasIdentity = fields.Exists(f => f.Field.IsIdentity);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat == null)
						field.StringBuilder.Append(' ');

					if (field.Field.IsIdentity)
						WithStringBuilder(field.StringBuilder, static ctx => ctx.this_.BuildCreateTableIdentityAttribute1(ctx.field.Field), (this_: this, field));

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

				AppendToMax(fields, maxlen, false);
			}

			// Build nullable attribute.
			//
			foreach (var field in fields)
			{
				if (field.Field.CreateFormat == null)
					field.StringBuilder.Append(' ');

				WithStringBuilder(
					field.StringBuilder,
					static ctx => ctx.this_.BuildCreateTableNullAttribute(ctx.field.Field, ctx.createTable.DefaultNullable),
					(this_: this, field, createTable));

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

			AppendToMax(fields, maxlen, false);

			// Build identity attribute.
			//
			if (hasIdentity)
			{
				foreach (var field in fields)
				{
					if (field.Field.CreateFormat == null)
						field.StringBuilder.Append(' ');

					if (field.Field.IsIdentity)
						WithStringBuilder(field.StringBuilder, static ctx => ctx.this_.BuildCreateTableIdentityAttribute2(ctx.field.Field), (this_: this, field));

					if (field.Field.CreateFormat != null)
					{
						if (field.Identity.Length == 0)
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

				AppendToMax(fields, maxlen, false);
			}

			// Build fields.
			//
			for (var i = 0; i < fields.Count; i++)
			{
				while (fields[i].StringBuilder.Length > 0 && fields[i].StringBuilder[fields[i].StringBuilder.Length - 1] == ' ')
					fields[i].StringBuilder.Length--;

				StringBuilder.AppendLine(i == 0 ? "" : Comma);
				AppendIndent();

				var field = fields[i];

				if (field.Field.CreateFormat != null)
				{
					StringBuilder.AppendFormat(CultureInfo.InvariantCulture, field.Field.CreateFormat, field.Name, field.Type, field.Null, field.Identity);

					while (StringBuilder.Length > 0 && StringBuilder[StringBuilder.Length - 1] == ' ')
						StringBuilder.Length--;
				}
				else
				{
					StringBuilder.AppendBuilder(field.StringBuilder);
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
				StringBuilder.AppendLine(Comma).AppendLine();

				var pkName = "PK_" + createTable.Table.TableName.Name;

				if (DataProvider != null)
				{
					var iIdentifierService = ((IInfrastructure<IServiceProvider>)DataProvider).Instance.GetRequiredService<IIdentifierService>();

					pkName = IdentifiersHelper.TruncateIdentifier(iIdentifierService, IdentifierKind.PrimaryKey, pkName);
				}

				BuildCreateTablePrimaryKey(createTable, ConvertInline(pkName, ConvertType.NameToQueryTable),
					pk.Select(f => ConvertInline(f.Field.PhysicalName, ConvertType.NameToQueryField)));
			}

			Indent--;
			StringBuilder.AppendLine();
			AppendIndent().AppendLine(")");

			BuildEndCreateTableStatement(createTable);

			static void AppendToMax(IEnumerable<CreateFieldInfo> fields, int maxlen, bool addCreateFormat)
			{
				foreach (var field in fields)
					if (addCreateFormat || field.Field.CreateFormat == null)
						while (maxlen > field.StringBuilder.Length)
							field.StringBuilder.Append(' ');
			}
		}

		internal void BuildTypeName(StringBuilder sb, DbDataType type)
		{
			StringBuilder = sb;
			BuildDataType(type, forCreateTable: true, canBeNull: true);
		}

		protected virtual void BuildCreateTableFieldType(SqlField field)
		{
			BuildDataType(QueryHelper.GetDbDataType(field, MappingSchema), forCreateTable: true, field.CanBeNull);
		}

		protected virtual void BuildCreateTableNullAttribute(SqlField field, DefaultNullable defaultNullable)
		{
			if (defaultNullable == DefaultNullable.Null && field.CanBeNull)
				return;

			if (defaultNullable == DefaultNullable.NotNull && !field.CanBeNull)
				return;

			StringBuilder.Append(field.CanBeNull ? "    NULL" : "NOT NULL");
		}

		protected virtual void BuildCreateTableIdentityAttribute1(SqlField field)
		{
		}

		protected virtual void BuildCreateTableIdentityAttribute2(SqlField field)
		{
		}

		protected virtual void BuildCreateTablePrimaryKey(SqlCreateTableStatement createTable, string pkName, IEnumerable<string> fieldNames)
		{
			AppendIndent();
			StringBuilder.Append("CONSTRAINT ").Append(pkName).Append(" PRIMARY KEY (");
			StringBuilder.AppendJoinStrings(InlineComma, fieldNames);
			StringBuilder.Append(')');
		}

		#endregion

		#region Build From

		protected virtual void BuildDeleteFromClause(SqlDeleteStatement deleteStatement)
		{
			BuildFromClause(Statement, deleteStatement.SelectQuery);
		}

		protected virtual void BuildFromClause(SqlStatement statement, SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count == 0 || string.Equals(selectQuery.From.Tables[0].Alias, "$F", StringComparison.Ordinal))
				return;

			AppendIndent();

			StringBuilder.Append("FROM").AppendLine();

			Indent++;
			AppendIndent();

			var first = true;

			foreach (var ts in selectQuery.From.Tables)
			{
				if (!first)
				{
					StringBuilder.AppendLine(Comma);
					AppendIndent();
				}

				first = false;

				var jn = ParenthesizeJoin(ts.Joins) ? ts.GetJoinNumber() : 0;

				if (jn > 0)
				{
					jn--;
					for (var i = 0; i < jn; i++)
						StringBuilder.Append('(');
				}

				BuildTableName(ts, true, true);

				foreach (var jt in ts.Joins)
					BuildJoinTable(selectQuery, ts, jt, ref jn);
			}

			BuildFromExtensions(selectQuery);

			Indent--;

			StringBuilder.AppendLine();
		}

		protected virtual void BuildFromExtensions(SelectQuery selectQuery)
		{
		}

		private const string SelectPattern = /* lang=regex */ @"^[\W\r\n]*select\b";
#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(SelectPattern, RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1)]
		private static partial Regex SelectRegex();
#else
		private static readonly Regex _selectDetector = new (SelectPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(1));
		private static Regex SelectRegex() => _selectDetector;
#endif

		protected virtual bool? BuildPhysicalTable(ISqlTableSource table, string? alias, string? defaultDatabaseName = null)
		{
			var tablePath = TablePath;

			if (alias != null)
			{
				if (TablePath is { Length: > 0 })
					TablePath += '.';
				TablePath += alias;
			}

			bool? buildAlias = null;

			switch (table.ElementType)
			{
				case QueryElementType.SqlTable        :
				case QueryElementType.TableSource     :
				{
					var name = GetPhysicalTableName(table, alias, defaultDatabaseName : defaultDatabaseName);

					StringBuilder.Append(name);

					if (alias != null && table is SqlTable { ID: {} id })
					{
						var path = TablePath;

						if (QueryName is not null)
							path += $"@{QueryName}";

						(TableIDs ??= new(StringComparer.Ordinal))[id] = new(alias, name, path!);
					}

					break;
				}

				case QueryElementType.SqlQuery        :
					StringBuilder.AppendLine(OpenParens);
					BuildSqlBuilder((SelectQuery)table, Indent + 1, false);
					AppendIndent().Append(')');
					break;

				case QueryElementType.SqlCteTable     :
				case QueryElementType.SqlTableLikeSource:
					StringBuilder.Append(GetPhysicalTableName(table, alias));
					break;

				case QueryElementType.SqlRawSqlTable  :

					var rawSqlTable = (SqlRawSqlTable)table;

					var appendParentheses = SelectRegex().IsMatch(rawSqlTable.SQL);
					var multiLine         = appendParentheses || rawSqlTable.SQL.Contains('\n', StringComparison.Ordinal);

					if (appendParentheses)
						StringBuilder.AppendLine(OpenParens);
					else if (multiLine)
						StringBuilder.AppendLine();

					var parameters = rawSqlTable.Parameters;
					if (rawSqlTable.Parameters.Any(e => e.ElementType == QueryElementType.SqlAliasPlaceholder))
					{
						buildAlias = false;
						var aliasExpr = new SqlFragment(ConvertInline(alias!, ConvertType.NameToQueryTableAlias));
						parameters = rawSqlTable.Parameters.Select(e =>
								e.ElementType == QueryElementType.SqlAliasPlaceholder ? aliasExpr : e)
							.ToArray();
					}

					BuildFormatValues(IdentText(rawSqlTable.SQL, multiLine ? Indent + 1 : 0), parameters, Precedence.Primary);

					if (multiLine)
						StringBuilder.AppendLine();
					if (appendParentheses)
						AppendIndent().Append(')');

					if (rawSqlTable.IsScalar && alias != null && SupportsColumnAliasesInSource && buildAlias != false)
					{
						StringBuilder.Append(' ');
						BuildObjectName(StringBuilder, new(alias), ConvertType.NameToQueryFieldAlias, true, TableOptions.NotSet);
						StringBuilder.Append('(');
						BuildExpression(rawSqlTable.Fields[0], buildTableName: false, checkParentheses: false);
						StringBuilder.Append(')');
						buildAlias = false;
					}

					break;

				case QueryElementType.SqlValuesTable:
				{
					if (alias == null)
						throw new LinqToDBException("Alias required for SqlValuesTable.");
					BuildSqlValuesTable((SqlValuesTable)table, alias, out var aliasBuilt);
					buildAlias = !aliasBuilt;
					break;
				}

				default:
					throw new InvalidOperationException($"Unexpected table type {table.ElementType}");
			}

			TablePath = tablePath;

			return buildAlias;
		}

		protected virtual void BuildSqlValuesTable(SqlValuesTable valuesTable, string alias, out bool aliasBuilt)
		{
			valuesTable = ConvertElement(valuesTable);
			var rows = valuesTable.BuildRows(OptimizationContext.EvaluationContext);
			if (rows?.Count > 0)
			{
				StringBuilder.Append(OpenParens);

				if (IsValuesSyntaxSupported)
					BuildValues(valuesTable, rows);
				else
					BuildValuesAsSelectsUnion(valuesTable.Fields, valuesTable, rows);

				StringBuilder.Append(')');
			}
			else if (IsEmptyValuesSourceSupported)
			{
				StringBuilder.Append(OpenParens);
				BuildEmptyValues(valuesTable);
				StringBuilder.Append(')');
			}
			else
				throw new LinqToDBException($"{Name} doesn't support values with empty source");

			aliasBuilt = IsValuesSyntaxSupported;
			if (aliasBuilt)
			{
				BuildSqlValuesAlias(valuesTable, alias);
			}
		}

		private void BuildSqlValuesAlias(SqlValuesTable valuesTable, string alias)
		{
			valuesTable = ConvertElement(valuesTable);
			StringBuilder.Append(' ');

			BuildObjectName(StringBuilder, new (alias), ConvertType.NameToQueryFieldAlias, true, TableOptions.NotSet);

			if (SupportsColumnAliasesInSource)
			{
				StringBuilder.Append(OpenParens);

				var first = true;
				foreach (var field in valuesTable.Fields)
				{
					if (!first)
						StringBuilder.Append(Comma).Append(' ');

					first = false;
					Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
				}

				StringBuilder.Append(')');
			}
		}

		protected void BuildEmptyValues(SqlValuesTable valuesTable, bool useTypedExpression = true)
		{
			StringBuilder.Append("SELECT ");
			for (var i = 0; i < valuesTable.Fields.Count; i++)
			{
				if (i > 0)
					StringBuilder.Append(InlineComma);
				var field = valuesTable.Fields[i];
				if (useTypedExpression)
					BuildTypedExpression(QueryHelper.GetDbDataType(field, MappingSchema), new SqlValue(field.Type, null));
				else
					BuildExpression(new SqlValue(field.Type, null));
				StringBuilder.Append(' ');
				Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
			}

			if (FakeTable != null)
			{
				StringBuilder.Append(" FROM ");
				BuildFakeTableName();
			}

			StringBuilder
				.Append(" WHERE 1 = 0");
		}

		protected void BuildTableName(SqlTableSource ts, bool buildName, bool buildAlias)
		{
			string? alias = null;

			if (buildName)
			{
				alias = GetTableAlias(ts);
				var isBuildAlias = BuildPhysicalTable(ts.Source, alias);
				if (isBuildAlias is false)
					buildAlias = false;
			}

			if (buildAlias)
			{
				if (ts.SqlTableType != SqlTableType.Expression)
				{

					if (buildName)
					{
						if (ts.Source is SqlTable { SqlQueryExtensions: not null } t)
						{
							BuildTableNameExtensions(t);
						}

					}
					else
					{
						alias = GetTableAlias(ts);
					}

					if (!string.IsNullOrEmpty(alias))
					{
						if (buildName)
							StringBuilder.Append(' ');
						Convert(StringBuilder, alias!, ConvertType.NameToQueryTableAlias);
					}
				}
			}

			if (buildName && buildAlias && ts.Source is SqlTable { SqlQueryExtensions: not null } table)
			{
				BuildTableExtensions(table, alias!);
			}
		}

		protected virtual void BuildTableExtensions(SqlTable table, string alias)
		{
		}

		protected virtual void BuildTableNameExtensions(SqlTable table)
		{
		}

		static readonly ConcurrentDictionary<Type,ISqlExtensionBuilder> _extensionBuilders = new()
		{
			[typeof(NoneExtensionBuilder)]               = new NoneExtensionBuilder(),
			[typeof(HintExtensionBuilder)]               = new HintExtensionBuilder(),
			[typeof(HintWithParameterExtensionBuilder)]  = new HintWithParameterExtensionBuilder(),
			[typeof(HintWithParametersExtensionBuilder)] = new HintWithParametersExtensionBuilder(),
		};

		protected static ISqlExtensionBuilder GetExtensionBuilder(Type builderType)
		{
			return _extensionBuilders.GetOrAdd(builderType, ActivatorExt.CreateInstance<ISqlExtensionBuilder>);
		}

		protected void BuildTableExtensions(
			StringBuilder sb,
			SqlTable table, string alias,
			string? prefix, string delimiter, string? suffix)
		{
			BuildTableExtensions(sb,
				table,  alias,
				prefix, delimiter, suffix,
				ext =>
					ext.Scope is
						Sql.QueryExtensionScope.TableHint or
						Sql.QueryExtensionScope.IndexHint or
						Sql.QueryExtensionScope.TablesInScopeHint);
		}

		protected void BuildTableExtensions(
			StringBuilder sb,
			SqlTable table, string alias,
			string? prefix, string delimiter, string? suffix,
			Func<SqlQueryExtension,bool> tableExtensionFilter)
		{
			if (table.SqlQueryExtensions?.Any(tableExtensionFilter) == true)
			{
				if (prefix != null)
					sb.Append(prefix);

				foreach (var ext in table.SqlQueryExtensions.Where(tableExtensionFilter))
				{
					if (ext.BuilderType != null)
					{
						var extensionBuilder = GetExtensionBuilder(ext.BuilderType);

						switch (extensionBuilder)
						{
							case ISqlQueryExtensionBuilder queryExtensionBuilder:
								queryExtensionBuilder.Build(NullabilityContext, this, sb, ext);
								break;
							case ISqlTableExtensionBuilder tableExtensionBuilder:
								tableExtensionBuilder.Build(NullabilityContext, this, sb, ext, table, alias);
								break;
							default:
								throw new LinqToDBException($"Type '{ext.BuilderType.FullName}' must implement either '{typeof(ISqlQueryExtensionBuilder).FullName}' or '{typeof(ISqlTableExtensionBuilder).FullName}' interface.");
						}
					}

					sb.Append(delimiter);
				}

				sb.Length -= delimiter.Length;

				if (suffix != null)
					sb.Append(suffix);
			}
		}

		protected void BuildQueryExtensions(
			StringBuilder           sb,
			List<SqlQueryExtension> sqlQueryExtensions,
			string?                 prefix,
			string                  delimiter,
			string?                 suffix,
			Sql.QueryExtensionScope scope)
		{
			if (sqlQueryExtensions.Exists(ext => ext.Scope == scope))
			{
				if (prefix != null)
					sb.Append(prefix);

				foreach (var ext in sqlQueryExtensions!)
				{
					var convertedExt = ConvertElement(ext);
					if (convertedExt.BuilderType != null)
					{
						var extensionBuilder = GetExtensionBuilder(convertedExt.BuilderType!);

						switch (extensionBuilder)
						{
							case ISqlQueryExtensionBuilder queryExtensionBuilder:
								queryExtensionBuilder.Build(NullabilityContext, this, sb, convertedExt);
								break;
							default:
								throw new LinqToDBException($"Type '{convertedExt.BuilderType.FullName}' must implement either '{typeof(ISqlQueryExtensionBuilder).FullName}' or '{typeof(ISqlTableExtensionBuilder).FullName}' interface.");
						}
					}

					sb.Append(delimiter);
				}

				sb.Length -= delimiter.Length;

				if (suffix != null)
					sb.Append(suffix);
			}
		}

		protected void BuildJoinTable(SelectQuery selectQuery, SqlTableSource tableSource, SqlJoinedTable join, ref int joinCounter)
		{
			StringBuilder.AppendLine();
			Indent++;
			AppendIndent();

			var condition = ConvertElement(join.Condition);
			var buildOn   = BuildJoinType (join, condition);

			if (IsNestedJoinParenthesisRequired && join.Table.Joins.Count != 0)
				StringBuilder.Append('(');

			BuildTableName(join.Table, true, true);

			if (IsNestedJoinSupported && join.Table.Joins.Count != 0)
			{
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(selectQuery, tableSource, jt, ref joinCounter);

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

			if (WrapJoinCondition && condition.Predicates.Count > 0)
				StringBuilder.Append('(');

			if (buildOn)
			{
				if (!condition.IsTrue())
				{
					var saveNullability = NullabilityContext;
					NullabilityContext = NullabilityContext.WithJoinSource(join.Table.Source).WithQuery(selectQuery);

					BuildSearchCondition(Precedence.Unknown, condition, wrapCondition : false);

					NullabilityContext = saveNullability;
				}
				else
					StringBuilder.Append("1=1");
			}

			if (WrapJoinCondition && condition.Predicates.Count > 0)
				StringBuilder.Append(')');

			if (joinCounter > 0)
			{
				joinCounter--;
				StringBuilder.Append(')');
			}

			if (!IsNestedJoinSupported)
				foreach (var jt in join.Table.Joins)
					BuildJoinTable(selectQuery, tableSource, jt, ref joinCounter);

			Indent--;
		}

		protected virtual bool BuildJoinType(SqlJoinedTable join, SqlSearchCondition condition)
		{
			switch (join.JoinType)
			{
				case JoinType.Cross     : StringBuilder.Append("CROSS JOIN ");  return false;
				case JoinType.Inner     : StringBuilder.Append("INNER JOIN ");  return true;
				case JoinType.Left      : StringBuilder.Append("LEFT JOIN ");   return true;
				case JoinType.CrossApply: StringBuilder.Append("CROSS APPLY "); return false;
				case JoinType.OuterApply: StringBuilder.Append("OUTER APPLY "); return false;
				case JoinType.Right     : StringBuilder.Append("RIGHT JOIN ");  return true;
				case JoinType.Full      : StringBuilder.Append("FULL JOIN ");   return true;
				default: throw new InvalidOperationException();
			}
		}

#endregion

		#region Where Clause

		protected virtual bool ShouldBuildWhere(SelectQuery selectQuery, out SqlSearchCondition condition)
		{
			condition = PrepareSearchCondition(selectQuery.Where.SearchCondition);

			if (condition.IsTrue())
				return false;

			return true;
		}

		protected virtual void BuildWhereClause(SelectQuery selectQuery)
		{
			if (!ShouldBuildWhere(selectQuery, out var searchCondition))
				return;

			AppendIndent();

			StringBuilder.Append("WHERE").AppendLine();

			Indent++;
			AppendIndent();
			BuildWhereSearchCondition(selectQuery, searchCondition);
			Indent--;

			StringBuilder.AppendLine();
		}

		#endregion

		#region GroupBy Clause

		protected virtual void BuildGroupByClause(SelectQuery selectQuery)
		{
			if (selectQuery.GroupBy.Items.Count == 0)
				return;

			BuildGroupByBody(selectQuery.GroupBy.GroupingType, selectQuery.GroupBy.Items);
		}

		protected virtual void BuildGroupByBody(GroupingType groupingType, List<ISqlExpression> items)
		{
			AppendIndent();

			StringBuilder.Append("GROUP BY");

			switch (groupingType)
			{
				case GroupingType.Default:
					break;
				case GroupingType.GroupBySets:
					StringBuilder.Append(" GROUPING SETS");
					break;
				case GroupingType.Rollup:
					StringBuilder.Append(" ROLLUP");
					break;
				case GroupingType.Cube:
					StringBuilder.Append(" CUBE");
					break;
				default:
					throw new InvalidOperationException($"Unexpected grouping type: {groupingType}");
			}

			if (groupingType != GroupingType.Default)
				StringBuilder.Append(' ').AppendLine(OpenParens);
			else
				StringBuilder.AppendLine();

			Indent++;

			for (var i = 0; i < items.Count; i++)
			{
				AppendIndent();

				var expr = items[i];
				BuildExpression(expr);

				if (i + 1 < items.Count)
					StringBuilder.AppendLine(Comma);
				else
					StringBuilder.AppendLine();
			}

			Indent--;

			if (groupingType != GroupingType.Default)
			{
				AppendIndent();
				StringBuilder.Append(')').AppendLine();
			}
		}

		#endregion

		#region Having Clause

		protected virtual void BuildHavingClause(SelectQuery selectQuery)
		{
			var condition = PrepareSearchCondition(selectQuery.Having.SearchCondition);
			if (condition.IsTrue())
				return;

			++_binaryOptimized;

			AppendIndent();

			StringBuilder.Append("HAVING").AppendLine();

			Indent++;
			AppendIndent();
			BuildWhereSearchCondition(selectQuery, condition);
			Indent--;

			StringBuilder.AppendLine();

			--_binaryOptimized;
		}

		#endregion

		#region OrderBy Clause

		protected virtual void BuildOrderByClause(SelectQuery selectQuery)
		{
			if (selectQuery.OrderBy.Items.Count == 0)
				return;

			var orderBy = ConvertElement(selectQuery.OrderBy);

			IReadOnlyList<SqlOrderByItem> nonConstant = orderBy.Items.TrueForAll(i => !QueryHelper.IsConstantFast(i.Expression))
				? orderBy.Items
				: orderBy.Items.Where(i => !QueryHelper.IsConstantFast(i.Expression))
					.ToList();

			if (nonConstant.Count == 0)
				return;

			AppendIndent();

			StringBuilder.Append("ORDER BY").AppendLine();

			Indent++;

			for (var i = 0; i < nonConstant.Count; i++)
			{
				AppendIndent();

				var item            = nonConstant[i];
				var orderExpression = item.Expression;

				if (item.IsPositioned)
				{
					var idx = selectQuery.Select.Columns.FindIndex(c => c.Expression.Equals(item.Expression));
					if (idx >= 0)
					{
						orderExpression = new SqlFragment((idx + 1).ToString(CultureInfo.InvariantCulture));
					}
				}

				BuildExpressionForOrderBy(orderExpression);

				if (item.IsDescending)
					StringBuilder.Append(" DESC");

				if (i + 1 < nonConstant.Count)
					StringBuilder.AppendLine(Comma);
				else
					StringBuilder.AppendLine();
			}

			Indent--;
		}

		protected virtual void BuildExpressionForOrderBy(ISqlExpression expr)
		{
			BuildExpression(expr);
		}

		#endregion

		#region Skip/Take

		protected virtual bool   SkipFirst    => true;
		protected virtual string? SkipFormat  => null;
		protected virtual string? FirstFormat (SelectQuery selectQuery) => null;
		protected virtual string? LimitFormat (SelectQuery selectQuery) => null;
		protected virtual string? OffsetFormat(SelectQuery selectQuery) => null;
		protected virtual bool   OffsetFirst  => false;
		protected virtual string TakePercent  => "PERCENT";
		protected virtual string TakeTies     => "WITH TIES";

		protected bool NeedSkip(ISqlExpression? takeExpression, ISqlExpression? skipExpression)
			=> skipExpression != null && SqlProviderFlags.GetIsSkipSupportedFlag(takeExpression);

		protected bool NeedTake(ISqlExpression? takeExpression)
			=> takeExpression != null;

		protected virtual void BuildSkipFirst(SelectQuery selectQuery)
		{
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

			if (SkipFirst && NeedSkip(takeExpr, skipExpr) && SkipFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					CultureInfo.InvariantCulture, SkipFormat, WithStringBuilderBuildExpression(skipExpr!));

			if (NeedTake(takeExpr) && FirstFormat(selectQuery) != null)
			{
				var saveStep = BuildStep;
				BuildStep = Step.OffsetLimit;

				StringBuilder.Append(' ').AppendFormat(
					CultureInfo.InvariantCulture, FirstFormat(selectQuery)!, WithStringBuilderBuildExpression(takeExpr!));

				BuildStep = saveStep;

				BuildTakeHints(selectQuery);
			}

			if (!SkipFirst && NeedSkip(takeExpr, skipExpr) && SkipFormat != null)
				StringBuilder.Append(' ').AppendFormat(
					CultureInfo.InvariantCulture, SkipFormat, WithStringBuilderBuildExpression(skipExpr!));
		}

		protected virtual void BuildTakeHints(SelectQuery selectQuery)
		{
			if (selectQuery.Select.TakeHints == null)
				return;

			if ((selectQuery.Select.TakeHints.Value & TakeHints.Percent) != 0)
				StringBuilder.Append(' ').Append(TakePercent);

			if ((selectQuery.Select.TakeHints.Value & TakeHints.WithTies) != 0)
				StringBuilder.Append(' ').Append(TakeTies);
		}

		protected virtual void BuildOffsetLimit(SelectQuery selectQuery)
		{
			SqlOptimizer.ConvertSkipTake(NullabilityContext, MappingSchema, DataOptions, selectQuery, OptimizationContext, out var takeExpr, out var skipExpr);

			var doSkip = NeedSkip(takeExpr, skipExpr) && OffsetFormat(selectQuery) != null;
			var doTake = NeedTake(takeExpr)           && LimitFormat(selectQuery)  != null;

			if (doSkip || doTake)
			{
				AppendIndent();

				if (doSkip && OffsetFirst)
				{
					StringBuilder.AppendFormat(
						CultureInfo.InvariantCulture, OffsetFormat(selectQuery)!, WithStringBuilderBuildExpression(skipExpr!));

					if (doTake)
						StringBuilder.Append(' ');
				}

				if (doTake)
				{
					StringBuilder.AppendFormat(
						CultureInfo.InvariantCulture, LimitFormat(selectQuery)!, WithStringBuilderBuildExpression(takeExpr!));

					if (doSkip)
						StringBuilder.Append(' ');
				}

				if (doSkip && !OffsetFirst)
					StringBuilder.AppendFormat(
						CultureInfo.InvariantCulture, OffsetFormat(selectQuery)!, WithStringBuilderBuildExpression(skipExpr!));

				StringBuilder.AppendLine();
			}
		}

		#endregion

		#region Builders

		#region BuildSearchCondition

		protected virtual void BuildWhereSearchCondition(SelectQuery selectQuery, SqlSearchCondition condition)
		{
			BuildSearchCondition(Precedence.Unknown, condition, wrapCondition : true);
		}

		protected virtual void BuildSearchCondition(SqlSearchCondition condition, bool wrapCondition)
		{
			condition = PrepareSearchCondition(condition);

			var len = StringBuilder.Length;
			var parentPrecedence = condition.Precedence;

			if (condition.Predicates.Count == 0)
			{
				BuildPredicate(parentPrecedence, parentPrecedence,
					new SqlPredicate.ExprExpr(
						new SqlValue(true),
						SqlPredicate.Operator.Equal,
						new SqlValue(true), false));
			}

			var isFirst = true;

			foreach (var predicate in condition.Predicates)
			{
				if (!isFirst)
				{
					StringBuilder.Append(condition.IsOr ? " OR" : " AND");

					if (condition.Predicates.Count < 4 && StringBuilder.Length - len < 50 || !wrapCondition)
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
				else
					isFirst = false;

				var precedence = GetPrecedence(predicate);

				BuildPredicate(parentPrecedence, condition.Predicates.Count == 1 ? parentPrecedence : precedence, predicate);
			}
		}

		protected virtual void BuildSearchCondition(int parentPrecedence, SqlSearchCondition condition, bool wrapCondition)
		{
			var wrap = Wrap(GetPrecedence(condition as ISqlExpression), parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildSearchCondition(condition, wrapCondition);
			if (wrap) StringBuilder.Append(')');
		}

		#endregion

		#region BuildPredicate

		protected virtual void BuildPredicate(ISqlPredicate predicate)
		{
			switch (predicate.ElementType)
			{
				case QueryElementType.ExprExprPredicate:
					BuildExprExprPredicate((SqlPredicate.ExprExpr) predicate);
					break;

				case QueryElementType.LikePredicate:
					BuildLikePredicate((SqlPredicate.Like)predicate);
					break;

				case QueryElementType.BetweenPredicate:
					{
						BuildExpression(GetPrecedence((SqlPredicate.Between)predicate), ((SqlPredicate.Between)predicate).Expr1);
						if (((SqlPredicate.Between)predicate).IsNot) StringBuilder.Append(" NOT");
						StringBuilder.Append(" BETWEEN ");
						BuildExpression(GetPrecedence((SqlPredicate.Between)predicate), ((SqlPredicate.Between)predicate).Expr2);
						StringBuilder.Append(" AND ");
						BuildExpression(GetPrecedence((SqlPredicate.Between)predicate), ((SqlPredicate.Between)predicate).Expr3);
					}

					break;

				case QueryElementType.IsNullPredicate:
					{
						BuildExpression(GetPrecedence((SqlPredicate.IsNull)predicate), ((SqlPredicate.IsNull)predicate).Expr1);
						StringBuilder.Append(((SqlPredicate.IsNull)predicate).IsNot ? " IS NOT NULL" : " IS NULL");
					}

					break;

				case QueryElementType.IsDistinctPredicate:
					BuildIsDistinctPredicate((SqlPredicate.IsDistinct)predicate);
					break;

				case QueryElementType.InSubQueryPredicate:
					BuildInSubQueryPredicate((SqlPredicate.InSubQuery)predicate);
					break;

				case QueryElementType.InListPredicate:
					BuildInListPredicate((SqlPredicate.InList)predicate);
					break;

				case QueryElementType.ExistsPredicate:
					StringBuilder.Append(((SqlPredicate.Exists)predicate).IsNot ? "NOT EXISTS" : "EXISTS");
					BuildExpression(GetPrecedence((SqlPredicate.Exists)predicate), ((SqlPredicate.Exists)predicate).SubQuery);
					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(predicate.Precedence, (SqlSearchCondition)predicate, wrapCondition : false);
					break;

				case QueryElementType.NotPredicate:
					{
						var p = (SqlPredicate.Not)predicate;

						StringBuilder.Append("NOT ");

						BuildPredicate(p.Precedence, GetPrecedence(p.Predicate), p.Predicate);

						break;
					}

					case QueryElementType.ExprPredicate:
					{
						var p = (SqlPredicate.Expr)predicate;

						if (p.Expr1 is SqlValue sqlValue)
						{
							var value = sqlValue.Value;

							if (value is bool b)
							{
								StringBuilder.Append(b ? "1 = 1" : "1 = 0");
								return;
							}
						}

						BuildExpression(GetPrecedence(p), p.Expr1);

						break;
					}

					case QueryElementType.TruePredicate:
					{
						StringBuilder.Append("1 = 1");
						break;
					}

					case QueryElementType.FalsePredicate:
					{
						StringBuilder.Append("1 = 0");
						break;
					}

				default:
					throw new InvalidOperationException($"Unexpected predicate type {predicate.ElementType}");
			}
		}

		protected virtual void BuildExprExprPredicateOperator(SqlPredicate.ExprExpr expr)
		{
			switch (expr.Operator)
			{
				case SqlPredicate.Operator.Equal          : StringBuilder.Append(" = ");  break;
				case SqlPredicate.Operator.NotEqual       : StringBuilder.Append(" <> "); break;
				case SqlPredicate.Operator.Greater        : StringBuilder.Append(" > ");  break;
				case SqlPredicate.Operator.GreaterOrEqual : StringBuilder.Append(" >= "); break;
				case SqlPredicate.Operator.NotGreater     : StringBuilder.Append(" !> "); break;
				case SqlPredicate.Operator.Less           : StringBuilder.Append(" < ");  break;
				case SqlPredicate.Operator.LessOrEqual    : StringBuilder.Append(" <= "); break;
				case SqlPredicate.Operator.NotLess        : StringBuilder.Append(" !< "); break;
				case SqlPredicate.Operator.Overlaps       : StringBuilder.Append(" OVERLAPS "); break;
			}
		}

		protected virtual void BuildExprExprPredicate(SqlPredicate.ExprExpr expr)
		{
			BuildExpression(GetPrecedence(expr), expr.Expr1);

			BuildExprExprPredicateOperator(expr);

			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		protected virtual void BuildIsDistinctPredicate(SqlPredicate.IsDistinct expr)
		{
			BuildExpression(GetPrecedence(expr), expr.Expr1);
			StringBuilder.Append(expr.IsNot ? " IS NOT DISTINCT FROM " : " IS DISTINCT FROM ");
			BuildExpression(GetPrecedence(expr), expr.Expr2);
		}

		protected void BuildIsDistinctPredicateFallback(SqlPredicate.IsDistinct expr)
		{
			// This is the fallback implementation of IS DISTINCT FROM
			// for all providers that don't support the standard syntax
			// nor have a proprietary alternative
			expr.Expr1.ShouldCheckForNull(NullabilityContext);
			StringBuilder.Append("CASE WHEN ");
			BuildExpression(Precedence.Comparison, expr.Expr1);
			StringBuilder.Append(" = ");
			BuildExpression(Precedence.Comparison, expr.Expr2);
			StringBuilder.Append(" OR ");
			BuildExpression(Precedence.Comparison, expr.Expr1);
			StringBuilder.Append(" IS NULL AND ");
			BuildExpression(Precedence.Comparison, expr.Expr2);
			StringBuilder
				.Append(" IS NULL THEN 0 ELSE 1 END = ")
				.Append(expr.IsNot ? '0' : '1');
		}

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlField => (SqlField)expr,
				QueryElementType.Column	  => GetUnderlayingField(((SqlColumn)expr).Expression),
				_                         => throw new InvalidOperationException(),
			};
		}

		protected virtual void BuildInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			BuildExpression(GetPrecedence(predicate), predicate.Expr1);
			StringBuilder.Append((predicate).IsNot ? " NOT IN " : " IN ");
			BuildExpression(GetPrecedence(predicate), predicate.SubQuery);
		}

		protected virtual void BuildInListPredicate(SqlPredicate.InList predicate)
		{
			var values = predicate.Values;

			// Handle x.In(IEnumerable variable)
			if (values.Count == 1 && values[0] is SqlParameter pr)
			{
				var prValue = pr.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues).ProviderValue;
				switch (prValue)
				{
					case null:
						BuildPredicate(SqlPredicate.False);
						return;
					// Be careful that string is IEnumerable, we don't want to handle x.In(string) here
					case string:
						break;
					case IEnumerable items:
						if (predicate.Expr1 is ISqlTableSource table)
							TableSourceIn(table, items);
						else
							InValues(items);
						return;
				}
			}

			// Handle x.In(val1, val2, val3)
			InValues(values);
			return;

			void TableSourceIn(ISqlTableSource table, IEnumerable items)
			{
				var keys = table.GetKeys(true);
				if (keys is null or { Count: 0 })
					throw new LinqToDBException("Cannot create IN expression.");

				var firstValue = true;

				if (keys.Count == 1)
				{
					foreach (var item in items)
					{
						if (firstValue)
						{
							firstValue = false;
							BuildExpression(GetPrecedence(predicate), keys[0]);
							StringBuilder.Append(predicate.IsNot ? " NOT IN (" : " IN (");
						}

						var field = GetUnderlayingField(keys[0]);
						var value = field.ColumnDescriptor.MemberAccessor.GetValue(item!);

						if (value is ISqlExpression expression)
							BuildExpression(expression);
						else
							BuildValue(field.Type, value);

						StringBuilder.Append(InlineComma);
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
							var value = field.ColumnDescriptor.MemberAccessor.GetValue(item!);

							BuildExpression(GetPrecedence(predicate), key);

							if (value == null)
							{
								StringBuilder.Append(" IS NULL");
							}
							else
							{
								StringBuilder.Append(" = ");
								BuildValue(field.Type, value);
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
					BuildPredicate(SqlPredicate.MakeBool(predicate.IsNot));
				else
					StringBuilder.Remove(StringBuilder.Length - 2, 2).Append(')');
			}

			void InValues(IEnumerable values)
			{
				var firstValue    = true;
				var len           = StringBuilder.Length;
				var checkNull     = predicate.WithNull != null;
				var hasNull       = false;
				var count         = 0;
				var multipleParts = false;

				var dbDataType = QueryHelper.GetDbDataType(predicate.Expr1, MappingSchema);

				foreach (object? value in values)
				{
					if (++count > SqlProviderFlags.MaxInListValuesCount)
					{
						count       =  1;
						multipleParts = true;

						// start building next bucket
						firstValue = true;
						RemoveInlineComma()
							.Append(')')
							.Append(predicate.IsNot ? " AND " : " OR ");
					}

					object? val = value;

					if (checkNull)
					{
						if (val is ISqlExpression sqlExpr && sqlExpr.TryEvaluateExpression(OptimizationContext.EvaluationContext, out var evaluated))
							val = evaluated;

						if (val == null)
						{
							hasNull = true;
							continue;
						}
					}

					if (firstValue)
					{
						firstValue = false;
						BuildExpression(GetPrecedence(predicate), predicate.Expr1);
						StringBuilder.Append(predicate.IsNot ? " NOT IN (" : " IN (");
					}

					if (value is ISqlExpression expression)
						BuildExpression(expression);
					else
						BuildValue(dbDataType, value);

					StringBuilder.Append(InlineComma);
				}

				if (firstValue)
				{
					// Nothing was built, because the values contained only null values, or nothing at all.
					BuildPredicate(hasNull ?
						new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot) :
						SqlPredicate.MakeBool(predicate.IsNot));
				}
				else
				{
					RemoveInlineComma().Append(')');

					if (hasNull)
					{
						StringBuilder.Append(predicate.IsNot ? " AND " : " OR ");
						BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, predicate.IsNot));
						multipleParts = true;
					}
					else if (predicate.WithNull == true && predicate.Expr1.ShouldCheckForNull(NullabilityContext))
					{
						StringBuilder.Append(" OR ");
						BuildPredicate(new SqlPredicate.IsNull(predicate.Expr1, false));
						multipleParts = true;
					}
				}

				if (multipleParts)
					StringBuilder.Insert(len, '(').Append(')');
			}
		}

		protected SqlSearchCondition PrepareSearchCondition(SqlSearchCondition searchCondition)
		{
			if (_binaryOptimized > 0)
				return searchCondition;

			var condition = ConvertElement(searchCondition);
			var optimized = Optimize(condition, reducePredicates: true);

			if (optimized is SqlSearchCondition optimizedCondition)
				condition = optimizedCondition;
			else
				condition = new SqlSearchCondition().Add((ISqlPredicate)optimized);

			return condition;
		}

		protected void BuildPredicate(int parentPrecedence, int precedence, ISqlPredicate predicate)
		{
			if (_binaryOptimized == 0)
			{
				var optimized = Optimize(predicate, reducePredicates: true);
				if (!ReferenceEquals(optimized, predicate))
				{
					predicate  = (ISqlPredicate)optimized;
					precedence = GetPrecedence(predicate);
				}
			}

			++_binaryOptimized;

			var wrap = Wrap(precedence, parentPrecedence);

			if (wrap) StringBuilder.Append('(');
			BuildPredicate(predicate);
			if (wrap) StringBuilder.Append(')');

			--_binaryOptimized;
		}

		protected virtual void BuildLikePredicate(SqlPredicate.Like predicate)
		{
			var precedence = GetPrecedence(predicate);

			BuildExpression(precedence, predicate.Expr1);
			StringBuilder
				.Append(predicate.IsNot ? " NOT " : " ")
				.Append(predicate.FunctionName ?? "LIKE")
				.Append(' ');
			BuildExpression(precedence, predicate.Expr2);

			if (predicate.Escape != null)
			{
				StringBuilder.Append(" ESCAPE ");
				BuildExpression(predicate.Escape);
			}
		}

		#endregion

		#region BuildExpression

		/// <summary>
		/// Used to disable field table name (alias) generation.
		/// </summary>
		protected virtual bool BuildFieldTableAlias(SqlField field) => true;

		protected virtual StringBuilder BuildExpression(
			ISqlExpression     expr,
			bool               buildTableName,
			bool               checkParentheses,
			string?            alias,
			ref bool           addAlias,
			bool               throwExceptionIfTableNotFound = true)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField:
					{
						var field = (SqlField)expr;

						if (_disableAlias)
						{
							Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);
							break;
						}

						SqlObjectName? suffixName = null;

						if (BuildFieldTableAlias(field) && buildTableName && field.Table != null)
						{
							var ts = field.Table.SqlTableType == SqlTableType.SystemTable
								? field.Table
								: Statement.SelectQuery?.GetTableSource(field.Table);

							var noAlias = false;
							if (ts == null)
							{
								var current = Statement;

								do
								{
									ts = current.GetTableSource(field.Table, out noAlias);
									if (ts != null)
										break;
									current = current.ParentStatement;
								}
								while (current != null);
							}

							if (ts == null)
							{
								if (field != field.Table.All)
								{
#if DEBUG
									//SqlQuery.GetTableSource(field.Table);
#endif
									if (throwExceptionIfTableNotFound)
										throw new LinqToDBException($"Table '{field.Table.ToDebugString()}' not found.");
								}
							}
							else
							{
								var table = noAlias ? null : GetTableAlias(ts);
								var len   = StringBuilder.Length;

								if (table == null)
								{
									if (field.Table is SqlTable tbl)
										suffixName = tbl.TableName;
									StringBuilder.Append(GetPhysicalTableName(field.Table, null, ignoreTableExpression : true, withoutSuffix : suffixName != null));
								}
								else
									Convert(StringBuilder, table, ConvertType.NameToQueryTableAlias);

								if (len == StringBuilder.Length)
									throw new LinqToDBException($"Table {field.Table} should have an alias.");

								addAlias = !string.Equals(alias, field.PhysicalName, StringComparison.Ordinal);

								StringBuilder
									.Append('.');
							}
						}

						if (field == field.Table?.All)
							StringBuilder.Append('*');
						else
							Convert(StringBuilder, field.PhysicalName, ConvertType.NameToQueryField);

						if (suffixName != null)
							BuildObjectNameSuffix(StringBuilder, suffixName.Value, true);
					}

					break;

				case QueryElementType.Column:
				{
					var column = (SqlColumn)expr;

#if DEBUG
					var sql = Statement.SqlText;
#endif
					if (_disableAlias)
					{
						Convert(StringBuilder, column.Alias!, ConvertType.NameToQueryField);
						break;
					}

					ISqlTableSource? table;
					var currentStatement = Statement;
					var noAlias = false;

					do
					{
						table = currentStatement.GetTableSource(column.Parent!, out noAlias);
						if (table != null)
							break;
						currentStatement = currentStatement.ParentStatement;
					}
					while (currentStatement != null);

					if (table == null)
					{
#if DEBUG
						table = Statement.GetTableSource(column.Parent!, out noAlias);
#endif

						throw new LinqToDBException($"Table not found for '{column}'.");
					}

					var tableAlias = (noAlias ? null : GetTableAlias(table)) ?? GetPhysicalTableName(column.Parent!, null, ignoreTableExpression : true);

					if (string.IsNullOrEmpty(tableAlias))
						throw new LinqToDBException($"Table `{column.Parent}` should have an alias.");

					addAlias = !string.Equals(alias, column.Alias, StringComparison.Ordinal);

					Convert(StringBuilder, tableAlias, ConvertType.NameToQueryTableAlias);
					StringBuilder.Append('.');
					Convert(StringBuilder, column.Alias!, ConvertType.NameToQueryField);

					break;
				}

				case QueryElementType.SqlQuery:
				{
					var hasParentheses = checkParentheses && StringBuilder.Length > 0 && StringBuilder[^1] == '(';

					if (!hasParentheses)
						StringBuilder.AppendLine(OpenParens);
					else
						StringBuilder.AppendLine();

					BuildSqlBuilder((SelectQuery)expr, Indent + 1, BuildStep != Step.FromClause);

					AppendIndent();

					if (!hasParentheses)
						StringBuilder.Append(')');

					break;
				}

				case QueryElementType.SqlValue:
					var sqlval = (SqlValue)expr;

					BuildSqlValue(sqlval);
					break;

				case QueryElementType.SqlExpression:
					{
						var e = (SqlExpression)expr;

						if (string.Equals(e.Expr, "{0}", StringComparison.Ordinal))
							BuildExpression(e.Parameters[0], buildTableName, checkParentheses, alias, ref addAlias, throwExceptionIfTableNotFound);
						else
							BuildFormatValues(e.Expr, e.Parameters, GetPrecedence(e));
					}

					break;

				case QueryElementType.SqlFragment:
				{
					var e = (SqlFragment)expr;

					if (string.Equals(e.Expr, "{0}", StringComparison.Ordinal))
						BuildExpression(e.Parameters[0], buildTableName, checkParentheses, alias, ref addAlias, throwExceptionIfTableNotFound);
					else
						BuildFormatValues(e.Expr, e.Parameters, GetPrecedence(e));
				}

				break;

				case QueryElementType.SqlNullabilityExpression:
					BuildExpression(((SqlNullabilityExpression)expr).SqlExpression, buildTableName, checkParentheses, alias, ref addAlias, throwExceptionIfTableNotFound);
					break;

				case QueryElementType.SqlAnchor:
					BuildAnchor((SqlAnchor)expr);
					break;

				case QueryElementType.SqlBinaryExpression:
					BuildBinaryExpression((SqlBinaryExpression)expr);
					break;

				case QueryElementType.SqlFunction:
					BuildFunction((SqlFunction)expr);
					break;

				case QueryElementType.SqlParameter:
					{
						var parm     = (SqlParameter)expr;
						var inlining = !parm.IsQueryParameter;

						if (inlining)
						{
							var paramValue = parm.GetParameterValue(OptimizationContext.EvaluationContext.ParameterValues);
							if (!TryConvertParameterToSql(paramValue))
								inlining = false;
						}

						if (!inlining)
						{
							BuildParameter(parm);
						}

						break;
					}

				case QueryElementType.SqlDataType:
					BuildDataType(((SqlDataType)expr).Type, forCreateTable: false, canBeNull: true);
					break;

				case QueryElementType.SearchCondition:
					BuildSearchCondition(expr.Precedence, (SqlSearchCondition)expr, wrapCondition : false);
					break;

				case QueryElementType.SqlTable:
				case QueryElementType.SqlRawSqlTable:
				case QueryElementType.TableSource:
				case QueryElementType.SqlCteTable:
					{
						var table = (ISqlTableSource) expr;
						var tableAlias = GetTableAlias(table) ?? GetPhysicalTableName(table, null, ignoreTableExpression : true);
						StringBuilder.Append(tableAlias);
					}

					break;

				case QueryElementType.GroupingSet:
					{
						var groupingSet = (SqlGroupingSet) expr;
						StringBuilder.Append('(');
						for (var index = 0; index < groupingSet.Items.Count; index++)
						{
							var setItem = groupingSet.Items[index];
							BuildExpression(setItem, buildTableName, checkParentheses, throwExceptionIfTableNotFound);
							if (index < groupingSet.Items.Count - 1)
								StringBuilder.Append(InlineComma);
						}

						StringBuilder.Append(')');
					}

					break;

				case QueryElementType.SqlRow:
					BuildSqlRow((SqlRowExpression) expr, buildTableName, checkParentheses, throwExceptionIfTableNotFound);
					break;

				case QueryElementType.SqlCast:
					BuildSqlCastExpression((SqlCastExpression)expr);
					break;

				case QueryElementType.SqlCase:
					BuildSqlCaseExpression((SqlCaseExpression)expr);
					break;

				case QueryElementType.SqlCondition:
					BuildSqlConditionExpression((SqlConditionExpression)expr);
					break;

				case QueryElementType.SqlExtendedFunction:
					BuildSqlExtendedFunction((SqlExtendedFunction)expr);
					break;

				default:
					throw new InvalidOperationException($"Unexpected expression type {expr.ElementType}");
			}

			return StringBuilder;
		}

		protected virtual void BuildSqlCastExpression(SqlCastExpression castExpression)
		{
			BuildTypedExpression(castExpression.ToType, castExpression.Expression);
		}

		protected virtual void BuildSqlExtendedFunction(SqlExtendedFunction extendedFunction)
		{
			StringBuilder.Append(extendedFunction.FunctionName);
			StringBuilder.Append('(');

			if (extendedFunction.Arguments.Count > 0)
			{
				for (var i = 0; i < extendedFunction.Arguments.Count; i++)
				{
					if (i > 0)
						StringBuilder.Append(", ");
					var argument = extendedFunction.Arguments[i];
					if (argument.Modifier != Sql.AggregateModifier.None)
					{
						switch (argument.Modifier)
						{
							case Sql.AggregateModifier.All:
								StringBuilder.Append("ALL");
								break;
							case Sql.AggregateModifier.Distinct:
								StringBuilder.Append("DISTINCT");
								break;
							default:
								throw new InvalidOperationException($"Unexpected aggregate modifier: {argument.Modifier}");
						}

						StringBuilder.Append(' ');
					}

					BuildExpression(argument.Expression, true, i > 0);

					if (argument.Suffix != null)
					{
						StringBuilder.Append(' ');
						BuildExpression(argument.Suffix);
					}
				}
			}

			StringBuilder.Append(')');

			if (extendedFunction.WithinGroup?.Count > 0)
			{
				StringBuilder.Append(" WITHIN GROUP (");
				BuildOrderBy(extendedFunction.WithinGroup!);
				StringBuilder.Append(')');
			}

			if (extendedFunction.Filter != null)
			{
				StringBuilder.Append(" FILTER (WHERE ");
				BuildSearchCondition(extendedFunction.Filter, false);
				StringBuilder.Append(')');
			}

			if (extendedFunction.PartitionBy?.Count > 0     || 
			    extendedFunction.OrderBy?.Count     > 0     ||
			    extendedFunction.FrameClause        != null || 
			    (extendedFunction.WithinGroup?.Count > 0 && IsOverRequiredWithinGroup && extendedFunction.IsWindowFunction))
			{
				StringBuilder.Append(" OVER (");
				if (extendedFunction.PartitionBy?.Count > 0)
				{
					StringBuilder.Append("PARTITION BY ");
					for (var i = 0; i < extendedFunction.PartitionBy.Count; i++)
					{
						if (i > 0)
							StringBuilder.Append(", ");
						BuildExpression(extendedFunction.PartitionBy[i]);
					}
				}

				if (extendedFunction.OrderBy?.Count > 0)
				{
					if (extendedFunction.PartitionBy?.Count > 0)
						StringBuilder.Append(' ');
					BuildOrderBy(extendedFunction.OrderBy!);
				}

				if (extendedFunction.FrameClause != null)
				{
					var frame = extendedFunction.FrameClause;

					StringBuilder.Append(' ');

					switch (frame.FrameType)
					{
						case SqlFrameClause.FrameTypeKind.Rows:
							StringBuilder.Append("ROWS");
							break;
						case SqlFrameClause.FrameTypeKind.Groups:
							StringBuilder.Append("GROUPS");
							break;
						case SqlFrameClause.FrameTypeKind.Range:
							StringBuilder.Append("RANGE");
							break;
						default:
							throw new InvalidOperationException($"Unexpected window frame type: {frame.FrameType}");
					}

					StringBuilder.Append(" BETWEEN ");

					switch (frame.Start.BoundaryType)
					{
						case SqlFrameBoundary.FrameBoundaryType.Unbounded:
							StringBuilder.Append("UNBOUNDED PRECEDING");
							break;
						case SqlFrameBoundary.FrameBoundaryType.CurrentRow:
							StringBuilder.Append("CURRENT ROW");
							break;
						case SqlFrameBoundary.FrameBoundaryType.Offset:
							BuildExpression(frame.Start.Offset!);
							StringBuilder.Append(" PRECEDING");
							break;
						default:
							throw new InvalidOperationException($"Unexpected window frame boundary type: {frame.Start.BoundaryType}");
					}

					StringBuilder.Append(" AND ");

					switch (frame.End.BoundaryType)
					{
						case SqlFrameBoundary.FrameBoundaryType.Unbounded:
							StringBuilder.Append("UNBOUNDED FOLLOWING");
							break;
						case SqlFrameBoundary.FrameBoundaryType.CurrentRow:
							StringBuilder.Append("CURRENT ROW");
							break;
						case SqlFrameBoundary.FrameBoundaryType.Offset:
							BuildExpression(frame.End.Offset!);
							StringBuilder.Append(" FOLLOWING");
							break;
						default:
							throw new InvalidOperationException($"Unexpected window frame boundary type: {frame.End.BoundaryType}");
					}
				}

				StringBuilder.Append(')');
			}

			void BuildOrderBy(List<SqlWindowOrderItem> orderBy)
			{
				StringBuilder.Append("ORDER BY ");
				for (var i = 0; i < orderBy.Count; i++)
				{
					if (i > 0)
						StringBuilder.Append(", ");

					var orderItem = orderBy[i];
					BuildExpression(orderItem.Expression);
					if (orderItem.IsDescending)
						StringBuilder.Append(" DESC");

					if (orderItem.NullsPosition != Sql.NullsPosition.None)
					{
						StringBuilder.Append(" NULLS ");
						StringBuilder.Append(orderItem.NullsPosition == Sql.NullsPosition.First ? "FIRST" : "LAST");
					}
				}
			}
		}

		protected virtual void BuildSqlConditionExpression(SqlConditionExpression conditionExpression)
		{
			StringBuilder.Append("CASE").AppendLine();

			Indent++;

			AppendIndent().Append("WHEN ");

			var len = StringBuilder.Length;

			BuildPredicate(0, GetPrecedence(conditionExpression.Condition), conditionExpression.Condition);

			if (StringBuilder.Length - len > 50)
			{
				StringBuilder.AppendLine();
				AppendIndent().Append("\tTHEN ");
			}
			else
				StringBuilder.Append(" THEN ");

			BuildExpression(conditionExpression.TrueValue);
			StringBuilder.AppendLine();

			AppendIndent().Append("ELSE ");
			BuildExpression(conditionExpression.FalseValue);
			StringBuilder.AppendLine();

			Indent--;

			AppendIndent().Append("END");
		}

		protected void BuildSqlConditionExpressionAsFunction(string funcName, SqlConditionExpression conditionExpression)
		{
			StringBuilder
				.Append(funcName)
				.Append('(');

			BuildPredicate(0, GetPrecedence(conditionExpression.Condition), conditionExpression.Condition);

			StringBuilder.Append(", ");
			BuildExpression(conditionExpression.TrueValue);

			StringBuilder.Append(", ");
			BuildExpression(conditionExpression.FalseValue);

			StringBuilder.Append(')');
		}

		protected virtual void BuildSqlCaseExpression(SqlCaseExpression caseExpression)
		{
			StringBuilder.Append("CASE").AppendLine();

			Indent++;

			foreach (var caseItem in caseExpression.Cases)
			{
				AppendIndent().Append("WHEN ");

				var len = StringBuilder.Length;

				BuildPredicate(0, GetPrecedence(caseItem.Condition), caseItem.Condition);

				if (StringBuilder.Length - len > 50)
				{
					StringBuilder.AppendLine();
					AppendIndent().Append("\tTHEN ");
				}
				else
					StringBuilder.Append(" THEN ");

				BuildExpression(caseItem.ResultExpression);
				StringBuilder.AppendLine();
			}

			if (caseExpression.ElseExpression != null)
			{
				AppendIndent().Append("ELSE ");
				BuildExpression(caseExpression.ElseExpression);
				StringBuilder.AppendLine();
			}

			Indent--;

			AppendIndent().Append("END");
		}

		protected virtual void BuildAnchor(SqlAnchor anchor)
		{
			var addAlias = false;
			switch (anchor.AnchorKind)
			{
				case SqlAnchor.AnchorKindEnum.Deleted:
				{
					StringBuilder.Append(DeletedOutputTable)
						.Append('.');
					break;
				}
				case SqlAnchor.AnchorKindEnum.Inserted:
				{
					StringBuilder.Append(InsertedOutputTable)
						.Append('.');
					break;
				}
				case SqlAnchor.AnchorKindEnum.TableSource:
				{
					if (anchor.SqlExpression is not SqlField { Table: { } fieldTable })
						throw new LinqToDBException("Cannot find Table or Column associated with expression");

					var ts = Statement.GetTableSource(fieldTable, out var noAlias);
					if (ts == null)
						throw new LinqToDBException("Cannot find Table Source for table.");

					var table = noAlias ? null : GetTableAlias(ts);

					if (table == null)
						StringBuilder.Append(GetPhysicalTableName(fieldTable, null, ignoreTableExpression: true));
					else
						Convert(StringBuilder, table, ConvertType.NameToQueryTableAlias);

					return;
				}
				case SqlAnchor.AnchorKindEnum.TableName:
				{
					if (anchor.SqlExpression is not SqlField { Table: { } fieldTable })
						throw new LinqToDBException("Cannot find Table or Column associated with expression");

					BuildPhysicalTable(fieldTable, null);
					return;
				}
				case SqlAnchor.AnchorKindEnum.TableAsSelfColumn:
				{
					if (anchor.SqlExpression is not SqlField { Table: { } fieldTable })
						throw new LinqToDBException("Cannot find Table or Column associated with expression");

					var table = FindTable(fieldTable);
					if (table == null)
						throw new LinqToDBException("Cannot find table.");

					BuildExpression(new SqlField(table, table.TableName.Name));

					return;
				}
				case SqlAnchor.AnchorKindEnum.TableAsSelfColumnOrField:
				{
					if (anchor.SqlExpression is not SqlField { Table: { } fieldTable } sqlField)
						throw new LinqToDBException("Cannot find Table or Column associated with expression");

					if (FindTable(fieldTable) is not { } table)
						throw new LinqToDBException("Cannot find table.");

					if (sqlField == fieldTable.All)
					{
						BuildExpression(new SqlField(table, table.TableName.Name));
					}
					else
					{
						BuildExpression(anchor.SqlExpression);
					}

					return;
				}
				default:
					throw new ArgumentOutOfRangeException(anchor.AnchorKind.ToString());
			}

			var saveDisableAlias = _disableAlias;
			_disableAlias = true;
			BuildExpression(anchor.SqlExpression, false, false, null, ref addAlias, false);

			_disableAlias = saveDisableAlias;

			SqlTable? FindTable(ISqlTableSource tableSource)
			{
				if (tableSource is SqlTable table)
					return table;

				var currentTable = tableSource;
				while (currentTable is SelectQuery { From.Tables.Count: 1 } sc)
				{
					currentTable = sc.From.Tables[0].Source;
					if (currentTable is SqlTable st)
					{
						return st;
					}
				}

				return null;
			}
		}

		protected virtual bool TryConvertParameterToSql(SqlParameterValue paramValue)
		{
			return MappingSchema.TryConvertToSql(StringBuilder, paramValue.DbDataType, DataOptions, paramValue.ProviderValue);
		}

		protected virtual void BuildParameter(SqlParameter parameter)
		{
			var newParm = OptimizationContext.AddParameter(parameter);
			Convert(StringBuilder, newParm.Name!, ConvertType.NameToQueryParameter);
		}

		void BuildFormatValues(string format, IReadOnlyList<ISqlExpression>? parameters, int precedence)
		{
			if (parameters == null || parameters.Count == 0)
				StringBuilder.Append(format);
			else
			{
				var values = new object[parameters.Count];

				for (var i = 0; i < values.Length; i++)
				{
					var value = ConvertElement(parameters[i]);

					values[i] = WithStringBuilderBuildExpression(precedence, value);
				}

				StringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, values);
			}
		}

		static string IdentText(string text, int ident)
		{
			if (string.IsNullOrEmpty(text))
				return text;

			text = text.Replace("\r", "", StringComparison.Ordinal);

			var strArray = text.Split('\n');
			using var sb = Pools.StringBuilder.Allocate();

			for (var i = 0; i < strArray.Length; i++)
			{
				var s = strArray[i];
				sb.Value.Append('\t', ident).Append(s);
				if (i < strArray.Length - 1)
					sb.Value.AppendLine();
			}

			return sb.Value.ToString();
		}

		void BuildExpression(int parentPrecedence, ISqlExpression expr, string? alias, ref bool addAlias)
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

		public void BuildExpression(ISqlExpression expr, bool buildTableName, bool checkParentheses, bool throwExceptionIfTableNotFound = true)
		{
			var dummy = false;
			BuildExpression(expr, buildTableName, checkParentheses, null, ref dummy, throwExceptionIfTableNotFound);
		}

		protected void BuildExpression(int precedence, ISqlExpression expr)
		{
			var dummy = false;
			BuildExpression(precedence, expr, null, ref dummy);
		}

		protected virtual void BuildTypedExpression(DbDataType dataType, ISqlExpression value)
		{
			var saveStep = BuildStep;
			// TODO: Step.TypedExpression should be removed/reworked as it doesn't work with nested expressions
			// e.g. see Issue4963 test for Firebird
			BuildStep = value is SqlParameter ? Step.TypedExpression : BuildStep;

			StringBuilder.Append("CAST(");
			BuildExpression(value);
			StringBuilder.Append(" AS ");
			BuildDataType(dataType, false, value.CanBeNullable(NullabilityContext));
			StringBuilder.Append(')');

			BuildStep = saveStep;
		}

		protected virtual void BuildSqlRow(SqlRowExpression expr, bool buildTableName, bool checkParentheses, bool throwExceptionIfTableNotFound)
		{
			StringBuilder.Append('(');
			foreach (var value in expr.Values)
			{
				BuildExpression(value, buildTableName, checkParentheses, throwExceptionIfTableNotFound);
				StringBuilder.Append(InlineComma);
			}

			StringBuilder.Length -= InlineComma.Length; // Note that SqlRow are never empty
			StringBuilder.Append(')');
		}

		protected object? BuildExpressionContext;

		void ISqlBuilder.BuildExpression(StringBuilder sb, ISqlExpression expr, bool buildTableName, object? context)
		{
			WithStringBuilder(sb, static ctx => ctx.this_.BuildExpression(ctx.expr, ctx.buildTableName, true), (this_: this, expr, buildTableName));
			BuildExpressionContext = null;
		}

		#endregion

		#region BuildValue

		protected virtual void BuildValue(DbDataType? dataType, object? value)
		{
			if (value is Sql.SqlID id)
				TryBuildSqlID(id);
			else
			{
				if (!MappingSchema.TryConvertToSql(StringBuilder, dataType, DataOptions, value))
				{
					if (dataType == null)
					{
						throw new LinqToDBException($"Cannot convert value of type {value?.GetType()} to SQL");
					}

					BuildParameter(new SqlParameter(dataType.Value, "value", value));
				}
			}
		}

		public virtual void BuildSqlValue(SqlValue value)
		{
			BuildValue(value.ValueType, value.Value);
		}

		#endregion

		#region BuildBinaryExpression

		protected virtual void BuildBinaryExpression(SqlBinaryExpression expr)
		{
			BuildBinaryExpression(expr.Operation, expr);
		}

		void BuildBinaryExpression(string op, SqlBinaryExpression expr)
		{
			if (string.Equals(expr.Operation, "*", StringComparison.Ordinal) && expr.Expr1 is SqlValue value)
			{
				if (value.Value is int i && i == -1)
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
			BuildFunction(func.Name, func.Parameters);
		}

		void BuildFunction(string name, ISqlExpression[] exprs)
		{
			StringBuilder.Append(name).Append('(');

			var first = true;

			foreach (var parameter in exprs)
			{
				if (!first)
					StringBuilder.Append(InlineComma);

				BuildExpression(parameter, true, !first);

				first = false;
			}

			StringBuilder.Append(')');
		}

		#endregion

		#region BuildDataType

		/// <summary>
		/// Appends an <see cref="SqlDataType"/>'s String to a provided <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="dataType"></param>
		/// <returns>The stringbuilder with the type information appended.</returns>
		public StringBuilder BuildDataType(StringBuilder sb, DbDataType dataType)
		{
			WithStringBuilder(sb, static ctx =>
			{
				ctx.this_.BuildDataType(ctx.dataType, forCreateTable: false, canBeNull: false);
			}, (this_: this, dataType));
			return sb;
		}

		/// <param name="canBeNull">Type could store <c>NULL</c> values (could be used for column table type generation or for databases with explicit typee nullability like ClickHouse).</param>
		protected void BuildDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			if (!string.IsNullOrEmpty(type.DbType))
				StringBuilder.Append(type.DbType);
			else
			{
				var systemType = type.SystemType.FullName;
				if (type.DataType == DataType.Undefined)
					type = MappingSchema.GetDbDataType(type.SystemType);

				if (!string.IsNullOrEmpty(type.DbType))
				{
					StringBuilder.Append(type.DbType);
					return;
				}

				if (type.DataType == DataType.Undefined)
					// give some hint to user that it is expected situation and he need to fix something on his side
					throw new LinqToDBException($"Database column type cannot be determined automatically and must be specified explicitly for system type {systemType}");

				BuildDataTypeFromDataType(type, forCreateTable, canBeNull);
			}
		}

		/// <param name="type"></param>
		/// <param name="forCreateTable"></param>
		/// <param name="canBeNull">Type could store <c>NULL</c> values (could be used for column table type generation or for databases with explicit typee nullability like ClickHouse).</param>
		protected virtual void BuildDataTypeFromDataType(DbDataType type, bool forCreateTable, bool canBeNull)
		{
			switch (type.DataType)
			{
				case DataType.Double : StringBuilder.Append("Float");    return;
				case DataType.Single : StringBuilder.Append("Real");     return;
				case DataType.SByte  : StringBuilder.Append("TinyInt");  return;
				case DataType.UInt16 : StringBuilder.Append("Int");      return;
				case DataType.UInt32 : StringBuilder.Append("BigInt");   return;
				case DataType.UInt64 : StringBuilder.Append("Decimal");  return;
				case DataType.Byte   : StringBuilder.Append("TinyInt");  return;
				case DataType.Int16  : StringBuilder.Append("SmallInt"); return;
				case DataType.Int32  : StringBuilder.Append("Int");      return;
				case DataType.Int64  : StringBuilder.Append("BigInt");   return;
				case DataType.Boolean: StringBuilder.Append("Bit");      return;
			}

			StringBuilder.Append(CultureInfo.InvariantCulture, $"{type.DataType}");

			if (type.Length > 0)
				StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Length})");

			if (type.Precision > 0)
				StringBuilder.Append(CultureInfo.InvariantCulture, $"({type.Precision}{InlineComma}{type.Scale})");
		}

		protected static DbDataType CorrectDecimalFacets(DbDataType dataType, decimal decValue, bool updateNullsOnly = false)
		{
			if (updateNullsOnly && dataType.Precision != null && dataType.Scale != null)
				return dataType;

			var (precision, scale) = DecimalHelper.GetFacets(decValue);

			return updateNullsOnly
					? dataType.WithPrecision(dataType.Precision ?? precision).WithScale(dataType.Scale ?? scale)
					: dataType.WithPrecision(precision).WithScale(scale);
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

		#region Comments

		protected virtual void BuildTag(SqlStatement statement)
		{
			if (statement.Tag != null)
				BuildSqlComment(StringBuilder, statement.Tag);
		}

		protected virtual StringBuilder BuildSqlComment(StringBuilder sb, SqlComment comment)
		{
			sb.Append("/* ");

			for (var i = 0; i < comment.Lines.Count; i++)
			{
				sb.Append(comment.Lines[i].Replace("/*", "", StringComparison.Ordinal).Replace("*/", "", StringComparison.Ordinal));
				if (i < comment.Lines.Count - 1)
					sb.AppendLine();
			}

			sb.AppendLine(" */");

			return sb;
		}

		#endregion

		#endregion

		#region Internal Types

		protected enum Step
		{
			WithClause,
			SelectClause,
			DeleteClause,
			AlterDeleteClause,
			UpdateClause,
			MergeUpdateClause,
			InsertClause,
			MergeInsertClause,
			FromClause,
			WhereClause,
			GroupByClause,
			HavingClause,
			OrderByClause,
			OffsetLimit,
			Tag,
			Output,
			QueryExtensions,
			TypedExpression,
		}

		#endregion

		#region Alternative Builders

		protected static bool IsDateDataType(ISqlExpression expr, string dateName)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.Date,
				QueryElementType.SqlExpression => string.Equals(((SqlExpression)expr).Expr, dateName, StringComparison.Ordinal),
				_                              => false,
			};
		}

		protected static bool IsTimeDataType(ISqlExpression expr)
		{
			return expr.ElementType switch
			{
				QueryElementType.SqlDataType   => ((SqlDataType)expr).Type.DataType == DataType.Time,
				QueryElementType.SqlExpression => string.Equals(((SqlExpression)expr).Expr, "Time", StringComparison.Ordinal),
				_                              => false,
			};
		}

		#endregion

		#region Helpers

		protected SequenceNameAttribute? GetSequenceNameAttribute(SqlTable table, bool throwException)
		{
			var identityField = table.GetIdentityField();

			if (identityField == null)
				if (throwException)
					throw new LinqToDBException($"Identity field must be defined for '{table.NameForLogging}'.");
				else
					return null;

			if (table.ObjectType == null)
				if (throwException)
					throw new LinqToDBException($"Sequence name can not be retrieved for the '{table.NameForLogging}' table.");
				else
					return null;

			var attrs = table.SequenceAttributes;

			if (attrs.IsNullOrEmpty())
				if (throwException)
					throw new LinqToDBException($"Sequence name can not be retrieved for the '{table.NameForLogging}' table.");
				else
					return null;

			return attrs[0];
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

		protected string? GetTableAlias(ISqlTableSource table)
		{
			switch (table.ElementType)
			{
				case QueryElementType.TableSource:
					{
						var ts    = (SqlTableSource)table;
						var alias = string.IsNullOrEmpty(ts.Alias) ? GetTableAlias(ts.Source) : ts.Alias;
						return alias is not ("$" or "$F") ? alias : null;
					}
				case QueryElementType.SqlTable        :
				case QueryElementType.SqlCteTable     :
					{
						var alias = ((SqlTable)table).Alias;
						return alias is not ("$" or "$F") ? alias : null;
					}
				case QueryElementType.SqlRawSqlTable  :
				{
						var noAlias = false;
						var ts = Statement.SelectQuery?.GetTableSource(table) ?? Statement.GetTableSource(table, out noAlias);

						if (ts != null)
						{
							if (noAlias)
								return null;
							return GetTableAlias(ts);
						}

						var alias = ((SqlTable)table).Alias;
						return alias is not ("$" or "$F") ? alias : null;
					}
				case QueryElementType.SqlTableLikeSource:
					return null;

				default:
					throw new InvalidOperationException($"Unexpected table type {table.ElementType}");
			}
		}

		protected virtual string GetPhysicalTableName(ISqlTableSource table, string? alias, bool ignoreTableExpression = false, string? defaultDatabaseName = null, bool withoutSuffix = false)
		{
			switch (table.ElementType)
			{
				case QueryElementType.SqlTable:
					{
						var tbl = (SqlTable)table;

						var tableName = tbl.TableName;
						if (tableName.Database == null && defaultDatabaseName != null)
							tableName = tableName with { Database = defaultDatabaseName };

						using var sb = Pools.StringBuilder.Allocate();

						BuildObjectName(sb.Value, tableName, tbl.SqlTableType == SqlTableType.Function ? ConvertType.NameToProcedure : ConvertType.NameToQueryTable, true, tbl.TableOptions, withoutSuffix: withoutSuffix);

						if (!ignoreTableExpression && tbl.SqlTableType == SqlTableType.Expression)
						{
							var values = new object[2 + (tbl.TableArguments?.Length ?? 0)];

							values[0] = sb.Value.ToString();

							if (alias != null)
								values[1] = ConvertInline(alias, ConvertType.NameToQueryTableAlias);
							else
								values[1] = "";

							for (var i = 2; i < values.Length; i++)
							{
								var value = tbl.TableArguments![i - 2];

								values[i] = WithStringBuilderBuildExpression(Precedence.Primary, value);
							}

							sb.Value.Length = 0;
							sb.Value.AppendFormat(CultureInfo.InvariantCulture, tbl.Expression!, values);
						}
						else if (tbl.SqlTableType == SqlTableType.Function)
						{
							sb.Value.Append('(');

							if (tbl.TableArguments != null && tbl.TableArguments.Length > 0)
							{
								var first = true;

								foreach (var arg in tbl.TableArguments)
								{
									if (!first)
										sb.Value.Append(InlineComma);

									WithStringBuilder(sb.Value, static ctx => ctx.this_.BuildExpression(ctx.arg, true, !ctx.first), (this_: this, arg, first));

									first = false;
								}
							}

							sb.Value.Append(')');
						}

						return sb.Value.ToString();
					}

				case QueryElementType.TableSource:
					return GetPhysicalTableName(((SqlTableSource)table).Source, alias, withoutSuffix : withoutSuffix);

				case QueryElementType.SqlCteTable   :
				case QueryElementType.SqlRawSqlTable:
					return BuildObjectName(new (), ((SqlTable)table).TableName, ConvertType.NameToCteName, true, TableOptions.NotSet, withoutSuffix: withoutSuffix).ToString();

				case QueryElementType.SqlTableLikeSource:
					return ConvertInline(((SqlTableLikeSource)table).Name, ConvertType.NameToQueryTable);

				default:
					throw new InvalidOperationException($"Unexpected table type {table.ElementType}");
			}
		}

		protected StringBuilder AppendIndent()
		{
			if (Indent > 0)
				StringBuilder.Append('\t', Indent);

			return StringBuilder;
		}

		protected virtual bool IsReserved(string word)
		{
			return ReservedWords.IsReserved(word);
		}

		#endregion

		#region Common Helper methods

		protected ISqlExpression ConvertCaseToConditions(SqlCaseExpression caseExpression, int start)
		{
			if (start >= caseExpression.Cases.Count)
				return caseExpression.ElseExpression ?? new SqlValue(caseExpression.Type, null);

			return new SqlConditionExpression(caseExpression.Cases[start].Condition, caseExpression.Cases[start].ResultExpression, ConvertCaseToConditions(caseExpression, start + 1));
		}

		#endregion Common Helper methods

		#region ISqlProvider Members

		public virtual ISqlExpression? GetIdentityExpression(SqlTable table)
		{
			return null;
		}

		protected virtual void PrintParameterName(StringBuilder sb, DbParameter parameter)
		{
			if (!parameter.ParameterName.StartsWith('@'))
				sb.Append('@');
			sb.Append(parameter.ParameterName);
		}

		protected virtual string? GetTypeName(IDataContext dataContext, DbParameter parameter)
		{
			return null;
		}

		protected virtual string? GetUdtTypeName(IDataContext dataContext, DbParameter parameter)
		{
			return null;
		}

		protected virtual string? GetProviderTypeName(IDataContext dataContext, DbParameter parameter)
		{
			return parameter.DbType switch
			{
				DbType.AnsiString            => "VarChar",
				DbType.AnsiStringFixedLength => "Char",
				DbType.String                => "NVarChar",
				DbType.StringFixedLength     => "NChar",
				DbType.Decimal               => "Decimal",
				DbType.Binary                => "Binary",
				_                            => null,
			};
		}

		protected virtual void PrintParameterType(IDataContext dataContext, StringBuilder sb, DbParameter parameter)
		{
			var typeName = GetTypeName(dataContext, parameter);
			if (!string.IsNullOrEmpty(typeName))
				sb.Append(typeName).Append(" -- ");

			var udtTypeName = GetUdtTypeName(dataContext, parameter);
			if (!string.IsNullOrEmpty(udtTypeName))
				sb.Append(udtTypeName).Append(" -- ");

			var t1 = GetProviderTypeName(dataContext, parameter);
			var t2 = parameter.DbType.ToString();

			sb.Append(t1);

			if (t1 != null)
			{
				if (parameter.Size > 0)
				{
					if (t1.IndexOf('(', StringComparison.Ordinal) < 0)
						sb.Append(CultureInfo.InvariantCulture, $"({parameter.Size})");
				}
				else if (parameter.Precision > 0)
				{
					if (t1.IndexOf('(', StringComparison.Ordinal) < 0)
						sb.Append(CultureInfo.InvariantCulture, $"({parameter.Precision}{InlineComma}{parameter.Scale})");
				}
				else
				{
					switch (parameter.DbType)
					{
						case DbType.AnsiString           :
						case DbType.AnsiStringFixedLength:
						case DbType.String               :
						case DbType.StringFixedLength    :
							{
								var value = parameter.Value as string;

								if (!string.IsNullOrEmpty(value))
									sb.Append(CultureInfo.InvariantCulture, $"({value!.Length})");

								break;
							}
						case DbType.Decimal:
							{
								var value = parameter.Value;

								if (value is decimal dec)
								{
									var d = new SqlDecimal(dec);
									sb.Append(CultureInfo.InvariantCulture, $"({d.Precision}{InlineComma}{d.Scale})");
								}

								break;
							}
						case DbType.Binary:
							{
								if (parameter.Value is byte[] value)
									sb.Append(CultureInfo.InvariantCulture, $"({value.Length})");

								break;
							}
					}
				}
			}

			if (!string.Equals(t1, t2, StringComparison.Ordinal))
				sb.Append(" -- ").Append(t2);
		}

		public virtual StringBuilder PrintParameters(IDataContext dataContext, StringBuilder sb, IEnumerable<DbParameter>? parameters)
		{
			if (parameters != null)
			{
				foreach (var p in parameters)
				{
					sb.Append("DECLARE ");
					PrintParameterName(sb, p);
					sb.Append(' ');
					PrintParameterType(dataContext, sb, p);
					sb.AppendLine();

					sb.Append("SET     ");
					PrintParameterName(sb, p);
					sb.Append(" = ");
					var trimmed = PrintParameterValue(sb, p.Value);
					sb.AppendLine();
					if (trimmed)
						sb.AppendLine($"-- value above truncated for logging");
				}

				sb.AppendLine();
			}

			return sb;
		}

		protected virtual bool PrintParameterValue(StringBuilder sb, object? value)
		{
			var maxBinaryLogging = LinqToDB.Common.Configuration.MaxBinaryParameterLengthLogging;
			var maxStringLogging = LinqToDB.Common.Configuration.MaxStringParameterLengthLogging;

			if (value is byte[] bytes &&
				maxBinaryLogging >= 0 &&
				bytes.Length > maxBinaryLogging &&
				MappingSchema.ValueToSqlConverter.CanConvert(typeof(byte[])))
			{
				var trimmed = new byte[maxBinaryLogging];
				Array.Copy(bytes, 0, trimmed, 0, maxBinaryLogging);
				MappingSchema.ValueToSqlConverter.TryConvert(sb, MappingSchema, DataOptions, trimmed);
				return true;
			}
			else if (value is Binary binaryData &&
					 maxBinaryLogging >= 0 &&
					 binaryData.Length > maxBinaryLogging &&
					 MappingSchema.ValueToSqlConverter.CanConvert(typeof(Binary)))
			{
				//We aren't going to create a new Binary here,
				//since ValueToSql always just .ToArray() anyway
				var trimmed = new byte[maxBinaryLogging];
				Array.Copy(binaryData.ToArray(), 0, trimmed, 0, maxBinaryLogging);
				MappingSchema.TryConvertToSql(sb, null, DataOptions, trimmed);
				MappingSchema.ValueToSqlConverter.TryConvert(sb, MappingSchema, DataOptions, trimmed);
				return true;
			}
			else if (value is string s &&
					 maxStringLogging >= 0 &&
					 s.Length > maxStringLogging &&
					 MappingSchema.ValueToSqlConverter.CanConvert(typeof(string)))
			{
				var trimmed = s.Substring(0, maxStringLogging);
				MappingSchema.TryConvertToSql(sb, null, DataOptions, trimmed);
				return true;
			}
			else if (!MappingSchema.TryConvertToSql(sb, null, DataOptions, value))
				return FormatParameterValue(sb, value);

			return false;
		}

		// for values without literal support from provider we should generate debug string using fixed format
		// to avoid deviations on different locales or locale settings
		private bool FormatParameterValue(StringBuilder sb, object? value)
		{
			if (value is DateTime dt)
			{
				// ISO8601 format (with Kind-specific offset part)
				sb
					.Append('\'')
					.Append(dt.ToString("o", DateTimeFormatInfo.InvariantInfo))
					.Append('\'');
			}
			else if (value is DateTimeOffset dto)
			{
				// ISO8601 format with offset
				sb
					.Append('\'')
					.Append(dto.ToString("o", DateTimeFormatInfo.InvariantInfo))
					.Append('\'');
			}
			else if (value is IEnumerable collection)
			{
				var limit   = LinqToDB.Common.Configuration.MaxArrayParameterLengthLogging >= 0 ? LinqToDB.Common.Configuration.MaxArrayParameterLengthLogging : int.MaxValue;
				var trimmed = false;
				var pos     = 0;

				sb.Append('{');
				foreach (var item in collection)
				{
					pos++;
					if (pos > limit)
					{
						trimmed = true;
						break;
					}

					if (pos > 1)
						sb.Append(',');

					trimmed = PrintParameterValue(sb, item) || trimmed;
				}

				sb.Append('}');

				return trimmed;
			}
			else
				sb.Append(CultureInfo.InvariantCulture, $"{value}");

			return false;
		}

		public string ApplyQueryHints(string sqlText, IReadOnlyCollection<string> queryHints)
		{
			using var sb = Pools.StringBuilder.Allocate();

			foreach (var hint in queryHints)
				if (hint.StartsWith("**", StringComparison.Ordinal))
					sb.Value.Append(hint, 2, hint.Length - 2).AppendLine();

			sb.Value.Append(sqlText);

			foreach (var hint in queryHints)
				if (!hint.StartsWith("**", StringComparison.Ordinal))
					sb.Value.AppendLine(hint);

			return sb.Value.ToString();
		}

		public virtual string GetReserveSequenceValuesSql(int count, string sequenceName)
		{
			throw new NotSupportedException();
		}

		public virtual string GetMaxValueSql(EntityDescriptor entity, ColumnDescriptor column)
		{
			using var sb = Pools.StringBuilder.Allocate();
			sb.Value.Append("SELECT Max(");

			Convert(sb.Value, column.ColumnName, ConvertType.NameToQueryField)
				.Append(") FROM ");

			return BuildObjectName(sb.Value, entity.Name, ConvertType.NameToQueryTable, true, entity.TableOptions).ToString();
		}

		public virtual string Name => field ??= GetType().Name.Replace("SqlBuilder", "", StringComparison.Ordinal);

		#endregion

		#region Aliases

		HashSet<string>? _aliases;

		public void RemoveAlias(string alias)
		{
			_aliases?.Remove(alias);
		}

		string GetAlias(string desiredAlias, string defaultAlias)
		{
			_aliases ??= AliasesContext.GetUsedTableAliases();

			var alias = desiredAlias;

			if (string.IsNullOrEmpty(desiredAlias) || desiredAlias.Length > 25)
			{
				desiredAlias = defaultAlias;
				alias        = defaultAlias + "1";
			}

			for (var i = 1; ; i++)
			{
				if (!_aliases.Contains(alias) && !IsReserved(alias))
				{
					_aliases.Add(alias);
					break;
				}

				alias = string.Create(CultureInfo.InvariantCulture, $"{desiredAlias}{i}");
			}

			return alias;
		}

		public string[] GetTempAliases(int n, string defaultAlias)
		{
			var aliases = new string[n];

			for (var i = 0; i < aliases.Length; i++)
				aliases[i] = GetAlias(defaultAlias, defaultAlias);

			foreach (var t in aliases)
				RemoveAlias(t);

			return aliases;
		}

		#endregion

		#region BuildSub/QueryExtensions

		protected virtual void BuildSubQueryExtensions(SqlStatement statement)
		{
		}

		protected virtual void BuildQueryExtensions(SqlStatement statement)
		{
		}

		#endregion

		#region TableID

		public Dictionary<string,TableIDInfo>? TableIDs  { get; set; }
		public string?                         TablePath { get; set; }
		public string?                         QueryName { get; set; }

		public string BuildSqlID(Sql.SqlID id)
		{
			if (TableIDs?.TryGetValue(id.ID, out var path) == true)
				return id.Type switch
				{
					Sql.SqlIDType.TableAlias => path!.TableAlias,
					Sql.SqlIDType.TableName  => path!.TableName,
					Sql.SqlIDType.TableSpec  => path!.TableSpec,
					_ => throw new InvalidOperationException($"Unknown SqlID Type '{id.Type}'."),
				};

			throw new InvalidOperationException($"Table ID '{id.ID}' is not defined.");
		}

		int  _testReplaceNumber;

		void TryBuildSqlID(Sql.SqlID id)
		{
			if (TableIDs?.ContainsKey(id.ID) == true)
			{
				StringBuilder.Append(BuildSqlID(id));
			}
			else
			{
				var testToReplace = string.Create(CultureInfo.InvariantCulture, $"$$${++_testReplaceNumber}$$$");

				StringBuilder.Append(testToReplace);

				(_finalBuilders ??= new(1)).Add(() => StringBuilder.Replace(testToReplace, BuildSqlID(id)));
			}
		}

		#endregion
	}
}
