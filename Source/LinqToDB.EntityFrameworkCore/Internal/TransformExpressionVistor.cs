using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

using ExpressionEqualityComparer = LinqToDB.Internal.Expressions.ExpressionEqualityComparer;
using SqlQueryRootExpression = LinqToDB.Internal.Expressions.SqlQueryRootExpression;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace LinqToDB.EntityFrameworkCore.Internal
{
	/// <summary>
	/// Transforms EF Core expression tree to LINQ To DB expression.
	/// </summary>
	public class TransformExpressionVisitor : ExpressionVisitorBase
	{
		static readonly char[]                  _nameSeparator = ['.'];

		protected CanBeValuatedVisitor CanBeValuatedVisitor = new();

		protected IDataContext? DataContext    { get; set; }
		public    bool?         Tracking       { get; set; }
		public    bool?         IgnoreTracking { get; set; }

		public virtual Expression Transform(IDataContext? dc, IModel? model, Expression expression)
		{
			Cleanup();
			DataContext = dc;
			var newExpression = Visit(expression);
			return newExpression;
		}

		public override void Cleanup()
		{
			DataContext    = null;
			Tracking       = null;
			IgnoreTracking = null;

			base.Cleanup();
		}

		/// <summary>
		/// Tests that method is <see cref="IQueryable{T}"/> extension.
		/// </summary>
		/// <param name="method">Method to test.</param>
		/// <param name="enumerable">Allow <see cref="IEnumerable{T}"/> extensions.</param>
		/// <returns><c>true</c> if method is <see cref="IQueryable{T}"/> extension.</returns>
		public static bool IsQueryable(MethodCallExpression method, bool enumerable = true)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(Queryable)      || (enumerable && type == typeof(Enumerable)) || type == typeof(LinqExtensions)  ||
			       type == typeof(DataExtensions) || type                                               == typeof(TableExtensions) ||
			       type == typeof(EntityFrameworkQueryableExtensions);
		}

		protected bool CanBeEvaluated(Expression expression)
		{
			CanBeValuatedVisitor.Cleanup();
			CanBeValuatedVisitor.Visit(expression);
			return CanBeValuatedVisitor.CanBeEvaluated;
		}

		protected override Expression VisitLambda<T>(Expression<T> node)
		{
			return base.VisitLambda(node);
		}

		/// <inheritdoc />
		protected override Expression VisitConstant(ConstantExpression node)
		{
			if (DataContext != null && (typeof(EntityQueryable<>).IsSameOrParentOf(node.Type) || typeof(DbSet<>).IsSameOrParentOf(node.Type)))
			{
				var entityType = node.Type.GenericTypeArguments[0];
				var newExpr    = Expression.Call(null, Methods.LinqToDB.GetTable.MakeGenericMethod(entityType), SqlQueryRootExpression.Create(DataContext));
				return newExpr;
			}

			return base.VisitConstant(node);
		}

		/// <inheritdoc />
		protected override Expression VisitMember(MemberExpression node)
		{
			var newNode = base.VisitMember(node);

			if (!ReferenceEquals(newNode, node))
				return Visit(newNode);

			if (node.Expression != null && typeof(IQueryable<>).IsSameOrParentOf(node.Type) && CanBeEvaluated(node.Expression))
			{
				var evaluated = node.EvaluateExpression();

				if (evaluated is IQueryable query)
				{
					if (ExpressionEqualityComparer.Instance.Equals(query.Expression, node))
						return node;

					return Visit(query.Expression);
				}
			}

			return node;
		}

		protected override Expression VisitMethodCall(MethodCallExpression node)
		{
			var generic = node.Method.IsGenericMethod ? node.Method.GetGenericMethodDefinition() : node.Method;

			if (generic == ReflectionMethods.IncludeMethodInfo)
			{
				var method = Methods.LinqToDB.LoadWith.MakeGenericMethod(node.Method.GetGenericArguments());

				return Expression.Call(method, node.Arguments.Select(e => Visit(e)));
			}

			if (generic == ReflectionMethods.ThenIncludeMethodInfo)
			{
				var method = Methods.LinqToDB.ThenLoadFromSingle.MakeGenericMethod(node.Method.GetGenericArguments());

				return Expression.Call(method, node.Arguments.Select(e => Visit(e)));
			}

			if (generic == ReflectionMethods.ThenIncludeEnumerableMethodInfo)
			{
				var method = Methods.LinqToDB.ThenLoadFromMany.MakeGenericMethod(node.Method.GetGenericArguments());

				return Expression.Call(method, node.Arguments.Select(e => Visit(e)));
			}

#if !EF31
			if (generic.Name is "TemporalFromTo" or "TemporalAll" or "TemporalAsOfTable" or "TemporalBetween" or "TemporalContainedIn")
			{
				var evaluated = node.EvaluateExpression();
				if (evaluated is IQueryable query)
					return Visit(query.Expression);
				return node;
			}
#endif
			if (IsQueryable(node))
			{
				if (node.Method.IsGenericMethod)
				{
					if (generic == ReflectionMethods.IncludeMethodInfoString)
					{
						var arguments = new List<Expression>(2) { node.Arguments[0] };
						var propName  = node.Arguments[1].EvaluateExpression<string>();
						if (propName is null)
							return node;

						var param    = Expression.Parameter(node.Method.GetGenericArguments()[0], "e");
						var propPath = propName.Split(_nameSeparator, StringSplitOptions.RemoveEmptyEntries);
						var prop     = (Expression)param;
						for (var i = 0; i < propPath.Length; i++)
						{
							prop = Expression.PropertyOrField(prop, propPath[i]);
						}

						arguments.Add(Expression.Lambda(prop, param));

						var method = Methods.LinqToDB.LoadWith.MakeGenericMethod(param.Type, prop.Type);

						return Visit(Expression.Call(method, arguments.ToArray()));
					}

					if (generic == ReflectionMethods.IgnoreQueryFiltersMethodInfo)
					{
						var newMethod = Expression.Call(
										Methods.LinqToDB.IgnoreFilters.MakeGenericMethod(node.Method.GetGenericArguments()),
										node.Arguments[0], Expression.NewArrayInit(typeof(Type)));
						return newMethod;
					}

					if (generic == ReflectionMethods.AsNoTrackingMethodInfo
#if !EF31
						|| generic == ReflectionMethods.AsNoTrackingWithIdentityResolutionMethodInfo
#endif
						)
					{
						Tracking = false;
						return Visit(node.Arguments[0]);
					}

					if (generic == ReflectionMethods.AsTrackingMethodInfo)
					{
						Tracking = true;
						return Visit(node.Arguments[0]);
					}

					if (generic == Methods.LinqToDB.RemoveOrderBy)
					{
						// This is workaround. EagerLoading runs query again with RemoveOrderBy method.
						// it is only one possible way now how to detect nested query. 
						IgnoreTracking = true;

						return node;
					}

					if (generic == ReflectionMethods.TagWithMethodInfo)
					{
						var method = Methods.LinqToDB.TagQuery.MakeGenericMethod(node.Method.GetGenericArguments());

						return Visit(Expression.Call(method, node.Arguments));
					}
				}
			}

#if !EF31
			if (generic == ReflectionMethods.AsSplitQueryMethodInfo || generic == ReflectionMethods.AsSingleQueryMethodInfo)
				return Visit(node.Arguments[0]);
#endif

			if (generic == ReflectionMethods.ToLinqToDBTable)
			{
				return Visit(node.Arguments[0]);
			}

#if EF31
			if (generic == ReflectionMethods.FromSqlOnQueryableMethodInfo && DataContext != null)
			{
				//convert the arguments from the FromSqlOnQueryable method from EF, to a L2DB FromSql call
				return Visit(Expression.Call(null, ReflectionMethods.L2DBFromSqlMethodInfo.MakeGenericMethod(node.Method.GetGenericArguments()[0]),
					SqlQueryRootExpression.Create(DataContext),
					Expression.New(ReflectionMethods.RawSqlStringConstructor, node.Arguments[1]),
					node.Arguments[2]));
			}
#endif

			if (typeof(IQueryable<>).IsSameOrParentOf(node.Type) && node.Method.DeclaringType?.Assembly != typeof(LinqExtensions).Assembly)
			{
				if (CanBeEvaluated(node))
				{
					var evaluated = node.EvaluateExpression();
					if (evaluated is IQueryable query)
					{
						if (!ExpressionEqualityComparer.Instance.Equals(query.Expression, node))
						{
							return Visit(query.Expression);
						}
					}
				}
			}

			if (generic == ReflectionMethods.EFProperty)
			{
				var prop = Expression.Call(null, Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(node.Method.GetGenericArguments()[0]), node.Arguments[0], node.Arguments[1]);
				return Visit(prop);
			}

			List<Expression>? newArguments = null;
			var parameters = generic.GetParameters();
			for (var i = 0; i < parameters.Length; i++)
			{
				var arg = node.Arguments[i];
				var canWrap = true;

				if (arg.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression) arg;
					if (mc.Method.DeclaringType == typeof(Sql))
						canWrap = false;
				}

				if (canWrap)
				{
					if (parameters[i].HasAttribute<NotParameterizedAttribute>())
					{
						newArguments ??= [.. node.Arguments.Take(i)];

						newArguments.Add(Expression.Call(ReflectionMethods.ToSql.MakeGenericMethod(arg.Type), arg));
						continue;
					}
				}

				newArguments?.Add(node.Arguments[i]);
			}

			if (newArguments != null)
				node = node.Update(node.Object, newArguments);

			return base.VisitMethodCall(node);
		}

