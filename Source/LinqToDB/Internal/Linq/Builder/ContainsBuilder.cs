using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Extensions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("Contains")]
	[BuildsMethodCall("ContainsAsync", CanBuildName = nameof(CanBuildAsyncMethod))]
	sealed class ContainsBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			return call.IsQueryable()
				&& call.Arguments.Count == 2
				// Contains over constant works through ConvertPredicate
				&& !builder.CanBeEvaluatedOnClient(call.Arguments[0]);
		}

		public static bool CanBuildAsyncMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
		{
			return call.IsAsyncExtension()
				&& call.Arguments.Count == 3
				// Contains over constant works through ConvertPredicate
				&& !builder.CanBeEvaluatedOnClient(call.Arguments[0]);
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var innerQuery = new SelectQuery();

			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], innerQuery));
			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = new SubQueryContext(buildResult.BuildContext);

			var containsContext = new ContainsContext(builder.GetTranslationModifier(), buildInfo.Parent, methodCall, buildInfo.SelectQuery, sequence);
			var placeholder     = containsContext.TryCreatePlaceholder();
			if (placeholder == null)
				return BuildSequenceResult.Error(methodCall, ErrorHelper.Error_Correlated_Subqueries);

			return BuildSequenceResult.FromContext(containsContext);
		}

		sealed class ContainsContext : BuildContextBase
		{
			public override Expression    Expression    { get; }
			public override MappingSchema MappingSchema => InnerSequence.MappingSchema;

			SelectQuery   OuterQuery    { get; }
			IBuildContext InnerSequence { get; }

			readonly MethodCallExpression _methodCall;

			public ContainsContext(TranslationModifier translationModifier, IBuildContext? parent, MethodCallExpression methodCall, SelectQuery outerQuery, IBuildContext innerSequence)
				: base(translationModifier, innerSequence.Builder, typeof(bool), outerQuery)
			{
				Parent            = parent;
				OuterQuery        = outerQuery;
				Expression        = methodCall;
				_methodCall       = methodCall;
				InnerSequence     = innerSequence;
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				return this;
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(OuterQuery);
			}

			static IEnumerable<(Expression, SqlPlaceholderExpression)> EnumerateAssignments(Expression currentPath, Expression expr)
			{
				if (expr is SqlGenericConstructorExpression generic)
				{
					foreach (var assignment in generic.Assignments)
					{
						var memberInfo = currentPath.Type.GetMemberEx(assignment.MemberInfo);
						if (memberInfo == null)
							continue;

						var newPath = Expression.MakeMemberAccess(currentPath, memberInfo);

						if (assignment.Expression is SqlPlaceholderExpression placeholder)
						{
							yield return (newPath, placeholder);
						}

						if (assignment.Expression is SqlGenericConstructorExpression subGeneric)
						{
							foreach (var sub in EnumerateAssignments(newPath, subGeneric))
								yield return sub;
						}
					}
				}
			}

			SqlPlaceholderExpression? _cachedPlaceholder;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				var placeholder = TryCreatePlaceholder();
				if (placeholder == null)
					return path;

				return placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var result = new ContainsContext(TranslationModifier, null, _methodCall, context.CloneElement(OuterQuery), context.CloneContext(InnerSequence));
				if (_cachedPlaceholder != null)
					result._cachedPlaceholder = context.CloneExpression(_cachedPlaceholder);
				return result;
			}

			public SqlPlaceholderExpression? TryCreatePlaceholder()
			{
				if (_cachedPlaceholder != null)
					return _cachedPlaceholder;

				_cachedPlaceholder = CreatePlaceholder();

				return _cachedPlaceholder;
			}

			public SqlPlaceholderExpression? CreatePlaceholder()
			{
				var args     = _methodCall.Method.GetGenericArguments();
				var param    = Expression.Parameter(args[0], "param");
				var expr     = _methodCall.Arguments[1];

				var placeholderContext = Parent ?? InnerSequence;

				var contextRef   = new ContextRefExpression(args[0], InnerSequence);
				var sequenceExpr = Builder.BuildSqlExpression(InnerSequence, contextRef, BuildPurpose.Sql, BuildFlags.ForKeys);

				var sequencePlaceholders = ExpressionBuilder.CollectPlaceholders(sequenceExpr, false);
				if (sequencePlaceholders.Count == 0)
				{
					//TODO: better error handling
					return null;
				}

				var testExpr         = Builder.BuildSqlExpression(placeholderContext, expr, BuildPurpose.Sql, BuildFlags.ForKeys);
				var testPlaceholders = ExpressionBuilder.CollectPlaceholders(testExpr, false);

				ISqlPredicate predicate;

				var placeholderQuery = OuterQuery;

				if (Parent != null)
					placeholderQuery = Parent.SelectQuery;

				var useExists = testPlaceholders.Count != 1;

				if (useExists && testPlaceholders.Count == 0)
				{
					var availableComparisons = EnumerateAssignments(expr, sequenceExpr).Take(2).ToList();
					if (availableComparisons.Count == 1)
					{
						testExpr  = Builder.BuildSqlExpression(placeholderContext, availableComparisons[0].Item1, BuildPurpose.Sql, BuildFlags.ForKeys);
						if (testExpr is SqlPlaceholderExpression placeholder)
						{
							testPlaceholders.Add(placeholder);
							useExists = false;
						}
					}
				}

				if (useExists)
				{
					if (Builder.DataContext.SqlProviderFlags.SupportedCorrelatedSubqueriesLevel == 0)
					{
						return null;
					}

					var condition = Expression.Lambda(ExpressionBuilder.Equal(MappingSchema, param, expr), param);
					var sequence = Builder.BuildWhere(Parent, InnerSequence,
						condition : condition, checkForSubQuery : true, enforceHaving : false, out var error);

					if (sequence == null)
						return null;

					predicate = new SqlPredicate.Exists(false, sequence.SelectQuery);
				}
				else
				{
					var columns = Builder.ToColumns(InnerSequence, sequenceExpr);

					var testPlaceholder = testPlaceholders[0];
					testPlaceholder = Builder.UpdateNesting(placeholderContext, testPlaceholder);

					var inExpr = testPlaceholder.Sql;

					predicate = new SqlPredicate.InSubQuery(inExpr, false, InnerSequence.SelectQuery, false);
				}

				var subQuerySql = new SqlSearchCondition(false, predicate);

				return ExpressionBuilder.CreatePlaceholder(placeholderQuery, subQuerySql, _methodCall, convertType: typeof(bool));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);
				QueryRunner.SetRunQuery(query, mapper);
			}

			public override bool IsSingleElement => true;
		}
	}
}
