﻿using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using Reflection;

	sealed class DistinctBuilder : MethodCallBuilder
	{
		static readonly MethodInfo[] _supportedMethods = { Methods.Queryable.Distinct, Methods.Enumerable.Distinct, Methods.LinqToDB.SelectDistinct };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsSameGenericMethod(_supportedMethods);
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult.BuildContext == null)
				return buildResult;
			var sequence = buildResult.BuildContext;

			var sql      = sequence.SelectQuery;
			if (sql.Select.TakeValue != null || sql.Select.SkipValue != null)
			{
				sequence = new SubQueryContext(sequence);
			}

			var subQueryContext = new SubQueryContext(sequence);

			subQueryContext.SelectQuery.Select.IsDistinct = true;

			var outerSubqueryContext = new SubQueryContext(subQueryContext);

			// We do not need all fields for SelectDistinct
			//
			if (methodCall.IsSameGenericMethod(Methods.LinqToDB.SelectDistinct))
			{
				subQueryContext.SelectQuery.Select.OptimizeDistinct = true;
			}
			else
			{
				// create all columns
				var sqlExpr = builder.BuildSqlExpression(outerSubqueryContext, new ContextRefExpression(methodCall.Method.GetGenericArguments()[0], subQueryContext), buildInfo.GetFlags());
				SequenceHelper.EnsureNoErrors(sqlExpr);
				sqlExpr = builder.UpdateNesting(outerSubqueryContext, sqlExpr);
			}

			return BuildSequenceResult.FromContext(new DistinctContext(outerSubqueryContext));
		}

		class DistinctContext : PassThroughContext
		{
			public DistinctContext(IBuildContext context) : base(context)
			{
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
					return path;

				var corrected = SequenceHelper.CorrectExpression(path, this, Context);

				if (flags.IsTable() || flags.IsTraverse() || flags.IsSubquery())
					return corrected;

				Expression result;
				if (flags.IsExtractProjection())
				{
					result = Builder.MakeExpression(Context, corrected, flags);
				}
				else
				{
					result = Builder.BuildSqlExpression(Context, corrected, flags.SqlFlag());
					result = Builder.UpdateNesting(Context, result);
				}

				return result;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DistinctContext(context.CloneContext(Context));
			}
		}
	}
}
