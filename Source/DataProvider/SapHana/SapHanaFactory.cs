using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	[UsedImplicitly]
	class SapHanaFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(NameValueCollection attributes)
		{
			for (var i = 0; i < attributes.Count; i++)
				if (attributes.GetKey(i) == "assemblyName")
					SapHanaTools.AssemblyName = attributes.Get(i);

			return new SapHanaDataProvider();
		}
	}
}
