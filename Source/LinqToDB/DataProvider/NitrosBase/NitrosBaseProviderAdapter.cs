using System;

namespace LinqToDB.DataProvider.NitrosBase
{
	public class NitrosBaseProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new ();
		private static NitrosBaseProviderAdapter? _instance;

		public const string AssemblyName    = "Nitros.Net";
		public const string ClientNamespace = "NitrosData.Nitros.Net";

		private NitrosBaseProviderAdapter(
			Type connectionType,
			Type dataReaderType,
			Type parameterType,
			Type commandType,
			Type transactionType)
		{
			// those five types are minimal required set of data, that should be provided by adapter
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

		public static NitrosBaseProviderAdapter GetInstance()
		{
			if (_instance == null)
				lock (_syncRoot)
					if (_instance == null)
					{
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, null);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						var connectionType  = assembly.GetType($"{ClientNamespace}.NitrosBaseConnection"   , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.NitrosBaseDataReader"   , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.NitrosBaseDataParameter", true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.NitrosBaseCommand"      , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.NitrosBaseTransaction"  , true)!;

						_instance = new NitrosBaseProviderAdapter(
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
