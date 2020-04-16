using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
			else
			{
				var evaluatedSql = formatArg.EvaluateExpression()!;
#if !NET45
				if (evaluatedSql is FormattableString formattable)
				{
					format = formattable.Format;
					arguments = formattable.GetArguments().Select(Expression.Constant);
				}
				else
#endif
				{
					var rawSqlString = (RawSqlString)evaluatedSql;

					format        = rawSqlString.Format;
					var arrayExpr = parametersArg!;

					if (arrayExpr.NodeType == ExpressionType.NewArrayInit)
						arguments = ((NewArrayExpression)arrayExpr).Expressions;
					else
					{
						var array = (object[])arrayExpr.EvaluateExpression()!;
						arguments = array.Select(Expression.Constant);
					}
				}
			}
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
