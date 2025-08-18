using System;
using System.Linq;
using System.Linq.Dynamic.Core;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5111Tests : TestBase
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
		public void CollectionModifiedException([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle)] string context)
		{
			Configuration.Linq.DoNotClearOrderBys = true;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TextTranslationDTO>())
			using (db.CreateLocalTable<LanguageDTO>())
			using (db.CreateLocalTable<TextDTO>())
			{
				db.Insert(new TextTranslationDTO() { LanguageId = 1, TextId = 1, Text = "1bbb", TooltipText = "4ccc" });
				db.Insert(new TextTranslationDTO() { LanguageId = 1, TextId = 2, Text = "2bbb", TooltipText = "3ccc" });
				db.Insert(new TextTranslationDTO() { LanguageId = 2, TextId = 3, Text = "3bbb", TooltipText = "2ccc" });
				db.Insert(new TextTranslationDTO() { LanguageId = 2, TextId = 4, Text = "4bbb", TooltipText = "1ccc" });
				db.Insert(new LanguageDTO() { LanguageID = 1, Name = "aaaa", AlternativeLanguageID = 2 });
				db.Insert(new LanguageDTO() { LanguageID = 2, Name = "bbbb", AlternativeLanguageID = 1 });
				db.Insert(new TextDTO() { Id = 1, Nr = 77 });
				db.Insert(new TextDTO() { Id = 2, Nr = 78 });
				db.Insert(new TextDTO() { Id = 3, Nr = 79 });
				db.Insert(new TextDTO() { Id = 4, Nr = 80 });

				var qrySorted1 =
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
									t.ServerOnlyText).Single());

				var qrySorted2A =
					qrySorted1
						.ThenBy(tt => tt.TooltipText);

				var qrySorted2B =
					qrySorted1
						.OrderBy(tt => tt.TooltipText);

				var translation1 = qrySorted2A.FirstOrDefault();
				var qryString = ((DataConnection) db).LastQuery;

				var translation2 = qrySorted2B.FirstOrDefault();
				qryString = ((DataConnection) db).LastQuery;
			}
		}
	}
}
