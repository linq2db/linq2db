namespace Tests.UserTests
{
	using System.Linq;

	using NUnit.Framework;

	[TestFixture]
	public class Issue569Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = from territory in db.Patient
					from employee in db.Person
					from eter in db.Doctor
						.Where(x => x.PersonID == employee.ID && x.PersonID == territory.PersonID)
						.DefaultIfEmpty()
					where employee.FirstName.StartsWith("J")
					select new
					{
						PersonId = territory.PersonID,
						FirstName = employee.FirstName,
						Taxonomy = eter.Taxonomy
					};

				var expected = from territory in Patient
					from employee in Person
					from eter in Doctor
						.Where(x => x.PersonID == employee.ID && x.PersonID == territory.PersonID)
						.DefaultIfEmpty()
					where employee.FirstName.StartsWith("J")
					select new
					{
						PersonId = territory.PersonID,
						FirstName = employee.FirstName,
						Taxonomy = eter != null ? eter.Taxonomy : null
					};

				AreEqual(expected, result);
			}
		}
	}
}