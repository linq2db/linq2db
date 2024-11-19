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
			var isODBC = context.IsAnyOf(ProviderName.AccessOdbc);
			using (var db = GetDataConnection(context))
			{
				var schema = db.DataProvider.GetSchemaProvider().GetSchema(db);

				var proc = schema.Procedures.Where(_ => _.ProcedureName == "Person_SelectByKey").Single();

				Assert.Multiple(() =>
				{
					Assert.That(proc.CatalogName, Is.Null);
					Assert.That(proc.IsAggregateFunction, Is.EqualTo(false));
					Assert.That(proc.IsDefaultSchema, Is.EqualTo(true));
					Assert.That(proc.IsFunction, Is.EqualTo(false));
					Assert.That(proc.IsLoaded, Is.EqualTo(true));
					Assert.That(proc.IsResultDynamic, Is.EqualTo(false));
					Assert.That(proc.IsTableFunction, Is.EqualTo(false));
					Assert.That(proc.MemberName, Is.EqualTo("Person_SelectByKey"));
					Assert.That(proc.ProcedureName, Is.EqualTo("Person_SelectByKey"));
					Assert.That(proc.ResultException, Is.Null);
					Assert.That(proc.SchemaName, Is.Null);
					Assert.That(proc.SimilarTables, Is.Not.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.SimilarTables!, Has.Count.EqualTo(isODBC ? 0 : 2));

					Assert.That(proc.Parameters, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.Parameters[0].DataType, Is.EqualTo(DataType.Int32));
					Assert.That(proc.Parameters[0].IsIn, Is.EqualTo(true));
					Assert.That(proc.Parameters[0].IsNullable, Is.EqualTo(true));
					Assert.That(proc.Parameters[0].IsOut, Is.EqualTo(false));
					Assert.That(proc.Parameters[0].IsResult, Is.EqualTo(false));
					Assert.That(proc.Parameters[0].ParameterName, Is.EqualTo("@id"));
					Assert.That(proc.Parameters[0].ParameterType, Is.EqualTo("int?"));
					Assert.That(proc.Parameters[0].ProviderSpecificType, Is.Null);
					Assert.That(proc.Parameters[0].SchemaName, Is.EqualTo("@id"));
					Assert.That(proc.Parameters[0].SchemaType, Is.EqualTo(context.IsAnyOf(ProviderName.Access) ? "Long" : "INTEGER"));
					Assert.That(proc.Parameters[0].Size, Is.Null);
					Assert.That(proc.Parameters[0].SystemType, Is.EqualTo(typeof(int)));

					Assert.That(proc.ResultTable, Is.Not.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable!.CatalogName, Is.Null);
					Assert.That(proc.ResultTable.Description, Is.Null);
					Assert.That(proc.ResultTable.ForeignKeys, Is.Not.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.ForeignKeys, Is.Empty);
					Assert.That(proc.ResultTable.ID, Is.Null);
					Assert.That(proc.ResultTable.IsDefaultSchema, Is.EqualTo(false));
					Assert.That(proc.ResultTable.IsProcedureResult, Is.EqualTo(true));
					Assert.That(proc.ResultTable.IsProviderSpecific, Is.EqualTo(false));
					Assert.That(proc.ResultTable.IsView, Is.EqualTo(false));
					Assert.That(proc.ResultTable.SchemaName, Is.Null);
					Assert.That(proc.ResultTable.TableName, Is.Null);
					Assert.That(proc.ResultTable.TypeName, Is.EqualTo("Person_SelectByKeyResult"));

					Assert.That(proc.ResultTable.Columns, Is.Not.Null);
				});

				Assert.That(proc.ResultTable.Columns, Has.Count.EqualTo(5));

				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.Columns[0].ColumnName, Is.EqualTo("PersonID"));
					Assert.That(proc.ResultTable.Columns[0].ColumnType, Is.EqualTo(isODBC ? "COUNTER" : "Long"));
					Assert.That(proc.ResultTable.Columns[0].DataType, Is.EqualTo(DataType.Int32));
					Assert.That(proc.ResultTable.Columns[0].Description, Is.Null);
					Assert.That(proc.ResultTable.Columns[0].IsIdentity, Is.EqualTo(true));
					Assert.That(proc.ResultTable.Columns[0].IsNullable, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[0].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[0].Length, Is.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.Columns[0].MemberName, Is.EqualTo("PersonID"));
					Assert.That(proc.ResultTable.Columns[0].MemberType, Is.EqualTo("int"));
					Assert.That(proc.ResultTable.Columns[0].Precision, Is.Null);
					Assert.That(proc.ResultTable.Columns[0].PrimaryKeyOrder, Is.EqualTo(0));
					Assert.That(proc.ResultTable.Columns[0].ProviderSpecificType, Is.Null);
					Assert.That(proc.ResultTable.Columns[0].Scale, Is.Null);
					Assert.That(proc.ResultTable.Columns[0].SkipOnInsert, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[0].SkipOnUpdate, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[0].SystemType, Is.EqualTo(typeof(int)));
					Assert.That(proc.ResultTable.Columns[0].Table, Is.EqualTo(proc.ResultTable));

					Assert.That(proc.ResultTable.Columns[1].ColumnName, Is.EqualTo("FirstName"));
					Assert.That(proc.ResultTable.Columns[1].ColumnType, Is.EqualTo(isODBC ? "VARCHAR(255)" : "VarChar(50)"));
					Assert.That(proc.ResultTable.Columns[1].DataType, Is.EqualTo(DataType.VarChar));
					Assert.That(proc.ResultTable.Columns[1].Description, Is.Null);
					Assert.That(proc.ResultTable.Columns[1].IsIdentity, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[1].IsNullable, Is.EqualTo(true));
					Assert.That(proc.ResultTable.Columns[1].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[1].Length, Is.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.Columns[1].MemberName, Is.EqualTo("FirstName"));
					Assert.That(proc.ResultTable.Columns[1].MemberType, Is.EqualTo("string"));
					Assert.That(proc.ResultTable.Columns[1].Precision, Is.Null);
					Assert.That(proc.ResultTable.Columns[1].PrimaryKeyOrder, Is.EqualTo(0));
					Assert.That(proc.ResultTable.Columns[1].ProviderSpecificType, Is.Null);
					Assert.That(proc.ResultTable.Columns[1].Scale, Is.Null);
					Assert.That(proc.ResultTable.Columns[1].SkipOnInsert, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[1].SkipOnUpdate, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[1].SystemType, Is.EqualTo(typeof(string)));
					Assert.That(proc.ResultTable.Columns[1].Table, Is.EqualTo(proc.ResultTable));

					Assert.That(proc.ResultTable.Columns[2].ColumnName, Is.EqualTo("LastName"));
					Assert.That(proc.ResultTable.Columns[2].ColumnType, Is.EqualTo(isODBC ? "VARCHAR(255)" : "VarChar(50)"));
					Assert.That(proc.ResultTable.Columns[2].DataType, Is.EqualTo(DataType.VarChar));
					Assert.That(proc.ResultTable.Columns[2].Description, Is.Null);
					Assert.That(proc.ResultTable.Columns[2].IsIdentity, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[2].IsNullable, Is.EqualTo(true));
					Assert.That(proc.ResultTable.Columns[2].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[2].Length, Is.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.Columns[2].MemberName, Is.EqualTo("LastName"));
					Assert.That(proc.ResultTable.Columns[2].MemberType, Is.EqualTo("string"));
					Assert.That(proc.ResultTable.Columns[2].Precision, Is.Null);
					Assert.That(proc.ResultTable.Columns[2].PrimaryKeyOrder, Is.EqualTo(0));
					Assert.That(proc.ResultTable.Columns[2].ProviderSpecificType, Is.Null);
					Assert.That(proc.ResultTable.Columns[2].Scale, Is.Null);
					Assert.That(proc.ResultTable.Columns[2].SkipOnInsert, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[2].SkipOnUpdate, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[2].SystemType, Is.EqualTo(typeof(string)));
					Assert.That(proc.ResultTable.Columns[2].Table, Is.EqualTo(proc.ResultTable));

					Assert.That(proc.ResultTable.Columns[3].ColumnName, Is.EqualTo("MiddleName"));
					Assert.That(proc.ResultTable.Columns[3].ColumnType, Is.EqualTo(isODBC ? "VARCHAR(255)" : "VarChar(50)"));
					Assert.That(proc.ResultTable.Columns[3].DataType, Is.EqualTo(DataType.VarChar));
					Assert.That(proc.ResultTable.Columns[3].Description, Is.Null);
					Assert.That(proc.ResultTable.Columns[3].IsIdentity, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[3].IsNullable, Is.EqualTo(true));
					Assert.That(proc.ResultTable.Columns[3].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[3].Length, Is.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.Columns[3].MemberName, Is.EqualTo("MiddleName"));
					Assert.That(proc.ResultTable.Columns[3].MemberType, Is.EqualTo("string"));
					Assert.That(proc.ResultTable.Columns[3].Precision, Is.Null);
					Assert.That(proc.ResultTable.Columns[3].PrimaryKeyOrder, Is.EqualTo(0));
					Assert.That(proc.ResultTable.Columns[3].ProviderSpecificType, Is.Null);
					Assert.That(proc.ResultTable.Columns[3].Scale, Is.Null);
					Assert.That(proc.ResultTable.Columns[3].SkipOnInsert, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[3].SkipOnUpdate, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[3].SystemType, Is.EqualTo(typeof(string)));
					Assert.That(proc.ResultTable.Columns[3].Table, Is.EqualTo(proc.ResultTable));

					Assert.That(proc.ResultTable.Columns[4].ColumnName, Is.EqualTo("Gender"));
					Assert.That(proc.ResultTable.Columns[4].ColumnType, Is.EqualTo(isODBC ? "VARCHAR(255)" : "VarChar(1)"));
					Assert.That(proc.ResultTable.Columns[4].DataType, Is.EqualTo(DataType.VarChar));
					Assert.That(proc.ResultTable.Columns[4].Description, Is.Null);
					Assert.That(proc.ResultTable.Columns[4].IsIdentity, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[4].IsNullable, Is.EqualTo(true));
					Assert.That(proc.ResultTable.Columns[4].IsPrimaryKey, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[4].Length, Is.Null);
				});
				Assert.Multiple(() =>
				{
					Assert.That(proc.ResultTable.Columns[4].MemberName, Is.EqualTo("Gender"));
					Assert.That(proc.ResultTable.Columns[4].MemberType, Is.EqualTo("string"));
					Assert.That(proc.ResultTable.Columns[4].Precision, Is.Null);
					Assert.That(proc.ResultTable.Columns[4].PrimaryKeyOrder, Is.EqualTo(0));
					Assert.That(proc.ResultTable.Columns[4].ProviderSpecificType, Is.Null);
					Assert.That(proc.ResultTable.Columns[4].Scale, Is.Null);
					Assert.That(proc.ResultTable.Columns[4].SkipOnInsert, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[4].SkipOnUpdate, Is.EqualTo(false));
					Assert.That(proc.ResultTable.Columns[4].SystemType, Is.EqualTo(isODBC ? typeof(string) : typeof(char)));
					Assert.That(proc.ResultTable.Columns[4].Table, Is.EqualTo(proc.ResultTable));
				});
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

				Assert.That(db.Person.Where(_ => _.ID == id).Count(), Is.EqualTo(1));

				var cnt = Person_Delete(db, id, context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.Multiple(() =>
				{
					Assert.That(db.Person.Where(_ => _.ID == id).Count(), Is.EqualTo(0));
					Assert.That(cnt, Is.EqualTo(1));
				});
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

				Assert.That(db.Person.Where(_ => _.ID == id).Count(), Is.EqualTo(1));

				var cnt = Person_Update(db, id, "new first", "new middle", "new last", 'U', context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.That(cnt, Is.EqualTo(1));

				var record = db.Person.Where(_ => _.ID == id).Single();

				Assert.Multiple(() =>
				{
					Assert.That(record.ID, Is.EqualTo(cnt));
					Assert.That(record.FirstName, Is.EqualTo("new first"));
					Assert.That(record.MiddleName, Is.EqualTo("new middle"));
					Assert.That(record.LastName, Is.EqualTo("new last"));
					Assert.That(record.Gender, Is.EqualTo(Gender.Unknown));
				});
			}
		}

		[Test]
		public void Test_Person_Insert([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var maxId = db.Person.OrderByDescending(_ => _.ID).Select(_ => _.ID).FirstOrDefault();

				var cnt = Person_Insert(db, "new first", "new middle", "new last", 'U', context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.That(cnt, Is.EqualTo(1));

				var record = db.Person.Where(_ => _.ID > maxId).Single();

				Assert.Multiple(() =>
				{
					Assert.That(record.FirstName, Is.EqualTo("new first"));
					Assert.That(record.MiddleName, Is.EqualTo("new middle"));
					Assert.That(record.LastName, Is.EqualTo("new last"));
					Assert.That(record.Gender, Is.EqualTo(Gender.Unknown));
				});
			}
		}

		[Test]
		public void Test_ThisProcedureNotVisibleFromODBC([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				Assert.That(db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count(), Is.EqualTo(0));

				var cnt = ThisProcedureNotVisibleFromODBC(db, context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count(), Is.EqualTo(1));
				});
			}
		}

		[Test]
		public void Test_AddIssue792Record([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				Assert.That(db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count(), Is.EqualTo(0));

				var cnt = AddIssue792Record(db, 100500, context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.Multiple(() =>
				{
					Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.GetTable<Issue792Tests.AllTypes>().Where(_ => _.char20DataType == "issue792").Count(), Is.EqualTo(1));
				});
			}
		}

		[Test]
		public void Test_Scalar_DataReader([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Scalar_DataReader(db, context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.That(res, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(res[0].intField, Is.EqualTo(12345));
					Assert.That(res[0].stringField, Is.EqualTo("54321"));
				});
			}
		}

		[Test]
		public void Test_Person_SelectAll([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Person_SelectAll(db, context.IsAnyOf(ProviderName.AccessOdbc));

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
				var res = Person_SelectByKey(db, id, context.IsAnyOf(ProviderName.AccessOdbc));

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
				var res       = Person_SelectByName(db, firstName, lastName, context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.That(query.Count(), Is.EqualTo(1));
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
				var res       = Person_SelectListByName(db, $"%{firstName}%", $"%{lastName}%", context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.That(query.Count(), Is.EqualTo(2));
				AreEqual(query.OrderBy(_ => _.ID), res.OrderBy(_ => _.ID));
			}
		}

		[Test]
		public void Test_Patient_SelectAll([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.BeginTransaction())
			{
				var res = Patient_SelectAll(db, context.IsAnyOf(ProviderName.AccessOdbc));

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
				var res = Patient_SelectByName(db, firstName, lastName, context.IsAnyOf(ProviderName.AccessOdbc));

				Assert.That(query.Count(), Is.EqualTo(1));
				AreEqual(
					query.OrderBy(_ => _.PersonID),
					res  .OrderBy(_ => _.PersonID),
					ComparerBuilder.GetEqualityComparer<PatientResult>());
			}
		}

		#region Procedures
		private static int Person_Delete(DataConnection dataConnection, int id, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_Delete(?) }" : "[Person_Delete]";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("@id", id, DataType.Int32));
		}

		private static int Person_Update(DataConnection dataConnection, int id, string firstName, string? midleName, string lastName, char gender, bool odbc)
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

		private static int Person_Insert(DataConnection dataConnection, string firstName, string? midleName, string lastName, char gender, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_Insert(?, ?, ?, ?) }" : "Person_Insert";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("midleName", midleName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar),
				new DataParameter("gender"   , gender   , DataType.Char));
		}

		private static int ThisProcedureNotVisibleFromODBC(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL ThisProcedureNotVisibleFromODBC() }" : "ThisProcedureNotVisibleFromODBC";
			return dataConnection.ExecuteProc(commandText);
		}

		private static int AddIssue792Record(DataConnection dataConnection, int? unused, bool odbc)
		{
			var commandText = odbc ? "{ CALL AddIssue792Record(?) }" : "AddIssue792Record";
			return dataConnection.ExecuteProc(
				commandText,
				new DataParameter("unused", unused, DataType.Int32));
		}

		private static List<Scalar_DataReaderResult> Scalar_DataReader(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL Scalar_DataReader() }" : "Scalar_DataReader";
			return dataConnection.QueryProc<Scalar_DataReaderResult>(commandText).ToList();
		}

		public partial class Scalar_DataReaderResult
		{
			public int?    intField    { get; set; }
			public string? stringField { get; set; }
		}

		private static List<Person> Person_SelectByKey(DataConnection dataConnection, int id, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectByKey(?) }" : "Person_SelectByKey";
			return dataConnection.QueryProc<Person>(
				commandText,
				new DataParameter("id", id, DataType.Int32)).ToList();
		}

		private static List<Person> Person_SelectAll(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectAll() }" : "Person_SelectAll";
			return dataConnection.QueryProc<Person>(commandText).ToList();
		}

		private static List<Person> Person_SelectByName(DataConnection dataConnection, string firstName, string lastName, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectByName(?, ?) }" : "Person_SelectByName";
			return dataConnection.QueryProc<Person>(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar)).ToList();
		}

		private static List<Person> Person_SelectListByName(DataConnection dataConnection, string firstName, string lastName, bool odbc)
		{
			var commandText = odbc ? "{ CALL Person_SelectListByName(?, ?) }" : "Person_SelectListByName";
			return dataConnection.QueryProc<Person>(
				commandText,
				new DataParameter("firstName", firstName, DataType.VarChar),
				new DataParameter("lastName" , lastName , DataType.VarChar)).ToList();
		}

		private static List<PatientResult> Patient_SelectAll(DataConnection dataConnection, bool odbc)
		{
			var commandText = odbc ? "{ CALL Patient_SelectAll() }" : "Patient_SelectAll";
			return dataConnection.QueryProc<PatientResult>(commandText).ToList();
		}

		private static List<PatientResult> Patient_SelectByName(DataConnection dataConnection, string firstName, string lastName, bool odbc)
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
