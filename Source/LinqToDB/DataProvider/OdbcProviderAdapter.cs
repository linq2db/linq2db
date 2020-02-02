using System;

namespace LinqToDB.DataProvider
{
	public class OdbcProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new object();
		private static OdbcProviderAdapter? _instance;

		public static readonly string AssemblyName = "System.Data.Odbc";

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
			{
				lock (_syncRoot)
				{
					if (_instance == null)
					{
#if NET45 || NET46
						var connectionType = typeof(System.Data.Odbc.OdbcConnection);
#else
						var connectionType = Type.GetType($"System.Data.Odbc.OdbcConnection, {AssemblyName}", true);
#endif

						var dataReaderType  = connectionType.Assembly.GetType("System.Data.Odbc.OdbcDataReader", true);
						var parameterType   = connectionType.Assembly.GetType("System.Data.Odbc.OdbcParameter", true);
						var commandType     = connectionType.Assembly.GetType("System.Data.Odbc.OdbcCommand", true);
						var transactionType = connectionType.Assembly.GetType("System.Data.Odbc.OdbcTransaction", true);

						_instance = new OdbcProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType);
					}
				}
			}

			return _instance;
		}
	}
}
