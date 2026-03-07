#if NET8_0_OR_GREATER

using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Queryable.ExceptBy), nameof(Queryable.UnionBy), nameof(Queryable.IntersectBy))]
	sealed class SetOperationByBuilder : MethodCallBuilder
	{
		sealed class UnionByTuple<T>
		{
#pragma warning disable CS8618
			public T Data { get; set; }
			public long RowNumber { get; set; }
			public int SourceIndex { get; set; }
#pragma warning restore CS8618
		}

		public static bool CanBuildMethod(MethodCallExpression call) 
			=> call.IsQueryable() && call.Arguments.Count == 3;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!builder.DataContext.SqlProviderFlags.IsWindowFunctionsSupported)
			{
				return BuildSequenceResult.Error(methodCall, ErrorHelper.Error_RowNumber);
			}

			var sourceType = methodCall.Method.GetGenericArguments()[0];
			var keyType    = methodCall.Method.GetGenericArguments()[1];

			var sourceExpression = methodCall.Arguments[0];
			var secondExpression = methodCall.Arguments[1];
			var keySelector      = methodCall.Arguments[2].UnwrapLambda();

			var transformedExpression = methodCall.Method.Name switch
			{
				nameof(Queryable.ExceptBy)    => BuildExceptBy   (sourceExpression, secondExpression, keySelector, sourceType, keyType),
				nameof(Queryable.IntersectBy) => BuildIntersectBy(sourceExpression, secondExpression, keySelector, sourceType, keyType),
				_                             => BuildUnionBy    (sourceExpression, secondExpression, keySelector, sourceType),
			};

			return builder.TryBuildSequence(new BuildInfo(buildInfo, transformedExpression));
		}

		static Expression BuildExceptBy(Expression source, Expression second, LambdaExpression keySelector, Type sourceType, Type keyType)
		{
			source = BuildExpressionUtils.EnsureQueryable(source, sourceType);
			second = BuildExpressionUtils.EnsureQueryable(second, keyType);

			var distinctMethod = Methods.Queryable.Distinct.MakeGenericMethod(keyType);
			var distinctKeys   = Expression.Call(null, distinctMethod, second);

			var parameter       = Expression.Parameter(sourceType, "e");
			var keySelectorBody = keySelector.GetBody(parameter);

			var containsMethod = Methods.Queryable.Contains.MakeGenericMethod(keyType);
			var containsCall   = Expression.Call(null, containsMethod, distinctKeys, keySelectorBody);
			var notContains    = Expression.Not(containsCall);

			var whereLambda = Expression.Lambda(notContains, parameter);
			var whereMethod = Methods.Queryable.Where.MakeGenericMethod(sourceType);
			var filtered = Expression.Call(null, whereMethod, source, Expression.Quote(whereLambda));

			var itemType = typeof(UnionByTuple<>).MakeGenericType(sourceType);

			var partitionPart = ExpressionHelpers.CollectMembers(keySelectorBody).ToArray();
			if (partitionPart.Length == 0)
				partitionPart = [Expression.Constant(1)];

			var orderByPart = BuildOrderByPart(source, parameter);
			if (orderByPart.Length == 0)
				orderByPart = partitionPart.Select(p => (p, false)).ToArray();

			var rnCall = WindowFunctionHelpers.BuildRowNumber(partitionPart, orderByPart);

			var selectBody = Expression.MemberInit(
				Expression.New(itemType),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.Data))!, parameter),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.RowNumber))!, rnCall));

			var selectLambda = Expression.Lambda(selectBody, parameter);
			var selectCall   = Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(sourceType, itemType),
				filtered,
				Expression.Quote(selectLambda));

			var tupleParam = Expression.Parameter(itemType, "e");
			var rnFilter   = Expression.Equal(
				Expression.PropertyOrField(tupleParam, nameof(UnionByTuple<>.RowNumber)),
				Expression.Constant(1L));

			var whereRnCall = Expression.Call(
				null,
				Methods.Queryable.Where.MakeGenericMethod(itemType),
				selectCall,
				Expression.Quote(Expression.Lambda(rnFilter, tupleParam)));

			var resultParam = Expression.Parameter(itemType, "e");
			var resultBody  = Expression.PropertyOrField(resultParam, nameof(UnionByTuple<>.Data));

			return Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(itemType, sourceType),
				whereRnCall,
				Expression.Quote(Expression.Lambda(resultBody, resultParam)));
		}

		static Expression BuildIntersectBy(Expression source, Expression second, LambdaExpression keySelector, Type sourceType, Type keyType)
		{
			source = BuildExpressionUtils.EnsureQueryable(source, sourceType);
			second = BuildExpressionUtils.EnsureQueryable(second, keyType);

			var distinctMethod = Methods.Queryable.Distinct.MakeGenericMethod(keyType);
			var distinctKeys   = Expression.Call(null, distinctMethod, second);

			var parameter       = Expression.Parameter(sourceType, "e");
			var keySelectorBody = keySelector.GetBody(parameter);

			var containsMethod = Methods.Queryable.Contains.MakeGenericMethod(keyType);
			var containsCall   = Expression.Call(null, containsMethod, distinctKeys, keySelectorBody);

			var whereLambda = Expression.Lambda(containsCall, parameter);
			var whereMethod = Methods.Queryable.Where.MakeGenericMethod(sourceType);
			var filtered = Expression.Call(null, whereMethod, source, Expression.Quote(whereLambda));

			var itemType = typeof(UnionByTuple<>).MakeGenericType(sourceType);

			var partitionPart = ExpressionHelpers.CollectMembers(keySelectorBody).ToArray();
			if (partitionPart.Length == 0)
				partitionPart = [ExpressionInstances.Constant1];

			var orderByPart = BuildOrderByPart(source, parameter);
			if (orderByPart.Length == 0)
				orderByPart = partitionPart.Select(p => (p, false)).ToArray();

			var rnCall = WindowFunctionHelpers.BuildRowNumber(partitionPart, orderByPart);

			var selectBody = Expression.MemberInit(
				Expression.New(itemType),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.Data))!, parameter),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.RowNumber))!, rnCall));

			var selectLambda = Expression.Lambda(selectBody, parameter);
			var selectCall   = Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(sourceType, itemType),
				filtered,
				Expression.Quote(selectLambda));

			var tupleParam = Expression.Parameter(itemType, "e");
			var rnFilter   = Expression.Equal(
				Expression.PropertyOrField(tupleParam, nameof(UnionByTuple<>.RowNumber)),
				Expression.Constant(1L));

			var whereRnCall = Expression.Call(
				null,
				Methods.Queryable.Where.MakeGenericMethod(itemType),
				selectCall,
				Expression.Quote(Expression.Lambda(rnFilter, tupleParam)));

			var resultParam = Expression.Parameter(itemType, "e");
			var resultBody  = Expression.PropertyOrField(resultParam, nameof(UnionByTuple<>.Data));

			return Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(itemType, sourceType),
				whereRnCall,
				Expression.Quote(Expression.Lambda(resultBody, resultParam)));
		}

		static Expression BuildUnionBy(Expression source, Expression second, LambdaExpression keySelector, Type sourceType)
		{
			source = BuildExpressionUtils.EnsureQueryable(source, sourceType);
			second = BuildExpressionUtils.EnsureQueryable(second, sourceType);

			var itemType = typeof(UnionByTuple<>).MakeGenericType(sourceType);

			var sourceItem = Expression.Parameter(sourceType, "e");
			var sourceSelectBody = Expression.MemberInit(
				Expression.New(itemType),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.Data))!, sourceItem),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.SourceIndex))!, Expression.Constant(0)));

			var sourceProjected = Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(sourceType, itemType),
				source,
				Expression.Quote(Expression.Lambda(sourceSelectBody, sourceItem)));

			var secondItem = Expression.Parameter(sourceType, "e");
			var secondSelectBody = Expression.MemberInit(
				Expression.New(itemType),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.Data))!,        secondItem),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.SourceIndex))!, Expression.Constant(1)));

			var secondProjected = Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(sourceType, itemType),
				second,
				Expression.Quote(Expression.Lambda(secondSelectBody, secondItem)));

			var concatMethod = Methods.Queryable.Concat.MakeGenericMethod(itemType);
			var concatenated = Expression.Call(concatMethod, sourceProjected, secondProjected);

			var rowItem  = Expression.Parameter(itemType, "e");
			var dataBody = Expression.PropertyOrField(rowItem, nameof(UnionByTuple<>.Data));
			var keyBody  = keySelector.GetBody(dataBody);

			var partitionPart = ExpressionHelpers.CollectMembers(keyBody).ToArray();
			if (partitionPart.Length == 0)
				partitionPart = [ExpressionInstances.Constant1];

			var sourceOrderBy = BuildOrderByPart(source, dataBody);
			var orderByList = new System.Collections.Generic.List<(Expression expr, bool isDescending)>
			{
				(Expression.PropertyOrField(rowItem, nameof(UnionByTuple<>.SourceIndex)), false),
			};

			if (sourceOrderBy.Length == 0)
				orderByList.AddRange(partitionPart.Select(p => (p, false)));
			else
				orderByList.AddRange(sourceOrderBy);

			var orderByPart = orderByList.ToArray();

			var rnCall = WindowFunctionHelpers.BuildRowNumber(partitionPart, orderByPart);

			var selectBody = Expression.MemberInit(
				Expression.New(itemType),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.Data))!, dataBody),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.SourceIndex))!, Expression.PropertyOrField(rowItem, nameof(UnionByTuple<>.SourceIndex))),
				Expression.Bind(itemType.GetProperty(nameof(UnionByTuple<>.RowNumber))!, rnCall));

			var selectLambda = Expression.Lambda(selectBody, rowItem);
			var selectCall   = Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(itemType, itemType),
				concatenated,
				Expression.Quote(selectLambda));

			var tupleParam = Expression.Parameter(itemType, "e");
			var whereBody  = Expression.Equal(
				Expression.PropertyOrField(tupleParam, nameof(UnionByTuple<>.RowNumber)),
				Expression.Constant(1L));

			var whereCall = Expression.Call(
				null,
				Methods.Queryable.Where.MakeGenericMethod(itemType),
				selectCall,
				Expression.Quote(Expression.Lambda(whereBody, tupleParam)));

			var resultParam = Expression.Parameter(itemType, "e");
			var resultBody  = Expression.PropertyOrField(resultParam, nameof(UnionByTuple<>.Data));

			return Expression.Call(
				null,
				Methods.Queryable.Select.MakeGenericMethod(itemType, sourceType),
				whereCall,
				Expression.Quote(Expression.Lambda(resultBody, resultParam)));
		}

		static (Expression expr, bool isDescending)[] BuildOrderByPart(Expression source, Expression parameter)
		{
			var orderByPart = WindowFunctionHelpers.ExtractOrderByPart(source, out _);
			if (orderByPart.Length == 0)
				return [];

			return orderByPart
				.Select(o => (o.lambda.GetBody(parameter), o.isDescending))
				.ToArray();
		}
	}
}

#endif
