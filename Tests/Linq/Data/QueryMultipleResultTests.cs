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

		class MultipleResultExample
		{
			[ResultSetIndex(0)] public IEnumerable<Person>  AllPersons   { get; set; } = null!;
			[ResultSetIndex(1)] public IList<Doctor>        AllDoctors   { get; set; } = null!;
			[ResultSetIndex(2)] public IEnumerable<Patient> AllPatients  { get; set; } = null!;
			[ResultSetIndex(3)] public Patient              FirstPatient { get; set; } = null!;
		}

		[Test]
		public void TestQueryMulti([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = db.QueryMultiple<MultipleResultExample>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;" +
					 "select top 1 * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
				Assert.IsTrue(res.FirstPatient != null);
				Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", res.FirstPatient!.Diagnosis);
				Assert.AreEqual(2, res.FirstPatient.PersonID);
			}
		}

		[Test]
		public async Task TestQueryMultiAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = await db.QueryMultipleAsync<MultipleResultExample>(
					"select * from Person;" +
					"select * from Doctor;" +
					"select * from Patient;" +
					"select top 1 * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
				Assert.IsTrue(res.FirstPatient != null);
				Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", res.FirstPatient!.Diagnosis);
				Assert.AreEqual(2, res.FirstPatient.PersonID);
			}
		}

		class MultipleResultExampleWithoutAttributes
		{
			public IEnumerable<Person>  AllPersons   { get; set; } = null!;
			public IList<Doctor>        AllDoctors   { get; set; } = null!;
			public IEnumerable<Patient> AllPatients  { get; set; } = null!;
			public Patient              FirstPatient { get; set; } = null!;
		}

		[Test]
		public void TestQueryMultiWithoutAttributes([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = db.QueryMultiple<MultipleResultExampleWithoutAttributes>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;" +
					 "select top 1 * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
				Assert.IsTrue(res.FirstPatient != null);
				Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", res.FirstPatient!.Diagnosis);
				Assert.AreEqual(2, res.FirstPatient.PersonID);
			}
		}

		[Test]
		public async Task TestQueryMultiWithoutAttributesAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = await db.QueryMultipleAsync<MultipleResultExampleWithoutAttributes>(
					"select * from Person;" +
					"select * from Doctor;" +
					"select * from Patient;" +
					"select top 1 * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
				Assert.IsTrue(res.FirstPatient != null);
				Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", res.FirstPatient!.Diagnosis);
				Assert.AreEqual(2, res.FirstPatient.PersonID);
			}
		}


		[Table]
		class ProcedureMultipleResultExample
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
		public void TestSearchStoredProdecure([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = db.QueryProcMultiple<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}

		[Test]
		public void TestSearchStoredProdecureWithAnonymParameter([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = db.QueryProcMultiple<ProcedureMultipleResultExample>(
					"PersonSearch",
					new { nameFilter = "Jane" }
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}

		[Test]
		public async Task TestSearchStoredProdecureAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}

		[Test]
		public async Task TestSearchStoredProdecureWithTokenAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExample>(
					"PersonSearch",
					CancellationToken.None,
					new { nameFilter = "Jane" }
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}

		[Test]
		public void TestSearchStoredProdecure2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = db.QueryProcMultiple<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Pupkin")
				);

				Assert.IsTrue(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("John", res.MatchingPerson.FirstName);
				Assert.AreEqual("Pupkin", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Male, res.MatchingPerson.Gender);
			}
		}

		[Test]
		public async Task TestSearchStoredProdecure2Async([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Pupkin")
				);

				Assert.IsTrue(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("John", res.MatchingPerson.FirstName);
				Assert.AreEqual("Pupkin", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Male, res.MatchingPerson.Gender);
			}
		}


		[Table]
		class ProcedureMultipleResultExampleWithoutAttributes
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
		public void TestSearchStoredProdecureWithoutAttributes([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = db.QueryProcMultiple<ProcedureMultipleResultExampleWithoutAttributes>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}

		[Test]
		public async Task TestSearchStoredProdecureWithoutAttributesAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var res = await db.QueryProcMultipleAsync<ProcedureMultipleResultExampleWithoutAttributes>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count, 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Length, 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}

	}
}
