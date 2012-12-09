using System;
using System.Data;
using System.Text;

namespace LinqToDB
{
	using Data;
	using DataProvider;
	using Linq;
	using Mapping;
	using SqlProvider;

	public class DataContext : IDataContext
	{
		public DataContext() : this(DbManager.DefaultConfiguration)
		{
		}

		public DataContext(string configurationString)
		{
			ConfigurationString = configurationString;
			DataProvider        = DbManager.GetDataProvider(configurationString);
			ContextID           = DataProvider.Name;

			MappingSchema = DataProvider.MappingSchema ?? Map.DefaultSchema;
		}

		public string              ConfigurationString { get; private set; }
		public DataProviderBaseOld DataProvider        { get; private set; }
		public string              ContextID           { get; set;         }
		public MappingSchemaOld    MappingSchema       { get; set;         }
		public string              LastQuery           { get; set;         }

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
					if (_dbManager == null)
						return false;
					_isMarsEnabled = _dbManager.IsMarsEnabled;
				}

				return _isMarsEnabled.Value;
			}
			set { _isMarsEnabled = value; }
		}

		internal int LockDbManagerCounter;

		string    _connectionString;
		DbManager _dbManager;

		internal DbManager GetDBManager()
		{
			if (_dbManager == null)
			{
				if (_connectionString == null)
					_connectionString = DbManager.GetConnectionString(ConfigurationString);

				_dbManager = new DbManager(DataProvider, _connectionString) { MappingSchema = MappingSchema };
			}

			return _dbManager;
		}

		internal void ReleaseQuery()
		{
			LastQuery = _dbManager.LastQuery;

			if (_dbManager != null && LockDbManagerCounter == 0 && KeepConnectionAlive == false)
			{
				_dbManager.Dispose();
				_dbManager = null;
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
			if (_dbManager != null)
				return ((IDataContext)_dbManager).GetSqlText(query);

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

			if (forNestedQuery && _dbManager != null && _dbManager.IsMarsEnabled)
				dc._dbManager = _dbManager.Transaction != null ?
					new DbManager(DataProvider, _dbManager.Transaction) { MappingSchema = MappingSchema } :
					new DbManager(DataProvider, _dbManager.Connection)  { MappingSchema = MappingSchema };

			return dc;
		}

		public event EventHandler OnClosing;

		void IDisposable.Dispose()
		{
			if (_dbManager != null)
			{
				if (OnClosing != null)
					OnClosing(this, EventArgs.Empty);

				_dbManager.Dispose();
				_dbManager = null;
			}
		}
	}
}
