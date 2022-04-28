using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.SqlServer.Infrastructure.Internal;

namespace LinqToDB.Extensions
{
    /// <summary>
    ///     SQL Server specific extension methods for <see cref="DbContextOptionsBuilder" />.
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
        public static DbContextOptionsBuilder UseSqlServer(
            this DbContextOptionsBuilder              optionsBuilder,
            string                                    connectionString,
            Action<SqlServerDbContextOptionsBuilder>? sqlServerOptionsAction = null)
        {
	        if (optionsBuilder == null)
	        {
		        throw new ArgumentNullException(nameof(optionsBuilder));
	        }

	        if (connectionString == null)
	        {
		        throw new ArgumentNullException(nameof(connectionString));
	        }

            var extension = (SqlServerOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            ConfigureWarnings(optionsBuilder);

            sqlServerOptionsAction?.Invoke(new SqlServerDbContextOptionsBuilder(optionsBuilder));

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
        public static DbContextOptionsBuilder UseSqlServer(
            this DbContextOptionsBuilder              optionsBuilder,
            DbConnection                              connection,
            Action<SqlServerDbContextOptionsBuilder>? sqlServerOptionsAction = null)
        {
	        if (optionsBuilder == null)
	        {
		        throw new ArgumentNullException(nameof(optionsBuilder));
	        }

	        if (connection == null)
	        {
		        throw new ArgumentNullException(nameof(connection));
	        }

            var extension = (SqlServerOptionsExtension)GetOrCreateExtension(optionsBuilder).WithConnection(connection);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            ConfigureWarnings(optionsBuilder);

            sqlServerOptionsAction?.Invoke(new SqlServerDbContextOptionsBuilder(optionsBuilder));

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
        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(
            this DbContextOptionsBuilder<TContext>    optionsBuilder,
                  string                              connectionString,
            Action<SqlServerDbContextOptionsBuilder>? sqlServerOptionsAction = null)
            where TContext : IDataContext
            => (DbContextOptionsBuilder<TContext>)UseSqlServer(
                (DbContextOptionsBuilder)optionsBuilder, connectionString, sqlServerOptionsAction);

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
        public static DbContextOptionsBuilder<TContext> UseSqlServer<TContext>(
            this DbContextOptionsBuilder<TContext>    optionsBuilder,
            DbConnection                              connection,
            Action<SqlServerDbContextOptionsBuilder>? sqlServerOptionsAction = null)
            where TContext : IDataContext
            => (DbContextOptionsBuilder<TContext>)UseSqlServer(
                (DbContextOptionsBuilder)optionsBuilder, connection, sqlServerOptionsAction);

        private static SqlServerOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.Options.FindExtension<SqlServerOptionsExtension>()
                ?? new SqlServerOptionsExtension();

        private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
        {
            var coreOptionsExtension
                = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()
                ?? new CoreOptionsExtension();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(coreOptionsExtension);
        }
    }
}
