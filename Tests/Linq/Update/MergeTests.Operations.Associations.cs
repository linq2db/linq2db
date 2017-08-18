using System;
using System.Linq;
using Tests.Model;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		[Test, MergeBySourceDataContextSource]
		public void OtherSourceAssociationInDeleteBySourcePredicate(string context)
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

		// ASE: server dies
		[Test, MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void OtherSourceAssociationInDeletePredicate(string context)
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
		[Test, MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void OtherSourceAssociationInInsertCreate(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.Patient.Diagnosis.Contains("sick"), s => new Model.Person()
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
		[Test, MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana)]
		public void OtherSourceAssociationInInsertCreate2(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatched(s => new Model.Person()
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
		[Test, MergeDataContextSource(ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void OtherSourceAssociationInInsertPredicate(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.Patient.Diagnosis.Contains("sick"), s => new Model.Person()
					{
						FirstName = "Inserted 1",
						LastName = "Inserted 2",
						Gender = Gender.Male
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
				Assert.AreEqual(Gender.Male, result[6].Gender);
				Assert.AreEqual("Inserted 1", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE, DB2: Associations in match not supported
		// Informix: associations doesn't work right now
		[Test, MergeDataContextSource(ProviderName.DB2, ProviderName.Sybase, ProviderName.Informix)]
		public void OtherSourceAssociationInMatch(string context)
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

				Assert.AreEqual(1, rows);

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
		[Test, MergeDataContextSource(ProviderName.Sybase, ProviderName.Informix)]
		public void OtherSourceAssociationInUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatched((t, s) => new Model.Person()
					{
						MiddleName = "first " + s.Patient.Diagnosis,
						LastName = "last " + t.Patient.Diagnosis
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
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("last very sick", result[3].LastName);
				Assert.AreEqual("first very sick", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, MergeBySourceDataContextSource]
		public void OtherSourceAssociationInUpdateBySource(string context)
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
						t => new Model.Person()
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

		[Test, MergeBySourceDataContextSource]
		public void OtherSourceAssociationInUpdateBySourcePredicate(string context)
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
						t => new Model.Person()
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
		[Test, MergeDataContextSource(ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void OtherSourceAssociationInUpdatePredicate(string context)
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
						(t, s) => new Model.Person()
						{
							LastName = "Updated"
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
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("Updated", result[3].LastName);
				Assert.AreEqual(AssociationPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, MergeBySourceDataContextSource]
		public void SameSourceAssociationInDeleteBySourcePredicate(string context)
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

		// ASE: server dies
		[Test, MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void SameSourceAssociationInDeletePredicate(string context)
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
		[Test, MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void SameSourceAssociationInInsertCreate(string context)
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
						s => new Model.Person()
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
		[Test, MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana)]
		public void SameSourceAssociationInInsertCreate2(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatched(s => new Model.Person()
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
		[Test, MergeDataContextSource(ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void SameSourceAssociationInInsertPredicate(string context)
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
						s => new Model.Person()
						{
							FirstName = "Inserted 1",
							LastName = "Inserted 2",
							Gender = Gender.Male
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
				Assert.AreEqual(Gender.Male, result[6].Gender);
				Assert.AreEqual("Inserted 1", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE, DB2: Associations in match not supported
		// Informix: associations doesn't work right now
		[Test, MergeDataContextSource(ProviderName.DB2, ProviderName.Sybase, ProviderName.Informix)]
		public void SameSourceAssociationInMatch(string context)
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

				Assert.AreEqual(1, rows);

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
		[Test, MergeDataContextSource(ProviderName.Sybase, ProviderName.Informix)]
		public void SameSourceAssociationInUpdate(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatched((t, s) => new Model.Person()
					{
						MiddleName = "first " + s.Patient.Diagnosis,
						LastName = "last " + t.Patient.Diagnosis
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
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("last very sick", result[3].LastName);
				Assert.AreEqual("first very sick", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, MergeBySourceDataContextSource]
		public void SameSourceAssociationInUpdateBySource(string context)
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
						t => new Model.Person()
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

		[Test, MergeBySourceDataContextSource]
		public void SameSourceAssociationInUpdateBySourcePredicate(string context)
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
						t => new Model.Person()
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
		[Test, MergeDataContextSource(ProviderName.Sybase, ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
		public void SameSourceAssociationInUpdatePredicate(string context)
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
						(t, s) => new Model.Person()
						{
							MiddleName = "Updated"
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
				Assert.AreEqual(AssociationPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(AssociationPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("Updated", result[3].MiddleName);

				AssertPerson(AssociationPersons[4], result[4]);
				AssertPerson(AssociationPersons[5], result[5]);
			}
		}

		[Test, DataContextSource(false)]
		public void TestAssociationsData(string context)
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

		[Test, MergeUpdateWithDeleteDataContextSource]
		public void SameSourceAssociationInUpdateWithDeleteDeletePredicate(string context)
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
						(t, s) => new Model.Person()
						{
							LastName = s.LastName
						},
						(t, s) => s.Patient.Diagnosis == "very sick" && t.Patient.Diagnosis == "very sick")
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

		[Test, MergeUpdateWithDeleteDataContextSource]
		public void OtherSourceAssociationInUpdateWithDeleteDeletePredicate(string context)
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
						(t, s) => new Model.Person()
						{
							LastName = s.FirstName
						},
						(t, s) => s.Patient.Diagnosis == "very sick" && t.Patient.Diagnosis == "very sick")
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

		private void PrepareAssociationsData(TestDataConnection db)
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

					if (db.ConfigurationString == TestProvName.Firebird3 + "sdf")
						person.ID = Convert.ToInt32(db.Insert(person));
					else
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
