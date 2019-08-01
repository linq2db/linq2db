using System;
using System.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using LinqToDB.Data;
using System.Collections.Generic;
using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class QueryMultipleResultTests : TestBase
	{
		[Table]
		class MultipleResultExample
		{
			[ResultSetIndex(0)] public IEnumerable<Person> AllPersons { get; set; }
			[ResultSetIndex(1)] public IList<Doctor> AllDoctors { get; set; }
			[ResultSetIndex(2)] public IEnumerable<Patient> AllPatients { get; set; }
			[ResultSetIndex(3)] public Patient FirstPatient { get; set; }
		}

		[Test]
		public void TestQueryMulti([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryMulti<MultipleResultExample>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;" +
					 "select top 1 * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
				Assert.IsTrue(res.FirstPatient != null);
				Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", res.FirstPatient.Diagnosis);
				Assert.AreEqual(2, res.FirstPatient.PersonID);
			}
		}

		[Table]
		class MultipleResultExampleWithoutAttributes
		{
			public IEnumerable<Person> AllPersons { get; set; }
			public IList<Doctor> AllDoctors { get; set; }
			public IEnumerable<Patient> AllPatients { get; set; }
			public Patient FirstPatient { get; set; }
		}

		[Test]
		public void TestQueryMultiWithoutAttributes([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryMulti<MultipleResultExampleWithoutAttributes>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;" +
					 "select top 1 * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
				Assert.IsTrue(res.FirstPatient != null);
				Assert.AreEqual("Hallucination with Paranoid Bugs' Delirium of Persecution", res.FirstPatient.Diagnosis);
				Assert.AreEqual(2, res.FirstPatient.PersonID);
			}
		}


		[Table]
		class ProcedureMultipleResultExample
		{
			[ResultSetIndex(0)] public IList<int> MatchingPersonIds { get; set; }
			[ResultSetIndex(1)] public IEnumerable<Person> MatchingPersons { get; set; }
			[ResultSetIndex(2)] public IEnumerable<Patient> MatchingPatients { get; set; }
			[ResultSetIndex(3)] public bool DoctorFound { get; set; }
			[ResultSetIndex(4)] public Person[] MatchingPersons2 { get; set; }
			[ResultSetIndex(5)] public int MatchCount { get; set; }
			[ResultSetIndex(6)] public Person MatchingPerson { get; set; }
		}

		[Test]
		public void TestSearchStoredProdecure([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryProcMulti<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count(), 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Count(), 1);
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
			using (var db = new DataConnection(context))
			{
				var res = db.QueryProcMulti<ProcedureMultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Pupkin")
				);

				Assert.IsTrue(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count(), 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Count(), 1);
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
			public IList<int> MatchingPersonIds { get; set; }
			public IEnumerable<Person> MatchingPersons { get; set; }
			public IEnumerable<Patient> MatchingPatients { get; set; }
			public bool DoctorFound { get; set; }
			public Person[] MatchingPersons2 { get; set; }
			public int MatchCount { get; set; }
			public Person MatchingPerson { get; set; }
		}

		[Test]
		public void TestSearchStoredProdecureWithoutAttributes([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryProcMulti<ProcedureMultipleResultExampleWithoutAttributes>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count(), 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Count(), 1);
				Assert.AreEqual(res.MatchCount, 1);
				Assert.NotNull(res.MatchingPerson);
				Assert.AreEqual("Jane", res.MatchingPerson.FirstName);
				Assert.AreEqual("Doe", res.MatchingPerson.LastName);
				Assert.AreEqual(Gender.Female, res.MatchingPerson.Gender);
			}
		}
	}
}
