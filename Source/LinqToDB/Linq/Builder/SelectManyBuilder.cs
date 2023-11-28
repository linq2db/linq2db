using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class SelectManyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("SelectMany");
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var genericArguments = methodCall.Method.GetGenericArguments();

			var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (sequence == null)
				return null;

			var collectionSelector = SequenceHelper.GetArgumentLambda(methodCall, "collectionSelector") ??
			                         SequenceHelper.GetArgumentLambda(methodCall, "selector");

			if (collectionSelector == null)
			{
				var param            = Expression.Parameter(genericArguments[0], "source");
				collectionSelector   = Expression.Lambda(Expression.Convert(param, typeof(IEnumerable<>).MakeGenericType(genericArguments[1])), param);
			}

			var selector       = SequenceHelper.GetArgumentLambda(methodCall, "selector");
			var resultSelector = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");

			sequence = new SubQueryContext(sequence);

			var expr = SequenceHelper.PrepareBody(collectionSelector, sequence).Unwrap();

			BuildInfo      collectionInfo;
			IBuildContext? collection;
			Expression     resultExpression;
			Expression     projected;

			// GroupJoin handling
			expr = builder.UpdateNesting(sequence, expr);

			var collectionSelectQuery = new SelectQuery();
			collectionInfo = new BuildInfo(sequence, expr, collectionSelectQuery)
			{
				CreateSubQuery    = true,
				SourceCardinality = SourceCardinality.OneOrMany
			};

			collection = builder.TryBuildSequence(collectionInfo);

			if (collection == null)
				return null;

			// DefaultIfEmptyContext wil handle correctly projecting NULL objects
			//
			if (collectionInfo.JoinType == JoinType.Full || collectionInfo.JoinType == JoinType.Right)
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, collection, null, false);
			}

			projected = builder.BuildSqlExpression(collection,
				new ContextRefExpression(collection.ElementType, collection), buildInfo.GetFlags(),
				buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);

			collection = new SubQueryContext(collection);

			projected = builder.UpdateNesting(collection, projected);

			if (resultSelector == null)
			{
				resultExpression = projected;
			}
			else
			{
				resultExpression = SequenceHelper.ReplaceBody(resultSelector.Body, resultSelector.Parameters[0], sequence);
				if (resultSelector.Parameters.Count > 1)
				{
					resultExpression = SequenceHelper.ReplaceBody(resultExpression, resultSelector.Parameters[1], new ScopeContext(collection, sequence));
				}
			}

			var context = new SelectContext(buildInfo.Parent, builder, resultSelector == null ? collection : null, resultExpression, sequence.SelectQuery, buildInfo.IsSubQuery);
			context.SetAlias(collectionSelector.Parameters[0].Name);

			string? collectionAlias = null;

			if (resultSelector?.Parameters.Count > 1)
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
			sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

			if (buildInfo.Parent == null && !SequenceHelper.IsSupportedSubqueryForModifier(sequence, collection))
				return null;

			return context;
		}
	}
}
