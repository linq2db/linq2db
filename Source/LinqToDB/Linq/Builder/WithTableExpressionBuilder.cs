﻿using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Common;
	using LinqToDB.Expressions;
	using SqlQuery;

	class WithTableExpressionBuilder : MethodCallBuilder
	{
		private static readonly string[] MethodNames = { "With", "WithTableExpression" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable(MethodNames);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var table    = (TableBuilder.TableContext)sequence;
			var value    = (string)methodCall.Arguments[1].EvaluateExpression()!;

			table.SqlTable.SqlTableType   = SqlTableType.Expression;
			table.SqlTable.TableArguments = Array<ISqlExpression>.Empty;

			switch (methodCall.Method.Name)
			{
				case "With"                : table.SqlTable.Name = $"{{0}} {{1}} WITH ({value})"; break;
				case "WithTableExpression" : table.SqlTable.Name = value;                         break;
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
