using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue999Tests : TestBase
	{
		public class Address
		{
			public string City     { get; set; }
			public string Street   { get; set; }
			public int    Building { get; set; }
		}

		[Column("city", "Residence.City")]
		[Column("user_name", "Name")]
		public class User
		{
			public string Name;

			[Column("street", ".Street")]
			[Column("building_number", MemberName = nameof(Address.Building))]
			public Address Residence { get; set; }
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var ed = db.MappingSchema.GetEntityDescriptor(typeof(User));

				Assert.That(ed.Columns.Count, Is.EqualTo(4));

				var users = db.GetTable<User>();
				//var query = users.Select(u => u.Name);
				var query = users.Select(u => u.Residence.City);
				var sql   = query.GetSelectQuery();

				Console.WriteLine(((dynamic)query).SqlText);

				Assert.AreEqual(1, sql.Select.Columns.Count);
			}
		}
	}
}
