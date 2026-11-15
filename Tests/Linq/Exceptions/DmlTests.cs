using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Exceptions
{
	[TestFixture]
	public class DmlTests : TestBase
	{
		[Test]
		public void InsertOrUpdate1([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			db.BeginTransaction();

			Assert.That(
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
				Throws.TypeOf<LinqToDBException>().With.Message.EqualTo("InsertOrUpdate method requires the 'Doctor' table to have a primary key."));
		}

		[Test]
		public void InsertOrUpdate2([InsertOrUpdateDataSources] string context)
		{
			using var db = GetDataContext(context);
			db.BeginTransaction();

			Assert.That(
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
				Throws.TypeOf<LinqToDBException>().With.Message.EqualTo("InsertOrUpdate method requires the 'Patient.PersonID' field to be included in the insert setter."));
		}
	}
}
