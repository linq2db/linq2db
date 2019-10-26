using System;
using System.Linq;

using Tests.Model;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;
using System.Linq.Expressions;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Table("Person")]
		public class TestJoinPerson
		{
			[Column("PersonID"), Identity, PrimaryKey] public int ID;

			[Association(ThisKey = "ID", OtherKey = "PersonID", CanBeNull = false)]
			public TestJoinPatient Patient;

			[Association(QueryExpressionMethod = nameof(Query), CanBeNull = false)]
			public TestJoinPatient PatientQuery;

			static Expression<Func<TestJoinPerson, IDataContext, IQueryable<TestJoinPatient>>> Query
				=> (t, ctx) => ctx.GetTable<TestJoinPatient>().Where(p => p.PersonID == t.ID);
		}

		[Table("Patient")]
		public class TestJoinPatient
		{
			[PrimaryKey] public int    PersonID;
			[Column]     public string Diagnosis;
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TargetAssociation([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.GetTable<TestJoinPerson>()
					.Merge()
					.Using(db.GetTable<TestJoinPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient.Diagnosis.Contains("very"))
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[4], result[3]);
				AssertPerson(AssociationPersons[5], result[4]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TargetQueryAssociation([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.GetTable<TestJoinPerson>()
					.Merge()
					.Using(db.GetTable<TestJoinPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.PatientQuery.Diagnosis.Contains("very"))
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[4], result[3]);
				AssertPerson(AssociationPersons[5], result[4]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SourceAssociationAsInnerJoin1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var parsons = db.Person.ToArray();
				var patients = db.Patient.ToArray();

				var cnt = db.GetTable<TestJoinPerson>()
					.Merge()
					// inner join performed in source
					.Using(db.GetTable<TestJoinPerson>().Select(p => new { p.ID, p.Patient.Diagnosis }))
					.On((t, s) => t.ID == s.ID)
					.DeleteWhenMatchedAnd((t, s) => s.Diagnosis != "sick")
					.Merge();

				Assert.AreEqual(1, cnt);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SourceAssociationAsInnerJoin2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var parsons = db.Person.ToArray();
				var patients = db.Patient.ToArray();

				var cnt = db.GetTable<TestJoinPerson>()
					.Merge()
					.Using(db.GetTable<TestJoinPerson>())
					// inner join still performed in source
					.On((t, s) => t.ID == s.ID && s.Patient.Diagnosis != null)
					.DeleteWhenMatchedAnd((t, s) => s.Patient.Diagnosis != "sick")
					.Merge();

				Assert.AreEqual(1, cnt);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SourceAssociationAsOuterJoin([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var parsons = db.Person.ToArray();
				var patients = db.Patient.ToArray();

				var cnt = db.GetTable<TestJoinPerson>()
					.Merge()
					.Using(db.GetTable<TestJoinPerson>())
					.On((t, s) => t.ID == s.ID)
					// // inner join promoted to outer join
					.DeleteWhenMatchedAnd((t, s) => s.Patient.Diagnosis != "sick")
					.Merge();

				Assert.AreEqual(5, cnt);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OtherSourceAssociationInDeleteBySourcePredicate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient.Diagnosis.Contains("very"))
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[4], result[3]);
				AssertPerson(AssociationPersons[5], result[4]);
			}
		}

		[Test]
		public void OtherSourceAssociationInDeletePredicate([MergeDataContextSource(
			false,
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Patient
					.Merge()
					.Using(db.Patient)
					.On((t, s) => t.PersonID == s.PersonID && s.Diagnosis.Contains("very"))
					.DeleteWhenMatchedAnd((t, s) => s.Person.FirstName == "first 4" && t.Person.FirstName == "first 4")
					.Merge();

				var result = db.Patient.OrderBy(_ => _.PersonID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(1, result.Count);

				Assert.AreEqual(AssociationPatients[0].PersonID, result[0].PersonID);
				Assert.AreEqual(AssociationPatients[0].Diagnosis, result[0].Diagnosis);
			}
		}

		// ASE: server dies
		// Oracle: associations in insert setter
		[Test]
		public void OtherSourceAssociationInInsertCreate([MergeDataContextSource(
			false,
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.Patient.Diagnosis.Contains("sick"), s => new Person()
					{
						FirstName = s.Patient.Diagnosis,
						LastName = "Inserted 2",
						Gender = Gender.Unknown
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);

				Assert.AreEqual(AssociationPersons[5].ID + 1, result[6].ID);
				Assert.AreEqual(Gender.Unknown, result[6].Gender);
				Assert.AreEqual("sick", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE: server dies
		// Oracle: associations in insert setters
		// Informix: associations doesn't work right now
		// SAP: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInInsertCreate2([MergeDataContextSource(
			false,
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix, TestProvName.AllSapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatched(s => new Person()
					{
						FirstName = s.Patient.Diagnosis,
						LastName = "Inserted 2",
						Gender = Gender.Unknown
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);

				Assert.AreEqual(AssociationPersons[5].ID + 1, result[6].ID);
				Assert.AreEqual(Gender.Unknown, result[6].Gender);
				Assert.AreEqual("sick", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE: server dies
		[Test]
		public void OtherSourceAssociationInInsertPredicate([MergeDataContextSource(
			false,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.Patient.Diagnosis.Contains("sick"), s => new Person()
					{
						FirstName = "Inserted 1",
						LastName = "Inserted 2",
						Gender = Gender.Male
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(7, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);

				Assert.AreEqual(AssociationPersons[5].ID + 1, result[6].ID);
				Assert.AreEqual(Gender.Male, result[6].Gender);
				Assert.AreEqual("Inserted 1", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE, DB2: Associations in match not supported
		// Informix: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInMatch([MergeDataContextSource(
			false,
			ProviderName.DB2, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID
							&& t.Patient.Diagnosis.Contains("very")
							&& s.Patient.Diagnosis.Contains("sick"))
					.UpdateWhenMatched((t, s) => new Person()
					{
						MiddleName = "R.I.P."
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual(AssociationPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(AssociationPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("R.I.P.", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		// ASE: server dies
		// Informix: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInUpdate([MergeDataContextSource(
			false,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatched((t, s) => new Person()
					{
						MiddleName = "first " + s.Patient.Diagnosis,
						LastName = "last " + t.Patient.Diagnosis
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("last very sick", result[3].LastName);
				Assert.AreEqual("first very sick", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OtherSourceAssociationInUpdateBySource([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(t => t.FirstName == "first 3",
						t => new Person()
						{
							FirstName = "Updated",
							LastName = t.Patient.Diagnosis
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);

				Assert.AreEqual(AssociationPersons[2].ID, result[2].ID);
				Assert.AreEqual(AssociationPersons[2].Gender, result[2].Gender);
				Assert.AreEqual("Updated", result[2].FirstName);
				Assert.AreEqual("sick", result[2].LastName);
				Assert.AreEqual(AssociationPersons[2].MiddleName, result[2].MiddleName);

				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void OtherSourceAssociationInUpdateBySourcePredicate(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(t => t.Patient.Diagnosis.Contains("very"),
						t => new Person()
						{
							FirstName = "Updated"
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("Updated", result[3].FirstName);
				Assert.AreEqual(AssociationPersons[3].LastName, result[3].LastName);
				Assert.AreEqual(AssociationPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		// ASE: server dies
		[Test]
		public void OtherSourceAssociationInUpdatePredicate([MergeDataContextSource(
			false,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedAnd(
						(t, s) => s.Patient.Diagnosis == t.Patient.Diagnosis && t.Patient.Diagnosis.Contains("very"),
						(t, s) => new Person()
						{
							LastName = "Updated"
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("Updated", result[3].LastName);
				Assert.AreEqual(AssociationPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SameSourceAssociationInDeleteBySourcePredicate(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient.Diagnosis.Contains("very"))
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[4], result[3]);
				AssertPerson(AssociationPersons[5], result[4]);
			}
		}

		[Test]
		public void SameSourceAssociationInDeletePredicate([MergeDataContextSource(
			false,
			ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Patient
					.Merge()
					.Using(db.Patient)
					.On((t, s) => t.PersonID == s.PersonID && s.Diagnosis.Contains("very"))
					.DeleteWhenMatchedAnd((t, s) => s.Person.FirstName == "first 4" && t.Person.FirstName == "first 4")
					.Merge();

				var result = db.Patient.OrderBy(_ => _.PersonID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(1, result.Count);

				Assert.AreEqual(AssociationPatients[0].PersonID, result[0].PersonID);
				Assert.AreEqual(AssociationPatients[0].Diagnosis, result[0].Diagnosis);
			}
		}

		// ASE: server dies
		// Oracle: associations in instert setters
		[Test]
		public void SameSourceAssociationInInsertCreate([MergeDataContextSource(
			false,
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient.Diagnosis.Contains("sick"),
						s => new Person()
						{
							FirstName = s.Patient.Diagnosis,
							LastName = "Inserted 2",
							Gender = Gender.Unknown
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);

				Assert.AreEqual(AssociationPersons[5].ID + 1, result[6].ID);
				Assert.AreEqual(Gender.Unknown, result[6].Gender);
				Assert.AreEqual("sick", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE: server dies
		// Oracle: associations in instert setters
		// Informix: associations doesn't work right now
		// SAP: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInInsertCreate2([MergeDataContextSource(
			false,
			ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix, TestProvName.AllSapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatched(s => new Person()
					{
						FirstName = s.Patient.Diagnosis,
						LastName = "Inserted 2",
						Gender = Gender.Unknown
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);

				Assert.AreEqual(AssociationPersons[5].ID + 1, result[6].ID);
				Assert.AreEqual(Gender.Unknown, result[6].Gender);
				Assert.AreEqual("sick", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE: server dies
		[Test]
		public void SameSourceAssociationInInsertPredicate([MergeDataContextSource(
			false,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient.Diagnosis.Contains("sick"),
						s => new Person()
						{
							FirstName = "Inserted 1",
							LastName = "Inserted 2",
							Gender = Gender.Male
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(7, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);

				Assert.AreEqual(AssociationPersons[5].ID + 1, result[6].ID);
				Assert.AreEqual(Gender.Male, result[6].Gender);
				Assert.AreEqual("Inserted 1", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE, DB2: Associations in match not supported
		// Informix: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInMatch([MergeDataContextSource(
			false,
			ProviderName.DB2, ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID
							&& t.Patient.Diagnosis.Contains("very")
							&& s.Patient.Diagnosis.Contains("sick"))
					.UpdateWhenMatched((t, s) => new Person()
					{
						MiddleName = "R.I.P."
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual(AssociationPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(AssociationPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("R.I.P.", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		// ASE: server dies
		// Informix: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInUpdate([MergeDataContextSource(
			false,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatched((t, s) => new Person()
					{
						MiddleName = "first " + s.Patient.Diagnosis,
						LastName = "last " + t.Patient.Diagnosis
					})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("last very sick", result[3].LastName);
				Assert.AreEqual("first very sick", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SameSourceAssociationInUpdateBySource([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.FirstName == "first 3",
						t => new Person()
						{
							FirstName = "Updated",
							LastName = t.Patient.Diagnosis
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);

				Assert.AreEqual(AssociationPersons[2].ID, result[2].ID);
				Assert.AreEqual(AssociationPersons[2].Gender, result[2].Gender);
				Assert.AreEqual("Updated", result[2].FirstName);
				Assert.AreEqual("sick", result[2].LastName);
				Assert.AreEqual(AssociationPersons[2].MiddleName, result[2].MiddleName);

				AssertPerson(AssociationPersons[3], result[3]);
				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void SameSourceAssociationInUpdateBySourcePredicate(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Patient.Diagnosis.Contains("very"),
						t => new Person()
						{
							FirstName = "Updated"
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("Updated", result[3].FirstName);
				Assert.AreEqual(AssociationPersons[3].LastName, result[3].LastName);
				Assert.AreEqual(AssociationPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		// ASE: server dies
		[Test]
		public void SameSourceAssociationInUpdatePredicate([MergeDataContextSource(
			false,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedAnd(
						(t, s) => s.Patient.Diagnosis.Contains("very") && t.Patient.Diagnosis.Contains("very"),
						(t, s) => new Person()
						{
							MiddleName = "Updated"
						})
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(6, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);

				Assert.AreEqual(AssociationPersons[3].ID, result[3].ID);
				Assert.AreEqual(AssociationPersons[3].Gender, result[3].Gender);
				Assert.AreEqual(AssociationPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(AssociationPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("Updated", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test]
		public void TestAssociationsData([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var patients = db.Patient.OrderBy(_ => _.PersonID).ToList();
				var doctors = db.Doctor.OrderBy(_ => _.PersonID).ToList();
				var persons = db.Person.OrderBy(_ => _.ID).ToList();

				Assert.AreEqual(AssociationPersons.Length, persons.Count);
				Assert.AreEqual(AssociationPatients.Length, patients.Count);
				Assert.AreEqual(AssociationDoctors.Length, doctors.Count);

				for (var i = 0; i < persons.Count; i++)
				{
					AssertPerson(AssociationPersons[i], persons[i]);
				}

				for (var i = 0; i < patients.Count; i++)
				{
					Assert.AreEqual(AssociationPatients[i].PersonID, patients[i].PersonID);
					Assert.AreEqual(AssociationPatients[i].Diagnosis, patients[i].Diagnosis);
				}

				for (var i = 0; i < doctors.Count; i++)
				{
					Assert.AreEqual(AssociationDoctors[i].PersonID, doctors[i].PersonID);
					Assert.AreEqual(AssociationDoctors[i].Taxonomy, doctors[i].Taxonomy);
				}
			}
		}

		[Test]
		public void SameSourceAssociationInUpdateWithDeleteDeletePredicate(
			[IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedThenDelete(
						(t, s) => new Person()
						{
							LastName = s.LastName
						},
						(t, s) => s.Patient.Diagnosis == "very sick" && t.Patient.Diagnosis == "very sick")
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[4], result[3]);
				AssertPerson(AssociationPersons[5], result[4]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdateWithDeleteDeletePredicate(
			[IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedThenDelete(
						(t, s) => new Person()
						{
							LastName = s.FirstName
						},
						(t, s) => s.Patient.Diagnosis == "very sick" && t.Patient.Diagnosis == "very sick")
					.Merge();

				var result = db.Person.OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(5, result.Count);

				AssertPerson(AssociationPersons[0], result[0]);
				AssertPerson(AssociationPersons[1], result[1]);
				AssertPerson(AssociationPersons[2], result[2]);
				AssertPerson(AssociationPersons[4], result[3]);
				AssertPerson(AssociationPersons[5], result[4]);
			}
		}

		#region Test Data
		private static readonly Doctor[] AssociationDoctors = new[]
		{
			new Doctor() { PersonID = 3, Taxonomy = "Dr. Lector" },
			new Doctor() { PersonID = 4, Taxonomy = "Dr. who???" },
		};

		private static readonly Patient[] AssociationPatients = new[]
		{
			new Patient() { PersonID = 5, Diagnosis = "sick" },
			new Patient() { PersonID = 6, Diagnosis = "very sick" },
		};

		private static readonly Person[] AssociationPersons = new[]
		{
			new Person() { ID = 1, Gender = Gender.Female,  FirstName = "first 1",  LastName = "last 1" },
			new Person() { ID = 2, Gender = Gender.Male,    FirstName = "first 2",  LastName = "last 2" },
			new Person() { ID = 3, Gender = Gender.Other,   FirstName = "first 3",  LastName = "last 3" },
			new Person() { ID = 4, Gender = Gender.Unknown, FirstName = "first 4",  LastName = "last 4" },
			new Person() { ID = 5, Gender = Gender.Female,  FirstName = "first 5",  LastName = "last 5" },
			new Person() { ID = 6, Gender = Gender.Male,    FirstName = "first 6",  LastName = "last 6" },
		};

		private static void AssertPerson(Person expected, Person actual)
		{
			Assert.AreEqual(expected.ID, actual.ID);
			Assert.AreEqual(expected.Gender, actual.Gender);
			Assert.AreEqual(expected.FirstName, actual.FirstName);
			Assert.AreEqual(expected.LastName, actual.LastName);
			Assert.AreEqual(expected.MiddleName, actual.MiddleName);
		}

		private void PrepareAssociationsData(ITestDataContext db)
		{
			using (new DisableLogging())
			{
				db.Patient.Delete();
				db.Doctor.Delete();
				db.Person.Delete();

				var id = 1;
				foreach (var person in AssociationPersons)
				{
					person.ID = id++;

					person.ID = Convert.ToInt32(db.InsertWithIdentity(person));
				}

				AssociationDoctors[0].PersonID = AssociationPersons[4].ID;
				AssociationDoctors[1].PersonID = AssociationPersons[5].ID;

				foreach (var doctor in AssociationDoctors)
				{
					db.Insert(doctor);
				}

				AssociationPatients[0].PersonID = AssociationPersons[2].ID;
				AssociationPatients[1].PersonID = AssociationPersons[3].ID;

				foreach (var patient in AssociationPatients)
				{
					db.Insert(patient);
				}
			}
		}
		#endregion
	}
}
