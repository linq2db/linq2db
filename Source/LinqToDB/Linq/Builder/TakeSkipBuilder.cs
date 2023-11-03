using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class TakeSkipBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames = { "Skip", "Take" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (sequence == null)
				return null;

			if (buildInfo.IsSubQuery)
			{
				if (sequence is not TakeSkipContext)
				{
					if (!SequenceHelper.IsSupportedSubqueryForModifier(sequence))
						return null;
				}
			}

			var arg      = methodCall.Arguments[1].Unwrap();

			ISqlExpression expr;

            var linqOptions = builder.DataContext.Options.LinqOptions;
			var parametrize = !buildInfo.IsSubQuery && linqOptions.ParameterizeTakeSkip;

			if (arg.NodeType == ExpressionType.Lambda)
			{
				arg  = ((LambdaExpression)arg).Body.Unwrap();
				expr = builder.ConvertToSql(sequence, arg);
			}
			else
			{
				// revert unwrap
				arg  = methodCall.Arguments[1];
				expr = builder.ConvertToSql(sequence, arg);

				if (expr.ElementType == QueryElementType.SqlValue && builder.CanBeCompiled(methodCall.Arguments[1], false))
				{
					var param = builder.ParametersContext.BuildParameter(methodCall.Arguments[1], null, forceConstant : true)!.SqlParameter;
					param.Name             = methodCall.Method.Name == "Take" ? "take" : "skip";
					param.IsQueryParameter = param.IsQueryParameter && parametrize;
					expr                   = param;
				}
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

			return new TakeSkipContext(sequence);
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
