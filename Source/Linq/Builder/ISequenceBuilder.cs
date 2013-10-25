﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	interface ISequenceBuilder
	{
		int                 BuildCounter { get; set; }
		bool                CanBuild     (ExpressionBuilder builder, BuildInfo buildInfo);
		IBuildContext       BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo);
		SequenceConvertInfo Convert      (ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param);
		bool                IsSequence   (ExpressionBuilder builder, BuildInfo buildInfo);
	}
}
