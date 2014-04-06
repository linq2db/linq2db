using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Oracle
{
	[UsedImplicitly]
	class OracleFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			for (var i = 0; i < attributes.Count; i++)
				if (attributes.GetKey(i) == "assemblyName")
					OracleTools.AssemblyName = attributes.Get(i);

			return new OracleDataProvider();
		}
	}
}
