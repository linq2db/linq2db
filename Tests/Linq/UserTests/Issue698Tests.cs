using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;
using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue698Tests : TestBase
	{
		[Table]
		public class InfeedAdvicePositionDTO
		{
			[Column] public int Id { get; set; }
			[Column] public string? Text { get; set; }
		}

		[Test]
		public void RegexIsMatchTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);
				SqlRegex.AddRegexSupport();
				
				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "abcd" });
				db.Insert(new InfeedAdvicePositionDTO() { Id = 2, Text = "aabbcc" });
				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where (new Regex("aa.*")).IsMatch(infeed.Text!)
						   select new
						   {
							   InfeedAdvicePosition = infeed,
						   };

				var l = qryA.Single();
			}
		}

		[Test]
		public void RegexIsMatchParaTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);
				SqlRegex.AddRegexSupport();

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "abcd" });
				db.Insert(new InfeedAdvicePositionDTO() { Id = 2, Text = "AAbbcc" });
				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where (new Regex("aa.*", RegexOptions.IgnoreCase)).IsMatch(infeed.Text!)
						   select new
						   {
							   InfeedAdvicePosition = infeed,
						   };

				var l = qryA.Single();
			}
		}

		[Test]
		public void RegexReplaceTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "ab12cd" });
				db.Insert(new InfeedAdvicePositionDTO() { Id = 2, Text = "AA11bb22cc" });

				var replaced = db.GetTable<InfeedAdvicePositionDTO>()
					.OrderBy(_ => _.Id)
					.Select(_ => SqlRegex.Replace(_.Text!, "[0-9]+", ""))
					.ToArray();

				replaced.ShouldBe(new[] { "abcd", "AAbbcc" });
			}
		}

		[Test]
		public void RegexReplaceWithFrameworkOptionsTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "AAbb" });
				db.Insert(new InfeedAdvicePositionDTO() { Id = 2, Text = "XXaa" });

				var replaced = db.GetTable<InfeedAdvicePositionDTO>()
					.OrderBy(_ => _.Id)
					.Select(_ => SqlRegex.Replace(_.Text!, "aa", "", RegexOptions.IgnoreCase))
					.ToArray();

				replaced.ShouldBe(new[] { "bb", "XX" });
			}
		}

		[Test]
		public void RegexReplaceWithSqlRegexOptionsTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "AAbb" });
				db.Insert(new InfeedAdvicePositionDTO() { Id = 2, Text = "XXaa" });

				var replaced = db.GetTable<InfeedAdvicePositionDTO>()
					.OrderBy(_ => _.Id)
					.Select(_ => SqlRegex.Replace(_.Text!, "aa", "", SqlRegex.RegexOptions.IgnoreCase))
					.ToArray();

				replaced.ShouldBe(new[] { "bb", "XX" });
			}
		}

		[Test]
		public void RegexReplaceWithSinglelineOptionTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "a\nb" });

				var withFrameworkOptions = db.GetTable<InfeedAdvicePositionDTO>()
					.Select(_ => SqlRegex.Replace(_.Text!, ".", "x", RegexOptions.Singleline))
					.Single();

				var withCustomOptions = db.GetTable<InfeedAdvicePositionDTO>()
					.Select(_ => SqlRegex.Replace(_.Text!, ".", "x", SqlRegex.RegexOptions.Singleline))
					.Single();

				withFrameworkOptions.ShouldBe("xxx");
				withCustomOptions.ShouldBe("xxx");
			}
		}

		[Test]
		public void RegexReplaceWithStartCountTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "aa11aa22aa" });

				var replaced = db.GetTable<InfeedAdvicePositionDTO>()
					.Select(_ => SqlRegex.Replace(_.Text!, "aa", "X", 3, 1))
					.Single();

				replaced.ShouldBe("aa11X22aa");
			}
		}

		[Test]
		public void RegexInstanceReplaceMappingTest([IncludeDataSources(TestProvName.WithRegexSupport)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);
				SqlRegex.AddRegexSupport();

				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "aa11aa22aa" });

				var replaced = db.GetTable<InfeedAdvicePositionDTO>()
					.Select(_ => (new Regex("aa", RegexOptions.IgnoreCase)).Replace(_.Text!, "X", 1, 2))
					.Single();

				replaced.ShouldBe("aa11X22aa");
			}
		}
	}
}
