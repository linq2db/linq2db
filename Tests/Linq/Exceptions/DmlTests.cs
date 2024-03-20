using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Exceptions
{
	using Model;

	[TestFixture]
	public class DmlTests : TestBase
	{
		[Test]
		public void InsertOrUpdate1([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				Assert.Throws<LinqException>(
					() =>
						db.Doctor.InsertOrUpdate(
							() => new Doctor
							{
								PersonID = 10,
								Taxonomy = "....",
							},
							p => new Doctor
							{
								Taxonomy = "...",
							}),
					"InsertOrUpdate method requires the 'Doctor' table to have a primary key.");
			}
		}

		[Test]
		public void InsertOrUpdate2([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				Assert.Throws<LinqException>(
					() =>
						db.Patient.InsertOrUpdate(
							() => new Patient
							{
								Diagnosis = "....",
							},
							p => new Patient
							{
								Diagnosis = "...",
							}),
					"InsertOrUpdate method requires the 'Patient.PersonID' field to be included in the insert setter.");
			}
		}
	}
}
