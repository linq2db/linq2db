using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	using Data.Sql;

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

			if (arg.NodeType == ExpressionType.Lambda)
				arg = ((LambdaExpression)arg).Body.Unwrap();

			var expr = builder.ConvertToSql(sequence, arg);

			if (methodCall.Method.Name == "Take")
			{
				BuildTake(builder, sequence, expr);
			}
			else
			{
				BuildSkip(builder, sequence, sequence.SqlQuery.Select.SkipValue, expr);
			}

			return sequence;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		static void BuildTake(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression expr)
		{
			var sql = sequence.SqlQuery;

			builder.SqlProvider.SqlQuery = sql;

			sql.Select.Take(expr);

			if (sql.Select.SkipValue != null && builder.SqlProvider.IsTakeSupported && !builder.SqlProvider.IsSkipSupported)
			{
				if (sql.Select.SkipValue is SqlParameter && sql.Select.TakeValue is SqlValue)
				{
					var skip = (SqlParameter)sql.Select.SkipValue;
					var parm = (SqlParameter)sql.Select.SkipValue.Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);

					parm.SetTakeConverter((int)((SqlValue)sql.Select.TakeValue).Value);

					sql.Select.Take(parm);

					var ep = (from pm in builder.CurrentSqlParameters where pm.SqlParameter == skip select pm).First();

					ep = new ParameterAccessor
					{
						Expression   = ep.Expression,
						Accessor     = ep.Accessor,
						SqlParameter = parm
					};

					builder.CurrentSqlParameters.Add(ep);
				}
				else
					sql.Select.Take(builder.Convert(
						sequence,
						new SqlBinaryExpression(typeof(int), sql.Select.SkipValue, "+", sql.Select.TakeValue, Precedence.Additive)));
			}

			if (!builder.SqlProvider.TakeAcceptsParameter)
			{
				var p = sql.Select.TakeValue as SqlParameter;

				if (p != null)
					p.IsQueryParameter = false;
			}
		}

		static void BuildSkip(ExpressionBuilder builder, IBuildContext sequence, ISqlExpression prevSkipValue, ISqlExpression expr)
		{
			var sql = sequence.SqlQuery;

			builder.SqlProvider.SqlQuery = sql;

			sql.Select.Skip(expr);

			builder.SqlProvider.SqlQuery = sql;

			if (sql.Select.TakeValue != null)
			{
				if (builder.SqlProvider.IsSkipSupported || !builder.SqlProvider.IsTakeSupported)
					sql.Select.Take(builder.Convert(
						sequence,
						new SqlBinaryExpression(typeof(int), sql.Select.TakeValue, "-", sql.Select.SkipValue, Precedence.Additive)));

				if (prevSkipValue != null)
					sql.Select.Skip(builder.Convert(
						sequence,
						new SqlBinaryExpression(typeof(int), prevSkipValue, "+", sql.Select.SkipValue, Precedence.Additive)));
			}

			if (!builder.SqlProvider.TakeAcceptsParameter)
			{
				var p = sql.Select.SkipValue as SqlParameter;

				if (p != null)
					p.IsQueryParameter = false;
			}
		}
	}
}
