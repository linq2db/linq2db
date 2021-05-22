﻿using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using SqlProvider;

	class ContextParser : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return buildInfo.Expression is MethodCallExpression call 
				&& call.Method.Name == "GetContext";
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var call = (MethodCallExpression)buildInfo.Expression;
			return new Context(builder.BuildSequence(new BuildInfo(buildInfo, call.Arguments[0])));
		}

		public SequenceConvertInfo? Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return builder.IsSequence(new BuildInfo(buildInfo, ((MethodCallExpression)buildInfo.Expression).Arguments[0]));
		}

		public class Context : PassThroughContext
		{
			public Context(IBuildContext context) : base(context)
			{
			}

			public ISqlOptimizer? SqlOptimizer;

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				query.DoNotCache = true;

				QueryRunner.SetNonQueryQuery(query);

				SqlOptimizer  = query.SqlOptimizer;

				query.GetElement = (db, expr, ps, preambles) => this;
			}
		}
	}
}
