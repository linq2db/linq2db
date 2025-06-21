using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1969Tests : TestBase
	{
		[Table("CONFIG")]
		public partial class Config
		{
			[Column("ID"),      PrimaryKey,  NotNull] public int     Id   { get; set; } 
			[Column("NAME"),    Nullable            ] public string? Name { get; set; } 
	  
			[ExpressionMethod(nameof(GetFromExpr), IsColumn = true)]
			public int CountOf{ get; set; }

			static Expression<Func<IDataContext, Config, int>> GetFromExpr()
			{
				return (db, p) => db.GetTable<Kompo>().Where(k => k.Number == p.Id).Count();
			}
		}

		[Table("KOMPO")]
		public partial class Kompo
		{
			[Column("TYP"),    PrimaryKey, NotNull] public int Typ    { get; set; }
			[Column("NUMBER"),             NotNull] public int Number { get; set; }
		}

		[Test]
		public void ExtpressionColumnTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new Config{Id = 1, Name = "Some1"},
				new Config{Id = 2, Name = "Some2"}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Kompo{Number = 1, Typ = 1} ,
				new Kompo{Number = 1, Typ = 2}, 
				new Kompo{Number = 2, Typ = 3} 
			}))
			{
				var query =
					from k in db.GetTable<Kompo>()
					from c in db.GetTable<Config>().InnerJoin(c => c.Id == k.Number)
					where c.CountOf > 1
					select c;
				var res = query.ToArray();

				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].CountOf, Is.EqualTo(2));
					Assert.That(res[1].CountOf, Is.EqualTo(2));
				}
			}
		}
	}
}
