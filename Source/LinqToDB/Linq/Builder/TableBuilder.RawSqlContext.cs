using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	partial class TableBuilder
	{
#if !NET45
		private static MethodInfo _getArgumentMethodInfo =
			MemberHelper.MethodOf(() => ((FormattableString)null).GetArgument(0));
#endif

		static IBuildContext BuildRawSqlTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			//TODO: Parse SQL for parameters
			string sqlFormat ;
			var arguments = new List<ISqlExpression>();

			var sql = methodCall.Arguments[1].EvaluateExpression();
#if !NET45
			if (sql is FormattableString formattable)
			{
				var formattableExpr = methodCall.Arguments[1];

				var objects = formattable.GetArguments();
				for (var i = 0; i < objects.Length; i++)
				{
					var getter = Expression.Call(formattableExpr, _getArgumentMethodInfo,
						Expression.Constant(i));

					var v = builder.ConvertToSql(null, getter);
					arguments.Add(v);
				}

				sqlFormat = formattable.Format;
			} else

#endif
			{
				sqlFormat = ((RawSqlString)sql).Format;

				var parametersExpr = methodCall.Arguments[2];
				var array = (object[])parametersExpr.EvaluateExpression();

				for (var i = 0; i < array.Length; i++)
				{
					var getter = Expression.ArrayIndex(parametersExpr, Expression.Constant(i));

					var v = builder.ConvertToSql(null, getter);
					arguments.Add(v);
				}
			}


			return new RawSqlContext(builder, buildInfo, methodCall.Method.GetGenericArguments()[0], sqlFormat, arguments.ToArray());
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
