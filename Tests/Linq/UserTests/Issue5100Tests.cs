using System.Linq;
using System.Linq.Dynamic.Core;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5100Tests : TestBase
	{
		[Table]
		public class TextTranslationDTO
		{
			[Column] public int LanguageId { get; set; }
			[Column] public int TextId { get; set; }
			[Column] public string? Text { get; set; }
			[Column] public string? TooltipText { get; set; }
		}

		[Table]
		public class LanguageDTO
		{
			[Column] public int LanguageID { get; set; }
			[Column] public string? Name { get; set; }
			[Column] public int AlternativeLanguageID { get; set; }
		}

		[Table]
		public class TextDTO
		{
			[Column] public int Id { get; set; }
			[Column] public int Nr { get; set; }
			[Column] public bool ServerOnlyText { get; set; }
		}

		[Test]
		public void LinqToSqlNotPossible([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TextTranslationDTO>())
			using (db.CreateLocalTable<LanguageDTO>())
			using (db.CreateLocalTable<TextDTO>())
			{
				db.Insert(new TextTranslationDTO() { LanguageId = 1, TextId = 1, Text = "bbb", TooltipText = "ccc" });
				db.Insert(new LanguageDTO() { LanguageID = 1, Name = "aaaa", AlternativeLanguageID = 1 });
				db.Insert(new TextDTO() { Id = 1, Nr = 77 });

				Assert.Throws<LinqToDBException>(()=>
					db.GetTable<TextTranslationDTO>()
						.OrderBy(tt =>
							db.GetTable<LanguageDTO>()
								.Where(l => l.AlternativeLanguageID == tt.LanguageId)
								.Select(l => l.LanguageID)
								.Count())
						.ThenBy(tt =>
							db.GetTable<TextDTO>()
								.Where(t => t.Id == tt.TextId)
								.Select(t =>
									t.ServerOnlyText)).FirstOrDefault()
				);

				var qrySorted = db.GetTable<TextTranslationDTO>()
						.OrderBy(tt =>
							db.GetTable<LanguageDTO>()
								.Where(l => l.AlternativeLanguageID == tt.LanguageId)
								.Select(l => l.LanguageID)
								.Count())
						.ThenBy(tt =>
							db.GetTable<TextDTO>()
								.Where(t => t.Id == tt.TextId)
								.Select(t =>
									t.ServerOnlyText).Single());
				var translation1 = qrySorted.FirstOrDefault();
				var qryString = ((DataConnection) db).LastQuery;
			}
		}
	}
}
