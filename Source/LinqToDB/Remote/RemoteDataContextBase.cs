using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Expressions;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Cache;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Interceptors;
using LinqToDB.Internal.Remote;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

namespace LinqToDB.Remote
{
	[PublicAPI]
	public abstract partial class RemoteDataContextBase : IDataContext,
		IInfrastructure<IServiceProvider>
	{
		protected RemoteDataContextBase(DataOptions options)
		{
			Options = options;

			options.Apply(this);
		}

		public string?          ConfigurationString
		{
			get;
			// TODO: Mark private in v7 and remove warning suppressions from callers
			[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
			set;
		}

		public void AddMappingSchema(MappingSchema mappingSchema)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			MappingSchema    = MappingSchema.CombineSchemas(mappingSchema, MappingSchema);
			_configurationID = null;
		}

		void IDataContext.SetMappingSchema(MappingSchema mappingSchema)
		{
			MappingSchema    = mappingSchema;
#pragma warning restore CS0618 // Type or member is obsolete
			_configurationID = null;
		}

		protected void InitServiceProvider(SimpleServiceProvider serviceProvider)
		{
			serviceProvider.AddService(GetConfigurationInfoForPublicApi().MemberTranslator);
		}

		SimpleServiceProvider? _serviceProvider;
		readonly Lock          _guard = new();

		IServiceProvider IInfrastructure<IServiceProvider>.Instance
		{
			get
			{
				ThrowOnDisposed();

				if (_serviceProvider == null)
				{
					lock (_guard)
					{
						if (_serviceProvider == null)
						{
							var serviceProvider = new SimpleServiceProvider();
							InitServiceProvider(serviceProvider);
							_serviceProvider = serviceProvider;
						}
					}
				}

				return _serviceProvider;
			}
		}

		sealed class ConfigurationInfo
		{
			public LinqServiceInfo   LinqServiceInfo  = null!;
			public MappingSchema     MappingSchema    = null!;
			public IMemberTranslator MemberTranslator = null!;
		}

		static readonly ConcurrentDictionary<string,ConfigurationInfo> _configurations = new();

		sealed class RemoteMappingSchema : MappingSchema
		{
			static readonly MemoryCache<(string contextIDPrefix, Type mappingSchemaType), MappingSchema> _cache = new (new ());

			public static MappingSchema GetOrCreate(string contextIDPrefix, Type mappingSchemaType)
			{
				return _cache.GetOrCreate(
					(contextIDPrefix, mappingSchemaType),
					static entry =>
					{
						entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
						return new RemoteMappingSchema(entry.Key.contextIDPrefix, ActivatorExt.CreateInstance<MappingSchema>(entry.Key.mappingSchemaType));
					});
			}

			private RemoteMappingSchema(string configuration, MappingSchema mappingSchema)
				: base(configuration, mappingSchema)
			{
			}
		}

		sealed class RemoteMemberTranslator : IMemberTranslator
		{
			static readonly MemoryCache<Type, IMemberTranslator> _cache = new (new ());

			public IMemberTranslator ProviderTranslator { get; }

			public static IMemberTranslator GetOrCreate(Type methodCallTranslatorType)
			{
				return _cache.GetOrCreate(
					methodCallTranslatorType,
					static entry =>
					{
						entry.SlidingExpiration = Common.Configuration.Linq.CacheSlidingExpiration;
						return new RemoteMemberTranslator(ActivatorExt.CreateInstance<IMemberTranslator>(entry.Key));
					});
			}

			RemoteMemberTranslator(IMemberTranslator providerTranslator)
			{
				ProviderTranslator = providerTranslator;
			}

			public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
				=> ProviderTranslator.Translate(translationContext, memberExpression, translationFlags);
		}

		ConfigurationInfo? _configurationInfo;

		// call it only external sync calls
		// https://github.com/linq2db/linq2db/issues/4618
		// for internal use ensure we called PreloadConfigurationInfoAsync to preload it async
		ConfigurationInfo GetConfigurationInfoForPublicApi()
		{
			if (_configurationInfo != null || _configurations.TryGetValue(ConfigurationString ?? "", out _configurationInfo))
			{
				return _configurationInfo;
			}

			SafeAwaiter.Run(PreloadConfigurationInfoAsync);

			return _configurationInfo!;
		}

		// this cannot be used
		//[MemberNotNull(nameof(_configurationInfo))]
		async ValueTask PreloadConfigurationInfoAsync(CancellationToken cancellationToken)
		{
			if (_configurationInfo == null && !_configurations.TryGetValue(ConfigurationString ?? "", out _configurationInfo))
			{
				var client = GetClient();

				try
				{
					var info           = await client.GetInfoAsync(ConfigurationString, cancellationToken).ConfigureAwait(false);

					var type           = Type.GetType(info.MappingSchemaType)!;
					var ms             = RemoteMappingSchema.GetOrCreate(ContextIDPrefix, type);

					var translatorType = Type.GetType(info.MethodCallTranslatorType)!;
					var translator     = RemoteMemberTranslator.GetOrCreate(translatorType);

					_configurationInfo = _configurations[ConfigurationString ?? ""] = new ConfigurationInfo()
					{
						LinqServiceInfo  = info,
						MappingSchema    = ms,
						MemberTranslator = translator,
					};
				}
				finally
				{
					await DisposeClientAsync(client).ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Preload configuration info asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token to cancel operation.</param>
		/// <returns>Task which completes when configuration info is loaded.</returns>
		public ValueTask ConfigureAsync(CancellationToken cancellationToken)
		{
			// preload _configurationInfo asynchronously if needed
			return PreloadConfigurationInfoAsync(cancellationToken);
		}

		protected abstract ILinqService GetClient();
		protected abstract string       ContextIDPrefix { get; }

		string?            _contextName;
		string IDataContext.ContextName => _contextName ??= GetConfigurationInfoForPublicApi().MappingSchema.ConfigurationList[0];

		int  _msID;
		int? _configurationID;
		int IConfigurationID.ConfigurationID
		{
			get
			{
				if (_configurationID == null || _msID != ((IConfigurationID)MappingSchema).ConfigurationID)
				{
					using var idBuilder = new IdentifierBuilder();
					_configurationID = idBuilder
						.Add(_msID = ((IConfigurationID)MappingSchema).ConfigurationID)
						.Add(Options)
						.Add(GetType())
						.CreateID();
				}

				return _configurationID.Value;
			}
		}

		private MappingSchema? _providedMappingSchema;
		private MappingSchema? _mappingSchema;
		private MappingSchema? _serializationMappingSchema;

		public  MappingSchema   MappingSchema
		{
			get
			{
				ThrowOnDisposed();

				return _mappingSchema ??= _providedMappingSchema == null ? GetConfigurationInfoForPublicApi().MappingSchema : MappingSchema.CombineSchemas(_providedMappingSchema, GetConfigurationInfoForPublicApi().MappingSchema);
			}
			// TODO: Mark private in v7 and remove warning suppressions from callers
			[Obsolete("This API scheduled for removal in v7"), EditorBrowsable(EditorBrowsableState.Never)]
			set
			{
				ThrowOnDisposed();

				// Because setter could be called from constructor, we cannot build composite schemas here to avoid server calls on half-initialized context
				// Instead we reset schemas status and finish initialization in getters for MappingSchema and SerializationMappingSchema, when they are called
				if (!Equals(_providedMappingSchema, value))
				{
					_providedMappingSchema = value;
					// reset schemas
					_mappingSchema              = null;
					_serializationMappingSchema = null;
				}
			}
		}

		internal MappingSchema SerializationMappingSchema => _serializationMappingSchema ??= MappingSchema.CombineSchemas(Internal.Remote.SerializationMappingSchema.Instance, MappingSchema);

		public bool InlineParameters { get; set; }
		public bool CloseAfterUse    { get; set; }

		public List<string> QueryHints => field ??= new();
		public List<string> NextQueryHints => field ??= new();

		protected virtual Type SqlProviderType
		{
			get
			{
				ThrowOnDisposed();

				if (field == null)
				{
					var type = GetConfigurationInfoForPublicApi().LinqServiceInfo.SqlBuilderType;
					field = Type.GetType(type)!;
				}

				return field;
			}

			set
			{
				ThrowOnDisposed();

				field = value;
			}
		}

		protected virtual Type SqlOptimizerType
		{
			get
			{
				if (field == null)
				{
					var type = GetConfigurationInfoForPublicApi().LinqServiceInfo.SqlOptimizerType;
					field = Type.GetType(type)!;
				}

				return field;
			}

			set
			{
				ThrowOnDisposed();

				field = value;
			}
		}

		/// <summary>
		/// Current DataContext LINQ options
		/// </summary>
		public DataOptions Options { get; private set; }

		SqlProviderFlags IDataContext.SqlProviderFlags      => GetConfigurationInfoForPublicApi().LinqServiceInfo.SqlProviderFlags;
		TableOptions     IDataContext.SupportedTableOptions => GetConfigurationInfoForPublicApi().LinqServiceInfo.SupportedTableOptions;

		Type IDataContext.DataReaderType => typeof(RemoteDataReader);

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			ThrowOnDisposed();

			var dataType   = reader.GetFieldType(idx);
			var methodInfo = GetReaderMethodInfo(dataType);

			Expression ex = Expression.Call(readerExpression, methodInfo, ExpressionInstances.Int32Array(idx));

			if (ex.Type != dataType)
				ex = Expression.Convert(ex, dataType);

			return ex;
		}

		static MethodInfo GetReaderMethodInfo(Type type)
		{
			switch (type.ToNullableUnderlying().GetTypeCodeEx())
			{
				case TypeCode.Boolean  : return MemberHelper.MethodOf<DbDataReader>(r => r.GetBoolean (0));
				case TypeCode.Byte     : return MemberHelper.MethodOf<DbDataReader>(r => r.GetByte    (0));
				case TypeCode.Char     : return MemberHelper.MethodOf<DbDataReader>(r => r.GetChar    (0));
				case TypeCode.Int16    : return MemberHelper.MethodOf<DbDataReader>(r => r.GetInt16   (0));
				case TypeCode.Int32    : return MemberHelper.MethodOf<DbDataReader>(r => r.GetInt32   (0));
				case TypeCode.Int64    : return MemberHelper.MethodOf<DbDataReader>(r => r.GetInt64   (0));
				case TypeCode.Single   : return MemberHelper.MethodOf<DbDataReader>(r => r.GetFloat   (0));
				case TypeCode.Double   : return MemberHelper.MethodOf<DbDataReader>(r => r.GetDouble  (0));
				case TypeCode.String   : return MemberHelper.MethodOf<DbDataReader>(r => r.GetString  (0));
				case TypeCode.Decimal  : return MemberHelper.MethodOf<DbDataReader>(r => r.GetDecimal (0));
				case TypeCode.DateTime : return MemberHelper.MethodOf<DbDataReader>(r => r.GetDateTime(0));
			}

			if (type == typeof(Guid))
				return MemberHelper.MethodOf<DbDataReader>(r => r.GetGuid(0));

			return MemberHelper.MethodOf<DbDataReader>(dr => dr.GetValue(0));
		}

		bool? IDataContext.IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return null;
		}

		static readonly ConcurrentDictionary<Tuple<Type,MappingSchema,Type,SqlProviderFlags,DataOptions>,Func<ISqlBuilder>> _sqlBuilders = new ();

		Func<ISqlBuilder>? _createSqlBuilder;

		Func<ISqlBuilder> IDataContext.CreateSqlBuilder
		{
			get
			{
				ThrowOnDisposed();

				if (_createSqlBuilder == null)
				{
					var key = Tuple.Create(SqlProviderType, MappingSchema, SqlOptimizerType, ((IDataContext)this).SqlProviderFlags, Options);

					_createSqlBuilder = _sqlBuilders.GetOrAdd(
						key,
						static (key, args) =>
						{
							return Expression.Lambda<Func<ISqlBuilder>>(
								Expression.New(
									key.Item1.GetConstructor(new[]
									{
										typeof(IDataProvider),
										typeof(MappingSchema),
										typeof(DataOptions),
										typeof(ISqlOptimizer),
										typeof(SqlProviderFlags)
									}) ?? throw new InvalidOperationException($"Constructor for type '{key.Item1.Name}' not found."),
									new Expression[]
									{
										Expression.Constant(null, typeof(IDataProvider)),
										Expression.Constant(args.mappingSchema, typeof(MappingSchema)),
										Expression.Constant(key.Item5),
										Expression.Constant(args.sqlOptimizer),
										Expression.Constant(key.Item4)
									}))
								.CompileExpression();
						},
						(mappingSchema: MappingSchema, sqlOptimizer: ((IDataContext)this).GetSqlOptimizer(Options))
					);
				}

				return _createSqlBuilder;
			}
		}

		static readonly ConcurrentDictionary<Tuple<Type,SqlProviderFlags>,Func<DataOptions,ISqlOptimizer>> _sqlOptimizers = new ();

		Func<DataOptions,ISqlOptimizer>? _getSqlOptimizer;

		Func<DataOptions,ISqlOptimizer> IDataContext.GetSqlOptimizer
		{
			get
			{
				ThrowOnDisposed();

				if (_getSqlOptimizer == null)
				{
					var key = Tuple.Create(SqlOptimizerType, ((IDataContext)this).SqlProviderFlags);

					_getSqlOptimizer = _sqlOptimizers.GetOrAdd(key, static key =>
					{
						var p = Expression.Parameter(typeof(DataOptions));
						var c = key.Item1.GetConstructor(new[] {typeof(SqlProviderFlags)});

						if (c != null)
							return Expression.Lambda<Func<DataOptions,ISqlOptimizer>>(
								Expression.New(c, Expression.Constant(key.Item2)),
								p)
								.CompileExpression();

						return Expression.Lambda<Func<DataOptions,ISqlOptimizer>>(
							Expression.New(
								key.Item1.GetConstructor(new[] {typeof(SqlProviderFlags), typeof(DataOptions)}) ?? throw new InvalidOperationException($"Constructor for type '{key.Item1.Name}' not found."),
								Expression.Constant(key.Item2),
								p),
							p)
							.CompileExpression();
					});
				}

				return _getSqlOptimizer;
			}
		}

		List<string>? _queryBatch;
		int           _batchCounter;

		public void BeginBatch()
		{
			ThrowOnDisposed();

			_batchCounter++;

			_queryBatch ??= new List<string>();
		}

		public void CommitBatch() => SafeAwaiter.Run(CommitBatchAsync);
		
		public async Task CommitBatchAsync(CancellationToken cancellationToken = default)
		{
			ThrowOnDisposed();

			if (_batchCounter == 0)
				throw new InvalidOperationException();

			_batchCounter--;

			if (_batchCounter == 0)
			{
				var client = GetClient();

				try
				{
					await PreloadConfigurationInfoAsync(cancellationToken).ConfigureAwait(false);
					var data = LinqServiceSerializer.Serialize(SerializationMappingSchema, _queryBatch!.ToArray());
					await client.ExecuteBatchAsync(ConfigurationString, data, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					await DisposeClientAsync(client).ConfigureAwait(false);
					_queryBatch = null;
				}
			}
		}

		private static void DisposeClient(ILinqService client)
		{
			if (client is IDisposable disposable)
				disposable.Dispose();
			else if (client is IAsyncDisposable asyncDisposable)
				SafeAwaiter.Run(asyncDisposable.DisposeAsync);
		}

		private static async ValueTask DisposeClientAsync(ILinqService client)
		{
			if (client is IAsyncDisposable asyncDisposable)
				await asyncDisposable.DisposeAsync().ConfigureAwait(false);
			else if (client is IDisposable disposable)
				disposable.Dispose();
		}

		protected bool Disposed { get; private set; }

		protected void ThrowOnDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException(GetType().FullName, "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		void IDataContext.Close()
		{
			if (((IInterceptable<IDataContextInterceptor>)this).Interceptor != null)
			{
				using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosing))
					((IInterceptable<IDataContextInterceptor>)this).Interceptor.OnClosing(new(this));

				using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosed))
					((IInterceptable<IDataContextInterceptor>)this).Interceptor.OnClosed (new(this));
			}
		}

