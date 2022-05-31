using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Oracle
{
	using System.Collections.Generic;
	using System.Linq;
	using Configuration;

	[UsedImplicitly]
	class OracleFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var version      = attributes.FirstOrDefault(_ => _.Name == "version"     )?.Value;
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName")?.Value;

			var dialect = OracleVersion.v12;
			if (version?.Contains("11") == true)
				dialect = OracleVersion.v11;

			var provider = OracleProvider.Managed;
			if (assemblyName == OracleProviderAdapter.DevartAssemblyName)
				provider = OracleProvider.Devart;
#if NETFRAMEWORK
			else if (assemblyName == OracleProviderAdapter.NativeAssemblyName)
				provider = OracleProvider.Native;
#endif

			return OracleTools.GetDataProvider(dialect, provider);
		}
	}
}
