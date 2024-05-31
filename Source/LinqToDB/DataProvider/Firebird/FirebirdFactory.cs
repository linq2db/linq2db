using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.Firebird
{
	using Configuration;

	[UsedImplicitly]
	sealed class FirebirdFactory : IDataProviderFactory
	{
		IDataProvider IDataProviderFactory.GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			string? versionName = null;

			foreach (var attr in attributes)
			{
				if (attr.Name == "version" && versionName == null)
					versionName = attr.Value;
			}

			var version = versionName switch
			{
				"2.5" => FirebirdVersion.v25,
				"3"   => FirebirdVersion.v3,
				"4"   => FirebirdVersion.v4,
				"5"   => FirebirdVersion.v5,
				_     => FirebirdVersion.AutoDetect,
			};

			return FirebirdTools.GetDataProvider(version);
		}
	}
}
