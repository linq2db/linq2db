using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Join")]
	sealed class JoinBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			if (call.Method.DeclaringType == typeof(LinqExtensions) || !call.IsQueryable())
				return false;

			// other overload for Join
			if (call.Arguments[2].Unwrap() is not LambdaExpression lambda)
				return false;

			var body = lambda.Body.Unwrap();
			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;

				var throwExpr = 
					mi.NewExpression.Arguments.Count > 0
					|| mi.Bindings.Count == 0
					|| mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in join is not allowed.");
			}

			return true;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			outerContext = new SubQueryContext(outerContext);
			innerContext = new SubQueryContext(innerContext);

			List<SqlQueryExtension>? extensions = null;

			var jhc = SequenceHelper.GetJoinHintContext(innerContext);
			if (jhc != null)
			{
				innerContext = jhc.Context;
				extensions   = jhc.Extensions;
			}

			var join = innerContext.SelectQuery.InnerJoin();
			var sql  = outerContext.SelectQuery;

			if (extensions != null)
				join.JoinedTable.SqlQueryExtensions = extensions;

			var selector = methodCall.Arguments[4].UnwrapLambda();

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var outerKeyLambda = methodCall.Arguments[2].UnwrapLambda();
			var innerKeyLambda = methodCall.Arguments[3].UnwrapLambda();

			var innerKeyContext = innerContext;

			var outerKeySelector = SequenceHelper.PrepareBody(outerKeyLambda, outerContext).Unwrap();
			var innerKeySelector = SequenceHelper.PrepareBody(innerKeyLambda, innerKeyContext).Unwrap();

			if (!builder.TryGenerateComparison(outerContext, outerKeySelector, innerKeySelector, out var compareSearchCondition, out var error, BuildPurpose.Sql))
				return BuildSequenceResult.Error(error);

			outerKeySelector = builder.BuildSqlExpression(outerContext, outerKeySelector, BuildPurpose.Sql, BuildFlags.ForKeys);
			innerKeySelector = builder.BuildSqlExpression(outerContext, innerKeySelector, BuildPurpose.Sql, BuildFlags.ForKeys);

			sql.From.Tables[0].Joins.Add(join.JoinedTable);

			bool allowNullComparison = outerKeySelector is SqlGenericConstructorExpression ||
			                           innerKeySelector is SqlGenericConstructorExpression;

			if (!allowNullComparison && builder.CompareNulls is CompareNulls.LikeClr or CompareNulls.LikeSqlExceptParameters)
				compareSearchCondition = QueryHelper.CorrectComparisonForJoin(compareSearchCondition);

			join.JoinedTable.Condition = compareSearchCondition;

			var body = SequenceHelper.PrepareBody(selector, outerContext, new ScopeContext(innerContext, outerContext));

			return BuildSequenceResult.FromContext(new SelectContext(outerContext.TranslationModifier, buildInfo.Parent, builder, null, body, outerContext.SelectQuery, buildInfo.IsSubQuery)
#if DEBUG
				{
					Debug_MethodCall = methodCall
				}
#endif
				);
		}

	}
}
