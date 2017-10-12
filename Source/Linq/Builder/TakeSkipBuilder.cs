using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
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

			if (sequence.SelectQuery.Select.IsDistinct)
				sequence = new SubQueryContext(sequence);

			var arg = methodCall.Arguments[1].Unwrap();

			if (arg.NodeType == ExpressionType.Lambda)
				arg = ((LambdaExpression)arg).Body.Unwrap();

			var expr = builder.ConvertToSql(sequence, arg);

			if (methodCall.Method.Name == "Take")
			{
				TakeHints? hints = null;
				if (methodCall.Arguments.Count == 3 && methodCall.Arguments[2].Type == typeof(TakeHints))
					hints = (TakeHints)((ConstantExpression)methodCall.Arguments[2]).Value;

				BuildTake(builder, sequence, expr, hints);
			}
			else
			{
				BuildSkip(builder, sequence, sequence.SelectQuery.Select.SkipValue, expr);
			}

			return sequence;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			var info = builder.ConvertSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]), null);

			if (info != null)
			{
				info.Expression =
					Expression.Call(
						methodCall.Method.DeclaringType,
						methodCall.Method.Name,
						new[] { info.Expression.Type.GetGenericArgumentsEx()[0] },
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
				throw new LinqException("TakeHints are {0} not supported by current database".Args(hints));

			if (hints != null && sql.Select.SkipValue != null)
				throw new LinqException("Take with hints could not be applied with Skip");

			sql.Select.Take(expr, hints);

			if ( sql.Select.SkipValue != null &&
				 builder.DataContext.SqlProviderFlags.IsTakeSupported &&
				!builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql))
			{
				if (sql.Select.SkipValue is SqlParameter && sql.Select.TakeValue is SqlValue)
				{
					var skip = (SqlParameter)sql.Select.SkipValue;
					var parm = (SqlParameter)sql.Select.SkipValue.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);

					parm.SetTakeConverter((int)((SqlValue)sql.Select.TakeValue).Value);

					sql.Select.Take(parm, hints);

					var ep = (from pm in builder.CurrentSqlParameters where pm.SqlParameter == skip select pm).First();

					ep = new ParameterAccessor(ep.Expression, ep.Accessor, ep.DataTypeAccessor, parm);

					builder.CurrentSqlParameters.Add(ep);
				}
				else
					sql.Select.Take(builder.Convert(
						sequence,
						new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue, Precedence.Additive)), hints);
			}

			if (!builder.DataContext.SqlProviderFlags.GetAcceptsTakeAsParameterFlag(sql))
			{
				var p = sql.Select.TakeValue as SqlParameter;

				if (p != null)
					p.IsQueryParameter = false;
			}
		}

		static void BuildSkip(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression prevSkipValue, ISqlExpression expr)
		{
			var sql = sequence.SelectQuery;

			if (sql.Select.TakeHints != null)
				throw new LinqException("Skip could not be applied with Take with hints");


			sql.Select.Skip(expr);

			if (sql.Select.TakeValue != null)
			{
				if (builder.DataContext.SqlProviderFlags.GetIsSkipSupportedFlag(sql) ||
					!builder.DataContext.SqlProviderFlags.IsTakeSupported)
					sql.Select.Take(builder.Convert(
						sequence,
						new SqlBinaryExpression(typeof(int), sql.Select.TakeValue, "-", sql.Select.SkipValue, Precedence.Additive)), sql.Select.TakeHints);

				if (prevSkipValue != null)
					sql.Select.Skip(builder.Convert(
						sequence,
						new SqlBinaryExpression(typeof(int), prevSkipValue, "+", sql.Select.SkipValue, Precedence.Additive)));
			}

			if (!builder.DataContext.SqlProviderFlags.GetAcceptsTakeAsParameterFlag(sql))
			{
				var p = sql.Select.SkipValue as SqlParameter;

				if (p != null)
					p.IsQueryParameter = false;
			}
		}
	}
}
