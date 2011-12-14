using System;

using LinqToDB.Data.Linq;
using LinqToDB.Data.DataProvider;
using LinqToDB.DataAccess;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Data.Linq.ProviderSpecific
{
	[TestFixture]
	public class PostgreSQL : TestBase
	{
		[TableName(Owner="public", Name="entity")]
		public class Entity
		{
			[MapField("the_name") ] public string TheName { get; set; }
		}
	}
}
