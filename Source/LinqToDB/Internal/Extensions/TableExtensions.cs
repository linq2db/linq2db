using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.Extensions
{
	static class TableExtensions
	{
		public static IDataProvider GetDataProvider(this IDataContext context)
		{
			return context switch
			{
				DataConnection dataConnection => dataConnection.DataProvider,
				DataContext dataContext       => dataContext.DataProvider,

				_ => throw new ArgumentException($"Data context must be of {nameof(DataConnection)} or {nameof(DataContext)} type.", nameof(context)),
			};
		}

		public static IDataProvider GetDataProvider<T>(this ITable<T> table)
			where T : notnull
		{
			if (table.DataContext is DataConnection dataConnection)
				return dataConnection.DataProvider;
			if (table.DataContext is DataContext dataContext)
				return dataContext.DataProvider;

			throw new ArgumentException($"Data context must be of {nameof(DataConnection)} or {nameof(DataContext)} type.", nameof(table));
		}

		public static DataConnection GetDataConnection<T>(this ITable<T> table)
			where T : notnull
		{
			if (table.DataContext is DataConnection dataConnection)
				return dataConnection;
			if (table.DataContext is DataContext dataContext)
				return dataContext.GetDataConnection();

			throw new ArgumentException($"Data context must be of {nameof(DataConnection)} or {nameof(DataContext)} type.", nameof(table));
		}

		public static bool TryGetDataConnection<T>(this ITable<T> table, [NotNullWhen(true)] out DataConnection? dataConnection)
			where T : notnull
		{
			if (table.DataContext is DataConnection dc)
				dataConnection = dc;
			else if (table.DataContext is DataContext dataContext)
				dataConnection = dataContext.GetDataConnection();
			else
				dataConnection = null;

			return dataConnection != null;
		}

		/// <summary>
		/// Support for table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Creates new table in database for mapping class <typeparamref name="T"/>.
		/// Information about table name, columns names and types is taken from mapping class.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Create table options.</param>
		/// <returns>Created table as queryable source.</returns>
		internal static ITable<T> CreateTable<T>(
			this IDataContext  dataContext,
			EntityDescriptor?  tableDescriptor,
			CreateTableOptions createOptions)
			where T: notnull
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.Query(
				dataContext,
				tableDescriptor: tableDescriptor,
				createOptions  : createOptions);
		}

		/// <summary>
		/// Support for table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Asynchronously creates new table in database for mapping class <typeparamref name="T"/>.
		/// Information about table name, columns names and types is taken from mapping class.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="createOptions">Create table options.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Created table as queryable source.</returns>
		internal static Task<ITable<T>> CreateTableAsync<T>(
			this IDataContext    dataContext,
			TempTableDescriptor? tableDescriptor,
			CreateTableOptions   createOptions,
			CancellationToken    token           = default)
			where T : notnull
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.QueryAsync(
				dataContext,
				tableDescriptor: tableDescriptor,
				createOptions  : createOptions,
				token          : token);
		}
	}
}
