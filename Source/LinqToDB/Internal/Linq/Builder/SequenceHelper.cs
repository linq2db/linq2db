using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	internal static class SequenceHelper
	{
		/// <summary>
		/// Whether two stored values are handed to the reader on the same terms, so one reading can serve both.
		/// </summary>
		/// <remarks>
		/// A conversion is not arithmetic on the SQL side, so bringing two values that carry different ones into a
		/// single value loses the difference: whichever conversion the result is read through is right for at most
		/// one of them. Callers that only know one side decide for themselves what an unknown means - here both
		/// sides are known.
		/// <para>
		/// The declared duration unit is compared rather than the converter derived from it: two columns declaring
		/// the same unit get equivalent converters that are not the same object.
		/// </para>
		/// </remarks>
		public static bool ReadTheSameWay(ColumnDescriptor descriptor1, ColumnDescriptor descriptor2)
		{
			if (descriptor1.DurationUnit != null || descriptor2.DurationUnit != null)
				return descriptor1.DurationUnit == descriptor2.DurationUnit;

			return ConvertTheSameWay(descriptor1.ValueConverter, descriptor2.ValueConverter);
		}

		/// <summary>
		/// Whether two value converters carry a value between the model and the database on the same terms.
		/// </summary>
		/// <remarks>
		/// Compared by what they do rather than by which object they are, so the same conversion declared twice
		/// counts as one: two entities that each say <c>HasConversion(ts =&gt; ts.Ticks, v =&gt; TimeSpan.FromTicks(v))</c>
		/// get converters that are equal in every way except identity, and treating those as different would keep
		/// apart two columns that are interchangeable.
		/// <para>
		/// A converter built from delegates rather than expressions holds them as opaque constants, so two of those
		/// never compare equal however alike they behave. That is the safe direction to be wrong in: the answer is
		/// used to decide whether one reading can serve both values, and "no" costs a column or a refusal while
		/// "yes" would silently read one value through the other's conversion.
		/// </para>
		/// </remarks>
		public static bool ConvertTheSameWay(IValueConverter? converter1, IValueConverter? converter2)
		{
			// Covers both being absent, and the far more common case of one descriptor reached along two paths.
			if (ReferenceEquals(converter1, converter2))
				return true;

			if (converter1 == null || converter2 == null)
				return false;

			return converter1.HandlesNulls == converter2.HandlesNulls
				&& ConversionsMatch(converter1.FromProviderExpression, converter2.FromProviderExpression)
				&& ConversionsMatch(converter1.ToProviderExpression,   converter2.ToProviderExpression);
		}

		/// <summary>
		/// Whether two conversion lambdas carry the same value across, disregarding which of them speaks in terms of
		/// a nullable type.
		/// </summary>
		/// <remarks>
		/// The same conversion declared on a nullable property and on a plain one produces lambdas that differ only
		/// in where <c>Nullable&lt;&gt;</c> appears - the delegate types differ, and the body carries an extra
		/// conversion to reach the declared type. Compared as they stand they are never equal, which would keep
		/// apart two columns that hold the same thing.
		/// <para>
		/// Nulls themselves are not what is being compared here: whether a converter is prepared to see one is
		/// already settled by <see cref="IValueConverter.HandlesNulls"/>, and a NULL read is decided by the column's
		/// nullability rather than by the conversion.
		/// </para>
		/// </remarks>
		static bool ConversionsMatch(LambdaExpression lambda1, LambdaExpression lambda2)
		{
			if (lambda1.Parameters.Count != 1 || lambda2.Parameters.Count != 1)
				return ExpressionEqualityComparer.Instance.Equals(lambda1, lambda2);

			var parameter1 = lambda1.Parameters[0];
			var parameter2 = lambda2.Parameters[0];

			if (parameter1.Type.UnwrapNullableType()    != parameter2.Type.UnwrapNullableType()
				|| lambda1.ReturnType.UnwrapNullableType() != lambda2.ReturnType.UnwrapNullableType())
			{
				return false;
			}

			var common = Expression.Parameter(parameter1.Type.UnwrapNullableType(), "p");

			return ExpressionEqualityComparer.Instance.Equals(
				Canonicalize(lambda1.Body, parameter1, common),
				Canonicalize(lambda2.Body, parameter2, common));
		}

		/// <summary>
		/// Rewrites a conversion body so that two of them can be told apart by what they compute rather than by the
		/// nullability they were written against: the parameter becomes a shared one, and a conversion that only
		/// puts a value into or takes it out of <c>Nullable&lt;&gt;</c> is dropped.
		/// </summary>
		static Expression Canonicalize(Expression body, ParameterExpression parameter, ParameterExpression common)
		{
			return body.Transform(e =>
			{
				if (ReferenceEquals(e, parameter))
					return common;

				if (e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked, Method: null } unary
					&& unary.Type.UnwrapNullableType() == unary.Operand.Type.UnwrapNullableType())
				{
					return unary.Operand;
				}

				return e;
			});
		}

		public static Expression PrepareBody(LambdaExpression lambda, params IBuildContext[] sequences)
		{
			var body = lambda.Parameters.Count == 0
				? lambda.Body
				: lambda.GetBody(sequences
					.Select((s, idx) =>
					{
						var parameter = lambda.Parameters[idx];
						return (Expression)new ContextRefExpression(parameter.Type, s, parameter.Name);
					}).ToArray());

			if (!ReferenceEquals(body, lambda.Body))
			{
				body = body.Transform(e =>
				{
					if (e.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
						&& ((UnaryExpression)e).Operand is ContextRefExpression contextRef
						&& !e.Type.ToUnderlying().IsValueType)
					{
						return contextRef.WithType(e.Type);
					}

					return e;
				});
			}

			return body;
		}

		public static Expression ReplaceBody(Expression body, ParameterExpression parameter, IBuildContext sequence)
		{
			var contextRef = new ContextRefExpression(parameter.Type, sequence, parameter.Name);
			body = body.Replace(parameter, contextRef);
			return body;
		}

		public static bool IsSameContext(Expression? expression, IBuildContext context)
		{
			return expression == null
				|| (expression is ContextRefExpression contextRef && contextRef.BuildContext == context);
		}

		public static ContextRefExpression CreateRef(IBuildContext buildContext)
		{
			return new ContextRefExpression(buildContext.ElementType, buildContext);
		}

		public static IBuildContext UnwrapProxy(IBuildContext buildContext)
		{
			var current = buildContext;
			while (current is IBuildProxy proxy)
			{
				current = proxy.Owner;
			}

			return current;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CorrectExpression(Expression? expression, IBuildContext current,
			IBuildContext                                       underlying)
		{
			if (expression != null)
			{
				return ReplaceContext(expression, current, underlying);
			}

			return expression;
		}

		public static bool HasContextRef(Expression expression)
		{
			return null != expression.Find(e => e is ContextRefExpression);
		}

		public static Expression CorrectTrackingPath(Expression expression, IBuildContext from, IBuildContext to)
		{
			var result = expression.Transform((from, to), (ctx, e) =>
			{
				if (e is SqlPlaceholderExpression { TrackingPath: { } path } placeholder)
				{
					return placeholder.WithTrackingPath(ReplaceContext(path, ctx.from, ctx.to));
				}

				return e;
			});

			return result;
		}

		public static Expression EnsureType(Expression expr, Type type)
		{
			if (expr.Type != type)
			{
				expr = expr.UnwrapConvert();
				if (expr.Type != type)
				{
					if (expr is ContextRefExpression refExpression)
						return refExpression.WithType(type);
					return Expression.Convert(expr, type);
				}

				return expr;
			}

			return expr;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CorrectTrackingPath(ExpressionBuilder builder, Expression? expression, Expression toPath)
		{
			if (expression == null)
				return null;

			if (toPath is not (ContextRefExpression or MemberExpression))
				return expression;

			switch (expression)
			{
				case SqlGenericConstructorExpression generic:
				{
					List<SqlGenericConstructorExpression.Assignment>? assignments = null;
					List<SqlGenericConstructorExpression.Parameter>?  parameters  = null;

					var contextRef = toPath as ContextRefExpression;

					for (int i = 0; i < generic.Assignments.Count; i++)
					{
						var assignment = generic.Assignments[i];

						var currentPath = toPath;

						var applicable = true;
						if (assignment.MemberInfo.DeclaringType != null)
						{
							applicable = assignment.MemberInfo.DeclaringType.IsAssignableFrom(currentPath.Type);
							if (applicable)
								currentPath = EnsureType(currentPath, assignment.MemberInfo.DeclaringType);
						}

						if (!applicable)
						{
							assignments?.Add(assignment);
							continue;
						}

						var memberTrackingPath = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);
						var newExpression = CorrectTrackingPath(builder, assignment.Expression, memberTrackingPath);

						if (!ReferenceEquals(assignment.Expression, newExpression))
						{
							if (assignments == null)
							{
								assignments = new();
								for (int j = 0; j < i; j++)
								{
									assignments.Add(generic.Assignments[j]);
								}
							}

							assignments.Add(assignment.WithExpression(newExpression));
						}
						else
							assignments?.Add(assignment);
					}

					if (assignments != null)
					{
						generic = generic.ReplaceAssignments(assignments.AsReadOnly());
					}

					for (var i = 0; i < generic.Parameters.Count; i++)
					{
						var parameter     = generic.Parameters[i];
						var currentPath   = toPath;
						var newExpression = parameter.Expression;

						if (parameter.MemberInfo != null)
						{
							var memberTrackingPath = Expression.MakeMemberAccess(currentPath, parameter.MemberInfo);
							newExpression = CorrectTrackingPath(builder, parameter.Expression, memberTrackingPath);
						}

						if (!ReferenceEquals(parameter.Expression, newExpression))
						{
							if (parameters == null)
							{
								parameters = new();
								for (int j = 0; j < i; j++)
								{
									parameters.Add(generic.Parameters[j]);
								}
							}

							parameters.Add(parameter.WithExpression(newExpression));
						}
						else
							parameters?.Add(parameter.WithExpression(newExpression));

					}

					if (parameters != null)
					{
						generic = generic.ReplaceParameters(parameters.AsReadOnly());
					}

					return generic;
				}

				case NewExpression or MemberInitExpression:
				{
					var parsed = builder.ParseGenericConstructor(expression, ProjectFlags.SQL, null);
					if (!ReferenceEquals(parsed, expression))
						return CorrectTrackingPath(builder, parsed, toPath);
					break;
				}

				case SqlPlaceholderExpression placeholder:
				{
					if (placeholder.TrackingPath != null && !placeholder.Type.IsAssignableFrom(toPath.Type) && !placeholder.Type.IsValueType)
					{
						if (IsSpecialProperty(placeholder.TrackingPath, out var propType, out var propName))
						{
							toPath = CreateSpecialProperty(toPath, propType, propName);
							if (placeholder.Type != toPath.Type)
							{
								toPath = Expression.Convert(toPath, placeholder.Type);
							}

							return placeholder.WithTrackingPath(toPath);
						}
					}

					if (placeholder.TrackingPath is MemberExpression { Member.DeclaringType: { } declaringType, Expression: not null} me && declaringType.IsAssignableFrom(toPath.Type))
					{
						var toPathConverted = EnsureType(toPath, declaringType);
						var newExpr         = (Expression)Expression.MakeMemberAccess(toPathConverted, me.Member);

						return placeholder.WithTrackingPath(newExpr);
					}

					return placeholder.WithTrackingPath(toPath);
				}

				case SqlDefaultIfEmptyExpression defaultIfEmptyExpression:
				{
					var newExpr = defaultIfEmptyExpression.Update(
						CorrectTrackingPath(builder, defaultIfEmptyExpression.InnerExpression, toPath),
						defaultIfEmptyExpression.NotNullExpressions.Select(n => CorrectTrackingPath(builder, n, toPath))
							.ToList().AsReadOnly()
					);

					return newExpr;
				}

				case ConstantExpression:
					return expression;

				case ConditionalExpression conditional:
				{
					return conditional.Update(
						CorrectTrackingPath(builder, conditional.Test, toPath),
						CorrectTrackingPath(builder, conditional.IfTrue, toPath),
						CorrectTrackingPath(builder, conditional.IfFalse, toPath));
				}

				case BinaryExpression binary:
				{
					return binary.Update(CorrectTrackingPath(builder, binary.Left, toPath), binary.Conversion, CorrectTrackingPath(builder, binary.Right, toPath));
				}

				case UnaryExpression unary:
				{
					return unary.Update(CorrectTrackingPath(builder, unary.Operand, toPath));
				}

				/*
				if (expression is MemberExpression eme && eme.Expression is ContextRefExpression && toPath is MemberExpression && expression.Type == toPath.Type)
					return toPath;

				if (expression is ContextRefExpression && toPath is ContextRefExpression && expression.Type == toPath.Type)
					return toPath;
					*/
			}

			return expression;
		}

		public static Expression ReplacePlaceholdersPathByTrackingPath(Expression expression)
		{
			var transformed = expression.Transform(e =>
			{
				if (e is SqlPlaceholderExpression { TrackingPath: { } path } placeholder)
				{
					return placeholder.WithPath(path);
				}

				return e;
			});

			return transformed;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? RemapToNewPathSimple(ExpressionBuilder builder, Expression? expression, Expression toPath, ProjectFlags flags)
		{
			if (expression == null)
				return null;

			if (toPath is not (ContextRefExpression or MemberExpression or SqlGenericParamAccessExpression))
				return expression;

			if (expression is SqlGenericConstructorExpression generic)
			{
				List<SqlGenericConstructorExpression.Assignment>? assignments = null;
				List<SqlGenericConstructorExpression.Parameter>?  parameters  = null;

				var contextRef = toPath as ContextRefExpression;

				for (int i = 0; i < generic.Assignments.Count; i++)
				{
					var assignment = generic.Assignments[i];

					var currentPath = toPath;
					if (contextRef != null 
						&& assignment.MemberInfo.DeclaringType != null 
						&& !assignment.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
					{
						currentPath = contextRef.WithType(assignment.MemberInfo.DeclaringType);
					}

					var memberPath = Expression.MakeMemberAccess(currentPath, assignment.MemberInfo);
					var parsed     = builder.ParseGenericConstructor(assignment.Expression, flags, null);

					Expression newExpression = memberPath;
					if (parsed is SqlGenericConstructorExpression { Assignments.Count: > 0 } genericParsed)
					{
						newExpression = RemapToNewPathSimple(builder, assignment.Expression, memberPath, flags);
					}
					else if (parsed is SqlErrorExpression)
					{
						newExpression = assignment.Expression;
					}

					if (!ReferenceEquals(assignment.Expression, newExpression))
					{
						if (assignments == null)
						{
							assignments = new();
							for (int j = 0; j < i; j++)
							{
								assignments.Add(generic.Assignments[j]);
							}
						}

						assignments.Add(assignment.WithExpression(newExpression));
					}
					else
						assignments?.Add(assignment);
				}

				for (int i = 0; i < generic.Parameters.Count; i++)
				{
					var parameter = generic.Parameters[i];

					var currentPath = toPath;
					if (contextRef != null
						&& parameter.MemberInfo?.DeclaringType != null
						&& !parameter.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
					{
						currentPath = contextRef.WithType(parameter.MemberInfo.DeclaringType);
					}

					Expression newExpression;

					if (parameter.Expression is SqlErrorExpression)
					{
						newExpression = parameter.Expression;
					}
					else if (parameter.MemberInfo != null)
					{
						newExpression = Expression.MakeMemberAccess(currentPath, parameter.MemberInfo);
					}
					else
					{
						var paramAccess = new SqlGenericParamAccessExpression(currentPath, parameter.ParameterInfo);

						newExpression = paramAccess;
					}

					if (!ReferenceEquals(parameter.Expression, newExpression))
					{
						if (parameters == null)
						{
							parameters = new();
							for (int j = 0; j < i; j++)
							{
								parameters.Add(generic.Parameters[j]);
							}
						}

						parameters.Add(parameter.WithExpression(newExpression));
					}
					else
						parameters?.Add(parameter);
				}

				if (assignments != null)
				{
					generic = generic.ReplaceAssignments(assignments.AsReadOnly());
				}

				if (parameters != null)
				{
					generic = generic.ReplaceParameters(parameters.AsReadOnly());
				}

				generic = generic.WithConstructionRoot(toPath);

				return generic;
			}

			if (expression is NewExpression or MemberInitExpression)
			{
				var parsed = builder.ParseGenericConstructor(expression, ProjectFlags.SQL, null);
				if (parsed is SqlGenericConstructorExpression { Assignments.Count: > 0 } genericParsed)
					return RemapToNewPathSimple(builder, parsed, toPath, flags);
			}

			/*
			if (expression is MemberExpression && toPath is MemberExpression && expression.Type == toPath.Type)
				return toPath;

			if (expression is ContextRefExpression && toPath is ContextRefExpression && expression.Type == toPath.Type)
				return toPath;
				*/

			return expression;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? RemapToNewPath(ExpressionBuilder builder, Expression? expression, Expression toPath, ProjectFlags flags)
		{
			if (expression == null)
				return null;

			if (toPath is not (ContextRefExpression or MemberExpression or SqlGenericParamAccessExpression))
				return expression;

			switch (expression)
			{
				case SqlGenericConstructorExpression generic:
				{
					List<SqlGenericConstructorExpression.Assignment>? assignments = null;
					List<SqlGenericConstructorExpression.Parameter>?  parameters  = null;

					var contextRef = toPath as ContextRefExpression;

					for (int i = 0; i < generic.Assignments.Count; i++)
					{
						var assignment = generic.Assignments[i];

						var currentPath = toPath;
						if (contextRef != null
							&& assignment.MemberInfo.DeclaringType != null
							&& !assignment.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
						{
							currentPath = contextRef.WithType(assignment.MemberInfo.DeclaringType);
						}

						var newExpression = RemapToNewPath(
							builder,
							assignment.Expression,
							Expression.MakeMemberAccess(currentPath, assignment.MemberInfo), flags
						);

						if (!ReferenceEquals(assignment.Expression, newExpression))
						{
							if (assignments == null)
							{
								assignments = new();
								for (int j = 0; j < i; j++)
								{
									assignments.Add(generic.Assignments[j]);
								}
							}

							assignments.Add(assignment.WithExpression(newExpression));
						}
						else
							assignments?.Add(assignment);
					}

					for (int i = 0; i < generic.Parameters.Count; i++)
					{
						var parameter = generic.Parameters[i];

						var currentPath = toPath;
						if (contextRef != null
							&& parameter.MemberInfo?.DeclaringType != null
							&& !parameter.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
						{
							currentPath = contextRef.WithType(parameter.MemberInfo.DeclaringType);
						}

						var paramAccess = new SqlGenericParamAccessExpression(currentPath, parameter.ParameterInfo);

						var newExpression = RemapToNewPath(builder, parameter.Expression, paramAccess, flags);

						if (!ReferenceEquals(parameter.Expression, newExpression))
						{
							if (parameters == null)
							{
								parameters = new();
								for (int j = 0; j < i; j++)
								{
									parameters.Add(generic.Parameters[j]);
								}
							}

							parameters.Add(parameter.WithExpression(newExpression));
						}
						else
							parameters?.Add(parameter);
					}

					if (assignments != null)
					{
						generic = generic.ReplaceAssignments(assignments.AsReadOnly());
					}

					if (parameters != null)
					{
						generic = generic.ReplaceParameters(parameters.AsReadOnly());
					}

					generic = generic.WithConstructionRoot(toPath);

					return generic;
				}

				case NewExpression or MemberInitExpression:
				{
					return RemapToNewPath(builder, builder.ParseGenericConstructor(expression, ProjectFlags.SQL, null), toPath, flags);
				}

				case SqlPlaceholderExpression placeholder:
				{
					if (placeholder.Sql.IsNullValue)
						return Expression.Default(placeholder.Type);

					if (placeholder.Type == toPath.Type)
					{
						return toPath;
					}

					if (IsSpecialProperty(expression, out var propType, out var propName))
					{
						Expression newExpr = CreateSpecialProperty(toPath, propType, propName);
						if (placeholder.Type != newExpr.Type)
						{
							newExpr = Expression.Convert(newExpr, placeholder.Type);
						}

						return newExpr;
					}

					if (placeholder.Path is MemberExpression me && me.Expression?.Type == toPath.Type)
					{
						var newExpr = (Expression)Expression.MakeMemberAccess(toPath, me.Member);
						if (placeholder.Type != newExpr.Type)
						{
							newExpr = Expression.Convert(newExpr, placeholder.Type);
						}

						return newExpr;
					}

					return RemapToNewPath(builder, placeholder.TrackingPath, toPath, flags)!;
				}

				case BinaryExpression binary when toPath.Type != binary.Type:
				{
					var left  = binary.Left;
					var right = binary.Right;

					if (left is SqlPlaceholderExpression)
					{
						left = RemapToNewPath(builder, left, toPath, flags);
					}

					if (right is SqlPlaceholderExpression)
					{
						right = RemapToNewPath(builder, right, toPath, flags);
					}

					if (left.Type != right.Type)
					{
						var newLeft  = left.UnwrapConvert();
						var newRight = right.UnwrapConvert();

						if (newLeft.Type != newRight.Type)
						{
							if (!ReferenceEquals(left, newLeft))
								newLeft = Expression.Convert(newLeft, newRight.Type);
							else
								newRight = Expression.Convert(newRight, newLeft.Type);
						}

						left = newLeft;
						right = newRight;
					}

					return binary.Update(left, binary.Conversion, right);
				}

				case ConditionalExpression conditional:
				{
					var newTest  = RemapToNewPath(builder, conditional.Test,    toPath, flags);
					var newTrue  = RemapToNewPath(builder, conditional.IfTrue,  toPath, flags);
					var newFalse = RemapToNewPath(builder, conditional.IfFalse, toPath, flags);

					if (newTrue.Type != expression.Type)
						newTrue = Expression.Convert(newTrue, expression.Type);

					if (newFalse.Type != expression.Type)
						newFalse = Expression.Convert(newFalse, expression.Type);

					return conditional.Update(newTest, newTrue, newFalse);
				}

				case { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked }:
				{
					var unary = (UnaryExpression)expression;
					return unary.Update(RemapToNewPath(builder, unary.Operand, toPath, flags));
				}

				case MethodCallExpression mc:
				{
					return mc.Update(mc.Object, mc.Arguments.Select(a => RemapToNewPath(builder, a, toPath, flags)));
				}

				case SqlAdjustTypeExpression adjust:
				{
					return adjust.Update(RemapToNewPath(builder, adjust.Expression, toPath, flags));
				}

				case SqlEagerLoadExpression eager:
					return eager;

				case DefaultExpression or DefaultValueExpression:
					return expression;
			}

			if (flags.IsExpression())
			{
				if (expression is DefaultValueExpression)
				{
					return expression;
				}

				if (!expression.Type.IsValueType)
				{
					if (expression is DefaultExpression)
					{
						return expression;
					}

					if (expression is ConstantExpression constant && constant.Value == null)
					{
						return expression;
					}
				}
			}

			return toPath;
		}

		#region ReplaceContext

		public static Expression ReplaceContext(Expression expression, IBuildContext current, IBuildContext onContext)
		{
			using var visitor = _replaceContextVisitorPool.Allocate();

			return visitor.Value.ReplaceContext(expression, current, onContext);
		}

		static readonly ObjectPool<ReplaceContextVisitor> _replaceContextVisitorPool = new(() => new ReplaceContextVisitor(), v => v.Cleanup(), 100);

		sealed class ReplaceContextVisitor : ExpressionVisitorBase
		{
			IBuildContext _current   = null!;
			IBuildContext _onContext = null!;

			public Expression ReplaceContext(Expression expression, IBuildContext current, IBuildContext onContext)
			{
				_current   = current;
				_onContext = onContext;

				return Visit(expression);
			}

			public override void Cleanup()
			{
				_current   = null!;
				_onContext = null!;

				base.Cleanup();
			}

			protected override Expression VisitUnary(UnaryExpression node)
			{
				if (node.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					if (node.Operand is ContextRefExpression contextOperand)
					{
						return Visit(contextOperand.WithType(node.Type));
					}
				}

				return base.VisitUnary(node);
			}

			internal override Expression VisitContextRefExpression(ContextRefExpression node)
			{
				if (node.BuildContext == _current)
					return new ContextRefExpression(node.Type, _onContext, node.Alias);

				return node;
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				if (node.TrackingPath != null)
					return node.WithTrackingPath(Visit(node.TrackingPath));

				return node;
			}
		}

		#endregion

		public static Expression CorrectSelectQuery(Expression expression, SelectQuery selectQuery)
		{
			var newExpression = expression.Transform((expression, selectQuery), (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlPlaceholderExpression sqlPlaceholderExpression)
				{
					return sqlPlaceholderExpression.WithSelectQuery(ctx.selectQuery);
				}

				return e;
			});

			return newExpression;
		}

		public static ISqlExpression UnwrapNullability(ISqlExpression expression)
		{
			while (expression is SqlNullabilityExpression nullability)
			{
				expression = nullability.SqlExpression;
			}

			return expression;
		}

		public static Expression MoveToScopedContext(Expression expression, IBuildContext upTo)
		{
			var scoped        = new ScopeContext(upTo, upTo);
			var newExpression = ReplaceContext(expression, upTo, scoped);
			return newExpression;
		}

		public static ITableContext? GetTableOrCteContext(ExpressionBuilder builder, Expression pathExpression)
		{
			var rootContext = builder.BuildTableExpression(pathExpression) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as ITableContext;

			return tableContext;
		}

		public static TableBuilder.TableContext? GetTableContext(ExpressionBuilder builder, Expression pathExpression)
		{
			var rootContext = builder.BuildTableExpression(pathExpression) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as TableBuilder.TableContext;

			return tableContext;
		}

		public static TableBuilder.TableContext? GetTableContext(IBuildContext context)
		{
			var rootContext = context.Builder.BuildTableExpression(CreateRef(context)) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as TableBuilder.TableContext;

			return tableContext;
		}

		public static ITableContext? GetTableOrCteContext(IBuildContext context)
		{
			var rootContext = context.Builder.BuildTableExpression(CreateRef(context)) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as ITableContext;

			return tableContext;
		}

		public static bool IsSqlReady(Expression expression)
		{
			if (expression.Find(e => e is SqlErrorExpression or SqlEagerLoadExpression or ContextRefExpression) != null)
				return false;
			return true;
		}

		public static void EnsureNoErrors(Expression expression)
		{
			var found = FindError(expression);
			if (found != null)
			{
				throw found.CreateException();
			}
		}

		public static bool HasError(Expression expression)
		{
			return FindError(expression) != null;
		}

		public static SqlErrorExpression? FindError(Expression expression)
		{
			var found = expression.Find(e => e is SqlErrorExpression) as SqlErrorExpression;
			return found;
		}

		static IBuildContext UnwrapSubqueryContext(IBuildContext context)
		{
			var current = context;
			while (true)
			{
				if (current is SubQueryContext sc)
				{
					current = sc.SubQuery;
				}
				else if (current is PassThroughContext pass)
				{
					current = pass.Context;
				}
				else
					break;
			}

			return current;
		}

		public static DefaultIfEmptyBuilder.DefaultIfEmptyContext? GetDefaultIfEmptyContext(IBuildContext context)
		{
			return UnwrapSubqueryContext(context) as DefaultIfEmptyBuilder.DefaultIfEmptyContext;
		}

		public static Expression UnwrapDefaultIfEmpty(Expression expression)
		{
			if (expression is SqlDefaultIfEmptyExpression defaultIfEmptyExpression)
				return UnwrapDefaultIfEmpty(defaultIfEmptyExpression.InnerExpression);

			return expression;
		}

		public static Expression UnwrapProxy(Expression expression)
		{
			if (expression is ContextRefExpression { BuildContext: IBuildProxy proxy })
				return UnwrapDefaultIfEmpty(proxy.InnerExpression);
			return expression;
		}
		public static Expression RemoveMarkers(Expression expression)
		{
			var result = expression.Transform(e => e is MarkerExpression marker ? marker.InnerExpression : e);
			return result;
		}

		public static LambdaExpression? GetArgumentLambda(MethodCallExpression methodCall, string argumentName)
		{
			var idx = Array.FindIndex(methodCall.Method.GetParameters(), a => string.Equals(a.Name, argumentName, StringComparison.Ordinal));
			if (idx < 0)
				return null;
			return methodCall.Arguments[idx].UnwrapLambda();
		}

		//TODO: I don't like this. Hints are like mess. Quick workaround before review
		public static QueryExtensionBuilder.JoinHintContext? GetJoinHintContext(IBuildContext context)
		{
			return context switch
			{
				QueryExtensionBuilder.JoinHintContext hintContext => hintContext,
				PassThroughContext pt => GetJoinHintContext(pt.Context),
				SubQueryContext sc => GetJoinHintContext(sc.SubQuery),
				DefaultIfEmptyBuilder.DefaultIfEmptyContext di => GetJoinHintContext(di.Sequence),
				_ => null,
			};
		}

		public static Expression MakeNotNullCondition(Expression expr)
		{
			if (!expr.Type.IsNullableOrReferenceType)
			{
				expr = expr is SqlPlaceholderExpression placeholder
					? placeholder.MakeNullable()
					: Expression.Convert(expr, expr.Type.AsNullable());
			}

			return Expression.NotEqual(expr, Expression.Default(expr.Type));
		}

		public static bool GetIsOptional(BuildInfo buildInfo)
		{
			if (!buildInfo.IsSubQuery)
				return false;

			if (buildInfo.SourceCardinality.HasFlag(SourceCardinality.Zero))
				return true;

			return false;
		}

		public static Expression UnwrapConstantAndParameter(Expression expression)
		{
			if (expression is MethodCallExpression mc && (mc.IsSameGenericMethod(Methods.LinqToDB.SqlParameter) || mc.IsSameGenericMethod(Methods.LinqToDB.SqlConstant)))
			{
				return UnwrapConstantAndParameter(mc.Arguments[0]);
			}

			return expression;
		}

		public static Expression WrapAsParameter(Expression expression)
		{
			if (expression is MethodCallExpression mc && mc.IsSameGenericMethod(Methods.LinqToDB.SqlParameter))
			{
				return expression;
			}

			var unwrapped = UnwrapConstantAndParameter(expression);

			return Expression.Call(Methods.LinqToDB.SqlParameter.MakeGenericMethod(unwrapped.Type), unwrapped);
		}

		public static IBuildContext? GetOrderSequence(IBuildContext context)
		{
			var prevSequence = context;
			while (true)
			{
				if (prevSequence.SelectQuery.Select.HasModifier)
				{
					return null;
				}

				if (!prevSequence.SelectQuery.OrderBy.IsEmpty)
					break;

				if (prevSequence is SubQueryContext { IsSelectWrapper: true } subQuery)
				{
					prevSequence = subQuery.SubQuery;
				}
				else if (prevSequence is SelectContext { InnerContext: not null } selectContext)
				{
					prevSequence = selectContext.InnerContext;
				}
				else
					break;
			}

			return prevSequence.SelectQuery.OrderBy.IsEmpty ? null : prevSequence;
		}

		#region Special fields helpers

		public static MemberExpression CreateSpecialProperty(Expression obj, Type type, string name)
		{
			return Expression.MakeMemberAccess(obj, new SpecialPropertyInfo(obj.Type, type, name));
		}

		public static bool IsSpecialProperty(Expression expression, Type type, string propName)
		{
			if (expression.Type != type)
				return false;

			if (expression is not MemberExpression memberExpression)
				return false;

			if (memberExpression.Member is not SpecialPropertyInfo)
				return false;

			if (!string.Equals(memberExpression.Member.Name, propName, StringComparison.Ordinal))
				return false;

			return true;
		}

		public static bool IsSpecialProperty(Expression expression, [NotNullWhen(true)] out Type? type, [NotNullWhen(true)] out string? propName)
		{
			type     = null;
			propName = null;

			if (expression is not MemberExpression memberExpression)
				return false;

			if (memberExpression.Member is not SpecialPropertyInfo)
				return false;

			type     = expression.Type;
			propName = memberExpression.Member.Name;

			return true;
		}

		public static bool IsSpecialProperty(Expression expression, IBuildContext context)
		{
			if (expression is not MemberExpression memberExpression)
				return false;

			if (memberExpression.Member is not SpecialPropertyInfo)
				return false;

			if (memberExpression.Expression is not ContextRefExpression contextRef || !ReferenceEquals(contextRef.BuildContext, context))
				return false;

			return true;
		}

		public static MemberExpression ChangeSpecialPropertyObject(Expression expression, IBuildContext context)
		{
			if (expression is not MemberExpression memberExpression)
				throw new InvalidOperationException("Expression is not a member access");

			if (memberExpression.Member is not SpecialPropertyInfo)
				throw new InvalidOperationException("Member is not a special property");

			if (memberExpression.Expression is not ContextRefExpression contextRef)
				throw new InvalidOperationException("Member expression is not based on a context reference");

			return CreateSpecialProperty(CreateRef(context), memberExpression.Type, memberExpression.Member.Name);
		}

		#endregion
	}
}
