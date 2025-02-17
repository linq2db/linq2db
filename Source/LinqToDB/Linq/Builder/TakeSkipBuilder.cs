using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("Skip", "Take")]
	sealed class TakeSkipBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var info        = new BuildInfo(buildInfo, methodCall.Arguments[0]);
			var buildResult = builder.TryBuildSequence(info);

			if (buildResult.BuildContext == null)
				return buildResult;

			builder.PushTranslationModifier(buildResult.BuildContext.TranslationModifier, true);

			try
			{
				var sequence = buildResult.BuildContext;

				var arg      = methodCall.Arguments[1].Unwrap();

				ISqlExpression expr;

				var linqOptions  = builder.DataContext.Options.LinqOptions;
				var parameterize = !buildInfo.IsSubQuery && linqOptions.ParameterizeTakeSkip;

				if (arg.NodeType == ExpressionType.Lambda)
				{
					arg  = ((LambdaExpression)arg).Body.Unwrap();
					if (!builder.TryConvertToSql(sequence, arg, out expr!))
						return BuildSequenceResult.Error(arg);
				}
				else
				{
					// revert unwrap
					arg  = methodCall.Arguments[1];

					if (!builder.TryConvertToSql(sequence, arg, out expr!))
						return BuildSequenceResult.Error(arg);

					if (expr.ElementType == QueryElementType.SqlValue && builder.CanBeEvaluatedOnClient(methodCall.Arguments[1]))
					{
						var param = builder.ParametersContext.BuildParameter(sequence, methodCall.Arguments[1], null, forceNew : true)!;

						param.Name             = methodCall.Method.Name == "Take" ? "take" : "skip";
						param.IsQueryParameter = param.IsQueryParameter && parameterize;
						expr                   = param;
					}
				}

				if (expr is SqlParameter paramExpr)
				{
					// This parameter can be optimized out by QueryRunner, so we ensure that after finalization, parameter accessor will be in place.
					builder.ParametersContext.RegisterNonQueryParameter(paramExpr);
				}

				if (methodCall.Method.Name == "Take")
				{
					TakeHints? hints = null;
					if (methodCall.Arguments.Count == 3 && methodCall.Arguments[2].Type == typeof(TakeHints))
						hints = (TakeHints)builder.EvaluateExpression(methodCall.Arguments[2])!;

					builder.BuildTake(sequence, expr, hints);
				}
				else
				{
					builder.BuildSkip(sequence, expr);
				}

				return BuildSequenceResult.FromContext(new TakeSkipContext(sequence));
			}
			finally
			{
				builder.PopTranslationModifier();
			}
		}

		class TakeSkipContext : PassThroughContext
		{
			public TakeSkipContext(IBuildContext context) : base(context)
			{
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TakeSkipContext(context.CloneContext(Context));
			}
		}
	}
}
