using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.EntityFrameworkCore.Internal;
using LinqToDB.Expressions;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;
using LinqToDB.Metadata;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;

namespace LinqToDB.EntityFrameworkCore
{
	/// <summary>
	/// EF Core <see cref="DbContext"/> extensions to call LINQ To DB functionality.
	/// </summary>
	[PublicAPI]
	public static partial class LinqToDBForEFTools
	{
		static readonly Lazy<bool> _initialized = new(InitializeInternal);

		/// <summary>
		/// Initializes integration of LINQ To DB with EF Core.
		/// </summary>
		public static void Initialize()
		{
			var _ = _initialized.Value;
		}

		static bool InitializeInternal()
		{
			var prev = LinqExtensions.ProcessSourceQueryable;

			InitializeMapping();

			var instantiator = MemberHelper.MethodOf(() => Internals.CreateExpressionQueryInstance<int>(null!, null!))
				.GetGenericMethodDefinition();

			LinqExtensions.ProcessSourceQueryable = queryable =>
			{
				// our Provider - nothing to do
				if (queryable.Provider is IQueryProviderAsync)
					return queryable;

				var context = Implementation.GetCurrentContext(queryable)
					?? throw new LinqToDBForEFToolsException("Can not evaluate current context from query");

				var dc = CreateLinqToDBContext(context);
				var newExpression = queryable.Expression;

				var result = instantiator.MakeGenericMethod(queryable.ElementType)
					.InvokeExt<IQueryable>(null, [dc, newExpression]);

				if (prev != null)
					result = prev(result);

				return result;
			};

			LinqExtensions.ExtensionsAdapter = new LinqToDBExtensionsAdapter();

			return true;
		}

		static ILinqToDBForEFTools _implementation;

		/// <summary>
		/// Gets or sets EF Core to LINQ To DB integration bridge implementation.
		/// </summary>
		public static ILinqToDBForEFTools Implementation
		{
			get => _implementation;
			[MemberNotNull(nameof(_implementation), nameof(_defaultMetadataReader))]
			set
			{
				ArgumentNullException.ThrowIfNull(value);
				_implementation = value;
				_metadataReaders.Clear();
				_defaultMetadataReader = new Lazy<IMetadataReader?>(() => Implementation.CreateMetadataReader(null, null));
			}
		}

		static readonly ConcurrentDictionary<IModel, IMetadataReader?> _metadataReaders = new();

		static Lazy<IMetadataReader?> _defaultMetadataReader;

		/// <summary>
		/// Clears internal caches
		/// </summary>
		public static void ClearCaches()
		{
			_metadataReaders.Clear();
			Implementation.ClearCaches();
			Query.ClearCaches();
		}

		static LinqToDBForEFTools()
		{
			Implementation = new LinqToDBForEFToolsImplDefault();
			Initialize();
		}

		/// <summary>
		/// Creates or return existing metadata provider for provided EF Core data model. If model is null, empty metadata
		/// provider will be returned.
		/// </summary>
		/// <param name="model">EF Core data model instance. Could be <c>null</c>.</param>
		/// <param name="accessor">EF Core service provider.</param>
		/// <returns>LINQ To DB metadata provider.</returns>
		public static IMetadataReader? GetMetadataReader(
			IModel? model,
			IInfrastructure<IServiceProvider>? accessor)
		{
			if (model == null)
				return _defaultMetadataReader.Value;

			return _metadataReaders.GetOrAdd(model, m => Implementation.CreateMetadataReader(model, accessor));
		}

		/// <summary>
		/// Returns EF Core <see cref="DbContextOptions"/> for specific <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="context">EF Core <see cref="DbContext"/> instance.</param>
		/// <returns><see cref="DbContextOptions"/> instance.</returns>
		public static IDbContextOptions? GetContextOptions(DbContext context)
		{
			return Implementation.GetContextOptions(context);
		}

