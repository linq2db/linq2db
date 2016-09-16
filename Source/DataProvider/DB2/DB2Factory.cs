using System;
using System.Collections.Specialized;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.DB2
{
	using System.Collections.Generic;
	using System.Linq;
	using Configuration;

	[UsedImplicitly]
	class DB2Factory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var version = attributes.FirstOrDefault(_ => _.Name == "version");
			if (version != null)
			{
				switch (version.Value)
				{
					case "zOS" :
					case "z/OS": return new DB2DataProvider(ProviderName.DB2zOS, DB2Version.zOS);
				}
			}

			return new DB2DataProvider(ProviderName.DB2LUW, DB2Version.LUW);
		}
	}
}
