using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class JoinBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (methodCall.Method.DeclaringType == typeof(LinqExtensions) || !methodCall.IsQueryable("Join"))
				return false;

			// other overload for Join
			if (methodCall.Arguments[2].Unwrap() is not LambdaExpression lambda)
				return false;

			var body = lambda.Body.Unwrap();

			if (body.NodeType == ExpressionType.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					ThrowHelper.ThrowNotSupportedException($"Explicit construction of entity type '{body.Type}' in join is not allowed.");
			}

			return true;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
			var innerContext = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));

			outerContext = new SubQueryContext(outerContext);
			innerContext = new SubQueryContext(innerContext);

			List<SqlQueryExtension>? extensions = null;

			if (innerContext is QueryExtensionBuilder.JoinHintContext jhc)
			{
				innerContext = jhc.Context;
				extensions   = jhc.Extensions;
			}

			var join = innerContext.SelectQuery.InnerJoin();
			var sql  = outerContext.SelectQuery;

			sql.From.Tables[0].Joins.Add(join.JoinedTable);

			if (extensions != null)
				join.JoinedTable.SqlQueryExtensions = extensions;

			var selector = (LambdaExpression)methodCall.Arguments[4].Unwrap();

			outerContext.     SetAlias(selector.Parameters[0].Name);
			innerContext.SetAlias(selector.Parameters[1].Name);

			var outerKeyLambda = (LambdaExpression)methodCall.Arguments[2].Unwrap();
			var innerKeyLambda = (LambdaExpression)methodCall.Arguments[3].Unwrap();

			var innerKeyContext  = innerContext;

			var outerKeySelector = SequenceHelper.PrepareBody(outerKeyLambda, outerContext).Unwrap();
			var innerKeySelector = SequenceHelper.PrepareBody(innerKeyLambda, innerKeyContext).Unwrap();

			// Make join and where for the counter.
			//
			if (outerKeySelector.NodeType == ExpressionType.New)
			{
				var new1 = (NewExpression)outerKeySelector;
				var new2 = (NewExpression)innerKeySelector;

				for (var i = 0; i < new1.Arguments.Count; i++)
				{
					var arg1 = new1.Arguments[i];
					var arg2 = new2.Arguments[i];

					BuildJoin(builder, join.JoinedTable.Condition, outerContext, arg1, innerKeyContext, arg2);
				}
			}
			else if (outerKeySelector.NodeType == ExpressionType.MemberInit)
			{
				var mi1 = (MemberInitExpression)outerKeySelector;
				var mi2 = (MemberInitExpression)innerKeySelector;

				for (var i = 0; i < mi1.Bindings.Count; i++)
				{
					if (mi1.Bindings[i].Member != mi2.Bindings[i].Member)
						ThrowHelper.ThrowLinqException($"List of member inits does not match for entity type '{outerKeySelector.Type}'.");

					var arg1 = ((MemberAssignment)mi1.Bindings[i]).Expression;
					var arg2 = ((MemberAssignment)mi2.Bindings[i]).Expression;

					BuildJoin(builder, join.JoinedTable.Condition, outerContext, arg1, innerKeyContext, arg2);
				}
			}
			else
			{
				BuildJoin(builder, join.JoinedTable.Condition, outerContext, outerKeySelector, innerKeyContext, innerKeySelector);
			}

			return new SelectContext(buildInfo.Parent, selector, buildInfo.IsSubQuery, outerContext, innerContext)
#if DEBUG
			{
				Debug_MethodCall = methodCall
			}
#endif
				;
		}

		internal static void BuildJoin(
			ExpressionBuilder builder,
			SqlSearchCondition condition,
			IBuildContext outerKeyContext, Expression outerKeySelector,
			IBuildContext innerKeyContext, Expression innerKeySelector)
		{
			var predicate = builder.ConvertCompare(outerKeyContext,
				ExpressionType.Equal,
				outerKeySelector, 
				innerKeySelector, ProjectFlags.SQL);

			if (predicate == null)
			{
				predicate = new SqlPredicate.ExprExpr(
					builder.ConvertToSql(outerKeyContext, outerKeySelector),
					SqlPredicate.Operator.Equal,
					builder.ConvertToSql(innerKeyContext, innerKeySelector),
					Common.Configuration.Linq.CompareNullsAsValues ? true : null);
			}

			condition.Conditions.Add(new SqlCondition(false, predicate));
		}

	}
}
