using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall("DefaultIfEmpty")]
	sealed class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call)
			=> call.IsQueryable;

		static ReadOnlyCollection<Expression>? PrepareNoNullConditions(ExpressionBuilder builder, IBuildContext notNullHandlerSequence, IBuildContext sequence, IBuildContext nullabilitySequence, out bool guardsEnclosingValues)
		{
			var sequenceRef     = new ContextRefExpression(sequence.ElementType, sequence);
			var translated      = builder.BuildSqlExpression(sequence, sequenceRef);
			var allPlaceholders = ExpressionBuilder.CollectDistinctPlaceholders(translated, false);

			// Only a value of the sequence's own query is NULL-extended by the outer join. A value of an enclosing source
			// keeps its value on a missing row, so it cannot tell whether the row is there.
			var ownSources   = QueryHelper.EnumerateAccessibleSources(sequence.SelectQuery).ToList();
			var placeholders = allPlaceholders
				.Where(p => p.SelectQuery != null && ownSources.Contains(p.SelectQuery, QueryElement.ReferenceComparer))
				.ToList();

			var nullability = NullabilityContext.GetContext(nullabilitySequence.SelectQuery);
			var notNull = placeholders
				.Where(p => !p.Sql.CanBeNullable(nullability))
				.Cast<Expression>()
				.ToList();

			guardsEnclosingValues = true;

			if (notNull.Count == 0)
			{
				if (builder.DataContext.SqlProviderFlags.IsAccessBuggyLeftJoinConstantNullability)
				{
					// Access does not NULL-extend the constant marker below, and a nullable marker reads a matched row holding
					// a NULL as missing. Without a non-nullable value of its own the sequence keeps the markers it always had,
					// enclosing values included, and they guard no enclosing value.
					guardsEnclosingValues = false;

					notNull = allPlaceholders
						.Where(p => !p.Sql.CanBeNullable(nullability))
						.Take(1)
						.Cast<Expression>()
						.ToList();

					if (notNull.Count == 0)
					{
						if (allPlaceholders.Count == 0)
							return null;

						notNull = allPlaceholders.Cast<Expression>().ToList();
					}
				}
				else
				{
					notNull.Add(
						SequenceHelper.CreateSpecialProperty(
							new ContextRefExpression(notNullHandlerSequence.ElementType, notNullHandlerSequence),
							typeof(int?),
							DefaultIfEmptyContext.NotNullPropName
						)
					);
				}

			}
			else if (notNull.Count > 0)
			{
				notNull.RemoveRange(1, notNull.Count - 1);
			}

			return notNull.AsReadOnly();
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var defaultValue = methodCall.Arguments.Count == 1 ? null : methodCall.Arguments[1].Unwrap();

			// Generating LEFT JOIN from one record resultset
			if (buildInfo.SourceCardinality == SourceCardinality.Unknown || (defaultValue != null && buildInfo.SourceCardinality.HasFlag(SourceCardinality.Zero)))
			{
				var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				if (sequenceResult.BuildContext == null)
					return sequenceResult;

				var sequence = sequenceResult.BuildContext;

				defaultValue ??= Expression.Default(methodCall.Method.GetGenericArguments()[0]);

				var defaultValueContext = new SelectContext(
					builder.GetTranslationModifier(),
					buildInfo.Parent,
					builder,
					null,
					defaultValue,
					new SelectQuery(), buildInfo.IsSubQuery);

				var subqueryContext = new SubQueryContext(defaultValueContext);

				var joinedTable = new SqlJoinedTable(JoinType.Left, sequence.SelectQuery, "d", false);

				subqueryContext.SelectQuery.From.Tables[0].Joins.Add(joinedTable);

				var defaultRef = new ContextRefExpression(defaultValueContext.ElementType, defaultValueContext);

				// force to generate fields
				var translated = builder.BuildSqlExpression(defaultValueContext, defaultRef);
				translated = builder.UpdateNesting(subqueryContext, translated);
				if (defaultValueContext.SelectQuery.Select.Columns.Count == 0)
				{
					//TODO: consider to move to SelectQueryOptimizerVisitor
					defaultValueContext.SelectQuery.Select.AddNew(new SqlValue(1));
				}

				var defaultIfEmptyContext = new DefaultIfEmptyContext(
					buildInfo.Parent,
					sequence,
					sequence,
					defaultValue: null,
					isNullValidationDisabled: false);

				var notNullConditions = defaultIfEmptyContext.GetNotNullConditions();

				var defaultIfEmptyRef = new ContextRefExpression(defaultIfEmptyContext.ElementType, defaultIfEmptyContext);
				var defaultRefExpr    = (Expression)defaultRef;
				if (defaultRefExpr.Type != defaultIfEmptyRef.Type)
				{
					defaultRefExpr = Expression.Convert(defaultRefExpr, defaultIfEmptyRef.Type);
				}

				var condition = notNullConditions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);
				var bodyValue = Expression.Condition(condition, defaultIfEmptyRef, defaultRefExpr);

				var resultSelectContext =
					new SelectContext(buildInfo.Parent, bodyValue, subqueryContext, buildInfo.IsSubQuery);

				if (!buildInfo.IsSubQuery)
				{
					if (!builder.IsSupportedSubquery(resultSelectContext, resultSelectContext, out var errorMessage))
						return BuildSequenceResult.Error(methodCall, errorMessage);
				}

				return BuildSequenceResult.FromContext(new SubQueryContext(resultSelectContext));
			}
			else
			{
				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]) {SourceCardinality = buildInfo.SourceCardinality | SourceCardinality.Zero});
				if (buildResult.BuildContext == null)
					return buildResult;
				var sequence = buildResult.BuildContext;

				return BuildSequenceResult.FromContext(new DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, defaultValue, false));
			}
		}

		public sealed class DefaultIfEmptyContext : SequenceContextBase
		{
			readonly IBuildContext _nullabilitySequence;

			ReadOnlyCollection<Expression>? _notNullConditions;
			bool                            _guardsEnclosingValues;

			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, IBuildContext nullabilitySequence, Expression? defaultValue, bool isNullValidationDisabled)
				: base(parent, sequence, null)
			{
				_nullabilitySequence     = nullabilitySequence;
				DefaultValue             = defaultValue;
				IsNullValidationDisabled = isNullValidationDisabled;
			}

			public bool        IsNullValidationDisabled { get; set; }
			public Expression? DefaultValue             { get; }

			public const string NotNullPropName = "not_null";

			public ReadOnlyCollection<Expression> GetNotNullConditions()
			{
				return _notNullConditions ??= PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, out _guardsEnclosingValues) ?? throw new InvalidOperationException();
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsTraverse())
					return path;

				if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
					return path;

				var newPath = SequenceHelper.CorrectExpression(path, this, Sequence);

				if (ExpressionEqualityComparer.Instance.Equals(newPath, path))
					return path;

				// A Nullable<T> member stays rooted here, so what is read from it (HasValue, Value) still passes the
				// not-null guard below and sees that the row can be missing.
				if (flags.IsMemberRoot() && !IsNullValidationDisabled && path is MemberExpression && path.Type.IsNullableType)
					return path;

				if (flags.IsRoot() || flags.IsTable() || flags.IsExtractProjection() || flags.IsAggregationRoot())
					return newPath;

				if ((flags.IsSql() || flags.IsExpression()) && SequenceHelper.IsSpecialProperty(path, typeof(int?), NotNullPropName))
				{
					var placeholder = ExpressionBuilder.CreatePlaceholder(this,
						new SqlValue(1),
						path,
						alias : NotNullPropName);

					return placeholder;
				}

				if (!IsNullValidationDisabled && DefaultValue != null)
				{
					return ProjectWithDefaultValue(flags, newPath);
				}

				var expr = Builder.BuildExpression(this, newPath);

				expr = Builder.UpdateNesting(this, expr);

				expr = SequenceHelper.CorrectTrackingPath(Builder, expr, path);

				if (!IsNullValidationDisabled
					&& expr.UnwrapConvert() is not SqlEagerLoadExpression
					&& !flags.IsAssociationRoot())
				{
					if (expr is SqlPlaceholderExpression placeholder)
					{
						// A value of an enclosing source is not a column of this query, so the outer join does not
						// NULL-extend it: it is read only when the markers say the row is there. Markers that cannot say
						// so (see PrepareNoNullConditions) leave it as it is.
						if (!ReferenceEquals(placeholder.SelectQuery, SelectQuery))
						{
							_notNullConditions ??= PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, out _guardsEnclosingValues);

							if (_notNullConditions == null || !_guardsEnclosingValues)
								return placeholder;

							var notNullTest = _notNullConditions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);

							return Expression.Condition(notNullTest, placeholder, new DefaultValueExpression(MappingSchema, placeholder.Type, true));
						}

						if (flags.IsExpression())
						{
							var nullablePlaceholder = placeholder.MakeNullable();
							if (path.Type != placeholder.Type)
							{
								return Expression.Condition(
									Expression.NotEqual(nullablePlaceholder, Expression.Default(placeholder.Type)),
									placeholder, Expression.Default(path.Type));
							}
						}

						return placeholder;
					}

					_notNullConditions ??= PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, out _guardsEnclosingValues);

					if (_notNullConditions != null)
					{
						expr = new SqlDefaultIfEmptyExpression(expr, _notNullConditions);
						return expr;
					}
				}

				return expr;

				Expression ProjectWithDefaultValue(ProjectFlags flags, Expression newPath)
				{
					var notNullConditions = GetNotNullConditions();

					var sequenceRef = new ContextRefExpression(ElementType, Sequence);

					var testCondition = notNullConditions.Select(SequenceHelper.MakeNotNullCondition).Aggregate(Expression.AndAlso);

					var defaultValue = DefaultValue;
					if (defaultValue.Type != sequenceRef.Type)
					{
						defaultValue = Expression.Convert(defaultValue, sequenceRef.Type);
					}

					var body = Expression.Condition(testCondition, sequenceRef, defaultValue);

					var projectedDefault = Builder.Project(Sequence, newPath, null, -1, flags, body, true);
					return projectedDefault;
				}
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null,
					context.CloneContext(Sequence),
					context.CloneContext(_nullabilitySequence),
					context.CloneExpression(DefaultValue),
					IsNullValidationDisabled);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, buildInfo);
			}

			public override bool IsOptional => true;
		}
	}
}
