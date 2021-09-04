using System;

namespace LinqToDB.DataProvider.NitrosBase
{
	using LinqToDB.Expressions;

	public class NitrosBaseProviderAdapter : IDynamicProviderAdapter
	{
		private static readonly object _syncRoot = new ();
		private static NitrosBaseProviderAdapter? _instance;

		// TODO: specify names, used in provider
		public const string AssemblyName        = "TODO";
		public const string ClientNamespace     = "TODO";
		public const string ProviderFactoryName = "TODO";

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
						var assembly = Common.Tools.TryLoadAssembly(AssemblyName, ProviderFactoryName);
						if (assembly == null)
							throw new InvalidOperationException($"Cannot load assembly {AssemblyName}");

						// TODO: specify real class names for provider
						var connectionType  = assembly.GetType($"{ClientNamespace}.TODOConnection" , true)!;
						var dataReaderType  = assembly.GetType($"{ClientNamespace}.TODODataReader" , true)!;
						var parameterType   = assembly.GetType($"{ClientNamespace}.TODOParameter"  , true)!;
						var commandType     = assembly.GetType($"{ClientNamespace}.TODOCommand"    , true)!;
						var transactionType = assembly.GetType($"{ClientNamespace}.TODOTransaction", true)!;

						var typeMapper = new TypeMapper();
						// TODO: register type wrappers for additional non-ado.net functionality, used by linq2db
						typeMapper.FinalizeMappings();

						_instance = new NitrosBaseProviderAdapter(
							connectionType,
							dataReaderType,
							parameterType,
							commandType,
							transactionType);
					}

			return _instance;
		}

		#region Wrappers
		// TODO: add wrapper classes for additional functionality, used by linq2db
		#endregion
	}
}
