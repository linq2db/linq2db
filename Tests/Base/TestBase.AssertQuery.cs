using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Reflection;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

namespace Tests
{
	partial class TestBase
	{
		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CheckForNull(Expression? expression)
		{
			if (expression != null && expression.NodeType.NotIn(ExpressionType.Lambda, ExpressionType.Quote) &&
			    (expression.Type.IsClass || expression.Type.IsInterface) &&
			    !typeof(IQueryable<>).IsSameOrParentOf(expression.Type))
			{
				var test = Expression.ReferenceEqual(expression,
					Expression.Constant(null, expression.Type));
				return Expression.Condition(test,
					Expression.Constant(DefaultValue.GetValue(expression.Type), expression.Type), expression);
			}

			return expression;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? CheckForNull(Expression? expression, Expression trueExpression, Expression falseExpression)
		{
			if (expression is MemberExpression me)
				expression = CheckForNull(me);

			if (expression != null && expression.NodeType.NotIn(ExpressionType.Lambda, ExpressionType.Quote) &&
			    (expression.Type.IsClass || expression.Type.IsInterface) &&
			    !typeof(IQueryable<>).IsSameOrParentOf(expression.Type))
			{
				var test = Expression.ReferenceEqual(expression,
					Expression.Constant(null, expression.Type));
				return Expression.Condition(test, trueExpression, falseExpression);
			}

			return expression;
		}

		public static List<Expression> GetMemberPath(Expression? expression)
		{
			var result = new List<Expression>();

			var current = expression;
			while (current != null)
			{
				var prev = current;
				switch (current.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						result.Add(current);
						current = ((MemberExpression)current).Expression;
						break;
					}
				}

				if (prev == current)
				{
					result.Add(current);
					break;
				}
			}

			result.Reverse();
			return result;
		}


		public static Expression CheckForNull(MemberExpression expr)
		{
			Expression? test = null;

			var path = GetMemberPath(expr);

			for (int i = 0; i < path.Count - 1; i++)
			{
				var objExpr = path[i];
				if (objExpr != null && (objExpr.Type.IsClass || objExpr.Type.IsInterface))
				{
					var currentTest = Expression.ReferenceEqual(objExpr,
						Expression.Constant(null, objExpr.Type));
					if (test == null)
						test = currentTest;
					else
					{
						test = Expression.OrElse(test, currentTest);
					}
				}
			}

			if (test == null)
				return expr;

			return Expression.Condition(test,
				Expression.Constant(DefaultValue.GetValue(expr.Type), expr.Type), expr);
		}

		static Expression ApplyNullCheck(Expression expr)
		{
			var newExpr = expr.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;

					if (!mc.Method.IsStatic && mc.Object != null)
					{
						var checkedMethod = CheckForNull(mc.Object, Expression.Constant(DefaultValue.GetValue(mc.Method.ReturnType), mc.Method.ReturnType), mc);
						return new TransformInfo(checkedMethod);
					}

					if (mc.Method.IsStatic && mc.Method.DeclaringType == typeof(Enumerable))
					{
						var arguments = mc.Arguments.Select(a => ApplyNullCheck(a)).ToList();
						mc = mc.Update(mc.Object, arguments);
						var firstArg = mc.Arguments[0];

						if (firstArg.Type.IsClass || firstArg.Type.IsInterface)
						{
							var checkedExpr = Expression.Condition(
								Expression.NotEqual(firstArg, Expression.Default(firstArg.Type)),
									mc,
									Expression.Default(mc.Type));

							return new TransformInfo(checkedExpr);
						}

						return new TransformInfo(mc);
					}
				}
				else if (e.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression)e;

					return new TransformInfo(CheckForNull(ma));
				}

