using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Cast")]
	sealed class CastBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
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

			public override Type ElementType => _methodCall.Method.GetGenericArguments()[0];

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
				{
					// Check if the corrected type is compatible with the target type
					// (e.g., derived type can be used as base type)
					if (type.IsAssignableFrom(corrected.Type))
					{
						// If corrected type is derived from target type, we can use it as-is
						// No explicit conversion is needed
						return corrected;
					}

					// Otherwise, attempt the conversion
					corrected = Expression.Convert(corrected, type);
				}

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
