using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Data;
	using DataProvider;
	using Linq;
	using Mapping;
	using SqlProvider;

	public class DataContext : IDataContext
	{
		public DataContext() : this(DataConnection.DefaultConfiguration)
		{
		}

		public DataContext(string configurationString)
		{
			DataProvider        = DataConnection.GetDataProvider(configurationString);
			ConfigurationString = configurationString ?? DataConnection.DefaultConfiguration;
			ContextID           = DataProvider.Name;
			MappingSchema       = DataProvider.MappingSchema;
		}

		public DataContext([JetBrains.Annotations.NotNull] IDataProvider dataProvider, [JetBrains.Annotations.NotNull] string connectionString)
		{
			if (dataProvider     == null) throw new ArgumentNullException("dataProvider");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			DataProvider     = dataProvider;
			ConnectionString = connectionString;
			ContextID        = DataProvider.Name;
			MappingSchema    = DataProvider.MappingSchema;
		}

		public string        ConfigurationString { get; private set; }
		public string        ConnectionString    { get; private set; }
		public IDataProvider DataProvider        { get; private set; }
		public string        ContextID           { get; set;         }
		public MappingSchema MappingSchema       { get; set;         }
		public bool          InlineParameters    { get; set;         }
		public string        LastQuery           { get; set;         }

		private bool _keepConnectionAlive;
		public  bool  KeepConnectionAlive
		{
			get { return _keepConnectionAlive; }
			set
			{
				_keepConnectionAlive = value;

				if (value == false)
					ReleaseQuery();
			}
		}

		private bool? _isMarsEnabled;
		public  bool   IsMarsEnabled
		{
			get
			{
				if (_isMarsEnabled == null)
				{
					if (_dataConnection == null)
						return false;
					_isMarsEnabled = _dataConnection.IsMarsEnabled;
				}

				return _isMarsEnabled.Value;
			}
			set { _isMarsEnabled = value; }
		}

		internal int LockDbManagerCounter;

		DataConnection _dataConnection;

		internal DataConnection GetDataConnection()
		{
			return _dataConnection ??
			(
				_dataConnection = ConnectionString != null
					? new DataConnection(DataProvider, ConnectionString)
					: new DataConnection(ConfigurationString)
			);
		}

		internal void ReleaseQuery()
		{
			LastQuery = _dataConnection.LastQuery;

			if (_dataConnection != null && LockDbManagerCounter == 0 && KeepConnectionAlive == false)
			{
				_dataConnection.Dispose();
				_dataConnection = null;
			}
		}

		Func<ISqlBuilder> IDataContext.CreateSqlProvider
		{
			get { return DataProvider.CreateSqlBuilder; }
		}

		Func<ISqlOptimizer> IDataContext.GetSqlOptimizer
		{
			get { return DataProvider.GetSqlOptimizer; }
		}

		Type IDataContext.DataReaderType
		{
			get { return DataProvider.DataReaderType; }
		}

		Expression IDataContext.GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			return DataProvider.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(IDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(reader, idx);
		}

		#region GetQueryContext

		class QueryContext : IQueryContext
		{
			readonly DataContext   _dataContext;
			readonly IQueryContext _queryContext;

			public QueryContext(Query query, DataContext dataContext)
			{
				_dataContext  = dataContext;
				_queryContext = ((IDataContext)dataContext.GetDataConnection()).GetQueryContext(query);
			}

			public void        Dispose        () { _dataContext.ReleaseQuery();            }
			public int         ExecuteNonQuery() { return _queryContext.ExecuteNonQuery(); }
			public object      ExecuteScalar  () { return _queryContext.ExecuteScalar  (); }
			public IDataReader ExecuteReader  () { return _queryContext.ExecuteReader  (); }
		}

		IQueryContext IDataContext.GetQueryContext(Query query)
		{
			return new QueryContext(query, this);
		}

		#endregion

		object IDataContext.SetQuery(IQueryContextOld queryContext)
		{
			var ctx = GetDataConnection() as IDataContext;
			return ctx.SetQuery(queryContext);
		}

		int IDataContext.ExecuteNonQuery(object query)
		{
			var ctx = GetDataConnection() as IDataContext;
			return ctx.ExecuteNonQuery(query);
		}

		object IDataContext.ExecuteScalar(object query)
		{
			var ctx = GetDataConnection() as IDataContext;
			return ctx.ExecuteScalar(query);
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			var ctx = GetDataConnection() as IDataContext;
			return ctx.ExecuteReader(query);
		}

		void IDataContext.ReleaseQuery(object query)
		{
			ReleaseQuery();
		}

		SqlProviderFlags IDataContext.SqlProviderFlags
		{
			get { return DataProvider.SqlProviderFlags; }
		}

		string IDataContext.GetSqlText(object query)
		{
			if (_dataConnection != null)
				return ((IDataContext)_dataConnection).GetSqlText(query);

			var ctx = GetDataConnection() as IDataContext;
			var str = ctx.GetSqlText(query);

			ReleaseQuery();

			return str;
		}

		DataContext(int n) {}

		IDataContext IDataContext.Clone(bool forNestedQuery)
		{
			var dc = new DataContext(0)
			{
				ConfigurationString = ConfigurationString,
				ConnectionString    = ConnectionString,
				KeepConnectionAlive = KeepConnectionAlive,
				DataProvider        = DataProvider,
				ContextID           = ContextID,
				MappingSchema       = MappingSchema,
			};

			if (forNestedQuery && _dataConnection != null && _dataConnection.IsMarsEnabled)
				dc._dataConnection = _dataConnection.Transaction != null ?
					new DataConnection(DataProvider, _dataConnection.Transaction) :
					new DataConnection(DataProvider, _dataConnection.Connection);

			return dc;
		}

		public event EventHandler OnClosing;

		void IDisposable.Dispose()
		{
			if (_dataConnection != null)
			{
				if (OnClosing != null)
					OnClosing(this, EventArgs.Empty);

				_dataConnection.Dispose();
				_dataConnection = null;
			}
		}

		public virtual DataContextTransaction BeginTransaction(IsolationLevel level)
		{
			var dct = new DataContextTransaction(this);

			dct.BeginTransaction(level);

			return dct;
		}

		public virtual DataContextTransaction BeginTransaction(bool autoCommitOnDispose = true)
		{
			var dct = new DataContextTransaction(this);

			dct.BeginTransaction();

			return dct;
		}
	}
}
