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

				if (expr.ElementType == QueryElementType.SqlValue)
				{
					var param   = builder.ParametersContext.BuildParameter(methodCall.Arguments[1], null, true).SqlParameter;
					param.Name             = methodCall.Method.Name == "Take" ? "take" : "skip";
					param.IsQueryParameter = param.IsQueryParameter && parametrize;
					expr                   = param;
				}
			}

			if (methodCall.Method.Name == "Take")
			{
				TakeHints? hints = null;
				if (methodCall.Arguments.Count == 3 && methodCall.Arguments[2].Type == typeof(TakeHints))
					hints = (TakeHints)methodCall.Arguments[2].EvaluateExpression(builder.DataContext)!;

				BuildTake(builder, sequence, expr, hints);
			}
			else
			{
				BuildSkip(builder, sequence, expr);
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

		static void BuildTake(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression expr, TakeHints? hints)
		{
			var sql = sequence.SelectQuery;

			if (hints != null && !builder.DataContext.SqlProviderFlags.GetIsTakeHintsSupported(hints.Value))
				throw new LinqException($"TakeHints are {hints} not supported by current database");

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqException("Take with hints could not be applied with Skip");

			if (sql.Select.TakeValue != null)
			{
				expr = new SqlFunction(
					typeof(int),
					"CASE",
					new SqlBinaryExpression(typeof(bool), sql.Select.TakeValue, "<", expr, Precedence.Comparison),
					sql.Select.TakeValue,
					expr);
			}

			sql.Select.Take(expr, hints);

			if ( sql.Select.SkipValue != null &&
				 builder.DataContext.SqlProviderFlags.IsTakeSupported &&
				!builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql.Select.TakeValue, sql.Select.SkipValue))
			{
				sql.Select.Take(
					new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue!,
						Precedence.Additive), hints);
			}
		}

		static void BuildSkip(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression expr)
		{
			var sql = sequence.SelectQuery;

			if (sql.Select.TakeHints != null)
				throw new LinqException("Skip could not be applied with Take with hints");

			if (sql.Select.SkipValue != null)
				sql.Select.Skip(new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", expr, Precedence.Additive));
			else
				sql.Select.Skip(expr);

			if (sql.Select.TakeValue != null)
			{
				if (builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql.Select.TakeValue, sql.Select.SkipValue) ||
					!builder.DataContext.SqlProviderFlags.IsTakeSupported)
				{
					sql.Select.Take(
						new SqlBinaryExpression(typeof(int), sql.Select.TakeValue, "-", expr, Precedence.Additive),
						sql.Select.TakeHints);
				}
			}
		}
	}
}
