using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Extensions;
	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("DefaultIfEmpty");
		}

		static Expression MakeNotNullCondition(Expression expr)
		{
			if (expr.Type.IsValueType && !expr.Type.IsNullable())
			{
				if (expr is SqlPlaceholderExpression placeholder)
					expr = placeholder.WithSql(SqlNullabilityExpression.ApplyNullability(placeholder.Sql, true)).MakeNullable();
				else
					expr = Expression.Convert(expr, expr.Type.AsNullable());
			}

			return Expression.NotEqual(expr, Expression.Default(expr.Type));
		}

		static Expression? PrepareNoNullCondition(ExpressionBuilder builder, IBuildContext notNullHandlerSequence, IBuildContext sequence, IBuildContext nullabilitySequence, bool allowNullField)
		{
			var sequenceRef  = new ContextRefExpression(sequence.ElementType, sequence);
			var translated   = builder.BuildSqlExpression(sequence, sequenceRef, ProjectFlags.SQL, buildFlags: ExpressionBuilder.BuildFlags.ForceAssignments);
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(translated);

			var nullability = NullabilityContext.GetContext(nullabilitySequence.SelectQuery);
			var notNull = placeholders
				.Where(p => !p.Sql.CanBeNullable(nullability))
				.Cast<Expression>()
				.ToList();

			if (notNull.Count == 0)
			{
				/*
				if (!allowNullField)
					return null;
					*/

				if (builder.DataContext.SqlProviderFlags.IsAccessBuggyLeftJoinConstantNullability)
				{
					if (placeholders.Count == 0)
						return null;

					var combined = placeholders.Select(MakeNotNullCondition).Aggregate(Expression.AndAlso);

					return combined;
				}

				notNull = new()
				{
					SequenceHelper.CreateSpecialProperty(new ContextRefExpression(notNullHandlerSequence.ElementType, notNullHandlerSequence), typeof(int?),
						DefaultIfEmptyContext.NotNullPropName)
				};
			}

			var notNullCondition = notNull.Select(MakeNotNullCondition).First();

			return notNullCondition;
		}

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var defaultValue = methodCall.Arguments.Count == 1 ? null : methodCall.Arguments[1].Unwrap();

			// Generating LEFT JOIN from one record resultset
			if (buildInfo.SourceCardinality.HasFlag(SourceCardinality.Zero))
			{
				var sequenceResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				if (sequenceResult.BuildContext == null)
					return sequenceResult;

				var sequence = sequenceResult.BuildContext;

				if (buildInfo.IsSubQuery)
				{
					// At lease Oracle and MySql cannot handle such subquery
					if (!SequenceHelper.IsSupportedSubqueryNesting(sequence))
						return BuildSequenceResult.Error(methodCall);
				}

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
				var translated = builder.BuildSqlExpression(defaultValueContext, defaultRef, ProjectFlags.SQL, buildFlags : ExpressionBuilder.BuildFlags.ForceAssignments);
				translated = builder.UpdateNesting(subqueryContext, translated);
				if (defaultValueContext.SelectQuery.Select.Columns.Count == 0)
				{
					//TODO: consider to move to SelectQueryOptimizerVisitor
					defaultValueContext.SelectQuery.Select.AddNew(new SqlValue(1));
				}

				var notNullCondition = PrepareNoNullCondition(builder, sequence, sequence, subqueryContext, true);

				if (notNullCondition == null)
					return BuildSequenceResult.Error(methodCall);

				var sequenceRef = new ContextRefExpression(sequence.ElementType, sequence);
				var defaultRefExpr = (Expression)defaultRef;
				if (defaultRefExpr.Type != sequenceRef.Type)
				{
					defaultRefExpr = Expression.Convert(defaultRefExpr, sequenceRef.Type);
				}

				var bodyValue = Expression.Condition(notNullCondition, sequenceRef, defaultRefExpr);

				var resultSelectContext =
					new SelectContext(buildInfo.Parent, bodyValue, subqueryContext, buildInfo.IsSubQuery);

				return BuildSequenceResult.FromContext(new SubQueryContext(resultSelectContext));
			}
			else
			{
				var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
				if (buildResult.BuildContext == null)
					return buildResult;
				var sequence = buildResult.BuildContext;

				return BuildSequenceResult.FromContext(new DefaultIfEmptyContext(buildInfo.Parent, sequence, sequence, defaultValue, true));
			}
		}

		public sealed class DefaultIfEmptyContext : SequenceContextBase
		{
			readonly IBuildContext _nullabilitySequence;
			readonly bool          _allowNullField;

			Expression? _notNullCondition;

			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, IBuildContext nullabilitySequence, Expression? defaultValue, bool allowNullField)
				: base(parent, sequence, null)
			{
				_nullabilitySequence = nullabilitySequence;
				_allowNullField      = allowNullField;
				DefaultValue         = defaultValue;
			}

			public Expression? DefaultValue { get; }

			public const string NotNullPropName = "not_null";

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) && (flags.IsRoot() || flags.IsAssociationRoot()))
					return path;

				var newPath = SequenceHelper.CorrectExpression(path, this, Sequence);

				if (ExpressionEqualityComparer.Instance.Equals(newPath, path))
					return path;

				if (flags.IsTraverse() || flags.IsRoot() || flags.IsTable() || flags.IsExtractProjection())
					return newPath;

				if ((flags.IsSql() || flags.IsExpression()) && SequenceHelper.IsSpecialProperty(path, typeof(int?), NotNullPropName))
				{
					var placeholder = ExpressionBuilder.CreatePlaceholder(this,
						new SqlNullabilityExpression(new SqlValue(1), true),
						path,
						alias : NotNullPropName);

					return placeholder;
				}

				if (DefaultValue != null)
				{
					if (_notNullCondition == null)
						_notNullCondition = PrepareNoNullCondition(Builder, this, Sequence, _nullabilitySequence, true) ?? throw new InvalidOperationException();;

					var sequenceRef = new ContextRefExpression(ElementType, Sequence);

					var body = Expression.Condition(_notNullCondition, sequenceRef, DefaultValue);

					var projectedDefault = Builder.Project(Sequence, newPath, null, -1, flags, body, true);
					return projectedDefault;
				}

				var expr = Builder.BuildSqlExpression(this, newPath, flags/*.SqlFlag()*/);

				if (!flags.IsTest())
				{
					expr = Builder.UpdateNesting(this, expr);
				}

				expr = SequenceHelper.CorrectTrackingPath(Builder, expr, path);

				if (/*!flags.IsKeys() && */expr.UnwrapConvert() is not SqlEagerLoadExpression)
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

					if (_notNullCondition == null)
					{
						_notNullCondition = PrepareNoNullCondition(Builder, this, Sequence, _nullabilitySequence, _allowNullField);
					}

					if (_notNullCondition != null)
					{
						expr = new SqlDefaultIfEmptyExpression(expr, _notNullCondition);
						return expr;
					}
				}

				return expr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, context.CloneContext(Sequence), context.CloneContext(_nullabilitySequence), context.CloneExpression(DefaultValue), _allowNullField);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, buildInfo);
			}
		}
	}
}
