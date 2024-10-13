using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Model;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void TargetAssociation([MergeNotMatchedBySourceDataContextSource(true, TestProvName.AllPostgreSQL17Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient!.Diagnosis.Contains("very"))
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		[Test]
		public void TargetQueryAssociation([MergeNotMatchedBySourceDataContextSource(true, TestProvName.AllPostgreSQL17Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.PatientQuery.Diagnosis.Contains("very"))
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		[Test]
		public void SourceAssociationAsInnerJoin1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var parsons = db.GetTable<MPerson>().ToArray();
				var patients = db.GetTable<MPatient>().ToArray();

				var cnt = db.GetTable<MPerson>()
					.Merge()
					// inner join performed in source
					.Using(db.GetTable<MPerson>().Select(p => new { p.ID, p.Patient!.Diagnosis }))
					.On((t, s) => t.ID == s.ID)
					.DeleteWhenMatchedAnd((t, s) => s.Diagnosis != "sick")
					.Merge();

				Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void SourceAssociationAsInnerJoin2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var parsons = db.GetTable<MPerson>().ToArray();
				var patients = db.GetTable<MPatient>().ToArray();

				var cnt = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					// inner join still performed in source
					.On((t, s) => t.ID == s.ID && s.Patient!.Diagnosis != null)
					.DeleteWhenMatchedAnd((t, s) => s.Patient!.Diagnosis != "sick")
					.Merge();

				Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void SourceAssociationAsOuterJoin([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var parsons = db.GetTable<MPerson>().ToArray();
				var patients = db.GetTable<MPatient>().ToArray();

				var cnt = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID)
					// // inner join promoted to outer join
					.DeleteWhenMatchedAnd((t, s) => s.Patient!.Diagnosis != "sick")
					.Merge();

				Assert.That(cnt, Is.EqualTo(5));
			}
		}

		[Test]
		public void OtherSourceAssociationInDeleteBySourcePredicate([MergeNotMatchedBySourceDataContextSource(true, TestProvName.AllPostgreSQL17Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient!.Diagnosis.Contains("very"))
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		[Test]
		public void OtherSourceAssociationInDeletePredicate([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, TestProvName.AllFirebird)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPatient>()
					.Merge()
					.Using(db.GetTable<MPatient>())
					.On((t, s) => t.PersonID == s.PersonID && s.Diagnosis.Contains("very"))
					.DeleteWhenMatchedAnd((t, s) => s.Person.FirstName == "first 4" && t.Person.FirstName == "first 4")
					.Merge();

				var result = db.GetTable<MPatient>().OrderBy(_ => _.PersonID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(1));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].PersonID, Is.EqualTo(IdentityPatients[0].PersonID));
					Assert.That(result[0].Diagnosis, Is.EqualTo(IdentityPatients[0].Diagnosis));
				});
			}
		}

		// Oracle: associations in insert setter
		[Test]
		public void OtherSourceAssociationInInsertCreate([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.Patient!.Diagnosis.Contains("sick"), s => new MPerson()
					{
						FirstName = s.Patient!.Diagnosis,
						LastName = "Inserted 2",
						Gender = Gender.Unknown
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
					Assert.That(result[6].ID, Is.EqualTo(IdentityPersons[5].ID + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Unknown));
					Assert.That(result[6].FirstName, Is.EqualTo("sick"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		// Oracle: associations in insert setters
		// Informix: associations doesn't work right now
		// SAP: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInInsertCreate2([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllInformix, TestProvName.AllSapHana)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatched(s => new MPerson()
					{
						FirstName = s.Patient!.Diagnosis,
						LastName = "Inserted 2",
						Gender = Gender.Unknown
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
					Assert.That(result[6].ID, Is.EqualTo(IdentityPersons[5].ID + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Unknown));
					Assert.That(result[6].FirstName, Is.EqualTo("sick"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		[Test]
		public void OtherSourceAssociationInInsertPredicate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(s => s.Patient!.Diagnosis.Contains("sick"), s => new MPerson()
					{
						FirstName = "Inserted 1",
						LastName = "Inserted 2",
						Gender = Gender.Male
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
					Assert.That(result[6].ID, Is.EqualTo(IdentityPersons[5].ID + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Male));
					Assert.That(result[6].FirstName, Is.EqualTo("Inserted 1"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		// ASE, DB2: Associations in match not supported
		// Informix: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInMatch([MergeDataContextSource(
			false,
			ProviderName.DB2, TestProvName.AllSybase, TestProvName.AllInformix)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID
							&& t.Patient!.Diagnosis.Contains("very")
							&& s.Patient!.Diagnosis.Contains("sick"))
					.UpdateWhenMatched((t, s) => new MPerson()
					{
						MiddleName = "R.I.P."
					})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo(IdentityPersons[3].FirstName));
					Assert.That(result[3].LastName, Is.EqualTo(IdentityPersons[3].LastName));
					Assert.That(result[3].MiddleName, Is.EqualTo("R.I.P."));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		// Informix: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInUpdate([MergeDataContextSource(false, TestProvName.AllInformix)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatched((t, s) => new MPerson()
					{
						MiddleName = "first " + s.Patient!.Diagnosis,
						LastName = "last " + t.Patient!.Diagnosis
					})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo("first 4"));
					Assert.That(result[3].LastName, Is.EqualTo("last very sick"));
					Assert.That(result[3].MiddleName, Is.EqualTo("first very sick"));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdateBySource([MergeNotMatchedBySourceDataContextSource(true)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(t => t.FirstName == "first 3",
						t => new MPerson()
						{
							FirstName = "Updated",
							LastName = t.Patient!.Person.Patient!.Diagnosis
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(6));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);

				Assert.Multiple(() =>
				{
					Assert.That(result[2].ID, Is.EqualTo(IdentityPersons[2].ID));
					Assert.That(result[2].Gender, Is.EqualTo(IdentityPersons[2].Gender));
					Assert.That(result[2].FirstName, Is.EqualTo("Updated"));
					Assert.That(result[2].LastName, Is.EqualTo("sick"));
					Assert.That(result[2].MiddleName, Is.EqualTo(IdentityPersons[2].MiddleName));
				});

				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdateBySourcePredicate([MergeNotMatchedBySourceDataContextSource(true)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(t => t.Patient!.Diagnosis.Contains("very"),
						t => new MPerson()
						{
							FirstName = "Updated"
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(6));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo("Updated"));
					Assert.That(result[3].LastName, Is.EqualTo(IdentityPersons[3].LastName));
					Assert.That(result[3].MiddleName, Is.EqualTo(IdentityPersons[3].MiddleName));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdatePredicate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedAnd(
						(t, s) => s.Patient!.Diagnosis == t.Patient!.Diagnosis && t.Patient!.Diagnosis.Contains("very"),
						(t, s) => new MPerson()
						{
							LastName = "Updated"
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo("first 4"));
					Assert.That(result[3].LastName, Is.EqualTo("Updated"));
					Assert.That(result[3].MiddleName, Is.EqualTo(IdentityPersons[3].MiddleName));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInDeleteBySourcePredicate([MergeNotMatchedBySourceDataContextSource(true, TestProvName.AllPostgreSQL17Plus)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Patient!.Diagnosis.Contains("very"))
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(5));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		[Test]
		public void SameSourceAssociationInDeletePredicate([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPatient>()
					.Merge()
					.Using(db.GetTable<MPatient>())
					.On((t, s) => t.PersonID == s.PersonID && s.Diagnosis.Contains("very"))
					.DeleteWhenMatchedAnd((t, s) => s.Person.FirstName == "first 4" && t.Person.FirstName == "first 4")
					.Merge();

				var result = db.GetTable<MPatient>().OrderBy(_ => _.PersonID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(1));
				});

				Assert.Multiple(() =>
				{
					Assert.That(result[0].PersonID, Is.EqualTo(IdentityPatients[0].PersonID));
					Assert.That(result[0].Diagnosis, Is.EqualTo(IdentityPatients[0].Diagnosis));
				});
			}
		}

		// Oracle: associations in instert setters
		[Test]
		public void SameSourceAssociationInInsertCreate([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient!.Diagnosis.Contains("sick"),
						s => new MPerson()
						{
							FirstName = s.Patient!.Diagnosis,
							LastName = "Inserted 2",
							Gender = Gender.Unknown
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
					Assert.That(result[6].ID, Is.EqualTo(IdentityPersons[5].ID + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Unknown));
					Assert.That(result[6].FirstName, Is.EqualTo("sick"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		// Oracle: associations in instert setters
		// Informix: associations doesn't work right now
		// SAP: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInInsertCreate2([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllInformix, TestProvName.AllSapHana)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatched(s => new MPerson()
					{
						FirstName = s.Patient!.Diagnosis,
						LastName = "Inserted 2",
						Gender = Gender.Unknown
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
					Assert.That(result[6].ID, Is.EqualTo(IdentityPersons[5].ID + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Unknown));
					Assert.That(result[6].FirstName, Is.EqualTo("sick"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		[Test]
		public void SameSourceAssociationInInsertPredicate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && t.FirstName != "first 3")
					.InsertWhenNotMatchedAnd(
						s => s.Patient!.Diagnosis.Contains("sick"),
						s => new MPerson()
						{
							FirstName = "Inserted 1",
							LastName = "Inserted 2",
							Gender = Gender.Male
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
					Assert.That(result[6].ID, Is.EqualTo(IdentityPersons[5].ID + 1));
					Assert.That(result[6].Gender, Is.EqualTo(Gender.Male));
					Assert.That(result[6].FirstName, Is.EqualTo("Inserted 1"));
					Assert.That(result[6].LastName, Is.EqualTo("Inserted 2"));
					Assert.That(result[6].MiddleName, Is.Null);
				});
			}
		}

		// ASE, DB2: Associations in match not supported
		// Informix: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInMatch([MergeDataContextSource(
			false,
			ProviderName.DB2, TestProvName.AllSybase, TestProvName.AllInformix)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID
							&& t.Patient!.Diagnosis.Contains("very")
							&& s.Patient!.Diagnosis.Contains("sick"))
					.UpdateWhenMatched((t, s) => new MPerson()
					{
						MiddleName = "R.I.P."
					})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo(IdentityPersons[3].FirstName));
					Assert.That(result[3].LastName, Is.EqualTo(IdentityPersons[3].LastName));
					Assert.That(result[3].MiddleName, Is.EqualTo("R.I.P."));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		// Informix: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInUpdate([MergeDataContextSource(false, TestProvName.AllInformix)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatched((t, s) => new MPerson()
					{
						MiddleName = "first " + s.Patient!.Diagnosis,
						LastName = "last " + t.Patient!.Diagnosis
					})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo("first 4"));
					Assert.That(result[3].LastName, Is.EqualTo("last very sick"));
					Assert.That(result[3].MiddleName, Is.EqualTo("first very sick"));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInUpdateBySource([MergeNotMatchedBySourceDataContextSource(true)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.FirstName == "first 3",
						t => new MPerson()
						{
							FirstName = "Updated",
							LastName = t.Patient!.Diagnosis
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(6));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);

				Assert.Multiple(() =>
				{
					Assert.That(result[2].ID, Is.EqualTo(IdentityPersons[2].ID));
					Assert.That(result[2].Gender, Is.EqualTo(IdentityPersons[2].Gender));
					Assert.That(result[2].FirstName, Is.EqualTo("Updated"));
					Assert.That(result[2].LastName, Is.EqualTo("sick"));
					Assert.That(result[2].MiddleName, Is.EqualTo(IdentityPersons[2].MiddleName));
				});

				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInUpdateBySourcePredicate([MergeNotMatchedBySourceDataContextSource(true)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID + 10)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Patient!.Diagnosis.Contains("very"),
						t => new MPerson()
						{
							FirstName = "Updated"
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(6));
				});

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo("Updated"));
					Assert.That(result[3].LastName, Is.EqualTo(IdentityPersons[3].LastName));
					Assert.That(result[3].MiddleName, Is.EqualTo(IdentityPersons[3].MiddleName));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInUpdatePredicate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird25)]
			string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedAnd(
						(t, s) => s.Patient!.Diagnosis.Contains("very") && t.Patient!.Diagnosis.Contains("very"),
						(t, s) => new MPerson()
						{
							MiddleName = "Updated"
						})
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].ID, Is.EqualTo(IdentityPersons[3].ID));
					Assert.That(result[3].Gender, Is.EqualTo(IdentityPersons[3].Gender));
					Assert.That(result[3].FirstName, Is.EqualTo(IdentityPersons[3].FirstName));
					Assert.That(result[3].LastName, Is.EqualTo(IdentityPersons[3].LastName));
					Assert.That(result[3].MiddleName, Is.EqualTo("Updated"));
				});

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void TestAssociationsData([DataSources(false, TestProvName.AllClickHouse)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var patients = db.GetTable<MPatient>().OrderBy(_ => _.PersonID).ToList();
				var doctors = db.GetTable<MDoctor>().OrderBy(_ => _.PersonID).ToList();
				var persons = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(persons, Has.Count.EqualTo(IdentityPersons.Length));
					Assert.That(patients, Has.Count.EqualTo(IdentityPatients.Length));
					Assert.That(doctors, Has.Count.EqualTo(IdentityDoctors.Length));
				});

				for (var i = 0; i < persons.Count; i++)
				{
					AssertPerson(IdentityPersons[i], persons[i]);
				}

				for (var i = 0; i < patients.Count; i++)
				{
					Assert.Multiple(() =>
					{
						Assert.That(patients[i].PersonID, Is.EqualTo(IdentityPatients[i].PersonID));
						Assert.That(patients[i].Diagnosis, Is.EqualTo(IdentityPatients[i].Diagnosis));
					});
				}

				for (var i = 0; i < doctors.Count; i++)
				{
					Assert.Multiple(() =>
					{
						Assert.That(doctors[i].PersonID, Is.EqualTo(IdentityDoctors[i].PersonID));
						Assert.That(doctors[i].Taxonomy, Is.EqualTo(IdentityDoctors[i].Taxonomy));
					});
				}
			}
		}

		[Test]
		public void SameSourceAssociationInUpdateWithDeleteDeletePredicate(
			[IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedThenDelete(
						(t, s) => new MPerson()
						{
							LastName = s.LastName
						},
						(t, s) => s.Patient!.Diagnosis == "very sick" && t.Patient!.Diagnosis == "very sick")
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(5));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdateWithDeleteDeletePredicate(
			[IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				PrepareIdentityData(db, context);

				var rows = db.GetTable<MPerson>()
					.Merge()
					.Using(db.GetTable<MPerson>())
					.On((t, s) => t.ID == s.ID && s.FirstName == "first 4")
					.UpdateWhenMatchedThenDelete(
						(t, s) => new MPerson()
						{
							LastName = s.FirstName
						},
						(t, s) => s.Patient!.Diagnosis == "very sick" && t.Patient!.Diagnosis == "very sick")
					.Merge();

				var result = db.GetTable<MPerson>().OrderBy(_ => _.ID).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(5));

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		#region Test Data

		#endregion
	}
}
