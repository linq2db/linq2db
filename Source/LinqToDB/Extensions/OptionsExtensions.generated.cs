// Generated.
//
using System;
using System.Data.Common;

#nullable enable

namespace LinqToDB
{
	using DataProvider.Access;
	using DataProvider.Oracle;
	using DataProvider.SqlServer;
	using Infrastructure;
	using Infrastructure.Internal;

	public static partial class OptionsExtensions
	{
		#region Access

		/// <summary>
		/// Configure connection to use Access default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataContextOptionsBuilder UseAccess(this DataContextOptionsBuilder optionsBuilder, string connectionString)
		{
			if (optionsBuilder   == null) throw new ArgumentNullException(nameof(optionsBuilder));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			optionsBuilder = optionsBuilder
				.UseConnectionString(connectionString)
				.UseProvider        (null)
				.UseDataProvider    (null);

			var extension = optionsBuilder.Options.FindExtension<AccessOptionsExtension>() ?? new AccessOptionsExtension();

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			return optionsBuilder;
		}

		/// <summary>
		/// Configure connection to use Access default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Access connection string.</param>
		/// <param name="optionsAction">An optional action to allow additional Access specific configuration.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataContextOptionsBuilder UseAccess(
			this DataContextOptionsBuilder          optionsBuilder,
			string                                  connectionString,
			Action<AccessDataContextOptionsBuilder> optionsAction)
		{
			var ob = UseAccess(optionsBuilder, connectionString);

			optionsAction(new (ob));

			return ob;
		}

