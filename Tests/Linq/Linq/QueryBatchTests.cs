using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryBatchTests : TestBase
	{
		// Access refuses a FROM-less SELECT holding a subquery ("Query input must contain at least one table or query"),
		// which a batch with a scalar read over a table builds; SelectScalarTests.SubQueryAggregate excludes it likewise.
		const string NoFromlessSubquery = TestProvName.AllAccess;

		[Table]
		sealed class BatchItem
		{
			[Column, PrimaryKey]  public int     Id      { get; set; }
			[Column]              public int?    Value   { get; set; }
			[Column]              public int     GroupId { get; set; }
			[Column(Length = 10)] public string? Name    { get; set; }

			public static readonly BatchItem[] Seed = Enumerable
				.Range(1, 10)
				.Select(i => new BatchItem
				{
					Id      = i,
					Value   = i % 3 == 0 ? null : i,
					GroupId = i % 4 + 1,
					Name    = "N" + i,
				})
				.ToArray();
		}

		[Table]
		sealed class BatchGroup
		{
			[Column, PrimaryKey] public int Id { get; set; }

			public static readonly BatchGroup[] Seed = Enumerable
				.Range(1, 4)
				.Select(i => new BatchGroup { Id = i })
				.ToArray();
		}

		sealed class CommandTrace
		{
			public int          Readers;
			public int          Begins;
			public bool         AnyAsync;
			public List<string> Texts = new();

			public void Reset()
			{
				Readers  = 0;
				Begins   = 0;
				AnyAsync = false;
				Texts.Clear();
			}
		}

		// Round trips are counted from the trace, not from a command interceptor: an interceptor disables DbBatch, so it
		// would hide the backend a test is meant to observe.
		static Func<DataOptions, DataOptions> Traced(CommandTrace trace, bool useCombinedCommands = false) => options => options
			.UseCombinedCommands(useCombinedCommands)
			.UseTracing(TraceLevel.Info, ti =>
			{
				if (ti.TraceInfoStep != TraceInfoStep.BeforeExecute)
					return;

				if (ti.Operation == TraceOperation.ExecuteReader)
				{
					trace.Readers++;
					trace.AnyAsync |= ti.IsAsync;
					trace.Texts.Add(ti.SqlText);
				}
				else if (ti.Operation == TraceOperation.BeginTransaction)
				{
					trace.Begins++;
				}
			});

		static int[] Ids(IEnumerable<BatchItem> items)
		{
			return items
				.Select(i => i.Id)
				.ToArray();
		}

		static int[] PageIds(int skip, int take)
		{
			var page = BatchItem.Seed
				.OrderBy(i => i.Id)
				.Skip(skip)
				.Take(take);

			return Ids(page);
		}

		static string[] GroupedIds(IEnumerable<(int Id, IEnumerable<BatchItem> Items)> groups)
		{
			return groups
				.Select(g => g.Id + ":" + string.Join(",", Ids(g.Items)))
				.ToArray();
		}

		static string[] ExpectedGroupedIds()
		{
			var groups = BatchGroup.Seed
				.Select(g => (g.Id, BatchItem.Seed.Where(i => i.GroupId == g.Id)));

			return GroupedIds(groups);
		}

		// Traced keeps this test out of the SQL baselines: a remote context runs the batch as separate commands, so its
		// baseline differs from the direct one by design and the harness would reject the pair.
		[Test]
		public void ReturnsTheValuesOfEachPart([DataSources(NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context, Traced(new CommandTrace()));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var (total, page) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Skip(2)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(2, 3));
		}

		[Test]
		public void SendsOneCommandWhateverTheOption([DataSources(false, NoFromlessSubquery)] string context, [Values] bool useCombinedCommands)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace, useCombinedCommands));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var (total, page) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Skip(2)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(2, 3));
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : 2);
		}

		// SQLite renders no Skip without a Take, so the reader skips the rows; the combined command's reader does too.
		[Test]
		public void SkipWithoutTake([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var (total, rest) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Skip(2)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(rest).ShouldBe(PageIds(2, BatchItem.Seed.Length));
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : 2);
		}

		[Test]
		public void SelectOfTheSameShapeIsUnchanged([DataSources(false, NoFromlessSubquery)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var result = db.Select(() => new
			{
				Total = items.Count(),
				Page  = items
					.OrderBy(i => i.Id)
					.Skip(2)
					.Take(3)
					.ToList(),
			});

			result.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(result.Page).ShouldBe(PageIds(2, 3));
			trace.Readers.ShouldBe(2);
		}

		// The flag lives on the cached query, so the check needs only one provider that combines; SQLite does.
		[Test]
		public void SelectAndQueryBatchDoNotShareACacheEntry([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			int Run(bool batch)
			{
				trace.Reset();

				if (batch)
				{
					db.QueryBatch(() => new
					{
						Total = items.Count(),
						Page  = items
							.OrderBy(i => i.Id)
							.Take(3)
							.ToList(),
					});
				}
				else
				{
					db.Select(() => new
					{
						Total = items.Count(),
						Page  = items
							.OrderBy(i => i.Id)
							.Take(3)
							.ToList(),
					});
				}

				return trace.Readers;
			}

			var select      = Run(batch: false);
			var queryBatch  = Run(batch: true);
			var selectAgain = Run(batch: false);

			select.ShouldBe(2);
			queryBatch.ShouldBe(1);
			selectAgain.ShouldBe(2);
		}

		// The combined gate requires a DataConnection whatever the provider; one concat and one DbBatch provider show it.
		[Test]
		public void DataContextRunsTheBatchSequentially([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var trace = new CommandTrace();

			using var dc = new DataContext(Traced(trace)(new DataOptions().UseConfiguration(context)));

			var table = dc
				.GetTable<BatchItem>()
				.TableName(items.TableName);

			trace.Reset();

			var (total, page) = dc.QueryBatch(
				() => table.Count(),
				() => table
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(2);
		}

		// A SQL comment is a hint every provider renders; SQLite and SQL Server cover the concat and the DbBatch provider.
		[Test]
		public void PendingHintsRunTheBatchSequentially([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			const string hint = "-- query batch hint";

			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();
			db.NextQueryHints.Add(hint);

			var (total, page) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(2);

			// On the sequential path the child harvester runs before the main query, so it takes the one-shot hint.
			trace.Texts[0].ShouldContain(hint);
			db.NextQueryHints.ShouldBeEmpty();
		}

		public enum Terminal
		{
			Any,
			All,
			Contains,
			Sum,
			Min,
			Max,
			Average,
			LongCount,
			FirstMember,
			FirstEntity,
			FirstOrDefaultEmpty,
			Single,
			SingleOrDefaultEmpty,
			TwoCollections,
			ScalarsOnly,
		}

		[Test]
		public void EveryTerminalReadsItsValue([DataSources(NoFromlessSubquery)] string context, [Values] Terminal terminal)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var seed        = BatchItem.Seed;
			var collections = 1;

			List<BatchItem>? rows = null;

			trace.Reset();

			switch (terminal)
			{
				case Terminal.Any:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.Any(i => i.Value > 5),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(seed.Any(i => i.Value > 5));
					rows = r.Rows;
					break;
				}

				case Terminal.All:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.All(i => i.Id > 0),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(seed.All(i => i.Id > 0));
					rows = r.Rows;
					break;
				}

				case Terminal.Contains:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items
							.Select(i => i.Id)
							.Contains(3),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBeTrue();
					rows = r.Rows;
					break;
				}

				case Terminal.Sum:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.Sum(i => i.Value),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(seed.Sum(i => i.Value));
					rows = r.Rows;
					break;
				}

				case Terminal.Min:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.Min(i => i.Value),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(seed.Min(i => i.Value));
					rows = r.Rows;
					break;
				}

				case Terminal.Max:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.Max(i => i.Value),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(seed.Max(i => i.Value));
					rows = r.Rows;
					break;
				}

				case Terminal.Average:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.Average(i => (double?)i.Value),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X!.Value.ShouldBe(seed.Average(i => (double?)i.Value)!.Value, 1e-9);
					rows = r.Rows;
					break;
				}

				case Terminal.LongCount:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.LongCount(),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(seed.LongLength);
					rows = r.Rows;
					break;
				}

				case Terminal.FirstMember:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items
							.OrderBy(i => i.Id)
							.First()
							.Id,
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBe(1);
					rows = r.Rows;
					break;
				}

				case Terminal.FirstEntity:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items
							.OrderBy(i => i.Id)
							.First(),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.Id.ShouldBe(1);
					rows = r.Rows;
					break;
				}

				case Terminal.FirstOrDefaultEmpty:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items
							.Where(i => i.Id > 100)
							.FirstOrDefault(),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBeNull();
					rows = r.Rows;
					break;
				}

				case Terminal.Single:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.Single(i => i.Id == 2),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.Id.ShouldBe(2);
					rows = r.Rows;
					break;
				}

				case Terminal.SingleOrDefaultEmpty:
				{
					var r = db.QueryBatch(() => new
					{
						X    = items.SingleOrDefault(i => i.Id == 200),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					r.X.ShouldBeNull();
					rows = r.Rows;
					break;
				}

				case Terminal.TwoCollections:
				{
					var r = db.QueryBatch(() => new
					{
						A = items
							.Where(i => i.Id < 3)
							.OrderBy(i => i.Id)
							.ToList(),
						B = items
							.Where(i => i.Id > 8)
							.OrderBy(i => i.Id)
							.ToArray(),
					});

					Ids(r.A).ShouldBe(Ids(seed.Where(i => i.Id < 3)));
					Ids(r.B).ShouldBe(Ids(seed.Where(i => i.Id > 8)));
					collections = 2;
					break;
				}

				case Terminal.ScalarsOnly:
				{
					var r = db.QueryBatch(() => new
					{
						A = items.Count(),
						B = items.Max(i => i.Id),
					});

					r.A.ShouldBe(seed.Length);
					r.B.ShouldBe(seed.Max(i => i.Id));
					collections = 0;
					break;
				}
			}

			if (rows != null)
				Ids(rows).ShouldBe(PageIds(0, 2));

			if (!context.IsRemote())
				trace.Readers.ShouldBe(collections == 0 || db.UsesCombinedQueryBatch() ? 1 : 1 + collections);
		}

		// The results the arity tests take a prefix of, so the sync and async overloads read the same expressions.
		sealed class TupleResults(ITable<BatchItem> items)
		{
			public readonly Expression<Func<int>>             Count     = () => items.Count();
			public readonly Expression<Func<string?>>         Name      = () => items
				.Where(i => i.Id == 3)
				.Select(i => i.Name)
				.First();
			public readonly Expression<Func<List<BatchItem>>> Rows      = () => items
				.OrderBy(i => i.Id)
				.Take(2)
				.ToList();
			public readonly Expression<Func<long>>            LongCount = () => items
				.Where(i => i.Id > 5)
				.LongCount();
			public readonly Expression<Func<bool>>            AnyNull   = () => items.Any(i => i.Value == null);
			public readonly Expression<Func<int?>>            Max       = () => items.Max(i => i.Value);
			public readonly Expression<Func<double?>>         Average   = () => items.Average(i => (double?)i.Value);
		}

		// Tuple positions are assembled before translation, so one provider shows them; distinct types make a swap fail.
		[Test]
		public void TupleArity2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var (count, rows) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(2)
					.ToList());

			count.ShouldBe(10);
			Ids(rows).ShouldBe(PageIds(0, 2));
		}

		[Test]
		public async Task TupleArity3([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool async)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows) = async
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows)
				: db.QueryBatch(r.Count, r.Name, r.Rows);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows).ShouldBe(PageIds(0, 2));
		}

		[Test]
		public async Task TupleArity4([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool async)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount) = async
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows).ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
		}

		[Test]
		public async Task TupleArity5([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool async)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount, anyNull) = async
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows).ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
			anyNull.ShouldBeTrue();
		}

		[Test]
		public async Task TupleArity6([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool async)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount, anyNull, max) = async
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows).ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
			anyNull.ShouldBeTrue();
			max.ShouldBe(10);
		}

		[Test]
		public async Task TupleArity7([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool async)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount, anyNull, max, average) = async
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max, r.Average)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max, r.Average);

			var expectedAverage = BatchItem.Seed.Average(i => (double?)i.Value)!.Value;

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows).ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
			anyNull.ShouldBeTrue();
			max.ShouldBe(10);
			average!.Value.ShouldBe(expectedAverage, 1e-9);
		}

		[Test]
		public async Task AsyncMatchesSync([DataSources(false, NoFromlessSubquery)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var (total, page) = await db.QueryBatchAsync(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Skip(2)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(2, 3));
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : 2);
			trace.AnyAsync.ShouldBeTrue();

			trace.Reset();

			var named = await db.QueryBatchAsync(() => new
			{
				Total = items.Count(),
				Page  = items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList(),
			});

			named.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(named.Page).ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : 2);
		}

		[Test]
		public void CollectionsAreExplicitLoads([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context, o => o.UseImplicitCollectionLoading(ImplicitCollectionLoading.Throw));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.QueryBatch(() => new
			{
				Total = items.Count(),
				Page  = items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList(),
			});

			batch.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(batch.Page).ShouldBe(PageIds(0, 3));

			Action select = () => db.Select(() => new
			{
				Total = items.Count(),
				Page  = items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList(),
			});

			select
				.ShouldThrow<LinqToDBException>()
				.Message
				.ShouldStartWith("Implicit eager loading is not allowed");
		}

		// Concat (SQLite) and DbBatch (SQL Server) both bind the new value into the one cached query.
		[Test, QueryCacheTest]
		public void ChangedValuesReuseTheCachedQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var take = 2;

			(int, List<BatchItem>) Run()
			{
				return db.QueryBatch(
					() => items.Count(),
					() => items
						.OrderBy(i => i.Id)
						.Take(take)
						.ToList());
			}

			var first  = Run();
			var misses = Query<(int, List<BatchItem>)>.CacheMissCount;

			take = 4;

			var second = Run();

			Query<(int, List<BatchItem>)>.CacheMissCount.ShouldBe(misses);
			Ids(first.Item2).ShouldBe(PageIds(0, 2));
			Ids(second.Item2).ShouldBe(PageIds(0, 4));
		}

		[Test]
		public void ReadsInOneTransaction([DataSources(false, NoFromlessSubquery)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			// The load transaction is skipped where the provider has none.
			trace.Begins.ShouldBe(((DataConnection)db).DataProvider.TransactionsSupported ? 1 : 0);
		}

		// Whether a TransactionScope suppresses the load transaction is decided before any provider is involved.
		[Test]
		public void NoTransactionInsideTransactionScope([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			using (new TransactionScope())
			{
				var (total, page) = db.QueryBatch(
					() => items.Count(),
					() => items
						.OrderBy(i => i.Id)
						.Take(3)
						.ToList());

				total.ShouldBe(BatchItem.Seed.Length);
				Ids(page).ShouldBe(PageIds(0, 3));
			}

			trace.Begins.ShouldBe(0);
		}

		[Test]
		public void NoTransactionInsideAnExplicitOne([DataSources(false, NoFromlessSubquery)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			using var transaction = ((DataConnection)db).BeginTransaction();

			trace.Reset();

			db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			trace.Begins.ShouldBe(0);
		}

		[Test]
		public void ScalarsOnlyIsOneStatementWithoutTransaction([DataSources(false, NoFromlessSubquery)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var (count, max) = db.QueryBatch(
				() => items.Count(),
				() => items.Max(i => i.Id));

			count.ShouldBe(BatchItem.Seed.Length);
			max.ShouldBe(BatchItem.Seed.Max(i => i.Id));
			trace.Readers.ShouldBe(1);
			trace.Begins.ShouldBe(0);
		}

		// Nesting is decided by the builder, so one provider shows it; the enclosing collection gives the gate a load to combine.
		// The table-free body is the one a client-evaluable call would turn into a parameter, so it shows that check.
		[Test]
		public void NestedQueryBatchDoesNotCombineTheEnclosingQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db     = GetDataContext(context, Traced(trace));
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			trace.Reset();

			var withSelect = groups
				.OrderBy(g => g.Id)
				.Select(g => new
				{
					g.Id,
					N     = db.Select(() => items.Count()),
					One   = db.Select(() => 1),
					Items = items
						.Where(i => i.GroupId == g.Id)
						.ToList(),
				})
				.ToList();

			var selectTexts = trace.Texts.ToArray();

			trace.Reset();

			var withBatch = groups
				.OrderBy(g => g.Id)
				.Select(g => new
				{
					g.Id,
					N     = db.QueryBatch(() => items.Count()),
					One   = db.QueryBatch(() => 1),
					Items = items
						.Where(i => i.GroupId == g.Id)
						.ToList(),
				})
				.ToList();

			var selectCounts = withSelect
				.Select(g => g.N + ":" + g.One)
				.ToArray();

			var batchCounts = withBatch
				.Select(g => g.N + ":" + g.One)
				.ToArray();

			batchCounts.ShouldBe(selectCounts);
			trace.Texts.ShouldBe(selectTexts);
			selectTexts.Length.ShouldBe(2);
		}

		// A nested batch is a scalar subquery of the enclosing row, in both forms; the binding is the builder's, so one
		// provider shows it.
		[Test]
		public void NestedQueryBatchReturnsItsValue([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db     = GetDataContext(context, Traced(trace));
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			trace.Reset();

			var result = groups
				.OrderBy(g => g.Id)
				.Select(g => new
				{
					g.Id,
					Single = db.QueryBatch(() => items.Count()),
					Tuple  = db.QueryBatch(
						() => items.Count(i => i.GroupId == g.Id),
						() => items.Max(i => i.Id)),
				})
				.ToList();

			var actual = result
				.Select(r => $"{r.Id}:{r.Single}:{r.Tuple.Item1}:{r.Tuple.Item2}")
				.ToArray();

			var expected = BatchGroup.Seed
				.OrderBy(g => g.Id)
				.Select(g => $"{g.Id}:{BatchItem.Seed.Length}:{BatchItem.Seed.Count(i => i.GroupId == g.Id)}:{BatchItem.Seed.Max(i => i.Id)}")
				.ToArray();

			actual.ShouldBe(expected);
			trace.Readers.ShouldBe(1);
		}

		// A compiled query runs its element through its own path; both forms are compiled once and run.
		[Test]
		public void CompiledQueryBatch([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var single = CompiledQuery.Compile((IDataContext db, int take) => db.QueryBatch(() => new
			{
				Total = db.GetTable<BatchItem>().Count(),
				Page  = db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList(),
			}));

			var tuple = CompiledQuery.Compile((IDataContext db, int take) => db.QueryBatch(
				() => db.GetTable<BatchItem>().Count(),
				() => db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList()));

			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var named      = single(db, 2);
			var positional = tuple(db, 3);

			named.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(named.Page).ShouldBe(PageIds(0, 2));
			positional.Item1.ShouldBe(BatchItem.Seed.Length);
			Ids(positional.Item2).ShouldBe(PageIds(0, 3));
		}

		// Where the statements cannot be combined, forcing Default only adds round trips: a configured CteUnion already reads
		// the nested collection in one statement. A DataContext never combines; the DataConnection arm is the control that
		// does, and the providers that cannot are covered by NestedCollectionCombinesWhateverTheDefaultStrategy.
		[Test]
		public void ConfiguredStrategyStaysWhereNothingCombines(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values] bool dataContext)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var trace   = new CommandTrace();
			var options = Traced(trace)(new DataOptions().UseConfiguration(context))
				.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.CteUnion);

			using IDataContext target = dataContext ? new DataContext(options) : new DataConnection(options);

			var itemTable  = target.GetTable<BatchItem>();
			var groupTable = target.GetTable<BatchGroup>();

			trace.Reset();

			var selected = target.Select(() => new
			{
				Total = groupTable.Count(),
				Page  = groupTable
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = itemTable
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.ToList(),
					})
					.ToList(),
			});

			var selectReaders = trace.Readers;

			trace.Reset();

			var (total, page) = target.QueryBatch(
				() => groupTable.Count(),
				() => groupTable
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = itemTable
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.ToList(),
					})
					.ToList());

			var batchReaders = trace.Readers;

			var selectedIds = GroupedIds(selected.Page.Select(g => (g.Id, (IEnumerable<BatchItem>)g.Items)));
			var batchIds    = GroupedIds(page.Select(g => (g.Id, (IEnumerable<BatchItem>)g.Items)));

			total.ShouldBe(selected.Total);
			batchIds.ShouldBe(selectedIds);
			batchIds.ShouldBe(ExpectedGroupedIds());
			batchReaders.ShouldBe(target.UsesCombinedQueryBatch() ? 1 : selectReaders);
		}

		// Where the batch combines, it loads with the Default strategy whatever the configured one, and KeyedQuery shows the
		// override in the command count. Elsewhere the configured strategy stays, so the batch reads what db.Select reads.
		[Test]
		public void NestedCollectionCombinesWhateverTheDefaultStrategy(
			[DataSources(false, NoFromlessSubquery)] string context,
			[Values(EagerLoadingStrategy.KeyedQuery, EagerLoadingStrategy.CteUnion)] EagerLoadingStrategy strategy)
		{
			var trace = new CommandTrace();

			using var db = GetDataContext(context, o => Traced(trace)(o)
				.UseDefaultEagerLoadingStrategy(strategy));

			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			trace.Reset();

			db.Select(() => new
			{
				Total = groups.Count(),
				Page  = groups
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = items
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.ToList(),
					})
					.ToList(),
			});

			var selectReaders = trace.Readers;

			trace.Reset();

			var (total, page) = db.QueryBatch(
				() => groups.Count(),
				() => groups
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = items
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.ToList(),
					})
					.ToList());

			var actual = GroupedIds(page.Select(g => (g.Id, (IEnumerable<BatchItem>)g.Items)));

			total.ShouldBe(BatchGroup.Seed.Length);
			actual.ShouldBe(ExpectedGroupedIds());
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : selectReaders);
		}

		// The marker yields to the Default strategy only where the batch combines.
		[Test]
		public void InnerLoadStrategyIsOverridden([DataSources(false, NoFromlessSubquery)] string context)
		{
			var trace = new CommandTrace();

			using var db     = GetDataContext(context, Traced(trace));
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			trace.Reset();

			db.Select(() => new
			{
				Total = groups.Count(),
				Page  = groups
					.WithKeyedLoadStrategy()
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = items
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.ToList(),
					})
					.ToList(),
			});

			var selectReaders = trace.Readers;

			trace.Reset();

			var (total, page) = db.QueryBatch(
				() => groups.Count(),
				() => groups
					.WithKeyedLoadStrategy()
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = items
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.ToList(),
					})
					.ToList());

			var actual = GroupedIds(page.Select(g => (g.Id, (IEnumerable<BatchItem>)g.Items)));

			total.ShouldBe(BatchGroup.Seed.Length);
			actual.ShouldBe(ExpectedGroupedIds());
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : selectReaders);
		}

		// Npgsql implements DbBatch on every TFM linq2db batches on; SQL Server's sequential-access contexts register an
		// interceptor, which turns DbBatch off, so only the PostgreSQL contexts assert the backend.
		[Test]
		public void DbBatchRunsWithTheOptionOff([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllPostgreSQL)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var (total, page) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(1);

#if !NETFRAMEWORK
			if (context.IsAnyOf(TestProvName.AllPostgreSQL))
				trace.Texts[0].ShouldContain("-- Batch ");
#endif
		}

		sealed class CountingInterceptor : CommandInterceptor
		{
			public int Count { get; private set; }

			public override DbCommand CommandInitialized(CommandEventData eventData, DbCommand command)
			{
				Count++;
				return command;
			}
		}

		// The providers that combine; an interceptor turns DbBatch off, so each sends one concatenated command.
		[Test]
		public void ConcatRunsWithAnInterceptor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySql)] string context)
		{
			var trace       = new CommandTrace();
			var interceptor = new CountingInterceptor();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			db.AddInterceptor(interceptor);
			trace.Reset();

			var (total, page) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page).ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(1);
			interceptor.Count.ShouldBe(1);
			trace.Texts[0].ShouldNotContain("-- Batch ");
		}
	}
}
