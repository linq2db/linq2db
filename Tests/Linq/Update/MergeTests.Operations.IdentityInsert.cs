using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void ImplicitIdentityInsert([IdentityInsertMergeDataContextSource(false)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var nextId = db.GetTable<MPerson>().Select(_ => _.ID).Max() + 1;

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(
						db.GetTable<MPerson>().Select(p => new MPerson()
						{
							ID         = p.ID + 50,
							FirstName  = p.FirstName,
							LastName   = p.LastName,
							Gender     = p.Gender,
							MiddleName = p.MiddleName
						}))
					.On((t, s) => t.ID + 50 == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.FirstName == "first 3")
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(7));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				IdentityPersons[2].ID = nextId;
				AssertPerson(IdentityPersons[2], result[6]);
			}
		}

		[Test]
		public void ExplicitIdentityInsert([IdentityInsertMergeDataContextSource(false, TestProvName.AllSybase)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var nextId = db.GetTable<MPerson>().Select(_ => _.ID).Max() + 1;

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient!.Diagnosis.Contains("sick"),
						s => new MPerson()
						{
							ID        = nextId + 1,
							FirstName = "Inserted 1",
							LastName  = "Inserted 2",
							Gender    = Gender.Male
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(7));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.Multiple(() =>
				{
					Assert.That(result[6].ID, Is.EqualTo(nextId + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Male));
					Assert.That(result[6].FirstName, Is.EqualTo("Inserted 1"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		[Test]
		public void ExplicitNoIdentityInsert([IdentityInsertMergeDataContextSource(false, TestProvName.AllSybase)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var nextId = db.GetTable<MPerson>().Select(_ => _.ID).Max() + 1;

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient!.Diagnosis.Contains("sick"),
						s => new MPerson()
						{
							FirstName = "Inserted 1",
							LastName  = "Inserted 2",
							Gender    = Gender.Male
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(7));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.Multiple(() =>
				{
					Assert.That(result[6].ID, Is.EqualTo(nextId));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Male));
					Assert.That(result[6].FirstName, Is.EqualTo("Inserted 1"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		// see https://github.com/linq2db/linq2db/issues/914
		// rationale:
		// we shouldn't ignore SkipOnInsert attribute for insert operation with implicit field list
		[Test]
		public void ImplicitInsertIdentityWithSkipOnInsert(
			[IdentityInsertMergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
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

				Assert.That(result, Has.Count.EqualTo(3));

				var newRecord = new TestMapping1();

				Assert.Multiple(() =>
				{
					Assert.That(result[0].Id, Is.EqualTo(lastId));
					Assert.That(result[0].Field, Is.EqualTo(null));
					Assert.That(result[1].Id, Is.EqualTo(lastId + 1));
					Assert.That(result[1].Field, Is.EqualTo(22));
					Assert.That(result[2].Id, Is.EqualTo(lastId + 2));
					Assert.That(result[2].Field, Is.EqualTo(23));
				});
			}
		}

		#region Test Data (Identity/Association Tests)
		[Table("Doctor")]
		public class MDoctor
		{
			[PrimaryKey] public int    PersonID;
			[Column    ] public string Taxonomy = null!;
		}

		[Table("Patient")]
		public class MPatient
		{
			[PrimaryKey] public int    PersonID;
			[Column    ] public string Diagnosis = null!;

			[Association(ThisKey = nameof(PersonID), OtherKey = nameof(MPerson.ID), CanBeNull = false)]
			public MPerson Person = null!;
		}

		[Table("Person")]
		public class MPerson
		{
			[Column("PersonID", Configuration = ProviderName.ClickHouse)]
			[Column("PersonID", IsIdentity = true), PrimaryKey]
														   public int     ID;
			[Column(CanBeNull = false)                   ] public string  FirstName { get; set; } = null!;
			[Column(CanBeNull = false)                   ] public string  LastName = null!;
			[Column                                      ] public string? MiddleName;
			[Column(DataType = DataType.Char, Length = 1)] public Gender  Gender;

			[Association(ThisKey = "ID", OtherKey = "PersonID", CanBeNull = false)]
			public MPatient Patient = null!;

			[Association(QueryExpressionMethod = nameof(Query), CanBeNull = false)]
			public MPatient PatientQuery = null!;

			static Expression<Func<MPerson, IDataContext, IQueryable<MPatient>>> Query
				=> (t, ctx) => ctx.GetTable<MPatient>().Where(p => p.PersonID == t.ID);
		}

		private static readonly MDoctor[] IdentityDoctors = new[]
		{
			new MDoctor() { PersonID = 105, Taxonomy = "Dr. Lector" },
			new MDoctor() { PersonID = 106, Taxonomy = "Dr. who???" },
		};

		private static readonly MPatient[] IdentityPatients = new[]
		{
			new MPatient() { PersonID = 102, Diagnosis = "sick" },
			new MPatient() { PersonID = 103, Diagnosis = "very sick" },
		};

		private static readonly MPerson[] IdentityPersons = new[]
		{
			new MPerson() { ID = 101, Gender = Gender.Female,  FirstName = "first 1",  LastName = "last 1" },
			new MPerson() { ID = 102, Gender = Gender.Male,    FirstName = "first 2",  LastName = "last 2" },
			new MPerson() { ID = 103, Gender = Gender.Other,   FirstName = "first 3",  LastName = "last 3" },
			new MPerson() { ID = 104, Gender = Gender.Unknown, FirstName = "first 4",  LastName = "last 4" },
			new MPerson() { ID = 105, Gender = Gender.Female,  FirstName = "first 5",  LastName = "last 5" },
			new MPerson() { ID = 106, Gender = Gender.Male,    FirstName = "first 6",  LastName = "last 6" },
		};

		private static void AssertPerson(MPerson expected, MPerson actual)
		{
			Assert.Multiple(() =>
			{
				Assert.That(actual.ID, Is.EqualTo(expected.ID));
				Assert.That(actual.Gender, Is.EqualTo(expected.Gender));
				Assert.That(actual.FirstName, Is.EqualTo(expected.FirstName));
				Assert.That(actual.LastName, Is.EqualTo(expected.LastName));
				Assert.That(actual.MiddleName, Is.EqualTo(expected.MiddleName));
			});
		}

		private void PrepareIdentityData(ITestDataContext db, string context)
		{
			using var _1 = new DisableBaseline("Test Setup");
			using var _2 = new DisableLogging();

			db.Patient.Delete();
			db.Doctor .Delete();
			db.Person .Delete();

			var id = 1;
			foreach (var person in IdentityPersons)
			{
				person.ID = id++;

				person.ID = Convert.ToInt32(db.InsertWithIdentity(person));
			}

			IdentityDoctors[0].PersonID = IdentityPersons[4].ID;
			IdentityDoctors[1].PersonID = IdentityPersons[5].ID;

			foreach (var doctor in IdentityDoctors)
				db.Insert(doctor);

			IdentityPatients[0].PersonID = IdentityPersons[2].ID;
			IdentityPatients[1].PersonID = IdentityPersons[3].ID;

			foreach (var patient in IdentityPatients)
				db.Insert(patient);
		}
		#endregion
	}
}
