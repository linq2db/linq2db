using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.Linq.Builder
{
	using System.IO;

	using LinqToDB.Expressions;
	using SqlQuery;

	sealed class DefaultIfEmptyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return methodCall.IsQueryable("DefaultIfEmpty");
		}

		static SqlPlaceholderExpression? PrepareNoNullPlaceholder(ExpressionBuilder builder, IBuildContext sequence, IBuildContext forContext, bool allowNullField)
		{
			var sequenceRef  = new ContextRefExpression(sequence.ElementType, sequence);
			var translated   = builder.BuildSqlExpression(sequence, sequenceRef, ProjectFlags.SQL, buildFlags: ExpressionBuilder.BuildFlags.ForceAssignments);
			var placeholders = ExpressionBuilder.CollectDistinctPlaceholders(translated);

			var notNull = placeholders
				.FirstOrDefault(p => !p.Sql.CanBeNullable(NullabilityContext.NonQuery));

			if (notNull == null)
			{
				if (!allowNullField)
					return null;

				notNull = ExpressionBuilder.CreatePlaceholder(sequence,
					new SqlNullabilityExpression(new SqlValue(1), true),
					SequenceHelper.CreateSpecialProperty(sequenceRef, typeof(int?), DefaultIfEmptyContext.NotNullPropName),
					alias : DefaultIfEmptyContext.NotNullPropName);
			}

			notNull = builder.UpdateNesting(forContext, notNull);

			return notNull;
		}

		protected override IBuildContext? BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var defaultValue = methodCall.Arguments.Count == 1 ? null : methodCall.Arguments[1].Unwrap();

			// Generating LEFT JOIN from one record resultset
			// 
			if (buildInfo.SourceCardinality.HasFlag(SourceCardinality.Zero))
			{
				var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0], new SelectQuery()));
				if (sequence == null)
					return null;

				if (buildInfo.IsSubQuery)
				{
					// At lease Oracle and MySql cannot handle such subquery
					if (!SequenceHelper.IsSupportedSubqueryNesting(sequence))
						return null;
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

				var notNull = PrepareNoNullPlaceholder(builder, sequence, subqueryContext, true)!;

				var notNullNullable = notNull.MakeNullable();

				var sequenceRef = new ContextRefExpression(sequence.ElementType, sequence);
				var defaultRefExpr = (Expression)defaultRef;
				if (defaultRefExpr.Type != sequenceRef.Type)
				{
					defaultRefExpr = Expression.Convert(defaultRefExpr, sequenceRef.Type);
				}

				var bodyValue =
					Expression.Condition(Expression.NotEqual(notNullNullable, Expression.Default(notNullNullable.Type)),
						sequenceRef, defaultRefExpr);

				var resultSelectContext =
					new SelectContext(buildInfo.Parent, bodyValue, subqueryContext, buildInfo.IsSubQuery);

				return new SubQueryContext(resultSelectContext);
			}
			else
			{
				var sequence = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
				if (sequence == null)
					return null;

				return new DefaultIfEmptyContext(buildInfo.Parent, sequence, defaultValue, true);
			}
		}

		public sealed class DefaultIfEmptyContext : SequenceContextBase
		{
			readonly bool _allowNullField;

			SqlPlaceholderExpression? _notNullPlaceholder;

			public DefaultIfEmptyContext(IBuildContext? parent, IBuildContext sequence, Expression? defaultValue, bool allowNullField)
				: base(parent, sequence, null)
			{
				_allowNullField = allowNullField;
				DefaultValue    = defaultValue;
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
					if (_notNullPlaceholder == null)
					{
						_notNullPlaceholder = PrepareNoNullPlaceholder(Builder, Sequence, Parent ?? this, true)!;
						if (!_notNullPlaceholder.Type.IsNullable())
							_notNullPlaceholder = _notNullPlaceholder.MakeNullable();
					}

					var sequenceRef = new ContextRefExpression(ElementType, Sequence);

					var body =
						Expression.Condition(
							Expression.NotEqual(_notNullPlaceholder, Expression.Default(_notNullPlaceholder.Type)),
							sequenceRef, DefaultValue);

					var projectedDefault = Builder.Project(Sequence, newPath, null, -1, flags, body, true);
					return projectedDefault;
				}

				var expr = Builder.BuildSqlExpression(this, newPath, flags/*.SqlFlag()*/);

				if (!flags.IsTest())
				{
					expr = Builder.UpdateNesting(this, expr);
				}

				expr = SequenceHelper.CorrectTrackingPath(Builder, expr, path);

				if (!flags.IsKeys() && expr.UnwrapConvert() is not SqlEagerLoadExpression)
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

					if (_notNullPlaceholder == null)
					{
						_notNullPlaceholder = PrepareNoNullPlaceholder(Builder, Sequence, this, _allowNullField);
					}

					if (_notNullPlaceholder != null)
					{
						Expression notNullField = _notNullPlaceholder;

						if (flags.IsExtractProjection())
						{
							notNullField = _notNullPlaceholder.Path;
							if (notNullField.Type.IsValueType && !notNullField.Type.IsNullable())
							{
								notNullField = Expression.Convert(notNullField, notNullField.Type.AsNullable());
							}
						}
						else if (_notNullPlaceholder.Type.IsValueType && !_notNullPlaceholder.Type.IsNullable())
						{
							notNullField = _notNullPlaceholder.MakeNullable();
						}

						var defaultValue = new DefaultValueExpression(MappingSchema, expr.Type);

						var notNullExpression = Expression.NotEqual(notNullField, Expression.Constant(null, notNullField.Type));

						if (flags.IsExtractProjection())
						{
							expr = newPath;
						}

						expr = Expression.Condition(notNullExpression, expr, defaultValue);
					}

				}

				return expr;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new DefaultIfEmptyContext(null, context.CloneContext(Sequence), context.CloneExpression(DefaultValue), _allowNullField);
			}

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				expression = SequenceHelper.CorrectExpression(expression, this, Sequence);
				return Sequence.GetContext(expression, buildInfo);
			}
		}
	}
}
