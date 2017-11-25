using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class ContainsBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Contains") && methodCall.Arguments.Count == 2;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var buildInStatement = false;

			if (sequence.SelectQuery.Select.TakeValue != null ||
			    sequence.SelectQuery.Select.SkipValue != null)
			{
				sequence         = new SubQueryContext(sequence);
				buildInStatement = true;
			}

			return new ContainsContext(buildInfo.Parent, methodCall, sequence, buildInStatement);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public static bool IsConstant(MethodCallExpression methodCall)
		{
			if (!methodCall.IsQueryable("Contains"))
				return false;

			return methodCall.IsQueryable(false) == false;
		}

		class ContainsContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;
			readonly bool                 _buildInStatement;

			public ContainsContext(IBuildContext parent, MethodCallExpression methodCall, IBuildContext sequence, bool buildInStatement)
				: base(parent, sequence, null)
			{
				_methodCall       = methodCall;
				_buildInStatement = buildInStatement;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var sql = GetSubQuery(null);

				query.Queries[0].SelectQuery = new SelectQuery();
				query.Queries[0].SelectQuery.Select.Add(sql);

				var expr   = Builder.BuildSql(typeof(bool), 0);
				var mapper = Builder.BuildMapper<object>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				var idx = ConvertToIndex(expression, level, ConvertFlags.Field);
				return Builder.BuildSql(typeof(bool), idx[0].Index);
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (expression == null)
				{
					var sql   = GetSubQuery(null);
					var query = SelectQuery;

					if (Parent != null)
						query = Parent.SelectQuery;

					return new[] { new SqlInfo { Query = query, Sql = sql } };
				}

				throw new InvalidOperationException();
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

				switch (requestFlag)
				{
					case RequestFor.Root : return IsExpressionResult.False;
				}

				throw new InvalidOperationException();
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				throw new InvalidOperationException();
			}

			ISqlExpression _subQuerySql;

			public override ISqlExpression GetSubQuery(IBuildContext context)
			{
				if (_subQuerySql == null)
				{
					var args      = _methodCall.Method.GetGenericArguments();
					var param     = Expression.Parameter(args[0], "param");
					var expr      = _methodCall.Arguments[1];
					var condition = Expression.Lambda(ExpressionBuilder.Equal(Builder.MappingSchema, param, expr), param);

					IBuildContext ctx = new ExpressionContext(Parent, Sequence, condition);

					ctx = Builder.GetContext(ctx, expr) ?? ctx;

					Builder.ReplaceParent(ctx, this);

					SelectQuery.Condition cond;

					if ((Sequence.SelectQuery != SelectQuery || _buildInStatement) &&
						(ctx.IsExpression(expr, 0, RequestFor.Field).     Result ||
						 ctx.IsExpression(expr, 0, RequestFor.Expression).Result))
					{
						Sequence.ConvertToIndex(null, 0, ConvertFlags.All);
						var ex = Builder.ConvertToSql(ctx, _methodCall.Arguments[1]);
						cond = new SelectQuery.Condition(false, new SelectQuery.Predicate.InSubQuery(ex, false, SelectQuery));
					}
					else
					{
						var sequence = Builder.BuildWhere(Parent, Sequence, condition, true);
						cond = new SelectQuery.Condition(false, new SelectQuery.Predicate.FuncLike(SqlFunction.CreateExists(sequence.SelectQuery)));
					}

					_subQuerySql = new SelectQuery.SearchCondition(cond);
				}

				return _subQuerySql;
			}
		}
	}
}
