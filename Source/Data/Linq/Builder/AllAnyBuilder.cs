using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	using Data.Sql;

	class AllAnyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("All", "Any");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (methodCall.Arguments.Count == 2)
			{
				var condition = (LambdaExpression)methodCall.Arguments[1].Unwrap();

				if (methodCall.Method.Name == "All")
#if FW4 || SILVERLIGHT
					condition = Expression.Lambda(Expression.Not(condition.Body), condition.Name, condition.Parameters);
#else
					condition = Expression.Lambda(Expression.Not(condition.Body), condition.Parameters.ToArray());
#endif

				sequence = builder.BuildWhere(buildInfo.Parent, sequence, condition, true);
				sequence.SetAlias(condition.Parameters[0].Name);
			}

			return new AllAnyContext(buildInfo.Parent, methodCall, sequence);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			if (methodCall.Arguments.Count == 2)
			{
				var predicate = (LambdaExpression)methodCall.Arguments[1].Unwrap();
				var info      = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), predicate.Parameters[0]);

				if (info != null)
				{
					info.Expression = methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, predicate.Parameters[0], ex));
					info.Parameter  = param;

					return info;
				}
			}
			else
			{
				var info = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), null);

				if (info != null)
				{
					info.Expression = methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, null, ex));
					info.Parameter  = param;

					return info;
				}
			}

			return null;
		}

		class AllAnyContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;

			public AllAnyContext(IBuildContext parent, MethodCallExpression methodCall, IBuildContext sequence)
				: base(parent, sequence, null)
			{
				_methodCall = methodCall;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var sql = GetSubQuery(null);

				query.Queries[0].SqlQuery = new SqlQuery();
				query.Queries[0].SqlQuery.Select.Add(sql);

				var expr   = Builder.BuildSql(typeof(bool), 0);
				var mapper = Builder.BuildMapper<object>(expr);

				query.SetElementQuery(mapper.Compile());
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				var idx = ConvertToIndex(expression, level, ConvertFlags.Field);
				return Builder.BuildSql(typeof(bool), idx[0].Index);
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (expression == null)
				{
					var sql   = GetSubQuery(null);
					var query = SqlQuery;

					if (Parent != null)
						query = Parent.SqlQuery;

					return new[] { new SqlInfo { Query = query, Sql = sql } };
				}

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				var sql = ConvertToSql(expression, level, flags);

				if (sql[0].Index < 0)
					sql[0].Index = sql[0].Query.Select.Add(sql[0].Sql);

				return sql;
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				if (expression == null)
				{
					switch (requestFlag)
					{
						case RequestFor.Expression :
						case RequestFor.Field      : return IsExpressionResult.False;
					}
				}

				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}

			ISqlExpression _subQuerySql;

			public override ISqlExpression GetSubQuery(IBuildContext context)
			{
				if (_subQuerySql == null)
				{
					var cond = new SqlQuery.Condition(
						_methodCall.Method.Name == "All",
						new SqlQuery.Predicate.FuncLike(SqlFunction.CreateExists(SqlQuery)));

					_subQuerySql = new SqlQuery.SearchCondition(cond);
				}

				return _subQuerySql;
			}
		}
	}
}
