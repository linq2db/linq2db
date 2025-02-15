using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2994Tests : TestBase
	{
		public class Combined
		{
			public Model.Person? p { get; set; }
			public Doctor? d { get; set; }
			public Doctor? g { get; set; }
		}

		[Test]
		public void Issue2994Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var qryFlt = from p in db.Person
							 join d in db.Doctor on p.FirstName equals d.Taxonomy
							 join g in db.Doctor on p.LastName equals g.Taxonomy  // remove this and it also would work
						select new Combined { p = p, d = d };
				var qry = db.Person.Where(x => qryFlt.Any(y => y.p == x)).Set(x => x.LastName, "a").Update();
				//var qry = db.Person.Where(x => qryFlt.Any(y => y.p.ID == x.ID)).Set(x => x.LastName, "a").Update(); // workin
				var sql = ((DataConnection)db).LastQuery;
			}
		}
	}
}
