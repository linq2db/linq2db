using System;
using System.Data.Common;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using JetBrains.Annotations;
using LinqToDB.Common.Internal;

namespace LinqToDB.Remote
{
	using System.Threading;
	using Common;
	using DataProvider;
	using Expressions;
	using Extensions;
	using Interceptors;
	using Mapping;
	using SqlProvider;

	[PublicAPI]
	public abstract partial class RemoteDataContextBase : IDataContext
	{
		public string? Configuration { get; set; }

		sealed class ConfigurationInfo
		{
			public LinqServiceInfo LinqServiceInfo = null!;
			public MappingSchema   MappingSchema   = null!;
		}

		static readonly ConcurrentDictionary<string,ConfigurationInfo> _configurations = new();

		sealed class RemoteMappingSchema : MappingSchema
		{
			public RemoteMappingSchema(string configuration, MappingSchema mappingSchema)
				: base(configuration, mappingSchema)
			{
			}
		}

		ConfigurationInfo? _configurationInfo;

		ConfigurationInfo GetConfigurationInfo()
		{
			if (_configurationInfo == null && !_configurations.TryGetValue(Configuration ?? "", out _configurationInfo))
			{
				var client = GetClient();

				try
				{
					var info = client.GetInfo(Configuration);
					var type = Type.GetType(info.MappingSchemaType)!;
					var ms   = new RemoteMappingSchema(ContextIDPrefix, (MappingSchema)Activator.CreateInstance(type)!);

					_configurationInfo = new ConfigurationInfo
					{
						LinqServiceInfo = info,
						MappingSchema   = ms,
					};
				}
				finally
				{
					(client as IDisposable)?.Dispose();
				}
			}

			return _configurationInfo;
		}

		async Task<ConfigurationInfo> GetConfigurationInfoAsync(CancellationToken cancellationToken)
		{
			if (_configurationInfo == null && !_configurations.TryGetValue(Configuration ?? "", out _configurationInfo))
			{
				var client = GetClient();

				try
				{
					var info = await client.GetInfoAsync(Configuration, cancellationToken)
						.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					var type = Type.GetType(info.MappingSchemaType)!;
					var ms   = new RemoteMappingSchema(ContextIDPrefix, (MappingSchema)Activator.CreateInstance(type)!);

					_configurationInfo = new ConfigurationInfo
					{
						LinqServiceInfo = info,
						MappingSchema = ms,
					};
				}
				finally
				{
					(client as IDisposable)?.Dispose();
				}
			}

			return _configurationInfo;
		}

		protected abstract ILinqService GetClient();
		protected abstract IDataContext Clone    ();
		protected abstract string       ContextIDPrefix { get; }

		string?            _contextName;
		string IDataContext.ContextName => _contextName ??= GetConfigurationInfo().MappingSchema.ConfigurationList[0];

		int?               _contextID;
		int    IDataContext.ContextID   => _contextID   ??= new IdentifierBuilder(((IDataContext)this).ContextName).CreateID();

		private MappingSchema? _mappingSchema;
		public  MappingSchema   MappingSchema
		{
			get => _mappingSchema ??= GetConfigurationInfo().MappingSchema;
			set
			{
				_mappingSchema = value;
				_serializationMappingSchema = new SerializationMappingSchema(_mappingSchema);
			}
		}

		private  MappingSchema? _serializationMappingSchema;
		internal MappingSchema   SerializationMappingSchema => _serializationMappingSchema ??= new SerializationMappingSchema(MappingSchema);

		public  bool InlineParameters { get; set; }
		public  bool CloseAfterUse    { get; set; }


		private List<string>? _queryHints;
		public  List<string>   QueryHints => _queryHints ??= new List<string>();

		private List<string>? _nextQueryHints;
		public  List<string>   NextQueryHints => _nextQueryHints ??= new List<string>();

		private        Type? _sqlProviderType;
		public virtual Type   SqlProviderType
		{
			get
			{
				if (_sqlProviderType == null)
				{
					var type = GetConfigurationInfo().LinqServiceInfo.SqlBuilderType;
					_sqlProviderType = Type.GetType(type)!;
				}

				return _sqlProviderType;
			}

			set => _sqlProviderType = value;
		}

		private        Type? _sqlOptimizerType;
		public virtual Type   SqlOptimizerType
		{
			get
			{
				if (_sqlOptimizerType == null)
				{
					var type = GetConfigurationInfo().LinqServiceInfo.SqlOptimizerType;
					_sqlOptimizerType = Type.GetType(type)!;
				}

				return _sqlOptimizerType;
			}

			set => _sqlOptimizerType = value;
		}

		SqlProviderFlags IDataContext.SqlProviderFlags      => GetConfigurationInfo().LinqServiceInfo.SqlProviderFlags;
		TableOptions     IDataContext.SupportedTableOptions => GetConfigurationInfo().LinqServiceInfo.SupportedTableOptions;

		Type IDataContext.DataReaderType => typeof(RemoteDataReader);

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
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

		static readonly ConcurrentDictionary<Tuple<Type, MappingSchema, Type, SqlProviderFlags>, Func<ISqlBuilder>> _sqlBuilders = new ();

		Func<ISqlBuilder>? _createSqlProvider;

