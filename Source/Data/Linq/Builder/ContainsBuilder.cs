using System;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	using Data.Sql;

	class ContainsBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Contains") && methodCall.Arguments.Count == 2;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			return new ContainsContext(buildInfo.Parent, methodCall, sequence);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		class ContainsContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;

			public ContainsContext(IBuildContext parent, MethodCallExpression methodCall, IBuildContext sequence)
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
					var args      = _methodCall.Method.GetGenericArguments();
					var param     = Expression.Parameter(args[0], "param");
					var expr      = _methodCall.Arguments[1];
					var condition = Expression.Lambda(Expression.Equal(param, expr), param);

					IBuildContext ctx = new ExpressionContext(Parent, Sequence, condition);

					ctx = Builder.GetContext(ctx, expr) ?? ctx;

					Builder.ReplaceParent(ctx, this);

					SqlQuery.Condition cond;

					if (Sequence.SqlQuery != SqlQuery &&
						(ctx.IsExpression(expr, 0, RequestFor.Field).     Result ||
						 ctx.IsExpression(expr, 0, RequestFor.Expression).Result))
					{
						Sequence.ConvertToIndex(null, 0, ConvertFlags.All);
						var ex = Builder.ConvertToSql(ctx, _methodCall.Arguments[1]);
						cond = new SqlQuery.Condition(false, new SqlQuery.Predicate.InSubQuery(ex, false, SqlQuery));
					}
					else
					{
						var sequence = Builder.BuildWhere(Parent, Sequence, condition, true);
						cond = new SqlQuery.Condition(false, new SqlQuery.Predicate.FuncLike(SqlFunction.CreateExists(sequence.SqlQuery)));
					}

					_subQuerySql = new SqlQuery.SearchCondition(cond);
				}

				return _subQuerySql;
			}
		}
	}
}
