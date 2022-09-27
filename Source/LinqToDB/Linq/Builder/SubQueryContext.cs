using System.Diagnostics;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class SubQueryContext : PassThroughContext
	{
#if DEBUG
		public override string? SqlQueryText => SelectQuery.ToString();
#endif

		public SubQueryContext(IBuildContext subQuery, SelectQuery selectQuery, bool addToSql)
			: base(subQuery)
		{
			if (selectQuery == subQuery.SelectQuery)
				throw new ArgumentException("Wrong subQuery argument.", nameof(subQuery));

			SubQuery        = subQuery;
			SubQuery.Parent = this;
			SelectQuery     = selectQuery;
			Statement       = subQuery.Statement;

			if (addToSql)
				selectQuery.From.Table(SubQuery.SelectQuery);
		}

		public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
			: this(subQuery, new SelectQuery { ParentSelect = subQuery.SelectQuery.ParentSelect }, addToSql)
		{
			Statement = subQuery.Statement;
		}

		public          IBuildContext  SubQuery    { get; private set; }
		public override SelectQuery    SelectQuery { get; set; }
		public override IBuildContext? Parent      { get; set; }

		public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		protected virtual bool OptimizeColumns => true;
		protected internal readonly Dictionary<int,int> ColumnIndexes = new ();

		protected virtual int GetIndex(int index, ISqlExpression column)
		{
			throw new NotImplementedException();
			}

		public override int ConvertToParentIndex(int index, IBuildContext context)
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

		public override ISqlExpression? GetSubQuery(IBuildContext context)
		{
			return null;
		}

		public override SqlStatement GetResultStatement()
		{
			return Statement ??= new SqlSelectStatement(SelectQuery);
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

			var result = base.MakeExpression(path, flags);

			if (flags.HasFlag(ProjectFlags.SQL))
			{
				result = Builder.ConvertToSqlExpr(SubQuery, result, flags);
			}

			if (!flags.HasFlag(ProjectFlags.Test))
			{
				result = Builder.UpdateNesting(this, result);

				// remap back, especially for Recursive CTE
				result = SequenceHelper.ReplaceContext(result, SubQuery, this);
			}

			return result;
		}
	}
}
