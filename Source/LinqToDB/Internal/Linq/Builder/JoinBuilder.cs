using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(Enumerable.Join))]
#if NET10_0_OR_GREATER
	[BuildsMethodCall(nameof(Enumerable.LeftJoin))]
	[BuildsMethodCall(nameof(Enumerable.RightJoin))]
#endif
#if NET11_0_OR_GREATER
	[BuildsMethodCall(nameof(Enumerable.FullJoin))]
#endif
	sealed class JoinBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
		{
			if (call.Method.DeclaringType == typeof(LinqExtensions) || !call.IsQueryable)
				return false;

			// outer, inner, two key selectors, the result selector (absent on the tuple-returning overloads), then an optional
			// comparer, which translates only when null, i.e. the default one
			var selectorCount = IsTupleJoin(call) ? 0 : 1;
			var arguments     = call.Arguments;

			if (arguments.Count == 5 + selectorCount)
			{
				if (arguments[4 + selectorCount] is not ConstantExpression { Value: null })
					return false;
			}
			else if (arguments.Count != 4 + selectorCount)
				return false;

			return selectorCount == 0 || arguments[4].Unwrap() is LambdaExpression;
		}

		// .NET 11 Join<TOuter, TInner, TKey>(outer, inner, outerKeySelector, innerKeySelector, comparer) and its LeftJoin /
		// RightJoin / FullJoin siblings return (TOuter Outer, TInner Inner) instead of taking a result selector. An [IsQueryable]
		// method of the same name can have three generic arguments too, so the declaring type is part of the check.
		static bool IsTupleJoin(MethodCallExpression call)
			=> call.Method.IsGenericMethod
			&& call.Method.GetGenericArguments().Length == 3
			&& (call.Method.DeclaringType == typeof(Queryable) || call.Method.DeclaringType == typeof(Enumerable));

		static LambdaExpression BuildTupleSelector(MethodCallExpression methodCall, LambdaExpression outerKeyLambda, LambdaExpression innerKeyLambda)
		{
			var types     = methodCall.Method.GetGenericArguments();
			var tupleType = typeof(ValueTuple<,>).MakeGenericType(types[0], types[1]);
			var outer     = Expression.Parameter(types[0], outerKeyLambda.Parameters[0].Name);
			var inner     = Expression.Parameter(types[1], innerKeyLambda.Parameters[0].Name);

			return Expression.Lambda(
				Expression.New(
					tupleType.GetConstructorEx(types[0], types[1])!,
					[outer, inner],
					tupleType.GetField(nameof(ValueTuple<,>.Item1))!,
					tupleType.GetField(nameof(ValueTuple<,>.Item2))!),
				outer,
				inner);
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var outerContextResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], buildInfo.SelectQuery));
			if (outerContextResult.BuildContext == null)
				return outerContextResult;

			var innerContextResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[1], new SelectQuery()));
			if (innerContextResult.BuildContext == null)
				return innerContextResult;

			var outerContext = outerContextResult.BuildContext;
			var innerContext = innerContextResult.BuildContext;

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
				nameof(Enumerable.LeftJoin)      => JoinType.Left,
				nameof(Enumerable.RightJoin)     => JoinType.Right,
#if NET11_0_OR_GREATER
				nameof(Enumerable.FullJoin)      => JoinType.Full,
#endif
				_                                => JoinType.Inner,
			};

			if (joinType is JoinType.Right or JoinType.Full)
			{
				outerContext = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(
					buildInfo.Parent,
					outerContext,
					outerContext,
					defaultValue: null,
					isNullValidationDisabled: false);

				outerContext = new SubQueryContext(outerContext);
			}

			if (joinType is JoinType.Left or JoinType.Full)
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

			var selector = IsTupleJoin(methodCall)
				? BuildTupleSelector(methodCall, outerKeyLambda, innerKeyLambda)
				: methodCall.Arguments[4].UnwrapLambda();

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
				Debug_MethodCall = methodCall,
			}
#endif
				);
		}

	}
}