		/// <summary>
		/// Returns EF Core database provider information for specific <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="context">EF Core <see cref="DbContext"/> instance.</param>
		/// <returns>EF Core provider information.</returns>
		public static EFProviderInfo GetEFProviderInfo(DbContext context)
		{
			var info = new EFProviderInfo
			{
				Connection = context.Database.GetDbConnection(),
				Transaction = context.Database.CurrentTransaction?.GetDbTransaction(),
				Context = context,
				Options = GetContextOptions(context)
			};

			return info;
		}

		/// <summary>
		/// Returns EF Core database provider information for specific <see cref="DbConnection"/> instance.
		/// </summary>
		/// <param name="connection">EF Core <see cref="DbConnection"/> instance.</param>
		/// <returns>EF Core provider information.</returns>
		public static EFProviderInfo GetEFProviderInfo(DbConnection connection)
		{
			var info = new EFProviderInfo
			{
				Connection = connection,
				Context = null,
				Options = null
			};

			return info;
		}

		/// <summary>
		/// Returns EF Core database provider information for specific <see cref="DbContextOptions"/> instance.
		/// </summary>
		/// <param name="options">EF Core <see cref="DbContextOptions"/> instance.</param>
		/// <returns>EF Core provider information.</returns>
		public static EFProviderInfo GetEFProviderInfo(DbContextOptions options)
		{
			var info = new EFProviderInfo
			{
				Connection = null,
				Context = null,
				Options = options
			};

			return info;
		}

		/// <summary>
		/// Returns LINQ To DB provider, based on provider data from EF Core.
		/// </summary>
		/// <param name="options">Linq To DB context options.</param>
		/// <param name="info">EF Core provider information.</param>
		/// <param name="connectionInfo">Database connection information.</param>
		/// <returns>LINQ TO DB provider instance.</returns>
		public static IDataProvider GetDataProvider(DataOptions options, EFProviderInfo info, EFConnectionInfo connectionInfo)
		{
			var provider = Implementation.GetDataProvider(options, info, connectionInfo)
				?? throw new LinqToDBForEFToolsException("Can not detect provider from Entity Framework or provider not supported");

			return provider;
		}

		/// <summary>
		/// Creates mapping schema using provided EF Core data model.
		/// </summary>
		/// <param name="model">EF Core data model.</param>
		/// <param name="accessor">EF Core service provider.</param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		/// <returns>Mapping schema for provided EF Core model.</returns>
		public static MappingSchema GetMappingSchema(
			IModel model,
			IInfrastructure<IServiceProvider>? accessor,
			DataOptions? dataOptions)
		{
			var converterSelector = accessor?.GetService<IValueConverterSelector>();
			var mappingSource = accessor?.GetService<IRelationalTypeMappingSource>();
			
			return Implementation.GetMappingSchema(model, mappingSource, GetMetadataReader(model, accessor), converterSelector, dataOptions);
		}
		
		/// <summary>
		/// Creates mapping schema using provided EF Core data model.
		/// </summary>
		/// <param name="model">EF Core data model.</param>
		/// <param name="mappingSource">EF Core mapping source.</param>
		/// <param name="converterSelector">EF Core converter selector.</param>
		/// <param name="dataOptions">Linq To DB context options.</param>
		/// <returns>Mapping schema for provided EF Core model.</returns>
		public static MappingSchema GetMappingSchema(
			IModel                        model,
			IRelationalTypeMappingSource? mappingSource,
			IValueConverterSelector?      converterSelector,
			DataOptions?                  dataOptions)
		{
			return Implementation.GetMappingSchema(model, mappingSource, GetMetadataReader(model, null), converterSelector, dataOptions);
		}

		/// <summary>
		/// Transforms EF Core expression tree to LINQ To DB expression.
		/// </summary>
		/// <param name="expression">EF Core expression tree.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> instance.</param>
		/// <param name="ctx">Optional DbContext instance.</param>
		/// <param name="model">EF Core data model instance.</param>
		/// <param name="isQueryExpression">Indicates that query may contain tracking information</param>
		/// <returns>Transformed expression.</returns>
		public static Expression TransformExpression(Expression expression, IDataContext? dc, DbContext? ctx, IModel? model, bool isQueryExpression)
		{
			return Implementation.TransformExpression(expression, dc, ctx, model, isQueryExpression);
		}

