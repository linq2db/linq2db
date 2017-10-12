using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class CastBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Cast");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			return new CastContext(sequence, methodCall);
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		class CastContext : PassThroughContext
		{
			public CastContext(IBuildContext context, MethodCallExpression methodCall)
				: base(context)
			{
				_methodCall = methodCall;
			}

			private readonly MethodCallExpression _methodCall;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildExpression(null, 0, false);
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				var expr = base.BuildExpression(expression, level, enforceServerSide);
				var type = _methodCall.Method.GetGenericArguments()[0];

				if (expr.Type != type)
					expr = Expression.Convert(expr, type);

				return expr;
			}
		}
	}
}
