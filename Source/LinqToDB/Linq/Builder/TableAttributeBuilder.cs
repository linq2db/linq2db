﻿using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;

	class TableAttributeBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("TableName", "ServerName", "DatabaseName", "SchemaName");
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = (TableBuilder.TableContext)sequence;
			var value    = (string?)methodCall.Arguments[1].EvaluateExpression();

			switch (methodCall.Method.Name)
			{
				case "TableName"    : table.SqlTable.PhysicalName = value!; break;
				case "ServerName"   : table.SqlTable.Server       = value;  break;
				case "DatabaseName" : table.SqlTable.Database     = value;  break;
				case "SchemaName"   : table.SqlTable.Schema       = value;  break;
			}

			return sequence;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}
	}
}
