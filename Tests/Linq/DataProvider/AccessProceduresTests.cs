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
		public void Test_SelectProcedureSchema([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			var isODBC = context == ProviderName.AccessOdbc;
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var proc = schema.Procedures.Where(_ => _.ProcedureName == "Person_SelectByKey").Single();

				Assert.IsNull(proc.CatalogName);
				Assert.AreEqual(false, proc.IsAggregateFunction);
				Assert.AreEqual(true, proc.IsDefaultSchema);
				Assert.AreEqual(false, proc.IsFunction);
				Assert.AreEqual(true, proc.IsLoaded);
				Assert.AreEqual(false, proc.IsResultDynamic);
				Assert.AreEqual(false, proc.IsTableFunction);
				Assert.AreEqual("Person_SelectByKey", proc.MemberName);
				Assert.AreEqual("Person_SelectByKey", proc.ProcedureName);
				Assert.IsNull(proc.ResultException);
				Assert.IsNull(proc.SchemaName);
				Assert.IsNotNull(proc.SimilarTables);
				Assert.AreEqual(isODBC ? 0 : 2, proc.SimilarTables!.Count);

				Assert.AreEqual(1, proc.Parameters.Count);
				Assert.AreEqual(DataType.Int32, proc.Parameters[0].DataType);
				Assert.AreEqual(true, proc.Parameters[0].IsIn);
				Assert.AreEqual(true, proc.Parameters[0].IsNullable);
				Assert.AreEqual(false, proc.Parameters[0].IsOut);
				Assert.AreEqual(false, proc.Parameters[0].IsResult);
				Assert.AreEqual("@id", proc.Parameters[0].ParameterName);
				Assert.AreEqual("int?", proc.Parameters[0].ParameterType);
				Assert.IsNull(proc.Parameters[0].ProviderSpecificType);
				Assert.AreEqual("@id", proc.Parameters[0].SchemaName);
				Assert.AreEqual(context == ProviderName.Access ? "Long" : "INTEGER", proc.Parameters[0].SchemaType);
				Assert.IsNull(proc.Parameters[0].Size);
				Assert.AreEqual(typeof(int), proc.Parameters[0].SystemType);

				Assert.IsNotNull(proc.ResultTable);
				Assert.IsNull(proc.ResultTable!.CatalogName);
				Assert.IsNull(proc.ResultTable.Description);
				Assert.IsNotNull(proc.ResultTable.ForeignKeys);
				Assert.AreEqual(0, proc.ResultTable.ForeignKeys.Count);
				Assert.IsNull(proc.ResultTable.ID);
				Assert.AreEqual(false, proc.ResultTable.IsDefaultSchema);
				Assert.AreEqual(true, proc.ResultTable.IsProcedureResult);
				Assert.AreEqual(false, proc.ResultTable.IsProviderSpecific);
				Assert.AreEqual(false, proc.ResultTable.IsView);
				Assert.IsNull(proc.ResultTable.SchemaName);
				Assert.IsNull(proc.ResultTable.TableName);
				Assert.AreEqual("Person_SelectByKeyResult", proc.ResultTable.TypeName);

				Assert.IsNotNull(proc.ResultTable.Columns);

				Assert.AreEqual(5, proc.ResultTable.Columns.Count);

				Assert.AreEqual("PersonID", proc.ResultTable.Columns[0].ColumnName);
				Assert.AreEqual(isODBC ? "COUNTER" : "Long", proc.ResultTable.Columns[0].ColumnType);
				Assert.AreEqual(DataType.Int32, proc.ResultTable.Columns[0].DataType);
				Assert.IsNull(proc.ResultTable.Columns[0].Description);
				Assert.AreEqual(true, proc.ResultTable.Columns[0].IsIdentity);
				Assert.AreEqual(false, proc.ResultTable.Columns[0].IsNullable);
				Assert.AreEqual(false, proc.ResultTable.Columns[0].IsPrimaryKey);
				Assert.IsNull(proc.ResultTable.Columns[0].Length);
				Assert.AreEqual("PersonID", proc.ResultTable.Columns[0].MemberName);
				Assert.AreEqual("int", proc.ResultTable.Columns[0].MemberType);
				Assert.IsNull(proc.ResultTable.Columns[0].Precision);
				Assert.AreEqual(0, proc.ResultTable.Columns[0].PrimaryKeyOrder);
				Assert.IsNull(proc.ResultTable.Columns[0].ProviderSpecificType);
				Assert.IsNull(proc.ResultTable.Columns[0].Scale);
				Assert.AreEqual(false, proc.ResultTable.Columns[0].SkipOnInsert);
				Assert.AreEqual(false, proc.ResultTable.Columns[0].SkipOnUpdate);
				Assert.AreEqual(typeof(int), proc.ResultTable.Columns[0].SystemType);
				Assert.AreEqual(proc.ResultTable, proc.ResultTable.Columns[0].Table);

				Assert.AreEqual("FirstName", proc.ResultTable.Columns[1].ColumnName);
				Assert.AreEqual(isODBC ? "VARCHAR(255)" : "VarChar(50)", proc.ResultTable.Columns[1].ColumnType);
				Assert.AreEqual(DataType.VarChar, proc.ResultTable.Columns[1].DataType);
				Assert.IsNull(proc.ResultTable.Columns[1].Description);
				Assert.AreEqual(false, proc.ResultTable.Columns[1].IsIdentity);
				Assert.AreEqual(true, proc.ResultTable.Columns[1].IsNullable);
				Assert.AreEqual(false, proc.ResultTable.Columns[1].IsPrimaryKey);
				Assert.IsNull(proc.ResultTable.Columns[1].Length);
				Assert.AreEqual("FirstName", proc.ResultTable.Columns[1].MemberName);
				Assert.AreEqual("string", proc.ResultTable.Columns[1].MemberType);
				Assert.IsNull(proc.ResultTable.Columns[1].Precision);
				Assert.AreEqual(0, proc.ResultTable.Columns[1].PrimaryKeyOrder);
				Assert.IsNull(proc.ResultTable.Columns[1].ProviderSpecificType);
				Assert.IsNull(proc.ResultTable.Columns[1].Scale);
				Assert.AreEqual(false, proc.ResultTable.Columns[1].SkipOnInsert);
				Assert.AreEqual(false, proc.ResultTable.Columns[1].SkipOnUpdate);
				Assert.AreEqual(typeof(string), proc.ResultTable.Columns[1].SystemType);
				Assert.AreEqual(proc.ResultTable, proc.ResultTable.Columns[1].Table);

				Assert.AreEqual("LastName", proc.ResultTable.Columns[2].ColumnName);
				Assert.AreEqual(isODBC ? "VARCHAR(255)" : "VarChar(50)", proc.ResultTable.Columns[2].ColumnType);
				Assert.AreEqual(DataType.VarChar, proc.ResultTable.Columns[2].DataType);
				Assert.IsNull(proc.ResultTable.Columns[2].Description);
				Assert.AreEqual(false, proc.ResultTable.Columns[2].IsIdentity);
				Assert.AreEqual(true, proc.ResultTable.Columns[2].IsNullable);
				Assert.AreEqual(false, proc.ResultTable.Columns[2].IsPrimaryKey);
				Assert.IsNull(proc.ResultTable.Columns[2].Length);
				Assert.AreEqual("LastName", proc.ResultTable.Columns[2].MemberName);
				Assert.AreEqual("string", proc.ResultTable.Columns[2].MemberType);
				Assert.IsNull(proc.ResultTable.Columns[2].Precision);
				Assert.AreEqual(0, proc.ResultTable.Columns[2].PrimaryKeyOrder);
				Assert.IsNull(proc.ResultTable.Columns[2].ProviderSpecificType);
				Assert.IsNull(proc.ResultTable.Columns[2].Scale);
				Assert.AreEqual(false, proc.ResultTable.Columns[2].SkipOnInsert);
				Assert.AreEqual(false, proc.ResultTable.Columns[2].SkipOnUpdate);
				Assert.AreEqual(typeof(string), proc.ResultTable.Columns[2].SystemType);
				Assert.AreEqual(proc.ResultTable, proc.ResultTable.Columns[2].Table);

				Assert.AreEqual("MiddleName", proc.ResultTable.Columns[3].ColumnName);
				Assert.AreEqual(isODBC ? "VARCHAR(255)" : "VarChar(50)", proc.ResultTable.Columns[3].ColumnType);
				Assert.AreEqual(DataType.VarChar, proc.ResultTable.Columns[3].DataType);
				Assert.IsNull(proc.ResultTable.Columns[3].Description);
				Assert.AreEqual(false, proc.ResultTable.Columns[3].IsIdentity);
				Assert.AreEqual(true, proc.ResultTable.Columns[3].IsNullable);
				Assert.AreEqual(false, proc.ResultTable.Columns[3].IsPrimaryKey);
				Assert.IsNull(proc.ResultTable.Columns[3].Length);
				Assert.AreEqual("MiddleName", proc.ResultTable.Columns[3].MemberName);
				Assert.AreEqual("string", proc.ResultTable.Columns[3].MemberType);
				Assert.IsNull(proc.ResultTable.Columns[3].Precision);
				Assert.AreEqual(0, proc.ResultTable.Columns[3].PrimaryKeyOrder);
				Assert.IsNull(proc.ResultTable.Columns[3].ProviderSpecificType);
				Assert.IsNull(proc.ResultTable.Columns[3].Scale);
				Assert.AreEqual(false, proc.ResultTable.Columns[3].SkipOnInsert);
				Assert.AreEqual(false, proc.ResultTable.Columns[3].SkipOnUpdate);
				Assert.AreEqual(typeof(string), proc.ResultTable.Columns[3].SystemType);
				Assert.AreEqual(proc.ResultTable, proc.ResultTable.Columns[3].Table);

				Assert.AreEqual("Gender", proc.ResultTable.Columns[4].ColumnName);
				Assert.AreEqual(isODBC ? "VARCHAR(255)" : "VarChar(1)", proc.ResultTable.Columns[4].ColumnType);
				Assert.AreEqual(DataType.VarChar, proc.ResultTable.Columns[4].DataType);
				Assert.IsNull(proc.ResultTable.Columns[4].Description);
				Assert.AreEqual(false, proc.ResultTable.Columns[4].IsIdentity);
				Assert.AreEqual(true, proc.ResultTable.Columns[4].IsNullable);
				Assert.AreEqual(false, proc.ResultTable.Columns[4].IsPrimaryKey);
				Assert.IsNull(proc.ResultTable.Columns[4].Length);
				Assert.AreEqual("Gender", proc.ResultTable.Columns[4].MemberName);
				Assert.AreEqual("string", proc.ResultTable.Columns[4].MemberType);
				Assert.IsNull(proc.ResultTable.Columns[4].Precision);
				Assert.AreEqual(0, proc.ResultTable.Columns[4].PrimaryKeyOrder);
				Assert.IsNull(proc.ResultTable.Columns[4].ProviderSpecificType);
				Assert.IsNull(proc.ResultTable.Columns[4].Scale);
				Assert.AreEqual(false, proc.ResultTable.Columns[4].SkipOnInsert);
				Assert.AreEqual(false, proc.ResultTable.Columns[4].SkipOnUpdate);
				Assert.AreEqual(isODBC ? typeof(string) : typeof(char), proc.ResultTable.Columns[4].SystemType);
				Assert.AreEqual(proc.ResultTable, proc.ResultTable.Columns[4].Table);
			}
		}

		[Test]
		public void Test_Person_Delete([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Person_SelectAll(db, context == ProviderName.AccessOdbc);

				AreEqual(db.Person.OrderBy(_ => _.ID), res.OrderBy( _ => _.ID));
			}
		}

		[Test]
		public void Test_Person_SelectByKey([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			var commandText = odbc ? "{ CALL Person_Delete(?) }" : "[Person_Delete]";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("@id", id, DataType.Int32));
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