		/// <summary>
		/// Creates LINQ To DB <see cref="DataConnection"/> instance, attached to provided
		/// EF Core <see cref="DbContext"/> instance connection and transaction.
		/// </summary>
		/// <param name="context">EF Core <see cref="DbContext"/> instance.</param>
		/// <param name="transaction">Optional transaction instance, to which created connection should be attached.
		/// If not specified, will use current <see cref="DbContext"/> transaction if it available.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDBConnection(this DbContext context,
			IDbContextTransaction? transaction = null)
		{
			ArgumentNullException.ThrowIfNull(context);

			var info    = GetEFProviderInfo(context);
			var options = context.GetLinqToDBOptions() ?? new DataOptions();
			options     = options.UseAdditionalMappingSchema(GetMappingSchema(context.Model, context, options));
			options     = EnableTracing(options, CreateLogger(info.Options));

			DataConnection? dc = null;

			transaction     ??= context.Database.CurrentTransaction;
			var dbTransaction = transaction?.GetDbTransaction() ?? info.Transaction;

			var connectionInfo = GetConnectionInfo(info, dbTransaction);
			var provider       = GetDataProvider(options, info, connectionInfo);

			if (dbTransaction != null)
			{
				// TODO: we need API for testing current connection
				//if (provider.IsCompatibleConnection(dbTransaction.Connection))
				options = options.UseTransaction(provider, dbTransaction);
				dc = new LinqToDBForEFToolsDataConnection(context, options, context.Model, TransformExpression);
			}

			if (dc == null)
			{
				var dbConnection = context.Database.GetDbConnection();
				// TODO: we need API for testing current connection
				options = options.UseConnection(provider, dbConnection);
				if (true /*provider.IsCompatibleConnection(dbConnection)*/)
					dc = new LinqToDBForEFToolsDataConnection(context, options, context.Model, TransformExpression);
				else
				{
					//dc = new LinqToDBForEFToolsDataConnection(context, provider, connectionInfo.ConnectionString, context.Model, TransformExpression);
				}
			}

			return dc;
		}

		private static readonly TraceSwitch _defaultTraceSwitch =
			new("DataConnection", "DataConnection trace switch", TraceLevel.Info.ToString());

		static DataOptions EnableTracing(DataOptions options, ILogger? logger)
		{
			return logger == null
				? options
				: options
					.UseTracing(t => Implementation.LogConnectionTrace(t, logger))
					.UseTraceSwitch(_defaultTraceSwitch);
		}

		/// <summary>
		/// Creates logger instance.
		/// </summary>
		/// <param name="options"><see cref="DbContext" /> options.</param>
		/// <returns>Logger instance.</returns>
		public static ILogger? CreateLogger(IDbContextOptions? options)
		{
			return Implementation.CreateLogger(options);
		}

