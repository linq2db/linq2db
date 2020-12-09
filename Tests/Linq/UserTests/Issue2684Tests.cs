using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2684Tests : TestBase
	{
		[Table("TextTranslations")]
		class TextTranslationDTO
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? LanguageId { get; set; }
			[Column]
			public string? TextId { get; set; }
		}

		[Table("Languages")]
		class LanguageDTO
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? LanguageId { get; set; }
			[Column]
			public string? AlternativeLanguageID { get; set; }
		}

		[Table("Texts")]
		class TextDTO
		{
			[PrimaryKey]
			public string? Id { get; set; }
			[Column]
			public bool ServerOnlyText { get; set; }
			[Column]
			public string? Text { get; set; }
		}

		[Test]
		public void OrderByOrderTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table1 = db.CreateLocalTable<TextTranslationDTO>())
			using (var table2 = db.CreateLocalTable<LanguageDTO>())
			using (var table3 = db.CreateLocalTable<TextDTO>())
			{
				var qrySorted1 =
					from tt in db.GetTable<TextTranslationDTO>()
					orderby (from l in db.GetTable<LanguageDTO>()  where l.AlternativeLanguageID == tt.LanguageId select l.LanguageId).Count()
					select tt;

				var qrySorted2 =
					from tt in qrySorted1
					join t in db.GetTable<TextDTO>() on tt.TextId equals t.Id
					/* then */
					orderby t.ServerOnlyText // 2. Order by
                    select tt;

				var translation1 = qrySorted2.FirstOrDefault();
				var qryString = ((DataConnection)db).LastQuery;

				Assert.Greater(qryString.LastIndexOf("LanguageID", StringComparison.OrdinalIgnoreCase), 0);
				Assert.Greater(qryString.LastIndexOf("ServerOnlyText", StringComparison.OrdinalIgnoreCase), 0);
				Assert.Greater(qryString.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase), 0);
				Assert.Greater(qryString.LastIndexOf("LanguageID", StringComparison.OrdinalIgnoreCase), qryString.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase));
				Assert.Greater(qryString.LastIndexOf("ServerOnlyText", StringComparison.OrdinalIgnoreCase), qryString.LastIndexOf("ORDER BY", StringComparison.OrdinalIgnoreCase));
				Assert.Less(qryString.LastIndexOf("LanguageID", StringComparison.OrdinalIgnoreCase), qryString.LastIndexOf("ServerOnlyText", StringComparison.OrdinalIgnoreCase), "Warnung in LINQ-OrderBy-Reihenfolge");
			}
		}
	}
}