		Func<ISqlBuilder> IDataContext.CreateSqlProvider
		{
			get
			{
				if (_createSqlProvider == null)
				{
					var key  = Tuple.Create(SqlProviderType, MappingSchema, SqlOptimizerType, ((IDataContext)this).SqlProviderFlags);

#if NET45 || NET46 || NETSTANDARD2_0
					_createSqlProvider = _sqlBuilders.GetOrAdd(
						key,
						key =>
					{
						var mappingSchema = MappingSchema;
						var sqlOptimizer  = GetSqlOptimizer();
#else
					_createSqlProvider = _sqlBuilders.GetOrAdd(
						key,
						static (key, args) =>
					{
						var (mappingSchema, sqlOptimizer) = args;
#endif
						return Expression.Lambda<Func<ISqlBuilder>>(
							Expression.New(
								key.Item1.GetConstructor(new[]
								{
									typeof(IDataProvider),
									typeof(MappingSchema),
									typeof(ISqlOptimizer),
									typeof(SqlProviderFlags)
								}) ?? throw new InvalidOperationException($"Constructor for type '{key.Item1.Name}' not found."),
								new Expression[]
								{
									Expression.Constant(null, typeof(IDataProvider)),
									Expression.Constant(mappingSchema, typeof(MappingSchema)),
									Expression.Constant(sqlOptimizer),
									Expression.Constant(key.Item4)
								}))
							.CompileExpression();
					}
#if NET45 || NET46 || NETSTANDARD2_0
					);
#else
					, (MappingSchema, GetSqlOptimizer()));
#endif
				}

				return _createSqlProvider;
			}
		}

		static readonly ConcurrentDictionary<Tuple<Type, SqlProviderFlags>, Func<ISqlOptimizer>> _sqlOptimizers = new ();

		Func<ISqlOptimizer>? _getSqlOptimizer;

		public Func<ISqlOptimizer> GetSqlOptimizer
		{
			get
			{
				if (_getSqlOptimizer == null)
				{
					var key  = Tuple.Create(SqlOptimizerType, ((IDataContext)this).SqlProviderFlags);

					_getSqlOptimizer = _sqlOptimizers.GetOrAdd(key, static key =>
						Expression.Lambda<Func<ISqlOptimizer>>(
								Expression.New(
									key.Item1.GetConstructor(new[] {typeof(SqlProviderFlags)}) ??
									throw new InvalidOperationException(
										$"Constructor for type '{key.Item1.Name}' not found."),
									Expression.Constant(key.Item2)))
							.CompileExpression());
				}

				return _getSqlOptimizer;
			}
		}

		List<string>? _queryBatch;
		int           _batchCounter;

		public void BeginBatch()
		{
			_batchCounter++;

			_queryBatch ??= new List<string>();
		}

		public void CommitBatch()
		{
			if (_batchCounter == 0)
				throw new InvalidOperationException();

			_batchCounter--;

			if (_batchCounter == 0)
			{
				var client = GetClient();

				try
				{
					var data = LinqServiceSerializer.Serialize(SerializationMappingSchema, _queryBatch!.ToArray());
					client.ExecuteBatch(Configuration, data);
				}
				finally
				{
					(client as IDisposable)?.Dispose();
					_queryBatch = null;
				}
			}
		}

		public async Task CommitBatchAsync()
		{
			if (_batchCounter == 0)
				throw new InvalidOperationException();

			_batchCounter--;

			if (_batchCounter == 0)
			{
				var client = GetClient();

				try
				{
					var data = LinqServiceSerializer.Serialize(SerializationMappingSchema, _queryBatch!.ToArray());
					await client.ExecuteBatchAsync(Configuration, data).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				}
				finally
				{
					(client as IDisposable)?.Dispose();
					_queryBatch = null;
				}
			}
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			ThrowOnDisposed();

			var ctx = (RemoteDataContextBase)Clone();

			ctx._dataContextInterceptor   = _dataContextInterceptor   is AggregatedDataContextInterceptor   dc ? (AggregatedDataContextInterceptor)  dc.Clone() : _dataContextInterceptor;
			ctx._entityServiceInterceptor = _entityServiceInterceptor is AggregatedEntityServiceInterceptor es ? (AggregatedEntityServiceInterceptor)es.Clone() : _entityServiceInterceptor;

			return ctx;
		}

		protected bool Disposed { get; private set; }

		protected void ThrowOnDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException("RemoteDataContext", "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		void IDataContext.Close()
		{
			_dataContextInterceptor?.OnClosing(new (this));
			_dataContextInterceptor?.OnClosed (new (this));
		}

		async Task IDataContext.CloseAsync()
		{
			if (_dataContextInterceptor != null)
			{
				await _dataContextInterceptor.OnClosingAsync(new (this)).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				await _dataContextInterceptor.OnClosedAsync (new (this)).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		public FluentMappingBuilder GetFluentMappingBuilder()
		{
			return MappingSchema.GetFluentMappingBuilder();
		}

		public virtual void Dispose()
		{
			Disposed = true;

			((IDataContext)this).Close();
		}

#if !NATIVE_ASYNC
		public virtual Task DisposeAsync()
		{
			Disposed = true;

			return ((IDataContext)this).CloseAsync();
		}
#else
		public virtual ValueTask DisposeAsync()
		{
			Disposed = true;

			return new ValueTask(((IDataContext)this).CloseAsync());
		}
#endif

	}
}
