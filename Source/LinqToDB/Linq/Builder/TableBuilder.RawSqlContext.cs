using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using LinqToDB.Expressions;
	using SqlQuery;

	partial class TableBuilder
	{
		static IBuildContext BuildRawSqlTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			PrepareRawSqlArguments(methodCall.Arguments[1],
				methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null,
				out var format, out var arguments);

			var sqlArguments = arguments.Select(a => builder.ConvertToSql(buildInfo.Parent, a)).ToArray();

			return new RawSqlContext(builder, buildInfo, methodCall.Method.GetGenericArguments()[0], format, sqlArguments);
		}

		public static void PrepareRawSqlArguments(Expression formatArg, Expression? parametersArg, out string format, out IEnumerable<Expression> arguments)
		{
			// Consider that FormattableString is used
			if (formatArg.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)formatArg;

				if (mc.Arguments[1].NodeType != ExpressionType.NewArrayInit)
				{
					format    = (string)mc.Arguments[0].EvaluateExpression()!;
					arguments = mc.Arguments.Skip(1).ToArray();
				}
				else
				{
					format    = (string)mc.Arguments[0].EvaluateExpression()!;
					arguments = ((NewArrayExpression)mc.Arguments[1]).Expressions;
				}
			}
#if !NET45
			else if (formatArg is ConstantExpression constExpr && typeof(FormattableString).IsAssignableFrom(formatArg.Type))
			{
				var formattable    = (FormattableString)constExpr.Value;
				format             = formattable.Format;
				arguments          = formattable
					.GetArguments()
					.Select((a, i) =>
					{
						var type = a?.GetType();
						if (type == null || !typeof(ISqlExpression).IsAssignableFrom(type))
							return (Expression)new FormattableParameterExpression(constExpr, i);
						return Expression.Constant(a);
					});
			}
#endif
			else if (formatArg.Type == typeof(RawSqlString))
			{
				format = ((RawSqlString)formatArg.EvaluateExpression()!).Format;

				var arrayExpr = parametersArg!;

				if (arrayExpr.NodeType == ExpressionType.NewArrayInit)
					arguments = ((NewArrayExpression)arrayExpr).Expressions;
				else
				{
					var array = (object[])arrayExpr.EvaluateExpression()!;
					arguments = array.Select((a, i) => new FormattableParameterExpression(formatArg, arrayExpr, i, a?.GetType() ?? typeof(object)));
				}
			}
			else
				throw new NotImplementedException($"Unsupported {nameof(formatArg)}: {formatArg.NodeType} ({formatArg.Type})");
		}

		class RawSqlContext : TableContext
		{
			public RawSqlContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType, string sql, params ISqlExpression[] parameters)
				: base(builder, buildInfo, new SqlRawSqlTable(builder.MappingSchema, originalType, sql, parameters))
			{
			}
		}
	}
}
