using System;
using System.Data.Common;
using System.Threading;

using LinqToDB.Internal.Expressions.Types;

namespace LinqToDB.Internal.DataProvider
{
	public sealed class LibRedProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly Lock _syncRoot = new();
		private static LibRedProviderAdapter? _instance;

		public const string AssemblyName    = "LibRed.Ado";
		public const string ClientNamespace = "LibRed.Data";

		const string CoreAssemblyName = "LibRed.Core";

		private LibRedProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType,
			Func<string, DbConnection> connectionFactory)
		{
			ConnectionType     = connectionType;
			DataReaderType     = dataReaderType;
			ParameterType      = parameterType;
			CommandType        = commandType;
			TransactionType    = transactionType;
			_connectionFactory = connectionFactory;

			// bound on first use: the statics belong to a prerelease surface, and a signature change there must not
			// fail every LibRed connection
			_databaseManagement = new(() => CreateDatabaseManagement(connectionType), LazyThreadSafetyMode.ExecutionAndPublication);
		}

#region IDynamicProviderAdapter

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		readonly Func<string, DbConnection> _connectionFactory;
		public DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);

#endregion

		readonly Lazy<DatabaseManagement> _databaseManagement;

		public Func  <string, bool>   DatabaseExists => _databaseManagement.Value.DatabaseExists;
		/// <summary>
		/// Creates a database file: connection string, and <see langword="true"/> for an <c>.accdb</c> (ACE 12) file,
		/// <see langword="false"/> for an <c>.mdb</c> (Jet 4) one.
		/// </summary>
		public Action<string, bool>   CreateDatabase => _databaseManagement.Value.CreateDatabase;
		public Action<string>         DropDatabase   => _databaseManagement.Value.DropDatabase;

		public static LibRedProviderAdapter GetInstance()
		{
			if (_instance == null)
			{
				lock (_syncRoot)
					_instance ??= CreateAdapter();
			}

			return _instance;

			static LibRedProviderAdapter CreateAdapter()
			{
				var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
				if (assembly == null)
					throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

				var connectionType  = assembly.GetType($"{ClientNamespace}.LibRedConnection" , true)!;
				var dataReaderType  = assembly.GetType($"{ClientNamespace}.LibRedDataReader" , true)!;
				var parameterType   = assembly.GetType($"{ClientNamespace}.LibRedParameter"  , true)!;
				var commandType     = assembly.GetType($"{ClientNamespace}.LibRedCommand"    , true)!;
				var transactionType = assembly.GetType($"{ClientNamespace}.LibRedTransaction", true)!;

				var typeMapper = new TypeMapper();
				typeMapper.RegisterTypeWrapper<LibRedConnection>(connectionType);
				typeMapper.FinalizeMappings();

				return new LibRedProviderAdapter(
					connectionType,
					dataReaderType,
					parameterType,
					commandType,
					transactionType,
					typeMapper.BuildTypedFactory<string, LibRedConnection, DbConnection>(connectionString => new LibRedConnection(connectionString)));
			}
		}

		static DatabaseManagement CreateDatabaseManagement(Type connectionType)
		{
			var coreAssembly = Common.Tools.TryLoadAssembly(CoreAssemblyName, null)
				?? throw new InvalidOperationException($"Cannot load assembly {CoreAssemblyName}");

			var collationType = coreAssembly.GetType("LibRed.Catalog.Collation", true)!;
			var versionType   = coreAssembly.GetType("LibRed.Formats.JetVersion", true)!;

			var typeMapper = new TypeMapper();
			typeMapper.RegisterTypeWrapper<LibRedConnection>(connectionType);
			typeMapper.RegisterTypeWrapper<Collation>(collationType);
			typeMapper.RegisterTypeWrapper<JetVersion>(versionType);
			typeMapper.FinalizeMappings();

			var createAce = typeMapper.BuildAction<string>(typeMapper.MapActionLambda((string cs) => LibRedConnection.CreateDatabase(cs, null, JetVersion.Version12_2007)));
			var createJet = typeMapper.BuildAction<string>(typeMapper.MapActionLambda((string cs) => LibRedConnection.CreateDatabase(cs, null, JetVersion.Version4)));

			return new DatabaseManagement(
				typeMapper.BuildFunc<string, bool>(typeMapper.MapLambda((string cs) => LibRedConnection.DatabaseExists(cs))),
				(cs, accdb) => (accdb ? createAce : createJet)(cs),
				typeMapper.BuildAction<string>(typeMapper.MapActionLambda((string cs) => LibRedConnection.DropDatabase(cs))));
		}

		sealed record DatabaseManagement(
			Func  <string, bool> DatabaseExists,
			Action<string, bool> CreateDatabase,
			Action<string>       DropDatabase);

		#region Wrappers

		[Wrapper]
		private sealed class LibRedConnection
		{
			public LibRedConnection(string connectionString) => throw new NotSupportedException();

			public static bool DatabaseExists(string connectionString)                                        => throw new NotSupportedException();
			public static void CreateDatabase(string connectionString, Collation? collation, JetVersion version) => throw new NotSupportedException();
			public static void DropDatabase  (string connectionString)                                        => throw new NotSupportedException();
		}

		// LibRed.Catalog.Collation, in LibRed.Core
		[Wrapper]
		private struct Collation;

		// LibRed.Formats.JetVersion, in LibRed.Core
		[Wrapper]
		private enum JetVersion
		{
			Version3       = 0,
			Version4       = 1,
			Version12_2007 = 2,
			Version14_2010 = 3,
			Version15_2013 = 4,
			Version16_2016 = 5,
			Version17_2019 = 6,
		}

		#endregion
	}
}
