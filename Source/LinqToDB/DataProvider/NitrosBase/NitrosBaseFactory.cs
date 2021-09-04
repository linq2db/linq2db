using JetBrains.Annotations;
using System.Collections.Generic;

namespace LinqToDB.DataProvider.NitrosBase
{
	using Configuration;

	// provider factory is a rarely used way to specify provider in connection settings using
	// IDataProviderSettings.TypeName property to contain factory type name
	[UsedImplicitly]
	class NitrosFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			// as current provider is not multi-driver/versioned provider and doesn't accept additional
			// connection configuration attributes we always return specific instance from factory
			return NitrosBaseTools.GetDataProvider();
		}
	}
}