		async Task IDataContext.CloseAsync()
		{
			if (((IInterceptable<IDataContextInterceptor>)this).Interceptor != null)
			{
				await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosingAsync))
					await ((IInterceptable<IDataContextInterceptor>)this).Interceptor.OnClosingAsync(new (this))
						.ConfigureAwait(false);

				await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosedAsync))
					await ((IInterceptable<IDataContextInterceptor>)this).Interceptor.OnClosedAsync (new (this))
						.ConfigureAwait(false);
			}
		}

		public virtual void Dispose()
		{
			((IDataContext)this).Close();

			Disposed = true;
		}

		public virtual async ValueTask DisposeAsync()
		{
			await ((IDataContext)this).CloseAsync().ConfigureAwait(false);

			Disposed = true;
		}

		internal static class ConfigurationApplier
		{
			public static void Apply(RemoteDataContextBase dataContext, ConnectionOptions options)
			{
				if (options.ConfigurationString != null)
				{
#pragma warning disable CS0618 // Type or member is obsolete
					dataContext.ConfigurationString = options.ConfigurationString;
#pragma warning restore CS0618 // Type or member is obsolete
				}

				if (options.MappingSchema != null)
				{
#pragma warning disable CS0618 // Type or member is obsolete
					dataContext.MappingSchema = options.MappingSchema;
#pragma warning restore CS0618 // Type or member is obsolete
				}
				else if (dataContext.Options.LinqOptions.EnableContextSchemaEdit)
				{
#pragma warning disable CS0618 // Type or member is obsolete
					dataContext.MappingSchema = new(dataContext.MappingSchema);
#pragma warning restore CS0618 // Type or member is obsolete
				}
			}

			public static Action? Reapply(RemoteDataContextBase dataContext, ConnectionOptions options, ConnectionOptions? previousOptions)
			{
				// For ConnectionOptions we reapply only mapping schema and connection interceptor.
				// Connection string, configuration, data provider, etc. are not reapplyable.
				//
				if (options.ConfigurationString       != previousOptions?.ConfigurationString)       throw new LinqToDBException($"Option '{nameof(options.ConfigurationString)} cannot be changed for context dynamically.");
				if (options.ConnectionString          != previousOptions?.ConnectionString)          throw new LinqToDBException($"Option '{nameof(options.ConnectionString)} cannot be changed for context dynamically.");
				if (options.ProviderName              != previousOptions?.ProviderName)              throw new LinqToDBException($"Option '{nameof(options.ProviderName)} cannot be changed for context dynamically.");
				if (options.DbConnection              != previousOptions?.DbConnection)              throw new LinqToDBException($"Option '{nameof(options.DbConnection)} cannot be changed for context dynamically.");
				if (options.DbTransaction             != previousOptions?.DbTransaction)             throw new LinqToDBException($"Option '{nameof(options.DbTransaction)} cannot be changed for context dynamically.");
				if (options.DisposeConnection         != previousOptions?.DisposeConnection)         throw new LinqToDBException($"Option '{nameof(options.DisposeConnection)} cannot be changed for context dynamically.");
				if (options.DataProvider              != previousOptions?.DataProvider)              throw new LinqToDBException($"Option '{nameof(options.DataProvider)} cannot be changed for context dynamically.");
				if (options.ConnectionFactory         != previousOptions?.ConnectionFactory)         throw new LinqToDBException($"Option '{nameof(options.ConnectionFactory)} cannot be changed for context dynamically.");
				if (options.DataProviderFactory       != previousOptions?.DataProviderFactory)       throw new LinqToDBException($"Option '{nameof(options.DataProviderFactory)} cannot be changed for context dynamically.");
				if (options.OnEntityDescriptorCreated != previousOptions?.OnEntityDescriptorCreated) throw new LinqToDBException($"Option '{nameof(options.OnEntityDescriptorCreated)} cannot be changed for context dynamically.");

				Action? action = null;

				if (!ReferenceEquals(options.MappingSchema, previousOptions?.MappingSchema))
				{
					var mappingSchema              = dataContext._mappingSchema;
					var serializationMappingSchema = dataContext._serializationMappingSchema;

					dataContext._mappingSchema = null;

#pragma warning disable CS0618 // Type or member is obsolete
					if (options.MappingSchema != null)
					{
						dataContext.MappingSchema = options.MappingSchema;
					}
					else if (dataContext.Options.LinqOptions.EnableContextSchemaEdit)
					{
						dataContext.MappingSchema = new (dataContext.MappingSchema);
					}
#pragma warning restore CS0618 // Type or member is obsolete

					action += () =>
					{
						dataContext._mappingSchema              = mappingSchema;
						dataContext._serializationMappingSchema = serializationMappingSchema;
					};
				}

				return action;
			}

			public static void Apply(RemoteDataContextBase dataContext, DataContextOptions options)
			{
				if (options.Interceptors != null)
					foreach (var interceptor in options.Interceptors)
						dataContext.AddInterceptor(interceptor);
			}

			public static Action? Reapply(RemoteDataContextBase dataContext, DataContextOptions options, DataContextOptions? previousOptions)
			{
				Action? action = null;

				if (!ReferenceEquals(options.Interceptors, previousOptions?.Interceptors))
				{
					if (previousOptions?.Interceptors != null)
						foreach (var interceptor in previousOptions.Interceptors)
							dataContext.RemoveInterceptor(interceptor);

					if (options.Interceptors != null)
						foreach (var interceptor in options.Interceptors)
							dataContext.AddInterceptor(interceptor);

					action += () =>
					{
						if (options.Interceptors != null)
							foreach (var interceptor in options.Interceptors)
								dataContext.RemoveInterceptor(interceptor);

						if (previousOptions?.Interceptors != null)
							foreach (var interceptor in previousOptions.Interceptors)
								dataContext.AddInterceptor(interceptor);
					};
				}

				return action;
			}
		}

		/// <inheritdoc cref="IDataContext.UseOptions"/>
		public IDisposable? UseOptions(Func<DataOptions,DataOptions> optionsSetter)
		{
			var prevOptions = Options;
			var newOptions  = optionsSetter(Options) ?? throw new ArgumentNullException(nameof(optionsSetter));

			if (((IConfigurationID)prevOptions).ConfigurationID == ((IConfigurationID)newOptions).ConfigurationID)
				return null;

			var configurationID = _configurationID;

			Options          = newOptions;
			_configurationID = null;

			var action = Options.Reapply(this, prevOptions);

			action += () =>
			{
				Options          = prevOptions;
				_configurationID = configurationID;
			};

			return new DisposableAction(action);
		}

		/// <inheritdoc cref="IDataContext.UseMappingSchema"/>
		public IDisposable? UseMappingSchema(MappingSchema mappingSchema)
		{
			var oldSchema       = MappingSchema;
			var configurationID = _configurationID;

			AddMappingSchema(mappingSchema);

			return new DisposableAction(() =>
			{
				((IDataContext)this).SetMappingSchema(oldSchema);
				_configurationID = configurationID;
			});
		}
	}
}