#if !EF31
		/// <summary>
		/// Gets current property value via reflection.
		/// </summary>
		/// <typeparam name="TValue">Property value type.</typeparam>
		/// <param name="obj">Object instance</param>
		/// <param name="propName">Property name</param>
		/// <returns>Property value.</returns>
		/// <exception cref="InvalidOperationException"></exception>
		protected static TValue GetPropValue<TValue>(object obj, string propName)
		{
			var prop = obj.GetType().GetProperty(propName)
			           ?? throw new InvalidOperationException($"Property {obj.GetType().Name}.{propName} not found.");
			var propValue = prop.GetValue(obj);
			if (propValue == default)
				return default!;
			return (TValue)propValue;
		}

		/// <summary>
		/// Transforms <see cref="QueryRootExpression"/> descendants to linq2db analogue. Handles Temporal tables also.
		/// </summary>
		/// <param name="dc">Data context.</param>
		/// <param name="queryRoot">Query root expression</param>
		/// <returns>Transformed expression.</returns>
		protected virtual Expression TransformQueryRootExpression(IDataContext dc, QueryRootExpression queryRoot)
		{
			static Expression GetAsOfSqlServer(Expression getTableExpr, Type entityType)
			{
				return Expression.Call(
					ReflectionMethods.AsSqlServerTable.MakeGenericMethod(entityType),
					getTableExpr);
			}

			if (queryRoot is FromSqlQueryRootExpression fromSqlQueryRoot)
			{
				//convert the arguments from the FromSqlOnQueryable method from EF, to a L2DB FromSql call
				return Expression.Call(null,
					ReflectionMethods.L2DBFromSqlMethodInfo.MakeGenericMethod(fromSqlQueryRoot.EntityType.ClrType),
					Expression.Constant(dc),
					Expression.New(ReflectionMethods.RawSqlStringConstructor, Expression.Constant(fromSqlQueryRoot.Sql)),
					fromSqlQueryRoot.Argument);
			}

#if EF6
			var entityType = queryRoot.EntityType.ClrType;
#else
			var entityType = queryRoot.ElementType;
#endif
			var getTableExpr = Expression.Call(null, Methods.LinqToDB.GetTable.MakeGenericMethod(entityType),
				Expression.Constant(dc));

			var expressionTypeName = queryRoot.GetType().Name;
			if (expressionTypeName == "TemporalAsOfQueryRootExpression")
			{
				var pointInTime = GetPropValue<DateTime>(queryRoot, "PointInTime");

				var asOf = Expression.Call(ReflectionMethods.TemporalAsOfTable.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(pointInTime));

				return asOf;
			}

			if (expressionTypeName == "TemporalFromToQueryRootExpression")
			{
				var from = GetPropValue<DateTime>(queryRoot, "From");
				var to = GetPropValue<DateTime>(queryRoot, "To");

				var fromTo = Expression.Call(ReflectionMethods.TemporalFromTo.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(from),
					Expression.Constant(to));

				return fromTo;
			}

			if (expressionTypeName == "TemporalBetweenQueryRootExpression")
			{
				var from = GetPropValue<DateTime>(queryRoot, "From");
				var to = GetPropValue<DateTime>(queryRoot, "To");

				var fromTo = Expression.Call(ReflectionMethods.TemporalBetween.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(from),
					Expression.Constant(to));

				return fromTo;
			}

			if (expressionTypeName == "TemporalContainedInQueryRootExpression")
			{
				var from = GetPropValue<DateTime>(queryRoot, "From");
				var to = GetPropValue<DateTime>(queryRoot, "To");

				var fromTo = Expression.Call(ReflectionMethods.TemporalContainedIn.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType),
					Expression.Constant(from),
					Expression.Constant(to));

				return fromTo;
			}

			if (expressionTypeName == "TemporalAllQueryRootExpression")
			{
				var all = Expression.Call(ReflectionMethods.TemporalAll.MakeGenericMethod(entityType),
					GetAsOfSqlServer(getTableExpr, entityType));

				return all;
			}

			return getTableExpr;
		}
#endif

		protected override Expression VisitExtension(Expression node)
		{
#if !EF31
			if (DataContext != null && node is QueryRootExpression queryRoot)
				return TransformQueryRootExpression(DataContext, queryRoot);
#endif
			return base.VisitExtension(node);
		}
	}
}
