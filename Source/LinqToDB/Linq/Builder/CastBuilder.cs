using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	sealed class CastBuilder : MethodCallBuilder
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

		sealed class CastContext : PassThroughContext
		{
			public CastContext(IBuildContext context, MethodCallExpression methodCall)
				: base(context)
			{
				_methodCall = methodCall;
			}

			readonly MethodCallExpression _methodCall;

			public override IBuildContext Clone(CloningContext context)
			{
				return new CastContext(context.CloneContext(Context), _methodCall);
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				var corrected = base.MakeExpression(path, flags);

				if (flags.IsTable())
					return corrected;

				var type = _methodCall.Method.GetGenericArguments()[0];

				if (corrected.Type != type)
					corrected = Expression.Convert(corrected, type);

				return corrected;
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);
				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