		/// <summary>
		/// Creates Linq To DB data context for EF Core database context.
		/// </summary>
		/// <param name="context">EF Core database context.</param>
		/// <param name="transaction">Transaction instance.</param>
		/// <returns>Linq To DB data context.</returns>
		public static IDataContext CreateLinqToDBContext(this DbContext context,
			IDbContextTransaction? transaction = null)
		{
			ArgumentNullException.ThrowIfNull(context);

			var info    = GetEFProviderInfo(context);
			var options = context.GetLinqToDBOptions() ?? new DataOptions();
			options     = options.UseAdditionalMappingSchema(GetMappingSchema(context.Model, context, options));

			DataConnection? dc = null;

			transaction     ??= context.Database.CurrentTransaction;
			var dbTransaction = transaction?.GetDbTransaction() ?? info.Transaction;

			var connectionInfo = GetConnectionInfo(info, dbTransaction);
			var provider       = GetDataProvider(options, info, connectionInfo);
			var logger         = CreateLogger(info.Options);
			options            = EnableTracing(options, logger);

			if (dbTransaction != null)
			{
				// TODO: we need API for testing current connection
				// if (provider.IsCompatibleConnection(dbTransaction.Connection))
				options = options.UseTransaction(provider, dbTransaction);
				dc = new LinqToDBForEFToolsDataConnection(context, options, context.Model, TransformExpression);
			}

			if (dc == null)
			{
				var dbConnection = context.Database.GetDbConnection();
				// TODO: we need API for testing current connection
				options = options.UseConnection(provider, dbConnection);
				if (true /*provider.IsCompatibleConnection(dbConnection)*/)
					dc = new LinqToDBForEFToolsDataConnection(context, options, context.Model, TransformExpression);
				else
				{
					/*
					// special case when we have to create data connection by itself
					var dataContext = new LinqToDBForEFToolsDataContext(context, provider, connectionInfo.ConnectionString, context.Model, TransformExpression);

					if (mappingSchema != null)
						dataContext.MappingSchema = mappingSchema;

					if (logger != null)
						dataContext.OnTraceConnection = t => Implementation.LogConnectionTrace(t, logger);

					return dataContext;
					*/
				}
			}

			return dc;
		}

		/// <summary>
		/// Creates LINQ To DB <see cref="DataConnection"/> instance that creates new database connection using connection
		/// information from EF Core <see cref="DbContext"/> instance.
		/// </summary>
		/// <param name="context">EF Core <see cref="DbContext"/> instance.</param>
		/// <returns>LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDBConnectionDetached(this DbContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			var info           = GetEFProviderInfo(context);
			var connectionInfo = GetConnectionInfo(info, null);
			var options        = context.GetLinqToDBOptions() ?? new DataOptions();
			var dataProvider   = GetDataProvider(options, info, connectionInfo);

			options = options
				.UseAdditionalMappingSchema(GetMappingSchema(context.Model, context, options))
				.UseDataProvider(dataProvider)
				.UseConnectionString(connectionInfo.ConnectionString!);

			options = EnableTracing(options, CreateLogger(info.Options));

			var dc = new LinqToDBForEFToolsDataConnection(context, options, context.Model, TransformExpression);

