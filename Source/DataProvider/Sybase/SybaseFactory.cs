using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Sybase
{
	[UsedImplicitly]
	class SybaseFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			for (var i = 0; i < attributes.Count; i++)
				if (attributes.GetKey(i) == "assemblyName")
					SybaseTools.AssemblyName = attributes.Get(i);

			return new SybaseDataProvider();
		}
	}
}
