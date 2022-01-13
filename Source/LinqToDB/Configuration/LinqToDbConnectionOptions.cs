using System;
using System.Diagnostics;

namespace LinqToDB.Configuration
{
	using System.Collections.Generic;
	using System.Data.Common;
	using Data;
	using DataProvider;
	using LinqToDB.Interceptors;
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
			Interceptors  = builder.Interceptors;
		}

		/// <summary>
		/// constructor for unit tests
		/// </summary>
		internal LinqToDbConnectionOptions()
		{
			SetupType = ConnectionSetupType.DefaultConfiguration;
		}

		/// <summary>
		/// Gets <see cref="MappingSchema"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public MappingSchema?                        MappingSchema       { get; }
		/// <summary>
		/// Gets <see cref="IDataProvider"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public IDataProvider?                        DataProvider        { get; }
		/// <summary>
		/// Gets <see cref="System.Data.Common.DbConnection"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public DbConnection?                         DbConnection        { get; }
		/// <summary>
		/// Gets <see cref="DbConnection"/> ownership status for <see cref="DataConnection"/> instance.
		/// If <c>true</c>, <see cref="DataConnection"/> will dispose provided connection on own dispose.
		/// </summary>
		public bool                                  DisposeConnection   { get; }
		/// <summary>
		/// Gets configuration string name to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public string?                               ConfigurationString { get; }
		/// <summary>
		/// Gets provider name to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public string?                               ProviderName        { get; }
		/// <summary>
		/// Gets connection string to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public string?                               ConnectionString    { get; }
		/// <summary>
		/// Gets connection factory to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public Func<DbConnection>?                   ConnectionFactory   { get; }
		/// <summary>
		/// Gets <see cref="DbTransaction"/> instance to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public DbTransaction?                        DbTransaction       { get; }
		/// <summary>
		/// Gets custom trace method to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public Action<TraceInfo>?                    OnTrace             { get; }
		/// <summary>
		/// Gets custom trace level to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public TraceLevel?                           TraceLevel          { get; }
		/// <summary>
		/// Gets custom trace writer to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public Action<string?, string?, TraceLevel>? WriteTrace          { get; }
		/// <summary>
		/// Gets list of interceptors to use with <see cref="DataConnection"/> instance.
		/// </summary>
		public IReadOnlyList<IInterceptor>?          Interceptors        { get; }

		internal ConnectionSetupType SetupType { get; }

		public virtual bool IsValidConfigForConnectionType(DataConnection connection)
		{
			return true;
		}
	}
}