		/// <summary>
		/// Configures the context to connect to a Access database.
		/// </summary>
		/// <param name="optionsBuilder"> The builder being used to configure the context. </param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		public static DataContextOptionsBuilder UseAccess(this DataContextOptionsBuilder optionsBuilder, DbConnection connection)
		{
			if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));
			if (connection     == null) throw new ArgumentNullException(nameof(connection));

			optionsBuilder = optionsBuilder
				.UseProvider    (null)
				.UseDataProvider(null)
				.UseConnection  (connection);

			var extension = GetOrCreateExtension(optionsBuilder);

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			ConfigureWarnings(optionsBuilder);

			return optionsBuilder;
		}

		/// <summary>
		/// Configures the context to connect to a Access database.
		/// </summary>
		/// <param name="optionsBuilder"> The builder being used to configure the context. </param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <param name="optionsAction">An optional action to allow additional SQL Server specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		public static DataContextOptionsBuilder UseAccess(
			this DataContextOptionsBuilder          optionsBuilder,
			DbConnection                            connection,
			Action<AccessDataContextOptionsBuilder> optionsAction)
		{
			var ob = UseAccess(optionsBuilder, connection);

			optionsAction?.Invoke(new (ob));

			return ob;
		}

		/// <summary>
		/// Configures the context to connect to a Access database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connectionString">The connection string of the database to connect to.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		public static DataContextOptionsBuilder<TContext> UseAccess<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			string                                   connectionString)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseAccess((DataContextOptionsBuilder)optionsBuilder, connectionString);
		}

		/// <summary>
		/// Configures the context to connect to a Access database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connectionString">The connection string of the database to connect to.</param>
		/// <param name="optionsAction">An optional action to allow additional Access specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		public static DataContextOptionsBuilder<TContext> UseAccess<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			string                                   connectionString,
			Action<AccessDataContextOptionsBuilder>  optionsAction)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseAccess((DataContextOptionsBuilder)optionsBuilder, connectionString, optionsAction);
		}

		/// <summary>
		/// Configures the context to connect to a Access database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		public static DataContextOptionsBuilder<TContext> UseAccess<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			DbConnection                             connection)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseAccess((DataContextOptionsBuilder)optionsBuilder, connection);
		}

		/// <summary>
		/// Configures the context to connect to a Access database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <param name="optionsAction">An optional action to allow additional Access specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		public static DataContextOptionsBuilder<TContext> UseAccess<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			DbConnection                             connection,
			Action<AccessDataContextOptionsBuilder>  optionsAction)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseAccess((DataContextOptionsBuilder)optionsBuilder, connection, optionsAction);
		}

		#endregion

		#region Oracle

		/// <summary>
		/// Configure connection to use Oracle default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(this DataContextOptionsBuilder optionsBuilder, string connectionString)
		{
			if (optionsBuilder   == null) throw new ArgumentNullException(nameof(optionsBuilder));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			optionsBuilder = optionsBuilder
				.UseConnectionString(connectionString)
				.UseProvider        (null)
				.UseDataProvider    (null);

			var extension = optionsBuilder.Options.FindExtension<OracleOptionsExtension>() ?? new OracleOptionsExtension();

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			return optionsBuilder;
		}

		/// <summary>
		/// Configure connection to use Oracle default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Oracle connection string.</param>
		/// <param name="optionsAction">An optional action to allow additional Oracle specific configuration.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(
			this DataContextOptionsBuilder          optionsBuilder,
			string                                  connectionString,
			Action<OracleDataContextOptionsBuilder> optionsAction)
		{
			var ob = UseOracle(optionsBuilder, connectionString);

			optionsAction(new (ob));

			return ob;
		}

		/// <summary>
		/// Configures the context to connect to a Oracle database.
		/// </summary>
		/// <param name="optionsBuilder"> The builder being used to configure the context. </param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(this DataContextOptionsBuilder optionsBuilder, DbConnection connection)
		{
			if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));
			if (connection     == null) throw new ArgumentNullException(nameof(connection));

			optionsBuilder = optionsBuilder
				.UseProvider    (null)
				.UseDataProvider(null)
				.UseConnection  (connection);

			var extension = GetOrCreateExtension(optionsBuilder);

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			ConfigureWarnings(optionsBuilder);

			return optionsBuilder;
		}

		/// <summary>
		/// Configures the context to connect to a Oracle database.
		/// </summary>
		/// <param name="optionsBuilder"> The builder being used to configure the context. </param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <param name="optionsAction">An optional action to allow additional SQL Server specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder UseOracle(
			this DataContextOptionsBuilder          optionsBuilder,
			DbConnection                            connection,
			Action<OracleDataContextOptionsBuilder> optionsAction)
		{
			var ob = UseOracle(optionsBuilder, connection);

			optionsAction?.Invoke(new (ob));

			return ob;
		}

		/// <summary>
		/// Configures the context to connect to a Oracle database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connectionString">The connection string of the database to connect to.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseOracle<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			string                                   connectionString)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseOracle((DataContextOptionsBuilder)optionsBuilder, connectionString);
		}

		/// <summary>
		/// Configures the context to connect to a Oracle database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connectionString">The connection string of the database to connect to.</param>
		/// <param name="optionsAction">An optional action to allow additional Oracle specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseOracle<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			string                                   connectionString,
			Action<OracleDataContextOptionsBuilder>  optionsAction)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseOracle((DataContextOptionsBuilder)optionsBuilder, connectionString, optionsAction);
		}

		/// <summary>
		/// Configures the context to connect to a Oracle database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseOracle<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			DbConnection                             connection)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseOracle((DataContextOptionsBuilder)optionsBuilder, connection);
		}

		/// <summary>
		/// Configures the context to connect to a Oracle database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <param name="optionsAction">An optional action to allow additional Oracle specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// By default LinqToDB tries to load managed version of Oracle provider.
		/// </para>
		/// <para>
		/// Oracle dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="OracleTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="OracleTools.DefaultVersion"/> (default: <see cref="OracleVersion.v12"/>) will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseOracle<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			DbConnection                             connection,
			Action<OracleDataContextOptionsBuilder>  optionsAction)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseOracle((DataContextOptionsBuilder)optionsBuilder, connection, optionsAction);
		}

		#endregion

		#region SqlServer

		/// <summary>
		/// Configure connection to use Microsoft SQL Server default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Microsoft SQL Server connection string.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder UseSqlServer(this DataContextOptionsBuilder optionsBuilder, string connectionString)
		{
			if (optionsBuilder   == null) throw new ArgumentNullException(nameof(optionsBuilder));
			if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

			optionsBuilder = optionsBuilder
				.UseConnectionString(connectionString)
				.UseProvider        (null)
				.UseDataProvider    (null);

			var extension = optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>() ?? new SqlServerOptionsExtension();

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			return optionsBuilder;
		}

		/// <summary>
		/// Configure connection to use Microsoft SQL Server default provider, dialect and connection string.
		/// </summary>
		/// <param name="optionsBuilder">Instance of <see cref="DataContextOptionsBuilder"/>.</param>
		/// <param name="connectionString">Microsoft SQL Server connection string.</param>
		/// <param name="optionsAction">An optional action to allow additional Microsoft SQL Server specific configuration.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder UseSqlServer(
			this DataContextOptionsBuilder          optionsBuilder,
			string                                  connectionString,
			Action<SqlServerDataContextOptionsBuilder> optionsAction)
		{
			var ob = UseSqlServer(optionsBuilder, connectionString);

			optionsAction(new (ob));

			return ob;
		}

		/// <summary>
		/// Configures the context to connect to a Microsoft SQL Server database.
		/// </summary>
		/// <param name="optionsBuilder"> The builder being used to configure the context. </param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder UseSqlServer(this DataContextOptionsBuilder optionsBuilder, DbConnection connection)
		{
			if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));
			if (connection     == null) throw new ArgumentNullException(nameof(connection));

			optionsBuilder = optionsBuilder
				.UseProvider    (null)
				.UseDataProvider(null)
				.UseConnection  (connection);

			var extension = GetOrCreateExtension(optionsBuilder);

			((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

			ConfigureWarnings(optionsBuilder);

			return optionsBuilder;
		}

		/// <summary>
		/// Configures the context to connect to a Microsoft SQL Server database.
		/// </summary>
		/// <param name="optionsBuilder"> The builder being used to configure the context. </param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <param name="optionsAction">An optional action to allow additional SQL Server specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder UseSqlServer(
			this DataContextOptionsBuilder          optionsBuilder,
			DbConnection                            connection,
			Action<SqlServerDataContextOptionsBuilder> optionsAction)
		{
			var ob = UseSqlServer(optionsBuilder, connection);

			optionsAction?.Invoke(new (ob));

			return ob;
		}

		/// <summary>
		/// Configures the context to connect to a Microsoft SQL Server database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connectionString">The connection string of the database to connect to.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseSqlServer<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			string                                   connectionString)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseSqlServer((DataContextOptionsBuilder)optionsBuilder, connectionString);
		}

		/// <summary>
		/// Configures the context to connect to a Microsoft SQL Server database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connectionString">The connection string of the database to connect to.</param>
		/// <param name="optionsAction">An optional action to allow additional Microsoft SQL Server specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseSqlServer<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			string                                   connectionString,
			Action<SqlServerDataContextOptionsBuilder>  optionsAction)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseSqlServer((DataContextOptionsBuilder)optionsBuilder, connectionString, optionsAction);
		}

		/// <summary>
		/// Configures the context to connect to a Microsoft SQL Server database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseSqlServer<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			DbConnection                             connection)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseSqlServer((DataContextOptionsBuilder)optionsBuilder, connection);
		}

		/// <summary>
		/// Configures the context to connect to a Microsoft SQL Server database.
		/// </summary>
		/// <typeparam name="TContext">The type of context to be configured.</typeparam>
		/// <param name="optionsBuilder">The builder being used to configure the context.</param>
		/// <param name="connection">An existing <see cref="DbConnection" /> to be used to connect to the database.</param>
		/// <param name="optionsAction">An optional action to allow additional Microsoft SQL Server specific configuration.</param>
		/// <returns>The options builder so that further configuration can be chained.</returns>
		/// <remarks>
		/// <para>
		/// Default provider configured using <see cref="SqlServerTools.Provider"/> option and set to <see cref="SqlServerProvider.SystemDataSqlClient"/> by default.
		/// </para>
		/// <para>
		/// SQL Server dialect will be chosen automatically:
		/// <list type="bullet">
		/// <item>if <see cref="SqlServerTools.AutoDetectProvider"/> (default: <c>true</c>) enabled, LinqToDB will query server for version</item>
		/// <item>otherwise <see cref="SqlServerVersion.v2008"/> will be used as default dialect.</item>
		/// </list>
		/// </para>
		/// For more fine-grained configuration see <see cref="UseSqlServer(DataContextOptionsBuilder, string, SqlServerProvider, SqlServerVersion)"/> overload.
		/// </remarks>
		public static DataContextOptionsBuilder<TContext> UseSqlServer<TContext>(
			this DataContextOptionsBuilder<TContext> optionsBuilder,
			DbConnection                             connection,
			Action<SqlServerDataContextOptionsBuilder>  optionsAction)
			where TContext : IDataContext
		{
			return (DataContextOptionsBuilder<TContext>)UseSqlServer((DataContextOptionsBuilder)optionsBuilder, connection, optionsAction);
		}

		#endregion

	}
}
