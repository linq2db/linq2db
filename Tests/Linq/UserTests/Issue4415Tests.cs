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

			public string? AlternativeLanguageID { get; set; }

			public int Order { get; set; }

			public string? IsoCode { get; set; }
		}

		[Test]
		public void TestIssue4415_Test1([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsPrimaryKey()
						.Property(e => e.AlternativeLanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID    = "de",
						Name = "deutsch"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.AreEqual(1, lst.Count);
		}

		[Test]
		public void TestIssue4415_Test2([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable().IsPrimaryKey()
						.Property(e => e.AlternativeLanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID    = "de",
						Name = "deutsch"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.AreEqual(1, lst.Count);
		}

		[Test]
		public void TestIssue4415_Test3([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNotNull().IsPrimaryKey()
						.Property(e => e.AlternativeLanguageID).IsNullable()
					.Build()
					.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID    = "de",
						Name = "deutsch"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var qry2 = p.Where(x => qry.Contains(x.LanguageID));
			var lst = qry2.ToList();

			Assert.AreEqual(1, lst.Count);
		}

		[Test]
		public void TestIssue4415_Test4([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllOracle)] string configuration)
		{
			var ms = new FluentMappingBuilder()
				.Entity<LanguageDTO>()
					.HasTableName("Common_Language")
					.Property(e => e.LanguageID).IsPrimaryKey()
					.Property(e => e.AlternativeLanguageID).IsNullable()
				.Build()
				.MappingSchema;

			using var db = GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID    = "de",
						Name = "deutsch"
					}
				});

			var p = db.GetTable<LanguageDTO>();
			var qry = p.GroupBy(x => x.Name).Select(x => x.Max(y => y.LanguageID));
			var lst = qry.ToList();

			Assert.AreEqual(1, lst.Count);
		}
	}
}
