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
		public void OrderByCleanUp([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TextTranslationDTO>())
			using (db.CreateLocalTable<TextDTO>())
			{
				db.Insert(new TextTranslationDTO() { LanguageId = 1, TextId = 1, Text = "bbb", TooltipText = "ccc" });
				db.Insert(new TextDTO() { Id = 1, Nr = 77 });

				var qryUnsorted =
				from tt in db.GetTable<TextTranslationDTO>()
				select tt;

				var qrySortedJoin1 = from tt in qryUnsorted
									join t in db.GetTable<TextDTO>() on tt.TextId equals t.Id
									orderby t.ServerOnlyText
									select tt;
				var qry1 = qrySortedJoin1.OrderBy(x => x.LanguageId);

				Configuration.Linq.DoNotClearOrderBys = false;

				var l1 = qry1.ToList();
				var sql1 = ((DataConnection)db).LastQuery!;
				Assert.That(sql1.LastIndexOf("ServerOnlyText", StringComparison.OrdinalIgnoreCase), Is.LessThan(0), "first order by should be deleted");

				Configuration.Linq.DoNotClearOrderBys = true;

				var qrySortedJoin2 = from tt in qryUnsorted
									 join t in db.GetTable<TextDTO>() on tt.TextId equals t.Id
									 orderby t.ServerOnlyText
									 select tt;
				var qry2 = qrySortedJoin2.OrderBy(x => x.LanguageId);
				var l2 = qry2.ToList();
				var sql2 = ((DataConnection)db).LastQuery!;
				using (Assert.EnterMultipleScope())
				{
					Assert.That(sql2.ToLower(), Does.Contain("serveronlytext"));
					Assert.That(sql2.LastIndexOf("ServerOnlyText", StringComparison.OrdinalIgnoreCase), Is.LessThan(sql2.LastIndexOf("LanguageId", StringComparison.OrdinalIgnoreCase)), "error in order by order");
				}

				var qry3 = qry2.RemoveOrderBy();
				var l3 = qry3.ToList();
				var sql3 = ((DataConnection)db).LastQuery!;
				Assert.That(sql3.ToLower(), Does.Not.Contain("order"), "es sollte kein order by enthalten sein");
			}
		}
	}
}
