using System;
using System.Linq.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	public class AssociationContext : IBuildContext
	{
		public string? _sqlQueryText { get; }
		public string Path { get; }
		public ExpressionBuilder Builder { get; }
		public Expression? Expression { get; }
		public SelectQuery SelectQuery { get; set { throw new NotImplementedException();} }
		public SqlStatement? Statement { get; set; }
		public IBuildContext? Parent { get; set; }

		public AssociationContext(TableBuilder.TableContext tableContext, IBuildContext subqueryContext)
		{
			
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new System.NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new System.NotImplementedException();
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new System.NotImplementedException();
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			throw new System.NotImplementedException();
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new System.NotImplementedException();
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			throw new System.NotImplementedException();
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new System.NotImplementedException();
		}

		public void SetAlias(string alias)
		{
			throw new System.NotImplementedException();
		}

		public ISqlExpression? GetSubQuery(IBuildContext context)
		{
			throw new System.NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			throw new System.NotImplementedException();
		}
	}
}
