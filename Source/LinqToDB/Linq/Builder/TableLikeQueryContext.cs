using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class TableLikeQueryContext : IBuildContext
	{
#if DEBUG
		public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif
		public SelectQuery? SelectQuery
		{
			get => InnerQueryContext?.SelectQuery;
			set { }
		}

		public SqlStatement?  Statement { get; set; }
		public IBuildContext? Parent    { get; set; }

		public ExpressionBuilder  Builder           { get; }
		public Expression?        Expression        { get; }

		public IBuildContext      InnerQueryContext { get; }
		public SubQueryContext    SubqueryContext   { get; }
		public SqlTableLikeSource Source            { get; }

		public TableLikeQueryContext(IBuildContext sourceContext)
		{
			Builder           = sourceContext.Builder;
			InnerQueryContext = sourceContext;
			SubqueryContext   = new SubQueryContext(sourceContext);

			Source = sourceContext is EnumerableContext enumerableSource
				? new SqlTableLikeSource { SourceEnumerable = enumerableSource.Table }
				: new SqlTableLikeSource { SourceQuery = sourceContext.SelectQuery };
		}

		Dictionary<SqlPlaceholderExpression, SqlPlaceholderExpression> _knownMap = new (ExpressionEqualityComparer.Instance);

		public void       BuildQuery<T>(Query<T>      query,      ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int                 level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			var subqueryPath  = SequenceHelper.CorrectExpression(path, this, SubqueryContext);
			var correctedPath = subqueryPath;

			if (!ReferenceEquals(subqueryPath, path))
			{
				correctedPath = Builder.ConvertToSqlExpr(InnerQueryContext, correctedPath, flags);

				if (!flags.HasFlag(ProjectFlags.Test))
				{
					correctedPath = SequenceHelper.CorrectTrackingPath(correctedPath, SubqueryContext, this);

					var memberPath = TableLikeHelpers.GetMemberPath(subqueryPath);
					correctedPath = Builder.UpdateNesting(SubqueryContext, correctedPath);
					var placeholders = ExpressionBuilder.CollectPlaceholders2(correctedPath, memberPath).ToList();

					var remapped = TableLikeHelpers.RemapToFields(SubqueryContext, Source, Source.SourceFields, _knownMap, correctedPath, placeholders);

					return remapped;
				}
			}

			return correctedPath;
		}

		public IBuildContext Clone(CloningContext context)
		{
			throw new NotImplementedException();
		}

		public void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			throw new NotImplementedException();
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public void SetAlias(string? alias)
		{
		}

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			return SubqueryContext.GetResultStatement();
		}

		public void CompleteColumns()
		{
		}
	}
}
