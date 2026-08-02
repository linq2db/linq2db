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
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

namespace Tests
{
	partial class TestBase
	{
		sealed class ApplyNullPropagationVisitor : ExpressionVisitorBase
		{

			private bool CanBeNull(Type type)
			{
				if (type.IsValueType)
					return false;
				return true;
			}

			private bool CanBeNull(Expression expression)
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

			private bool IsLINQMethod(MethodInfo method)
			{
				return (method.IsStatic && method.DeclaringType == typeof(Enumerable)) || method.DeclaringType == typeof(Queryable);
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
						var checkedExpression = Visit(node.Expression);
						return Expression.Condition(Expression.Equal(checkedExpression, Expression.Constant(null, checkedExpression.Type)),
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

		static string [] validPasthroughMethods = { nameof(Queryable.Where), nameof(Queryable.Select), nameof(Queryable.DefaultIfEmpty), nameof(Queryable.Distinct) };
		static string [] validOrderByMethods    = { nameof(Queryable.OrderBy), nameof(Queryable.OrderByDescending), nameof(Queryable.ThenBy), nameof(Queryable.ThenBy) };
		static string [] validMethodNames       = [..validPasthroughMethods, ..validOrderByMethods];

		static bool IsQueryableMethod(MethodCallExpression method)
		{
			var type = method.Method.DeclaringType;

			return
				type == typeof(Queryable)              ||
				type == typeof(LinqExtensions)         ||
				type == typeof(DataExtensions)         ||
				type == typeof(TableExtensions);
		}

		static bool IsQueryableMethod(MethodCallExpression method, string[] names)
		{
			if (IsQueryableMethod(method))
				foreach (var name in names)
					if (method.Method.Name == name)
						return true;

			return false;
		}

		static Expression RemoveOrderBy(Expression expression)
		{
			if (expression is not MethodCallExpression methodCall || !IsQueryableMethod(methodCall, validMethodNames))
				return expression;

			Expression result;

			if (IsQueryableMethod(methodCall, validOrderByMethods))
			{
				result = RemoveOrderBy(methodCall.Arguments[0]);
			}
			else
			{
				var firstArg = RemoveOrderBy(methodCall.Arguments[0]);
				result = methodCall.Update(methodCall.Object,
					[firstArg, .. methodCall.Arguments.Skip(1)]);
			}

			if (!expression.Type.IsAssignableFrom(result.Type))
				result = Expression.Convert(result, expression.Type);

			return result;
		}

		static Expression RemapMethod(MethodCallExpression mc)
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
				if (mc.Arguments.Count == 2)
				{
					return TypeHelper.MakeMethodCall(Methods.Queryable.Where, mc.Arguments.ToArray());
				}
			}

			if (mc.Method.Name == nameof(LinqExtensions.LeftJoin))
			{
				if (mc.Arguments.Count == 2)
				{
					var whereCall = TypeHelper.MakeMethodCall(Methods.Queryable.Where, mc.Arguments.ToArray());
					return TypeHelper.MakeMethodCall(Methods.Queryable.DefaultIfEmpty, whereCall);
				}
			}

			if (mc.Method.Name == nameof(LinqExtensions.Having))
			{
				return TypeHelper.MakeMethodCall(Methods.Queryable.Where, mc.Arguments.ToArray());
			}

			if (mc.Method.Name == nameof(LinqExtensions.RemoveOrderBy))
			{
				return RemoveOrderBy(mc.Arguments[0]);
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

			// linq2db OrderBy/OrderByDescending/ThenBy/ThenByDescending overloads that take a Sql.NullsPosition.
			// Translate to the equivalent in-memory ordering: a leading null-grouping key that reproduces the
			// requested NULLS FIRST/LAST placement, then the actual value ordering.
			if (mc.Method.DeclaringType == typeof(LinqExtensions)
				&& mc.Arguments.Count == 3
				&& mc.Method.GetParameters()[2].ParameterType == typeof(Sql.NullsPosition))
			{
				return RemapNullsOrdering(mc);
			}

			return mc;
		}

		static readonly MethodInfo _queryableOrderBy           = GetOrderingMethod(nameof(Queryable.OrderBy));
		static readonly MethodInfo _queryableOrderByDescending = GetOrderingMethod(nameof(Queryable.OrderByDescending));
		static readonly MethodInfo _queryableThenBy            = GetOrderingMethod(nameof(Queryable.ThenBy));
		static readonly MethodInfo _queryableThenByDescending  = GetOrderingMethod(nameof(Queryable.ThenByDescending));

		static MethodInfo GetOrderingMethod(string name) =>
			typeof(Queryable).GetMethods().Single(m => m.Name == name && m.GetParameters().Length == 2);

		static Expression RemapNullsOrdering(MethodCallExpression mc)
		{
			var genArgs = mc.Method.GetGenericArguments();
			var tSource = genArgs[0];
			var tKey    = genArgs[1];

			var keyQuote   = mc.Arguments[1];
			var keyLambda  = (LambdaExpression)(keyQuote is UnaryExpression { NodeType: ExpressionType.Quote } q ? q.Operand : keyQuote);
			var nulls      = (Sql.NullsPosition)((ConstantExpression)mc.Arguments[2]).Value!;
			var descending = mc.Method.Name is nameof(LinqExtensions.OrderByDescending) or nameof(LinqExtensions.ThenByDescending);
			var isThen     = mc.Method.Name is nameof(LinqExtensions.ThenBy)             or nameof(LinqExtensions.ThenByDescending);

			var source    = mc.Arguments[0];
			var canBeNull = !tKey.IsValueType || Nullable.GetUnderlyingType(tKey) != null;

			if (canBeNull && nulls != Sql.NullsPosition.None)
			{
				var whenNull    = nulls == Sql.NullsPosition.Last ? 1 : 0;
				var nullKeyBody = Expression.Condition(
					Expression.Equal(keyLambda.Body, Expression.Constant(null, tKey)),
					Expression.Constant(whenNull),
					Expression.Constant(whenNull == 1 ? 0 : 1));
				var nullKeyLambda = Expression.Lambda(nullKeyBody, keyLambda.Parameters[0]);

				// group nulls first/last (ascending key), preserving the existing ordering level
				var primary = Expression.Call(
					(isThen ? _queryableThenBy : _queryableOrderBy).MakeGenericMethod(tSource, typeof(int)),
					source, Expression.Quote(nullKeyLambda));

				// then order by the actual value
				return Expression.Call(
					(descending ? _queryableThenByDescending : _queryableThenBy).MakeGenericMethod(tSource, tKey),
					primary, keyQuote);
			}

			var plain = (isThen, descending) switch
			{
				(false, false) => _queryableOrderBy,
				(false, true ) => _queryableOrderByDescending,
				(true , false) => _queryableThenBy,
				(true , true ) => _queryableThenByDescending,
			};

			return Expression.Call(plain.MakeGenericMethod(tSource, tKey), source, keyQuote);
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

					var newExpr = RemapMethod(mc);

					return new TransformInfo(newExpr, false, true);
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

			var empty = LinqToDB.Internal.Common.Tools.CreateEmptyQuery<T>();
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
