using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Linq;

	using SqlQuery;

	public static class LinqExtensions
	{
		#region Table Helpers

		static public ITable<T> TableName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			return table;
		}

		static public ITable<T> DatabaseName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			return table;
		}

		static public ITable<T> OwnerName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			return table;
		}

		static public ITable<T> TableTempType<T>([NotNull] this ITable<T> table, SqlTableTempType sqlTableTempType)
		{
			if (table == null) throw new ArgumentNullException("table");

			table.Expression = Expression.Call(
				null,
				((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(sqlTableTempType) });

			return table;
		}

		#endregion

		#region LoadWith

		static public ITable<T> LoadWith<T>(
			[NotNull]                this ITable<T> table,
			[NotNull, InstantHandle] Expression<Func<T,object>> selector)
		{
			if (table == null) throw new ArgumentNullException("table");

			table.Expression = Expression.Call(
				null,
				((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Quote(selector) });

			return table;
		}

		#endregion

		#region Scalar Select

		static public T Select<T>(
			[NotNull]                this IDataContext   dataContext,
			[NotNull, InstantHandle] Expression<Func<T>> selector)
		{
			if (dataContext == null) throw new ArgumentNullException("dataContext");
			if (selector    == null) throw new ArgumentNullException("selector");

			var q = new Table<T>(dataContext, selector);

			foreach (var item in q)
				return item;

			throw new InvalidOperationException();
		}

		#endregion

		#region Delete

		public static int Delete<T>([NotNull] this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression }));
		}

		public static int Delete<T>(
			[NotNull]                this IQueryable<T>       source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException("source");
			if (predicate == null) throw new ArgumentNullException("predicate");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression, Expression.Quote(predicate) }));
		}

		#endregion

		#region Update

		public static int Update<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (target == null) throw new ArgumentNullException("target");
			if (setter == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		public static int Update<T>(
			[NotNull]                this IQueryable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,T>> setter)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (setter == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression, Expression.Quote(setter) }));
		}

		public static int Update<T>(
			[NotNull]                this IQueryable<T>       source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate,
			[NotNull, InstantHandle] Expression<Func<T,T>>    setter)
		{
			if (source    == null) throw new ArgumentNullException("source");
			if (predicate == null) throw new ArgumentNullException("predicate");
			if (setter    == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression, Expression.Quote(predicate), Expression.Quote(setter) }));
		}

		public static int Update<T>([NotNull] this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((Updatable<T>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		class Updatable<T> : IUpdatable<T>
		{
			public IQueryable<T> Query;
		}

		public static IUpdatable<T> AsUpdatable<T>([NotNull] this IQueryable<T> source)
		{
			if (source  == null) throw new ArgumentNullException("source");

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression }));

			return new Updatable<T> { Query = query };
		}

		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IQueryable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<T,TV>> update)
		{
			if (source  == null) throw new ArgumentNullException("source");
			if (extract == null) throw new ArgumentNullException("extract");
			if (update  == null) throw new ArgumentNullException("update");

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { source.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IUpdatable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<T,TV>> update)
		{
			if (source  == null) throw new ArgumentNullException("source");
			if (extract == null) throw new ArgumentNullException("extract");
			if (update  == null) throw new ArgumentNullException("update");

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IQueryable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<TV>>   update)
		{
			if (source  == null) throw new ArgumentNullException("source");
			if (extract == null) throw new ArgumentNullException("extract");
			if (update  == null) throw new ArgumentNullException("update");

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { source.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IUpdatable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<TV>>   update)
		{
			if (source  == null) throw new ArgumentNullException("source");
			if (extract == null) throw new ArgumentNullException("extract");
			if (update  == null) throw new ArgumentNullException("update");

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IQueryable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			TV                                              value)
		{
			if (source  == null) throw new ArgumentNullException("source");
			if (extract == null) throw new ArgumentNullException("extract");

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { source.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)) }));

			return new Updatable<T> { Query = query };
		}

		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IUpdatable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			TV                                              value)
		{
			if (source  == null) throw new ArgumentNullException("source");
			if (extract == null) throw new ArgumentNullException("extract");

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)) }));

			return new Updatable<T> { Query = query };
		}

		#endregion

		#region Insert

		public static int Insert<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			if (target == null) throw new ArgumentNullException("target");
			if (setter == null) throw new ArgumentNullException("setter");

			IQueryable<T> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression, Expression.Quote(setter) }));
		}

		public static object InsertWithIdentity<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			if (target == null) throw new ArgumentNullException("target");
			if (setter == null) throw new ArgumentNullException("setter");

			IQueryable<T> query = target;

			return query.Provider.Execute<object>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression, Expression.Quote(setter) }));
		}

		#region ValueInsertable

		class ValueInsertable<T> : IValueInsertable<T>
		{
			public IQueryable<T> Query;
		}

		public static IValueInsertable<T> Into<T>(this IDataContext dataContext, [NotNull] ITable<T> target)
		{
			if (target == null) throw new ArgumentNullException("target");

			IQueryable<T> query = target;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { Expression.Constant(null, typeof(IDataContext)), query.Expression }));

			return new ValueInsertable<T> { Query = q };
		}

		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this ITable<T>         source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> field,
			[NotNull, InstantHandle] Expression<Func<TV>>   value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");
			if (value  == null) throw new ArgumentNullException("value");

			var query = (IQueryable<T>)source;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new ValueInsertable<T> { Query = q };
		}

		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this ITable<T>         source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> field,
			TV                                              value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");

			var query = (IQueryable<T>)source;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)) }));

			return new ValueInsertable<T> { Query = q };
		}

		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this IValueInsertable<T> source,
			[NotNull, InstantHandle] Expression<Func<T,TV>>   field,
			[NotNull, InstantHandle] Expression<Func<TV>>     value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");
			if (value  == null) throw new ArgumentNullException("value");

			var query = ((ValueInsertable<T>)source).Query;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new ValueInsertable<T> { Query = q };
		}

		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this IValueInsertable<T> source,
			[NotNull, InstantHandle] Expression<Func<T,TV>>   field,
			TV                                                value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");

			var query = ((ValueInsertable<T>)source).Query;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)) }));

			return new ValueInsertable<T> { Query = q };
		}

		public static int Insert<T>([NotNull] this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((ValueInsertable<T>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		public static object InsertWithIdentity<T>([NotNull] this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((ValueInsertable<T>)source).Query;

			return query.Provider.Execute<object>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		#endregion

		#region SelectInsertable

		public static int Insert<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (target == null) throw new ArgumentNullException("target");
			if (setter == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		public static object InsertWithIdentity<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (target == null) throw new ArgumentNullException("target");
			if (setter == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<object>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		class SelectInsertable<T,TT> : ISelectInsertable<T,TT>
		{
			public IQueryable<T> Query;
		}

		public static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(
			[NotNull] this IQueryable<TSource> source,
			[NotNull] ITable<TTarget>          target)
		{
			if (target == null) throw new ArgumentNullException("target");

			var q = source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			[NotNull]                this ISelectInsertable<TSource,TTarget> source,
			[NotNull, InstantHandle] Expression<Func<TTarget,TValue>>        field,
			[NotNull, InstantHandle] Expression<Func<TSource,TValue>>        value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");
			if (value  == null) throw new ArgumentNullException("value");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var q = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget), typeof(TValue) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			[NotNull]                this ISelectInsertable<TSource,TTarget> source,
			[NotNull, InstantHandle] Expression<Func<TTarget,TValue>>        field,
			[NotNull, InstantHandle] Expression<Func<TValue>>                value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");
			if (value  == null) throw new ArgumentNullException("value");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var q = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget), typeof(TValue) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			[NotNull]                this ISelectInsertable<TSource,TTarget> source,
			[NotNull, InstantHandle] Expression<Func<TTarget,TValue>>        field,
			TValue                                                           value)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (field  == null) throw new ArgumentNullException("field");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var q = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget), typeof(TValue) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TValue)) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		public static int Insert<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { query.Expression }));
		}

		public static object InsertWithIdentity<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			return query.Provider.Execute<object>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { query.Expression }));
		}

		#endregion

		#endregion

		#region InsertOrUpdate

		public static int InsertOrUpdate<T>(
			[NotNull]                this ITable<T>        target,
			[NotNull, InstantHandle] Expression<Func<T>>   insertSetter,
			[NotNull, InstantHandle] Expression<Func<T,T>> onDuplicateKeyUpdateSetter)
		{
			if (target                     == null) throw new ArgumentNullException("target");
			if (insertSetter               == null) throw new ArgumentNullException("insertSetter");
			if (onDuplicateKeyUpdateSetter == null) throw new ArgumentNullException("onDuplicateKeyUpdateSetter");

			IQueryable<T> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression, Expression.Quote(insertSetter), Expression.Quote(onDuplicateKeyUpdateSetter) }));
		}

		public static int InsertOrUpdate<T>(
			[NotNull]                this ITable<T>        target,
			[NotNull, InstantHandle] Expression<Func<T>>   insertSetter,
			[NotNull, InstantHandle] Expression<Func<T,T>> onDuplicateKeyUpdateSetter,
			[NotNull, InstantHandle] Expression<Func<T>>   keySelector)
		{
			if (target                     == null) throw new ArgumentNullException("target");
			if (insertSetter               == null) throw new ArgumentNullException("insertSetter");
			if (onDuplicateKeyUpdateSetter == null) throw new ArgumentNullException("onDuplicateKeyUpdateSetter");
			if (keySelector                == null) throw new ArgumentNullException("keySelector");

			IQueryable<T> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[]
					{
						query.Expression,
						Expression.Quote(insertSetter),
						Expression.Quote(onDuplicateKeyUpdateSetter),
						Expression.Quote(keySelector)
					}));
		}

		#endregion

		#region DDL Operations

		public static int Drop<T>([NotNull] this ITable<T> target)
		{
			if (target == null) throw new ArgumentNullException("target");

			IQueryable<T> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		#endregion

		#region Take / Skip / ElementAt

		public static IQueryable<TSource> Take<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (count  == null) throw new ArgumentNullException("count");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(count) }));
		}

		public static IQueryable<TSource> Skip<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (count  == null) throw new ArgumentNullException("count");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(count) }));
		}

		public static TSource ElementAt<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    index)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (index  == null) throw new ArgumentNullException("index");

			return source.Provider.Execute<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(index) }));
		}

		public static TSource ElementAtOrDefault<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    index)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (index  == null) throw new ArgumentNullException("index");

			return source.Provider.Execute<TSource>(
				Expression.Call(
					null,
					((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(index) }));
		}

		#endregion

		#region Stub helpers

		public static TOutput Where<TOutput,TSource,TInput>(this TInput source, Func<TSource,bool> predicate)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}
}
