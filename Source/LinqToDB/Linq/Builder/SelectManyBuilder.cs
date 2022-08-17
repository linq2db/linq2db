using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class SelectManyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable("SelectMany") &&
				methodCall.Arguments.Count == 3      &&
				((LambdaExpression)methodCall.Arguments[1].Unwrap()).Parameters.Count == 1;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence           = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var collectionSelector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var resultSelector     = (LambdaExpression)methodCall.Arguments[2].Unwrap();

			sequence = new SubQueryContext(sequence);

			var scopeContext = new ScopeContext(sequence, sequence);
			var expr         = SequenceHelper.PrepareBody(collectionSelector, scopeContext).Unwrap();

			// GroupJoin handling
			expr = builder.MakeExpression(expr, ProjectFlags.Expand);

			// correcting query for Eager Loading
			expr = SequenceHelper.MoveAllToScopedContext(expr, scopeContext);

			var collectionInfo = new BuildInfo(sequence, expr, new SelectQuery()) { CreateSubQuery = true };
			var collection     = builder.BuildSequence(collectionInfo);

			// DefaultIfEmptyContext wil handle correctly projecting NULL objects
			//
			if (collectionInfo.JoinType == JoinType.Full || collectionInfo.JoinType == JoinType.Right)
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, null);

			IBuildContext context = new SelectContext(buildInfo.Parent, resultSelector, buildInfo.IsSubQuery, sequence,
				collection);

			context.SetAlias(collectionSelector.Parameters[0].Name);

			string? collectionAlias = null;

			if (resultSelector.Parameters.Count > 1)
			{
				collectionAlias = resultSelector.Parameters[1].Name;
				collection.SetAlias(collectionAlias);
			}

			var isLeftJoin =
				SequenceHelper.IsDefaultIfEmpty(collection) ||
				collectionInfo.JoinType == JoinType.Left;

			var joinType = collectionInfo.JoinType;
			joinType = joinType switch
			{
				JoinType.Inner => isLeftJoin ? JoinType.OuterApply : JoinType.CrossApply,
				JoinType.Auto  => isLeftJoin ? JoinType.OuterApply : JoinType.CrossApply,
				JoinType.Left  => JoinType.OuterApply,
				JoinType.Full  => JoinType.FullApply,
				JoinType.Right => JoinType.RightApply,
				_ => joinType
			};

			var join = new SqlFromClause.Join(joinType, collection.SelectQuery, collectionAlias, false, null);
			context.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

			return context;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

	}
}
