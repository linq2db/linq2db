using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class ContainsBuilder : MethodCallBuilder
	{
		static readonly string[] MethodNames      = { "Contains"      };
		static readonly string[] MethodNamesAsync = { "ContainsAsync" };

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable     (MethodNames     ) && methodCall.Arguments.Count == 2 ||
				methodCall.IsAsyncExtension(MethodNamesAsync) && methodCall.Arguments.Count == 3;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var innerQuery = new SelectQuery();
			var sequence   = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], innerQuery));
            sequence = new SubQueryContext(sequence);

			var buildInStatement = false;

			if (sequence.SelectQuery.Select.TakeValue != null                              ||
			    sequence.SelectQuery.Select.SkipValue != null                              ||
			    builder.DataContext.SqlProviderFlags.DoesNotSupportCorrelatedSubquery      ||
			    builder.DataContext.SqlProviderFlags.IsExistsPreferableForContains == false &&
			    builder.DataOptions.LinqOptions.PreferExistsForScalar == false              &&
			    builder.MappingSchema.IsScalarType(methodCall.Arguments[1].Type))
			{
				buildInStatement = true;
			}

			return new ContainsContext(buildInfo.Parent, methodCall, buildInfo.SelectQuery, sequence, buildInStatement);
		}

		public static bool IsConstant(MethodCallExpression methodCall)
		{
			if (!methodCall.IsQueryable("Contains"))
				return false;

			return methodCall.IsQueryable(false) == false;
		}

		sealed class ContainsContext : BuildContextBase
		{
			public override Expression Expression { get; }

			SelectQuery   OuterQuery    { get; }
			IBuildContext InnerSequence { get; }

			readonly MethodCallExpression _methodCall;
			readonly bool                 _buildInStatement;

			public ContainsContext(IBuildContext? parent, MethodCallExpression methodCall, SelectQuery outerQuery, IBuildContext innerSequence, bool buildInStatement)
				:base(innerSequence.Builder, typeof(bool), outerQuery)
			{
				Parent            = parent;
				OuterQuery        = outerQuery;
				Expression        = methodCall;
				_methodCall       = methodCall;
				_buildInStatement = buildInStatement;
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
				var result = new ContainsContext(null, _methodCall, context.CloneElement(OuterQuery), context.CloneContext(InnerSequence), _buildInStatement);
				if (_cachedPlaceholder != null)
					result._cachedPlaceholder = context.CloneExpression(_cachedPlaceholder);
				return result;
			}

			SqlPlaceholderExpression? CreatePlaceholder(ProjectFlags flags)
			{
				var args  = _methodCall.Method.GetGenericArguments();
				var param = Expression.Parameter(args[0], "param");
				var expr  = _methodCall.Arguments[1];

				var testPlaceholder = Builder.TryConvertToSqlPlaceholder(InnerSequence, expr, flags);

				var contextRef          = new ContextRefExpression(args[0], InnerSequence);
				var sequencePlaceholder = Builder.TryConvertToSqlPlaceholder(InnerSequence, contextRef, flags);

				SqlCondition cond;

				if (_buildInStatement && testPlaceholder != null && sequencePlaceholder != null)
				{
					if (!flags.IsTest())
						_ = Builder.ToColumns(InnerSequence, sequencePlaceholder);
					cond = new SqlCondition(false, new SqlPredicate.InSubQuery(testPlaceholder.Sql, false, InnerSequence.SelectQuery));
				}
				else
				{
					var condition = Expression.Lambda(ExpressionBuilder.Equal(Builder.MappingSchema, param, expr), param);
					var sequence = Builder.BuildWhere(Parent, InnerSequence,
						condition: condition, checkForSubQuery: true, enforceHaving: false, isTest: flags.IsTest());

					if (sequence == null)
						return null;

					cond = new SqlCondition(false, new SqlPredicate.FuncLike(SqlFunction.CreateExists(sequence.SelectQuery)));
				}

				var subQuerySql = new SqlSearchCondition(cond);

				return ExpressionBuilder.CreatePlaceholder(OuterQuery, subQuerySql, _methodCall);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<object>(SelectQuery, expr);
				QueryRunner.SetRunQuery(query, mapper);
			}
		}
	}
}
