using System;
using JetBrains.Annotations;

namespace LinqToDB.DataProvider.SapHana
{
	using System.Collections.Generic;
	using System.Linq;
	using Configuration;

	[UsedImplicitly]
	class SapHanaFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var assemblyName = attributes.FirstOrDefault(_ => _.Name == "assemblyName");
#if !NETSTANDARD2_0
			if (assemblyName != null)
				SapHanaTools.AssemblyName = assemblyName.Value;

			return new SapHanaDataProvider();
#else
			throw new PlatformNotSupportedException();
#endif
		}
	}
}
