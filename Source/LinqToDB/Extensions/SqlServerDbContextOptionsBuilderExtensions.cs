using System;
using System.Data.Common;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using Infrastructure;
	using Infrastructure.Internal;

    /// <summary>
    ///     SQL Server specific extension methods for <see cref="DataContextOptionsBuilder" />.
    /// </summary>
    public static class SqlServerDbContextOptionsExtensions
    {
        /// <summary>
        ///     Configures the context to connect to a Microsoft SQL Server database.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="connectionString"> The connection string of the database to connect to. </param>
        /// <param name="sqlServerOptionsAction">An optional action to allow additional SQL Server specific configuration.</param>
        /// <returns> The options builder so that further configuration can be chained. </returns>
        public static DataContextOptionsBuilder UseSqlServer(
            this DataContextOptionsBuilder            optionsBuilder,
            string                                    connectionString,
            Action<SqlServerDataContextOptionsBuilder>? sqlServerOptionsAction = null)
        {
	        if (optionsBuilder == null)
	        {
		        throw new ArgumentNullException(nameof(optionsBuilder));
	        }

	        if (connectionString == null)
	        {
		        throw new ArgumentNullException(nameof(connectionString));
	        }

	        optionsBuilder = optionsBuilder.UseConnectionString(connectionString);
            var extension = GetOrCreateExtension(optionsBuilder);
            ((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            ConfigureWarnings(optionsBuilder);

            sqlServerOptionsAction?.Invoke(new SqlServerDataContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        /// <summary>
        ///     Configures the context to connect to a Microsoft SQL Server database.
        /// </summary>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="connection">
        ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
        ///     in the open state then EF will not open or close the connection. If the connection is in the closed
        ///     state then EF will open and close the connection as needed.
        /// </param>
        /// <param name="sqlServerOptionsAction">An optional action to allow additional SQL Server specific configuration.</param>
        /// <returns> The options builder so that further configuration can be chained. </returns>
        public static DataContextOptionsBuilder UseSqlServer(
            this DataContextOptionsBuilder              optionsBuilder,
            DbConnection                              connection,
            Action<SqlServerDataContextOptionsBuilder>? sqlServerOptionsAction = null)
        {
	        if (optionsBuilder == null)
	        {
		        throw new ArgumentNullException(nameof(optionsBuilder));
	        }

	        if (connection == null)
	        {
		        throw new ArgumentNullException(nameof(connection));
	        }

	        optionsBuilder = optionsBuilder.UseConnection(connection);

            var extension = GetOrCreateExtension(optionsBuilder);
            ((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            ConfigureWarnings(optionsBuilder);

            sqlServerOptionsAction?.Invoke(new SqlServerDataContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        /// <summary>
        ///     Configures the context to connect to a Microsoft SQL Server database.
        /// </summary>
        /// <typeparam name="TContext"> The type of context to be configured. </typeparam>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="connectionString"> The connection string of the database to connect to. </param>
        /// <param name="sqlServerOptionsAction">An optional action to allow additional SQL Server specific configuration.</param>
        /// <returns> The options builder so that further configuration can be chained. </returns>
        public static DataContextOptionsBuilder<TContext> UseSqlServer<TContext>(
            this DataContextOptionsBuilder<TContext>    optionsBuilder,
                  string                              connectionString,
            Action<SqlServerDataContextOptionsBuilder>? sqlServerOptionsAction = null)
            where TContext : IDataContext
            => (DataContextOptionsBuilder<TContext>)UseSqlServer(
                (DataContextOptionsBuilder)optionsBuilder, connectionString, sqlServerOptionsAction);

        /// <summary>
        ///     Configures the context to connect to a Microsoft SQL Server database.
        /// </summary>
        /// <typeparam name="TContext"> The type of context to be configured. </typeparam>
        /// <param name="optionsBuilder"> The builder being used to configure the context. </param>
        /// <param name="connection">
        ///     An existing <see cref="DbConnection" /> to be used to connect to the database. If the connection is
        ///     in the open state then EF will not open or close the connection. If the connection is in the closed
        ///     state then EF will open and close the connection as needed.
        /// </param>
        /// <param name="sqlServerOptionsAction">An optional action to allow additional SQL Server specific configuration.</param>
        /// <returns> The options builder so that further configuration can be chained. </returns>
        public static DataContextOptionsBuilder<TContext> UseSqlServer<TContext>(
            this DataContextOptionsBuilder<TContext>    optionsBuilder,
            DbConnection                              connection,
            Action<SqlServerDataContextOptionsBuilder>? sqlServerOptionsAction = null)
            where TContext : IDataContext
        {
	        return (DataContextOptionsBuilder<TContext>)UseSqlServer(
		        (DataContextOptionsBuilder)optionsBuilder, connection, sqlServerOptionsAction);
        }

        private static SqlServerOptionsExtension GetOrCreateExtension(DataContextOptionsBuilder optionsBuilder)
        {
	        return optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>()
	               ?? new SqlServerOptionsExtension();
        }

        private static void ConfigureWarnings(DataContextOptionsBuilder optionsBuilder)
        {
            var coreOptionsExtension
                = optionsBuilder.Options.FindExtension<CoreDataContextOptionsExtension>()
                ?? new CoreDataContextOptionsExtension();

            ((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
        }
    }
}
