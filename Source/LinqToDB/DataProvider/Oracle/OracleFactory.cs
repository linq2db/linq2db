using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Oracle
{
	using Configuration;

	[UsedImplicitly]
	sealed class OracleFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			string? version      = null;
			string? assemblyName = null;

			foreach (var attr in attributes)
			{
				if (attr.Name == "version" && version == null)
					version = attr.Value;
				else if (attr.Name == "assemblyName" && assemblyName == null)
					assemblyName = attr.Value;
			}

			var dialect = OracleVersion.v12;
			if (version?.Contains("11") == true)
				dialect = OracleVersion.v11;
			else if (version?.Contains("23") == true)
				dialect = OracleVersion.v23;

			var provider = OracleProvider.Managed;
			if (assemblyName == OracleProviderAdapter.DevartAssemblyName)
				provider = OracleProvider.Devart;
			else if (assemblyName == OracleProviderAdapter.NativeAssemblyName)
				provider = OracleProvider.Native;

			return OracleTools.GetDataProvider(dialect, provider);
		}
	}
}
