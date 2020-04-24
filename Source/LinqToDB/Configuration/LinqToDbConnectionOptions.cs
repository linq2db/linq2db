using System;
using System.Data;
using System.Diagnostics;

namespace LinqToDB.Configuration
{
	using Data;
	using DataProvider;
	using Mapping;

	public class LinqToDbConnectionOptions<T> : LinqToDbConnectionOptions
	{
		public override bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return connection is T;
		}

		public LinqToDbConnectionOptions(LinqToDbConnectionOptionsBuilder builder) : base(builder)
		{
		}
	}

	public class LinqToDbConnectionOptions
	{
		public LinqToDbConnectionOptions(LinqToDbConnectionOptionsBuilder builder)
		{
			SetupType = builder.SetupType;
			switch (SetupType)
			{
				case ConnectionSetupType.DefaultConfiguration:
				case ConnectionSetupType.ConnectionString:
					ConnectionString = builder.ConnectionString;
					ProviderName     = builder.ProviderName;
					break;
				case ConnectionSetupType.ConfigurationString:
					ConfigurationString = builder.ConfigurationString;
					break;
				case ConnectionSetupType.Connection:
					DbConnection      = builder.DbConnection;
					DisposeConnection = builder.DisposeConnection;
					break;
				case ConnectionSetupType.ConnectionFactory:
					ConnectionFactory = builder.ConnectionFactory;
					break;
				case ConnectionSetupType.Transaction:
					DbTransaction = builder.DbTransaction;
					break;
			}

			MappingSchema = builder.MappingSchema;
			DataProvider  = builder.DataProvider;
			OnTrace       = builder.OnTrace;
			TraceLevel    = builder.TraceLevel;
			WriteTrace    = builder.WriteTrace;
		}

		/// <summary>
		/// constructor for unit tests
		/// </summary>
		internal LinqToDbConnectionOptions()
		{
			SetupType = ConnectionSetupType.DefaultConfiguration;
		}

		public MappingSchema                      MappingSchema       { get; }
		public IDataProvider                      DataProvider        { get; }
		public IDbConnection                      DbConnection        { get; }
		public bool                               DisposeConnection   { get; }
		public string                             ConfigurationString { get; }
		public string                             ProviderName        { get; }
		public string                             ConnectionString    { get; }
		public Func<IDbConnection>                ConnectionFactory   { get; }
		public IDbTransaction                     DbTransaction       { get; }
		public Action<TraceInfo>?                 OnTrace             { get; }
		public TraceLevel?                        TraceLevel          { get; }
		public Action<string, string, TraceLevel> WriteTrace          { get; }

		internal ConnectionSetupType SetupType { get; }

		public virtual bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return true;
		}
	}
}
