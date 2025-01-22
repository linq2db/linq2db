using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	[BuildsMethodCall("Cast")]
	sealed class CastBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult.BuildContext == null)
				return buildResult;

			return BuildSequenceResult.FromContext(new CastContext(buildResult.BuildContext, methodCall));
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
