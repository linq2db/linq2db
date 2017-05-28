using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		// DB2, Oracle: not supported
		[MergeDataContextSource(ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird)]
		public void ImlicitIdentityInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var rows = db.Person
					.FromSame(
						db.Person.Select(p => new Model.Person()
						{
							ID = p.ID + 50,
							FirstName = p.FirstName,
							LastName = p.LastName,
							Gender = p.Gender,
							MiddleName = p.MiddleName
						}),
						(t, s) => t.ID + 50 == s.ID && t.FirstName != "first 3")
					.Insert(s => s.FirstName == "first 3")
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

				AssociationPersons[2].ID += 50;
				AssertPerson(AssociationPersons[2], result[6]);
			}
		}

		// identity DB2: insert for DB2 is not supported for now (some db2 versions support it)
		// ASE: server just dies ("Enterprise Quality")
		// Oracle: not supported
		[MergeDataContextSource(ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS, ProviderName.Sybase,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird)]
		public void ExplicitIdentityInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var nextId = db.Person.Select(_ => _.ID).Max() + 1;

				var rows = db.Person
					.FromSame(db.Person, (t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.Insert(s => s.Patient.Diagnosis.Contains("sick"), s => new Model.Person()
					{
						ID = nextId + 1,
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

				Assert.AreEqual(nextId + 1, result[6].ID);
				Assert.AreEqual(Gender.Male, result[6].Gender);
				Assert.AreEqual("Inserted 1", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}

		// ASE: server dies
		[MergeDataContextSource(ProviderName.Sybase, ProviderName.Firebird)]
		public void ExplicitNoIdentityInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var nextId = db.Person.Select(_ => _.ID).Max() + 1;

				var rows = db.Person
					.FromSame(db.Person, (t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.Insert(s => s.Patient.Diagnosis.Contains("sick"), s => new Model.Person()
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

				Assert.AreEqual(nextId, result[6].ID);
				Assert.AreEqual(Gender.Male, result[6].Gender);
				Assert.AreEqual("Inserted 1", result[6].FirstName);
				Assert.AreEqual("Inserted 2", result[6].LastName);
				Assert.IsNull(result[6].MiddleName);
			}
		}
	}
}
