using System;
using System.Data;
using System.Text;

namespace LinqToDB
{
	using Data;
	using Data.Linq;
	using Data.DataProvider;
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

		public string           ConfigurationString { get; private set; }
		public DataProviderBase DataProvider        { get; private set; }
		public string           ContextID           { get; set;         }
		public MappingSchema    MappingSchema       { get; set;         }
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
			var q = (IQueryContext)query;

			var sqlProvider = DataProvider.CreateSqlProvider();

			var sb = new StringBuilder();

			sb.Append("-- ").Append(ConfigurationString);

			if (ConfigurationString != DataProvider.Name)
				sb.Append(' ').Append(DataProvider.Name);

			if (DataProvider.Name != sqlProvider.Name)
				sb.Append(' ').Append(sqlProvider.Name);

			sb.AppendLine();

			if (q.SqlQuery.Parameters != null && q.SqlQuery.Parameters.Count > 0)
			{
				foreach (var p in q.SqlQuery.Parameters)
					sb
						.Append("-- DECLARE ")
						.Append(p.Name)
						.Append(' ')
						.Append(p.Value == null ? p.SystemType.ToString() : p.Value.GetType().Name)
						.AppendLine();

				sb.AppendLine();

				foreach (var p in q.SqlQuery.Parameters)
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

			var cc       = sqlProvider.CommandCount(q.SqlQuery);
			var commands = new string[cc];

			for (var i = 0; i < cc; i++)
			{
				sb.Length = 0;

				sqlProvider.BuildSql(i, q.SqlQuery, sb, 0, 0, false);
				commands[i] = sb.ToString();
			}

			if (!q.SqlQuery.ParameterDependent)
				q.Context = commands;

			foreach (var command in commands)
				sb.AppendLine(command);

			return sb.ToString();
		}

		DataContext(int n) {}

		IDataContext IDataContext.Clone()
		{
			return new DataContext(0)
			{
				ConfigurationString = ConfigurationString,
				KeepConnectionAlive = KeepConnectionAlive,
				DataProvider        = DataProvider,
				ContextID           = ContextID,
				MappingSchema       = MappingSchema,
			};
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
