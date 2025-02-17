using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;

namespace LinqToDB
{
	/// <summary>
	/// Provides API for compilation and caching of queries for reuse.
	/// </summary>
	[PublicAPI]
	public class CompiledQuery
	{
		protected CompiledQuery(LambdaExpression query)
		{
			_query = query;
		}

		readonly Lock                     _sync = new ();
		readonly LambdaExpression         _query;
		volatile Func<object?[],object?[]?,object?>? _compiledQuery;

		TResult ExecuteQuery<TResult>(params object?[] args)
		{
			if (_compiledQuery == null)
				lock (_sync)
					_compiledQuery ??= CompileQuery(_query);

			//TODO: pass preambles
			return (TResult)_compiledQuery(args, null)!;
		}

		enum MethodType
		{
			Queryable,
			Element,
			ElementAsync
		}

		interface ITableHelper
		{
			Expression CallTable(LambdaExpression query, Expression expr, ParameterExpression ps, ParameterExpression preambles, MethodType type);
		}

		sealed class TableHelper<T> : ITableHelper
			where T : notnull
		{
			public Expression CallTable(LambdaExpression query, Expression expr, ParameterExpression ps, ParameterExpression preambles, MethodType type)
			{
				var table = new CompiledTable<T>(query, expr);

				return Expression.Call(
					Expression.Constant(table),
					type == MethodType.Queryable ?
						MemberHelper.MethodOf<CompiledTable<T>>(t => t.Create      (null!, null!)) :
					type == MethodType.Element ?
						MemberHelper.MethodOf<CompiledTable<T>>(t => t.Execute     (null!, null!)) :
						MemberHelper.MethodOf<CompiledTable<T>>(t => t.ExecuteAsync(null!, null!)),
					ps, preambles);
			}
		}

		static Func<object?[],object?[]?,object?> CompileQuery(LambdaExpression query)
		{
			var ps        = ExpressionBuilder.ParametersParam;
			var preambles = Expression.Parameter(typeof(object[]), "preambles");

			var info = query.Body.Transform((query, ps, preambles), static (context, pi) =>
			{
				switch (pi.NodeType)
				{
					case ExpressionType.Parameter :
						{
							var idx = context.query.Parameters.IndexOf((ParameterExpression)pi);

							if (idx >= 0)
								return Expression.Convert(Expression.ArrayIndex(context.ps, ExpressionInstances.Int32(idx)), pi.Type);

							break;
						}
				}

				return pi;
			});

			info = info.Transform((query, ps, preambles), static (context, pi) =>
			{
				switch (pi.NodeType)
				{
					case ExpressionType.Call :
						{
							var expr = (MethodCallExpression)pi;

							if (expr.Method.DeclaringType == typeof(AsyncExtensions) &&
								expr.Method.HasAttribute<AsyncExtensions.ElementAsyncAttribute>())
							{
								var type = expr.Type.GetGenericArguments()[0];

								var helper = ActivatorExt.CreateInstance<ITableHelper>(typeof(TableHelper<>).MakeGenericType(type));

								return helper.CallTable(context.query, expr, context.ps, context.preambles, MethodType.ElementAsync);
							}
							else if (expr.IsQueryable())
							{
								var type   = typeof(IQueryable).IsSameOrParentOf(expr.Type) ?
										typeof(IQueryable<>) :
										typeof(IEnumerable<>);

								var qtype  = type.GetGenericType(expr.Type);
								var helper = ActivatorExt.CreateInstance<ITableHelper>(
									typeof(TableHelper<>).MakeGenericType(qtype == null ? expr.Type : qtype.GetGenericArguments()[0]));

								return helper.CallTable(context.query, expr, context.ps, context.preambles, qtype != null ? MethodType.Queryable : MethodType.Element);
							}

							if (expr.Method.Name == "GetTable" && expr.Method.DeclaringType == typeof(DataExtensions))
								goto case ExpressionType.MemberAccess;
						}

						break;

					case ExpressionType.MemberAccess :
						if (typeof(ITable<>).IsSameOrParentOf(pi.Type))
						{
							var helper = ActivatorExt
								.CreateInstance<ITableHelper>(typeof(TableHelper<>)
								.MakeGenericType(pi.Type.GetGenericArguments()[0]));
							return helper.CallTable(context.query, pi, context.ps, context.preambles, MethodType.Queryable);
						}

						break;
				}

				return pi;
			});

			return Expression.Lambda<Func<object?[],object?[]?,object?>>(Expression.Convert(info, typeof(object)), ps, preambles).CompileExpression();
		}

		#region Invoke

		/// <summary>
		/// Executes compiled query against provided database connection context.
		/// </summary>
		/// <typeparam name="TDC">Database connection context type.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <returns>Query execution result.</returns>
		public TResult Invoke<TDC,TResult>(TDC dataContext)
		{
			return ExecuteQuery<TResult>(dataContext);
		}

		/// <summary>
		/// Executes compiled query with one parameter against provided database connection context.
		/// </summary>
		/// <typeparam name="TDC">Database connection context type.</typeparam>
		/// <typeparam name="T1">Query parameter type.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="arg1">Query parameter value.</param>
		/// <returns>Query execution result.</returns>
		public TResult Invoke<TDC,T1,TResult>(TDC dataContext, T1 arg1)
		{
			return ExecuteQuery<TResult>(dataContext, arg1);
		}

