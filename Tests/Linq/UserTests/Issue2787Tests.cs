using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2787Tests : TestBase
	{
		[ActiveIssue]
		[Test]
		public void Issue2787Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var query  = (from pers in db.Person
						  from patient in db.Patient.Where(x => x.PersonID == pers.ID)
						  select new
						  {
							  Pers    = pers,
							  Patient = patient
						  });

			var res = query.Select(x => new {a = x.Pers.ID == 3 ? x.Pers.FirstName : string.Empty}).ToList();

			var lastQuery = ((DataConnection)db).LastQuery;

			Assert.That(lastQuery?.ToLower().Contains("case"), Is.True);
		}
	}
}
