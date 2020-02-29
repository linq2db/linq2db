using System;

namespace LinqToDB.DataProvider
{
	public class OdbcProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static OdbcProviderAdapter? _instance;

		public const string AssemblyName    = "System.Data.Odbc";
		public const string ClientNamespace = "System.Data.Odbc";

		private OdbcProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType)
		{
			ConnectionType  = connectionType;
			DataReaderType  = dataReaderType;
			ParameterType   = parameterType;
			CommandType     = commandType;
			TransactionType = transactionType;
		}

		public Type ConnectionType  { get; }
		public Type DataReaderType  { get; }
		public Type ParameterType   { get; }
		public Type CommandType     { get; }
		public Type TransactionType { get; }

		public static OdbcProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
#if NET45 || NET46
						var assembly = typeof(System.Data.Odbc.OdbcConnection).Assembly;
#else
						var assembly = LinqToDB.Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");
#endif

						var connectionType  = assembly.GetType($"{ClientNamespace}.OdbcConnection" , true);
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.OdbcDataReader" , true);
						var parameterType   = assembly.GetType($"{ClientNamespace}.OdbcParameter"  , true);
						var commandType     = assembly.GetType($"{ClientNamespace}.OdbcCommand"    , true);
						var transactionType = assembly.GetType($"{ClientNamespace}.OdbcTransaction", true);

						_instance = new OdbcProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType);
					}

			return _instance;
		}
	}
}
