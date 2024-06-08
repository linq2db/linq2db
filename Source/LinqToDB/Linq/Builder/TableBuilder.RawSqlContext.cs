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
		static IBuildContext BuildRawSqlTable(ExpressionBuilder builder, BuildInfo buildInfo, bool isScalar)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			PrepareRawSqlArguments(methodCall.Arguments[1],
				methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null,
				out var format, out var arguments);

			var sqlArguments = new ISqlExpression[arguments.Count];
			for (var i = 0; i < arguments.Count; i++)
				sqlArguments[i] = builder.ConvertToSql(buildInfo.Parent, arguments[i]);

			return new RawSqlContext(builder, buildInfo, methodCall.Method.GetGenericArguments()[0], isScalar, format, sqlArguments);
		}

		public static void PrepareRawSqlArguments(Expression formatArg, Expression? parametersArg, out string format, out IReadOnlyList<Expression> arguments)
		{
			// Consider that FormattableString is used
			if (formatArg.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)formatArg;

				if (mc.Arguments[1].NodeType != ExpressionType.NewArrayInit)
				{
					format    = mc.Arguments[0].EvaluateExpression<string>()!;
					var args  = new Expression[mc.Arguments.Count - 1];

					for (var i = 0; i < args.Length; i++)
						args[i] = mc.Arguments[i + 1];

					arguments = args;
				}
				else
				{
					format    = mc.Arguments[0].EvaluateExpression<string>()!;
					arguments = ((NewArrayExpression)mc.Arguments[1]).Expressions;
				}
			}
			else
			{
				var evaluatedSql = formatArg.EvaluateExpression()!;
#if !NET45
				if (evaluatedSql is FormattableString formattable)
				{
					format     = formattable.Format;

					var array = formattable.GetArguments();
					var args   = new Expression[array.Length];

					for (var i = 0; i < array.Length; i++)
					{
						var type = array[i]?.GetType() ?? typeof(object);

						if (typeof(ISqlExpression).IsAssignableFrom(type))
						{
							args[i] = Expression.Constant(array[i]);
							continue;
						}

						Expression expr = Expression.Call(formatArg, ReflectionHelper.Functions.FormattableString.GetArguments, ExpressionInstances.Int32Array(i));

						if (type != typeof(object))
							expr = Expression.Convert(expr, type);

						args[i] = expr;
					}

					arguments = args;
				}
				else
#endif
				{
					var rawSqlString = (RawSqlString)evaluatedSql;

					format        = rawSqlString.Format;
					var arrayExpr = parametersArg!;

					if (arrayExpr.NodeType == ExpressionType.NewArrayInit)
					{
						arguments = ((NewArrayExpression)arrayExpr).Expressions;
					}
					else
					{
						var array = arrayExpr.EvaluateExpression<object[]>()!;
						var args  = new Expression[array.Length];
						for (var i = 0; i < array.Length; i++)
						{
							var type = array[i]?.GetType() ?? typeof(object);

							if (typeof(ISqlExpression).IsAssignableFrom(type))
							{
								args[i] = Expression.Constant(array[i]);
								continue;
							}

							Expression expr = Expression.ArrayIndex(arrayExpr, ExpressionInstances.Int32(i));
							if (type != typeof(object))
								expr = Expression.Convert(expr, type);

							args[i] = expr;
						}

						arguments = args;
					}
				}
			}
		}

		//TODO: We have to separate TableContext in proper hierarchy
		sealed class RawSqlContext : TableContext
		{
			public RawSqlContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType, bool isScalar, string sql, ISqlExpression[] parameters)
				: base(builder, buildInfo, new SqlRawSqlTable(builder.MappingSchema.GetEntityDescriptor(originalType, builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated), sql, parameters))
			{
				// Marking All field as not nullable for satisfying DefaultIfEmptyBuilder 
				if (isScalar)
				{
					IsScalar = true;
					SqlTable.CanBeNull = false;
				}
			}
		}
	}
}
