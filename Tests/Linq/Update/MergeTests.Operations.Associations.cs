using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Model;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void TargetAssociation([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[4], result[3]);
				AssertPerson(IdentityPersons[5], result[4]);
			}
		}

		[Test]
		public void TargetQueryAssociation([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

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

				Assert.AreEqual(1, cnt);
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

				Assert.AreEqual(1, cnt);
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

				Assert.AreEqual(5, cnt);
			}
		}

		[Test]
		public void OtherSourceAssociationInDeleteBySourcePredicate([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(1, result.Count);

				Assert.AreEqual(IdentityPatients[0].PersonID, result[0].PersonID);
				Assert.AreEqual(IdentityPatients[0].Diagnosis, result[0].Diagnosis);
			}
		}

		// ASE: server dies
		// Oracle: associations in insert setter
		[Test]
		public void OtherSourceAssociationInInsertCreate([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.AreEqual(IdentityPersons[5].ID + 1, result[6].ID);
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
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix, TestProvName.AllSapHana)]
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.AreEqual(IdentityPersons[5].ID + 1, result[6].ID);
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
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(7, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.AreEqual(IdentityPersons[5].ID + 1, result[6].ID);
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

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual(IdentityPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(IdentityPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("R.I.P.", result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		// ASE: server dies
		// Informix: associations doesn't work right now
		[Test]
		public void OtherSourceAssociationInUpdate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix)]
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

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("last very sick", result[3].LastName);
				Assert.AreEqual("first very sick", result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdateBySource([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);

				Assert.AreEqual(IdentityPersons[2].ID, result[2].ID);
				Assert.AreEqual(IdentityPersons[2].Gender, result[2].Gender);
				Assert.AreEqual("Updated", result[2].FirstName);
				Assert.AreEqual("sick", result[2].LastName);
				Assert.AreEqual(IdentityPersons[2].MiddleName, result[2].MiddleName);

				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void OtherSourceAssociationInUpdateBySourcePredicate(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("Updated", result[3].FirstName);
				Assert.AreEqual(IdentityPersons[3].LastName, result[3].LastName);
				Assert.AreEqual(IdentityPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		// ASE: server dies
		[Test]
		public void OtherSourceAssociationInUpdatePredicate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("Updated", result[3].LastName);
				Assert.AreEqual(IdentityPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInDeleteBySourcePredicate(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(5, result.Count);

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
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(1, result.Count);

				Assert.AreEqual(IdentityPatients[0].PersonID, result[0].PersonID);
				Assert.AreEqual(IdentityPatients[0].Diagnosis, result[0].Diagnosis);
			}
		}

		// ASE: server dies
		// Oracle: associations in instert setters
		[Test]
		public void SameSourceAssociationInInsertCreate([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.AreEqual(IdentityPersons[5].ID + 1, result[6].ID);
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
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix, TestProvName.AllSapHana)]
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(7, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.AreEqual(IdentityPersons[5].ID + 1, result[6].ID);
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
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(7, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);
				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);

				Assert.AreEqual(IdentityPersons[5].ID + 1, result[6].ID);
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

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual(IdentityPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(IdentityPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("R.I.P.", result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		// ASE: server dies
		// Informix: associations doesn't work right now
		[Test]
		public void SameSourceAssociationInUpdate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix)]
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

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("first 4", result[3].FirstName);
				Assert.AreEqual("last very sick", result[3].LastName);
				Assert.AreEqual("first very sick", result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInUpdateBySource([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);

				Assert.AreEqual(IdentityPersons[2].ID, result[2].ID);
				Assert.AreEqual(IdentityPersons[2].Gender, result[2].Gender);
				Assert.AreEqual("Updated", result[2].FirstName);
				Assert.AreEqual("sick", result[2].LastName);
				Assert.AreEqual(IdentityPersons[2].MiddleName, result[2].MiddleName);

				AssertPerson(IdentityPersons[3], result[3]);
				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		[Test]
		public void SameSourceAssociationInUpdateBySourcePredicate(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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

				Assert.AreEqual(1, rows);

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual("Updated", result[3].FirstName);
				Assert.AreEqual(IdentityPersons[3].LastName, result[3].LastName);
				Assert.AreEqual(IdentityPersons[3].MiddleName, result[3].MiddleName);

				AssertPerson(IdentityPersons[4], result[4]);
				AssertPerson(IdentityPersons[5], result[5]);
			}
		}

		// ASE: server dies
		[Test]
		public void SameSourceAssociationInUpdatePredicate([MergeDataContextSource(
			false,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, ProviderName.Firebird)]
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

				Assert.AreEqual(6, result.Count);

				AssertPerson(IdentityPersons[0], result[0]);
				AssertPerson(IdentityPersons[1], result[1]);
				AssertPerson(IdentityPersons[2], result[2]);

				Assert.AreEqual(IdentityPersons[3].ID, result[3].ID);
				Assert.AreEqual(IdentityPersons[3].Gender, result[3].Gender);
				Assert.AreEqual(IdentityPersons[3].FirstName, result[3].FirstName);
				Assert.AreEqual(IdentityPersons[3].LastName, result[3].LastName);
				Assert.AreEqual("Updated", result[3].MiddleName);

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

				Assert.AreEqual(IdentityPersons.Length, persons.Count);
				Assert.AreEqual(IdentityPatients.Length, patients.Count);
				Assert.AreEqual(IdentityDoctors.Length, doctors.Count);

				for (var i = 0; i < persons.Count; i++)
				{
					AssertPerson(IdentityPersons[i], persons[i]);
				}

				for (var i = 0; i < patients.Count; i++)
				{
					Assert.AreEqual(IdentityPatients[i].PersonID, patients[i].PersonID);
					Assert.AreEqual(IdentityPatients[i].Diagnosis, patients[i].Diagnosis);
				}

				for (var i = 0; i < doctors.Count; i++)
				{
					Assert.AreEqual(IdentityDoctors[i].PersonID, doctors[i].PersonID);
					Assert.AreEqual(IdentityDoctors[i].Taxonomy, doctors[i].Taxonomy);
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

				Assert.AreEqual(5, result.Count);

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

				Assert.AreEqual(5, result.Count);

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
