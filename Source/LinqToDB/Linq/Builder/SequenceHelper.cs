using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

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
			return expression == null ||
			       (expression is ContextRefExpression contextRef && contextRef.BuildContext == context);
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CorrectExpression(Expression? expression, IBuildContext current,
			IBuildContext                                       underlying)
		{
			if (expression != null)
			{
				return ReplaceContext(expression, current, underlying);
				/*var root = current.Builder.GetRootObject(expression);
				if (root is ContextRefExpression refExpression)
				{
					if (refExpression.BuildContext == current)
					{
						expression = expression.Replace(root, new ContextRefExpression(root.Type, underlying), EqualityComparer<Expression>.Default);
					}
				}*/
			}

			return expression;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CorrectTrackingPath(Expression? expression, IBuildContext toContext)
		{
			if (expression == null || expression.Find(1, (_, e) => e is SqlPlaceholderExpression) == null)
				return expression;

			var contextRef = new ContextRefExpression(expression.Type, toContext);

			var transformed = CorrectTrackingPath(expression, contextRef);

			return transformed;
		}

		public static Expression CorrectTrackingPath(Expression expression, Expression toPath)
		{
			return CorrectTrackingPath(expression, null, toPath);
		}


		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CorrectTrackingPath(Expression? expression, Expression? except, Expression toPath)
		{
			if (expression == null)
				return null;

			if (toPath is not ContextRefExpression && toPath is not MemberExpression)
				return expression;

			if (expression is SqlGenericConstructorExpression generic)
			{
				List<SqlGenericConstructorExpression.Assignment>? assignments = null;

				var contextRef = toPath as ContextRefExpression;

				for (int i = 0; i < generic.Assignments.Count; i++)
				{
					var assignment = generic.Assignments[i];

					var currentPath = toPath;
					if (contextRef != null && assignment.MemberInfo.DeclaringType != null && !assignment.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
					{
						currentPath = contextRef.WithType(assignment.MemberInfo.DeclaringType);
					}

					var newExpression = CorrectTrackingPath(assignment.Expression,
						Expression.MakeMemberAccess(currentPath, assignment.MemberInfo));

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
					return generic.ReplaceAssignments(assignments);
				}

				return generic;
			}

			if (expression is NewExpression or MemberInitExpression)
			{
				return CorrectTrackingPath(SqlGenericConstructorExpression.Parse(expression), toPath);
			}

			if (expression is SqlPlaceholderExpression placeholder)
			{
				if (!placeholder.Type.IsAssignableFrom(toPath.Type))
				{
					toPath = Expression.MakeMemberAccess(toPath, ((MemberExpression)placeholder.Path).Member);
				}
				return placeholder.WithTrackingPath(toPath);
			}

			/*
			if (expression == except)
				return expression;

			var transformed = expression.Transform(toPath, (t, e) => CorrectTrackingPath(e, e, t));
			return transformed;
			*/

			if (expression is ConstantExpression)
				return expression;

			if (expression is ConditionalExpression conditional)
			{
				return conditional.Update(
					CorrectTrackingPath(conditional.Test, toPath),
					CorrectTrackingPath(conditional.IfTrue, toPath),
					CorrectTrackingPath(conditional.IfFalse, toPath));
			}

			if (expression is ContextConstructionExpression construct)
			{
				return construct.Update(construct.BuildContext, CorrectTrackingPath(construct.InnerExpression, toPath));
			}

			if (expression is BinaryExpression binary)
			{
				return binary.Update(CorrectTrackingPath(binary.Left, toPath), binary.Conversion, CorrectTrackingPath(binary.Right, toPath));
			}

			if (expression is UnaryExpression unary)
			{
				return unary.Update(CorrectTrackingPath(unary.Operand, toPath));
			}

			return expression;
		}


		public static Expression ReplacePlaceholdersByTrackingPath(Expression expression)
		{
			var transformed = expression.Transform(e =>
			{
				if (e is SqlPlaceholderExpression { TrackingPath: { } } placeholder)
				{
					var path = placeholder.TrackingPath;
					if (e.Type != path.Type)
						path = Expression.Convert(path, e.Type);
					return path;
				}

				return e;
			});

			return transformed;
		}

		[return: NotNullIfNotNull(nameof(expression))]	
		public static Expression? RemapToNewPath(IBuildContext buildContext, Expression? expression, Expression toPath, ProjectFlags flags)
		{
			if (expression == null)
				return null;

			if (toPath is not ContextRefExpression && toPath is not MemberExpression && toPath is not SqlGenericParamAccessExpression)
				return expression;

			if (expression is SqlGenericConstructorExpression generic)
			{
				List<SqlGenericConstructorExpression.Assignment>? assignments = null;
				List<SqlGenericConstructorExpression.Parameter>? parameters = null;

				var contextRef = toPath as ContextRefExpression;

				for (int i = 0; i < generic.Assignments.Count; i++)
				{
					var assignment = generic.Assignments[i];

					var currentPath = toPath;
					if (contextRef != null && assignment.MemberInfo.DeclaringType != null && !assignment.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
					{
						currentPath = contextRef.WithType(assignment.MemberInfo.DeclaringType);
					}

					var newExpression = RemapToNewPath(buildContext, assignment.Expression,
						Expression.MakeMemberAccess(currentPath, assignment.MemberInfo), flags);

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
					if (contextRef != null && parameter.MemberInfo?.DeclaringType != null && !parameter.MemberInfo.DeclaringType.IsAssignableFrom(contextRef.Type))
					{
						currentPath = contextRef.WithType(parameter.MemberInfo.DeclaringType);
					}

					var paramAccess = new SqlGenericParamAccessExpression(currentPath, parameter.ParameterInfo);

					var newExpression = RemapToNewPath(buildContext, parameter.Expression, paramAccess, flags);

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
					generic = generic.ReplaceAssignments(assignments);
				}

				if (parameters != null)
				{
					generic = generic.ReplaceParameters(parameters);
				}

				generic = generic.WithConstructionRoot(toPath);

				return generic;
			}

			if (expression is NewExpression or MemberInitExpression)
			{
				return RemapToNewPath(buildContext, SqlGenericConstructorExpression.Parse(expression), toPath, flags);
			}

			if (expression is ContextConstructionExpression contextConstructionExpression)
			{
				return contextConstructionExpression.Update(buildContext,
					RemapToNewPath(buildContext, contextConstructionExpression.InnerExpression, toPath, flags));
			}

			if (expression is SqlPlaceholderExpression placeholder)
			{
				if (placeholder.Type == toPath.Type)
				{
					return toPath;
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
			}

			if (expression is BinaryExpression binary && toPath.Type != binary.Type)
			{
				var left  = binary.Left;
				var right = binary.Right;

				if (left is SqlPlaceholderExpression)
				{
					left = RemapToNewPath(buildContext, left, toPath, flags);
				}

				if (right is SqlPlaceholderExpression)
				{
					right = RemapToNewPath(buildContext, right, toPath, flags);
				}

				return binary.Update(left, binary.Conversion, right);
			}

			if (expression is ConditionalExpression conditional)
			{
				return conditional.Update(RemapToNewPath(buildContext, conditional.Test, toPath, flags),
					RemapToNewPath(buildContext, conditional.IfTrue, toPath, flags),
					RemapToNewPath(buildContext, conditional.IfFalse, toPath, flags));
			}

			if (expression.NodeType == ExpressionType.Convert)
			{
				var unary = (UnaryExpression)expression;
				return unary.Update(RemapToNewPath(buildContext, unary.Operand, toPath, flags));
			}

			if (expression is MethodCallExpression mc)
			{
				return mc.Update(mc.Object, mc.Arguments.Select(a => RemapToNewPath(buildContext, a, toPath, flags)));
			}

			if (expression is SqlAdjustTypeExpression adjust)
			{
				return adjust.Update(RemapToNewPath(buildContext, adjust.Expression, toPath, flags));
			}

			if (expression is SqlEagerLoadExpression eager)
			{
				return eager;
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


		public static Expression ReplaceContext(Expression expression, IBuildContext current, IBuildContext onContext)
		{
			var newExpression = expression.Transform((expression, current, onContext), (ctx, e) =>
			{
				if (e is ContextRefExpression contextRef)
				{
					if (contextRef.BuildContext == ctx.current)
						return new ContextRefExpression(contextRef.Type, ctx.onContext, contextRef.Alias);
				}

				if (e is SqlPlaceholderExpression { TrackingPath: { } } placeholder)
				{
					return placeholder.WithTrackingPath(ReplaceContext(placeholder.TrackingPath, ctx.current,
						ctx.onContext));
				}

				return e;
			});

			return newExpression;
		}

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

		public static Expression MoveAllToDefaultIfEmptyContext(Expression expression)
		{
			if (expression is ContextRefExpression)
				return expression;

			var newExpression = expression.Transform((expression), (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension)
				{
					if (e is ContextRefExpression contextRef)
					{
						if (contextRef.BuildContext is DefaultIfEmptyBuilder.DefaultIfEmptyContext)
						{
							return e;
						}

						return contextRef.WithContext(new DefaultIfEmptyBuilder.DefaultIfEmptyContext(null, contextRef.BuildContext, null, false));
					}
				}

				return e;
			});

			return newExpression;
		}

		public static Expression MoveAllToScopedContext(Expression expression, IBuildContext upTo)
		{
			if (expression is ContextRefExpression)
				return expression;

			var newExpression = expression.Transform((expression, upTo), (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension)
				{
					if (e is ContextRefExpression contextRef)
					{
						if (contextRef.BuildContext == upTo || contextRef.BuildContext.SelectQuery == upTo.SelectQuery)
						{
							return e;
						}

						// already correctly scoped
						if (contextRef.BuildContext is ScopeContext scopeContext && scopeContext.UpTo == ctx.upTo)
						{
							return e;
						}

						return contextRef.WithContext(new ScopeContext(contextRef.BuildContext, ctx.upTo));
					}
				}

				return e;
			});

			return newExpression;
		}

		public static Expression MoveToScopedContext(Expression expression, IBuildContext upTo)
		{
			var scoped        = new ScopeContext(upTo, upTo);
			var newExpression = ReplaceContext(expression, upTo, scoped);
			return newExpression;
		}

		public static ITableContext? GetTableOrCteContext(ExpressionBuilder builder, Expression pathExpression)
		{
			var rootContext = builder.MakeExpression(null, pathExpression, ProjectFlags.Table) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as ITableContext;

			return tableContext;
		}

		public static TableBuilder.TableContext? GetTableContext(IBuildContext context)
		{
			var contextRef = new ContextRefExpression(context.ElementType, context);

			var rootContext =
				context.Builder.MakeExpression(context, contextRef, ProjectFlags.Table) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as TableBuilder.TableContext;

			return tableContext;
		}

		public static TableBuilder.CteTableContext? GetCteContext(IBuildContext context)
		{
			var contextRef = new ContextRefExpression(context.ElementType, context);

			var rootContext =
				context.Builder.MakeExpression(context, contextRef, ProjectFlags.Table) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as TableBuilder.CteTableContext;

			return tableContext;
		}

		public static ITableContext? GetTableOrCteContext(IBuildContext context)
		{
			var contextRef = new ContextRefExpression(context.ElementType, context);

			var rootContext =
				context.Builder.MakeExpression(context, contextRef, ProjectFlags.Table) as ContextRefExpression;

			var tableContext = rootContext?.BuildContext as ITableContext;

			return tableContext;
		}


		public static IBuildContext UnwrapSubqueryContext(IBuildContext context)
		{
			while (context is SubQueryContext sc)
			{
				context = sc.SubQuery;
			}

			return context;
		}

		public static bool IsDefaultIfEmpty(IBuildContext context)
		{
			return UnwrapSubqueryContext(context) is DefaultIfEmptyBuilder.DefaultIfEmptyContext;
		}

		public static Expression RequireSqlExpression(this IBuildContext context, Expression? path)
		{
			var sql = context.Builder.MakeExpression(context, path, ProjectFlags.SQL);
			if (sql == null)
			{
				throw new LinqException("'{0}' cannot be converted to SQL.", path);
			}

			return sql;
		}

		public static LambdaExpression? GetArgumentLambda(MethodCallExpression methodCall, string argumentName)
		{
			var idx = Array.FindIndex(methodCall.Method.GetParameters(), a => a.Name == argumentName);
			if (idx < 0)
				return null;
			return methodCall.Arguments[idx].UnwrapLambda();
		}

	}
}
