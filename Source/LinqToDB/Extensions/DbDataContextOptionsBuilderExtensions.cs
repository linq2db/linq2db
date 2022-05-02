using System;
using System.Data.Common;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace LinqToDB
{
	using Data;
	using DataProvider;
	using Infrastructure;
	using Mapping;

	public static class DbDataContextOptionsBuilderExtensions
	{
        public static DataContextOptionsBuilder UseConnectionString(this DataContextOptionsBuilder optionsBuilder, string providerName, string connectionString)
        {
	        return WithOption(optionsBuilder, e => e.WithProviderName(providerName).WithConnectionString(connectionString));
        }

        public static DataContextOptionsBuilder UseConnectionString(this DataContextOptionsBuilder optionsBuilder, IDataProvider dataProvider, string connectionString)
        {
	        return WithOption(optionsBuilder, e => e.WithDataProvider(dataProvider).WithConnectionString(connectionString));
        }

        public static DataContextOptionsBuilder UseConnectionString(this DataContextOptionsBuilder optionsBuilder, string connectionString)
        {
	        return WithOption(optionsBuilder, e => e.WithConnectionString(connectionString));
        }

        public static DataContextOptionsBuilder UseConfigurationString(this DataContextOptionsBuilder optionsBuilder, string? configurationString)
        {
	        return WithOption(optionsBuilder, e => e.WithConfigurationString(configurationString));
        }

        public static DataContextOptionsBuilder UseConfigurationString(this DataContextOptionsBuilder optionsBuilder, string? configurationString, MappingSchema mappingSchema)
        {
	        return WithOption(optionsBuilder, e => e.WithConfigurationString(configurationString).WithMappingSchema(mappingSchema));
        }

        public static DataContextOptionsBuilder UseConnection(this DataContextOptionsBuilder optionsBuilder, DbConnection connection)
        {
	        return WithOption(optionsBuilder, e => e.WithConnection(connection));
        }

        public static DataContextOptionsBuilder UseConnection(this DataContextOptionsBuilder optionsBuilder, IDataProvider dataProvider, DbConnection connection)
        {
	        return WithOption(optionsBuilder, e => e.WithDataProvider(dataProvider).WithConnection(connection));
        }

        public static DataContextOptionsBuilder UseConnection(this DataContextOptionsBuilder optionsBuilder, IDataProvider dataProvider, DbConnection connection, bool disposeConnection)
        {
	        return WithOption(optionsBuilder, e => e.WithDataProvider(dataProvider).WithConnection(connection).WithDisposeConnection(disposeConnection));
        }

        public static DataContextOptionsBuilder UseProvider(this DataContextOptionsBuilder optionsBuilder, string providerName)
        {
	        return WithOption(optionsBuilder, e => e.WithProviderName(providerName));
        }

        public static DataContextOptionsBuilder UseDataProvider(this DataContextOptionsBuilder optionsBuilder, IDataProvider dataProvider)
        {
	        return WithOption(optionsBuilder, e => e.WithDataProvider(dataProvider));
        }

        public static DataContextOptionsBuilder UseMappingSchema(this DataContextOptionsBuilder optionsBuilder, MappingSchema mappingSchema)
        {
	        return WithOption(optionsBuilder, e => e.WithMappingSchema(mappingSchema));
        }

        public static DataContextOptionsBuilder UseConnectionFactory(this DataContextOptionsBuilder optionsBuilder, Func<DbConnection> connectionFactory)
        {
	        return WithOption(optionsBuilder, e => e.WithConnectionFactory(connectionFactory));
        }

        public static DataContextOptionsBuilder UseConnectionFactory(this DataContextOptionsBuilder optionsBuilder, IDataProvider dataProvider, Func<DbConnection> connectionFactory)
        {
	        return WithOption(optionsBuilder, e => e.WithDataProvider(dataProvider).WithConnectionFactory(connectionFactory));
        }

        public static DataContextOptionsBuilder UseTransaction(this DataContextOptionsBuilder optionsBuilder, IDataProvider dataProvider, DbTransaction transaction)
        {
	        return WithOption(optionsBuilder, e => e.WithDataProvider(dataProvider).WithTransaction(transaction));
        }

        /// <summary>
        /// Configure the database to use specified trace level.
        /// </summary>
        /// <returns>The builder instance so calls can be chained.</returns>
        public static DataContextOptionsBuilder WithTraceLevel(this DataContextOptionsBuilder optionsBuilder, TraceLevel traceLevel)
        {
	        return WithOption(optionsBuilder, e => e.WithTraceLevel(traceLevel));
        }

        /// <summary>
        /// Configure the database to use the specified callback for logging or tracing.
        /// </summary>
        /// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
        /// <returns>The builder instance so calls can be chained.</returns>
        public static DataContextOptionsBuilder WithTracing(this DataContextOptionsBuilder optionsBuilder, Action<TraceInfo> onTrace)
        {
	        return WithOption(optionsBuilder, e => e.WithTracing(onTrace));
        }

        /// <summary>
        /// Configure the database to use the specified trace level and callback for logging or tracing.
        /// </summary>
        /// <param name="traceLevel">Trace level to use.</param>
        /// <param name="onTrace">Callback, may not be called depending on the trace level.</param>
        /// <returns>The builder instance so calls can be chained.</returns>
        public static DataContextOptionsBuilder WithTracing(this DataContextOptionsBuilder optionsBuilder, TraceLevel traceLevel, Action<TraceInfo> onTrace)
        {
	        return WithOption(optionsBuilder, e => e.WithTracing(onTrace).WithTraceLevel(traceLevel));
        }

        /// <summary>
        /// Configure the database to use the specified a string trace callback.
        /// </summary>
        /// <param name="write">Callback, may not be called depending on the trace level.</param>
        /// <returns>The builder instance so calls can be chained.</returns>
        public static DataContextOptionsBuilder WriteTraceWith(this DataContextOptionsBuilder optionsBuilder, Action<string?, string?, TraceLevel> write)
        {
	        return WithOption(optionsBuilder, e => e.WriteTraceWith(write));
        }

        private static DataContextOptionsBuilder WithOption(DataContextOptionsBuilder optionsBuilder,
	        Func<DbDataContextOptionsExtension, DbDataContextOptionsExtension>        action)
        {
	        var dbOptionsExtension
		        = optionsBuilder.Options.FindExtension<DbDataContextOptionsExtension>()
		          ?? new DbDataContextOptionsExtension();

	        dbOptionsExtension = action(dbOptionsExtension);

	        ((IDataContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(dbOptionsExtension);

			return optionsBuilder;
        }
		
	}
}
