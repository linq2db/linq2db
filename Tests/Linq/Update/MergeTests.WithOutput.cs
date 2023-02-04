﻿using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Async;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void MergeWithOutputFull([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutput((a, deleted, inserted) => new {a, deleted, inserted});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.a.Should().Be("INSERT");
				record.deleted.Id.Should().Be(0);

				record.inserted.Id.Should().Be(5);
				record.inserted.Field1.Should().Be(10);
			}
		}

		[Test]
		public void MergeWithOutputWithoutAction([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird3Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutput((a, deleted, inserted) => new { deleted, inserted});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.deleted.Id.Should().Be(0);

				record.inserted.Id.Should().Be(5);
				record.inserted.Field1.Should().Be(10);
			}
		}

		[Test]
		public async Task MergeWithOutputFullAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutputAsync((a, deleted, inserted) => new {a, deleted, inserted});

				var cnt = 0;
				await foreach (var record in outputRows)
				{
					cnt++;

					record.a.Should().Be("INSERT");
					record.deleted.Id.Should().Be(0);

					record.inserted.Id.Should().Be(5);
					record.inserted.Field1.Should().Be(10);
				}

				Assert.AreEqual(1, cnt);
			}
		}

		[Test]
		public async Task MergeWithOutputWithoutActionAsync([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird3Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutputAsync((a, deleted, inserted) => new {deleted, inserted});

				var hasRecord = false;
				await foreach (var record in outputRows)
				{
					Assert.False(hasRecord);
					hasRecord = true;

					record.deleted.Id.Should().Be(0);

					record.inserted.Id.Should().Be(5);
					record.inserted.Field1.Should().Be(10);
				}

				Assert.True(hasRecord);
			}
		}

		[Test]
		public void MergeWithOutputProjected([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutput((a, deleted, inserted) => new {a, inserted.Id});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.a.Should().Be("INSERT");

				record.Id.Should().Be(5);
			}
		}

		[Test]
		public void MergeWithOutputSource([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutput((a, deleted, inserted, source) => new
					{
						source.Field1,
						Filed1 = Sql.AsSql(source.Field1.ToString()),
						a,
						Id = Sql.AsSql(inserted.Id.ToString())
					});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.a.Should().Be("INSERT");

				record.Id.Should().Be("5");
			}
		}

		[Test]
		public void MergeWithOutputSourceNoAction([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird3Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutput((a, deleted, inserted, source) => new
					{
						source.Field1,
						Filed1 = Sql.AsSql(source.Field1.ToString()),
						Id = Sql.AsSql(inserted.Id.ToString())
					});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.Id.Should().Be("5");
			}
		}

		[Test]
		public void MergeWithOutputFromQuery([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird3Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAnd(
						(t, s) => s.Id == 3,
						(t, s) => new TestMapping1()
						{
							Field1 = t.Field1 + s.Field5
						})
					.MergeWithOutput((a, deleted, inserted, source) => new
					{
						// Field2 used only in output
						source.Field2
					});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);
				var record = result[0];

				record.Field2.Should().Be(3);
			}
		}

		[Test]
		public void MergeWithOutputProjectedWithoutAction([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllFirebird3Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var outputRows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutput((a, deleted, inserted) => new {inserted.Id});

				var result = outputRows.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.Id.Should().Be(5);
			}
		}

		sealed class InsertTempTable
		{
			public string? Action    { get; set; }
			public int     NewId     { get; set; }
			public int?    DeletedId { get; set; }
			public int?    SourceId  { get; set; }
		}

		[Test]
		public void MergeWithOutputInto([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var temp = db.CreateTempTable<InsertTempTable>("#InsertTempTable");

				PrepareData(db);

				var table = GetTarget(db);

				var affected = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutputInto(temp,
						(a, deleted, inserted, source) => new ()
						{
							Action    = a,
							NewId     = inserted.Id,
							DeletedId = deleted.Id,
							SourceId  = source.Id + 1
						}
					);

				affected.Should().Be(1);

				var result = temp.ToArray();

				result.Should().HaveCount(1);

				var record = result[0];

				record.Action.   Should().Be("INSERT");
				record.NewId.    Should().Be(5);
				record.DeletedId.Should().BeNull();
				record.SourceId. Should().Be(6);
			}
		}

		[Test]
		public void MergeWithOutputIntoTempTable([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var temp = db.CreateTempTable<InsertTempTable>("InsertTempTable", tableOptions: TableOptions.IsTemporary);

				PrepareData(db);

				var table = GetTarget(db);

				var affected = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutputInto(temp,
						(a, deleted, inserted) => new InsertTempTable { Action = a, NewId = inserted.Id, DeletedId = deleted.Id }
					);

				affected.Should().Be(1);

				var result = temp.ToArray();

				result.Should().HaveCount(1);
				result.Should().HaveCount(1);

				var record = result[0];

				record.Action.Should().Be("INSERT");

				record.NewId.Should().Be(5);
				record.DeletedId.Should().BeNull();
			}
		}

		[Test]
		public void MergeWithOutputIntoNonTemp([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (var temp = db.CreateLocalTable<InsertTempTable>())
			{
				PrepareData(db);

				var table = GetTarget(db);

				var affected = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutputInto(temp,
						(a, deleted, inserted) => new InsertTempTable { Action = a, NewId = inserted.Id, DeletedId = deleted.Id }
					);

				affected.Should().Be(1);

				var result = temp.ToArray();

				result.Should().HaveCount(1);
				result.Should().HaveCount(1);

				var record = result[0];

				record.Action.Should().Be("INSERT");

				record.NewId.Should().Be(5);
				record.DeletedId.Should().BeNull();
			}
		}

		[Test]
		public async Task MergeWithOutputIntoAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var temp = db.CreateTempTable<InsertTempTable>("#InsertTempTable");

				PrepareData(db);

				var table = GetTarget(db);

				var affected = await table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == 5))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.MergeWithOutputIntoAsync(temp,
						(a, deleted, inserted) => new InsertTempTable { Action = a, NewId = inserted.Id, DeletedId = deleted.Id }
					);

				affected.Should().Be(1);

				var result = await temp.ToArrayAsync();

				result.Should().HaveCount(1);
				result.Should().HaveCount(1);

				var record = result[0];

				record.Action.Should().Be("INSERT");

				record.NewId.Should().Be(5);
				record.DeletedId.Should().BeNull();
			}
		}

	}
}
