using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;
	using LinqToDB.Expressions;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SubQueryContext : BuildContextBase
	{
		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery.Builder, selectQuery)
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
		protected internal readonly Dictionary<int,int> ColumnIndexes = new ();

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

			if (SelectQuery.From.Tables[0].Alias == null)
				SelectQuery.From.Tables[0].Alias = alias;
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
			if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
				return path;

			var result = SequenceHelper.CorrectExpression(path, this, SubQuery);
			result = Builder.MakeExpression(SubQuery, result, flags);

			if (flags.HasFlag(ProjectFlags.Table))
				return result;

			result = Builder.ConvertToSqlExpr(SubQuery, result, flags);

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
