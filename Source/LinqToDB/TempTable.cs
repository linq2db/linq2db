using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Data;
	using Expressions;
	using Extensions;
	using Linq;
	using Mapping;

	/// <summary>
	/// Temporary table. Temporary table is a table, created when you create instance of this class and deleted when
	/// you dispose it. It uses regular tables even if underlying database supports temporary tables concept.
	/// </summary>
	/// <typeparam name="T">Table record mapping class.</typeparam>
	[PublicAPI]
	public class TempTable<T> : ITable<T>, IDisposable
	{
		readonly ITable<T> _table;

		/// <summary>
		/// Gets total number of records, inserted into table using BulkCopy.
		/// </summary>
		public long TotalCopied;

		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		public TempTable([JetBrains.Annotations.NotNull] IDataContext db,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null)
		{
			if (db == null) throw new ArgumentNullException(nameof(db));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		public TempTable([JetBrains.Annotations.NotNull] IDataContext db,
			[JetBrains.Annotations.NotNull] IEnumerable<T> items,
			BulkCopyOptions options = null,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName);
			Copy(items, options);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		public TempTable([JetBrains.Annotations.NotNull] IDataContext db,
			string tableName,
			[JetBrains.Annotations.NotNull] IEnumerable<T> items,
			BulkCopyOptions options = null,
			string databaseName = null,
			string schemaName   = null)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName);
			Copy(items, options);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		public TempTable([JetBrains.Annotations.NotNull] IDataContext db,
			[JetBrains.Annotations.NotNull] IQueryable<T> items,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null,
			Action<ITable<T>> action = null)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName);
			action?.Invoke(_table);
			Insert(items);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		public TempTable([JetBrains.Annotations.NotNull] IDataContext db,
			string tableName,
			[JetBrains.Annotations.NotNull] IQueryable<T> items,
			string databaseName = null,
			string schemaName   = null,
			Action<ITable<T>> action = null)
		{
			if (db    == null) throw new ArgumentNullException(nameof(db));
			if (items == null) throw new ArgumentNullException(nameof(items));

			_table = db.CreateTable<T>(tableName, databaseName, schemaName);
			action?.Invoke(_table);
			Insert(items);
		}

		/// <summary>
		/// Insert new records into table using BulkCopy.
		/// </summary>
		/// <param name="items">Records to insert into table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public long Copy(IEnumerable<T> items, BulkCopyOptions options = null)
		{
			var count = options != null ?
				_table.BulkCopy(options, items) :
				_table.BulkCopy(items);

			TotalCopied += count.RowsCopied;

			return count.RowsCopied;
		}

		static ConcurrentDictionary<Type,Expression<Func<T,T>>> _setterDic = new ConcurrentDictionary<Type,Expression<Func<T,T>>>();

		/// <summary>
		/// Insert data into table using records, returned by provided query.
		/// </summary>
		/// <param name="items">Query with records to insert into temporary table.</param>
		/// <returns>Number of records, inserted into table.</returns>
		public long Insert(IQueryable<T> items)
		{
			var type = typeof(T);
			var ed   = _table.DataContext.MappingSchema.GetEntityDescriptor(type);
			var p    = Expression.Parameter(type, "t");

			var l = _setterDic.GetOrAdd(type, t =>
			{
				if (t.IsAnonymous())
				{
					var nctor   = (NewExpression)items.Expression.Find(e => e.NodeType == ExpressionType.New && e.Type == t);
					var members = nctor.Members
						.Select(m => m is MethodInfo info ? info.GetPropertyInfo() : m)
						.ToList();

					return Expression.Lambda<Func<T,T>>(
						Expression.New(
							nctor.Constructor,
							members.Select(m => Expression.PropertyOrField(p, m.Name)),
							members),
						p);
				}

				return Expression.Lambda<Func<T,T>>(
					Expression.MemberInit(
						Expression.New(t),
						ed.Columns.Select(c => Expression.Bind(c.MemberInfo, Expression.MakeMemberAccess(p, c.MemberInfo)))),
					p);
			});

			var count = items.Insert(_table, l);

			TotalCopied += count;

			return count;
		}

		#region ITable<T> implementation

		string ITable<T>.DatabaseName => _table.DatabaseName;
		string ITable<T>.SchemaName   => _table.SchemaName;
		string ITable<T>.TableName    => _table.TableName;

		string ITable<T>.GetTableName()
		{
			return _table.GetTableName();
		}

		#endregion

		#region IQueryProvider

		IQueryable IQueryProvider.CreateQuery(Expression expression)
		{
			return _table.CreateQuery(expression);
		}

		IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
		{
			return _table.CreateQuery<TElement>(expression);
		}

		object IQueryProvider.Execute(Expression expression)
		{
			return _table.Execute(expression);
		}

		TResult IQueryProvider.Execute<TResult>(Expression expression)
		{
			return _table.Execute<TResult>(expression);
		}

		#endregion

		#region IQueryProviderAsync

		Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken token)
		{
			return _table.ExecuteAsync<TResult>(expression, token);
		}

		#endregion

		#region IExpressionQuery<T>

		Expression IExpressionQuery<T>.Expression
		{
			get => _table.Expression;
			set => _table.Expression = value;
		}

		#endregion

		#region IExpressionQuery

		/// <summary>
		/// Gets data connection, associated with current table.
		/// </summary>
		public IDataContext DataContext => _table.DataContext;

		string       IExpressionQuery.SqlText    => _table.SqlText;
		Expression   IExpressionQuery.Expression => ((IExpressionQuery)_table).Expression;

		#endregion

		#region IQueryable

		Expression IQueryable.Expression => ((IQueryable)_table).Expression;

		Type           IQueryable.ElementType => _table.ElementType;
		IQueryProvider IQueryable.Provider    => _table.Provider;

		#endregion

		#region IEnumerable<T>

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _table.GetEnumerator();
		}

		#endregion

		#region IEnumerable

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_table).GetEnumerator();
		}

		#endregion

		void IDisposable.Dispose()
		{
			_table.DropTable();
		}
	}

	public static partial class DataExtensions
	{
		/// <summary>
		/// Creates new temporary table.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null)
		{
			return new TempTable<T>(db, tableName, databaseName, schemaName);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			[JetBrains.Annotations.NotNull] IEnumerable<T> items,
			BulkCopyOptions options = null,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null)
		{
			return new TempTable<T>(db, items, options, tableName, databaseName, schemaName);
		}

		/// <summary>
		/// Creates new temporary table and populate it using BulkCopy.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Initial records to insert into created table.</param>
		/// <param name="options">Optional BulkCopy options.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			[JetBrains.Annotations.NotNull] string tableName,
			[JetBrains.Annotations.NotNull] IEnumerable<T> items,
			BulkCopyOptions options = null,
			string databaseName = null,
			string schemaName   = null)
		{
			return new TempTable<T>(db, tableName, items, options, databaseName, schemaName);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			[JetBrains.Annotations.NotNull] IQueryable<T> items,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null,
			Action<ITable<T>> action = null)
		{
			return new TempTable<T>(db, items, tableName, databaseName, schemaName, action);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			[JetBrains.Annotations.NotNull] IQueryable<T> items,
			[JetBrains.Annotations.NotNull] Action<EntityMappingBuilder<T>> setTable,
			string tableName    = null,
			string databaseName = null,
			string schemaName   = null,
			Action<ITable<T>> action = null)
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			setTable(db.MappingSchema.GetFluentMappingBuilder().Entity<T>());

			return new TempTable<T>(db, items, tableName, databaseName, schemaName, action);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			[JetBrains.Annotations.NotNull] string tableName,
			[JetBrains.Annotations.NotNull] IQueryable<T> items,
			string databaseName = null,
			string schemaName   = null,
			Action<ITable<T>> action = null)
		{
			return new TempTable<T>(db, tableName, items, databaseName, schemaName, action);
		}

		/// <summary>
		/// Creates new temporary table and populate it using data from provided query. Table mapping could be changed
		/// using fluent mapper.
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="db">Database connection instance.</param>
		/// <param name="tableName">Optional name of temporary table. If not specified, value from mapping will be used.</param>
		/// <param name="items">Query to get records to populate created table with initial data.</param>
		/// <param name="setTable">Action to modify <typeparamref name="T"/> entity's mapping using fluent mapping.</param>
		/// <param name="databaseName">Optional name of table's database. If not specified, value from mapping will be used.</param>
		/// <param name="schemaName">Optional name of table shema/owner. If not specified, value from mapping will be used.</param>
		/// <param name="action">Optional action that will be executed after table creation but before it populated with data from <paramref name="items"/>.</param>
		/// <returns>Returns temporary table instance.</returns>
		public static TempTable<T> CreateTempTable<T>(
			[JetBrains.Annotations.NotNull] this IDataContext db,
			[JetBrains.Annotations.NotNull] string tableName,
			[JetBrains.Annotations.NotNull] IQueryable<T> items,
			[JetBrains.Annotations.NotNull] Action<EntityMappingBuilder<T>> setTable,
			string databaseName = null,
			string schemaName   = null,
			Action<ITable<T>> action = null)
		{
			if (setTable == null) throw new ArgumentNullException(nameof(setTable));

			setTable(db.MappingSchema.GetFluentMappingBuilder().Entity<T>());

			return new TempTable<T>(db, tableName, items, databaseName, schemaName, action);
		}
	}
}
