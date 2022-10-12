using LinqToDB.Expressions;
using LinqToDB.SqlQuery;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal class TargetRedirectorContext : IBuildContext
	{
#if DEBUG
		public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif
		public SelectQuery? SelectQuery
		{
			get => Target.SelectQuery;
			set { }
		}

		public SqlStatement?  Statement { get; set; }
		public IBuildContext? Parent    { get; set; }

		public ExpressionBuilder Builder          { get; }
		public IBuildContext     Target           { get; }
		public IBuildContext     Source           { get; }
		public LambdaExpression  ConnectionLambda { get; }
		public Expression?       Expression       { get; }

		public TargetRedirectorContext(ExpressionBuilder builder, IBuildContext target, IBuildContext source, LambdaExpression connectionLambda)
		{
			Builder          = builder;
			Target           = target;
			Source           = source;
			ConnectionLambda = connectionLambda;

#if DEBUG
			ContextId = Builder.GenerateContextId();
#endif
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
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
			if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.Expand))
				return path;

			if (flags.HasFlag(ProjectFlags.AssociationRoot))
			{
				var corrected = SequenceHelper.ReplaceContext(path, this, Source);
				return corrected;
			}

			return path;
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
			throw new NotImplementedException();
		}

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			throw new NotImplementedException();
		}

		public void CompleteColumns()
		{
			throw new NotImplementedException();
		}
	}

}
