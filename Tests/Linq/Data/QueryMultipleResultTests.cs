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
		[MultipleResultSetsAttribute]
		class MultipleResultExample
		{
			[ResultSetIndex(0)] public IEnumerable<Person> AllPersons { get; set; }
			[ResultSetIndex(1)] public IList<Doctor> AllDoctors { get; set; }
			[ResultSetIndex(2)] public IEnumerable<Patient> AllPatients { get; set; }
		}

		[Test]
		public void TestQueryMulti([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.QueryMulti<MultipleResultExample>(
					"select * from Person;" +
					 "select * from Doctor;" +
					 "select * from Patient;"
				);
				Assert.IsTrue(res.AllDoctors.Any());
				Assert.IsTrue(res.AllPatients.Any());
				Assert.IsTrue(res.AllPersons.Any());
			}
		}

	}
}
