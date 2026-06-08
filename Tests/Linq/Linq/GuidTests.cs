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
			[PrimaryKey] public Guid Id { get; set; }
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
				select Sql.AsSql(t.Id.ToString())).First();

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
			Assert.That(lC, Has.Count.Zero);

			var qryD = from t in table
				where t.Id.ToString().ToLower().StartsWith("8f4-53")
				select t;

			var lD = qryD.ToList();
			Assert.That(lD, Has.Count.Zero);
		}

		// Regression for #5528: a provider's Guid→string translator already lower-cases its result —
		// wrapped in a CAST on Firebird (Lower(Cast(Lower(...) as VarChar(36)))) or behind an
		// IIf-nullability guard on Access (LCase(«not-null»(LCase(...)))). A user-supplied .ToLower()
		// on top must collapse, not add a second redundant case wrap. Asserted inline (not only via
		// baselines) so the nested shape can't silently reappear in a future baseline refresh.
		[Test]
		public void GuidToStringNoRedundantCaseWrap([DataSources] string context)
		{
			// Matches Lower(Lower(...)), Lower(Cast(Lower(...))), LCase(LCase(...)), LCase(Cast(LCase(...))).
			const string nestedCasePattern = @"(?i)(lower|lcase)\s*\(\s*(cast\s*\(\s*)?(lower|lcase)\s*\(";

			TableWithGuid[] data = [new () { Id = TestData.Guid1 }];

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);

			_ = (from t in table select t.Id.ToString().ToLower()).ToList();
			Assert.That(LastQuery, Does.Not.Match(nestedCasePattern));

			_ = (from t in table where Sql.ConvertTo<string>.From(t.Id).ToLower() == "x" select t.Id).ToList();
			Assert.That(LastQuery, Does.Not.Match(nestedCasePattern));
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