			return dc;
		}

		/// <summary>
		/// Extracts database connection information from EF Core provider data.
		/// </summary>
		/// <param name="info">EF Core database provider data.</param>
		/// <param name="transaction">EF Core database transaction instance.</param>
		/// <returns>Database connection information.</returns>
		public static EFConnectionInfo GetConnectionInfo(EFProviderInfo info, DbTransaction? transaction)
		{
			var connection = info.Connection;

			var extracted = Implementation.ExtractConnectionInfo(info.Options);

			return new EFConnectionInfo
			{
				Connection = connection ?? extracted?.Connection,
				Transaction = transaction ?? info.Transaction ?? extracted?.Transaction,
				ConnectionString = extracted?.ConnectionString
			};
		}

		/// <summary>
		/// Extracts EF Core data model instance from <see cref="DbContextOptions"/>.
		/// </summary>
		/// <param name="options"><see cref="DbContextOptions"/> instance.</param>
		/// <returns>EF Core data model instance.</returns>
		public static IModel? GetModel(DbContextOptions? options)
		{
			if (options == null)
				return null;
			return Implementation.ExtractModel(options);
		}

		/// <summary>
		/// Creates new LINQ To DB <see cref="DataConnection"/> instance using connectivity information from
		/// EF Core <see cref="DbContextOptions"/> instance.
		/// </summary>
		/// <param name="options">EF Core <see cref="DbContextOptions"/> instance.</param>
		/// <returns>New LINQ To DB <see cref="DataConnection"/> instance.</returns>
		public static DataConnection CreateLinqToDBConnection(this DbContextOptions options)
		{
			var info = GetEFProviderInfo(options);

			DataConnection? dc = null;

			var connectionInfo = GetConnectionInfo(info, null);
			var dataOptions    = options.GetLinqToDBOptions() ?? new DataOptions();
			var dataProvider   = GetDataProvider(dataOptions, info, connectionInfo);
			var model          = GetModel(options);

			if (model != null)
				dataOptions = dataOptions.UseAdditionalMappingSchema(GetMappingSchema(model, null, dataOptions));

			dataOptions = dataOptions.UseDataProvider(dataProvider);
			dataOptions = EnableTracing(dataOptions, CreateLogger(info.Options));

			if (connectionInfo.Connection != null)
			{
				dataOptions = dataOptions.UseConnection(connectionInfo.Connection);
				dc          = new LinqToDBForEFToolsDataConnection(null, dataOptions, model, TransformExpression);
			}
			else if (connectionInfo.ConnectionString != null)
			{
				dataOptions = dataOptions.UseConnectionString(connectionInfo.ConnectionString);
				dc          = new LinqToDBForEFToolsDataConnection(null, dataOptions, model, TransformExpression);
			}

			if (dc == null)
				throw new LinqToDBForEFToolsException($"Can not extract connection information from {nameof(DbContextOptions)}");

			return dc;
		}

		/// <summary>
		/// Converts EF Core's query to LINQ To DB query and attach it to provided LINQ To DB <see cref="IDataContext"/>.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="query">EF Core query.</param>
		/// <param name="dc">LINQ To DB <see cref="IDataContext"/> to use with provided query.</param>
		/// <returns>LINQ To DB query, attached to provided <see cref="IDataContext"/>.</returns>
		public static IQueryable<T> ToLinqToDB<T>(this IQueryable<T> query, IDataContext dc)
		{
			ArgumentNullException.ThrowIfNull(query);
			ArgumentNullException.ThrowIfNull(dc);

			var context = Implementation.GetCurrentContext(query)
				?? throw new LinqToDBForEFToolsException("Can not evaluate current context from query");

			AddInterceptorsToDataContext(context, dc);
			return new LinqToDBForEFQueryProvider<T>(dc, query.Expression);
		}

		private static void AddInterceptorsToDataContext(DbContext context, IDataContext dc)
		{
			var options = context.GetLinqToDBOptions();

			if (options?.DataContextOptions.Interceptors?.Any() == true)
			{
				foreach (var interceptor in options.DataContextOptions.Interceptors)
					dc.AddInterceptor(interceptor);
			}
		}

		/// <summary>
		/// Converts EF Core's query to LINQ To DB query and attach it to current EF Core connection.
		/// </summary>
		/// <typeparam name="T">Entity type.</typeparam>
		/// <param name="query">EF Core query.</param>
		/// <returns>LINQ To DB query, attached to current EF Core connection.</returns>
		public static IQueryable<T> ToLinqToDB<T>(this IQueryable<T> query)
		{
			if (query.Provider is IQueryProviderAsync)
			{
				return query;
			}

			var context = Implementation.GetCurrentContext(query)
				?? throw new LinqToDBForEFToolsException("Can not evaluate current context from query");

#pragma warning disable CA2000 // Dispose objects before losing scope
			var dc = CreateLinqToDBContext(context);
#pragma warning restore CA2000 // Dispose objects before losing scope

			return new LinqToDBForEFQueryProvider<T>(dc, query.Expression);
		}

		/// <summary>
		/// Extracts <see cref="DbContext"/> instance from <see cref="IQueryable"/> object.
		/// </summary>
		/// <param name="query">EF Core query.</param>
		/// <returns>Current <see cref="DbContext"/> instance.</returns>
		public static DbContext? GetCurrentContext(IQueryable query)
		{
			return Implementation.GetCurrentContext(query);
		}

		/// <summary>
		/// Enables attaching entities to change tracker.
		/// Entities will be attached only if AsNoTracking() is not used in query and DbContext is configured to track entities.
		/// </summary>
		public static bool EnableChangeTracker
		{
			get => Implementation.EnableChangeTracker;
			set => Implementation.EnableChangeTracker = value;
		}
	}
}
