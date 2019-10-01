using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Extensions;

namespace LinqToDB.Linq.Builder
{
	using SqlQuery;

	partial class TableBuilder
	{
		static IBuildContext BuildValuesTable(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var methodCall = (MethodCallExpression)buildInfo.Expression;

			var originalType = methodCall.Method.GetGenericArguments()[0];
			var isScalarType = builder.MappingSchema.IsScalarType(originalType);

			Expression dataContext, sourceExpression;

			if (typeof(IDataContext).IsAssignableFromEx(methodCall.Arguments[0].Type))
			{
				dataContext      = methodCall.Arguments[0];
				sourceExpression = methodCall.Arguments[1];
			}
			else
			{
				dataContext      = methodCall.Arguments[1];
				sourceExpression = methodCall.Arguments[0];
			}

			ISqlExpression expr = null;

			switch (sourceExpression.NodeType)
			{
				case ExpressionType.Constant:
					break;
			}

			return new ValuesTableContext(builder, buildInfo,
				new SqlValuesTable(builder.MappingSchema, originalType, "Values", expr));
		}

		class ValuesTableContext : TableContext
		{
			public ValuesTableContext(ExpressionBuilder builder, BuildInfo buildInfo, SqlValuesTable table)
				: base(builder, buildInfo, table)
			{
			}
		}
	}
}
