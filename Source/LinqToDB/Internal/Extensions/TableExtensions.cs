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
	public static class TableExtensions
	{
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
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="statementHeader">Optional replacement for <c>"CREATE TABLE table_name"</c> header. Header is a template with <c>{0}</c> parameter for table name.</param>
		/// <param name="statementFooter">Optional SQL, appended to generated create table statement.</param>
		/// <param name="defaultNullable">Defines how columns nullability flag should be generated:
		/// <para> - <see cref="DefaultNullable.Null"/> - generate only <c>NOT NULL</c> for non-nullable fields. Missing nullability information treated as <c>NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.NotNull"/> - generate only <c>NULL</c> for nullable fields. Missing nullability information treated as <c>NOT NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.None"/> - explicitly generate <c>NULL</c> and <c>NOT NULL</c> for all columns.</para>
		/// Default value: <see cref="DefaultNullable.None"/>.
		/// </param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <returns>Created table as queryable source.</returns>
		internal static ITable<T> CreateTable<T>(
			this IDataContext dataContext,
			EntityDescriptor? tableDescriptor,
			string?           tableName       = default,
			string?           databaseName    = default,
			string?           schemaName      = default,
			string?           statementHeader = default,
			string?           statementFooter = default,
			DefaultNullable   defaultNullable = DefaultNullable.None,
			string?           serverName      = default,
			TableOptions      tableOptions    = default)
			where T: notnull
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.Query(
				dataContext,
				tableDescriptor: tableDescriptor,
				tableName      : tableName,
				serverName     : serverName,
				databaseName   : databaseName,
				schemaName     : schemaName,
				statementHeader: statementHeader,
				statementFooter: statementFooter,
				defaultNullable: defaultNullable,
				tableOptions   : tableOptions);
		}

		/// <summary>
		/// Support for table creation using custom entity descriptor <paramref name="tableDescriptor"/>.
		/// Asynchronously creates new table in database for mapping class <typeparamref name="T"/>.
		/// Information about table name, columns names and types is taken from mapping class.
		/// </summary>
		/// <typeparam name="T">Mapping class.</typeparam>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="tableDescriptor">Temporary table entity descriptor.</param>
		/// <param name="tableName">Optional table name to override default table name, extracted from <typeparamref name="T"/> mapping.</param>
		/// <param name="databaseName">Optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="schemaName">Optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="statementHeader">Optional replacement for <c>"CREATE TABLE table_name"</c> header. Header is a template with <c>{0}</c> parameter for table name.</param>
		/// <param name="statementFooter">Optional SQL, appended to generated create table statement.</param>
		/// <param name="defaultNullable">Defines how columns nullability flag should be generated:
		/// <para> - <see cref="DefaultNullable.Null"/> - generate only <c>NOT NULL</c> for non-nullable fields. Missing nullability information treated as <c>NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.NotNull"/> - generate only <c>NULL</c> for nullable fields. Missing nullability information treated as <c>NOT NULL</c> by database.</para>
		/// <para> - <see cref="DefaultNullable.None"/> - explicitly generate <c>NULL</c> and <c>NOT NULL</c> for all columns.</para>
		/// Default value: <see cref="DefaultNullable.None"/>.
		/// </param>
		/// <param name="serverName">Optional linked server name. See <see cref="LinqExtensions.ServerName{T}(ITable{T}, string)"/> method for support information per provider.</param>
		/// <param name="tableOptions">Table options. See <see cref="TableOptions"/> enum for support information per provider.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Created table as queryable source.</returns>
		internal static Task<ITable<T>> CreateTableAsync<T>(
			this IDataContext    dataContext,
			TempTableDescriptor? tableDescriptor,
			string?              tableName       = default,
			string?              databaseName    = default,
			string?              schemaName      = default,
			string?              statementHeader = default,
			string?              statementFooter = default,
			DefaultNullable      defaultNullable = DefaultNullable.None,
			string?              serverName      = default,
			TableOptions         tableOptions    = default,
			CancellationToken    token           = default)
			where T : notnull
		{
			if (dataContext == null) throw new ArgumentNullException(nameof(dataContext));
			return QueryRunner.CreateTable<T>.QueryAsync(
				dataContext,
				tableDescriptor: tableDescriptor,
				tableName      : tableName,
				serverName     : serverName,
				databaseName   : databaseName,
				schemaName     : schemaName,
				statementHeader: statementHeader,
				statementFooter: statementFooter,
				defaultNullable: defaultNullable,
				tableOptions   : tableOptions,
				token          : token);
		}
	}
}
