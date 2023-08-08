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

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var genericArguments = methodCall.Method.GetGenericArguments();

			var sequence         = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			var collectionSelector = SequenceHelper.GetArgumentLambda(methodCall, "collectionSelector") ??
			                         SequenceHelper.GetArgumentLambda(methodCall, "selector");

			if (collectionSelector == null)
			{
				var param            = Expression.Parameter(genericArguments[0], "source");
				collectionSelector   = Expression.Lambda(Expression.Convert(param, typeof(IEnumerable<>).MakeGenericType(genericArguments[1])), param);
			}

			var selector       = SequenceHelper.GetArgumentLambda(methodCall, "selector");
			var resultSelector = SequenceHelper.GetArgumentLambda(methodCall, "resultSelector");

			var expr = SequenceHelper.PrepareBody(collectionSelector, sequence).Unwrap();

			sequence = new SubQueryContext(sequence);

			var scopeContext = new ScopeContext(sequence, sequence);
			// correcting query for Eager Loading
			expr = SequenceHelper.MoveAllToScopedContext(expr, scopeContext);

			expr = builder.UpdateNesting(scopeContext, expr);

			// GroupJoin handling
			expr = builder.MakeExpression(scopeContext, expr, ProjectFlags.ExtractProjection);


			var collectionSelectQuery    = new SelectQuery();
			var collectionInfo = new BuildInfo(sequence, expr, collectionSelectQuery) { CreateSubQuery = true };


			var fakejoin = new SqlFromClause.Join(JoinType.Auto, collectionSelectQuery, null, false, null);
			sequence.SelectQuery.From.Tables[0].Joins.Add(fakejoin.JoinedTable);

			var collection     = builder.BuildSequence(collectionInfo);

			sequence.SelectQuery.From.Tables[0].Joins.Remove(fakejoin.JoinedTable);

			// DefaultIfEmptyContext wil handle correctly projecting NULL objects
			//
			if (collectionInfo.JoinType == JoinType.Full || collectionInfo.JoinType == JoinType.Right)
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, null, false);
			}

			Expression resultExpression;

			if (resultSelector == null)
			{
				resultExpression = new ContextRefExpression(genericArguments[^1], collection);
			}
			else 
			{
				resultExpression = SequenceHelper.ReplaceBody(resultSelector.Body, resultSelector.Parameters[0], sequence);
				if (resultSelector.Parameters.Count > 1)
				{
					resultExpression = SequenceHelper.ReplaceBody(resultExpression, resultSelector.Parameters[1], collection);
				}
			}

			var context = new SelectContext(buildInfo.Parent, resultExpression, collection, sequence.SelectQuery, buildInfo.IsSubQuery);
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
			context.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

			return context;
		}
	}
}
