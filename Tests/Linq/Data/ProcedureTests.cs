using System.Collections.Generic;
using System.Linq;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Data
{
	using System.Data;
	using System.Threading.Tasks;
	using LinqToDB;
	using LinqToDB.DataProvider.SqlServer;
	using LinqToDB.Mapping;
	using Model;

	[TestFixture]
	public class ProcedureTests : TestBase
	{
		public static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, int? @id)
		{
			var databaseName     = TestUtils.GetDatabaseName(dataConnection);
			var escapedTableName = SqlServerTools.QuoteIdentifier(databaseName);

			return dataConnection.QueryProc<Person>(escapedTableName + "..[Person_SelectByKey]",
				new DataParameter("@id", @id));
		}

		public static IEnumerable<Person> PersonSelectByKeyLowercaseColumns(DataConnection dataConnection, int? @id)
		{
			var databaseName     = TestUtils.GetDatabaseName(dataConnection);
			var escapedTableName = SqlServerTools.QuoteIdentifier(databaseName);

			return dataConnection.QueryProc<Person>(escapedTableName + "..[Person_SelectByKeyLowercase]",
				new DataParameter("@id", @id));
		}

		[Test]
		public void TestColumnNameComparerCaseInsensivity([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p1 = PersonSelectByKeyLowercaseColumns(db, 1).First();
				var p2 = db.Query<Person>("SELECT PersonID, FirstName FROM Person WHERE PersonID = @id", new { id = 1 }).First();
				var p3 = PersonSelectByKey(db, 1).First();

				Assert.AreEqual(p1.FirstName, p2.FirstName);
				Assert.AreEqual(p1.FirstName, p3.FirstName);
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p1 = PersonSelectByKey(db, 1).First();
				var p2 = db.Query<Person>("SELECT * FROM Person WHERE PersonID = @id", new { id = 1 }).First();

				Assert.AreEqual(p1, p2);
			}
		}

		class VariableResult
		{
			public int     Code   { get; set; }
			public string? Value1 { get; set; }

			protected bool Equals(VariableResult other)
			{
				return Code == other.Code && string.Equals(Value1, other.Value1) && string.Equals(Value2, other.Value2);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((VariableResult)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = Code;
					hashCode = (hashCode * 397) ^ (Value1 != null ? Value1.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (Value2 != null ? Value2.GetHashCode() : 0);
					return hashCode;
				}
			}

			public string? Value2 { get; set; }
		}

		[Test]
		public void VariableResultsTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var set1 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 0)).First();

				var set2 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 1)).First();

				var set11 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 0)).First();

				var set22 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 1)).First();

				Assert.AreEqual(2, set1.Code);
				Assert.AreEqual("v", set1.Value1);
				Assert.IsNull(set1.Value2);

				Assert.AreEqual(1, set2.Code);
				Assert.AreEqual("Val1", set2.Value1);
				Assert.AreEqual("Val2", set2.Value2);

				Assert.AreEqual(set1, set11);
				Assert.AreEqual(set2, set22);
			}
		}

		[Test]
		public void VariableResultsTestWithAnonymParam([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var set1 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 0 }).First();

				var set2 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 1 }).First();

				var set11 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 0 }).First();

				var set22 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 1 }).First();

				Assert.AreEqual(2, set1.Code);
				Assert.AreEqual("v", set1.Value1);
				Assert.IsNull(set1.Value2);

				Assert.AreEqual(1, set2.Code);
				Assert.AreEqual("Val1", set2.Value1);
				Assert.AreEqual("Val2", set2.Value2);

				Assert.AreEqual(set1, set11);
				Assert.AreEqual(set2, set22);
			}
		}

		[Test]
		public void TestQueryProcRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input   = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = db.QueryProc<Person>("QueryProcParameters", input, output1, output2);
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);

				persons.ToList();

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		[Test]
		public async Task TestQueryProcAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = await db.QueryProcAsync<Person>("QueryProcParameters", input, output1, output2);
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);

				persons.ToList();

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		[Test]
		public void TestQueryProcTemplateRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = db.QueryProc(new Person(), "QueryProcParameters", input, output1, output2);

				persons.ToList();

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		[Test]
		public async Task TestQueryProcAsyncTemplateRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = await db.QueryProcAsync(new Person(), "QueryProcParameters", input, output1, output2);
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);

				persons.ToList();

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		[Test]
		public void TestQueryProcReaderRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = db.QueryProc(reader => new Person(), "QueryProcParameters", input, output1, output2);
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);

				persons.ToList();

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		[Test]
		public async Task TestQueryProcAsyncReaderRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = await db.QueryProcAsync(reader => new Person(), "QueryProcParameters", input, output1, output2);
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);

				persons.ToList();

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		class QueryProcMultipleResult
		{
			[ResultSetIndex(0)] public IEnumerable<Person> Persons { get; set; } = null!;
			[ResultSetIndex(1)] public IEnumerable<Doctor> Doctors { get; set; } = null!;
		}

		[Test]
		public void TestQueryProcMultipleRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output3 = new DataParameter("output3", null, DataType.Int32) { Direction = ParameterDirection.Output };
				db.QueryProcMultiple<Person>("QueryProcMultipleParameters", input, output1, output2, output3);
				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
				Assert.AreEqual(4, output3.Value);
			}
		}

		[Test]
		public async Task TestQueryProcMultipleAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output3 = new DataParameter("output3", null, DataType.Int32) { Direction = ParameterDirection.Output };
				await db.QueryProcMultipleAsync<Person>("QueryProcMultipleParameters", input, output1, output2, output3);
				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
				Assert.AreEqual(4, output3.Value);
			}
		}

		[Test]
		public void TestExecuteProcIntRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = db.ExecuteProc("ExecuteProcIntParameters", input, output);
				Assert.AreEqual(2, output.Value);
				Assert.AreEqual(1, result);
			}
		}

		[Test]
		public async Task TestExecuteProcAsyncIntRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = await db.ExecuteProcAsync("ExecuteProcIntParameters", input, output);
				Assert.AreEqual(2, output.Value);
				Assert.AreEqual(1, result);
			}
		}

		[Test]
		public void TestExecuteProcTRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = db.ExecuteProc<string>("ExecuteProcStringParameters", input, output);
				Assert.AreEqual(2, output.Value);
				Assert.AreEqual("издрасте", result);
			}
		}

		[Test]
		public async Task TestExecuteProcAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = await db.ExecuteProcAsync<string>("ExecuteProcStringParameters", input, output);
				Assert.AreEqual(2, output.Value);
				Assert.AreEqual("издрасте", result);
			}
		}

		[Test]
		public void TestExecuteReaderProcRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var reader = new CommandInfo(db, "QueryProcParameters", input, output1, output2).ExecuteReaderProc();
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);
				using (reader)
					while (reader.Reader!.Read())
					{
					}

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}

		[Test]
		public async Task TestExecuteReaderProcAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var reader = await new CommandInfo(db, "QueryProcParameters", input, output1, output2).ExecuteReaderProcAsync();
				Assert.IsNull(output1.Value);
				Assert.IsNull(output2.Value);
				using (reader)
					while (await reader.Reader!.ReadAsync())
					{
					}

				Assert.AreEqual(2, output1.Value);
				Assert.AreEqual(3, output2.Value);
			}
		}
	}
}
