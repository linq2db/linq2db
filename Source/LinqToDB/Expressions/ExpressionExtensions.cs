using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Internal.Reflection;

namespace LinqToDB.Expressions
{
	public static class ExpressionExtensions
	{
		#region GetDebugView

		private static Func<Expression,string>? _getDebugView;

		/// <summary>
		/// Gets the DebugView internal property value of provided expression.
		/// </summary>
		/// <param name="expression">Expression to get DebugView.</param>
		/// <returns>DebugView value.</returns>
		[DynamicDependency("get_DebugView", typeof(Expression))]
		public static string GetDebugView(this Expression expression)
		{
			if (_getDebugView == null)
			{
				var p = Expression.Parameter(typeof(Expression));

				try
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						ExpressionHelper.PropertyOrField(p, "DebugView"),
						p);

					_getDebugView = l.CompileExpression();
				}
				catch (ArgumentException)
				{
					_getDebugView = e => e.ToString();
				}
			}

			return _getDebugView(expression);
		}

		#endregion

		#region GetCount

		/// <summary>
		/// Returns the total number of expression items which are matching the given.
		/// <paramref name="func"/>.
		/// </summary>
		/// <param name="expr">Expression-Tree which gets counted.</param>
		/// <param name="context">Expression-Tree visitor context.</param>
		/// <param name="func">Predicate which is used to test if the given expression should be counted.</param>
		public static int GetCount<TContext>(this Expression expr, TContext context, Func<TContext, Expression, bool> func)
		{
			var ctx = new CountContext<TContext>(context, func);

			expr.Visit(ctx, static (context, e) =>
			{
				if (context.Func(context.Context, e))
					context.Count++;
			});

			return ctx.Count;
		}

		private sealed class CountContext<TContext>
		{
			public CountContext(TContext context, Func<TContext, Expression, bool> func)
			{
				Context = context;
				Func    = func;
			}

			public readonly TContext                         Context;
			public          int                              Count;
			public readonly Func<TContext, Expression, bool> Func;
		}

		#endregion

		#region Visit
		/// <summary>
		/// Calls the given <paramref name="func"/> for each child node of the <paramref name="expr"/>.
		/// </summary>
		public static void Visit<TContext>(this Expression expr, TContext context, Action<TContext, Expression> func)
			where TContext : notnull
		{
			if (expr == null)
				return;

			using var visitor = VisitActionVisitor<TContext>.Pool.Allocate();
			visitor.Value.VisitAction(expr, context, func);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each child node of the <paramref name="expr"/>.
		/// </summary>
		public static void Visit(this Expression expr, Action<Expression> func)
		{
			if (expr == null)
				return;

			using var visitor = VisitActionVisitor.Pool.Allocate();
			visitor.Value.VisitAction(expr, func);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each node of the <paramref name="expr"/>.
		/// If the <paramref name="func"/> returns false, no childs of the tested expression will be enumerated.
		/// </summary>
		public static void Visit<TContext>(this Expression expr, TContext context, Func<TContext, Expression, bool> func)
			where TContext : notnull
		{
			if (expr == null || !func(context, expr))
				return;

			using var visitor = VisitFuncVisitor<TContext>.Pool.Allocate();
			visitor.Value.VisitFunc(expr, context, func);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each node of the <paramref name="expr"/>.
		/// If the <paramref name="func"/> returns false, no childs of the tested expression will be enumerated.
		/// </summary>
		public static void Visit(this Expression expr, Func<Expression, bool> func)
		{
			if (expr == null || !func(expr))
				return;

			using var visitor = VisitFuncVisitor.Pool.Allocate();
			visitor.Value.VisitFunc(expr, func);
		}

		#endregion

		#region Find

		/// <summary>
		/// Enumerates the expression tree and returns the <paramref name="exprToFind"/> if it's
		/// contained within the <paramref name="expr"/>.
		/// </summary>
		public static Expression? Find(this Expression? expr, Expression exprToFind)
		{
			return expr.Find(exprToFind, static (exprToFind, e) => e == exprToFind);
		}

		/// <summary>
		/// Enumerates the expression tree and returns the <paramref name="exprToFind"/> if it's
		/// contained within the <paramref name="expr"/>.
		/// </summary>
		public static Expression? Find(this Expression? expr, Expression exprToFind, IEqualityComparer<Expression> comparer)
		{
			return expr.Find((exprToFind, comparer), static (ctx, e) => ctx.comparer.Equals(e, ctx.exprToFind));
		}

		/// <summary>
		/// Enumerates the given <paramref name="expr"/> and returns the first sub-expression
		/// which matches the given <paramref name="func"/>. If no expression was found, null is returned.
		/// </summary>
		public static Expression? Find<TContext>(this Expression? expr, TContext context, Func<TContext,Expression, bool> func)
			where TContext : notnull
		{
			if (expr == null)
				return expr;

			using var visitor = FindVisitor<TContext>.Pool.Allocate();
			return visitor.Value.Find(expr, context, func);
		}

		/// <summary>
		/// Enumerates the given <paramref name="expr"/> and returns the first sub-expression
		/// which matches the given <paramref name="func"/>. If no expression was found, null is returned.
		/// </summary>
		public static Expression? Find(this Expression? expr, Func<Expression, bool> func)
		{
			if (expr == null)
				return expr;

			using var visitor = FindVisitor.Pool.Allocate();
			return visitor.Value.Find(expr, func);
		}

		#endregion

		#region Transform

		public static Expression Replace(this Expression expression, Expression toReplace, Expression replacedBy)
		{
			return expression.Transform(
				(toReplace, replacedBy),
				static (context, e) => e == context.toReplace ? context.replacedBy : e);
		}

		public static Expression Replace(this Expression expression, Expression toReplace, Expression replacedBy, IEqualityComparer<Expression> equalityComparer)
		{
			return expression.Transform(
				(toReplace, replacedBy, equalityComparer),
				static (context, e) => context.equalityComparer.Equals(e, context.toReplace) ? context.replacedBy : e);
		}

		public static Expression Replace(this Expression expression, IReadOnlyDictionary<Expression, Expression> replaceMap)
		{
			return expression.Transform(
				replaceMap,
				static (map, e) => map.TryGetValue(e, out var newExpression) ? newExpression : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first parameter of that
		/// lambda expression with the <paramref name="exprToReplaceParameter"/> expression.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter)
		{
			return lambda.Body.Transform(
				(parameter: lambda.Parameters[0], exprToReplaceParameter),
				static (context, e) => e == context.parameter ? context.exprToReplaceParameter : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first two parameters of
		/// that lambda expression with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2)
		{
			return lambda.Body.Transform(
				(parameters: lambda.Parameters, exprToReplaceParameter1, exprToReplaceParameter2),
				static (context, e) =>
					e == context.parameters[0] ? context.exprToReplaceParameter1 :
					e == context.parameters[1] ? context.exprToReplaceParameter2 : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first three parameters of
		/// that lambda expression with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2, Expression exprToReplaceParameter3)
		{
			return lambda.Body.Transform(
				(parameters: lambda.Parameters, exprToReplaceParameter1, exprToReplaceParameter2, exprToReplaceParameter3),
				static (context, e) =>
					e.NodeType != ExpressionType.Parameter ? e                               :
					e == context.parameters[0]             ? context.exprToReplaceParameter1 :
					e == context.parameters[1]             ? context.exprToReplaceParameter2 :
					e == context.parameters[2]             ? context.exprToReplaceParameter3 : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces all parameters
		/// with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, params Expression[] replacement)
		{
			return lambda.Body.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = lambda.Parameters.IndexOf((ParameterExpression)e);
					if (idx >= 0 && idx < replacement.Length)
						return replacement[idx];
				}

				return e;
			});
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces all parameters
		/// with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, ReadOnlyCollection<Expression> replacement)
		{
			return lambda.Body.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					var idx = lambda.Parameters.IndexOf((ParameterExpression)e);
					if (idx >= 0 && idx < replacement.Count)
						return replacement[idx];
				}

				return e;
			});
		}

		/// <summary>
		/// Enumerates the expression tree of <paramref name="expr"/> and might
		/// replace expression with the returned value of the given <paramref name="func"/>.
		/// </summary>
		/// <returns>The modified expression.</returns>
		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform<TContext>(this Expression? expr, TContext context, [InstantHandle] Func<TContext, Expression, Expression> func)
			where TContext : notnull
		{
			if (expr == null)
				return null;

			using var visitor = TransformVisitor<TContext>.Pool.Allocate();
			return visitor.Value.Transform(expr, context, func);
		}

		/// <summary>
		/// Enumerates the expression tree of <paramref name="expr"/> and might
		/// replace expression with the returned value of the given <paramref name="func"/>.
		/// </summary>
		/// <returns>The modified expression.</returns>
		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform(this Expression? expr, [InstantHandle] Func<Expression, Expression> func)
		{
			if (expr == null)
				return null;

			using var visitor = TransformVisitor.Pool.Allocate();
			return visitor.Value.Transform(expr, func);
		}

		#endregion

		#region Transform2

		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform<TContext>(this Expression? expr, TContext context, Func<TContext, Expression, TransformInfo> func)
			where TContext : notnull
		{
			if (expr == null)
				return null;

			using var visitor = TransformInfoVisitor<TContext>.Pool.Allocate();
			return visitor.Value.Transform(expr, context, func);
		}

		[return: NotNullIfNotNull(nameof(expr))]
		public static Expression? Transform(this Expression? expr, Func<Expression, TransformInfo> func)
		{
			if (expr == null)
				return null;

			using var visitor = TransformInfoVisitor.Pool.Allocate();
			return visitor.Value.Transform(expr, func);
		}

		#endregion

		public static Expression GetMemberGetter(MemberInfo mi, Expression obj)
		{
			return mi switch
			{
				DynamicColumnInfo => Expression.Call(
					Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(mi.GetMemberType()),
					obj,
					Expression.Constant(mi.Name)
				),

				_ => Expression.MakeMemberAccess(obj, mi),
			};
		}

		internal static Expression EnsureType(this Expression expression, Type type)
		{
			if (expression.Type == type)
				return expression;

			return Expression.Convert(expression, type);
		}

		internal static Expression EnsureType<T>(this Expression expression)
		{
			if (expression.Type == typeof(T))
				return expression;

			return Expression.Convert(expression, typeof(T));
		}
	}
}
