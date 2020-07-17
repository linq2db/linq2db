using System;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class TestTemplate : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var result = table.ToArray();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2348")]
		public void SchemaOnlyTestIssue2348([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = (DataConnection)GetDataContext(context);

			var schema = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions
			{
				GetTables     = false,
				GetProcedures = true
			});
		}
	}
}
