using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.ServiceModel
{
	using Data.Linq;
	using Data.Sql.SqlProvider;
	using Mapping;

	public abstract class RemoteDataContextBase : IDataContext
	{
		protected abstract ILinqService GetClient();
		protected abstract IDataContext Clone    ();
		protected abstract string       ContextIDPrefix { get; }

		string             _contextID;
		string IDataContext.ContextID
		{
			get { return _contextID ?? (_contextID = ContextIDPrefix + SqlProviderType.Name.Replace("SqlProvider", "")); }
		}

		private MappingSchema _mappingSchema;
		public  MappingSchema  MappingSchema
		{
			get
			{
				if (_mappingSchema == null)
				{
					var sp = ((IDataContext)this).CreateSqlProvider();
					_mappingSchema = sp is IMappingSchemaProvider ? ((IMappingSchemaProvider)sp).MappingSchema : Map.DefaultSchema;
				}

				return _mappingSchema;
			}

			set { _mappingSchema = value; }
		}

		private        Type _sqlProviderType;
		public virtual Type  SqlProviderType
		{
			get
			{
				if (_sqlProviderType == null)
				{
					var client = GetClient();

					try
					{
						var type = client.GetSqlProviderType();
						_sqlProviderType = Type.GetType(type);
					}
					finally
					{
						((IDisposable)client).Dispose();
					}
				}

				return _sqlProviderType;
			}

			set { _sqlProviderType = value;  }
		}

		static readonly Dictionary<Type,Func<ISqlProvider>> _sqlProviders = new Dictionary<Type, Func<ISqlProvider>>();

		Func<ISqlProvider> _createSqlProvider;

		Func<ISqlProvider> IDataContext.CreateSqlProvider
		{
			get
			{
				if (_createSqlProvider == null)
				{
					var type = SqlProviderType;

					if (!_sqlProviders.TryGetValue(type, out _createSqlProvider))
						lock (_sqlProviderType)
							if (!_sqlProviders.TryGetValue(type, out _createSqlProvider))
								_sqlProviders.Add(type, _createSqlProvider = Expression.Lambda<Func<ISqlProvider>>(Expression.New(type)).Compile());
				}

				return _createSqlProvider;
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
					client.ExecuteBatch(data);
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
			return new QueryContext { Query = queryContext };
		}

		int IDataContext.ExecuteNonQuery(object query)
		{
			var ctx  = (QueryContext)query;
			var q    = ctx.Query.SqlQuery.ProcessParameters();
			var data = LinqServiceSerializer.Serialize(q, q.ParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters());

			if (_batchCounter > 0)
			{
				_queryBatch.Add(data);
				return -1;
			}

			ctx.Client = GetClient();

			return ctx.Client.ExecuteNonQuery(data);
		}

		object IDataContext.ExecuteScalar(object query)
		{
			if (_batchCounter > 0)
				throw new LinqException("Incompatible batch operation.");

			var ctx = (QueryContext)query;

			ctx.Client = GetClient();

			var q = ctx.Query.SqlQuery.ProcessParameters();

			return ctx.Client.ExecuteScalar(
				LinqServiceSerializer.Serialize(q, q.ParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters()));
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			if (_batchCounter > 0)
				throw new LinqException("Incompatible batch operation.");

			var ctx = (QueryContext)query;

			ctx.Client = GetClient();

			var q      = ctx.Query.SqlQuery.ProcessParameters();
			var ret    = ctx.Client.ExecuteReader(
				LinqServiceSerializer.Serialize(q, q.ParameterDependent ? q.Parameters.ToArray() : ctx.Query.GetParameters()));
			var result = LinqServiceSerializer.DeserializeResult(ret);

			return new ServiceModelDataReader(result);
		}

		public void ReleaseQuery(object query)
		{
			var ctx = (QueryContext)query;

			if (ctx.Client != null)
				((IDisposable)ctx.Client).Dispose();
		}

		string IDataContext.GetSqlText(object query)
		{
			var ctx         = (QueryContext)query;
			var sqlProvider = ((IDataContext)this).CreateSqlProvider();
			var sb          = new StringBuilder();

			sb
				.Append("-- ")
				.Append("ServiceModel")
				.Append(' ')
				.Append(((IDataContext)this).ContextID)
				.Append(' ')
				.Append(sqlProvider.Name)
				.AppendLine();

			if (ctx.Query.SqlQuery.Parameters != null && ctx.Query.SqlQuery.Parameters.Count > 0)
			{
				foreach (var p in ctx.Query.SqlQuery.Parameters)
					sb
						.Append("-- DECLARE ")
						.Append(p.Name)
						.Append(' ')
						.Append(p.Value == null ? p.SystemType.ToString() : p.Value.GetType().Name)
						.AppendLine();

				sb.AppendLine();

				foreach (var p in ctx.Query.SqlQuery.Parameters)
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

			var cc       = sqlProvider.CommandCount(ctx.Query.SqlQuery);
			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				sqlProvider.BuildSql(i, ctx.Query.SqlQuery, sb, 0, 0, false);
				commands[i] = sb.ToString();
			}

			if (!ctx.Query.SqlQuery.ParameterDependent)
				ctx.Query.Context = commands;

			foreach (var command in commands)
				sb.AppendLine(command);

			return sb.ToString();
		}

		IDataContext IDataContext.Clone()
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
