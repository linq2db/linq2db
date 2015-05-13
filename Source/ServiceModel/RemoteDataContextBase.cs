﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using LinqToDB.SqlQuery;

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
						ms = (MappingSchema)Activator.CreateInstance(type);
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

		public  bool         InlineParameters { get; set; }

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

		#region GetQueryContext

		class QueryContext : IQueryContext
		{
			public QueryContext(RemoteDataContextBase dataContext, Query query, Expression expression)
			{
				_dataContext = dataContext;
				_query       = query;
				_expression  = expression;
			}

			readonly RemoteDataContextBase _dataContext;
			readonly Query                 _query;
			readonly Expression            _expression;

			ILinqService _client;

			public void Dispose()
			{
				if (_client != null)
					((IDisposable)_client).Dispose();
			}

			public int ExecuteNonQuery()
			{
				var q    = (SelectQuery)_query.SqlQuery.ProcessParameters();
				var data = LinqServiceSerializer.Serialize(
					q,
					new SqlParameter[0]//q.IsParameterDependent ? q.Parameters.ToArray() : _query.GetCommandInfo(_dataContext, _expression).Parameters
					);

				if (_dataContext._batchCounter > 0)
				{
					_dataContext._queryBatch.Add(data);
					return -1;
				}

				_client = _dataContext.GetClient();

				return _client.ExecuteNonQuery(_dataContext.Configuration, data);
			}

			public object ExecuteScalar()
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				_client = _dataContext.GetClient();

				var q = (SelectQuery)_query.SqlQuery.ProcessParameters();

				return _client.ExecuteScalar(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						new SqlParameter[0]//q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters()
						));
			}

			public IDataReader ExecuteReader()
			{
				if (_dataContext._batchCounter > 0)
					throw new LinqException("Incompatible batch operation.");

				_client = _dataContext.GetClient();

				var q      = (SelectQuery)_query.SqlQuery.ProcessParameters();
				var ret    = _client.ExecuteReader(
					_dataContext.Configuration,
					LinqServiceSerializer.Serialize(
						q,
						new SqlParameter[0]//q.IsParameterDependent ? q.Parameters.ToArray() : _c.Query.GetParameters()
						));
				var result = LinqServiceSerializer.DeserializeResult(ret);

				return new ServiceModelDataReader(_dataContext.MappingSchema, result);
			}

			public Task<DataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken)
			{
				throw new NotImplementedException();
			}
		}

		IQueryContext IDataContext.GetQueryContext(Query query, Expression expression)
		{
			return new QueryContext(this, query, expression);
		}

		#endregion

		class QueryContextOld
		{
			public IQueryContextOld Query;
			public ILinqService  Client;
		}

		object IDataContext.SetQuery(IQueryContextOld queryContext)
		{
			return new QueryContextOld { Query = queryContext };
		}

		int IDataContext.ExecuteNonQuery(object query)
		{
			var ctx  = (QueryContextOld)query;
			var q    = (SelectQuery)ctx.Query.SelectQuery.ProcessParameters();
			var data = LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters());

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
			if (_batchCounter > 0)
				throw new LinqException("Incompatible batch operation.");

			var ctx = (QueryContextOld)query;

			ctx.Client = GetClient();

			var q = (SelectQuery)ctx.Query.SelectQuery.ProcessParameters();

			return ctx.Client.ExecuteScalar(
				Configuration,
				LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters()));
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			if (_batchCounter > 0)
				throw new LinqException("Incompatible batch operation.");

			var ctx = (QueryContextOld)query;

			ctx.Client = GetClient();

			var q      = (SelectQuery)ctx.Query.SelectQuery.ProcessParameters();
			var ret    = ctx.Client.ExecuteReader(
				Configuration,
				LinqServiceSerializer.Serialize(q, q.IsParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters()));
			var result = LinqServiceSerializer.DeserializeResult(ret);

			return new ServiceModelDataReader(MappingSchema, result);
		}

		public void ReleaseQuery(object query)
		{
			var ctx = (QueryContextOld)query;

			if (ctx.Client != null)
				((IDisposable)ctx.Client).Dispose();
		}

		string IDataContext.GetSqlText(object query)
		{
			var ctx        = (QueryContextOld)query;
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
					sb
						.Append("-- DECLARE ")
						.Append(p.Name)
						.Append(' ')
						.Append(p.Value == null ? p.SystemType.ToString() : p.Value.GetType().Name)
						.AppendLine();

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
			}

			return sb.ToString();
		}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			return Clone();
		}

		public event EventHandler OnClosing;

		public void Dispose()
		{
			if (OnClosing != null)
				OnClosing(this, EventArgs.Empty);
		}
	}
}
