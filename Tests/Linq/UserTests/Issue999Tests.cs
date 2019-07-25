using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
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

		[Column("city", "Residence.Street")]
		[Column("user_name", "Name")]
		public class User
		{
		    public string Name;

		    [Column("street", ".Street")]
		    [Column("building_number", MemberName = ".Building")]
		    public Address Residence { get; set; }
		}

		[ActiveIssue(999)]
		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var users = db.CreateLocalTable<User>())
			{
				var query = users.Select(u => u.Residence.City);
				Assert.AreEqual(1, query.GetSelectQuery().Select.Columns.Count);
			}
		}
	}
}
