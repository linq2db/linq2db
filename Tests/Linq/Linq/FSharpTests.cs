using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class FSharpTests : TestBase
	{
		[Test]
		public void LoadSingle([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingle(db);
		}

		[Test]
		public void RecordParametersMapping([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordParametersMapping(db);
		}

		[ActiveIssue("F# unnecessary converts sub-query to enumerable leading to client-side filtering", SkipForLinqService = true)]
		[Test]
		public void RecordProjectionColumnsOnly([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordProjectionColumnsOnly(db);

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WHERE"));
			}
		}

		[ActiveIssue("F# unnecessary converts sub-query to enumerable leading to client-side filtering", SkipForLinqService = true)]
		[Test]
		public void RecordComplexProjection([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordComplexProjection(db);

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WHERE"));
			}
		}

		[Test]
		public void RecordProjectionAll([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.RecordProjectionAll(db);

			if (db is DataConnection dc)
			{
				Assert.That(dc.LastQuery, Contains.Substring("WHERE"));
			}
		}

		[Test]
		public void ComplexRecordParametersMapping([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.ComplexRecordParametersMapping(db);
		}

		[Test]
		public void ComplexRecordParametersMappingUsingRecordReaderBuilder([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataConnection(context);
			FSharp.WhereTest.ComplexRecordParametersMappingUsingRecordReaderBuilder(db);
		}

		[Test]
		public void UnionRecord1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.UnionRecord1(db);
		}

		[Test]
		public void UnionRecord2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.WhereTest.UnionRecord2(db);
		}

		[Test]
		public void LoadSinglesWithPatient([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSinglesWithPatient( db);
		}

		[Test]
		public void LoadSingleWithOptions([DataSources] string context)
		{
			var ms = FSharp.MappingSchema.Initialize();

			using (var db = GetDataContext(context, ms))
				FSharp.WhereTest.LoadSingleWithOptions(db);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/195")]
		public void BuiltInOptionsHandling([DataSources] string context)
		{
			using var db = GetDataContext(context);

			FSharp.WhereTest.LoadSingleWithOptions(db);
		}

		[Test]
		public void LoadSingleCLIMutable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleCLIMutable(db);
		}

		[Test]
		public void LoadSingleComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleComplexPerson(db);
		}

		[Test]
		public void LoadSingleDeeplyComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadSingleDeeplyComplexPerson(db);
		}

		[Test]
		public void LoadColumnOfDeeplyComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.WhereTest.LoadColumnOfDeeplyComplexPerson(db);
		}

		[Test]
		public void SelectField([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectField(db);
		}

		[Test]
		public void SelectFieldDeeplyComplexPerson([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectFieldDeeplyComplexPerson(db);
		}

		[Test]
		public void Insert1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.InsertTest.Insert1(db);
		}

		[Test]
		public void Insert2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.InsertTest.Insert2(db, context.IsAnyOf(TestProvName.AllClickHouse) ? 100 : 0);
		}

		[Test]
		public void SelectLeftJoin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.SelectTest.SelectLeftJoin(db);
		}

		[Test]
		public void TestIssue2678_SelectObject([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.Issue2678.InsertAndSelectObject(db);
		}

		[Test]
		public void TestIssue2678_SelectRecord([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				FSharp.Issue2678.InsertAndSelectRecord(db);
		}

		[Test(Description = "record type support")]
		public void Issue3357_FSharp1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3357.Union1(db);
		}

		[Test(Description = "record type support")]
		public void Issue3357_FSharp2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3357.Union2(db);
		}

		[Test(Description = "record type support")]
		public void Issue3357_FSharp3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3357.Union3(db);
		}

		[Test]
		public void Issue3699_Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.SelectTest.Issue3699Test(db);
		}

		[Test]
		public void Issue3743Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3743.Issue3743Test1(db, 1);
		}

		[Test]
		public void Issue3743Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue3743.Issue3743Test2(db, 1);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4132")]
		public void Issue4132Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue4132.Issue4132Test1(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4132")]
		public void Issue4132Test2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue4132.Issue4132Test2(db);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test1(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test2(db);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test3(db);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test4(db);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test5(db);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test6(db);
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1813")]
		public void Issue1813Test7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			FSharp.Issue1813.Issue1813Test7(db);
		}
	}
}
