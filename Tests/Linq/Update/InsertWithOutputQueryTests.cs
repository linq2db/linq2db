using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// <see cref="LinqExtensions.InsertWithOutputQuery{TTarget}(ITable{TTarget}, System.Linq.Expressions.Expression{Func{TTarget}})"/>
	/// and overloads: output of INSERT used as a composable query source (https://github.com/linq2db/linq2db/issues/5717).
	/// </summary>
	[TestFixture]
	public class InsertWithOutputQueryTests : TestBase
	{
		[Table("InsertOutputQuerySource")]
		sealed class SourceTable
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table("InsertOutputQueryTarget")]
		sealed class TargetTable
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table("InsertOutputQueryAudit")]
		sealed class AuditTable
		{
			[PrimaryKey]          public int     TargetId { get; set; }
			[Column(Length = 50)] public string? Text     { get; set; }
		}

		[Table("InsertOutputQueryInput")]
		sealed class EventInput
		{
			[PrimaryKey]                               public string PersistenceId  { get; set; } = null!;
			[PrimaryKey]                               public int    SequenceNumber { get; set; }
			[Column(Length = 50, CanBeNull = false)]   public string Tag            { get; set; } = null!;
		}

		[Table("InsertOutputQueryEvent")]
		sealed class EventRecord
		{
			[PrimaryKey, Identity]                     public int    Id             { get; set; }
			[Column(Length = 50, CanBeNull = false)]   public string PersistenceId  { get; set; } = null!;
			[Column]                                   public int    SequenceNumber { get; set; }
		}

		[Table("InsertOutputQueryEventTag")]
		sealed class EventTag
		{
			[PrimaryKey, Identity]                     public int    Id             { get; set; }
			[Column]                                   public int    EventId        { get; set; }
			[Column(Length = 50, CanBeNull = false)]   public string Tag            { get; set; } = null!;
		}

		[Table("InsertOutputQuerySeq")]
		sealed class SequenceTarget
		{
			[PrimaryKey, Identity, SequenceName("InsertOutputQuerySeq_Id_seq")] public int Id    { get; set; }
			[Column]                                                            public int Value { get; set; }
		}

		[Table("oas_probe")]
		sealed class ProbeTable
		{
			[Column("id"), PrimaryKey] public int Id { get; set; }
		}

		static SourceTable[] GetSourceData()
		{
			return Enumerable.Range(1, 10)
				.Select(i => new SourceTable { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
				.ToArray();
		}

		[Test]
		public void FromQuery_Select([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context, [Values(100, 200)] int param)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var output = source
				.Where(s => s.Id > 3)
				.InsertWithOutputQuery(
					target,
					s => new TargetTable
					{
						Id       = s.Id + param,
						Value    = s.Value + param,
						ValueStr = s.ValueStr + "_new",
					})
				.OrderBy(t => t.Id)
				.ToArray();

			output.Length.ShouldBe(7);
			output.Select(o => o.Id).ShouldBe(Enumerable.Range(4, 7).Select(i => i + param));
			output[0].Value.ShouldBe(-4 + param);
			output[0].ValueStr.ShouldBe("Str4_new");

			target.OrderBy(t => t.Id).Select(t => t.Id).ToArray().ShouldBe(output.Select(o => o.Id));
		}

		[Test]
		public void FromQuery_ProjectionFilterAndAggregate([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value * -1, ValueStr = s.ValueStr })
				.Select(t => new { t.Id, Doubled = t.Value * 2 });

			// filter and aggregate apply to the output rows; the insert itself is not filtered
			var count = inserted.Where(o => o.Doubled > 10).Count();

			count.ShouldBe(5);
			target.Count().ShouldBe(10);
		}

		[Test]
		public void SingleRecord_Select([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<TargetTable>();

			var output = target
				.InsertWithOutputQuery(() => new TargetTable { Id = 42, Value = 7, ValueStr = "single" })
				.Select(t => new { t.Id, t.ValueStr })
				.ToArray();

			output.Length.ShouldBe(1);
			output[0].Id.ShouldBe(42);
			output[0].ValueStr.ShouldBe("single");

			target.Single().Value.ShouldBe(7);
		}

		[Test]
		public void OutputFeedsInsert([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();
			using var audit  = db.CreateLocalTable<AuditTable>();

			// WITH t AS (INSERT ... RETURNING ...) INSERT INTO audit ... SELECT ... FROM t
			var affected = source
				.Where(s => s.Id <= 4)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id + 10, Value = s.Value, ValueStr = s.ValueStr })
				.Insert(audit, t => new AuditTable { TargetId = t.Id, Text = "inserted " + t.ValueStr });

			affected.ShouldBe(4);

			target.OrderBy(t => t.Id).Select(t => t.Id).ToArray().ShouldBe(new[] { 11, 12, 13, 14 });

			audit.OrderBy(a => a.TargetId).Select(a => a.TargetId).ToArray().ShouldBe(new[] { 11, 12, 13, 14 });
			audit.Single(a => a.TargetId == 11).Text.ShouldBe("inserted Str1");
		}

		[Test(Description = "https://github.com/linq2db/linq2db/discussions/5679")]
		public void GeneratedKeysJoinedBackToInput([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var input  = db.CreateLocalTable(new[]
			{
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 1, Tag = "auth" },
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 2, Tag = "profile" },
				new EventInput { PersistenceId = "actor-xyz", SequenceNumber = 1, Tag = "ping" },
			});
			using var events = db.CreateLocalTable<EventRecord>();
			using var tags   = db.CreateLocalTable<EventTag>();

			var inputCte = input.AsCte("input_data");

			var insertedEvents = inputCte
				.InsertWithOutputQuery(events, i => new EventRecord { PersistenceId = i.PersistenceId, SequenceNumber = i.SequenceNumber })
				.Select(e => new { e.Id, e.PersistenceId, e.SequenceNumber });

			// join generated identity values back to the input rows by domain key and insert dependent rows
			var affected = insertedEvents
				.Join(
					inputCte,
					e => new { e.PersistenceId, e.SequenceNumber },
					i => new { i.PersistenceId, i.SequenceNumber },
					(e, i) => new { EventId = e.Id, i.Tag })
				.Insert(tags, x => new EventTag { EventId = x.EventId, Tag = x.Tag });

			affected.ShouldBe(3);

			var result =
				(
					from t in tags
					join e in events on t.EventId equals e.Id
					orderby e.PersistenceId, e.SequenceNumber
					select new { e.PersistenceId, e.SequenceNumber, t.Tag }
				)
				.ToArray();

			result.Length.ShouldBe(3);
			result[0].ShouldBe(new { PersistenceId = "actor-abc", SequenceNumber = 1, Tag = "auth" });
			result[1].ShouldBe(new { PersistenceId = "actor-abc", SequenceNumber = 2, Tag = "profile" });
			result[2].ShouldBe(new { PersistenceId = "actor-xyz", SequenceNumber = 1, Tag = "ping" });
		}

		[Test]
		public void OutputReferencedTwiceInsertsOnce([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			var count = inserted.Select(t => t.Id).Concat(inserted.Select(t => t.Id)).Count();

			count.ShouldBe(6);
			target.Count().ShouldBe(3);
		}

		[Test]
		public void NotSupported([DataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			// thrown when the query is built, before any database access
			var query = db.GetTable<SourceTable>()
				.InsertWithOutputQuery(db.GetTable<TargetTable>(), s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			var ex = Assert.Throws<LinqToDBException>(() => query.ToArray());
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_NotSupported);
		}

		[Test]
		public void NotSupported_Remote([DataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetTable<SourceTable>()
				.InsertWithOutputQuery(db.GetTable<TargetTable>(), s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			var ex = Assert.Throws<LinqToDBException>(() => query.ToArray());
			ex.Message.ShouldContain(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_NotSupported);
		}

		[Test]
		public void SingleRecord_DefaultOutput([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<TargetTable>();

			var output = target
				.InsertWithOutputQuery(() => new TargetTable { Id = 42, Value = 7, ValueStr = "single" })
				.ToArray();

			output.Length.ShouldBe(1);
			output[0].Id.ShouldBe(42);
			output[0].Value.ShouldBe(7);
			output[0].ValueStr.ShouldBe("single");

			target.Single().Id.ShouldBe(42);
		}

		[Test]
		public void FromQuery_SequenceIdentity([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<SequenceTarget>();

			var output = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new SequenceTarget { Value = s.Value })
				.OrderBy(t => t.Id)
				.ToArray();

			output.Length.ShouldBe(3);
			output.Select(o => o.Id).Distinct().Count().ShouldBe(3);
			target.Count().ShouldBe(3);
		}

		[Test]
		public void EagerLoadOverOutputInsertsOnce([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var input  = db.CreateLocalTable(new[]
			{
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 1, Tag = "auth" },
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 2, Tag = "profile" },
				new EventInput { PersistenceId = "actor-xyz", SequenceNumber = 1, Tag = "ping" },
			});
			using var events = db.CreateLocalTable<EventRecord>();
			using var lookup = db.CreateLocalTable(GetSourceData());

			// children are keyed by the generated identity, so they match only if the
			// eager-load query sees the same insert as the main query
			var result = input
				.InsertWithOutputQuery(events, i => new EventRecord { PersistenceId = i.PersistenceId, SequenceNumber = i.SequenceNumber })
				.Select(e => new
				{
					e.Id,
					Children = lookup.Where(s => s.Id == e.Id).Select(s => s.Id).ToList(),
				})
				.ToArray();

			events.Count().ShouldBe(3);
			result.Length.ShouldBe(3);
			result.ShouldAllBe(r => r.Children.Count == 1 && r.Children[0] == r.Id);
		}

		[Test]
		public void EagerLoadOverOutput_UnionStrategyMarker([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var input  = db.CreateLocalTable(new[]
			{
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 1, Tag = "auth" },
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 2, Tag = "profile" },
				new EventInput { PersistenceId = "actor-xyz", SequenceNumber = 1, Tag = "ping" },
			});
			using var events = db.CreateLocalTable<EventRecord>();
			using var lookup = db.CreateLocalTable(GetSourceData());

			// an explicit strategy marker is overridden: children are keyed to the buffered output rows
			var result = input
				.InsertWithOutputQuery(events, i => new EventRecord { PersistenceId = i.PersistenceId, SequenceNumber = i.SequenceNumber })
				.Select(e => new
				{
					e.Id,
					Children = lookup.Where(s => s.Id == e.Id).Select(s => s.Id).ToList(),
				})
				.WithUnionLoadStrategy()
				.ToArray();

			events.Count().ShouldBe(3);
			result.Length.ShouldBe(3);
			result.ShouldAllBe(r => r.Children.Count == 1 && r.Children[0] == r.Id);
		}

		[Test]
		public void EagerLoadOverOutput_NotKeyableThrows([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();
			using var lookup = db.CreateLocalTable(GetSourceData());

			// the parent is referenced only in the child projection: children cannot be keyed to the output rows
			var query = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr })
				.Select(t => new
				{
					t.Id,
					Children = lookup.Select(s => s.Id + t.Id).ToList(),
				});

			var ex = Assert.Throws<LinqToDBException>(() => query.ToArray());
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_EagerLoad);

			target.Count().ShouldBe(0);
		}

		[Test]
		public void OutputInSubqueryPredicate([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// the data-modifying CTE referenced only from a subquery must still be hoisted to the top-level WITH
			var ids = source
				.Where(s => inserted.Select(i => i.Id).Contains(s.Id))
				.OrderBy(s => s.Id)
				.Select(s => s.Id)
				.ToArray();

			ids.ShouldBe(new[] { 1, 2, 3 });
			target.Count().ShouldBe(3);
		}

		[Test]
		public void OutputFeedsUpdate([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// WITH t AS (INSERT ... RETURNING ...) UPDATE ... WHERE EXISTS (SELECT ... FROM t)
			var affected = source
				.Where(s => inserted.Any(i => i.Id == s.Id))
				.Set(s => s.ValueStr, "copied")
				.Update();

			affected.ShouldBe(3);
			target.Count().ShouldBe(3);
			source.Count(s => s.ValueStr == "copied").ShouldBe(3);
		}

		[Test]
		public void UnreadOutputThrows([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// the output is referenced by the query but the column reading it is not projected:
			// the insert would be dropped with it, so the query is rejected instead of running without it
			var query = source
				.Select(s => new { s.Id, Inserted = inserted.Count() })
				.Select(x => x.Id);

			var ex = Assert.Throws<LinqToDBException>(() => query.ToArray());
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_NotRead);

			target.Count().ShouldBe(0);
		}

		// Guard test: keep SqlProviderFlags.IsInsertOutputQuerySupported honest. It probes whether the provider accepts the
		// data-modifying CTE that BasicSqlBuilder.BuildDataModificationCteBody renders and fails when reality diverges
		// from the declared flag, signalling that the provider's flag (set in its DataProvider) needs updating.
		[Test]
		public void OutputAsSourceSurface([DataSources(false)] string context)
		{
			using var _  = new DisableBaseline("probes provider capability");
			using var db = GetDataConnection(context);

			var actual = ProbeOutputAsSource(db);

			actual.ShouldBe(
				db.DataProvider.SqlProviderFlags.IsInsertOutputQuerySupported,
				$"Data-modifying CTE support for '{context}' diverged from SqlProviderFlags.IsInsertOutputQuerySupported; update the provider's flag.");
		}

		static bool ProbeOutputAsSource(DataConnection db)
		{
			// setup stays outside the try: a table-creation failure must surface as a test error rather than be
			// swallowed into a "not supported" verdict, which would make the guard pass vacuously
			using var t = db.CreateLocalTable<ProbeTable>();

			var sb    = ((IDataContext)db).CreateSqlBuilder();
			var table = sb.ConvertInline("oas_probe", LinqToDB.Internal.SqlProvider.ConvertType.NameToQueryTable);
			var id    = sb.ConvertInline("id",        LinqToDB.Internal.SqlProvider.ConvertType.NameToQueryField);

			try
			{
				var ids = db.Query<int>($"WITH t AS (INSERT INTO {table} ({id}) VALUES (1) RETURNING {id}) SELECT {id} FROM t").ToList();

				return ids.Count == 1 && ids[0] == 1 && t.Count() == 1;
			}
			catch
			{
				return false;
			}
		}
	}
}
