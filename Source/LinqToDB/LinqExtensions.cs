using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Expressions;
	using Linq;
	using Linq.Builder;

	/// <summary>
	/// Contains extension methods for LINQ queries.
	/// </summary>
	[PublicAPI]
	public static partial class LinqExtensions
	{
		#region Table Helpers

		internal static readonly MethodInfo TableNameMethodInfo = MemberHelper.MethodOf(() => TableName<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Overrides table or view name with new name for current query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of table.</param>
		/// <returns>Table-like query source with new name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> TableName<T>([NotNull] this ITable<T> table, [NotNull, SqlQueryDependent] string name)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (name  == null) throw new ArgumentNullException(nameof(name));

			if (table is Table<T> tbl)
				tbl.TableName = name;
			else
				table.Expression = Expression.Call(
					null,
					TableNameMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { table.Expression, Expression.Constant(name) });

			return table;
		}

		internal static readonly MethodInfo DatabaseNameMethodInfo = MemberHelper.MethodOf(() => DatabaseName<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Overrides database name with new name for current query. This call will have effect only for databases that support
		/// database name in fully-qualified table name.
		/// <para>Supported by: Access, DB2, MySQL, PostgreSQL, SAP HANA, SQLite, Informix, SQL Server, Sybase ASE.</para>
		/// <para>Requires schema name (see <see cref="SchemaName{T}(ITable{T}, string)"/>): DB2, SAP HANA, PostgreSQL.</para>
		/// <para>PostgreSQL supports only name of current database.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of database.</param>
		/// <returns>Table-like query source with new database name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> DatabaseName<T>([NotNull] this ITable<T> table, [NotNull, SqlQueryDependent] string name)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (name  == null) throw new ArgumentNullException(nameof(name));

			if (table is Table<T> tbl)
				tbl.DatabaseName = name;
			else
				table.Expression = Expression.Call(
					null,
					DatabaseNameMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { table.Expression, Expression.Constant(name) });

			return table;
		}

		/// <summary>
		/// Overrides owner/schema name with new name for current query. This call will have effect only for databases that support
		/// owner/schema name in fully-qualified table name.
		/// <see cref="SchemaName{T}(ITable{T}, string)"/> method is a synonym of this method.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, SAP HANA, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of owner/schema.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		[LinqTunnel]
		[Pure]
		[Obsolete("Use SchemaName instead.")]
		public static ITable<T> OwnerName<T>([NotNull] this ITable<T> table, [NotNull, SqlQueryDependent] string name)
		{
			return SchemaName(table, name);
		}

		internal static readonly MethodInfo SchemaNameMethodInfo = MemberHelper.MethodOf(() => SchemaName<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Overrides owner/schema name with new name for current query. This call will have effect only for databases that support
		/// owner/schema name in fully-qualified table name.
		/// <para>Supported by: DB2, Oracle, PostgreSQL, SAP HANA, Informix, SQL Server, Sybase ASE.</para>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="name">Name of owner/schema.</param>
		/// <returns>Table-like query source with new owner/schema name.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> SchemaName<T>([NotNull] this ITable<T> table, [NotNull, SqlQueryDependent] string name)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));
			if (name  == null) throw new ArgumentNullException(nameof(name));

			if (table is Table<T> tbl)
				tbl.SchemaName = name;
			else
				table.Expression = Expression.Call(
					null,
					SchemaNameMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { table.Expression, Expression.Constant(name) });

			return table;
		}

		static readonly MethodInfo _withTableExpressionMethodInfo = MemberHelper.MethodOf(() => WithTableExpression<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Replaces access to a table in generated query with SQL expression.
		/// Example below adds hint to a table. Also see <see cref="With{T}(ITable{T}, string)"/> method.
		/// <code>
		/// var tableWithHint = db.Table.WithTableExpression("{0} {1} with (UpdLock)");
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="expression">SQL template to use instead of table name. Template supports two parameters:
		/// <para> - {0} original table name;</para>
		/// <para> - {1} table alias.</para>
		/// </param>
		/// <returns>Table-like query source with new table source expression.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> WithTableExpression<T>([NotNull] this ITable<T> table, [NotNull, SqlQueryDependent] string expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			table.Expression = Expression.Call(
				null,
				_withTableExpressionMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { table.Expression, Expression.Constant(expression) });

			return table;
		}

		static readonly MethodInfo _with = MemberHelper.MethodOf(() => With<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds table hints to a table in generated query.
		/// Also see <see cref="WithTableExpression{T}(ITable{T}, string)"/> method.
		/// <code>
		/// // will produce following SQL code in generated query: table tablealias with(UpdLock)
		/// var tableWithHint = db.Table.With("UpdLock");
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="args">SQL text, added to WITH({0}) after table name in generated query.</param>
		/// <returns>Table-like query source with table hints.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> With<T>([NotNull] this ITable<T> table, [NotNull, SqlQueryDependent] string args)
		{
			if (args == null) throw new ArgumentNullException(nameof(args));

			table.Expression = Expression.Call(
				null,
				_with.MakeGenericMethod(typeof(T)),
				new[] { table.Expression, Expression.Constant(args) });

			return table;
		}

		#endregion

		#region LoadWith

		static readonly MethodInfo _loadWithMethodInfo = MemberHelper.MethodOf(() => LoadWith<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Specifies associations, that should be loaded for each loaded record from current table.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// Some usage examples:
		/// <code>
		/// // loads records from Table1 with Reference association loaded for each Table1 record
		/// db.Table1.LoadWith(r => r.Reference);
		///
		/// // loads records from Table1 with Reference1 association loaded for each Table1 record
		/// // loads records from Reference2 association for each loaded Reference1 record
		/// db.Table1.LoadWith(r => r.Reference1.Reference2);
		///
		/// // loads records from Table1 with References collection association loaded for each Table1 record
		/// db.Table1.LoadWith(r => r.References);
		///
		/// // loads records from Table1 with Reference1 collection association loaded for each Table1 record
		/// // loads records from Reference2 collection association for each loaded Reference1 record
		/// // loads records from Reference3 association for each loaded Reference2 record
		/// // note that a way you access collection association record (by index, using First() method) doesn't affect
		/// // query results and allways select all records
		/// db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="selector">Association selection expression.</param>
		/// <returns>Table-like query source.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> LoadWith<T>(
			[NotNull]                this ITable<T> table,
			[NotNull, InstantHandle] Expression<Func<T,object>> selector)
		{
			if (table == null) throw new ArgumentNullException(nameof(table));

			table.Expression = Expression.Call(
				null,
				_loadWithMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { table.Expression, Expression.Quote(selector) });

			return table;
		}

		#endregion

		#region Scalar Select

		/// <summary>
		/// Loads scalar value or record from database without explicit table source.
		/// Could be usefull for function calls, querying of database variables or properties, subqueries, execution of code on server side.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <returns>Requested value.</returns>
		[Pure]
		public static T Select<T>(
			[NotNull]                this IDataContext   dataContext,
			[NotNull, InstantHandle] Expression<Func<T>> selector)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (selector    == null) throw new ArgumentNullException(nameof(selector));

			var q = new Table<T>(dataContext, selector);

			foreach (var item in q)
				return item;

			throw new InvalidOperationException();
		}

		/// <summary>
		/// Loads scalar value or record from database without explicit table source asynchronously.
		/// Could be usefull for function calls, querying of database variables or properties, subqueries, execution of code on server side.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="selector">Value selection expression.</param>
		/// <returns>Requested value.</returns>
		[Pure]
		public static async Task<T> SelectAsync<T>(
			[NotNull]                this IDataContext   dataContext,
			[NotNull, InstantHandle] Expression<Func<T>> selector)
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			if (selector    == null) throw new ArgumentNullException(nameof(selector));

			var q = new Table<T>(dataContext, selector);

			var read = false;
			var item = default(T);

			await q.ForEachUntilAsync(r =>
			{
				read = true;
				item = r;
				return false;
			});

			if (read)
				return item;

			throw new InvalidOperationException();
		}

		#endregion

		#region Delete

		static readonly MethodInfo _deleteMethodInfo = MemberHelper.MethodOf(() => Delete<int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes delete operation, using source query as filter for records, that should be deleted.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <returns>Number of deleted records.</returns>
		public static int Delete<T>([NotNull] this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					_deleteMethodInfo.MakeGenericMethod(typeof(T)),
					currentSource.Expression));
		}

		/// <summary>
		/// Executes delete operation asynchronously, using source query as filter for records, that should be deleted.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static async Task<int> DeleteAsync<T>([NotNull] this IQueryable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_deleteMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _deleteMethodInfo2 = MemberHelper.MethodOf(() => Delete<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes delete operation, using source query as initial filter for records, that should be deleted, and predicate expression as additional filter.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="predicate">Filter expression, to specify what records from source should be deleted.</param>
		/// <returns>Number of deleted records.</returns>
		public static int Delete<T>(
			[NotNull]                this IQueryable<T>       source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					_deleteMethodInfo2.MakeGenericMethod(typeof(T)),
					new[] { currentSource.Expression, Expression.Quote(predicate) }));
		}

		/// <summary>
		/// Executes delete operation asynchronously, using source query as initial filter for records, that should be deleted, and predicate expression as additional filter.
		/// </summary>
		/// <typeparam name="T">Mapping class for delete operation target table.</typeparam>
		/// <param name="source">Query that returns records to delete.</param>
		/// <param name="predicate">Filter expression, to specify what records from source should be deleted.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of deleted records.</returns>
		public static async Task<int> DeleteAsync<T>(
			[NotNull]           this IQueryable<T>            source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate,
			CancellationToken                                 token = default)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_deleteMethodInfo2.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Quote(predicate) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		#endregion

		#region Update

		internal static readonly MethodInfo UpdateMethodInfo =
			MemberHelper.MethodOf(() => Update<int,int>(null, (ITable<int>)null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					UpdateMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					new[] { currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Executes update-from-source operation asynchronously against target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static async Task<int> UpdateAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				UpdateMethodInfo.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				new[] { currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		internal static readonly MethodInfo UpdateMethodInfo2 = MemberHelper.MethodOf(() => Update<int>(null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>([NotNull] this IQueryable<T> source, [NotNull, InstantHandle] Expression<Func<T,T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					UpdateMethodInfo2.MakeGenericMethod(typeof(T)),
					new[] { currentSource.Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Executes update operation asynchronously using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static async Task<int> UpdateAsync<T>(
			[NotNull]           this IQueryable<T>         source,
			[NotNull, InstantHandle] Expression<Func<T,T>> setter,
			CancellationToken                              token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				UpdateMethodInfo2.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _updateMethodInfo3 = MemberHelper.MethodOf(() => Update<int>(null, null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes update operation using source query as record filter with additional filter expression.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="predicate">Filter expression, to specify what records from source query should be updated.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>(
			[NotNull]                this IQueryable<T>       source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate,
			[NotNull, InstantHandle] Expression<Func<T,T>>    setter)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (setter    == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					_updateMethodInfo3.MakeGenericMethod(typeof(T)),
					new[] { currentSource.Expression, Expression.Quote(predicate), Expression.Quote(setter) }));
		}

		/// <summary>
		/// Executes update operation asynchronously using source query as record filter with additional filter expression.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="predicate">Filter expression, to specify what records from source query should be updated.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static async Task<int> UpdateAsync<T>(
			[NotNull]           this IQueryable<T>            source,
			[NotNull, InstantHandle] Expression<Func<T,bool>> predicate,
			[NotNull, InstantHandle] Expression<Func<T,T>>    setter,
			CancellationToken                                 token = default)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (setter    == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_updateMethodInfo3.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Quote(predicate), Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _updateMethodInfo4 = MemberHelper.MethodOf(() => Update<int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes update operation for already configured update query.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Update query.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>([NotNull] this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_updateMethodInfo4.MakeGenericMethod(typeof(T)),
					currentQuery.Expression));
		}

		/// <summary>
		/// Executes update operation asynchronously for already configured update query.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Update query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static async Task<int> UpdateAsync<T>([NotNull] this IUpdatable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var q = ((Updatable<T>)source).Query;

			var currentQuery = ProcessSourceQueryable?.Invoke(q) ?? q;

			var expr = Expression.Call(
				null,
				_updateMethodInfo4.MakeGenericMethod(typeof(T)),
				currentQuery.Expression);

			if (currentQuery is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentQuery.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _updateMethodInfo5 = MemberHelper.MethodOf(()
			=> Update(null, (Expression<Func<int,int>>)null, null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes update-from-source operation against target table.
		/// Also see <seealso cref="Update{TSource, TTarget}(IQueryable{TSource}, ITable{TTarget}, Expression{Func{TSource, TTarget}})"/> method.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table selection expression.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					_updateMethodInfo5.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					new[] { currentSource.Expression, Expression.Quote(target), Expression.Quote(setter) }));
		}

		/// <summary>
		/// Executes update-from-source operation asynchronously against target table.
		/// Also see <seealso cref="UpdateAsync{TSource, TTarget}(IQueryable{TSource}, ITable{TTarget}, Expression{Func{TSource, TTarget}}, CancellationToken)"/> method.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table selection expression.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static async Task<int> UpdateAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_updateMethodInfo5.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				new[] { currentSource.Expression, Expression.Quote(target), Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		class Updatable<T> : IUpdatable<T>
		{
			public IQueryable<T> Query;
		}

		static readonly MethodInfo _asUpdatableMethodInfo = MemberHelper.MethodOf(() => AsUpdatable<int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Casts <see cref="IQueryable{T}"/> query to <see cref="IUpdatable{T}"/> query.
		/// </summary>
		/// <typeparam name="T">Query record type.</typeparam>
		/// <param name="source">Source <see cref="IQueryable{T}"/> query.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> AsUpdatable<T>([NotNull] this IQueryable<T> source)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_asUpdatableMethodInfo.MakeGenericMethod(typeof(T)),
					source.Expression));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo = MemberHelper.MethodOf(() =>
			Set<int,int>((IQueryable<int>)null,null,(Expression<Func<int,int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression. Uses updated record as parameter.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IQueryable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<T,TV>> update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_setMethodInfo.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { source.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo2 = MemberHelper.MethodOf(() =>
			Set<int,int>((IUpdatable<int>)null,null,(Expression<Func<int,int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression. Uses updated record as parameter.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IUpdatable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<T,TV>> update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_setMethodInfo2.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo3 = MemberHelper.MethodOf(() =>
			Set<int,int>((IQueryable<int>)null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IQueryable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<TV>>   update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_setMethodInfo3.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { source.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo4 = MemberHelper.MethodOf(() =>
			Set<int,int>((IUpdatable<int>)null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="update">Updated field setter expression.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IUpdatable<T>    source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			[NotNull, InstantHandle] Expression<Func<TV>>   update)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update  == null) throw new ArgumentNullException(nameof(update));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_setMethodInfo4.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(extract), Expression.Quote(update) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo5 = MemberHelper.MethodOf(() => Set((IQueryable<int>)null,null,0)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="value">Value, assigned to updated field.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IQueryable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			TV                                              value)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_setMethodInfo5.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { source.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)) }));

			return new Updatable<T> { Query = query };
		}

		static readonly MethodInfo _setMethodInfo6 = MemberHelper.MethodOf(() => Set((IUpdatable<int>)null,null,0)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds update field expression to query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Updated field type.</typeparam>
		/// <param name="source">Source query with records to update.</param>
		/// <param name="extract">Updated field selector expression.</param>
		/// <param name="value">Value, assigned to updated field.</param>
		/// <returns><see cref="IUpdatable{T}"/> query.</returns>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T,TV>(
			[NotNull]                this IUpdatable<T>     source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> extract,
			TV                                              value)
		{
			if (source  == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_setMethodInfo6.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)) }));

			return new Updatable<T> { Query = query };
		}

		#endregion

		#region Insert

		static readonly MethodInfo _insertMethodInfo = MemberHelper.MethodOf(() => Insert<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts single record into target table.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			IQueryable<T> query = target;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { currentQuery.Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Inserts single record into target table asynchronously.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int> InsertAsync<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter,
			CancellationToken                            token = default)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			IQueryable<T> source = target;

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_insertMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo = MemberHelper.MethodOf(() => InsertWithIdentity<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static object InsertWithIdentity<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			IQueryable<T> query = target;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<object>(
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { currentQuery.Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int InsertWithInt32Identity<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<int>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long InsertWithInt64Identity<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<long>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal InsertWithDecimalIdentity<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter)
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<decimal>(InsertWithIdentity(target, setter));
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<object> InsertWithIdentityAsync<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter,
			CancellationToken                            token = default)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			IQueryable<T> source = target;

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_insertWithIdentityMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<object>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<object>(expr), token);
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int> InsertWithInt32IdentityAsync<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter,
			CancellationToken                            token = default)
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<int>(await InsertWithIdentityAsync(target, setter, token));
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long> InsertWithInt64IdentityAsync<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter,
			CancellationToken                            token = default)
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<long>(await InsertWithIdentityAsync(target, setter, token));
		}

		/// <summary>
		/// Inserts single record into target table asynchronously and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Inserted record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Insert expression. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal> InsertWithDecimalIdentityAsync<T>(
			[NotNull]                this ITable<T>      target,
			[NotNull, InstantHandle] Expression<Func<T>> setter,
			CancellationToken                            token = default)
		{
			return target.DataContext.MappingSchema.ChangeTypeTo<decimal>(await InsertWithIdentityAsync(target, setter, token));
		}

		#region ValueInsertable

		class ValueInsertable<T> : IValueInsertable<T>
		{
			public IQueryable<T> Query;
		}

		static readonly MethodInfo _intoMethodInfo = MemberHelper.MethodOf(() => Into<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Starts insert operation LINQ query definition.
		/// </summary>
		/// <typeparam name="T">Target table mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Into<T>(this IDataContext dataContext, [NotNull] ITable<T> target)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			IQueryable<T> query = target;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_intoMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { Expression.Constant(null, typeof(IDataContext)), query.Expression }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo =
			MemberHelper.MethodOf(() => Value<int,int>((ITable<int>)null,null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Starts insert operation LINQ query definition from field setter expression.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Source table to insert to.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this ITable<T>         source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> field,
			[NotNull, InstantHandle] Expression<Func<TV>>   value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = (IQueryable<T>)source;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_valueMethodInfo.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo2 =
			MemberHelper.MethodOf(() => Value((ITable<int>)null,null,0)).GetGenericMethodDefinition();

		/// <summary>
		/// Starts insert operation LINQ query definition from field setter expression.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Source table to insert to.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this ITable<T>         source,
			[NotNull, InstantHandle] Expression<Func<T,TV>> field,
			TV                                              value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));

			var query = (IQueryable<T>)source;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_valueMethodInfo2.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo3 =
			MemberHelper.MethodOf(() => Value<int,int>((IValueInsertable<int>)null,null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this IValueInsertable<T> source,
			[NotNull, InstantHandle] Expression<Func<T,TV>>   field,
			[NotNull, InstantHandle] Expression<Func<TV>>     value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = ((ValueInsertable<T>)source).Query;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_valueMethodInfo3.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo4 =
			MemberHelper.MethodOf(() => Value((IValueInsertable<int>)null,null,0)).GetGenericMethodDefinition();

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <typeparam name="TV">Setter field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static IValueInsertable<T> Value<T,TV>(
			[NotNull]                this IValueInsertable<T> source,
			[NotNull, InstantHandle] Expression<Func<T,TV>>   field,
			TV                                                value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));

			var query = ((ValueInsertable<T>)source).Query;

			var q = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					_valueMethodInfo4.MakeGenericMethod(typeof(T), typeof(TV)),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TV)) }));

			return new ValueInsertable<T> { Query = q };
		}

		static readonly MethodInfo _insertMethodInfo2 = MemberHelper.MethodOf(() => Insert<int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes insert query.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<T>([NotNull] this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((ValueInsertable<T>)source).Query;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertMethodInfo2.MakeGenericMethod(typeof(T)),
					currentQuery.Expression));
		}

		/// <summary>
		/// Executes insert query asynchronously.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int> InsertAsync<T>([NotNull] this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var queryable = ((ValueInsertable<T>)source).Query;

			var currentQueryable = ProcessSourceQueryable?.Invoke(queryable) ?? queryable;

			var expr = Expression.Call(
				null,
				_insertMethodInfo2.MakeGenericMethod(typeof(T)), currentQueryable.Expression);

			if (currentQueryable is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentQueryable.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo2 = MemberHelper.MethodOf(() => InsertWithIdentity<int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes insert query and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		[Pure]
		public static object InsertWithIdentity<T>([NotNull] this IValueInsertable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var queryable = ((ValueInsertable<T>)source).Query;

			var currentQueryable = ProcessSourceQueryable?.Invoke(queryable) ?? queryable;

			return currentQueryable.Provider.Execute<object>(
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo2.MakeGenericMethod(typeof(T)),
					currentQueryable.Expression));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static int? InsertWithInt32Identity<T>([NotNull] this IValueInsertable<T> source)
		{
			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static long? InsertWithInt64Identity<T>([NotNull] this IValueInsertable<T> source)
		{
			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static decimal? InsertWithDecimalIdentity<T>([NotNull] this IValueInsertable<T> source)
		{
			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<object> InsertWithIdentityAsync<T>(
			[NotNull] this IValueInsertable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var queryable = ((ValueInsertable<T>)source).Query;

			var currentQueryable = ProcessSourceQueryable?.Invoke(queryable) ?? queryable;

			var expr = Expression.Call(
				null,
				_insertWithIdentityMethodInfo2.MakeGenericMethod(typeof(T)),
				currentQueryable.Expression);

			if (currentQueryable is IQueryProviderAsync query)
				return await query.ExecuteAsync<object>(expr, token);

			return await TaskEx.Run(() => currentQueryable.Provider.Execute<object>(expr), token);
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<T>(
			[NotNull] this IValueInsertable<T> source, CancellationToken token = default)
		{
			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, token));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<T>(
			[NotNull] this IValueInsertable<T> source, CancellationToken token = default)
		{
			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, token));
		}

		/// <summary>
		/// Executes insert query asynchronously and returns identity value of inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Inserted record's identity value.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<T>(
			[NotNull] this IValueInsertable<T> source, CancellationToken token = default)
		{
			return ((ExpressionQuery<T>)((ValueInsertable<T>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, token));
		}

		#endregion

		#region SelectInsertable

		internal static readonly MethodInfo InsertMethodInfo3 =
			MemberHelper.MethodOf(() => Insert<int,int>(null,null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts records from source query into target table.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<int>(
				Expression.Call(
					null,
					InsertMethodInfo3.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					new[] { currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int> InsertAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				InsertMethodInfo3.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				new[] { currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo3 =
			MemberHelper.MethodOf(() => InsertWithIdentity<int,int>(null,null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static object InsertWithIdentity<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<object>(
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo3.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					new[] { currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) }));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static int? InsertWithInt32Identity<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			return ((ExpressionQuery<TSource>)source).DataContext.MappingSchema.ChangeTypeTo<int?>(
				InsertWithIdentity(source, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static long? InsertWithInt64Identity<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			return ((ExpressionQuery<TSource>)source).DataContext.MappingSchema.ChangeTypeTo<long?>(
				InsertWithIdentity(source, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static decimal? InsertWithDecimalIdentity<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			return ((ExpressionQuery<TSource>)source).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				InsertWithIdentity(source, target, setter));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<object> InsertWithIdentityAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo3.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					new[] { currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<object>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<object>(expr), token);
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			return ((ExpressionQuery<TSource>)source).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, target, setter, token));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			return ((ExpressionQuery<TSource>)source).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, target, setter, token));
		}

		/// <summary>
		/// Inserts records from source query into target table asynchronously and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type</typeparam>
		/// <param name="source">Source query, that returns data for insert operation.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Last inserted record's identity value.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<TSource,TTarget>(
			[NotNull]                this IQueryable<TSource>          source,
			[NotNull]                ITable<TTarget>                   target,
			[NotNull, InstantHandle] Expression<Func<TSource,TTarget>> setter,
			CancellationToken                                          token = default)
		{
			return ((ExpressionQuery<TSource>)source).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, target, setter, token));
		}

		class SelectInsertable<T,TT> : ISelectInsertable<T,TT>
		{
			public IQueryable<T> Query;
		}

		static readonly MethodInfo _intoMethodInfo2 =
			MemberHelper.MethodOf(() => Into<int,int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Converts LINQ query into insert query with source query data as data to insert.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Insertable source query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Into<TSource,TTarget>(
			[NotNull] this IQueryable<TSource> source,
			[NotNull] ITable<TTarget>          target)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var q = source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_intoMethodInfo2.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					new[] { source.Expression, ((IQueryable<TTarget>)target).Expression }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo5 =
			MemberHelper.MethodOf(() => Value<int,int,int>(null,null,(Expression<Func<int,int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression. Accepts source record as parameter.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			[NotNull]                this ISelectInsertable<TSource,TTarget> source,
			[NotNull, InstantHandle] Expression<Func<TTarget,TValue>>        field,
			[NotNull, InstantHandle] Expression<Func<TSource,TValue>>        value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var q = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_valueMethodInfo5.MakeGenericMethod(typeof(TSource), typeof(TTarget), typeof(TValue)),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo6 =
			MemberHelper.MethodOf(() => Value<int,int,int>(null,null,(Expression<Func<int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value expression.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			[NotNull]                this ISelectInsertable<TSource,TTarget> source,
			[NotNull, InstantHandle] Expression<Func<TTarget,TValue>>        field,
			[NotNull, InstantHandle] Expression<Func<TValue>>                value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));
			if (value  == null) throw new ArgumentNullException(nameof(value));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var q = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_valueMethodInfo6.MakeGenericMethod(typeof(TSource), typeof(TTarget), typeof(TValue)),
					new[] { query.Expression, Expression.Quote(field), Expression.Quote(value) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _valueMethodInfo7 =
			MemberHelper.MethodOf(() => Value<int,int,int>(null,null,0)).GetGenericMethodDefinition();

		/// <summary>
		/// Add field setter to insert operation LINQ query.
		/// </summary>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TTarget">Target record type</typeparam>
		/// <typeparam name="TValue">Field type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="field">Setter field selector expression.</param>
		/// <param name="value">Setter field value.</param>
		/// <returns>Insert query.</returns>
		[LinqTunnel]
		[Pure]
		public static ISelectInsertable<TSource,TTarget> Value<TSource,TTarget,TValue>(
			[NotNull]                this ISelectInsertable<TSource,TTarget> source,
			[NotNull, InstantHandle] Expression<Func<TTarget,TValue>>        field,
			TValue                                                           value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (field  == null) throw new ArgumentNullException(nameof(field));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var q = query.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_valueMethodInfo7.MakeGenericMethod(typeof(TSource), typeof(TTarget), typeof(TValue)),
					new[] { query.Expression, Expression.Quote(field), Expression.Constant(value, typeof(TValue)) }));

			return new SelectInsertable<TSource,TTarget> { Query = q };
		}

		static readonly MethodInfo _insertMethodInfo4 =
			MemberHelper.MethodOf(() => Insert<int,int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes configured insert query.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int Insert<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((SelectInsertable<TSource,TTarget>)source).Query;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertMethodInfo4.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					currentQuery.Expression));
		}

		/// <summary>
		/// Executes configured insert query asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int> InsertAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var queryable = ((SelectInsertable<TSource,TTarget>)source).Query;

			var currentQueryable = ProcessSourceQueryable?.Invoke(queryable) ?? queryable;

			var expr = Expression.Call(
				null,
				_insertMethodInfo4.MakeGenericMethod(typeof(TSource), typeof(TTarget)), currentQueryable.Expression);

			if (currentQueryable is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentQueryable.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _insertWithIdentityMethodInfo4 =
			MemberHelper.MethodOf(() => InsertWithIdentity<int,int>(null)).GetGenericMethodDefinition();

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static object InsertWithIdentity<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var queryable = ((SelectInsertable<TSource,TTarget>)source).Query;

			var currentQueryable = ProcessSourceQueryable?.Invoke(queryable) ?? queryable;

			return currentQueryable.Provider.Execute<object>(
				Expression.Call(
					null,
					_insertWithIdentityMethodInfo4.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
					currentQueryable.Expression));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static int? InsertWithInt32Identity<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static long? InsertWithInt64Identity<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <returns>Number of affected records.</returns>
		public static decimal? InsertWithDecimalIdentity<TSource,TTarget>([NotNull] this ISelectInsertable<TSource,TTarget> source)
		{
			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				InsertWithIdentity(source));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<object> InsertWithIdentityAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var queryable = ((SelectInsertable<TSource,TTarget>)source).Query;

			var currentQueryable = ProcessSourceQueryable?.Invoke(queryable) ?? queryable;

			var expr = Expression.Call(
				null,
				_insertWithIdentityMethodInfo4.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentQueryable.Expression);

			if (currentQueryable is IQueryProviderAsync query)
				return await query.ExecuteAsync<object>(expr, token);

			return await TaskEx.Run(() => currentQueryable.Provider.Execute<object>(expr), token);
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="int"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int?> InsertWithInt32IdentityAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<int?>(
				await InsertWithIdentityAsync(source, token));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="long"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<long?> InsertWithInt64IdentityAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<long?>(
				await InsertWithIdentityAsync(source, token));
		}

		/// <summary>
		/// Executes configured insert query asynchronously and returns identity value of last inserted record as <see cref="decimal"/> value.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table record type.</typeparam>
		/// <param name="source">Insert query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<decimal?> InsertWithDecimalIdentityAsync<TSource,TTarget>(
			[NotNull] this ISelectInsertable<TSource,TTarget> source, CancellationToken token = default)
		{
			return ((ExpressionQuery<TSource>)((SelectInsertable<TSource,TTarget>)source).Query).DataContext.MappingSchema.ChangeTypeTo<decimal?>(
				await InsertWithIdentityAsync(source, token));
		}

		#endregion

		#endregion

		#region InsertOrUpdate

		static readonly MethodInfo _insertOrUpdateMethodInfo =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null,null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts new record into target table or updates existing record if record with the same primary key value already exists in target table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrUpdate<T>(
			[NotNull]                this ITable<T>        target,
			[NotNull, InstantHandle] Expression<Func<T>>   insertSetter,
			[NotNull, InstantHandle] Expression<Func<T,T>> onDuplicateKeyUpdateSetter)
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));
			if (onDuplicateKeyUpdateSetter == null) throw new ArgumentNullException(nameof(onDuplicateKeyUpdateSetter));

			IQueryable<T> query = target;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertOrUpdateMethodInfo.MakeGenericMethod(typeof(T)),
					new[] { currentQuery.Expression, Expression.Quote(insertSetter), Expression.Quote(onDuplicateKeyUpdateSetter) }));
		}

		/// <summary>
		/// Asynchronously inserts new record into target table or updates existing record if record with the same primary key value already exists in target table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int> InsertOrUpdateAsync<T>(
			[NotNull]                this ITable<T>        target,
			[NotNull, InstantHandle] Expression<Func<T>>   insertSetter,
			[NotNull, InstantHandle] Expression<Func<T,T>> onDuplicateKeyUpdateSetter,
			CancellationToken                              token = default)
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));
			if (onDuplicateKeyUpdateSetter == null) throw new ArgumentNullException(nameof(onDuplicateKeyUpdateSetter));

			IQueryable<T> source = target;

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Quote(insertSetter), Expression.Quote(onDuplicateKeyUpdateSetter) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		static readonly MethodInfo _insertOrUpdateMethodInfo2 =
			MemberHelper.MethodOf(() => InsertOrUpdate<int>(null,null,null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Inserts new record into target table or updates existing record if record with the same key value already exists in target table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="keySelector">Key fields selector to specify what fields and values must be used as key fields for selection between insert and update operations.
		/// Expression supports only target table record new expression with field initializers for each key field. Assigned key field value will be used as key value by operation type selector.</param>
		/// <returns>Number of affected records.</returns>
		public static int InsertOrUpdate<T>(
			[NotNull]                this ITable<T>        target,
			[NotNull, InstantHandle] Expression<Func<T>>   insertSetter,
			[NotNull, InstantHandle] Expression<Func<T,T>> onDuplicateKeyUpdateSetter,
			[NotNull, InstantHandle] Expression<Func<T>>   keySelector)
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));
			if (onDuplicateKeyUpdateSetter == null) throw new ArgumentNullException(nameof(onDuplicateKeyUpdateSetter));
			if (keySelector                == null) throw new ArgumentNullException(nameof(keySelector));

			IQueryable<T> query = target;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_insertOrUpdateMethodInfo2.MakeGenericMethod(typeof(T)),
					currentQuery.Expression,
					Expression.Quote(insertSetter),
					Expression.Quote(onDuplicateKeyUpdateSetter),
					Expression.Quote(keySelector)));
		}

		/// <summary>
		/// Asynchronously inserts new record into target table or updates existing record if record with the same key value already exists in target table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="insertSetter">Inserted record constructor expression.
		/// Expression supports only target table record new expression with field initializers.</param>
		/// <param name="onDuplicateKeyUpdateSetter">Updated record constructor expression.
		/// Expression supports only target table record new expression with field initializers.
		/// Accepts updated record as parameter.</param>
		/// <param name="keySelector">Key fields selector to specify what fields and values must be used as key fields for selection between insert and update operations.
		/// Expression supports only target table record new expression with field initializers for each key field. Assigned key field value will be used as key value by operation type selector.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records.</returns>
		public static async Task<int> InsertOrUpdateAsync<T>(
			[NotNull]                this ITable<T>        target,
			[NotNull, InstantHandle] Expression<Func<T>>   insertSetter,
			[NotNull, InstantHandle] Expression<Func<T,T>> onDuplicateKeyUpdateSetter,
			[NotNull, InstantHandle] Expression<Func<T>>   keySelector,
			CancellationToken                              token = default)
		{
			if (target                     == null) throw new ArgumentNullException(nameof(target));
			if (insertSetter               == null) throw new ArgumentNullException(nameof(insertSetter));
			if (onDuplicateKeyUpdateSetter == null) throw new ArgumentNullException(nameof(onDuplicateKeyUpdateSetter));
			if (keySelector                == null) throw new ArgumentNullException(nameof(keySelector));

			IQueryable<T> source = target;

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_insertOrUpdateMethodInfo2.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Quote(insertSetter),
				Expression.Quote(onDuplicateKeyUpdateSetter),
				Expression.Quote(keySelector));

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		#endregion

		#region Drop

		static readonly MethodInfo _dropMethodInfo2 = MemberHelper.MethodOf(() => Drop<int>(null, true)).GetGenericMethodDefinition();

		/// <summary>
		/// Drops database table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Dropped table.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static int Drop<T>([NotNull] this ITable<T> target, bool throwExceptionIfNotExists = true)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			IQueryable<T> query = target;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			var expr = Expression.Call(
				null,
				_dropMethodInfo2.MakeGenericMethod(typeof(T)),
				new[] { currentQuery.Expression, Expression.Constant(throwExceptionIfNotExists) });

			if (throwExceptionIfNotExists)
			{
				return currentQuery.Provider.Execute<int>(expr);
			}

			try
			{
				return currentQuery.Provider.Execute<int>(expr);
			}
			catch
			{
			}

			return 0;
		}

		/// <summary>
		/// Drops database table asynchronously.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Dropped table.</param>
		/// <param name="throwExceptionIfNotExists">If <c>false</c>, any exception during drop operation will be silently catched and <c>0</c> returned.
		/// This behavior is not correct and will be fixed in future to mask only missing table exceptions.
		/// Tracked by <a href="https://github.com/linq2db/linq2db/issues/798">issue</a>.
		/// Default value: <c>true</c>.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static async Task<int> DropAsync<T>(
			[NotNull] this ITable<T> target,
			bool                     throwExceptionIfNotExists = true,
			CancellationToken        token = default)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			IQueryable<T> source = target;

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
					null,
					_dropMethodInfo2.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Constant(throwExceptionIfNotExists)  });

			var query = currentSource as IQueryProviderAsync;

			if (throwExceptionIfNotExists)
			{
				if (query != null)
					return await query.ExecuteAsync<int>(expr, token);

				return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
			}

			try
			{
				if (query != null)
					return await query.ExecuteAsync<int>(expr, token);

				return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
			}
			catch
			{
			}

			return 0;
		}

		#endregion

		#region Truncate

		static readonly MethodInfo _truncateMethodInfo = MemberHelper.MethodOf(() => Truncate<int>(null, true)).GetGenericMethodDefinition();

		/// <summary>
		/// Truncates database table.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Truncated table.</param>
		/// <param name="resetIdentity">Performs reset identity column.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static int Truncate<T>([NotNull] this ITable<T> target, bool resetIdentity = true)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			IQueryable<T> query = target;

			var currentQuery = ProcessSourceQueryable?.Invoke(query) ?? query;

			var expr = Expression.Call(
				null,
				_truncateMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { currentQuery.Expression, Expression.Constant(resetIdentity) });

			return currentQuery.Provider.Execute<int>(expr);
		}

		/// <summary>
		/// Truncates database table asynchronously.
		/// </summary>
		/// <typeparam name="T">Table record type.</typeparam>
		/// <param name="target">Truncated table.</param>
		/// <param name="resetIdentity">Performs reset identity column.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of affected records. Usually <c>-1</c> as it is not data modification operation.</returns>
		public static async Task<int> TruncateAsync<T>(
			[NotNull] this ITable<T> target,
			bool                     resetIdentity = true,
			CancellationToken        token = default)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			IQueryable<T> source = target;

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr = Expression.Call(
				null,
				_truncateMethodInfo.MakeGenericMethod(typeof(T)),
				new[] { currentSource.Expression, Expression.Constant(resetIdentity) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<int>(expr), token);
		}

		#endregion

		#region Take / Skip / ElementAt

		static readonly MethodInfo _takeMethodInfo = MemberHelper.MethodOf(() => Take<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines number of records to select.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_takeMethodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression, Expression.Quote(count) }));
		}

		static readonly MethodInfo _takeMethodInfo2 = MemberHelper.MethodOf(() => Take<int>(null,null,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query. Allows to specify TAKE clause hints.
		/// Using this method may cause runtime <see cref="LinqException"/> if take hints are not supported by database.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines SQL TAKE parameter value.</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL TAKE clause.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    count,
			[SqlQueryDependent]      TakeHints                hints)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_takeMethodInfo2.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression, Expression.Quote(count), Expression.Constant(hints) }));
		}

		static readonly MethodInfo _takeMethodInfo3 = MemberHelper.MethodOf(() => Take<int>(null,0,TakeHints.Percent)).GetGenericMethodDefinition();

		/// <summary>
		/// Limits number of records, returned from query. Allows to specify TAKE clause hints.
		/// Using this method may cause runtime <see cref="LinqException"/> if take hints are not supported by database.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">SQL TAKE parameter value.</param>
		/// <param name="hints"><see cref="TakeHints"/> hints for SQL TAKE clause.</param>
		/// <returns>Query with limit applied.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Take<TSource>(
			[NotNull]      this IQueryable<TSource> source,
			                    int                 count,
			[SqlQueryDependent] TakeHints           hints)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_takeMethodInfo3.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression, Expression.Constant(count), Expression.Constant(hints) }));
		}

		static readonly MethodInfo _skipMethodInfo = MemberHelper.MethodOf(() => Skip<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Ignores first N records from source query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="count">Expression that defines number of records to skip.</param>
		/// <returns>Query without skipped records.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Skip<TSource>(
			[NotNull]           this IQueryable<TSource>   source,
			[NotNull, InstantHandle] Expression<Func<int>> count)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (count  == null) throw new ArgumentNullException(nameof(count));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_skipMethodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression, Expression.Quote(count) }));
		}

		static readonly MethodInfo _elementAtMethodInfo = MemberHelper.MethodOf(() => ElementAt<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Selects record at specified position from source query.
		/// If query doesn't return enough records, <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <exception cref="InvalidOperationException">Source query doesn't have record with specified index.</exception>
		/// <returns>Record at specified position.</returns>
		[Pure]
		public static TSource ElementAt<TSource>(
			[NotNull]           this IQueryable<TSource>   source,
			[NotNull, InstantHandle] Expression<Func<int>> index)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TSource>(
				Expression.Call(
					null,
					_elementAtMethodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { currentSource.Expression, Expression.Quote(index) }));
		}

		/// <summary>
		/// Selects record at specified position from source query asynchronously.
		/// If query doesn't return enough records, <see cref="InvalidOperationException"/> will be thrown.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <exception cref="InvalidOperationException">Source query doesn't have record with specified index.</exception>
		/// <returns>Record at specified position.</returns>
		[Pure]
		public static async Task<TSource> ElementAtAsync<TSource>(
			[NotNull]           this IQueryable<TSource>   source,
			[NotNull, InstantHandle] Expression<Func<int>> index,
			CancellationToken                              token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					_elementAtMethodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { currentSource.Expression, Expression.Quote(index) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<TSource>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<TSource>(expr), token);
		}

		static readonly MethodInfo _elementAtOrDefaultMethodInfo = MemberHelper.MethodOf(() => ElementAtOrDefault<int>(null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Selects record at specified position from source query.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <returns>Record at specified position or default value, if source query doesn't have record with such index.</returns>
		[Pure]
		public static TSource ElementAtOrDefault<TSource>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<int>>    index)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<TSource>(
				Expression.Call(
					null,
					_elementAtOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { currentSource.Expression, Expression.Quote(index) }));
		}

		/// <summary>
		/// Selects record at specified position from source query asynchronously.
		/// </summary>
		/// <typeparam name="TSource">Source table record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="index">Expression that defines index of record to select.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Record at specified position or default value, if source query doesn't have record with such index.</returns>
		[Pure]
		public static async Task<TSource> ElementAtOrDefaultAsync<TSource>(
			[NotNull]           this IQueryable<TSource>   source,
			[NotNull, InstantHandle] Expression<Func<int>> index,
			CancellationToken                              token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (index  == null) throw new ArgumentNullException(nameof(index));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			var expr =
				Expression.Call(
					null,
					_elementAtOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
					new[] { currentSource.Expression, Expression.Quote(index) });

			if (currentSource is IQueryProviderAsync query)
				return await query.ExecuteAsync<TSource>(expr, token);

			return await TaskEx.Run(() => currentSource.Provider.Execute<TSource>(expr), token);
		}

		#endregion

		#region Having

		static readonly MethodInfo _setMethodInfo7 = MemberHelper.MethodOf(() => Having((IQueryable<int>)null,null)).GetGenericMethodDefinition();

		/// <summary>
		/// Filters source query using HAVING SQL clause.
		/// In general you don't need to use this method as linq2db is able to propely identify current context for
		/// <see cref="Queryable.Where{TSource}(IQueryable{TSource}, Expression{Func{TSource, bool}})"/> method and generate
		/// HAVING clause.
		/// <a href="https://github.com/linq2db/linq2db/issues/133">More details</a>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query to filter.</param>
		/// <param name="predicate">Filtering expression.</param>
		/// <returns>Filtered query.</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<TSource> Having<TSource>(
			[NotNull]                this IQueryable<TSource>       source,
			[NotNull, InstantHandle] Expression<Func<TSource,bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_setMethodInfo7.MakeGenericMethod(typeof(TSource)),
					new[] { source.Expression, Expression.Quote(predicate) }));
		}

		#endregion

		#region IOrderedQueryable

		static readonly MethodInfo _thenOrBy = MemberHelper.MethodOf(() =>
			ThenOrBy(null,(Expression<Func<int, int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds ascending sort expression to a query.
		/// If query already sorted, existing sorting will be preserved and updated with new sort.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Sort expression type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Sort expression selector.</param>
		/// <returns>Sorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IOrderedQueryable<TSource> ThenOrBy<TSource, TKey>(
			[NotNull]           this IQueryable<TSource>            source,
			[NotNull, InstantHandle] Expression<Func<TSource,TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_thenOrBy.MakeGenericMethod(typeof(TSource), typeof(TKey)),
					new[] { source.Expression, Expression.Quote(keySelector) }));
		}

		static readonly MethodInfo _thenOrByDescending = MemberHelper.MethodOf(() =>
			ThenOrByDescending(null, (Expression<Func<int, int>>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Adds descending sort expression to a query.
		/// If query already sorted, existing sorting will be preserved and updated with new sort.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TKey">Sort expression type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="keySelector">Sort expression selector.</param>
		/// <returns>Sorted query.</returns>
		[LinqTunnel]
		[Pure]
		public static IOrderedQueryable<TSource> ThenOrByDescending<TSource, TKey>(
			[NotNull]                this IQueryable<TSource> source,
			[NotNull, InstantHandle] Expression<Func<TSource, TKey>> keySelector)
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			return (IOrderedQueryable<TSource>)source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					_thenOrByDescending.MakeGenericMethod(typeof(TSource), typeof(TKey)),
					new[] { source.Expression, Expression.Quote(keySelector) }));
		}

		#endregion

		#region GetContext

		internal static readonly MethodInfo SetMethodInfo8 = MemberHelper.MethodOf(() => GetContext((IQueryable<int>)null)).GetGenericMethodDefinition();

		/// <summary>
		/// Converts query to <see cref="ContextParser.Context"/> object, used by merge operation generator.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Query context object.</returns>
		internal static ContextParser.Context GetContext<TSource>(this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = ProcessSourceQueryable?.Invoke(source) ?? source;

			return currentSource.Provider.Execute<ContextParser.Context>(
				Expression.Call(
					null,
					SetMethodInfo8.MakeGenericMethod(typeof(TSource)),
					currentSource.Expression));
		}

		#endregion

		#region Stub helpers

		// Please do not move it if you do not understand why it's here.
		//
		internal static TOutput Where<TOutput,TSource,TInput>(this TInput source, Func<TSource,bool> predicate)
		{
			throw new InvalidOperationException();
		}

		#endregion

		#region SqlJoin

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> Join<TSource>(
			[NotNull]           this IQueryable<TSource>        source,
			[SqlQueryDependent] SqlJoinType                     joinType,
			[NotNull, InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			if (source    == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Join, source, joinType, predicate),
					new[]
					{
						source.Expression,
						Expression.Constant(joinType),
						Expression.Quote(predicate)
					}));
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="joinType">Type of join.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> Join<TOuter, TInner, TResult>(
			[NotNull]           this IQueryable<TOuter>                   outer,
			[NotNull]           IQueryable<TInner>                        inner,
			[SqlQueryDependent] SqlJoinType                               joinType,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			if (outer          == null) throw new ArgumentNullException(nameof(outer));
			if (inner          == null) throw new ArgumentNullException(nameof(inner));
			if (predicate      == null) throw new ArgumentNullException(nameof(predicate));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			return outer.Provider.CreateQuery<TResult>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(Join, outer, inner, joinType, predicate, resultSelector),
					new[]
					{
						outer.Expression,
						inner.Expression,
						Expression.Constant(joinType),
						Expression.Quote(predicate),
						Expression.Quote(resultSelector)
					}));
		}


		/// <summary>
		/// Defines inner join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> InnerJoin<TSource>(
			[NotNull] this IQueryable<TSource>        source,
			[NotNull, InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Inner, predicate);
		}

		/// <summary>
		/// Defines inner or outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> InnerJoin<TOuter, TInner, TResult>(
			[NotNull] this IQueryable<TOuter>                   outer,
			[NotNull] IQueryable<TInner>                        inner,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Inner, predicate, resultSelector);
		}

		/// <summary>
		/// Defines left outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> LeftJoin<TSource>(
			[NotNull] this IQueryable<TSource>        source,
			[NotNull, InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Left, predicate);
		}

		/// <summary>
		/// Defines left outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> LeftJoin<TOuter, TInner, TResult>(
			[NotNull] this IQueryable<TOuter>                   outer,
			[NotNull] IQueryable<TInner>                        inner,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Left, predicate, resultSelector);
		}

		/// <summary>
		/// Defines right outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> RightJoin<TSource>(
			[NotNull] this IQueryable<TSource>        source,
			[NotNull, InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Right, predicate);
		}

		/// <summary>
		/// Defines right outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> RightJoin<TOuter, TInner, TResult>(
			[NotNull] this IQueryable<TOuter>                   outer,
			[NotNull] IQueryable<TInner>                        inner,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Right, predicate, resultSelector);
		}

		/// <summary>
		/// Defines full outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TSource">Type of record for right join operand.</typeparam>
		/// <param name="source">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> FullJoin<TSource>(
			[NotNull] this IQueryable<TSource>        source,
			[NotNull, InstantHandle] Expression<Func<TSource, bool>> predicate)
		{
			return Join(source, SqlJoinType.Full, predicate);
		}

		/// <summary>
		/// Defines full outer join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="predicate">Join predicate.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> FullJoin<TOuter, TInner, TResult>(
			[NotNull] this IQueryable<TOuter>                   outer,
			[NotNull] IQueryable<TInner>                        inner,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, bool>>    predicate,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			return Join(outer, inner, SqlJoinType.Full, predicate, resultSelector);
		}

		/// <summary>
		/// Defines cross join between two sub-queries or tables.
		/// </summary>
		/// <typeparam name="TOuter">Type of record for left join operand.</typeparam>
		/// <typeparam name="TInner">Type of record for right join operand.</typeparam>
		/// <typeparam name="TResult">The type of the result elements.</typeparam>
		/// <param name="outer">Left join operand.</param>
		/// <param name="inner">Right join operand.</param>
		/// <param name="resultSelector">A function to create a result element from two matching elements.</param>
		/// <returns>Right operand.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TResult> CrossJoin<TOuter, TInner, TResult>(
			[NotNull] this IQueryable<TOuter>                   outer,
			[NotNull] IQueryable<TInner>                        inner,
			[NotNull, InstantHandle] Expression<Func<TOuter, TInner, TResult>> resultSelector)
		{
			if (outer          == null) throw new ArgumentNullException(nameof(outer));
			if (inner          == null) throw new ArgumentNullException(nameof(inner));
			if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

			return outer.Provider.CreateQuery<TResult>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(CrossJoin, outer, inner, resultSelector),
					new[]
					{
						outer.Expression,
						inner.Expression,
						Expression.Quote(resultSelector)
					}));
		}

		#endregion

		#region CTE

		internal static IQueryable<T> AsCte<T>(IQueryable<T> cteTable, IQueryable<T> cteBody, string tableName)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Specifies a temporary named result set, known as a common table expression (CTE).
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>Common table expression.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsCte<TSource>([NotNull] this IQueryable<TSource> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsCte, source),
					source.Expression));
		}

		/// <summary>
		/// Specifies a temporary named result set, known as a common table expression (CTE).
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="name">Common table expression name.</param>
		/// <returns>Common table expression.</returns>
		[Pure]
		[LinqTunnel]
		public static IQueryable<TSource> AsCte<TSource>(
			[NotNull]   this IQueryable<TSource> source,
			[CanBeNull] string                   name)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			return source.Provider.CreateQuery<TSource>(
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsCte, source, name),
					new[] {source.Expression, Expression.Constant(name ?? string.Empty)}));
		}

		#endregion

		#region Tests

		/// <summary>
		/// Generates test source code for specified query.
		/// This method could be usefull to debug queries and attach test code to linq2db issue reports.
		/// </summary>
		/// <param name="query">Query to test.</param>
		/// <param name="mangleNames">Should we use real names for used types, members and namespace or generate obfuscated names.</param>
		/// <returns>Test source code.</returns>
		public static string GenerateTestString(this IQueryable query, bool mangleNames = false)
		{
			return new ExpressionTestGenerator(mangleNames).GenerateSourceString(query.Expression);
		}

		#endregion

		#region Queryable Helpers

		/// <summary>
		/// Gets or sets callback for preprocessing query before execution.
		/// Useful for intercepting queries.
		/// </summary>
		public static Func<IQueryable, IQueryable> ProcessSourceQueryable { get; set; }

		public static IExtensionsAdapter ExtensionsAdapter { get; set; }

		#endregion
	}
}
