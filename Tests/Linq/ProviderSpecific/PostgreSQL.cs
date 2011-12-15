using System;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.ProviderSpecific
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
