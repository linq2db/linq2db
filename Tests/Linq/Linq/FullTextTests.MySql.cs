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
			public string TestField1 { get; set; }

			[Column("TestField2")]
			public string TestField2 { get; set; }
		}

		#region MATCH
		[Test]
		public void MatchPredicate([IncludeDataSources(true, TestProvName.AllMySqlFullText)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.Where(r => Sql.Ext.MySql().Match("found", r.TestField1, r.TestField2))
					.OrderBy(r => r.Id);

				var results = query.ToList();
				Assert.AreEqual(2, results.Count);
				Assert.AreEqual("looking for something?", results[0].TestField1);
				Assert.AreEqual("found it!", results[0].TestField2);
				Assert.AreEqual("record not found", results[1].TestField1);
				Assert.AreEqual("empty", results[1].TestField2);
			}
		}

		[Test]
		public void MatchPredicateOneColumn([IncludeDataSources(true, TestProvName.AllMySqlFullText)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.Where(r => Sql.Ext.MySql().Match("found", r.TestField1))
					.OrderBy(r => r.Id);

				var results = query.ToList();
				Assert.AreEqual(1, results.Count);
				Assert.AreEqual("record not found", results[0].TestField1);
				Assert.AreEqual("empty", results[0].TestField2);
			}
		}

		[Test]
		public void MatchPredicateWithModifier([IncludeDataSources(true, TestProvName.AllMySqlFullText)] string context, [Values] MySqlExtensions.MatchModifier modifier)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.Where(r => Sql.Ext.MySql().Match(modifier, "found", r.TestField1, r.TestField2))
					.OrderBy(r => r.Id);

				var results = query.ToList();
				Assert.AreEqual(2, results.Count);
				Assert.AreEqual("looking for something?", results[0].TestField1);
				Assert.AreEqual("found it!", results[0].TestField2);
				Assert.AreEqual("record not found", results[1].TestField1);
				Assert.AreEqual("empty", results[1].TestField2);
			}
		}
		#endregion

		#region MATCH
		[Test]
		public void MatchRelevancePredicate([IncludeDataSources(true, TestProvName.AllMySqlFullText)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.OrderByDescending(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField1, r.TestField2))
					.Select(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField1, r.TestField2));

				var results = query.ToList();
				Assert.AreEqual(3, results.Count);
				Assert.AreEqual(results[0], results[1]);
				Assert.Greater(results[1], results[2]);
				Assert.AreEqual(0, results[2]);
			}
		}

		[Test]
		public void MatchRelevancePredicateOneColumn([IncludeDataSources(true, TestProvName.AllMySqlFullText)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.OrderByDescending(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField2))
					.Select(r => Sql.Ext.MySql().MatchRelevance("found", r.TestField2));

				var results = query.ToList();
				Assert.AreEqual(3, results.Count);
				Assert.Greater(results[0], results[1]);
				Assert.AreEqual(results[1], results[2]);
				Assert.AreEqual(0, results[2]);
			}
		}

		[Test]
		public void MatchRelevancePredicateWithModifier([IncludeDataSources(true, TestProvName.AllMySqlFullText)] string context, [Values] MySqlExtensions.MatchModifier modifier)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.GetTable<FullTextIndexTest>()
					.OrderByDescending(r => Sql.Ext.MySql().MatchRelevance(modifier, "found", r.TestField1, r.TestField2))
					.Select(r => Sql.Ext.MySql().MatchRelevance(modifier, "found", r.TestField1, r.TestField2));

				var results = query.ToList();
				Assert.AreEqual(3, results.Count);
				if (modifier == MySqlExtensions.MatchModifier.WithQueryExpansion)
					Assert.Greater(results[0], results[1]);
				else
					Assert.AreEqual(results[0], results[1]);
				Assert.Greater(results[1], results[2]);
				Assert.AreEqual(0, results[2]);
			}
		}
		#endregion
	}
}