		/// <summary>
		/// Executes compiled query with two parameters against provided database connection context.
		/// </summary>
		/// <typeparam name="TDC">Database connection context type.</typeparam>
		/// <typeparam name="T1">First query parameter type.</typeparam>
		/// <typeparam name="T2">Second query parameter type.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="arg1">First query parameter value.</param>
		/// <param name="arg2">Second query parameter value.</param>
		/// <returns>Query execution result.</returns>
		public TResult Invoke<TDC,T1,T2,TResult>(TDC dataContext, T1 arg1, T2 arg2)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2);
		}

		/// <summary>
		/// Executes compiled query with three parameters against provided database connection context.
		/// </summary>
		/// <typeparam name="TDC">Database connection context type.</typeparam>
		/// <typeparam name="T1">First query parameter type.</typeparam>
		/// <typeparam name="T2">Second query parameter type.</typeparam>
		/// <typeparam name="T3">Third query parameter type.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="arg1">First query parameter value.</param>
		/// <param name="arg2">Second query parameter value.</param>
		/// <param name="arg3">Third query parameter value.</param>
		/// <returns>Query execution result.</returns>
		public TResult Invoke<TDC,T1,T2,T3,TResult>(TDC dataContext, T1 arg1, T2 arg2, T3 arg3)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2, arg3);
		}

		/// <summary>
		/// Executes compiled query with four parameters against provided database connection context.
		/// </summary>
		/// <typeparam name="TDC">Database connection context type.</typeparam>
		/// <typeparam name="T1">First query parameter type.</typeparam>
		/// <typeparam name="T2">Second query parameter type.</typeparam>
		/// <typeparam name="T3">Third query parameter type.</typeparam>
		/// <typeparam name="T4">Forth query parameter type.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="arg1">First query parameter value.</param>
		/// <param name="arg2">Second query parameter value.</param>
		/// <param name="arg3">Third query parameter value.</param>
		/// <param name="arg4">Forth query parameter value.</param>
		/// <returns>Query execution result.</returns>
		public TResult Invoke<TDC,T1,T2,T3,T4,TResult>(TDC dataContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2, arg3, arg4);
		}

		/// <summary>
		/// Executes compiled query with five parameters against provided database connection context.
		/// </summary>
		/// <typeparam name="TDC">Database connection context type.</typeparam>
		/// <typeparam name="T1">First query parameter type.</typeparam>
		/// <typeparam name="T2">Second query parameter type.</typeparam>
		/// <typeparam name="T3">Third query parameter type.</typeparam>
		/// <typeparam name="T4">Forth query parameter type.</typeparam>
		/// <typeparam name="T5">Fifth query parameter type.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="arg1">First query parameter value.</param>
		/// <param name="arg2">Second query parameter value.</param>
		/// <param name="arg3">Third query parameter value.</param>
		/// <param name="arg4">Forth query parameter value.</param>
		/// <param name="arg5">Fifth query parameter value.</param>
		/// <returns>Query execution result.</returns>
		public TResult Invoke<TDC,T1,T2,T3,T4,T5,TResult>(TDC dataContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2, arg3, arg4, arg5);
		}

		#endregion

		#region Compile

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDC">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDC,TResult> Compile<TDC,TResult>(
			Expression<Func<TDC,TResult>> query)
			  where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException(nameof(query));
			return new CompiledQuery(query).Invoke<TDC,TResult>;
		}

		/// <summary>
		/// Compiles the query with parameter.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDC">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDC,TArg1,TResult> Compile<TDC,TArg1,TResult>(
			Expression<Func<TDC,TArg1,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException(nameof(query));
			return new CompiledQuery(query).Invoke<TDC,TArg1,TResult>;
		}

		/// <summary>
		/// Compiles the query with two parameters.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDC">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of first parameter for compiled query.</typeparam>
		/// <typeparam name="TArg2">Type of second parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDC,TArg1,TArg2,TResult> Compile<TDC,TArg1,TArg2,TResult>(
			Expression<Func<TDC,TArg1,TArg2,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException(nameof(query));
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TResult>;
		}

		/// <summary>
		/// Compiles the query with three parameters.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDC">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of first parameter for compiled query.</typeparam>
		/// <typeparam name="TArg2">Type of second parameter for compiled query.</typeparam>
		/// <typeparam name="TArg3">Type of third parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDC,TArg1,TArg2,TArg3,TResult> Compile<TDC,TArg1,TArg2,TArg3,TResult>(
			Expression<Func<TDC,TArg1,TArg2,TArg3,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException(nameof(query));
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TArg3,TResult>;
		}

		/// <summary>
		/// Compiles the query with four parameters.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDC">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of first parameter for compiled query.</typeparam>
		/// <typeparam name="TArg2">Type of second parameter for compiled query.</typeparam>
		/// <typeparam name="TArg3">Type of third parameter for compiled query.</typeparam>
		/// <typeparam name="TArg4">Type of forth parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDC,TArg1,TArg2,TArg3,TArg4,TResult> Compile<TDC,TArg1,TArg2,TArg3,TArg4,TResult>(
			Expression<Func<TDC,TArg1,TArg2,TArg3,TArg4,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException(nameof(query));
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TArg3,TArg4,TResult>;
		}

		/// <summary>
		/// Compiles the query with five parameters.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">The query expression to be compiled.</param>
		/// <typeparam name="TDC">Type of data context parameter, passed to compiled query.</typeparam>
		/// <typeparam name="TArg1">Type of first parameter for compiled query.</typeparam>
		/// <typeparam name="TArg2">Type of second parameter for compiled query.</typeparam>
		/// <typeparam name="TArg3">Type of third parameter for compiled query.</typeparam>
		/// <typeparam name="TArg4">Type of forth parameter for compiled query.</typeparam>
		/// <typeparam name="TArg5">Type of fifth parameter for compiled query.</typeparam>
		/// <typeparam name="TResult">Query result type.</typeparam>
		public static Func<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult> Compile<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult>(
			Expression<Func<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException(nameof(query));
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult>;
		}

		#endregion
	}
}
