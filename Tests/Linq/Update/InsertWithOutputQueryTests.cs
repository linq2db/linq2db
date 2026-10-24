using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Mapping;

using Npgsql;

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

		[Table("InsertOutputQueryIdentity")]
		sealed class IdentityTarget
		{
			[PrimaryKey, Identity] public int Id    { get; set; }
			[Column]               public int Value { get; set; }
		}

		[Table("InsertOutputQueryTph")]
		[InheritanceMapping(Code = 1, Type = typeof(TphBase), IsDefault = true)]
		[InheritanceMapping(Code = 2, Type = typeof(TphDerived))]
		class TphBase
		{
			[PrimaryKey]                     public int Id    { get; set; }
			[Column(IsDiscriminator = true)] public int Kind  { get; set; }
			[Column]                         public int Value { get; set; }
		}

		sealed class TphDerived : TphBase
		{
		}

		[Table("InsertOutputQueryAssociated")]
		sealed class AssociatedTarget
		{
			[PrimaryKey] public int Id       { get; set; }
			[Column]     public int ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(SourceTable.Id))]
			public SourceTable? Parent { get; set; }
		}

		public enum EagerLoadExecution
		{
			Enumerate,
			EnumerateAsync,
			CompiledElement,
		}

		public enum AsyncEnumeration
		{
			AwaitForeach,
			ForEachAsync,
		}

		static IQueryable<IdentityTarget> InsertValue(ITable<IdentityTarget> target, int value)
		{
			return target.InsertWithOutputQuery(() => new IdentityTarget { Value = value });
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

			var expectedIds = Enumerable
				.Range(4, 7)
				.Select(i => i + param)
				.ToArray();

			output.Length.ShouldBe(7);
			output
				.Select(o => o.Id)
				.ShouldBe(expectedIds);
			output[0].Value.ShouldBe(-4 + param);
			output[0].ValueStr.ShouldBe("Str4_new");

			var targetIds = target
				.OrderBy(t => t.Id)
				.Select(t => t.Id)
				.ToArray();

			targetIds.ShouldBe(expectedIds);
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
			var count = inserted
				.Where(o => o.Doubled > 10)
				.Count();

			count.ShouldBe(5);
			target
				.Count()
				.ShouldBe(10);
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

			var row = target.Single();

			row.Value.ShouldBe(7);
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

			var targetIds = target
				.OrderBy(t => t.Id)
				.Select(t => t.Id)
				.ToArray();

			targetIds.ShouldBe(new[] { 11, 12, 13, 14 });

			var auditIds = audit
				.OrderBy(a => a.TargetId)
				.Select(a => a.TargetId)
				.ToArray();

			auditIds.ShouldBe(new[] { 11, 12, 13, 14 });

			var first = audit.Single(a => a.TargetId == 11);

			first.Text.ShouldBe("inserted Str1");
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
			var joined =
				from e in insertedEvents
				join i in inputCte on new { e.PersistenceId, e.SequenceNumber } equals new { i.PersistenceId, i.SequenceNumber }
				select new { EventId = e.Id, i.Tag };

			var affected = joined.Insert(tags, x => new EventTag { EventId = x.EventId, Tag = x.Tag });

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

			var count = inserted
				.Select(t => t.Id)
				.Concat(inserted.Select(t => t.Id))
				.Count();

			count.ShouldBe(6);
			target
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void NotSupported([DataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			// thrown when the query is built, before any database access
			var query = db.GetTable<SourceTable>()
				.InsertWithOutputQuery(db.GetTable<TargetTable>(), s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			Action act = () => query.ToArray();

			var ex = act.ShouldThrow<LinqToDBException>();
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_NotSupported);
		}

		[Test]
		public void NotSupported_Remote([DataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			var query = db.GetTable<SourceTable>()
				.InsertWithOutputQuery(db.GetTable<TargetTable>(), s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			Action act = () => query.ToArray();

			var ex = act.ShouldThrow<LinqToDBException>();
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

			var row = target.Single();

			row.Id.ShouldBe(42);
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
			output
				.Select(o => o.Id)
				.Distinct()
				.Count()
				.ShouldBe(3);
			target
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void FromQuery_PreV18Dialect([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			// PostgreSQL < 18 has no NEW/OLD in RETURNING: the nested output must reference the target table.
			// The v15 dialect runs against the test server, which accepts both forms.
			var provider = PostgreSQLTools.GetDataProvider(PostgreSQLVersion.v15);
			var cs       = DataConnection.GetConnectionString(context);

			using var cn = new NpgsqlConnection(cs);

			var options = new DataOptions()
				.UseConnection(provider, cn);

			using var db     = new DataConnection(options);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var query = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr })
				.OrderBy(t => t.Id)
				.Select(t => t.Id);

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("NEW.");

			query
				.ToArray()
				.ShouldBe(new[] { 1, 2, 3 });
			target
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void EagerLoadOverOutputInsertsOnce([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var _      = context.IsRemote() ? new DisableBaseline("Only direct access commits the implicit load transaction") : null;
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
					Children = lookup
						.Where(s => s.Id == e.Id)
						.Select(s => s.Id)
						.ToList(),
				})
				.ToArray();

			events
				.Count()
				.ShouldBe(3);
			result.Length.ShouldBe(3);
			result.ShouldAllBe(r => r.Children.Count == 1 && r.Children[0] == r.Id);
		}

		[Test]
		public void EagerLoadOverOutput_UnionStrategyMarker([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var _      = context.IsRemote() ? new DisableBaseline("Only direct access commits the implicit load transaction") : null;
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
					Children = lookup
						.Where(s => s.Id == e.Id)
						.Select(s => s.Id)
						.ToList(),
				})
				.WithUnionLoadStrategy()
				.ToArray();

			events
				.Count()
				.ShouldBe(3);
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
					Children = lookup
						.Select(s => s.Id + t.Id)
						.ToList(),
				});

			Action act = () => query.ToArray();

			var ex = act.ShouldThrow<LinqToDBException>();
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_EagerLoad);

			target
				.Count()
				.ShouldBe(0);
		}

		[Test]
		public void CapturedConditionRemovesOutput([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context, [Values] bool flag)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// a condition on a captured value is evaluated before translation: when it removes the output,
			// the insert does not run and nothing throws (documented on InsertWithOutputQuery)
			var result = source
				.Where(s => s.Id == 2)
				.Select(s => new { s.Id, C = flag ? inserted.Count() : 0 })
				.ToArray();

			result.Length.ShouldBe(1);
			result[0].C.ShouldBe(flag ? 3 : 0);

			target
				.Count()
				.ShouldBe(flag ? 3 : 0);
		}

		[Test]
		public void EagerLoadReadsOutputInParentAndChildThrows([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// the child query would carry its own copy of the data-modifying CTE and insert a second time
			var query = inserted
				.Select(p => new
				{
					p.Id,
					Same = inserted
						.Where(c => c.Value == p.Value)
						.Select(c => c.Id)
						.ToList(),
				});

			Action act = () => query.ToArray();

			var ex = act.ShouldThrow<LinqToDBException>();
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_EagerLoad);

			target
				.Count()
				.ShouldBe(0);
		}

		[Test]
		public void EagerLoadReadsOutputInChildOnlyThrows([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// the output is read, but only by the eager-loaded child query
			var query = source
				.Select(s => new
				{
					s.Id,
					Children = inserted
						.Where(i => i.Id == s.Id)
						.Select(i => i.Id)
						.ToList(),
				});

			Action act = () => query.ToArray();

			var ex = act.ShouldThrow<LinqToDBException>();
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_EagerLoad);

			target
				.Count()
				.ShouldBe(0);
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
			target
				.Count()
				.ShouldBe(3);
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
			target
				.Count()
				.ShouldBe(3);
			source
				.Count(s => s.ValueStr == "copied")
				.ShouldBe(3);
		}

		[Test]
		public void UnreadOutputStillInserts([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var inserted = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr });

			// the member reading the output is not projected: the insert is part of the query, so it still runs once
			var ids = source
				.Select(s => new { s.Id, Inserted = inserted.Count() })
				.Select(x => x.Id)
				.OrderBy(id => id)
				.ToArray();

			ids.ShouldBe(Enumerable.Range(1, 10));
			target
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void OutputCountOnly([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var count = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr })
				.Count();

			count.ShouldBe(3);
			target
				.Count()
				.ShouldBe(3);
		}

		[Test, QueryCacheTest]
		public void OneQueryableTwiceThenTwoCalls([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			// both queries have the same shape, so the second one meets the first one's cache entry
			var once = InsertValue(target, 11);

			var onceValues = once
				.Select(t => t.Value)
				.Concat(once.Select(t => t.Value))
				.ToArray();

			onceValues.ShouldBe(new[] { 11, 11 });
			target
				.Count()
				.ShouldBe(1);

			var twiceValues = InsertValue(target, 12)
				.Select(t => t.Value)
				.Concat(
					InsertValue(target, 13)
						.Select(t => t.Value))
				.ToArray();

			twiceValues
				.OrderBy(v => v)
				.ShouldBe(new[] { 12, 13 });
			target
				.Count()
				.ShouldBe(3);
		}

		[Test, QueryCacheTest]
		public void TwoCallsThenOneQueryableTwice([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			// both queries have the same shape, so the second one meets the first one's cache entry
			var twiceValues = InsertValue(target, 22)
				.Select(t => new { t.Value })
				.Concat(
					InsertValue(target, 23)
						.Select(t => new { t.Value }))
				.ToArray();

			twiceValues
				.Select(v => v.Value)
				.OrderBy(v => v)
				.ShouldBe(new[] { 22, 23 });
			target
				.Count()
				.ShouldBe(2);

			var once = InsertValue(target, 21);

			var onceValues = once
				.Select(t => new { t.Value })
				.Concat(once.Select(t => new { t.Value }))
				.ToArray();

			onceValues
				.Select(v => v.Value)
				.ShouldBe(new[] { 21, 21 });
			target
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void TwoIdenticalCallsInsertTwice([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			var value  = 5;
			var first  = target.InsertWithOutputQuery(() => new IdentityTarget { Value = value });
			var second = target.InsertWithOutputQuery(() => new IdentityTarget { Value = value });

			var ids = first
				.Select(t => t.Id)
				.Concat(second.Select(t => t.Id))
				.ToArray();

			ids
				.Distinct()
				.Count()
				.ShouldBe(2);
			target
				.Count()
				.ShouldBe(2);
		}

		[Test, QueryCacheTest]
		public void OutputQueryIsCached([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			InsertValue(target, 1)
				.Select(t => new { t.Value })
				.ToArray();

			var query = InsertValue(target, 2)
				.Select(t => new { t.Value });
			var miss  = query.GetCacheMissCount();

			query.ToArray();

			query
				.GetCacheMissCount()
				.ShouldBe(miss);
			target
				.Count()
				.ShouldBe(2);
		}

		[Test, QueryCacheTest]
		public void TwoOutputQueriesAreCached([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			// two calls in one query record the constants pattern in the cache key, which must still match
			InsertValue(target, 1)
				.Select(t => new { t.Value })
				.Concat(
					InsertValue(target, 2)
						.Select(t => new { t.Value }))
				.ToArray();

			var query = InsertValue(target, 3)
				.Select(t => new { t.Value })
				.Concat(
					InsertValue(target, 4)
						.Select(t => new { t.Value }));
			var miss  = query.GetCacheMissCount();

			var values = query.ToArray();

			query
				.GetCacheMissCount()
				.ShouldBe(miss);
			values
				.Select(v => v.Value)
				.OrderBy(v => v)
				.ShouldBe(new[] { 3, 4 });
			target
				.Count()
				.ShouldBe(4);
		}

		[Test]
		public void CompiledQueryInsertsPerInvocation([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			var query = CompiledQuery.Compile((IDataContext dc, int value) =>
				dc.GetTable<IdentityTarget>()
					.InsertWithOutputQuery(() => new IdentityTarget { Value = value })
					.Select(t => t.Value)
					.Sum());

			query(db, 4)
				.ShouldBe(4);
			query(db, 5)
				.ShouldBe(5);

			target
				.OrderBy(t => t.Value)
				.Select(t => t.Value)
				.ToArray()
				.ShouldBe(new[] { 4, 5 });
		}

		[Test]
		public void CompiledQueryTwoIdenticalCalls([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateLocalTable<IdentityTarget>();

			var query = CompiledQuery.Compile((IDataContext dc, int value) =>
				dc.GetTable<IdentityTarget>()
					.InsertWithOutputQuery(() => new IdentityTarget { Value = value })
					.Select(t => t.Value)
					.Concat(dc.GetTable<IdentityTarget>()
						.InsertWithOutputQuery(() => new IdentityTarget { Value = value })
						.Select(t => t.Value))
					.Count());

			query(db, 7)
				.ShouldBe(2);
			target
				.Count()
				.ShouldBe(2);
		}

		[Test]
		public void CompiledConditionKeepsInsert([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<IdentityTarget>();

			// a compiled argument is not evaluated before translation: the call stays in the query for both values
			var query = CompiledQuery.Compile((IDataContext dc, bool flag) =>
				dc.GetTable<SourceTable>()
					.Where(s => s.Id == 2)
					.Select(s => flag
						? dc.GetTable<IdentityTarget>()
							.InsertWithOutputQuery(() => new IdentityTarget { Value = 1 })
							.Count()
						: 0)
					.First());

			query(db, true)
				.ShouldBe(1);
			query(db, false)
				.ShouldBe(0);

			target
				.Count()
				.ShouldBe(2);
		}

		[Test]
		public void InlineCallInQueryLambda([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var result = source
				.Where(s => s.Id == 2)
				.Select(s => new
				{
					s.Id,
					Inserted = target
						.InsertWithOutputQuery(() => new TargetTable { Id = 42, Value = 1, ValueStr = "inline" })
						.Count(),
				})
				.ToArray();

			var row = result.Single();

			row.Inserted.ShouldBe(1);
			target
				.Count()
				.ShouldBe(1);
		}

		[Test]
		public void ChainedOutputQueries([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();
			using var audit  = db.CreateLocalTable<AuditTable>();

			var audited = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new TargetTable { Id = s.Id, Value = s.Value, ValueStr = s.ValueStr })
				.InsertWithOutputQuery(audit, t => new AuditTable { TargetId = t.Id, Text = t.ValueStr })
				.Select(a => a.TargetId)
				.OrderBy(id => id)
				.ToArray();

			audited.ShouldBe(new[] { 1, 2, 3 });
			target
				.Count()
				.ShouldBe(3);
			audit
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void DerivedTypeTarget([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<TphBase>();

			var output = db.GetTable<TphDerived>()
				.InsertWithOutputQuery(() => new TphDerived { Id = 1, Kind = 2, Value = 5 })
				.ToArray();

			output.Length.ShouldBe(1);
			output[0].ShouldBeOfType<TphDerived>();
			output[0].Value.ShouldBe(5);

			table
				.Count()
				.ShouldBe(1);
		}

		[Test]
		public void AssociationOverOutput([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<AssociatedTarget>();

			var names = source
				.Where(s => s.Id <= 3)
				.InsertWithOutputQuery(target, s => new AssociatedTarget { Id = s.Id, ParentId = s.Id })
				.Select(t => t.Parent!.ValueStr)
				.ToArray();

			names
				.OrderBy(n => n)
				.ShouldBe(new[] { "Str1", "Str2", "Str3" });
			target
				.Count()
				.ShouldBe(3);
		}

		[Test]
		public void CorrelatedCallThrows([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable(GetSourceData());
			using var target = db.CreateLocalTable<TargetTable>();

			var query = source
				.Where(s => s.Id <= 2)
				.Select(s => new
				{
					s.Id,
					Inserted = target
						.InsertWithOutputQuery(() => new TargetTable { Id = s.Id + 100, Value = s.Value, ValueStr = s.ValueStr })
						.Count(),
				});

			Action act = () => query.ToArray();

			var ex = act.ShouldThrow<LinqToDBException>();
			ex.Message.ShouldBe(LinqToDB.Internal.Common.ErrorHelper.Error_OutputQuery_Correlated);

			target
				.Count()
				.ShouldBe(0);
		}

		[Test]
		public async Task EagerLoadOverOutputCommitsLoadTransaction([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] EagerLoadExecution execution)
		{
			var begun     = 0;
			var committed = 0;

			using var db = GetDataConnection(context, o => o.UseTracing(e =>
			{
				if (e.TraceInfoStep != TraceInfoStep.BeforeExecute)
					return;

				if (e.Operation == TraceOperation.BeginTransaction)
					begun++;
				else if (e.Operation == TraceOperation.CommitTransaction)
					committed++;
			}));

			using var input  = db.CreateLocalTable(new[]
			{
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 1, Tag = "auth" },
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 2, Tag = "profile" },
				new EventInput { PersistenceId = "actor-xyz", SequenceNumber = 1, Tag = "ping" },
			});
			using var events = db.CreateLocalTable<EventRecord>();
			using var lookup = db.CreateLocalTable(GetSourceData());

			begun     = 0;
			committed = 0;

			var query = input
				.InsertWithOutputQuery(events, i => new EventRecord { PersistenceId = i.PersistenceId, SequenceNumber = i.SequenceNumber })
				.Select(e => new
				{
					e.Id,
					Children = lookup
						.Where(s => s.Id == e.Id)
						.Select(s => s.Id)
						.ToList(),
				});

			switch (execution)
			{
				case EagerLoadExecution.Enumerate:
					var list = query.ToList();

					list.Count.ShouldBe(3);
					break;

				case EagerLoadExecution.EnumerateAsync:
					var asyncList = await query.ToListAsync();

					asyncList.Count.ShouldBe(3);
					break;

				case EagerLoadExecution.CompiledElement:
					var compiled = CompiledQuery.Compile((DataConnection dc) =>
						dc.GetTable<EventInput>()
							.InsertWithOutputQuery(dc.GetTable<EventRecord>(), i => new EventRecord { PersistenceId = i.PersistenceId, SequenceNumber = i.SequenceNumber })
							.Select(e => new
							{
								e.Id,
								Children = dc.GetTable<SourceTable>()
									.Where(s => s.Id == e.Id)
									.Select(s => s.Id)
									.ToList(),
							})
							.First());

					var element = compiled(db);

					element.Children.Count.ShouldBe(1);
					break;
			}

			events
				.Count()
				.ShouldBe(3);
			begun.ShouldBe(1);
			committed.ShouldBe(1);
		}

		[Test]
		public async Task EagerLoadOverOutputKeepsTransactionStartedWhileEnumerating([IncludeDataSources(TestProvName.AllPostgreSQL)] string context, [Values] AsyncEnumeration enumeration)
		{
			using var db     = GetDataConnection(context);
			using var input  = db.CreateLocalTable(new[]
			{
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 1, Tag = "auth" },
				new EventInput { PersistenceId = "actor-abc", SequenceNumber = 2, Tag = "profile" },
				new EventInput { PersistenceId = "actor-xyz", SequenceNumber = 1, Tag = "ping" },
			});
			using var events = db.CreateLocalTable<EventRecord>();
			using var lookup = db.CreateLocalTable(GetSourceData());
			using var audit  = db.CreateLocalTable<AuditTable>();

			var query = input
				.InsertWithOutputQuery(events, i => new EventRecord { PersistenceId = i.PersistenceId, SequenceNumber = i.SequenceNumber })
				.Select(e => new
				{
					e.Id,
					Children = lookup
						.Where(s => s.Id == e.Id)
						.Select(s => s.Id)
						.ToList(),
				});

			// the load transaction commits before the rows reach user code: a transaction started while
			// enumerating is the user's, and the end of the enumeration must not dispose it
			DataConnectionTransaction? transaction = null;

			switch (enumeration)
			{
				case AsyncEnumeration.AwaitForeach:
					await foreach (var row in query.AsAsyncEnumerable())
					{
						transaction ??= db.BeginTransaction();
						db.Insert(new AuditTable { TargetId = row.Id, Text = "audit" });
					}

					break;

				case AsyncEnumeration.ForEachAsync:
					await query.ForEachAsync(row =>
					{
						transaction ??= db.BeginTransaction();
						db.Insert(new AuditTable { TargetId = row.Id, Text = "audit" });
					});

					break;
			}

			transaction!.Commit();

			audit
				.Count()
				.ShouldBe(3);
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

			var dataContext = (IDataContext)db;
			var sb          = dataContext.CreateSqlBuilder();
			var table       = sb.ConvertInline("oas_probe", LinqToDB.Internal.SqlProvider.ConvertType.NameToQueryTable);
			var id          = sb.ConvertInline("id",        LinqToDB.Internal.SqlProvider.ConvertType.NameToQueryField);

			try
			{
				var ids = db
					.Query<int>($"WITH t AS (INSERT INTO {table} ({id}) VALUES (1) RETURNING {id}) SELECT {id} FROM t")
					.ToList();

				return ids.Count == 1 && ids[0] == 1 && t.Count() == 1;
			}
			catch
			{
				return false;
			}
		}
	}
}