				return new TransformInfo(e);
			})!;

			return newExpr;
		}

		static IEnumerable<T> UnionImpl<T>(IEnumerable<T> query1, IEnumerable<T> query2, IEqualityComparer<T> comparer)
		{
			return query1.AsEnumerable().Union(query2, comparer).AsQueryable();
		}

		static IEnumerable<T> UnionAllImpl<T>(IEnumerable<T> query1, IEnumerable<T> query2)
		{
			return query1.Concat(query2);
		}

		static IEnumerable<T> ExceptImpl<T>(IEnumerable<T> query1, IEnumerable<T> query2, IEqualityComparer<T> comparer)
		{
			return query1.AsEnumerable().Except(query2, comparer).AsQueryable();
		}

		static IEnumerable<T> ExceptAllImpl<T>(IEnumerable<T> query1, IEnumerable<T> query2, IEqualityComparer<T> comparer)
		{
			var items1 = query1.ToList();
			var items2 = query2.ToList();

			for (int i = 0; i < items1.Count; i++)
			{
				var item1 = items1[i];
				if (items2.Any(item2 => comparer.Equals(item1, item2)))
				{
					items1.RemoveAt(i);
					--i;
				}
			}

			return items1.AsQueryable();
		}

		static IEnumerable<T> IntersectImpl<T>(IEnumerable<T> query1, IEnumerable<T> query2, IEqualityComparer<T> comparer)
		{
			return query1.Intersect(query2, comparer);
		}

		static IEnumerable<T> IntersectAllImpl<T>(IEnumerable<T> query1, IEnumerable<T> query2, IEqualityComparer<T> comparer)
		{
			var items1 = query1.ToList();
			var items2 = query2.ToList();

			for (int i = 0; i < items1.Count; i++)
			{
				var item1 = items1[i];
				if (!items2.Any(item2 => comparer.Equals(item1, item2)))
				{
					items1.RemoveAt(i);
					--i;
				}
			}

			return items1;
		}

		public static MethodCallExpression RemapMethod(MethodCallExpression mc)
		{
			MethodCallExpression SetOperationRemap(MethodInfo genericMethodInfo)
			{
				MethodCallExpression newMethodCall;

				var elementType      = mc.Method.GetGenericArguments()[0];

				var methodIfo = genericMethodInfo.MakeGenericMethod(mc.Method.GetGenericArguments());
				if (methodIfo.GetParameters().Length == 3)
				{
					var equalityComparer = ComparerBuilder.GetEqualityComparer(elementType);

					var comparerExpr = Expression.Constant(equalityComparer,
						typeof(IEqualityComparer<>).MakeGenericType(elementType));

					newMethodCall = Expression.Call(methodIfo, mc.Arguments[0], mc.Arguments[1], comparerExpr);
				}
				else
					newMethodCall = Expression.Call(methodIfo, mc.Arguments[0], mc.Arguments[1]);

				if (typeof(IQueryable<>).IsSameOrParentOf(mc.Method.ReturnType))
				{
					newMethodCall = Expression.Call(Methods.Enumerable.AsQueryable.MakeGenericMethod(elementType),
						newMethodCall);
				}

				return newMethodCall;
			}

			if (mc.Method.Name == nameof(LinqExtensions.InnerJoin))
			{
				return TypeHelper.MakeMethodCall(Methods.Queryable.Where, mc.Arguments.ToArray());
			}

			if (mc.Method.Name == nameof(LinqExtensions.UnionAll))
			{
				return SetOperationRemap(MemberHelper.MethodOfGeneric<IQueryable<object>>(q => UnionAllImpl(q, q)));
			}

			if (mc.Method.Name == nameof(LinqExtensions.IntersectAll))
			{
				return SetOperationRemap(MemberHelper.MethodOfGeneric<IQueryable<object>>(q => IntersectAllImpl(q, q, null!)));
			}

			if (mc.Method.Name == nameof(LinqExtensions.ExceptAll))
			{
				return SetOperationRemap(MemberHelper.MethodOfGeneric<IQueryable<object>>(q => ExceptAllImpl(q, q, null!)));
			}

			if (mc.Method.Name == nameof(Queryable.Except))
			{
				return SetOperationRemap(MemberHelper.MethodOfGeneric<IQueryable<object>>(q => ExceptImpl(q, q, null!)));
			}

			if (mc.Method.Name == nameof(Queryable.Union))
			{
				return SetOperationRemap(MemberHelper.MethodOfGeneric<IQueryable<object>>(q => UnionImpl(q, q, null!)));
			}

			return mc;
		}

		public T[] AssertQuery<T>(IQueryable<T> query)
		{
			var expr   = query.Expression;
			var actual = query.ToArray();

			var loaded = new Dictionary<Type, Expression>();

			Expression RegisterLoaded(Type eType, Expression tableExpression)
			{
				if (!loaded.TryGetValue(eType, out var itemsExpression))
				{
					var newCall = TypeHelper.MakeMethodCall(Methods.Queryable.ToArray, tableExpression);
					using (new DisableLogging())
					{
						var items = newCall.EvaluateExpression();
						itemsExpression = Expression.Constant(items, eType.MakeArrayType());
						loaded.Add(eType, itemsExpression);
					}
				}

				var queryCall =
					TypeHelper.MakeMethodCall(Methods.Enumerable.AsQueryable,
						itemsExpression);

				return queryCall;
			}

			var newExpr = expr.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;

					if (mc.Method.Name == nameof(Methods.LinqToDB.AsSubQuery))
						return new TransformInfo(mc.Arguments[0], false, true);

					if (typeof(ITable<>).IsSameOrParentOf(mc.Type) || typeof(ILoadWithQueryable<,>).IsSameOrParentOf(mc.Type))
					{
						var entityType = mc.Method.ReturnType.GetGenericArguments()[0];

						if (entityType != null)
						{
							var itemsExpression = RegisterLoaded(entityType, mc);
							return new TransformInfo(itemsExpression);
						}
					}

					mc = RemapMethod(mc);

					return new TransformInfo(mc, false, true);
					//return mc.Update(CheckForNull(mc.Object), mc.Arguments.Select(CheckForNull));
				}
				else if (e.NodeType == ExpressionType.MemberAccess)
				{
					if (typeof(ITable<>).IsSameOrParentOf(e.Type))
					{
						var entityType = e.Type.GetGenericArguments()[0];
						var items = RegisterLoaded(entityType, e);
						return new TransformInfo(items);
					}
				}

				return new TransformInfo(e);
			})!;


			newExpr = ApplyNullCheck(newExpr);

			var empty = LinqToDB.Common.Tools.CreateEmptyQuery<T>();
			T[]? expected;

			expected = empty.Provider.CreateQuery<T>(newExpr).ToArray();

			if (actual.Length > 0 || expected.Length > 0)
				AreEqual(expected, actual, ComparerBuilder.GetEqualityComparer<T>());

			return actual;
		}

	}
}
