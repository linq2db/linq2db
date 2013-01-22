using System;
using System.Data;

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
			ConfigurationString = configurationString;
			DataProvider        = DataConnection.GetDataProvider(configurationString);
			ContextID           = DataProvider.Name;

			MappingSchema = /*DataProvider.MappingSchema ??*/ Map.DefaultSchema; //////////// TODO
		}

		public string           ConfigurationString { get; private set; }
		public IDataProvider    DataProvider        { get; private set; }
		public string           ContextID           { get; set;         }
		public MappingSchemaOld MappingSchema       { get; set;         }
		public string           LastQuery           { get; set;         }

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

		string         _connectionString;
		DataConnection _dataConnection;

		internal DataConnection GetDBManager()
		{
			if (_dataConnection == null)
			{
				if (_connectionString == null)
					_connectionString = DataConnection.GetConnectionString(ConfigurationString);

				_dataConnection = new DataConnection(DataProvider, _connectionString) { MappingSchemaOld = MappingSchema };
			}

			return _dataConnection;
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

		Func<ISqlProvider> IDataContext.CreateSqlProvider
		{
			get { return DataProvider.CreateSqlProvider; }
		}

		object IDataContext.SetQuery(IQueryContext queryContext)
		{
			var ctx = GetDBManager() as IDataContext;
			return ctx.SetQuery(queryContext);
		}

		int IDataContext.ExecuteNonQuery(object query)
		{
			var ctx = GetDBManager() as IDataContext;
			return ctx.ExecuteNonQuery(query);
		}

		object IDataContext.ExecuteScalar(object query)
		{
			var ctx = GetDBManager() as IDataContext;
			return ctx.ExecuteScalar(query);
		}

		IDataReader IDataContext.ExecuteReader(object query)
		{
			var ctx = GetDBManager() as IDataContext;
			return ctx.ExecuteReader(query);
		}

		void IDataContext.ReleaseQuery(object query)
		{
			ReleaseQuery();
		}

		string IDataContext.GetSqlText(object query)
		{
			if (_dataConnection != null)
				return ((IDataContext)_dataConnection).GetSqlText(query);

			var ctx = GetDBManager() as IDataContext;
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
				KeepConnectionAlive = KeepConnectionAlive,
				DataProvider        = DataProvider,
				ContextID           = ContextID,
				MappingSchema       = MappingSchema,
			};

			if (forNestedQuery && _dataConnection != null && _dataConnection.IsMarsEnabled)
				dc._dataConnection = _dataConnection.Transaction != null ?
					new DataConnection(DataProvider, _dataConnection.Transaction) { MappingSchemaOld = MappingSchema } :
					new DataConnection(DataProvider, _dataConnection.Connection)  { MappingSchemaOld = MappingSchema };

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
	}
}
