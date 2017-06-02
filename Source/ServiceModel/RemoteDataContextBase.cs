using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LinqToDB.ServiceModel
{
	using Expressions;
	using Extensions;
	using Linq;
	using Mapping;
	using SqlProvider;

	public abstract class RemoteDataContextBase : IDataContext
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

		protected abstract ILinqService GetClient();
		protected abstract IDataContext Clone    ();
		protected abstract string       ContextIDPrefix { get; }

		string             _contextID;
		string IDataContext.ContextID
		{
			get { return _contextID ?? (_contextID = GetConfigurationInfo().MappingSchema.ConfigurationList[0]); }
		}

		private MappingSchema _mappingSchema;
		public  MappingSchema  MappingSchema
		{
			get { return _mappingSchema ?? (_mappingSchema = GetConfigurationInfo().MappingSchema); }
			set { _mappingSchema = value; }
		}

		public  bool InlineParameters { get; set; }

		private List<string> _queryHints;
		public  List<string>  QueryHints
		{
			get { return _queryHints ?? (_queryHints = new List<string>()); }
		}

		private List<string> _nextQueryHints;
		public  List<string>  NextQueryHints
		{
			get { return _nextQueryHints ?? (_nextQueryHints = new List<string>()); }
		}

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

			set { _sqlProviderType = value;  }
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

			set { _sqlOptimizerType = value;  }
		}

		SqlProviderFlags IDataContext.SqlProviderFlags
		{
			get { return GetConfigurationInfo().LinqServiceInfo.SqlProviderFlags; }
		}

		Type IDataContext.DataReaderType
		{
			get { return typeof(ServiceModelDataReader); }
		}

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

		class QueryContext
		{
			public IQueryContext Query;
			public ILinqService  Client;
		}

		object IDataContext.SetQuery(IQueryContext queryContext)
		{
			ThrowOnDisposed();

			return new QueryContext { Query = queryContext };
		}

		int IDataContext.ExecuteNonQuery(object query)
		{
			ThrowOnDisposed();

			var ctx  = (QueryContext)query;
			var q    = ctx.Query.SelectQuery.ProcessParameters(MappingSchema);
			var data = LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters(), ctx.Query.QueryHints);

			if (_batchCounter > 0)
			{
				_queryBatch.Add(data);
				return -1;
			}

			ctx.Client = GetClient();

			return ctx.Client.ExecuteNonQuery(Configuration, data);
		}

		object IDataContext.ExecuteScalar(object query)
		{
			ThrowOnDisposed();

			if (_batchCounter > 0)
				throw new LinqException("Incompatible batch operation.");

			var ctx = (QueryContext)query;

			ctx.Client = GetClient();

			var q = ctx.Query.SelectQuery.ProcessParameters(MappingSchema);

			return ctx.Client.ExecuteScalar(
				Configuration,
				LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters(), ctx.Query.QueryHints));
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			ThrowOnDisposed();

			if (_batchCounter > 0)
				throw new LinqException("Incompatible batch operation.");

			var ctx = (QueryContext)query;

			ctx.Client = GetClient();

			var q      = ctx.Query.SelectQuery.ProcessParameters(MappingSchema);
			var ret    = ctx.Client.ExecuteReader(
				Configuration,
				LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters(), ctx.Query.QueryHints));
			var result = LinqServiceSerializer.DeserializeResult(ret);

			return new ServiceModelDataReader(MappingSchema, result);
		}

		public void ReleaseQuery(object query)
		{
			ThrowOnDisposed();

			var ctx = (QueryContext)query;

			if (ctx.Client != null)
				((IDisposable)ctx.Client).Dispose();
		}

		string IDataContext.GetSqlText(object query)
		{
			ThrowOnDisposed();

			var ctx        = (QueryContext)query;
			var sqlBuilder = ((IDataContext)this).CreateSqlProvider();
			var sb         = new StringBuilder();

			sb
				.Append("-- ")
				.Append("ServiceModel")
				.Append(' ')
				.Append(((IDataContext)this).ContextID)
				.Append(' ')
				.Append(sqlBuilder.Name)
				.AppendLine();

			if (ctx.Query.SelectQuery.Parameters != null && ctx.Query.SelectQuery.Parameters.Count > 0)
			{
				foreach (var p in ctx.Query.SelectQuery.Parameters)
				{
					var value = p.Value;

					sb
						.Append("-- DECLARE ")
						.Append(p.Name)
						.Append(' ')
						.Append(value == null ? p.SystemType.ToString() : value.GetType().Name)
						.AppendLine();
				}

				sb.AppendLine();

				foreach (var p in ctx.Query.SelectQuery.Parameters)
				{
					var value = p.Value;

					if (value is string || value is char)
						value = "'" + value.ToString().Replace("'", "''") + "'";

					sb
						.Append("-- SET ")
						.Append(p.Name)
						.Append(" = ")
						.Append(value)
						.AppendLine();
				}

				sb.AppendLine();
			}

			var cc = sqlBuilder.CommandCount(ctx.Query.SelectQuery);

			for (var i = 0; i < cc; i++)
			{
				sqlBuilder.BuildSql(i, ctx.Query.SelectQuery, sb);

				if (i == 0 && ctx.Query.QueryHints != null && ctx.Query.QueryHints.Count > 0)
				{
					var sql = sb.ToString();

					sql = sqlBuilder.ApplyQueryHints(sql, ctx.Query.QueryHints);

					sb = new StringBuilder(sql);
				}
			}

			return sb.ToString();
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			ThrowOnDisposed();

			return Clone();
		}

		public event EventHandler OnClosing;

		protected bool Disposed { get; private set; }

		protected void ThrowOnDisposed()
		{
			if (Disposed)
				throw new ObjectDisposedException("RemoteDataContext", "IDataContext is disposed");
		}

		void IDataContext.Close()
		{
			Close();
		}

		private void Close()
		{
			if (OnClosing != null)
				OnClosing(this, EventArgs.Empty);
		}

		public void Dispose()
		{
			Disposed = true;

			Close();
		}
	}
}
