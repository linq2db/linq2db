using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	public partial class MergeTests
	{
		[Test, IdentityInsertMergeDataContextSource]
		public void ImplicitIdentityInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var nextId = db.Person.Select(_ => _.ID).Max() + 1;

				var rows = db.Person
					.Merge()
					.Using(
						db.Person.Select(p => new Person()
						{
							ID = p.ID + 50,
							FirstName = p.FirstName,
							LastName = p.LastName,
							Gender = p.Gender,
							MiddleName = p.MiddleName
						}))
					.On((t, s) => t.ID + 50 == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.FirstName == "first 3")
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

				AssociationPersons[2].ID = nextId;
				AssertPerson(AssociationPersons[2], result[6]);
			}
		}

		// ASE: server dies
		[Test, IdentityInsertMergeDataContextSource(ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void ExplicitIdentityInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var nextId = db.Person.Select(_ => _.ID).Max() + 1;

				var rows = db.Person
					.Merge()
					.Using(db.Person)
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient.Diagnosis.Contains("sick")
						, s => new Model.Person()
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
		[Test, IdentityInsertMergeDataContextSource(ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void ExplicitNoIdentityInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareAssociationsData(db);

				var nextId = db.Person.Select(_ => _.ID).Max() + 1;

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

				AssertRowCount(1, rows, context);

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

		// see https://github.com/linq2db/linq2db/issues/914
		// rationale:
		// we shouldn't ignore SkipOnInsert attribute for insert operation with implicit field list
		[Test, IdentityInsertMergeDataContextSource]
		public void ImplicitInsertIdentityWithSkipOnInsert(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var table = db.GetTable<TestMappingWithIdentity>();
				table.Delete();

				db.Insert(new TestMappingWithIdentity());

				var lastId = table.Select(_ => _.Id).Max();

				var source = new[]
				{
					new TestMappingWithIdentity()
					{
						Field = 22,
					},
					new TestMappingWithIdentity()
					{
						Field = 23
					}
				};

				var rows = table
					.Merge()
					.Using(source)
					.On((s, t) => s.Field == t.Field)
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(3, result.Count);

				var newRecord = new TestMapping1();

				Assert.AreEqual(lastId, result[0].Id);
				Assert.AreEqual(null, result[0].Field);
				Assert.AreEqual(lastId + 1, result[1].Id);
				Assert.AreEqual(22, result[1].Field);
				Assert.AreEqual(lastId + 2, result[2].Id);
				Assert.AreEqual(23, result[2].Field);
			}
		}
	}
}
