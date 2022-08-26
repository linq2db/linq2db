using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class AllAnyBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames      = { "All"     , "Any"      };
		private static readonly string[] MethodNamesAsync = { "AllAsync", "AnyAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) ||
				methodCall.IsAsyncExtension(MethodNamesAsync);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) { /*CopyTable = true,*/ CreateSubQuery = true, SelectQuery = new SelectQuery()});

			var isAsync = methodCall.Method.DeclaringType == typeof(AsyncExtensions);

			if (methodCall.Arguments.Count == (isAsync ? 3 : 2))
			{
				if (sequence.SelectQuery.Select.TakeValue != null ||
				    sequence.SelectQuery.Select.SkipValue != null)
				{
					sequence = new SubQueryContext(sequence);
				}

				var condition = (LambdaExpression)methodCall.Arguments[1].Unwrap();

				if (methodCall.Method.Name.StartsWith("All"))
					condition = Expression.Lambda(Expression.Not(condition.Body), condition.Name, condition.Parameters);

				sequence = builder.BuildWhere(buildInfo.Parent, sequence, condition, true, false, buildInfo.AggregationTest);
				sequence.SetAlias(condition.Parameters[0].Name);
			}

			return new AllAnyContext(buildInfo.Parent, buildInfo.SelectQuery, methodCall, sequence);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		class AllAnyContext : SequenceContextBase
		{
			readonly MethodCallExpression _methodCall;

			public AllAnyContext(IBuildContext? parent,     SelectQuery   selectQuery,
				MethodCallExpression            methodCall, IBuildContext sequence)
				: base(parent, sequence, null)
			{
				SelectQuery = selectQuery;
				_methodCall = methodCall;
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

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

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return ThrowHelper.ThrowNotImplementedException<IBuildContext>();
			}

			SqlPlaceholderExpression? _innerSql;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (!SequenceHelper.IsSameContext(path, this))
					throw new InvalidOperationException();

				if (_innerSql == null)
				{ 
					var cond = new SqlCondition(
						_methodCall.Method.Name.StartsWith("All"),
						new SqlPredicate.FuncLike(SqlFunction.CreateExists(Sequence.SelectQuery)));

					_innerSql = ExpressionBuilder.CreatePlaceholder(this, new SqlSearchCondition(cond), path, convertType: typeof(bool));
				}

				return _innerSql;
			}

			public override SqlStatement GetResultStatement()
			{
				return Statement ??= new SqlSelectStatement(SelectQuery);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new AllAnyContext(null, context.CloneElement(SelectQuery), context.CloneExpression(_methodCall), context.CloneContext(Sequence));
			}
		}
	}
}
