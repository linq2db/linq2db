using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	sealed class SingleExpressionContext : IBuildContext
	{
		public SingleExpressionContext(IBuildContext? parent, ExpressionBuilder builder, ISqlExpression sqlExpression, SelectQuery selectQuery)
		{
			Parent        = parent;
			Builder       = builder;
			SqlExpression = sqlExpression;
			SelectQuery   = selectQuery;

			Builder.Contexts.Add(this);
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

#if DEBUG
		public string SqlQueryText => SelectQuery?.SqlText ?? "";
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif

		public IBuildContext?     Parent        { get; set; }
		public ExpressionBuilder  Builder       { get; set; }
		public ISqlExpression     SqlExpression { get; }
		public SelectQuery        SelectQuery   { get; set; }
		public SqlStatement?      Statement     { get; set; }
		Expression? IBuildContext.Expression    => null;

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

		public SqlInfo[] ConvertToIndex (Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		public Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				return ExpressionBuilder.CreatePlaceholder(this, SqlExpression, path);
			}

			throw new NotImplementedException();
		}

		public IBuildContext Clone(CloningContext context)
		{
			return new SingleExpressionContext(Parent, Builder, context.CloneElement(SqlExpression),
				context.CloneElement(SelectQuery));
		}

		public void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public IBuildContext? GetContext     (Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public SqlStatement GetResultStatement()
		{
			throw new InvalidOperationException();
		}

		public void CompleteColumns()
		{
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
			return null;
		}
	}
}
