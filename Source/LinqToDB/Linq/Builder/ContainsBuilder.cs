using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Mapping;
	using SqlQuery;
	using Extensions;
	using LinqToDB.Expressions;

	sealed class ContainsBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames      = { "Contains"      };
		static readonly string[] MethodNamesAsync = { "ContainsAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var result =
				methodCall.IsQueryable     (MethodNames     ) && methodCall.Arguments.Count == 2 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count == 3;

			if (result)
			{
				// Contains over constant works through ConvertPredicate
				if (builder.CanBeCompiled(methodCall.Arguments[0], false))
					result = false;
			}

			return result;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var innerQuery = new SelectQuery();

			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], innerQuery));
			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = new SubQueryContext(buildResult.BuildContext);

			var containsContext = new ContainsContext(buildInfo.Parent, methodCall, buildInfo.SelectQuery, sequence);
			var placeholder     = containsContext.CreatePlaceholder(ProjectFlags.SQL);
			if (placeholder == null)
				return BuildSequenceResult.Error(methodCall, "Provider does not support correlated subqueries.");

			return BuildSequenceResult.FromContext(containsContext);
		}

		public static bool IsConstant(MethodCallExpression methodCall)
		{
			if (!methodCall.IsQueryable("Contains"))
				return false;

			return methodCall.IsQueryable(false) == false;
		}

		sealed class ContainsContext : BuildContextBase
		{
			public override Expression    Expression    { get; }
			public override MappingSchema MappingSchema => InnerSequence.MappingSchema;

			SelectQuery   OuterQuery    { get; }
			IBuildContext InnerSequence { get; }

			readonly MethodCallExpression _methodCall;

			public ContainsContext(IBuildContext? parent, MethodCallExpression methodCall, SelectQuery outerQuery, IBuildContext innerSequence)
				:base(innerSequence.Builder, typeof(bool), outerQuery)
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
				if (_cachedPlaceholder != null)
					return _cachedPlaceholder;

				var placeholder = CreatePlaceholder(flags.SqlFlag());
				if (placeholder == null)
					return path;

				if (!flags.IsTest())
					_cachedPlaceholder = placeholder;

				return placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				var result = new ContainsContext(null, _methodCall, context.CloneElement(OuterQuery), context.CloneContext(InnerSequence));
				if (_cachedPlaceholder != null)
					result._cachedPlaceholder = context.CloneExpression(_cachedPlaceholder);
				return result;
			}

			public SqlPlaceholderExpression? CreatePlaceholder(ProjectFlags flags)
			{
				var args     = _methodCall.Method.GetGenericArguments();
				var param    = Expression.Parameter(args[0], "param");
				var expr     = _methodCall.Arguments[1];
				var keysFlag = flags.SqlFlag().KeyFlag();

				var placeholderContext = Parent ?? InnerSequence;

				var contextRef   = new ContextRefExpression(args[0], InnerSequence);
				var sequenceExpr = Builder.ConvertToSqlExpr(InnerSequence, contextRef, keysFlag);

				var sequencePlaceholders = ExpressionBuilder.CollectPlaceholders(sequenceExpr);
				if (sequencePlaceholders.Count == 0)
				{
					//TODO: better error handling
					return null;
				}

				var testExpr         = Builder.ConvertToSqlExpr(placeholderContext, expr, keysFlag);
				var testPlaceholders = ExpressionBuilder.CollectPlaceholders(testExpr);

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
						testExpr  = Builder.ConvertToSqlExpr(placeholderContext, availableComparisons[0].Item1, keysFlag);
						if (testExpr is SqlPlaceholderExpression placeholder)
						{
							testPlaceholders.Add(placeholder);
							useExists = false;
						}
					}
				}

				if (useExists)
				{
					if (Builder.DataContext.SqlProviderFlags.DoesNotSupportCorrelatedSubquery)
					{
						return null;
					}

					var condition = Expression.Lambda(ExpressionBuilder.Equal(MappingSchema, param, expr), param);
					var sequence = Builder.BuildWhere(Parent, InnerSequence,
						condition: condition, checkForSubQuery: true, enforceHaving: false, isTest: flags.IsTest(), false);

					if (sequence == null)
						return null;

					predicate = new SqlPredicate.FuncLike(SqlFunction.CreateExists(sequence.SelectQuery));
				}
				else
				{
					if (!flags.IsTest())
					{
						var columns = Builder.ToColumns(InnerSequence, sequenceExpr);
					}

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
		}
	}
}
