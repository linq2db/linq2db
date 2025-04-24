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
			[Column] public Guid? NullableGuid { get; set; }
		}

		[Test]
		public void GuidToString([DataSources] string context)
		{
			var expectedFirst = TestData.Guid1.ToString();

			TableWithGuid[] data = [new () { Id = TestData.Guid1, NullableGuid = null }];

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			
			var id = (from t in table
				select t.Id.ToString()).First();

			Assert.That(id, Is.EqualTo(expectedFirst));

			var subStr = expectedFirst.Substring(5, 6);

			var qryA = from t in table
				where t.Id.ToString().Contains(Sql.Constant(subStr))
				select t;

			var lA = qryA.ToList();
			Assert.That(lA, Has.Count.EqualTo(1));

			var startStr = expectedFirst.Substring(0, 5);

			var qryB = from t in table
				where t.Id.ToString().StartsWith(Sql.Constant(startStr))
				select t;

			var lB = qryB.ToList();
			Assert.That(lB, Has.Count.EqualTo(1));

			var qryC = from t in table
				where t.Id.ToString().Contains("8f4-53")
				select t;

			var lC = qryC.ToList();
			Assert.That(lC, Has.Count.EqualTo(0));

			var qryD = from t in table
				where t.Id.ToString().ToLower().StartsWith("8f4-53")
				select t;

			var lD = qryD.ToList();
			Assert.That(lD, Has.Count.EqualTo(0));
		}

		[Test]
		public void GuidToStringIsNull([DataSources] string context)
		{
			TableWithGuid[] data = [new ()
				{
					Id = TestData.Guid1
				},
				new ()
				{
					Id           = TestData.Guid2,
					NullableGuid = TestData.Guid2
				}];

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			_ = table.Single(x => x.NullableGuid.ToString() == null && x.Id == Sql.Parameter(TestData.Guid1));
		}

	}
}
