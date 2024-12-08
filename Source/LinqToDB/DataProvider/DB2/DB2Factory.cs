using System.Collections.Generic;

using JetBrains.Annotations;

namespace LinqToDB.DataProvider.DB2
{
	using Configuration;

	[UsedImplicitly]
	sealed class DB2Factory : DataProviderFactoryBase
	{
		public override IDataProvider GetDataProvider(IEnumerable<NamedValue> attributes)
		{
			var version = GetVersion(attributes) switch
			{
				"zOS" or "z/OS" => DB2Version.zOS,
				"LUW"           => DB2Version.LUW,
				_               => DB2Version.AutoDetect,
			};

			return DB2Tools.GetDataProvider(version);
		}
	}
}
