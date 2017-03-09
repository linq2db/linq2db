using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Expressions;
	using Linq;
	using Linq.Builder;

	[PublicAPI]
	public static class LinqExtensions
	{
		#region Table Helpers

		static readonly MethodInfo _tableNameMethodInfo = MemberHelper.MethodOf(() => TableName<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> TableName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				_tableNameMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			var tbl = table as Table<T>;
			if (tbl != null)
				tbl.TableName = name;

			return table;
		}

		static readonly MethodInfo _databaseNameMethodInfo = MemberHelper.MethodOf(() => DatabaseName<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> DatabaseName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				_databaseNameMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			var tbl = table as Table<T>;
			if (tbl != null)
				tbl.DatabaseName = name;

			return table;
		}

		static readonly MethodInfo _ownerNameMethodInfo = MemberHelper.MethodOf(() => OwnerName<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> OwnerName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				_ownerNameMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			var tbl = table as Table<T>;
			if (tbl != null)
				tbl.SchemaName = name;

			return table;
		}

		static readonly MethodInfo _schemaNameMethodInfo = MemberHelper.MethodOf(() => SchemaName<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> SchemaName<T>([NotNull] this ITable<T> table, [NotNull] string name)
		{
			if (table == null) throw new ArgumentNullException("table");
			if (name  == null) throw new ArgumentNullException("name");

			table.Expression = Expression.Call(
				null,
				_schemaNameMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(name) });

			var tbl = table as Table<T>;
			if (tbl != null)
				tbl.SchemaName = name;

			return table;
		}

		static readonly MethodInfo _withTableExpressionMethodInfo = MemberHelper.MethodOf(() => WithTableExpression<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> WithTableExpression<T>([NotNull] this ITable<T> table, [NotNull] string expression)
		{
			if (expression == null) throw new ArgumentNullException("expression");

			table.Expression = Expression.Call(
				null,
				_withTableExpressionMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(expression) });

			return table;
		}

		static readonly MethodInfo _with = MemberHelper.MethodOf(() => With<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> With<T>([NotNull] this ITable<T> table, [NotNull] string args)
		{
			if (args == null) throw new ArgumentNullException("args");

			table.Expression = Expression.Call(
				null,
				_with.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Constant(args) });

			return table;
		}

		#endregion

		#region LoadWith

		static readonly MethodInfo _loadWithMethodInfo = MemberHelper.MethodOf(() => LoadWith<int>(null, null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ITable<T> LoadWith<T>(
			[NotNull]                this ITable<T> table,
			[NotNull, InstantHandle] Expression<Func<T,object>> selector)
		{
			if (table == null) throw new ArgumentNullException("table");

			table.Expression = Expression.Call(
				null,
				_loadWithMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
				new[] { table.Expression, Expression.Quote(selector) });

			return table;
		}

		#endregion

		#region Scalar Select

		public static T Select<T>(
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

		static readonly MethodInfo _deleteMethodInfo = MemberHelper.MethodOf(() => Delete<int>(null)).GetGenericMethodDefinition();

		public static int Delete<T>([NotNull] this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					_deleteMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression }));
		}

		static readonly MethodInfo _deleteMethodInfo2 = MemberHelper.MethodOf(() => Delete<int>(null, null)).GetGenericMethodDefinition();

		public static int Delete<T>(
			[NotNull]                this IQueryable<T>       source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException("source");
			if (predicate == null) throw new ArgumentNullException("predicate");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					_deleteMethodInfo2.MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression, Expression.Quote(predicate) }));
		}

		#endregion

		#region Update

		static readonly MethodInfo _updateMethodInfo = MemberHelper.MethodOf(() => Update<int,int>(null, (ITable<int>)null, null)).GetGenericMethodDefinition();

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
					_updateMethodInfo.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		static readonly MethodInfo _updateMethodInfo2 = MemberHelper.MethodOf(() => Update<int>(null, null)).GetGenericMethodDefinition();

		public static int Update<T>(
			[NotNull]                this IQueryable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,T>> setter)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (setter == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					_updateMethodInfo2.MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression, Expression.Quote(setter) }));
		}

		static readonly MethodInfo _updateMethodInfo3 = MemberHelper.MethodOf(() => Update<int>(null, null, null)).GetGenericMethodDefinition();

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
					_updateMethodInfo3.MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression, Expression.Quote(predicate), Expression.Quote(setter) }));
		}

		static readonly MethodInfo _updateMethodInfo4 = MemberHelper.MethodOf(() => Update<int>(null)).GetGenericMethodDefinition();

		public static int Update<T>([NotNull] this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((Updatable<T>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					_updateMethodInfo4.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		static readonly MethodInfo _updateMethodInfo5 = MemberHelper.MethodOf(() => Update<int,int>(null, (Expression<Func<int,int>>)null, null)).GetGenericMethodDefinition();

		public static int Update<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (target == null) throw new ArgumentNullException("target");
			if (setter == null) throw new ArgumentNullException("setter");

			return source.Provider.Execute<int>(
				Expression.Call(
					null,
					_updateMethodInfo5.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, Expression.Quote(target), Expression.Quote(setter) }));
		}

		class Updatable<T> : IUpdatable<T>
		{
			public IQueryable<T> Query;
		}

		static readonly MethodInfo _asUpdatableMethodInfo = MemberHelper.MethodOf(() => AsUpdatable<int>(null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static IUpdatable<T> AsUpdatable<T>([NotNull] this IQueryable<T> source)
		{
			if (source  == null) throw new ArgumentNullException("source");

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_asUpdatableMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { source.Expression }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo = MemberHelper.MethodOf(() =>
			Set<int,int>((IQueryable<int>)null,null,(Expression<Func<int,int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_setMethodInfo.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { source.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo2 = MemberHelper.MethodOf(() =>
			Set<int,int>((IUpdatable<int>)null,null,(Expression<Func<int,int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_setMethodInfo2.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo3 = MemberHelper.MethodOf(() =>
			Set<int,int>((IQueryable<int>)null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_setMethodInfo3.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { source.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo4 = MemberHelper.MethodOf(() =>
			Set<int,int>((IUpdatable<int>)null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_setMethodInfo4.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo5 = MemberHelper.MethodOf(() => Set((IQueryable<int>)null,null,0)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_setMethodInfo5.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { source.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo6 = MemberHelper.MethodOf(() => Set((IUpdatable<int>)null,null,0)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_setMethodInfo6.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)) }));

			return new Updatable<T> { Query = query };
		}

		#endregion

		#region Insert

		static readonly MethodInfo _insertMethodInfo = MemberHelper.MethodOf(() => Insert<int>(null,null)).GetGenericMethodDefinition();

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
					_insertMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression, Expression.Quote(setter) }));
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo = MemberHelper.MethodOf(() => InsertWithIdentity<int>(null,null)).GetGenericMethodDefinition();

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
					_insertWithIdentityMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression, Expression.Quote(setter) }));
		}

		#region ValueInsertable

		class ValueInsertable<T> : IValueInsertable<T>
		{
			public IQueryable<T> Query;
		}

		static readonly MethodInfo _intoMethodInfo = MemberHelper.MethodOf(() => Into<int>(null,null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static IValueInsertable<T> Into<T>(this IDataContext dataContext, [NotNull] ITable<T> target)
		{
			if (target == null) throw new ArgumentNullException("target");

			IQueryable<T> query = target;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_intoMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { Expression.Constant(null, typeof(IDataContext)), query.Expression }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo =
			MemberHelper.MethodOf(() => Value<int,int>((ITable<int>)null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo2 =
			MemberHelper.MethodOf(() => Value((ITable<int>)null,null,0)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo2.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo3 =
			MemberHelper.MethodOf(() => Value<int,int>((IValueInsertable<int>)null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo3.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo4 =
			MemberHelper.MethodOf(() => Value((IValueInsertable<int>)null,null,0)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo4.MakeGenericMethod(new[] { typeof(T), typeof(TV) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _insertMethodInfo2 = MemberHelper.MethodOf(() => Insert<int>(null)).GetGenericMethodDefinition();

		public static int Insert<T>([NotNull] this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((ValueInsertable<T>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertMethodInfo2.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo2 = MemberHelper.MethodOf(() => InsertWithIdentity<int>(null)).GetGenericMethodDefinition();

		public static object InsertWithIdentity<T>([NotNull] this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((ValueInsertable<T>)source).Query;

			return query.Provider.Execute<object>(
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo2.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		#endregion

		#region SelectInsertable

		static readonly MethodInfo _insertMethodInfo3 =
			MemberHelper.MethodOf(() => Insert<int,int>(null,null,null)).GetGenericMethodDefinition();

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
					_insertMethodInfo3.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo3 =
			MemberHelper.MethodOf(() => InsertWithIdentity<int,int>(null,null,null)).GetGenericMethodDefinition();

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
					_insertWithIdentityMethodInfo3.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		class SelectInsertable<T,TT> : ISelectInsertable<T,TT>
		{
			public IQueryable<T> Query;
		}

		static readonly MethodInfo _intoMethodInfo2 =
			MemberHelper.MethodOf(() => Into<int,int>(null,null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(
			[NotNull] this IQueryable<TSource> source,
			[NotNull] ITable<TTarget>          target)
		{
			if (target == null) throw new ArgumentNullException("target");

			var q = source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_intoMethodInfo2.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo5 =
			MemberHelper.MethodOf(() => Value<int,int,int>(null,null,(Expression<Func<int,int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo5.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget), typeof(TValue) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo6 =
			MemberHelper.MethodOf(() => Value<int,int,int>(null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo6.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget), typeof(TValue) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo7 =
			MemberHelper.MethodOf(() => Value<int,int,int>(null,null,0)).GetGenericMethodDefinition();

		[LinqTunnel]
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
					_valueMethodInfo7.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget), typeof(TValue) }),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TValue)) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _insertMethodInfo4 =
			MemberHelper.MethodOf(() => Insert<int,int>(null)).GetGenericMethodDefinition();

		public static int Insert<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertMethodInfo4.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { query.Expression }));
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo4 =
			MemberHelper.MethodOf(() => InsertWithIdentity<int,int>(null)).GetGenericMethodDefinition();

		public static object InsertWithIdentity<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			return query.Provider.Execute<object>(
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo4.MakeGenericMethod(new[] { typeof(TSource), typeof(TTarget) }),
					new[] { query.Expression }));
		}

		#endregion

		#endregion

		#region InsertOrUpdate

		static readonly MethodInfo _insertOrUpdateMethodInfo =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null,null,null)).GetGenericMethodDefinition();

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
					_insertOrUpdateMethodInfo.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression, Expression.Quote(insertSetter), Expression.Quote(onDuplicateKeyUpdateSetter) }));
		}

		static readonly MethodInfo _insertOrUpdateMethodInfo2 =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null,null,null,null)).GetGenericMethodDefinition();

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
					_insertOrUpdateMethodInfo2.MakeGenericMethod(new[] { typeof(T) }),
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

		static readonly MethodInfo _dropMethodInfo2 = MemberHelper.MethodOf(() => Drop<int>(null)).GetGenericMethodDefinition();

		public static int Drop<T>([NotNull] this ITable<T> target)
		{
			if (target == null) throw new ArgumentNullException("target");

			IQueryable<T> query = target;

			return query.Provider.Execute<int>(
				Expression.Call(
					null,
					_dropMethodInfo2.MakeGenericMethod(new[] { typeof(T) }),
					new[] { query.Expression }));
		}

		#endregion

		#region Take / Skip / ElementAt

		static readonly MethodInfo _takeMethodInfo = MemberHelper.MethodOf(() => Take<int>(null,null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static IQueryable<TSource> Take<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (count  == null) throw new ArgumentNullException("count");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_takeMethodInfo.MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(count) }));
		}

		static readonly MethodInfo _takeMethodInfo2 = MemberHelper.MethodOf(() => Take<int>(null,null,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// <see cref="System.Linq.Enumerable.Take{TSource}(System.Collections.Generic.IEnumerable{TSource}, int)"/>
		/// Using this method may cause runtime <see cref="LinqException"/> if take hints are not supported by database
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="source">source</param>
		/// <param name="count">number of records to take</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL Take expression</param>
		/// <returns></returns>
		[LinqTunnel]
		public static IQueryable<TSource> Take<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count,
			[NotNull]                TakeHints                hints)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (count  == null) throw new ArgumentNullException("count");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_takeMethodInfo2.MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(count), Expression.Constant(hints) }));
		}

		static readonly MethodInfo _takeMethodInfo3 = MemberHelper.MethodOf(() => Take<int>(null,0,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// <see cref="System.Linq.Enumerable.Take{TSource}(System.Collections.Generic.IEnumerable{TSource}, int)"/>
		/// Using this method may cause runtime <see cref="LinqException"/> if take hints are not supported by database
		/// </summary>
		/// <typeparam name="TSource"></typeparam>
		/// <param name="source">source</param>
		/// <param name="count">number of records to take</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL Take expression</param>
		/// <returns></returns>
		[LinqTunnel]
		public static IQueryable<TSource> Take<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull]                int                      count,
			[NotNull]                TakeHints                hints)
		{
			if (source == null) throw new ArgumentNullException("source");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_takeMethodInfo3.MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Constant(count), Expression.Constant(hints) }));
		}

		static readonly MethodInfo _skipMethodInfo = MemberHelper.MethodOf(() => Skip<int>(null,null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static IQueryable<TSource> Skip<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (count  == null) throw new ArgumentNullException("count");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_skipMethodInfo.MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(count) }));
		}

		static readonly MethodInfo _elementAtMethodInfo = MemberHelper.MethodOf(() => ElementAt<int>(null,null)).GetGenericMethodDefinition();

		public static TSource ElementAt<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    index)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (index  == null) throw new ArgumentNullException("index");

			return source.Provider.Execute<TSource>(
				Expression.Call(
					null,
					_elementAtMethodInfo.MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(index) }));
		}

		static readonly MethodInfo _elementAtOrDefaultMethodInfo = MemberHelper.MethodOf(() => ElementAtOrDefault<int>(null,null)).GetGenericMethodDefinition();

		public static TSource ElementAtOrDefault<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    index)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (index  == null) throw new ArgumentNullException("index");

			return source.Provider.Execute<TSource>(
				Expression.Call(
					null,
					_elementAtOrDefaultMethodInfo.MakeGenericMethod(new[] { typeof(TSource) }),
					new[] { source.Expression, Expression.Quote(index) }));
		}

		#endregion

		#region Having

		static readonly MethodInfo _setMethodInfo7 = MemberHelper.MethodOf(() => Having((IQueryable<int>)null,null)).GetGenericMethodDefinition();

		[LinqTunnel]
		public static IQueryable<TSource> Having<TSource>(
			[NotNull]                this IQueryable<TSource>       source,
			[NotNull, InstantHandle] Expression<Func<TSource,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException("source");
			if (predicate == null) throw new ArgumentNullException("predicate");

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_setMethodInfo7.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression, Expression.Quote(predicate) }));
		}

		#endregion

		#region IOrderedQueryable

		static readonly MethodInfo _thenOrBy = MemberHelper.MethodOf(() => ThenOrBy((IQueryable<int>)null,(Expression<Func<int, int>>)null)).GetGenericMethodDefinition();

		public static IOrderedQueryable<TSource> ThenOrBy<TSource, TKey>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<TSource, TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException("source");
			if (keySelector == null) throw new ArgumentNullException("keySelector");

			return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_thenOrBy.MakeGenericMethod(new[] { typeof(TSource), typeof(TKey) }),
					new[] { source.Expression, Expression.Quote(keySelector) }));
		}

		static readonly MethodInfo _thenOrByDescending = MemberHelper.MethodOf(() => ThenOrByDescending((IQueryable<int>)null, (Expression<Func<int, int>>)null)).GetGenericMethodDefinition();

		public static IOrderedQueryable<TSource> ThenOrByDescending<TSource, TKey>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<TSource, TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException("source");
			if (keySelector == null) throw new ArgumentNullException("keySelector");

			return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_thenOrByDescending.MakeGenericMethod(new[] { typeof(TSource), typeof(TKey) }),
					new[] { source.Expression, Expression.Quote(keySelector) }));
		}

		#endregion

		static readonly MethodInfo _setMethodInfo8 = MemberHelper.MethodOf(() => GetContext((IQueryable<int>)null)).GetGenericMethodDefinition();

		internal static ContextParser.Context GetContext<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException("source");

			return source.Provider.Execute<ContextParser.Context>(
				Expression.Call(
					null,
					_setMethodInfo8.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression }));
		}

		#region Stub helpers

		internal static TOutput Where<TOutput,TSource,TInput>(this TInput source, Func<TSource,bool> predicate)
		{
			throw new InvalidOperationException();
		}

		#endregion
	}
}
