using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class GuidTests : TestBase
	{
		[Table]
		public class TableWithGuid
		{
			[Column] public Guid Id { get; set; }
		}

		[Test]
		public void GuidToString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TableWithGuid>())
			{
				var expected = "193AE7F4-5309-4EEE-A746-27B28C7E30F3".ToLowerInvariant();

				var a = new TableWithGuid() { Id = Guid.Parse(expected) };
				db.Insert(a);

				var id = (from t in db.GetTable<TableWithGuid>()
						   select t.Id.ToString()).First();

				Assert.That(id, Is.EqualTo(expected));

				var qryA = from t in db.GetTable<TableWithGuid>()
						   where t.Id.ToString().Contains("7f4-53")
						   select t;

				var lA = qryA.ToList();
				Assert.That(lA, Has.Count.EqualTo(1));

				var qryB = from t in db.GetTable<TableWithGuid>()
						   where t.Id.ToString().StartsWith("193ae")
						   select t;

				var lB = qryB.ToList();
				Assert.That(lB, Has.Count.EqualTo(1));

				var qryC = from t in db.GetTable<TableWithGuid>()
						   where t.Id.ToString().Contains("8f4-53")
						   select t;

				var lC = qryC.ToList();
				Assert.That(lC, Has.Count.EqualTo(0));

				var qryD = from t in db.GetTable<TableWithGuid>()
						   where t.Id.ToString().ToLower().StartsWith("293ae")
						   select t;

				var lD = qryD.ToList();
				Assert.That(lD, Has.Count.EqualTo(0));
			}
		}
	}
}
