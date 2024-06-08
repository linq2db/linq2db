using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using Newtonsoft.Json;

using NUnit.Framework;

using DataType = LinqToDB.DataType;

namespace Tests.UserTests.Test4415
{
	[TestFixture]
	public class Test4415Tests : TestBase
	{
		public class LanguageDTO
		{
			public string? LanguageID { get; set; }

			public string? Name { get; set; }
		}

		[Test]
		public void TestIssue4415_Test1([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new LanguageDTO
					{
						Name = "english"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(2));
		}

		[Test]
		public void TestIssue4415_Test2([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNotNull()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						Name = "deutsch"
					},
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestIssue4415_Test3([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID)
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new LanguageDTO
					{
						Name = "english"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(Sql.AsNotNull(x.LanguageID)));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestIssue4415_Test4([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new LanguageDTO
					{
						Name = "english"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			// use complex column
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID) + "test");
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(1));
		}

		[Test]
		public void TestIssue4415_Test5([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						Name = "deutsch"
					},
					new LanguageDTO
					{
						Name = "english"
					}
				});

			var p_2 = db.GetTable<LanguageDTO>().ToList();
			var qry_2 = p_2.GroupBy(x => x.Name).Select(x => (x.Max(y => y.LanguageID) ?? "") + "test");
			var qry2_2 = p_2.Where(x => qry_2.Contains(x.LanguageID));
			var expected = qry2_2.ToList();

			var p = db.GetTable<LanguageDTO>();
			// use complex column
			var qry = p.GroupBy(x => x.Name).Select(x => (x.Max(y => y.LanguageID) ?? "") + "test");
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.That(lst, Has.Count.EqualTo(expected.Count));
		}
	}
}
