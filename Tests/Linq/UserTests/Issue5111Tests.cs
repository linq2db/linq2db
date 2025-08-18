using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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
		public void PreservingOrderBy([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var textTranslationsData = new[]
			{
				new TextTranslationDTO { LanguageId = 1, TextId = 1, Text = "1aaa", TooltipText = "4bbb" },
				new TextTranslationDTO { LanguageId = 1, TextId = 2, Text = "2aaa", TooltipText = "3bbb" },
				new TextTranslationDTO { LanguageId = 2, TextId = 3, Text = "3aaa", TooltipText = "2bbb" },
				new TextTranslationDTO { LanguageId = 2, TextId = 4, Text = "4aaa", TooltipText = "1bbb" }
			};

			var languagesData = new[]
			{
				new LanguageDTO { LanguageID = 1, Name = "aaaa", AlternativeLanguageID = 2 },
				new LanguageDTO { LanguageID = 2, Name = "bbbb", AlternativeLanguageID = 1 }
			};

			var textsData = new[]
			{
				new TextDTO { Id = 1, Nr = 77, ServerOnlyText = true },
				new TextDTO { Id = 2, Nr = 78, ServerOnlyText = true },
				new TextDTO { Id = 3, Nr = 79, ServerOnlyText = true },
				new TextDTO { Id = 4, Nr = 80, ServerOnlyText = true }
			};

			using var db = GetDataContext(context);

			using var textTranslations = db.CreateLocalTable<TextTranslationDTO>(textTranslationsData);
			using var languages = db.CreateLocalTable<LanguageDTO>(languagesData);
			using var texts = db.CreateLocalTable<TextDTO>(textsData);

			using var disp = db.UseLinqOptions(o => o.WithDoNotClearOrderBys(true));

			var qrySorted1 =
				textTranslations
					.OrderBy(tt =>
						languages
							.Where(l => l.AlternativeLanguageID == tt.LanguageId)
							.Select(l => l.LanguageID)
							.Count())
					.ThenBy(tt =>
						texts
							.Where(t => t.Id == tt.TextId)
							.Select(t =>
								t.ServerOnlyText).Single());

			var qrySorted2A =
				qrySorted1
					.ThenBy(tt => tt.TooltipText);

			var sqA = qrySorted2A.GetSelectQuery();

			sqA.OrderBy.Items.Count.ShouldBe(3);
			sqA.OrderBy.Items[2].Expression.ShouldBeOfType<SqlField>().Name.ShouldBe("TooltipText");

			var qrySorted2B =
				qrySorted1
					.OrderBy(tt => tt.TooltipText);

			var sqB = qrySorted2B.GetSelectQuery();

			sqB.OrderBy.Items.Count.ShouldBe(3);
			sqB.OrderBy.Items[2].Expression.ShouldBeOfType<SqlField>().Name.ShouldBe("TooltipText");
		}
	}
}
