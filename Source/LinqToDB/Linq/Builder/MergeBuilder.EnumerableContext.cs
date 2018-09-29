using LinqToDB.SqlQuery;
using System.Collections;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	internal partial class MergeBuilder : MethodCallBuilder
	{
		private class EnumerableContext : IBuildContext
		{
			public EnumerableContext(IEnumerable values)
			{
				Values = values;
			}

			public IEnumerable Values { get; }

			string IBuildContext._sqlQueryText => throw new System.NotImplementedException();

			ExpressionBuilder IBuildContext.Builder => throw new System.NotImplementedException();

			Expression IBuildContext.Expression => throw new System.NotImplementedException();

			SelectQuery IBuildContext.SelectQuery { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
			SqlStatement IBuildContext.Statement { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
			IBuildContext IBuildContext.Parent { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

			Expression IBuildContext.BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				throw new System.NotImplementedException();
			}

			void IBuildContext.BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new System.NotImplementedException();
			}

			SqlInfo[] IBuildContext.ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				throw new System.NotImplementedException();
			}

			int IBuildContext.ConvertToParentIndex(int index, IBuildContext context)
			{
				throw new System.NotImplementedException();
			}

			SqlInfo[] IBuildContext.ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				throw new System.NotImplementedException();
			}

			IBuildContext IBuildContext.GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new System.NotImplementedException();
			}

			SqlStatement IBuildContext.GetResultStatement()
			{
				throw new System.NotImplementedException();
			}

			ISqlExpression IBuildContext.GetSubQuery(IBuildContext context)
			{
				throw new System.NotImplementedException();
			}

			IsExpressionResult IBuildContext.IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				throw new System.NotImplementedException();
			}

			void IBuildContext.SetAlias(string alias)
			{
				throw new System.NotImplementedException();
			}
		}
	}
}
