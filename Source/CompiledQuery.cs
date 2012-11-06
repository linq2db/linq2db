using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Expressions;
	using Extensions;
	using Linq;

	/// <summary>
	/// Provides for compilation and caching of queries for reuse.
	/// </summary>
	public class CompiledQuery
	{
		protected CompiledQuery(LambdaExpression query)
		{
			_query = query;
		}

		readonly object                _sync = new object();
		readonly LambdaExpression      _query;
		volatile Func<object[],object> _compiledQuery;

		TResult ExecuteQuery<TResult>(params object[] args)
		{
			if (_compiledQuery == null)
				lock (_sync)
					if (_compiledQuery == null)
						_compiledQuery = CompileQuery(_query);

			return (TResult)_compiledQuery(args);
		}

		private interface ITableHelper
		{
			Expression CallTable(LambdaExpression query, Expression expr, ParameterExpression ps, bool isQueriable);
		}

		internal class TableHelper<T> : ITableHelper
		{
			public Expression CallTable(LambdaExpression query, Expression expr, ParameterExpression ps, bool isQueriable)
			{
				var table = new CompiledTable<T>(query, expr);

				return Expression.Call(
					Expression.Constant(table),
					isQueriable ?
						ReflectionHelper.Expressor<CompiledTable<T>>.MethodExpressor(t => t.Create (null)) :
						ReflectionHelper.Expressor<CompiledTable<T>>.MethodExpressor(t => t.Execute(null)),
					ps);
			}
		}

		static Func<object[],object> CompileQuery(LambdaExpression query)
		{
			var ps = Expression.Parameter(typeof(object[]), "ps");

			var info = query.Body.Transform(pi =>
			{
				switch (pi.NodeType)
				{
					case ExpressionType.Parameter :
						{
							var idx = query.Parameters.IndexOf((ParameterExpression)pi);

							if (idx >= 0)
								return Expression.Convert(Expression.ArrayIndex(ps, Expression.Constant(idx)), pi.Type);

							break;
						}

					case ExpressionType.Call :
						{
							var expr = (MethodCallExpression)pi;

							if (expr.IsQueryable())
							{
								var type   = typeof(IQueryable).IsSameOrParentOf(expr.Type) ?
										typeof(IQueryable<>) :
										typeof(IEnumerable<>);

								var qtype  = type.GetGenericType(expr.Type);
								var helper = (ITableHelper)Activator.CreateInstance(
									typeof(TableHelper<>).MakeGenericType(qtype == null ? expr.Type : qtype.GetGenericArguments()[0]));

								return helper.CallTable(query, expr, ps, qtype != null);
							}

							if (expr.Method.Name == "GetTable" && expr.Method.DeclaringType == typeof(DataExtensions))
								goto case ExpressionType.MemberAccess;
						}

						break;

					case ExpressionType.MemberAccess :
						if (pi.Type.IsGenericType && pi.Type.GetGenericTypeDefinition() == typeof(Table<>))
						{
							var helper = (ITableHelper)Activator
								.CreateInstance(typeof(TableHelper<>)
								.MakeGenericType(pi.Type.GetGenericArguments()[0]));
							return helper.CallTable(query, pi, ps, true);
						}

						break;
				}

				return pi;
			});

			return Expression.Lambda<Func<object[],object>>(Expression.Convert(info, typeof(object)), ps).Compile();
		}

		#region Invoke

		public TResult Invoke<TDC,TResult>(TDC dataContext)
		{
			return ExecuteQuery<TResult>(dataContext);
		}

		public TResult Invoke<TDC,T1,TResult>(TDC dataContext, T1 arg1)
		{
			return ExecuteQuery<TResult>(dataContext, arg1);
		}

		public TResult Invoke<TDC,T1,T2,TResult>(TDC dataContext, T1 arg1, T2 arg2)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2);
		}

		public TResult Invoke<TDC,T1,T2,T3,TResult>(TDC dataContext, T1 arg1, T2 arg2, T3 arg3)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2, arg3);
		}

		public TResult Invoke<TDC,T1,T2,T3,T4,TResult>(TDC dataContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			return ExecuteQuery<TResult>(dataContext, arg1, arg2, arg3, arg4);
		}

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
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDC ">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDC,TResult> Compile<TDC,TResult>(
			[JetBrains.Annotations.NotNull] Expression<Func<TDC,TResult>> query)
			  where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException("query");
			return new CompiledQuery(query).Invoke<TDC,TResult>;
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDC">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDC,TArg1,TResult> Compile<TDC,TArg1,TResult>(
			[JetBrains.Annotations.NotNull] Expression<Func<TDC,TArg1,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException("query");
			return new CompiledQuery(query).Invoke<TDC,TArg1,TResult>;
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDC ">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg2">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDC,TArg1,TArg2,TResult> Compile<TDC,TArg1,TArg2,TResult>(
			[JetBrains.Annotations.NotNull] Expression<Func<TDC,TArg1,TArg2,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException("query");
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TResult>;
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDC ">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg2">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg3">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDC,TArg1,TArg2,TArg3,TResult> Compile<TDC,TArg1,TArg2,TArg3,TResult>(
			[JetBrains.Annotations.NotNull] Expression<Func<TDC,TArg1,TArg2,TArg3,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException("query");
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TArg3,TResult>;
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDC ">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg2">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg3">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg4">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDC,TArg1,TArg2,TArg3,TArg4,TResult> Compile<TDC,TArg1,TArg2,TArg3,TArg4,TResult>(
			[JetBrains.Annotations.NotNull] Expression<Func<TDC,TArg1,TArg2,TArg3,TArg4,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException("query");
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TArg3,TArg4,TResult>;
		}

		/// <summary>
		/// Compiles the query.
		/// </summary>
		/// <returns>
		/// A generic delegate that represents the compiled query.
		/// </returns>
		/// <param name="query">
		/// The query expression to be compiled.
		/// </param>
		/// <typeparam name="TDC ">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg1">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg2">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg3">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg4">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TArg5">
		/// Represents the type of the parameter that has to be passed in when executing the delegate returned by the method.
		/// </typeparam>
		/// <typeparam name="TResult">
		/// Returned type of the delegate returned by the method.
		/// </typeparam>
		public static Func<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult> Compile<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult>(
			[JetBrains.Annotations.NotNull] Expression<Func<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult>> query)
			where TDC : IDataContext
		{
			if (query == null) throw new ArgumentNullException("query");
			return new CompiledQuery(query).Invoke<TDC,TArg1,TArg2,TArg3,TArg4,TArg5,TResult>;
		}

		#endregion
	}
}
