﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.ServiceModel
{
	using Expressions;
	using Extensions;
	using Mapping;
	using SqlProvider;

	[PublicAPI]
	public abstract partial class RemoteDataContextBase : IDataContext, IEntityServices
	{
		public string Configuration { get; set; }

		class ConfigurationInfo
		{
			public LinqServiceInfo LinqServiceInfo;
			public MappingSchema   MappingSchema;
		}

		static readonly ConcurrentDictionary<string,ConfigurationInfo> _configurations = new ConcurrentDictionary<string,ConfigurationInfo>();

		class RemoteMappingSchema : MappingSchema
		{
			public RemoteMappingSchema(string configuration, MappingSchema mappingSchema)
				: base(configuration, mappingSchema)
			{
			}
		}

		ConfigurationInfo _configurationInfo;

		ConfigurationInfo GetConfigurationInfo()
		{
			if (_configurationInfo == null && !_configurations.TryGetValue(Configuration ?? "", out _configurationInfo))
			{
				var client = GetClient();

				try
				{
					var info = client.GetInfo(Configuration);

					MappingSchema ms;

					if (string.IsNullOrEmpty(info.MappingSchemaType))
					{
						ms = new MappingSchema(
							info.ConfigurationList
								.Select(c => ContextIDPrefix + "." + c).Concat(new[] { ContextIDPrefix }).Concat(info.ConfigurationList)
								.Select(c => new MappingSchema(c)).     Concat(new[] { MappingSchema.Default })
								.ToArray());
					}
					else
					{
						var type = Type.GetType(info.MappingSchemaType);
						ms = new RemoteMappingSchema(ContextIDPrefix, (MappingSchema)Activator.CreateInstance(type));
					}

					_configurationInfo = new ConfigurationInfo
					{
						LinqServiceInfo = info,
						MappingSchema   = ms,
					};
				}
				finally
				{
					((IDisposable)client).Dispose();
				}
			}

			return _configurationInfo;
		}

		protected abstract ILinqClient  GetClient();
		protected abstract IDataContext Clone    ();
		protected abstract string       ContextIDPrefix { get; }

		string             _contextID;
		string IDataContext.ContextID =>
			_contextID ?? (_contextID = GetConfigurationInfo().MappingSchema.ConfigurationList[0]);

		private MappingSchema _mappingSchema;
		public  MappingSchema  MappingSchema
		{
			get => _mappingSchema ?? (_mappingSchema = GetConfigurationInfo().MappingSchema);
			set => _mappingSchema = value;
		}

		public  bool InlineParameters { get; set; }
		public  bool CloseAfterUse    { get; set; }


		private List<string> _queryHints;
		public  List<string>  QueryHints => _queryHints ?? (_queryHints = new List<string>());

		private List<string> _nextQueryHints;
		public  List<string>  NextQueryHints => _nextQueryHints ?? (_nextQueryHints = new List<string>());

		private        Type _sqlProviderType;
		public virtual Type  SqlProviderType
		{
			get
			{
				if (_sqlProviderType == null)
				{
					var type = GetConfigurationInfo().LinqServiceInfo.SqlBuilderType;
					_sqlProviderType = Type.GetType(type);
				}

				return _sqlProviderType;
			}

			set => _sqlProviderType = value;
		}

		private        Type _sqlOptimizerType;
		public virtual Type  SqlOptimizerType
		{
			get
			{
				if (_sqlOptimizerType == null)
				{
					var type = GetConfigurationInfo().LinqServiceInfo.SqlOptimizerType;
					_sqlOptimizerType = Type.GetType(type);
				}

				return _sqlOptimizerType;
			}

			set => _sqlOptimizerType = value;
		}

		SqlProviderFlags IDataContext.SqlProviderFlags => GetConfigurationInfo().LinqServiceInfo.SqlProviderFlags;

		Type IDataContext.DataReaderType => typeof(ServiceModelDataReader);

		Expression IDataContext.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var dataType   = reader.GetFieldType(idx);
			var methodInfo = GetReaderMethodInfo(dataType);

			Expression ex = Expression.Call(readerExpression, methodInfo, Expression.Constant(idx));

			if (ex.Type != dataType)
				ex = Expression.Convert(ex, dataType);

			return ex;
		}

		static MethodInfo GetReaderMethodInfo(Type type)
		{
			switch (type.GetTypeCodeEx())
			{
				case TypeCode.Boolean  : return MemberHelper.MethodOf<IDataReader>(r => r.GetBoolean (0));
				case TypeCode.Byte     : return MemberHelper.MethodOf<IDataReader>(r => r.GetByte    (0));
				case TypeCode.Char     : return MemberHelper.MethodOf<IDataReader>(r => r.GetChar    (0));
				case TypeCode.Int16    : return MemberHelper.MethodOf<IDataReader>(r => r.GetInt16   (0));
				case TypeCode.Int32    : return MemberHelper.MethodOf<IDataReader>(r => r.GetInt32   (0));
				case TypeCode.Int64    : return MemberHelper.MethodOf<IDataReader>(r => r.GetInt64   (0));
				case TypeCode.Single   : return MemberHelper.MethodOf<IDataReader>(r => r.GetFloat   (0));
				case TypeCode.Double   : return MemberHelper.MethodOf<IDataReader>(r => r.GetDouble  (0));
				case TypeCode.String   : return MemberHelper.MethodOf<IDataReader>(r => r.GetString  (0));
				case TypeCode.Decimal  : return MemberHelper.MethodOf<IDataReader>(r => r.GetDecimal (0));
				case TypeCode.DateTime : return MemberHelper.MethodOf<IDataReader>(r => r.GetDateTime(0));
			}

			if (type == typeof(Guid))
				return MemberHelper.MethodOf<IDataReader>(r => r.GetGuid(0));

			return MemberHelper.MethodOf<IDataReader>(dr => dr.GetValue(0));
		}

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx)
		{
			return null;
		}

		static readonly Dictionary<Type,Func<ISqlBuilder>> _sqlBuilders = new Dictionary<Type, Func<ISqlBuilder>>();

		Func<ISqlBuilder> _createSqlProvider;

		Func<ISqlBuilder> IDataContext.CreateSqlProvider
		{
			get
			{
				if (_createSqlProvider == null)
				{
					var type = SqlProviderType;

					if (!_sqlBuilders.TryGetValue(type, out _createSqlProvider))
						lock (_sqlProviderType)
							if (!_sqlBuilders.TryGetValue(type, out _createSqlProvider))
								_sqlBuilders.Add(type, _createSqlProvider =
									Expression.Lambda<Func<ISqlBuilder>>(
										Expression.New(
											type.GetConstructorEx(new[]
											{
												typeof(ISqlOptimizer),
												typeof(SqlProviderFlags),
												typeof(ValueToSqlConverter)
											}),
											new Expression[]
											{
												Expression.Constant(GetSqlOptimizer()),
												Expression.Constant(((IDataContext)this).SqlProviderFlags),
												Expression.Constant(((IDataContext)this).MappingSchema.ValueToSqlConverter)
											})).Compile());
				}

				return _createSqlProvider;
			}
		}

		static readonly Dictionary<Type,Func<ISqlOptimizer>> _sqlOptimizers = new Dictionary<Type,Func<ISqlOptimizer>>();

		Func<ISqlOptimizer> _getSqlOptimizer;

		public Func<ISqlOptimizer> GetSqlOptimizer
		{
			get
			{
				if (_getSqlOptimizer == null)
				{
					var type = SqlOptimizerType;

					if (!_sqlOptimizers.TryGetValue(type, out _getSqlOptimizer))
						lock (_sqlOptimizerType)
							if (!_sqlOptimizers.TryGetValue(type, out _getSqlOptimizer))
								_sqlOptimizers.Add(type, _getSqlOptimizer =
									Expression.Lambda<Func<ISqlOptimizer>>(
										Expression.New(
											type.GetConstructorEx(new[]
											{
												typeof(SqlProviderFlags)
											}),
											new Expression[]
											{
												Expression.Constant(((IDataContext)this).SqlProviderFlags)
											})).Compile());
				}

				return _getSqlOptimizer;
			}
		}

		List<string> _queryBatch;
		int          _batchCounter;

		public void BeginBatch()
		{
			_batchCounter++;

			if (_queryBatch == null)
				_queryBatch = new List<string>();
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
					var data = LinqServiceSerializer.Serialize(_queryBatch.ToArray());
					client.ExecuteBatch(Configuration, data);
				}
				finally
				{
					((IDisposable)client).Dispose();
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
					var data = LinqServiceSerializer.Serialize(_queryBatch.ToArray());
					await client.ExecuteBatchAsync(Configuration, data);
				}
				finally
				{
					((IDisposable)client).Dispose();
					_queryBatch = null;
				}
			}
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			ThrowOnDisposed();

			return Clone();
		}

		public event EventHandler OnClosing;

		/// <inheritdoc/>
		public Action<EntityCreatedEventArgs> OnEntityCreated { get; set; }

		protected bool Disposed { get; private set; }

		protected void ThrowOnDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException("RemoteDataContext", "IDataContext is disposed, see https://github.com/linq2db/linq2db/wiki/Managing-data-connection");
		}

		void IDataContext.Close()
		{
			Close();
		}

		void Close()
		{
			OnClosing?.Invoke(this, EventArgs.Empty);
		}

		public void Dispose()
		{
			Disposed = true;

			Close();
		}
	}
}
