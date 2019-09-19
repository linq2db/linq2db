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
		static IBuildContext BuildValuesTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			if (builder.MappingSchema.IsScalarType(methodCall.Method.GetGenericArguments()[0]))
				throw new LinqToDBException("Selection of scalar types not supported by AsValuesTable method. Use mapping class with one column for scalar values");

			PrepareRawSqlArguments(methodCall.Arguments[1],
				methodCall.Arguments.Count > 2 ? methodCall.Arguments[2] : null,
				out var format, out var arguments);

			var sqlArguments = arguments.Select(a => builder.ConvertToSql(buildInfo.Parent, a)).ToArray();

			return new RawSqlContext(builder, buildInfo, methodCall.Method.GetGenericArguments()[0], format, sqlArguments);
		}

		class ValuesTableContext : TableContext
		{
			public ValuesTableContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType, string sql, params ISqlExpression[] parameters)
				: base(builder, buildInfo, new SqlRawSqlTable(builder.MappingSchema, originalType, sql, parameters))
			{
			}
		}
	}
}
