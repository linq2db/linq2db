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
	public class ProcedureMultipleResultTests : TestBase
	{
		[Table]
		[MultipleResultSetsAttribute]
		class MultipleResultExample
		{
			[ResultSetIndex(0)] public IList<int> MatchingPersonIds { get; set; }
			[ResultSetIndex(1)] public IEnumerable<Person> MatchingPersons { get; set; }
			[ResultSetIndex(2)] public IEnumerable<Patient> MatchingPatients { get; set; }
			[ResultSetIndex(3)] public bool DoctorFound { get; set; }
			[ResultSetIndex(4)] public Person[] MatchingPersons2 { get; set; }
			[ResultSetIndex(5)] public int MatchCount { get; set; }
		}

		[Test]
		public void TestSearchStoredProdecure([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryProcMulti<MultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Jane")
				);

				Assert.IsFalse(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count(), 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Count(), 1);
				Assert.AreEqual(res.MatchCount, 1);
			}
		}

		[Test]
		public void TestSearchStoredProdecure2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryProcMulti<MultipleResultExample>(
					"PersonSearch",
					new DataParameter("nameFilter", "Pupkin")
				);

				Assert.IsTrue(res.DoctorFound);
				Assert.AreEqual(res.MatchingPersonIds.Count(), 1);
				Assert.AreEqual(res.MatchingPersons.Count(), 1);
				Assert.AreEqual(res.MatchingPatients.Count(), 0);
				Assert.AreEqual(res.MatchingPersons2.Count(), 1);
				Assert.AreEqual(res.MatchCount, 1);
			}
		}
	}
}
