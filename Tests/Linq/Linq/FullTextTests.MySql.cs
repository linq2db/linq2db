using LinqToDB;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Linq;

namespace Tests.Linq
{
	[TestFixture]
	[Category(TestCategory.FTS)]
	public partial class FullTextTests : TestBase
	{
		[Table("FullTextIndexTest")]
		public class FullTextIndexTest
		{
			[PrimaryKey, Column("id")]
			public int Id { get; set; }

			[Column("TestField1")]
			public string? TestField1 { get; set; }

			[Column("TestField2")]
			public string? TestField2 { get; set; }
		}

		#region MATCH
		[Test]
		public void MatchPredicate([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.Where(r => Sql.Ext.MySql().Match("found", r.TestField1, r.TestField2))
					.OrderBy(r => r.Id);

				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(results[0].TestField1, Is.EqualTo("looking for something?"));
					Assert.That(results[0].TestField2, Is.EqualTo("found it!"));
					Assert.That(results[1].TestField1, Is.EqualTo("record not found"));
					Assert.That(results[1].TestField2, Is.EqualTo("empty"));
				});
			}
		}

		[Test]
		public void MatchPredicateOneColumn([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.Where(r => Sql.Ext.MySql().Match("found", r.TestField1))
					.OrderBy(r => r.Id);

				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(results[0].TestField1, Is.EqualTo("record not found"));
					Assert.That(results[0].TestField2, Is.EqualTo("empty"));
				});
			}
		}

		[Test]
		public void MatchPredicateWithModifier([IncludeDataSources(true, TestProvName.AllMySql)] string context, [Values] MySqlExtensions.MatchModifier modifier)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.Where(r => Sql.Ext.MySql().Match(modifier, "found", r.TestField1, r.TestField2))
					.OrderBy(r => r.Id);

				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(2));
				Assert.Multiple(() =>
				{
					Assert.That(results[0].TestField1, Is.EqualTo("looking for something?"));
					Assert.That(results[0].TestField2, Is.EqualTo("found it!"));
					Assert.That(results[1].TestField1, Is.EqualTo("record not found"));
					Assert.That(results[1].TestField2, Is.EqualTo("empty"));
				});
			}
		}
		#endregion

		#region MATCH
		[Test]
		public void MatchRelevancePredicate([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.OrderByDescending(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField1, r.TestField2))
					.Select(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField1, r.TestField2));

				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(3));
				Assert.That(results[1], Is.EqualTo(results[0]));
				Assert.Multiple(() =>
				{
					Assert.That(results[1], Is.GreaterThan(results[2]));
					Assert.That(results[2], Is.EqualTo(0));
				});
			}
		}

		[Test]
		public void MatchRelevancePredicateOneColumn([IncludeDataSources(true, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.OrderByDescending(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField2))
					.Select(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField2));

				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(3));
				Assert.Multiple(() =>
				{
					Assert.That(results[0], Is.GreaterThan(results[1]));
					Assert.That(results[2], Is.EqualTo(results[1]));
				});
				Assert.That(results[2], Is.EqualTo(0));
			}
		}

		[Test]
		public void MatchRelevancePredicateWithModifier([IncludeDataSources(true, TestProvName.AllMySql)] string context, [Values] MySqlExtensions.MatchModifier modifier)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.OrderByDescending(r => Sql.Ext.MySql().MatchRelevance(modifier, "found", r.TestField1, r.TestField2))
					.Select(r => Sql.Ext.MySql().MatchRelevance(modifier, "found", r.TestField1, r.TestField2));

				var results = query.ToList();
				Assert.That(results, Has.Count.EqualTo(3));
				if (modifier == MySqlExtensions.MatchModifier.WithQueryExpansion)
					Assert.That(results[0], Is.GreaterThan(results[1]));
				else
					Assert.That(results[1], Is.EqualTo(results[0]));
				Assert.Multiple(() =>
				{
					Assert.That(results[1], Is.GreaterThan(results[2]));
					Assert.That(results[2], Is.EqualTo(0));
				});
			}
		}
		#endregion
	}
}
