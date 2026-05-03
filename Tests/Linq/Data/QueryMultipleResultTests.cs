using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class QueryMultipleResultTests : TestBase
	{
		sealed class MultipleResultExample
		{
			[ResultSetIndex(0)] public IEnumerable<Person>  AllPersons   { get; set; } = null!;
			[ResultSetIndex(1)] public IList<Doctor>        AllDoctors   { get; set; } = null!;
			[ResultSetIndex(2)] public IEnumerable<Patient> AllPatients  { get; set; } = null!;
			[ResultSetIndex(3)] public Patient              FirstPatient { get; set; } = null!;
		}

		[Test]
		public void TestQueryMulti([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = db.QueryMultiple<MultipleResultExample>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;" +
					 "select top 1 * from Patient;"
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.AllDoctors.Any(), Is.True);
				Assert.That(res.AllPatients.Any(), Is.True);
				Assert.That(res.AllPersons.Any(), Is.True);
				Assert.That(res.FirstPatient, Is.Not.Null);
				Assert.That(res.FirstPatient!.Diagnosis, Is.EqualTo("Hallucination with Paranoid Bugs' Delirium of Persecution"));
				Assert.That(res.FirstPatient.PersonID, Is.EqualTo(2));
			}
		}

		[Test]
		public async Task TestQueryMultiAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = await db.QueryMultipleAsync<MultipleResultExample>(
					"select * from Person;" +
					"select * from Doctor;" +
					"select * from Patient;" +
					"select top 1 * from Patient;"
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.AllDoctors.Any(), Is.True);
				Assert.That(res.AllPatients.Any(), Is.True);
				Assert.That(res.AllPersons.Any(), Is.True);
				Assert.That(res.FirstPatient, Is.Not.Null);
				Assert.That(res.FirstPatient!.Diagnosis, Is.EqualTo("Hallucination with Paranoid Bugs' Delirium of Persecution"));
				Assert.That(res.FirstPatient.PersonID, Is.EqualTo(2));
			}
		}

		sealed class MultipleResultExampleWithoutAttributes
		{
			public IEnumerable<Person>  AllPersons   { get; set; } = null!;
			public IList<Doctor>        AllDoctors   { get; set; } = null!;
			public IEnumerable<Patient> AllPatients  { get; set; } = null!;
			public Patient              FirstPatient { get; set; } = null!;
		}

		[Test]
		public void TestQueryMultiWithoutAttributes([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = db.QueryMultiple<MultipleResultExampleWithoutAttributes>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;" +
					 "select top 1 * from Patient;"
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.AllDoctors.Any(), Is.True);
				Assert.That(res.AllPatients.Any(), Is.True);
				Assert.That(res.AllPersons.Any(), Is.True);
				Assert.That(res.FirstPatient, Is.Not.Null);
				Assert.That(res.FirstPatient!.Diagnosis, Is.EqualTo("Hallucination with Paranoid Bugs' Delirium of Persecution"));
				Assert.That(res.FirstPatient.PersonID, Is.EqualTo(2));
			}
		}

		[Test]
		public async Task TestQueryMultiWithoutAttributesAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = await db.QueryMultipleAsync<MultipleResultExampleWithoutAttributes>(
					"select * from Person;" +
					"select * from Doctor;" +
					"select * from Patient;" +
					"select top 1 * from Patient;"
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.AllDoctors.Any(), Is.True);
				Assert.That(res.AllPatients.Any(), Is.True);
				Assert.That(res.AllPersons.Any(), Is.True);
				Assert.That(res.FirstPatient, Is.Not.Null);
				Assert.That(res.FirstPatient!.Diagnosis, Is.EqualTo("Hallucination with Paranoid Bugs' Delirium of Persecution"));
				Assert.That(res.FirstPatient.PersonID, Is.EqualTo(2));
			}
		}

		[Table]
		sealed class ProcedureMultipleResultExample
		{
			[ResultSetIndex(0)] public IList<int>           MatchingPersonIds { get; set; } = null!;
			[ResultSetIndex(1)] public IEnumerable<Person>  MatchingPersons   { get; set; } = null!;
			[ResultSetIndex(2)] public IEnumerable<Patient> MatchingPatients  { get; set; } = null!;
			[ResultSetIndex(3)] public bool                 DoctorFound       { get; set; }
			[ResultSetIndex(4)] public Person[]             MatchingPersons2  { get; set; } = null!;
			[ResultSetIndex(5)] public int                  MatchCount        { get; set; }
			[ResultSetIndex(6)] public Person               MatchingPerson    { get; set; } = null!;
		}

		[Test]
		public void TestSearchStoredProcedure([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = db.QueryProcMultiple<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.False);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("Jane"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Doe"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Female));
			}
		}

		[Test]
		public void TestSearchStoredProcedureWithAnonymParameter([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = db.QueryProcMultiple<ProcedureMultipleResultExample>(
					"PersonSearch",
					new { nameFilter = "Jane" }
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.False);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("Jane"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Doe"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Female));
			}
		}

		[Test]
		public async Task TestSearchStoredProcedureAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.False);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("Jane"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Doe"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Female));
			}
		}

		[Test]
		public async Task TestSearchStoredProcedureWithTokenAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExample>(
					"PersonSearch",
					new { nameFilter = "Jane" }
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.False);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("Jane"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Doe"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Female));
			}
		}

		[Test]
		public void TestSearchStoredProcedure2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = db.QueryProcMultiple<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Pupkin")
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.True);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("John"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Pupkin"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Male));
			}
		}

		[Test]
		public async Task TestSearchStoredProcedure2Async([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Pupkin")
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.True);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("John"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Pupkin"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Male));
			}
		}

		[Table]
		sealed class ProcedureMultipleResultExampleWithoutAttributes
		{
			public IList<int>           MatchingPersonIds { get; set; } = null!;
			public IEnumerable<Person>  MatchingPersons   { get; set; } = null!;
			public IEnumerable<Patient> MatchingPatients  { get; set; } = null!;
			public bool                 DoctorFound       { get; set; }
			public Person[]             MatchingPersons2  { get; set; } = null!;
			public int                  MatchCount        { get; set; }
			public Person               MatchingPerson    { get; set; } = null!;
		}

		[Test]
		public void TestSearchStoredProcedureWithoutAttributes([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = db.QueryProcMultiple<ProcedureMultipleResultExampleWithoutAttributes>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.False);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("Jane"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Doe"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Female));
			}
		}

		[Test]
		public async Task TestSearchStoredProcedureWithoutAttributesAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExampleWithoutAttributes>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.DoctorFound, Is.False);
				Assert.That(res.MatchingPersonIds, Has.Count.EqualTo(1));
				Assert.That(res.MatchingPersons.Count(), Is.EqualTo(1));
				Assert.That(res.MatchingPatients.Count(), Is.Zero);
				Assert.That(res.MatchingPersons2, Has.Length.EqualTo(1));
				Assert.That(res.MatchCount, Is.EqualTo(1));
				Assert.That(res.MatchingPerson, Is.Not.Null);
				Assert.That(res.MatchingPerson.FirstName, Is.EqualTo("Jane"));
				Assert.That(res.MatchingPerson.LastName, Is.EqualTo("Doe"));
				Assert.That(res.MatchingPerson.Gender, Is.EqualTo(Gender.Female));
			}
		}

		#region Issue 4728
		class ToDeleteId
		{
			public int CaseLogId { get; set; }
			public int CaseNbr { get; set; }
		}

		class NotDeletedId
		{
			public int CaseLogId { get; set; }
			public int CaseNbr { get; set; }
			public int CrimeCodesId { get; set; }
		}

		class DeletedId
		{
			public int CaseLogId { get; set; }
			public int CaseNbr { get; set; }
		}

		class MultipleResultDeletion
		{
			// all 3 supported types
			[ResultSetIndex(0)] public ToDeleteId[] ToDeleteId { get; set; } = null!;
			[ResultSetIndex(1)] public List<NotDeletedId> NotDeletedId { get; set; } = null!;
			[ResultSetIndex(2)] public DeletedId DeletedId { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4728")]
		public async Task Issue4728Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			var dataset = await db.QueryMultipleAsync<MultipleResultDeletion>(@"
select 1, 1 from INFORMATION_SCHEMA.TABLES where 1 = 0;
select 1, 1, 1 from INFORMATION_SCHEMA.TABLES where 1 = 0;
select 1, 1 from INFORMATION_SCHEMA.TABLES where 1 = 0;");
		}
		#endregion
	}
}
