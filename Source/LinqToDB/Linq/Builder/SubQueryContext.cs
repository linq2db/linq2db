using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SubQueryContext : BuildContextBase
	{
		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery.Builder, subQuery.ElementType, selectQuery)
		{
			if (selectQuery == subQuery.SelectQuery)
				throw new ArgumentException("Wrong subQuery argument.", nameof(subQuery));

			SubQuery        = subQuery;
			SubQuery.Parent = this;

			if (addToSql)
				selectQuery.From.Table(SubQuery.SelectQuery);
		}

		public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
			: this(subQuery, new SelectQuery { ParentSelect = subQuery.SelectQuery.ParentSelect }, addToSql)
		{
		}

		public IBuildContext SubQuery { get; }

		protected virtual bool OptimizeColumns => true;

		protected virtual int GetIndex(int index, ISqlExpression column)
		{
			throw new NotImplementedException();
		}

		public override void SetAlias(string? alias)
		{
			if (alias == null)
				return;

			if (alias.Contains('<'))
				return;

			var table = SelectQuery.From.Tables.FirstOrDefault();

			if (table is { Alias: null })  
				table.Alias = alias;
		}

		public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			expression = SequenceHelper.CorrectExpression(expression, this, SubQuery);
			return SubQuery.GetContext(expression, buildInfo);
		}

		public override SqlStatement GetResultStatement()
		{
			return Statement ??= new SqlSelectStatement(SelectQuery);
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			SubQuery.SetRunQuery(query, expr);
		}

		public override IBuildContext Clone(CloningContext context)
		{
			var selectQuery = context.CloneElement(SelectQuery);
			return new SubQueryContext(context.CloneContext(SubQuery), selectQuery, false);
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() && SequenceHelper.IsSameContext(path, this))
				return path;

			var corrected = SequenceHelper.CorrectExpression(path, this, SubQuery);

			var result = Builder.ConvertToSqlExpr(SubQuery, corrected, flags);

			if (flags.IsTable() || flags.IsAggregationRoot())
				return result;

			if (flags.IsTraverse())
				return result;

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				result = SequenceHelper.CorrectTrackingPath(result, path);

				// correct all placeholders, they should target to appropriate SubQuery.SelectQuery
				//
				result = Builder.UpdateNesting(this, result);
				result = SequenceHelper.CorrectSelectQuery(result, SelectQuery);

				if (!flags.HasFlag(ProjectFlags.AssociationRoot))
				{
					// remap back, especially for Recursive CTE
					result = SequenceHelper.ReplaceContext(result, SubQuery, this);
				}
			}

			return result;
		}
	}
}
