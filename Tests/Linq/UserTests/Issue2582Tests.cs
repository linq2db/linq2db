using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2582Tests : TestBase
	{
		class Issue2582Class1
		{
			public int Id { get; set; }
			public string? Value { get; set; }
			public string? Value2 { get; set; }
		}

		class Issue2582Class2
		{
			public int Id { get; set; }
			public string? Value { get; set; }
			public string? Value2 { get; set; }
		}

		[Test]
		public void TestIssue2582([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.GetFluentMappingBuilder()
				.Entity<Issue2582Class1>()
				.HasTableName("TB_Issue2582Class1")
				.Property(m => m.Id).IsPrimaryKey();
			var data1 = new[] { new Issue2582Class1 { Id = 1, Value = "Hello World" } };

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable(data1))
			{
				using (var rd = ((DataConnection)db).ExecuteReader("select * from TB_Issue2582Class1"))
				{ }
			}

			Assert.Throws(typeof(InvalidOperationException), () => { 
				ms.GetFluentMappingBuilder()
					.Entity<Issue2582Class2>()
					.HasTableName("TB_Issue2582Class2")
					.Property(m => m.Id).IsPrimaryKey();
				var data2 = new[] { new Issue2582Class2 { Id = 1, Value = "Hello World" } };

				using (var db = GetDataContext(context, ms))
				using (db.CreateLocalTable(data1))
				using (db.CreateLocalTable(data2))
				{
					using (var rd = ((DataConnection)db).ExecuteReader("select * from TB_Issue2582Class1"))
					{ }
					using (var rd = ((DataConnection)db).ExecuteReader("select * from TB_Issue2582Class2"))
					{ }
				}
			});
		}
	}
}
