﻿using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.UserTests
{
	/// <summary>
	/// Test fixes to Issue #1305.
	/// Before fix fields in derived tables were added first in the column order by <see cref="DataExtensions.CreateTable{T}(IDataContext, string, string, string, string, string, LinqToDB.SqlQuery.DefaultNullable)"/>.
	/// </summary>
	[TestFixture]
	public class Issue1363Tests : TestBase
	{
		[Table("Issue1363")]
		public sealed class Issue1363Record
		{
			[Column("required_field")]
			public Guid Required { get; set; }

			[Column("optional_field")]
			public Guid? Optional { get; set; }
		}

		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.Access)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.SqlCe)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.MySql)]
		[ActiveIssue("CreateTable(Guid)", Configuration = TestProvName.MariaDB)]
		[ActiveIssue("CreateTable(Guid)", Configuration = TestProvName.MySql57)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.DB2)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.Sybase)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.SybaseManaged)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.Firebird)]
		[ActiveIssue("CreateTable(Guid)", Configuration = TestProvName.Firebird3)]
		[ActiveIssue("CreateTable(Guid)", Configuration = ProviderName.Informix)]
		[Test, DataContextSource]
		public void TestInsert(string context)
		{
			using (var db = GetDataContext(context))
			using (var tbl = db.CreateLocalTable<Issue1363Record>())
			{
				var id1 = Guid.NewGuid();
				var id2 = Guid.NewGuid();

				insert(id1, null);
				insert(id2, id1);

				var record = tbl.Where(_ => _.Required == id2).Single();
				Assert.AreEqual(id1, record.Optional);

				void insert(Guid id, Guid? testId)
				{
					tbl.Insert(() => new Issue1363Record()
					{
						Required = id,
						Optional = tbl.Where(_ => _.Required == testId).Select(_ => (Guid?)_.Required).SingleOrDefault()
					});
				}
			}
		}
	}
}
