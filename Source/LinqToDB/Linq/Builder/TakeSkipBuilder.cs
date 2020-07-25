using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	class TakeSkipBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("Skip", "Take");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var arg = methodCall.Arguments[1].Unwrap();

			ISqlExpression expr;
			var parametrize = Common.Configuration.Linq.ParametrizeTakeSkip;
			if (arg.NodeType == ExpressionType.Lambda)
			{
				arg  = ((LambdaExpression)arg).Body.Unwrap();
				expr = builder.ConvertToSql(sequence, arg);
			}
			else
			{
				expr = builder.ConvertToSql(sequence, arg);
				if (expr.ElementType == QueryElementType.SqlValue)
				{
					var param   = builder.BuildParameter(methodCall.Arguments[1], null, true).SqlParameter;
					param.Name  = methodCall.Method.Name == "Take" ? "take" : "skip";
					param.Value = expr.EvaluateExpression();
					param.IsQueryParameter = param.IsQueryParameter && parametrize;
					expr = param;
				}
			}

			if (methodCall.Method.Name == "Take")
			{
				TakeHints? hints = null;
				if (methodCall.Arguments.Count == 3 && methodCall.Arguments[2].Type == typeof(TakeHints))
					hints = (TakeHints)methodCall.Arguments[2].EvaluateExpression()!;

				BuildTake(builder, sequence, expr, hints);
			}
			else
			{
				BuildSkip(builder, sequence, expr);
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			var info = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), null, true);

			if (info != null)
			{
				info.Expression =
					Expression.Call(
						methodCall.Method.DeclaringType,
						methodCall.Method.Name,
						new[] { info.Expression.Type.GetGenericArguments()[0] },
						info.Expression, methodCall.Arguments[1]);
					//methodCall.Transform(ex => ConvertMethod(methodCall, 0, info, null, ex));
				info.Parameter  = param;

				return info;
			}

			return null;
		}

		static void BuildTake(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression expr, TakeHints? hints)
		{
			var sql = sequence.SelectQuery;

			if (hints != null && !builder.DataContext.SqlProviderFlags.GetIsTakeHintsSupported(hints.Value))
				throw new LinqException($"TakeHints are {hints} not supported by current database");

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqException("Take with hints could not be applied with Skip");

			if (sql.Select.TakeValue != null)
				expr = new SqlFunction(
					typeof(int),
					"CASE",
					new SqlBinaryExpression(typeof(bool), sql.Select.TakeValue, "<", expr, Precedence.Comparison),
					sql.Select.TakeValue,
					expr);

			sql.Select.Take(expr, hints);

			if ( sql.Select.SkipValue != null &&
				 builder.DataContext.SqlProviderFlags.IsTakeSupported &&
				!builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql))
				sql.Select.Take(
					new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue!, Precedence.Additive), hints);
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
				if (builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql) ||
					!builder.DataContext.SqlProviderFlags.IsTakeSupported)
					sql.Select.Take(
						new SqlBinaryExpression(typeof(int), sql.Select.TakeValue, "-", expr, Precedence.Additive), sql.Select.TakeHints);
			}
		}
	}
}
