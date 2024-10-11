using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsMethodCall("DefaultIfEmpty")]
	sealed class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		static ReadOnlyCollection<Expression>? PrepareNoNullConditions(ExpressionBuilder builder, IBuildContext notNullHandlerSequence, IBuildContext sequence, IBuildContext nullabilitySequence, bool allowNullField)
		{
			var sequenceRef  = new ContextRefExpression(sequence.ElementType, sequence);
			var translated   = builder.BuildSqlExpression(sequence, sequenceRef);
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(translated);

			var nullability = NullabilityContext.GetContext(nullabilitySequence.SelectQuery);
			var notNull = placeholders
				.Where(p => !p.Sql.CanBeNullable(nullability))
				.Cast<Expression>()
				.ToList();

			if (notNull.Count == 0)
			{
				if (builder.DataContext.SqlProviderFlags.IsAccessBuggyLeftJoinConstantNullability)
				{
					if (placeholders.Count == 0)
						return null;

					notNull = placeholders.Cast<Expression>().ToList();
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
			if (buildInfo.SourceCardinality == SourceCardinality.Unknown)
			{
				var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				if (sequenceResult.BuildContext == null)
					return sequenceResult;

				var sequence = sequenceResult.BuildContext;

				defaultValue ??= Expression.Default(methodCall.Method.GetGenericArguments()[0]);

				var defaultValueContext = new SelectContext(buildInfo.Parent,
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
					allowNullField: true,
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

				return BuildSequenceResult.FromContext(new DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, defaultValue, true, false));
			}
		}

		public sealed class DefaultIfEmptyContext : SequenceContextBase
		{
			readonly IBuildContext _nullabilitySequence;
			readonly bool          _allowNullField;

			ReadOnlyCollection<Expression>? _notNullConditions;

			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, IBuildContext nullabilitySequence, Expression? defaultValue, bool allowNullField, bool isNullValidationDisabled)
				: base(parent, sequence, null)
			{
				_nullabilitySequence     = nullabilitySequence;
				_allowNullField          = allowNullField;
				DefaultValue             = defaultValue;
				IsNullValidationDisabled = isNullValidationDisabled;
			}

			public bool        IsNullValidationDisabled { get; set; }
			public Expression? DefaultValue             { get; }

			public const string NotNullPropName = "not_null";

			public ReadOnlyCollection<Expression> GetNotNullConditions()
			{
				if (_notNullConditions == null)
					_notNullConditions = PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, true) ?? throw new InvalidOperationException();
				return _notNullConditions;
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

				if (flags.IsRoot() || flags.IsTable() || flags.IsExtractProjection() || flags.IsAggregationRoot())
					return newPath;

				if ((flags.IsSql() || flags.IsExpression()) && SequenceHelper.IsSpecialProperty(path, typeof(int?), NotNullPropName))
				{
					var placeholder = ExpressionBuilder.CreatePlaceholder(this,
						new SqlNullabilityExpression(new SqlValue(1), true),
						path,
						alias : NotNullPropName);

					return placeholder;
				}

				if (!IsNullValidationDisabled && DefaultValue != null)
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

				var expr = Builder.BuildExpression(this, newPath);

				expr = Builder.UpdateNesting(this, expr);

				expr = SequenceHelper.CorrectTrackingPath(Builder, expr, path);

				if (!IsNullValidationDisabled
					&& expr.UnwrapConvert() is not SqlEagerLoadExpression
					&& !flags.IsAssociationRoot())
				{
					if (expr is SqlPlaceholderExpression placeholder)
					{
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

					if (_notNullConditions == null)
					{
						_notNullConditions = PrepareNoNullConditions(Builder, this, Sequence, _nullabilitySequence, _allowNullField);
					}

					if (_notNullConditions != null)
					{
						expr = new SqlDefaultIfEmptyExpression(expr, _notNullConditions);
						return expr;
					}
				}

				return expr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, 
					context.CloneContext(Sequence), 
					context.CloneContext(_nullabilitySequence), 
					context.CloneExpression(DefaultValue), 
					_allowNullField, 
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
