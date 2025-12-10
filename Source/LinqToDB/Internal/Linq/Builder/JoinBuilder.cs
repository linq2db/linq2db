using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Enumerable.Join))]
#if NET10_0_OR_GREATER
	[BuildsMethodCall(nameof(Enumerable.LeftJoin))]
	[BuildsMethodCall(nameof(Enumerable.RightJoin))]
#endif
	sealed class JoinBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
		{
			if (call.Method.DeclaringType == typeof(LinqExtensions) || !call.IsQueryable())
				return false;

			if (call.Arguments.Count != 5)
				return false;

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

#if NET10_0_OR_GREATER
			var joinType = methodCall.Method.Name switch
			{
				nameof(Enumerable.LeftJoin)  => JoinType.Left,
				nameof(Enumerable.RightJoin) => JoinType.Right,
				_                            => JoinType.Inner,
			};

			if (joinType is JoinType.Right)
			{
				outerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					outerContext,
					outerContext,
					defaultValue: null,
					isNullValidationDisabled: false);

				outerContext = new SubQueryContext(outerContext);
			}

			if (joinType is JoinType.Left)
			{
				innerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					innerContext,
					innerContext,
					defaultValue: null,
					isNullValidationDisabled: false);

				innerContext = new SubQueryContext(innerContext);
			}
#else
			var joinType = JoinType.Inner;
#endif

			var outerKeyLambda = methodCall.Arguments[2].UnwrapLambda();
			var innerKeyLambda = methodCall.Arguments[3].UnwrapLambda();

			var outerBody = outerKeyLambda.Body.Unwrap();
			if (outerBody.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)outerBody;

				var throwExpr =
					mi.NewExpression.Arguments.Count > 0
					|| mi.Bindings.Count             == 0
					|| mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					return BuildSequenceResult.Error(outerKeyLambda, $"Explicit construction of entity type '{outerBody.Type}' in join is not allowed.");
			}

			var join = new SqlJoinedTable(joinType, innerContext.SelectQuery, null, false);
			var sql  = outerContext.SelectQuery;

			if (extensions != null)
				join.SqlQueryExtensions = extensions;

			var selector = methodCall.Arguments[4].UnwrapLambda();

			outerContext.SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var innerKeyContext = innerContext;

			var outerKeySelector = SequenceHelper.PrepareBody(outerKeyLambda, outerContext).Unwrap();
			var innerKeySelector = SequenceHelper.PrepareBody(innerKeyLambda, innerKeyContext).Unwrap();

			if (!builder.TryGenerateComparison(outerContext, outerKeySelector, innerKeySelector, out var compareSearchCondition, out var error, BuildPurpose.Sql))
				return BuildSequenceResult.Error(error);

			outerKeySelector = builder.BuildSqlExpression(outerContext, outerKeySelector, BuildPurpose.Sql, BuildFlags.ForKeys);
			innerKeySelector = builder.BuildSqlExpression(outerContext, innerKeySelector, BuildPurpose.Sql, BuildFlags.ForKeys);

			sql.From.Tables[0].Joins.Add(join);

			bool allowNullComparison = outerKeySelector is SqlGenericConstructorExpression ||
			                           innerKeySelector is SqlGenericConstructorExpression;

			if (!allowNullComparison && builder.CompareNulls is CompareNulls.LikeClr or CompareNulls.LikeSqlExceptParameters)
				compareSearchCondition = QueryHelper.CorrectComparisonForJoin(compareSearchCondition);

			join.Condition = compareSearchCondition;

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
