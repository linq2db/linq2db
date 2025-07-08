using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	internal static class SequenceHelper
	{
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
			return null != expression.Find(1, static (_, e) => e is ContextRefExpression);
		}

		public static Expression CorrectTrackingPath(ExpressionBuilder builder, Expression expression, Expression toPath)
		{
			return CorrectTrackingPath(builder, expression, null, toPath);
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
		public static Expression? CorrectTrackingPath(ExpressionBuilder builder, Expression? expression, Expression? except, Expression toPath)
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
					if (QueryHelper.IsNullValue(placeholder.Sql))
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

		static ObjectPool<ReplaceContextVisitor> _replaceContextVisitorPool = new(() => new ReplaceContextVisitor(), v => v.Cleanup(), 100);

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
			var contextRef = new ContextRefExpression(context.ElementType, context);

			var rootContext = context.Builder.BuildTableExpression(contextRef) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as TableBuilder.TableContext;

			return tableContext;
		}

		public static ITableContext? GetTableOrCteContext(IBuildContext context)
		{
			var contextRef = new ContextRefExpression(context.ElementType, context);

			var rootContext =
				context.Builder.BuildTableExpression(contextRef) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as ITableContext;

			return tableContext;
		}

		public static bool IsSqlReady(Expression expression)
		{
			if (expression.Find(1, (_, e) => e is SqlErrorExpression or SqlEagerLoadExpression or ContextRefExpression) != null)
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
			var found = expression.Find(1, (_, e) => e is SqlErrorExpression) as SqlErrorExpression;
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
			var idx = Array.FindIndex(methodCall.Method.GetParameters(), a => a.Name == argumentName);
			if (idx < 0)
				return null;
			return methodCall.Arguments[idx].UnwrapLambda();
		}

		//TODO: I don't like this. Hints are like mess. Quick workaround before review
		public static QueryExtensionBuilder.JoinHintContext? GetJoinHintContext(IBuildContext context)
		{
			if (context is QueryExtensionBuilder.JoinHintContext hintContext)
				return hintContext;
			if (context is PassThroughContext pt)
				return GetJoinHintContext(pt.Context);
			if (context is SubQueryContext sc)
				return GetJoinHintContext(sc.SubQuery);
			if (context is DefaultIfEmptyBuilder.DefaultIfEmptyContext di)
				return GetJoinHintContext(di.Sequence);

			return null;
		}

		public static Expression MakeNotNullCondition(Expression expr)
		{
			if (expr.Type.IsValueType && !expr.Type.IsNullable())
			{
				if (expr is SqlPlaceholderExpression placeholder)
					expr = placeholder.MakeNullable();
				else
					expr = Expression.Convert(expr, expr.Type.AsNullable());
			}

			return Expression.NotEqual(expr, Expression.Default(expr.Type));
		}

		public static bool GetIsOptional(BuildInfo buildInfo)
		{
			if (!buildInfo.IsSubQuery)
				return false;

			if ((buildInfo.SourceCardinality & SourceCardinality.Zero) != 0)
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

			if (memberExpression.Member.Name != propName)
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

		#endregion
	}
}
