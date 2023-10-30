using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
﻿	using Expressions;
	//TODO: probably remove, SqlAdjustTypeExpression is doing what you need
	[BuildsExpression(ChangeTypeExpression.ChangeTypeType)]
	sealed class ChangeTypeExpressionBuilder : ISequenceBuilder
	{
		public static bool CanBuild(Expression expr, BuildInfo info, ExpressionBuilder builder) => true;

		public IBuildContext? BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var expr = (ChangeTypeExpression)buildInfo.Expression;
			var info = new BuildInfo(buildInfo, expr.Expression);
			return builder.FindBuilder(info).BuildSequence(builder, info);
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
			=> null;

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var expr = (ChangeTypeExpression)buildInfo.Expression;
			var info = new BuildInfo(buildInfo, expr.Expression);
			return builder.FindBuilder(info).IsSequence(builder, info);
		}
	}
}
