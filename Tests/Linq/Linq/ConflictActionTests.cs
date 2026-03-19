using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Linq;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.DataProvider.MySql;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.Tools;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ConflictActionTests : TestBase
	{
		[Table]
		sealed class IgnoreConflictsTable
		{
			[Column, PrimaryKey] public int ID { get; set; }
			[Column] public string Value { get; set; } = null!;
		}

		[Test]
		public void IgnoreConflictsTest([IncludeDataSources(
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<IgnoreConflictsTable>();

			// insert initial rows
			table.BulkCopy(
				new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows },
				new[]
				{
					new IgnoreConflictsTable { ID = 1, Value = "original1" },
					new IgnoreConflictsTable { ID = 2, Value = "original2" },
				});

			// second insert: rows 1 and 2 conflict, row 3 is new
			table.BulkCopy(
				new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows, ConflictAction = ConflictAction.Ignore },
				new[]
				{
					new IgnoreConflictsTable { ID = 1, Value = "conflict1" },
					new IgnoreConflictsTable { ID = 2, Value = "conflict2" },
					new IgnoreConflictsTable { ID = 3, Value = "new3"      },
				});

			var rows = table.OrderBy(r => r.ID).ToArray();

			Assert.That(rows, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(rows[0].Value, Is.EqualTo("original1"));
				Assert.That(rows[1].Value, Is.EqualTo("original2"));
				Assert.That(rows[2].Value, Is.EqualTo("new3"));
			}
		}

		[Test]
		public async Task IgnoreConflictsTestAsync([IncludeDataSources(
			TestProvName.AllMySql,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<IgnoreConflictsTable>();

			// insert initial rows
			await table.BulkCopyAsync(
				new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows },
				new[]
				{
					new IgnoreConflictsTable { ID = 1, Value = "original1" },
					new IgnoreConflictsTable { ID = 2, Value = "original2" },
				});

			// second insert: rows 1 and 2 conflict, row 3 is new
			await table.BulkCopyAsync(
				new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows, ConflictAction = ConflictAction.Ignore },
				new[]
				{
					new IgnoreConflictsTable { ID = 1, Value = "conflict1" },
					new IgnoreConflictsTable { ID = 2, Value = "conflict2" },
					new IgnoreConflictsTable { ID = 3, Value = "new3"      },
				});

			var rows = await table.OrderBy(r => r.ID).ToArrayAsync();

			Assert.That(rows, Has.Length.EqualTo(3));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(rows[0].Value, Is.EqualTo("original1"));
				Assert.That(rows[1].Value, Is.EqualTo("original2"));
				Assert.That(rows[2].Value, Is.EqualTo("new3"));
			}
		}

	}
}
