using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Linq;
using LinqToDB.Reflection;
using LinqToDB.Tools.Comparers;

namespace Tests
{
	partial class TestBase
	{
		class ApplyNullPropagationVisitor : ExpressionVisitorBase
		{

			protected bool CanBeNull(Type type)
			{
				if (type.IsValueType)
					return false;
				return true;
			}

			protected bool CanBeNull(Expression expression)
			{
				if (expression.Type.IsValueType)
					return false;

				if (expression.NodeType == ExpressionType.Default)
					return true;

				if (expression.NodeType == ExpressionType.Constant)
				{
					return ((ConstantExpression)expression).Value == null;
				}

				if (expression.NodeType == ExpressionType.MemberInit || expression.NodeType == ExpressionType.New)
				{
					return false;
				}

				if (expression is MethodCallExpression mc)
				{
					return true;
				}

				return true;
			}

			protected bool IsLINQMethod(MethodInfo method)
			{
				return method.IsStatic && method.DeclaringType == typeof(Enumerable) || method.DeclaringType == typeof(Queryable);
			}

			[return: NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				return base.Visit(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				var newNode = node.Update(node.Object, VisitAndConvert<Expression>(node.Arguments, "VisitMethodCall"));

				if (newNode.Object != null)
				{
					if (CanBeNull(newNode.Object))
					{
						var checkedObs = Visit(newNode.Object);
						var resultNode = Expression.Condition(Expression.Equal(checkedObs, Expression.Constant(null, newNode.Object.Type)),
							Expression.Constant(DefaultValue.GetValue(newNode.Type), newNode.Type), newNode);

						return resultNode;
					}
				}
				else if (newNode.Method.IsStatic && IsLINQMethod(newNode.Method))
				{
					if (CanBeNull(node.Arguments[0]))
					{
						var checkedFirst = Visit(node.Arguments[0]);
						var resultNode = Expression.Condition(
							Expression.Equal(checkedFirst, Expression.Constant(null, newNode.Arguments[0].Type)),
							Expression.Constant(DefaultValue.GetValue(newNode.Type), newNode.Type), newNode);

						return resultNode;
					}
				}

				return newNode;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (node.Expression != null)
				{
					if (CanBeNull(node.Expression) || node.Member.IsNullableValueMember())
					{
						var checkedExoression = Visit(node.Expression);
						return Expression.Condition(Expression.Equal(checkedExoression, Expression.Constant(null, checkedExoression.Type)),
							Expression.Constant(DefaultValue.GetValue(node.Type), node.Type), node);
					}
				}

				return base.VisitMember(node);
			}
		}

		static Expression ApplyNullCheck(Expression expr)
		{
			var visitor = new ApplyNullPropagationVisitor();

			var newExpr = visitor.Visit(expr);

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

		static MethodCallExpression RemapMethod(MethodCallExpression mc)
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
					newMethodCall = Expression.Call(Methods.Queryable.AsQueryable.MakeGenericMethod(elementType),
						newMethodCall);
				}

				return newMethodCall;
			}

			if (mc.Method.Name == nameof(LinqExtensions.InnerJoin))
			{
				return TypeHelper.MakeMethodCall(Methods.Queryable.Where, mc.Arguments.ToArray());
			}

			if (mc.Method.Name == nameof(LinqExtensions.Having))
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

		protected T[] AssertQuery<T>(IQueryable<T> query, IEqualityComparer<T>? comparer = null)
		{
			var expr   = query.Expression;
			var actual = query.ToArray();

			Dictionary<Expression, Expression>? loadedTables = null;

			Expression RegisterLoaded(IDataContext dc, Expression tableExpression)
			{
				loadedTables ??= new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);

				if (!loadedTables.TryGetValue(tableExpression, out var itemsExpression))
				{
					var eType   = tableExpression.Type.GetGenericArguments()[0];
					var newCall = TypeHelper.MakeMethodCall(Methods.Queryable.ToArray, tableExpression);
					using (new DisableLogging())
					{
						var items = newCall.EvaluateExpression();
						itemsExpression = Expression.Constant(items, eType.MakeArrayType());
						loadedTables.Add(tableExpression, itemsExpression);
					}
				}

				var queryCall =
					TypeHelper.MakeMethodCall(Methods.Queryable.AsQueryable,
						itemsExpression);

				return queryCall;
			}

			var dc = Internals.GetDataContext(query);

			if (dc == null)
				throw new InvalidOperationException("Could not retrieve DataContext from IQueryable.");

			expr = Internals.ExposeQueryExpression(dc, expr);

			expr = expr.Transform(e =>
			{
				if (e == ExpressionConstants.DataContextParam || e is SqlQueryRootExpression)
				{
					return Expression.Constant(dc, e.Type);
				}

				return e;
			});

			var newExpr = expr.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)e;

					if (mc.Method.Name == nameof(Methods.LinqToDB.AsSubQuery) || mc.Method.Name == nameof(LinqExtensions.TagQuery))
						return new TransformInfo(mc.Arguments[0], false, true);

					if (typeof(ITable<>).IsSameOrParentOf(mc.Type) || typeof(ILoadWithQueryable<,>).IsSameOrParentOf(mc.Type))
					{
						var itemsExpression = RegisterLoaded(dc, mc);
						return new TransformInfo(itemsExpression);
					}

					mc = RemapMethod(mc);

					return new TransformInfo(mc, false, true);
					//return mc.Update(CheckForNull(mc.Object), mc.Arguments.Select(CheckForNull));
				}
				else if (e.NodeType == ExpressionType.MemberAccess)
				{
					if (typeof(ITable<>).IsSameOrParentOf(e.Type))
					{
						var items = RegisterLoaded(dc, e);
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
			{
				comparer ??= ComparerBuilder.GetEqualityComparer<T>();
				AreEqual(expected, actual, comparer, allowEmpty: true);
			}

			return actual;
		}
	}
}
