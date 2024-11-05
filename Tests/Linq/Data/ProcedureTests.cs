using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Tests.Data
{
	using LinqToDB;
	using LinqToDB.Data;
	using LinqToDB.DataProvider.SqlServer;
	using LinqToDB.Mapping;
	using Model;

	[TestFixture]
	public class ProcedureTests : TestBase
	{
		private static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, string context, int? @id)
		{
			var databaseName     = TestUtils.GetDatabaseName(dataConnection, context);
			var escapedTableName = SqlServerTools.QuoteIdentifier(databaseName);

			return dataConnection.QueryProc<Person>(escapedTableName + "..[Person_SelectByKey]",
				new DataParameter("@id", @id));
		}

		private static IEnumerable<Person> PersonSelectByKeyLowercaseColumns(DataConnection dataConnection, string context, int? @id)
		{
			var databaseName     = TestUtils.GetDatabaseName(dataConnection, context);
			var escapedTableName = SqlServerTools.QuoteIdentifier(databaseName);

			return dataConnection.QueryProc<Person>(escapedTableName + "..[Person_SelectByKeyLowercase]",
				new DataParameter("@id", @id));
		}

		[Test]
		public void TestColumnNameComparerCaseInsensivity([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p1 = PersonSelectByKeyLowercaseColumns(db, context, 1).First();
				var p2 = db.Query<Person>("SELECT PersonID, FirstName FROM Person WHERE PersonID = @id", new { id = 1 }).First();
				var p3 = PersonSelectByKey(db, context, 1).First();

				Assert.Multiple(() =>
				{
					Assert.That(p2.FirstName, Is.EqualTo(p1.FirstName));
					Assert.That(p3.FirstName, Is.EqualTo(p1.FirstName));
				});
			}
		}

		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p1 = PersonSelectByKey(db, context, 1).First();
				var p2 = db.Query<Person>("SELECT * FROM Person WHERE PersonID = @id", new { id = 1 }).First();

				Assert.That(p2, Is.EqualTo(p1));
			}
		}

		sealed class VariableResult
		{
			public int     Code   { get; set; }
			public string? Value1 { get; set; }

			private bool Equals(VariableResult other)
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
			using (var db = GetDataConnection(context))
			{
				var set1 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 0)).First();

				var set2 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 1)).First();

				var set11 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 0)).First();

				var set22 = db.QueryProc<VariableResult>("[VariableResults]",
					new DataParameter("@ReturnFullRow", 1)).First();

				Assert.Multiple(() =>
				{
					Assert.That(set1.Code, Is.EqualTo(2));
					Assert.That(set1.Value1, Is.EqualTo("v"));
					Assert.That(set1.Value2, Is.Null);

					Assert.That(set2.Code, Is.EqualTo(1));
					Assert.That(set2.Value1, Is.EqualTo("Val1"));
					Assert.That(set2.Value2, Is.EqualTo("Val2"));

					Assert.That(set11, Is.EqualTo(set1));
					Assert.That(set22, Is.EqualTo(set2));
				});
			}
		}

		[Test]
		public void VariableResultsTestWithAnonymParam([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var set1 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 0 }).First();

				var set2 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 1 }).First();

				var set11 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 0 }).First();

				var set22 = db.QueryProc<VariableResult>("[VariableResults]",
					new { ReturnFullRow = 1 }).First();

				Assert.Multiple(() =>
				{
					Assert.That(set1.Code, Is.EqualTo(2));
					Assert.That(set1.Value1, Is.EqualTo("v"));
					Assert.That(set1.Value2, Is.Null);

					Assert.That(set2.Code, Is.EqualTo(1));
					Assert.That(set2.Value1, Is.EqualTo("Val1"));
					Assert.That(set2.Value2, Is.EqualTo("Val2"));

					Assert.That(set11, Is.EqualTo(set1));
					Assert.That(set22, Is.EqualTo(set2));
				});
			}
		}

		[Test]
		public void TestQueryProcRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input   = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = db.QueryProc<Person>("QueryProcParameters", input, output1, output2);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});

				persons.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[Test]
		public async Task TestQueryProcAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = await db.QueryProcAsync<Person>("QueryProcParameters", input, output1, output2);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});

				persons.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[Test]
		public void TestQueryProcTemplateRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = db.QueryProc(new Person(), "QueryProcParameters", input, output1, output2);

				persons.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[Test]
		public async Task TestQueryProcAsyncTemplateRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = await db.QueryProcAsync(new Person(), "QueryProcParameters", input, output1, output2);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});

				persons.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[Test]
		public void TestQueryProcReaderRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = db.QueryProc(reader => new Person(), "QueryProcParameters", input, output1, output2);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});

				persons.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[Test]
		public async Task TestQueryProcAsyncReaderRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var persons = await db.QueryProcAsync(reader => new Person(), "QueryProcParameters", input, output1, output2);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});

				persons.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		sealed class QueryProcMultipleResult
		{
			[ResultSetIndex(0)] public IEnumerable<Person> Persons { get; set; } = null!;
			[ResultSetIndex(1)] public IEnumerable<Doctor> Doctors { get; set; } = null!;
		}

		[Test]
		public void TestQueryProcMultipleRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output3 = new DataParameter("output3", null, DataType.Int32) { Direction = ParameterDirection.Output };
				db.QueryProcMultiple<Person>("QueryProcMultipleParameters", input, output1, output2, output3);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
					Assert.That(output3.Value, Is.EqualTo(4));
				});
			}
		}

		[Test]
		public async Task TestQueryProcMultipleAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output3 = new DataParameter("output3", null, DataType.Int32) { Direction = ParameterDirection.Output };
				await db.QueryProcMultipleAsync<Person>("QueryProcMultipleParameters", input, output1, output2, output3);
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
					Assert.That(output3.Value, Is.EqualTo(4));
				});
			}
		}

		[Test]
		public void TestExecuteProcIntRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = db.ExecuteProc("ExecuteProcIntParameters", input, output);
				Assert.Multiple(() =>
				{
					Assert.That(output.Value, Is.EqualTo(2));
					Assert.That(result, Is.EqualTo(1));
				});
			}
		}

		[Test]
		public async Task TestExecuteProcAsyncIntRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = await db.ExecuteProcAsync("ExecuteProcIntParameters", input, output);
				Assert.Multiple(() =>
				{
					Assert.That(output.Value, Is.EqualTo(2));
					Assert.That(result, Is.EqualTo(1));
				});
			}
		}

		[Test]
		public void TestExecuteProcTRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = db.ExecuteProc<string>("ExecuteProcStringParameters", input, output);
				Assert.Multiple(() =>
				{
					Assert.That(output.Value, Is.EqualTo(2));
					Assert.That(result, Is.EqualTo("издрасте"));
				});
			}
		}

		[Test]
		public async Task TestExecuteProcAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output = new DataParameter("output", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var result = await db.ExecuteProcAsync<string>("ExecuteProcStringParameters", input, output);
				Assert.Multiple(() =>
				{
					Assert.That(output.Value, Is.EqualTo(2));
					Assert.That(result, Is.EqualTo("издрасте"));
				});
			}
		}

		[Test]
		public void TestExecuteReaderProcRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var reader = new CommandInfo(db, "QueryProcParameters", input, output1, output2).ExecuteReaderProc();
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});
				using (reader)
					while (reader.Reader!.Read())
					{
					}

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[Test]
		public async Task TestExecuteReaderProcAsyncRebind([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var input = DataParameter.Int32("input", 1);
				var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
				var reader = await new CommandInfo(db, "QueryProcParameters", input, output1, output2).ExecuteReaderProcAsync();
				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.Null);
					Assert.That(output2.Value, Is.Null);
				});
				await using (reader)
					while (await reader.Reader!.ReadAsync())
					{
					}

				Assert.Multiple(() =>
				{
					Assert.That(output1.Value, Is.EqualTo(2));
					Assert.That(output2.Value, Is.EqualTo(3));
				});
			}
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4431")]
		public void Issue4431Test([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] bool closeAfterUse)
		{
			var interceptor = new CountingContextInterceptor();
			using var db = GetDataConnection(context, o => o.UseInterceptor(interceptor));
			((IDataContext)db).CloseAfterUse = closeAfterUse;

			var input = DataParameter.Int32("input", 1);
			var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var persons = db.QueryProc(reader => new Person(), "QueryProcParameters", input, output1, output2);

			persons.ToList();

			Assert.Multiple(() =>
			{
				Assert.That(interceptor.OnClosedCount, Is.EqualTo(closeAfterUse ? 1 : 0));
				Assert.That(interceptor.OnClosedAsyncCount, Is.EqualTo(0));
			});
		}
	}
}
