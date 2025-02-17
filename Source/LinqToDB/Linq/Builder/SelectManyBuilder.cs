using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[BuildsMethodCall("SelectMany")]
	sealed class SelectManyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var genericArguments = methodCall.Method.GetGenericArguments();

			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

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

			Expression     resultExpression;

			// GroupJoin handling
			expr = builder.UpdateNesting(sequence, expr);

			var collectionSelectQuery = new SelectQuery();
			var collectionInfo = new BuildInfo(sequence, expr, collectionSelectQuery)
			{
				CreateSubQuery    = true,
				SourceCardinality = SourceCardinality.Many
			};

			using var snapshot = builder.CreateSnapshot();

			var collectionResult = builder.TryBuildSequence(collectionInfo);

			if (collectionResult.BuildContext == null)
				return collectionResult;

			var originalCollection = collectionResult.BuildContext;

			var collection = originalCollection;

			// DefaultIfEmptyContext wil handle correctly projecting NULL objects
			//
			if (collectionInfo.JoinType == JoinType.Full || collectionInfo.JoinType == JoinType.Right)
			{
				sequence = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					sequence,
					collection,
					defaultValue: null,
					allowNullField: false,
					isNullValidationDisabled: false);
			}

			var collectionDefaultIfEmptyContext = SequenceHelper.GetDefaultIfEmptyContext(collection);
			if (collectionDefaultIfEmptyContext != null)
			{
				collectionDefaultIfEmptyContext.IsNullValidationDisabled = true;
			}

			var isLeftJoin =
				collectionDefaultIfEmptyContext != null ||
				collectionInfo.JoinType         == JoinType.Left;

			var joinType = collectionInfo.JoinType;
			joinType = joinType switch
			{
				JoinType.Inner => isLeftJoin ? JoinType.OuterApply : JoinType.CrossApply,
				JoinType.Auto => isLeftJoin ? JoinType.OuterApply : JoinType.CrossApply,
				JoinType.Left => JoinType.OuterApply,
				JoinType.Full => JoinType.FullApply,
				JoinType.Right => JoinType.RightApply,
				_ => joinType
			};

			var expanded = builder.BuildExtractExpression(collection, new ContextRefExpression(collection.ElementType, collection));

			collection = new SubQueryContext(collection);

			if (collectionDefaultIfEmptyContext != null)
			{
				var collectionSelectContext = new SelectContext(sequence.TranslationModifier, buildInfo.Parent, builder, null, expanded, collection.SelectQuery, buildInfo.IsSubQuery);

				collection = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					sequence,
					collectionSelectContext,
					collection,
					defaultValue: collectionDefaultIfEmptyContext.DefaultValue,
					allowNullField: joinType is not (JoinType.Right or JoinType.RightApply or JoinType.Full or JoinType.FullApply),
					isNullValidationDisabled: false);
			}

			if (resultSelector == null)
			{
				resultExpression = expanded;
			}
			else
			{
				resultExpression = SequenceHelper.ReplaceBody(resultSelector.Body, resultSelector.Parameters[0], sequence);
				if (resultSelector.Parameters.Count > 1)
				{
					resultExpression = SequenceHelper.ReplaceBody(resultExpression, resultSelector.Parameters[1], collection);
				}
			}

			var context = new SelectContext(sequence.TranslationModifier, buildInfo.Parent, builder, resultSelector == null ? collection : null, resultExpression, sequence.SelectQuery, buildInfo.IsSubQuery);
			context.SetAlias(collectionSelector.Parameters[0].Name);

			string? collectionAlias = null;

			if (resultSelector?.Parameters.Count > 1)
			{
				collectionAlias = resultSelector.Parameters[1].Name;
				collection.SetAlias(collectionAlias);
			}

			var join = new SqlFromClause.Join(joinType, collection.SelectQuery, collectionAlias, false, null);
			sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

			var jhc = SequenceHelper.GetJoinHintContext(originalCollection);
			if (jhc != null)
			{
				join.JoinedTable.SqlQueryExtensions = jhc.Extensions;
			}

			if (buildInfo.Parent == null && !builder.IsSupportedSubquery(sequence, collection, out var errorMessage))
			{
				collection.Detach();
				return BuildSequenceResult.Error(methodCall, errorMessage);
			}

			snapshot.Accept();

			return BuildSequenceResult.FromContext(context);
		}
	}
}
