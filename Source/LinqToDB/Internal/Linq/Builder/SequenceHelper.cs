using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
		/// the same unit get equivalent converters that are not the same object. That substitution is sound because
		/// a declared unit is the only thing that can define the stored form - <see cref="ColumnDescriptor"/> refuses
		/// a column that states a unit and carries a hand-written converter as well, so equal units cannot hide
		/// unequal conversions.
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
			converter1 = StoredConversion(converter1);
			converter2 = StoredConversion(converter2);

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
		/// The converter that defines the stored form of a value, or <see langword="null"/> when the value is stored
		/// as is (no converter or <see cref="IStorageTransparentConverter"/>).
		/// </summary>
		public static IValueConverter? StoredConversion(IValueConverter? converter)
		{
			return converter is IStorageTransparentConverter ? null : converter;
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
			if (ExpressionEqualityComparer.Instance.Equals(lambda1, lambda2))
				return true;

			if (lambda1.Parameters.Count != 1 || lambda2.Parameters.Count != 1)
				return false;

			var parameter1 = lambda1.Parameters[0];
			var parameter2 = lambda2.Parameters[0];

			if (parameter1.Type.UnwrapNullableType()    != parameter2.Type.UnwrapNullableType()
				|| lambda1.ReturnType.UnwrapNullableType() != lambda2.ReturnType.UnwrapNullableType())
			{
				return false;
			}

			var common = Expression.Parameter(parameter1.Type.UnwrapNullableType(), "p");

			// The unwrapped parameter is what makes the two spellings of one conversion meet: standing for a
			// nullable value it leaves nothing behind that the plainly-typed side has no counterpart for. Some
			// bodies cannot be rebuilt around it - one that reads a member only Nullable<> has, or hands the value
			// to something that expects one - and then both sides keep the nullability they were written in, which
			// leaves them comparable to each other rather than to nothing.
			if (!TryCanonicalize(lambda1.Body, parameter1, common, out var body1)
				|| !TryCanonicalize(lambda2.Body, parameter2, common, out var body2))
			{
				// stripping a nullability cast can still leave a node whose operands no longer agree
				if (!TryCanonicalize(lambda1.Body, parameter1, AsWrittenIn(parameter1, common), out body1)
					|| !TryCanonicalize(lambda2.Body, parameter2, AsWrittenIn(parameter2, common), out body2))
				{
					return false;
				}
			}

			return ExpressionEqualityComparer.Instance.Equals(body1, body2);

			static Expression AsWrittenIn(ParameterExpression parameter, ParameterExpression common)
				=> parameter.Type == common.Type ? common : Expression.Convert(common, parameter.Type);
		}

		/// <summary>
		/// Rewrites a conversion body around <paramref name="replacement"/>, reporting whether the body survives it
		/// rather than throwing when it does not.
		/// </summary>
		static bool TryCanonicalize(Expression body, ParameterExpression parameter, Expression replacement, [NotNullWhen(true)] out Expression? result)
		{
			try
			{
				result = Canonicalize(body, parameter, replacement);
				return true;
			}
			catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
			{
				// Rebuilding an access against a type that does not have it - Property 'Int64 Value' is not
				// defined for type 'System.Int64' - or a lifted operator around an operand that is no longer
				// nullable says this body is written in terms of its own nullability.
				result = null;
				return false;
			}
		}

		/// <summary>
		/// Rewrites a conversion body so that two of them can be told apart by what they compute rather than by the
		/// nullability they were written against: the parameter becomes a shared one, and a conversion that only
		/// puts a value into or takes it out of <c>Nullable&lt;&gt;</c> is dropped.
		/// </summary>
		static Expression Canonicalize(Expression body, ParameterExpression parameter, Expression replacement)
		{
			return body.Transform(e =>
			{
				if (ReferenceEquals(e, parameter))
					return replacement;

				if (e is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked, Method: null } unary
					&& unary.Type.UnwrapNullableType() == unary.Operand.Type.UnwrapNullableType())
				{
					// Recursed rather than returned: the transform does not descend into a node the callback
					// replaced, so the parameter anywhere under a stripped conversion would keep its own
					// identity and two bodies that differ only there would compare unequal.
					return Canonicalize(unary.Operand, parameter, replacement);
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
					if (expr is SqlErrorExpression errorExpression)
						return errorExpression.WithType(type);
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

			if (toPath.UnwrapConvert() is not (ContextRefExpression or MemberExpression))
				return expression;

			switch (expression)
			{
				case SqlGenericConstructorExpression generic:
				{
					List<SqlGenericConstructorExpression.Assignment>? assignments = null;
					List<SqlGenericConstructorExpression.Parameter>?  parameters  = null;

					for (int i = 0; i < generic.Assignments.Count; i++)
					{
						var assignment = generic.Assignments[i];

						if (assignment.MemberInfo.DeclaringType?.IsAssignableFrom(toPath.Type) == false)
						{
							assignments?.Add(assignment);
							continue;
						}

						var newExpression = CorrectTrackingPath(builder, assignment.Expression, MemberPath(toPath, assignment.MemberInfo));

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
						var newExpression = parameter.Expression;

						if (parameter.MemberInfo != null)
						{
							newExpression = CorrectTrackingPath(builder, parameter.Expression, MemberPath(toPath, parameter.MemberInfo));
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

				// Only what can become the value at toPath is relabelled: an outer-joined value, the branches of a
				// conditional, either side of a coalesce and the operand of a conversion. The not-null columns that tell
				// whether an outer-joined row was found, a conditional's test and the operands of any other operation are
				// values of their own.
				case SqlDefaultIfEmptyExpression defaultIfEmptyExpression:
				{
					return defaultIfEmptyExpression.Update(
						CorrectTrackingPath(builder, defaultIfEmptyExpression.InnerExpression, toPath),
						defaultIfEmptyExpression.NotNullExpressions);
				}

				case ConstantExpression:
					return expression;

				case ConditionalExpression conditional:
				{
					return conditional.Update(
						conditional.Test,
						CorrectTrackingPath(builder, conditional.IfTrue, toPath),
						CorrectTrackingPath(builder, conditional.IfFalse, toPath));
				}

				case BinaryExpression { NodeType: ExpressionType.Coalesce } coalesce:
				{
					return coalesce.Update(CorrectTrackingPath(builder, coalesce.Left, toPath), coalesce.Conversion, CorrectTrackingPath(builder, coalesce.Right, toPath));
				}

				case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs } conversion:
				{
					return conversion.Update(CorrectTrackingPath(builder, conversion.Operand, ConvertBack(toPath, conversion)));
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

		/// <summary>
		/// Turns a <paramref name="path"/> to the result of <paramref name="conversion"/> into a path to its operand.
		/// </summary>
		/// <remarks>
		/// The operand's members are declared on the operand's type, so they can only be read from a path of that type.
		/// A conversion that widens the operand, to <c>T?</c>, a base class or an interface, is read back with a cast to
		/// the operand's type. Any other conversion leaves the path as it is.
		/// </remarks>
		internal static Expression ConvertBack(Expression path, UnaryExpression conversion)
		{
			var from = conversion.Operand.Type;
			var to   = conversion.Type;

			var widens = path.Type == to && to != from && to.IsAssignableFrom(from);

			return widens ? EnsureType(path, from) : path;
		}

		/// <summary>
		/// The tracking path of <paramref name="member"/> of the value at <paramref name="path"/>.
		/// </summary>
		/// <remarks>
		/// The member is read through a path of the type that declares it, so it gets one path however the value was
		/// typed on the way: a member declared on <c>Base</c> reads as <c>Ref&lt;Base&gt;.M</c> from
		/// <c>Ref&lt;Derived&gt;</c> too. Tracking paths are matched as keys, by a CTE's recursive fields among others.
		/// </remarks>
		internal static MemberExpression MemberPath(Expression path, MemberInfo member)
		{
			if (member.DeclaringType != null)
				path = EnsureType(path, member.DeclaringType);

			return Expression.MakeMemberAccess(path, member);
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

		/// <summary>
		/// Whether the expression projects a constructed object, seen through a <c>Convert</c> / <c>ConvertChecked</c>
		/// chain carrying no conversion operator, or a <c>SqlAdjustTypeExpression</c>. A set operation unifies branch
		/// projections of differing but assignable types by converting one of them, so a branch is a constructor with
		/// a conversion in front of it as often as a bare one. A <c>TypeAs</c> is <b>not</b> one of the node kinds
		/// seen through here - it is rewritten into a conversion earlier, while the projection is built.
		/// </summary>
		public static bool IsConstructorBranch(Expression expression)
		{
			return IsConstructorBranch(expression, out _);
		}

		/// <inheritdoc cref="IsConstructorBranch(Expression)"/>
		public static bool IsConstructorBranch(Expression expression, [NotNullWhen(true)] out SqlGenericConstructorExpression? constructor)
		{
			constructor = expression.UnwrapConvert() as SqlGenericConstructorExpression;

			return constructor != null;
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
