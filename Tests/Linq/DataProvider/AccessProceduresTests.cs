using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;
using Tests.Model;
using System.Linq;
using Tests.UserTests;
using System.Collections.Generic;
using LinqToDB.Tools.Comparers;

namespace Tests.DataProvider
{
	[TestFixture]
	public class AccessProceduresTests : DataProviderTestBase
	{
		[Test]
		public void Test_Person_Delete([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var id = db.Person.Insert(() => new Person()
				{
					FirstName  = "first name",
					LastName   = "last name",
					MiddleName = "middle name",
					Gender     = Gender.Female,
				});

				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id).Count());

				var cnt = Person_Delete(db, id, context == ProviderName.AccessOdbc);

				Assert.AreEqual(0, db.Person.Where(_ => _.ID == id).Count());
				Assert.AreEqual(1, cnt);
			}
		}

		[Test]
		public void Test_Person_Update([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var id = db.Person.Insert(() => new Person()
				{
					FirstName  = "first name",
					LastName   = "last name",
					MiddleName = "middle name",
					Gender     = Gender.Female,
				});

				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id).Count());

				var cnt = Person_Update(db, id, "new first", "new middle", "new last", 'U', context == ProviderName.AccessOdbc);

				Assert.AreEqual(1, cnt);

				var record = db.Person.Where(_ => _.ID == id).Single();

				Assert.AreEqual(cnt           , record.ID);
				Assert.AreEqual("new first"   , record.FirstName);
				Assert.AreEqual("new middle"  , record.MiddleName);
				Assert.AreEqual("new last"    , record.LastName);
				Assert.AreEqual(Gender.Unknown, record.Gender);
			}
		}

		[Test]
		public void Test_Person_Insert([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var maxId = db.Person.OrderByDescending(_ => _.ID).Select(_ => _.ID).FirstOrDefault();

				var cnt = Person_Insert(db, "new first", "new middle", "new last", 'U', context == ProviderName.AccessOdbc);

				Assert.AreEqual(1, cnt);

				var record = db.Person.Where(_ => _.ID > maxId).Single();

				Assert.AreEqual("new first"   , record.FirstName);
				Assert.AreEqual("new middle"  , record.MiddleName);
				Assert.AreEqual("new last"    , record.LastName);
				Assert.AreEqual(Gender.Unknown, record.Gender);
			}
		}

		[Test]
		public void Test_ThisProcedureNotVisibleFromODBC([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				Assert.AreEqual(0, db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count());

				var cnt = ThisProcedureNotVisibleFromODBC(db, context == ProviderName.AccessOdbc);

				Assert.AreEqual(1, cnt);

				Assert.AreEqual(1, db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count());
			}
		}

		[Test]
		public void Test_AddIssue792Record([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				Assert.AreEqual(0, db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count());

				var cnt = AddIssue792Record(db, 100500, context == ProviderName.AccessOdbc);

				Assert.AreEqual(1, cnt);

				Assert.AreEqual(1, db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count());
			}
		}

		[Test]
		public void Test_Scalar_DataReader([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Scalar_DataReader(db, context == ProviderName.AccessOdbc);

				Assert.AreEqual(1      , res.Count);
				Assert.AreEqual(12345  , res[0].intField);
				Assert.AreEqual("54321", res[0].stringField);
			}
		}

		[Test]
		public void Test_Person_SelectAll([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Person_SelectAll(db, context == ProviderName.AccessOdbc);

				AreEqual(db.Person.OrderBy(_ => _.ID), res.OrderBy( _ => _.ID));
			}
		}

		[Test]
		public void Test_Person_SelectByKey([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var id = db.Person.Select(_ => _.ID).Max();
				var res = Person_SelectByKey(db, id, context == ProviderName.AccessOdbc);

				AreEqual(db.Person.Where(_ => _.ID == id), res);
			}
		}

		[Test]
		public void Test_Person_SelectByName([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var firstName = "Jürgen";
				var lastName  = "König";
				var query     = db.Person.Where(_ => _.FirstName == firstName && _.LastName == lastName);
				var res       = Person_SelectByName(db, firstName, lastName, context == ProviderName.AccessOdbc);

				Assert.AreEqual(1, query.Count());
				AreEqual(query, res);
			}
		}

		[Test]
		public void Test_Person_SelectListByName([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var firstName = "e";
				var lastName  = "o";
				var query     = db.Person.Where(_ => _.FirstName.Contains(firstName) && _.LastName.Contains(lastName));
				var res       = Person_SelectListByName(db, $"%{firstName}%", $"%{lastName}%", context == ProviderName.AccessOdbc);

				Assert.AreEqual(2, query.Count());
				AreEqual(query.OrderBy(_ => _.ID), res.OrderBy(_ => _.ID));
			}
		}

		[Test]
		public void Test_Patient_SelectAll([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Patient_SelectAll(db, context == ProviderName.AccessOdbc);

				AreEqual(
					db.Patient.Select(p => new PatientResult()
					{
						PersonID   = p.Person.ID,
						FirstName  = p.Person.FirstName,
						LastName   = p.Person.LastName,
						MiddleName = p.Person.MiddleName,
						Gender     = p.Person.Gender,
						Diagnosis  = p.Diagnosis
					}) .OrderBy(_ => _.PersonID),
					res.OrderBy(_ => _.PersonID),
					ComparerBuilder.GetEqualityComparer<PatientResult>());
			}
		}

		[Test]
		public void Test_Patient_SelectByName([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{
				var firstName = "Tester";
				var lastName  = "Testerson";
				var query     = db.Patient
									.Where(_ => _.Person.FirstName == firstName && _.Person.LastName == lastName)
									.Select(p => new PatientResult()
									{
										PersonID   = p.Person.ID,
										FirstName  = p.Person.FirstName,
										LastName   = p.Person.LastName,
										MiddleName = p.Person.MiddleName,
										Gender     = p.Person.Gender,
										Diagnosis  = p.Diagnosis
									});
				var res = Patient_SelectByName(db, firstName, lastName, context == ProviderName.AccessOdbc);

				Assert.AreEqual(1, query.Count());
				AreEqual(
					query.OrderBy(_ => _.PersonID),
					res  .OrderBy(_ => _.PersonID),
					ComparerBuilder.GetEqualityComparer<PatientResult>());
			}
		}

		#region Procedures
		public static int Person_Delete(DataConnection dataConnection, int id, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_Delete(?) }" : "Person_Delete";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("id", id, DataType.Int32));
		}

		public static int Person_Update(DataConnection dataConnection, int id, string firstName, string? midleName, string lastName, char gender, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_Update(?, ?, ?, ?, ?) }" : "Person_Update";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("id"       , id       , DataType.Int32),
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("midleName", midleName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar),
				new DataParameter("gender"   , gender   , DataType.Char));
		}

		public static int Person_Insert(DataConnection dataConnection, string firstName, string? midleName, string lastName, char gender, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_Insert(?, ?, ?, ?) }" : "Person_Insert";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("midleName", midleName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar),
				new DataParameter("gender"   , gender   , DataType.Char));
		}

		public static int ThisProcedureNotVisibleFromODBC(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL ThisProcedureNotVisibleFromODBC() }" : "ThisProcedureNotVisibleFromODBC";
			return dataConnection.ExecuteProc(commandText);
		}

		public static int AddIssue792Record(DataConnection dataConnection, int? unused, bool odbc)
		{
			var commandText = odbc ? "{ CALL AddIssue792Record(?) }" : "AddIssue792Record";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("unused", unused, DataType.Int32));
		}

		public static List<Scalar_DataReaderResult> Scalar_DataReader(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL Scalar_DataReader() }" : "Scalar_DataReader";
			return dataConnection.QueryProc<Scalar_DataReaderResult>(commandText).ToList();
		}

		public partial class Scalar_DataReaderResult
		{
			public int?    intField    { get; set; }
			public string? stringField { get; set; }
		}

		public static List<Person> Person_SelectByKey(DataConnection dataConnection, int id, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectByKey(?) }" : "Person_SelectByKey";
			return dataConnection.QueryProc<Person>(
				commandText,
				new DataParameter("id", id, DataType.Int32)).ToList();
		}

		public static List<Person> Person_SelectAll(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectAll() }" : "Person_SelectAll";
			return dataConnection.QueryProc<Person>(commandText).ToList();
		}

		public static List<Person> Person_SelectByName(DataConnection dataConnection, string firstName, string lastName, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectByName(?, ?) }" : "Person_SelectByName";
			return dataConnection.QueryProc<Person>(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar)).ToList();
		}

		public static List<Person> Person_SelectListByName(DataConnection dataConnection, string firstName, string lastName, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectListByName(?, ?) }" : "Person_SelectListByName";
			return dataConnection.QueryProc<Person>(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar)).ToList();
		}

		public static List<PatientResult> Patient_SelectAll(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL Patient_SelectAll() }" : "Patient_SelectAll";
			return dataConnection.QueryProc<PatientResult>(commandText).ToList();
		}

		public static List<PatientResult> Patient_SelectByName(DataConnection dataConnection, string firstName, string lastName, bool odbc)
		{
			var commandText = odbc ? "{ CALL Patient_SelectByName(?, ?) }" : "Patient_SelectByName";
			return dataConnection.QueryProc<PatientResult>(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar)).ToList();
		}

		public partial class PatientResult
		{
			public int     PersonID   { get; set; }
			public string  FirstName  { get; set; } = null!;
			public string  LastName   { get; set; } = null!;
			public string? MiddleName { get; set; }
			public Gender  Gender     { get; set; }
			public string  Diagnosis  { get; set; } = null!;
		}
		#endregion
	}
}
