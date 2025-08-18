using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5108Tests : TestBase
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
		public class TextDTO
		{
			[Column] public int Id { get; set; }
			[Column] public int Nr { get; set; }
			[Column] public bool ServerOnlyText { get; set; }
		}

		[Test]
		public void OrderByCleanUp([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var textTranslationsData = new[]
			{
				new TextTranslationDTO() { LanguageId = 1, TextId = 1, Text = "bbb", TooltipText = "ccc" },
			};

			var textData = new[]
			{
				new TextDTO() { Id = 1, Nr = 77, ServerOnlyText = true },
			};

			using var db = GetDataContext(context);

			using var translations = db.CreateLocalTable(textTranslationsData);
			using var texts = db.CreateLocalTable(textData);
			
			var qryUnsorted =
				from tt in db.GetTable<TextTranslationDTO>()
				select tt;

			var qrySortedJoin1 = from tt in qryUnsorted
				join t in db.GetTable<TextDTO>() on tt.TextId equals t.Id
				orderby t.ServerOnlyText
				select tt;

			var qry = qrySortedJoin1.OrderBy(x => x.LanguageId);

			var sqlWithOrder = qry.ToSqlQuery().Sql;
			BaselinesManager.LogQuery(sqlWithOrder);

			var sqlWithOrderAst = qry.GetSelectQuery();

			sqlWithOrderAst.OrderBy.Items.Count.ShouldBe(2);
			sqlWithOrderAst.OrderBy.Items[0].Expression.ShouldBeOfType<SqlField>().Name.ShouldBe("LanguageId");
			sqlWithOrderAst.OrderBy.Items[1].Expression.ShouldBeOfType<SqlField>().Name.ShouldBe("ServerOnlyText");

			var noOrderBy = qry.RemoveOrderBy();

			var sqlNoOrder = noOrderBy.ToSqlQuery().Sql;
			BaselinesManager.LogQuery(sqlNoOrder);

			var sqlNoOrderAst = noOrderBy.GetSelectQuery();
			sqlNoOrderAst.OrderBy.Items.Count.ShouldBe(0);
		}
	}
}
