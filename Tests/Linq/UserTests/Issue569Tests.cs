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
				var result = from patient in db.Patient
					from person in db.Person
					from doctor in db.Doctor
						.Where(x => x.PersonID == person.ID && x.PersonID == patient.PersonID)
						.DefaultIfEmpty()
					where person.FirstName.StartsWith("J")
					select new
					{
						PersonId = patient.PersonID,
						FirstName = person.FirstName,
						Taxonomy = doctor.Taxonomy
					};

				var expected = from patient in Patient
					from person in Person
					from doctor in Doctor
						.Where(x => x.PersonID == person.ID && x.PersonID == patient.PersonID)
						.DefaultIfEmpty()
					where person.FirstName.StartsWith("J")
					select new
					{
						PersonId = patient.PersonID,
						FirstName = person.FirstName,
						Taxonomy = doctor != null ? doctor.Taxonomy : null
					};

				AreEqual(expected, result);
			}
		}
	}
}