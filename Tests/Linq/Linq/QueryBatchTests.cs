using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Interceptors;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
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

		// A local seed gives the tests known page ids and a fixed NULL pattern.
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

		static InvalidOperationException ThrowOfThePartAlone<T>(Expression<Func<T>> part)
		{
			var alone = part.Compile();

			return Shouldly.Should.Throw<InvalidOperationException>(() => alone());
		}

		// Catches only the LINQ exception, so a provider's refusal to translate the batch reaches a declared Throws*.
		static InvalidOperationException ThrowOfTheBatch(Action batch)
		{
			try
			{
				batch();
			}
			catch (InvalidOperationException ex)
			{
				return ex;
			}

			throw new ShouldAssertException("The batch should throw System.InvalidOperationException but did not.");
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
			Ids(page)
				.ShouldBe(PageIds(2, 3));
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
			Ids(page)
				.ShouldBe(PageIds(2, 3));
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
			Ids(rest)
				.ShouldBe(PageIds(2, BatchItem.Seed.Length));
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
			Ids(result.Page)
				.ShouldBe(PageIds(2, 3));
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

		// A DataContext combines the batch on the connection it holds, as a DataConnection does; one concat and one DbBatch
		// provider show it.
		[Test]
		public void DataContextRunsTheBatchInOneCommand([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var trace = new CommandTrace();

			var options = new DataOptions()
				.UseConfiguration(context);

			using var dc = new DataContext(Traced(trace)(options));

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
			Ids(page)
				.ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(1);
		}

		// The refusal comes before any command, so SQLite and SQL Server (concat and DbBatch) stand for every provider.
		[Test]
		public async Task PendingHintsAreRefused(
			[IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context,
			[Values] bool oneShot,
			[Values] bool isAsync)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			await BatchRefusesHints(db, items, trace, oneShot, isAsync);
		}

		// A DataContext takes its hints itself and hands them to a connection only when a command runs.
		[Test]
		public async Task PendingHintsAreRefusedOnADataContext(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values] bool oneShot,
			[Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var trace = new CommandTrace();

			var options = new DataOptions()
				.UseConfiguration(context);

			using var dc = new DataContext(Traced(trace)(options));

			var table = dc
				.GetTable<BatchItem>()
				.TableName(items.TableName);

			await BatchRefusesHints(dc, table, trace, oneShot, isAsync);
		}

		static async Task BatchRefusesHints(IDataContext db, IQueryable<BatchItem> items, CommandTrace trace, bool oneShot, bool isAsync)
		{
			const string hint = "-- query batch hint";

			var hints = oneShot ? db.NextQueryHints : db.QueryHints;

			trace.Reset();
			hints.Add(hint);

			Expression<Func<int>>             total = () => items.Count();
			Expression<Func<List<BatchItem>>> page  = () => items
				.OrderBy(i => i.Id)
				.Take(3)
				.ToList();

			var error = isAsync
				? await Shouldly.Should.ThrowAsync<LinqToDBException>(() => db.QueryBatchAsync(total, page))
				: Shouldly.Should.Throw<LinqToDBException>(() => db.QueryBatch(total, page));

			error.Message.ShouldBe(ErrorHelper.Error_QueryHints_In_QueryBatch);
			trace.Readers.ShouldBe(0);
			trace.Begins.ShouldBe(0);
			hints.ShouldBe([hint]);

			hints.Clear();
		}

		public enum TaskPart
		{
			Returned,
			TupleItem,
			Member,
			AsObject,
			ValueTask,
			PerRow,
			Nested,
			Compiled,
		}

		static ValueTask<int> CountLater(IQueryable<BatchItem> source)
		{
			return new ValueTask<int>(source.Count());
		}

		// A part that creates a task starts it while the batch row is read: a query of its own, outside the batch, that
		// nothing observes. The refusal comes before any command, so SQLite and SQL Server stand for every provider.
		[Test]
		public async Task TaskPartsAreRefused(
			[IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context,
			[Values] TaskPart part,
			[Values] bool isAsync)
		{
			var compiled = CompiledQuery.Compile((IDataContext dc) => dc.QueryBatch<object>(() => dc
				.GetTable<BatchItem>()
				.ToListAsync(CancellationToken.None)));

			var compiledAsync = CompiledQuery.Compile((IDataContext dc, CancellationToken token) => dc.QueryBatchAsync(() => dc
				.GetTable<BatchItem>()
				.ToListAsync(CancellationToken.None), token));

			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var error = part switch
			{
				TaskPart.Returned  => await RefusalOf(db, () => items.ToListAsync(CancellationToken.None), isAsync),
				TaskPart.TupleItem => isAsync
					? await Shouldly.Should.ThrowAsync<LinqToDBException>(() => db.QueryBatchAsync(() => items.Count(), () => items.ToListAsync(CancellationToken.None)))
					: Shouldly.Should.Throw<LinqToDBException>(() => db.QueryBatch(() => items.Count(), () => items.ToListAsync(CancellationToken.None))),
				TaskPart.Member    => await RefusalOf(db, () => new { Total = items.Count(), Rows = items.ToListAsync(CancellationToken.None) }, isAsync),
				TaskPart.AsObject  => await RefusalOf<object>(db, () => items.ToListAsync(CancellationToken.None), isAsync),
				TaskPart.ValueTask => await RefusalOf(db, () => CountLater(items), isAsync),
				TaskPart.PerRow    => await RefusalOf(db, () => items
					.Select(i => Task.FromResult(i.Id))
					.ToList(), isAsync),
				TaskPart.Nested    => isAsync
					? await Shouldly.Should.ThrowAsync<LinqToDBException>(() => db.SelectAsync(() => new { Rows = db.QueryBatch<object>(() => items.ToListAsync(CancellationToken.None)) }))
					: Shouldly.Should.Throw<LinqToDBException>(() => db.Select(() => new { Rows = db.QueryBatch<object>(() => items.ToListAsync(CancellationToken.None)) })),
				TaskPart.Compiled  => isAsync
					? await Shouldly.Should.ThrowAsync<LinqToDBException>(() => compiledAsync(db, CancellationToken.None))
					: Shouldly.Should.Throw<LinqToDBException>(() => compiled(db)),
				_                  => throw new InvalidOperationException($"Unexpected part {part}."),
			};

			error.Message.ShouldBe(ErrorHelper.Error_QueryBatch_TaskPart);
			trace.Readers.ShouldBe(0);
			trace.Begins.ShouldBe(0);
		}

		static async Task<LinqToDBException> RefusalOf<T>(IDataContext db, Expression<Func<T>> selector, bool isAsync)
		{
			return isAsync
				? await Shouldly.Should.ThrowAsync<LinqToDBException>(() => db.QueryBatchAsync(selector))
				: Shouldly.Should.Throw<LinqToDBException>(() => db.QueryBatch(selector));
		}

		public enum HeldTask
		{
			CapturedVariable,
			StaticField,
			ArrayElement,
			Cast,
			CompiledArgument,
			Returned,
		}

		static class HeldTasks
		{
			public static readonly Task<int> Five = Task.FromResult(5);
		}

		// A task the selector only reads starts nothing, wherever it is held.
		[Test]
		public void HeldTaskIsRead([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] HeldTask held)
		{
			var compiled = CompiledQuery.Compile((IDataContext dc, Task<int> argument) => dc.QueryBatch(() => new
			{
				Total = dc
					.GetTable<BatchItem>()
					.Count(),
				Held  = argument.Result,
			}));

			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var    task  = Task.FromResult(5);
			var    tasks = new[] { task };
			object boxed = task;

			if (held == HeldTask.Returned)
			{
				db.QueryBatch<object>(() => task)
					.ShouldBeSameAs(task);
				return;
			}

			var result = held switch
			{
				HeldTask.CapturedVariable => db.QueryBatch(() => new { Total = items.Count(), Held = task.Result }),
				HeldTask.StaticField      => db.QueryBatch(() => new { Total = items.Count(), Held = HeldTasks.Five.Result }),
				HeldTask.ArrayElement     => db.QueryBatch(() => new { Total = items.Count(), Held = tasks[0].Result }),
				HeldTask.Cast             => db.QueryBatch(() => new { Total = items.Count(), Held = ((Task<int>)boxed).Result }),
				HeldTask.CompiledArgument => compiled(db, task),
				_                         => throw new InvalidOperationException($"Unexpected task {held}."),
			};

			result.Total.ShouldBe(BatchItem.Seed.Length);
			result.Held.ShouldBe(5);
		}

		// A batch sends its parts together, so a token cancelled before the call sends none of them: no command and no load
		// transaction. One concat and one DbBatch provider, with a collection (which opens the transaction) and without, and
		// the compiled path, which enters on its own.
		[Test]
		public async Task CancelledTokenSendsNothing(
			[IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context,
			[Values] bool withCollection,
			[Values] bool compiled)
		{
			var scalars = CompiledQuery.Compile((IDataContext db, CancellationToken token) => db.QueryBatchAsync(
				() => db
					.GetTable<BatchItem>()
					.Count(),
				token));

			var withList = CompiledQuery.Compile((IDataContext db, CancellationToken token) => db.QueryBatchAsync(() => new
			{
				Total = db
					.GetTable<BatchItem>()
					.Count(),
				Page  = db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList(),
			}, token));

			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var cancelled = new CancellationToken(true);

			trace.Reset();

			var error = (withCollection, compiled) switch
			{
				(false, false) => await Shouldly.Should.ThrowAsync<OperationCanceledException>(() => db.QueryBatchAsync(() => items.Count(), cancelled)),
				(true,  false) => await Shouldly.Should.ThrowAsync<OperationCanceledException>(() => db.QueryBatchAsync(
					() => items.Count(),
					() => items
						.OrderBy(i => i.Id)
						.Take(3)
						.ToList(),
					cancelled)),
				(false, true)  => await Shouldly.Should.ThrowAsync<OperationCanceledException>(() => scalars(db, cancelled)),
				(true,  true)  => await Shouldly.Should.ThrowAsync<OperationCanceledException>(() => withList(db, cancelled)),
			};

			error.CancellationToken.ShouldBe(cancelled);
			trace.Readers.ShouldBe(0);
			trace.Begins.ShouldBe(0);
		}

		// The up-front token check is the batch's own: a query outside a batch still leaves the token to the provider.
		[Test]
		public async Task CancelledTokenOutsideABatchReachesTheProvider([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			await Shouldly.Should.ThrowAsync<OperationCanceledException>(() => items.CountAsync(new CancellationToken(true)));

			trace.Readers.ShouldBe(1);
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
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition, AlsoWhenParameter = "terminal", AlsoWhenValue = Terminal.SingleOrDefaultEmpty)]
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
					var run = () => db.QueryBatch(() => new
					{
						X    = items
							.Where(i => i.Id > 100)
							.FirstOrDefault(),
						Rows = items
							.OrderBy(i => i.Id)
							.Take(2)
							.ToList(),
					});

					// The same refusal SelectScalarTests.SubQueryTest declares for a FirstOrDefault in db.Select.
					if (context.IsAnyOf(TestProvName.AllSybase))
					{
						run
							.ShouldThrow<LinqToDBException>()
							.Message
							.ShouldContain(ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid);
						return;
					}

					var r = run();

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

					Ids(r.A)
						.ShouldBe(Ids(seed.Where(i => i.Id < 3)));
					Ids(r.B)
						.ShouldBe(Ids(seed.Where(i => i.Id > 8)));
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
				Ids(rows)
					.ShouldBe(PageIds(0, 2));

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
			Ids(rows)
				.ShouldBe(PageIds(0, 2));
		}

		[Test]
		public async Task TupleArity3([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows) = isAsync
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows)
				: db.QueryBatch(r.Count, r.Name, r.Rows);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows)
				.ShouldBe(PageIds(0, 2));
		}

		[Test]
		public async Task TupleArity4([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount) = isAsync
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows)
				.ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
		}

		[Test]
		public async Task TupleArity5([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount, anyNull) = isAsync
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows)
				.ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
			anyNull.ShouldBeTrue();
		}

		[Test]
		public async Task TupleArity6([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount, anyNull, max) = isAsync
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max);

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows)
				.ShouldBe(PageIds(0, 2));
			longCount.ShouldBe(5L);
			anyNull.ShouldBeTrue();
			max.ShouldBe(10);
		}

		[Test]
		public async Task TupleArity7([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var r = new TupleResults(items);

			var (count, name, rows, longCount, anyNull, max, average) = isAsync
				? await db.QueryBatchAsync(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max, r.Average)
				: db.QueryBatch(r.Count, r.Name, r.Rows, r.LongCount, r.AnyNull, r.Max, r.Average);

			var expectedAverage = BatchItem.Seed.Average(i => (double?)i.Value)!.Value;

			count.ShouldBe(10);
			name.ShouldBe("N3");
			Ids(rows)
				.ShouldBe(PageIds(0, 2));
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
			Ids(page)
				.ShouldBe(PageIds(2, 3));
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
			Ids(named.Page)
				.ShouldBe(PageIds(0, 3));
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
			Ids(batch.Page)
				.ShouldBe(PageIds(0, 3));

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
			Ids(first.Item2)
				.ShouldBe(PageIds(0, 2));
			Ids(second.Item2)
				.ShouldBe(PageIds(0, 4));
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
				Ids(page)
					.ShouldBe(PageIds(0, 3));
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

		// An empty First empties the one row the batch reads, which reports as First on an empty source does; the element
		// path is provider-independent.
		[Test]
		public async Task EmptyFirstThrowsAsFirst([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var plain = Shouldly.Should.Throw<InvalidOperationException>(() => items
				.Where(i => i.Id > 100)
				.First());

			Expression<Func<BatchItem>>       first = () => items
				.Where(i => i.Id > 100)
				.First();
			Expression<Func<List<BatchItem>>> rows  = () => items.ToList();

			var batch = isAsync
				? await Shouldly.Should.ThrowAsync<InvalidOperationException>(() => db.QueryBatchAsync(first, rows))
				: Shouldly.Should.Throw<InvalidOperationException>(() => db.QueryBatch(first, rows));

			batch.Message.ShouldBe(plain.Message);
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
				Total = db
					.GetTable<BatchItem>()
					.Count(),
				Page  = db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList(),
			}));

			var tuple = CompiledQuery.Compile((IDataContext db, int take) => db.QueryBatch(
				() => db
					.GetTable<BatchItem>()
					.Count(),
				() => db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList()));

			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var named      = single(db, 2);
			var positional = tuple(db, 3);

			named.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(named.Page)
				.ShouldBe(PageIds(0, 2));
			positional.Item1.ShouldBe(BatchItem.Seed.Length);
			Ids(positional.Item2)
				.ShouldBe(PageIds(0, 3));
		}

		// CompiledQuery replaces the async call with the sync form; only the call into the compiled table stays async.
		[Test]
		public async Task CompiledQueryBatchAsync([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var single = CompiledQuery.Compile((IDataContext db, int take, CancellationToken token) => db.QueryBatchAsync(() => new
			{
				Total = db
					.GetTable<BatchItem>()
					.Count(),
				Page  = db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList(),
			}, token));

			var tuple = CompiledQuery.Compile((IDataContext db, int take, CancellationToken token) => db.QueryBatchAsync(
				() => db
					.GetTable<BatchItem>()
					.Count(),
				() => db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList(),
				token));

			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var named      = await single(db, 2, default);
			var positional = await tuple(db, 3, default);

			named.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(named.Page)
				.ShouldBe(PageIds(0, 2));
			positional.Item1.ShouldBe(BatchItem.Seed.Length);
			Ids(positional.Item2)
				.ShouldBe(PageIds(0, 3));
		}

		sealed class ProcessingConnection(DataOptions options) : DataConnection(options)
		{
			protected override SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
			{
				return statement;
			}
		}

		sealed class ProcessingContext(DataOptions options) : DataContext(options)
		{
			protected override DataConnection CreateDataConnection(DataOptions options)
			{
				return new ProcessingConnection(options);
			}
		}

		// Where the statements cannot be combined, forcing Default only adds round trips: a configured CteUnion already reads
		// the nested collection in one statement. A ProcessQuery override keeps a connection from combining, on its own and
		// under a DataContext; the providers that cannot combine are covered by NestedCollectionCombinesWhateverTheDefaultStrategy.
		[Test]
		public void ConfiguredStrategyStaysWhereNothingCombines(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values] bool dataContext)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var trace = new CommandTrace();

			var configuration = new DataOptions()
				.UseConfiguration(context);

			var options = Traced(trace)(configuration)
				.UseDefaultEagerLoadingStrategy(EagerLoadingStrategy.CteUnion);

			using IDataContext target = dataContext ? new ProcessingContext(options) : new ProcessingConnection(options);

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
			batchReaders.ShouldBe(selectReaders);
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
			Ids(page)
				.ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(1);

#if !NETFRAMEWORK
			if (context.IsAnyOf(TestProvName.AllPostgreSQL))
				trace.Texts[0].ShouldContain("-- Command ");
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

		public enum MySqlDataRoute
		{
			ProviderName,
			ProviderInstance,
			UseMySql,
		}

		// A connection string can refuse several statements in one command, and connections share the provider instance,
		// so MySql.Data runs them one after another on every route, whichever context runs them and whatever the string says.
		[Test]
		public void MySqlDataRunsStatementsOneAfterAnother(
			[IncludeDataSources(TestProvName.AllMySql80)] string context,
			[Values("", ";AllowBatch=true", ";AllowBatch=false")] string option,
			[Values] MySqlDataRoute route,
			[Values] bool dataContext)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var trace            = new CommandTrace();
			var connectionString = GetConnectionString(context) + option;
			var empty            = new DataOptions();

			var configuration = route switch
			{
				MySqlDataRoute.ProviderName     => empty.UseConnectionString(ProviderName.MySql80MySqlData, connectionString),
				MySqlDataRoute.ProviderInstance => empty.UseConnectionString(MySqlTools.GetDataProvider(MySqlVersion.MySql80, MySqlProvider.MySqlData), connectionString),
				_                               => empty.UseMySql(connectionString, MySqlVersion.MySql80, MySqlProvider.MySqlData),
			};

			var options = Traced(trace)(configuration);

			using IDataContext target = dataContext ? new DataContext(options) : new DataConnection(options);

			var table = target
				.GetTable<BatchItem>()
				.TableName(items.TableName);

			trace.Reset();

			var (total, page) = target.QueryBatch(
				() => table.Count(),
				() => table
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page)
				.ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(2);
		}

		// The option on a query of its own takes the same way out.
		[Test]
		public void MySqlDataRunsCombinedCommandsOneAfterAnother(
			[IncludeDataSources(TestProvName.AllMySql80)] string context,
			[Values("", ";AllowBatch=false")] string option)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var trace = new CommandTrace();

			var configuration = new DataOptions()
				.UseMySql(GetConnectionString(context) + option, MySqlVersion.MySql80, MySqlProvider.MySqlData);

			using var target = new DataConnection(Traced(trace, useCombinedCommands: true)(configuration));

			var itemTable = target
				.GetTable<BatchItem>()
				.TableName(items.TableName);

			var groupTable = target
				.GetTable<BatchGroup>()
				.TableName(groups.TableName);

			trace.Reset();

			var rows = groupTable
				.OrderBy(g => g.Id)
				.Select(g => new
				{
					g.Id,
					Items = itemTable
						.Where(i => i.GroupId == g.Id)
						.ToList(),
				})
				.ToList();

			rows.Count.ShouldBe(BatchGroup.Seed.Length);
			trace.Readers.ShouldBe(2);
		}

		[Test]
		public void OnlyMySqlConnectorCombinesStatements(
			[Values(MySqlVersion.MySql57, MySqlVersion.MySql80, MySqlVersion.MariaDB10)] MySqlVersion version,
			[Values(MySqlProvider.MySqlData, MySqlProvider.MySqlConnector)] MySqlProvider provider)
		{
			MySqlTools.GetDataProvider(version, provider)
				.SqlProviderFlags
				.IsMultiStatementBatchSupported
				.ShouldBe(provider == MySqlProvider.MySqlConnector);
		}

		// A remote context runs each statement on a call of its own, on providers that combine on a direct connection.
		[Test]
		[UsesRemoteContext]
		public void RemoteContextRunsStatementsOneAfterAnother(
			[IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllMySqlConnector)] string context)
		{
			var readers = new List<string>();

			using var db = GetServerContainer()
				.CreateContext(
					(_, o) => o.UseConfiguration(context),
					(configuration, _) => new DataConnection(new DataOptions()
						.UseConfiguration(configuration)
						.UseTracing(TraceLevel.Info, info =>
						{
							if (info.TraceInfoStep == TraceInfoStep.BeforeExecute && info.Operation == TraceOperation.ExecuteReader)
								readers.Add(info.SqlText);
						})));

			using var items = db.CreateLocalTable(BatchItem.Seed);

			readers.Clear();

			var (total, page) = db.QueryBatch(
				() => items.Count(),
				() => items
					.OrderBy(i => i.Id)
					.Take(3)
					.ToList());

			total.ShouldBe(BatchItem.Seed.Length);
			Ids(page)
				.ShouldBe(PageIds(0, 3));
			readers.Count.ShouldBe(2);
		}

		// The providers that combine; an interceptor turns DbBatch off, so each sends one concatenated command.
		[Test]
		public void ConcatRunsWithAnInterceptor([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL, TestProvName.AllMySqlConnector)] string context)
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
			Ids(page)
				.ShouldBe(PageIds(0, 3));
			trace.Readers.ShouldBe(1);
			interceptor.Count.ShouldBe(1);
			trace.Texts[0].ShouldNotContain("-- Command ");
		}

		public enum EmptyRead
		{
			FirstValue,
			FirstMember,
			SingleValue,
		}

		// A scalar read is a subquery of the batch row, which SQL reads as NULL when it finds nothing.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid, AlsoWhenParameter = "read", AlsoWhenValue = EmptyRead.FirstValue)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition, AlsoWhenParameter = "read", AlsoWhenValue = EmptyRead.SingleValue)]
		public void EmptyScalarReadThrowsAsThePartAlone(
			[DataSources(false, NoFromlessSubquery)] string context,
			[Values] EmptyRead read)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>> part = read switch
			{
				EmptyRead.FirstValue  => () => items
					.Where(i => i.Id > 100)
					.Select(i => i.Id)
					.First(),
				EmptyRead.FirstMember => () => items
					.Where(i => i.Id > 100)
					.OrderBy(i => i.Id)
					.First()
					.Id,
				_                     => () => items
					.Where(i => i.Id > 100)
					.Select(i => i.Id)
					.Single(),
			};

			var alone = ThrowOfThePartAlone(part);
			var batch = ThrowOfTheBatch(() => db.QueryBatch(part, () => items.ToList()));

			batch.Message.ShouldBe(alone.Message);
		}

		// Where the batch does not combine, the configured strategy builds it, and both rebuild the reader of the batch row.
		[Test]
		public void EmptyScalarReadThrowsUnderTheConfiguredStrategy(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values(EagerLoadingStrategy.CteUnion, EagerLoadingStrategy.KeyedQuery)] EagerLoadingStrategy strategy)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var options = new DataOptions()
				.UseConfiguration(context)
				.UseDefaultEagerLoadingStrategy(strategy);

			using var target = new ProcessingConnection(options);

			var itemTable  = target.GetTable<BatchItem>();
			var groupTable = target.GetTable<BatchGroup>();

			Expression<Func<int>> first = () => itemTable
				.Where(i => i.Id > 100)
				.Select(i => i.Id)
				.First();

			var alone = ThrowOfThePartAlone(first);
			var batch = Shouldly.Should.Throw<InvalidOperationException>(() => target.QueryBatch(
				first,
				() => groupTable
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = itemTable
							.Where(i => i.GroupId == g.Id)
							.ToList(),
					})
					.ToList()));

			batch.Message.ShouldBe(alone.Message);
		}

		// A NULL the value itself can hold does not mean an empty source, so the part reads what it reads alone.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void NullValueReadsAsThePartAlone([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>> part = () => items
				.Where(i => i.Id == 3)
				.Select(i => i.Value!.Value)
				.First();

			var alone = part
				.Compile()
				.Invoke();

			var (value, _) = db.QueryBatch(part, () => items.ToList());

			value.ShouldBe(alone);
		}

		// A window value is NULL at a partition edge whatever its arguments, so its NULL is not an empty source either. Not
		// every provider has LAG, and those below read it through the same marker as any computed value. SQL Server has it
		// from 2012.
		[Test]
		public void WindowValueReadsAsThePartAlone(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2012Plus, TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>> part = () => items
				.OrderBy(i => i.Id)
				.Select(i => Sql.Ext.Lag(i.Id, Sql.Nulls.None).Over().OrderBy(i.Id).ToValue())
				.First();

			var alone = part
				.Compile()
				.Invoke();

			var (value, _) = db.QueryBatch(part, () => items.ToList());

			value.ShouldBe(alone);
		}

		// SQLite reads a division by zero as NULL, where the others raise it.
		[Test]
		public void ComputedNullReadsAsThePartAlone([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>> part = () => items
				.Where(i => i.Id == 1)
				.Select(i => i.Id / (i.Id - 1))
				.First();

			var alone = part
				.Compile()
				.Invoke();

			var (value, _) = db.QueryBatch(part, () => items.ToList());

			value.ShouldBe(alone);
		}

		sealed class NullToMinusSevenAttribute : ValueConverterAttribute
		{
			public NullToMinusSevenAttribute()
			{
				ValueConverter = new ValueConverter<int, int?>(v => v, v => v ?? -7, handlesNulls: true);
			}
		}

		[Table]
		sealed class ConvertedItem
		{
			[Column, PrimaryKey]                         public int Id    { get; set; }
			[Column(CanBeNull = true), NullToMinusSeven] public int Value { get; set; }
		}

		// A converter that handles nulls turns the NULL of an empty source into a value, so only the marker shows the empty
		// source.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void ConverterDoesNotHideAnEmptySource([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db   = GetDataContext(context);
			using var rows = db.CreateLocalTable(new[] { new ConvertedItem { Id = 1, Value = 1 } });

			Expression<Func<int>> part = () => rows
				.Where(x => x.Value > 100)
				.Select(x => x.Value)
				.First();

			var alone = ThrowOfThePartAlone(part);
			var batch = ThrowOfTheBatch(() => db.QueryBatch(part, () => rows.ToList()));

			batch.Message.ShouldBe(alone.Message);
		}

		static int ClientValue(int id) => id + 1;

		static int ClientSum(int first, int second) => first + second;

		public enum ComputedRead
		{
			ClientColumn,
			Constant,
			ClientConstant,
			Conditional,
		}

		static Expression<Func<int>> ComputedPart(IQueryable<BatchItem> source, ComputedRead read, bool isSingle)
		{
			return (read, isSingle) switch
			{
				(ComputedRead.ClientColumn,   false) => () => source
					.Select(i => ClientValue(i.Id))
					.First(),
				(ComputedRead.ClientColumn,   true ) => () => source
					.Select(i => ClientValue(i.Id))
					.Single(),
				(ComputedRead.Constant,       false) => () => source
					.Select(i => 2)
					.First(),
				(ComputedRead.Constant,       true ) => () => source
					.Select(i => 2)
					.Single(),
				(ComputedRead.ClientConstant, false) => () => source
					.Select(i => ClientSum(1, 1))
					.First(),
				(ComputedRead.ClientConstant, true ) => () => source
					.Select(i => ClientSum(1, 1))
					.Single(),
				(ComputedRead.Conditional,    false) => () => source
					.Select(i => i.Id > 0 ? ClientValue(i.Id) : ClientValue(i.GroupId))
					.First(),
				_                                    => () => source
					.Select(i => i.Id > 0 ? ClientValue(i.Id) : ClientValue(i.GroupId))
					.Single(),
			};
		}

		// A value that is no column of the row shows no more of the row than a NULL does.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid, AlsoWhenParameter = "isSingle", AlsoWhenValue = false)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition, AlsoWhenParameter = "isSingle", AlsoWhenValue = true)]
		public void ComputedValueOverAnEmptySourceThrowsAsThePartAlone(
			[DataSources(false, NoFromlessSubquery)] string context,
			[Values] ComputedRead read,
			[Values] bool isSingle)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var part = ComputedPart(items.Where(i => i.Id > 100), read, isSingle);

			var alone = ThrowOfThePartAlone(part);
			var batch = ThrowOfTheBatch(() => db.QueryBatch(part, () => items.ToList()));

			batch.Message.ShouldBe(alone.Message);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid, AlsoWhenParameter = "isSingle", AlsoWhenValue = false)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition, AlsoWhenParameter = "isSingle", AlsoWhenValue = true)]
		public void ComputedValueOfARowReadsAsThePartAlone(
			[DataSources(false, NoFromlessSubquery)] string context,
			[Values] ComputedRead read,
			[Values] bool isSingle)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var part = ComputedPart(items.Where(i => i.Id == 1), read, isSingle);

			var (value, _) = db.QueryBatch(part, () => items.ToList());

			value.ShouldBe(part.Compile()());
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid, AlsoWhenParameter = "isSingle", AlsoWhenValue = false)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition, AlsoWhenParameter = "isSingle", AlsoWhenValue = true)]
		public void NullableReadOverAnEmptySourceThrowsAsThePartAlone(
			[DataSources(false, NoFromlessSubquery)] string context,
			[Values] bool isSingle)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			if (isSingle)
			{
				Expression<Func<string?>> name = () => items
					.Where(i => i.Id > 100)
					.Select(i => i.Name)
					.Single();

				var aloneName = ThrowOfThePartAlone(name);
				var batchName = ThrowOfTheBatch(() => db.QueryBatch(name, () => items.ToList()));

				batchName.Message.ShouldBe(aloneName.Message);
			}
			else
			{
				Expression<Func<int?>> value = () => items
					.Where(i => i.Id > 100)
					.Select(i => i.Value)
					.First();

				var aloneValue = ThrowOfThePartAlone(value);
				var batchValue = ThrowOfTheBatch(() => db.QueryBatch(value, () => items.ToList()));

				batchValue.Message.ShouldBe(aloneValue.Message);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void NullInARowReadsNull([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int?>> part = () => items
				.Where(i => i.Id == 3)
				.Select(i => i.Value)
				.First();

			var (value, _) = db.QueryBatch(part, () => items.ToList());

			value.ShouldBeNull();
		}

		// The marker joins the rows to the batch row, which is read with Single.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition)]
		public void TwoRowNullableSingleThrowsAsThePartAlone([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int?>> part = () => items
				.Select(i => i.Value)
				.Single();

			ThrowOfThePartAlone(part);

			ThrowOfTheBatch(() => db.QueryBatch(part, () => items.ToList()));
		}

		// A scalar subquery would leave the second row to the server: an error of its own, or on SQLite the first row.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition)]
		public void TwoRowNotNullSingleThrowsAsThePartAlone([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>> part = () => items
				.Select(i => i.Id)
				.Single();

			ThrowOfThePartAlone(part);

			ThrowOfTheBatch(() => db.QueryBatch(part, () => items.ToList()));
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ActiveIssue(6289, Configuration = TestProvName.AllYdb, ErrorTypeName = "Ydb.Sdk.Ado.YdbException", ErrorMessage = "YdbDataReader was closed during transaction execution")]
		public void NestedNullableReadKeepsEveryRow([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var (rows, _) = db.QueryBatch(
				() => groups
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						First = items
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.Select(i => i.Value)
							.First(),
					})
					.ToList(),
				() => items.Count());

			var values = rows
				.Select(r => r.First)
				.ToArray();

			values.ShouldBe(new int?[] { 4, 1, 2, null });

			ThrowOfTheBatch(() => db.QueryBatch(
				() => groups
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						First = items
							.Where(i => i.GroupId == g.Id && i.Id > 8)
							.OrderBy(i => i.Id)
							.Select(i => i.Value)
							.First(),
					})
					.ToList(),
				() => items.Count()));
		}

		// Only a read that cannot show its row pays for a marker: a First of a NOT NULL column stays a plain scalar subquery. The
		// three providers read a marker by a join, by APPLY and by LATERAL.
		[Test]
		public void NotNullColumnStaysAScalarSubquery(
			[IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllPostgreSQL)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var (first, count) = db.QueryBatch(
				() => items
					.Where(i => i.Id > 1)
					.OrderBy(i => i.Id)
					.Select(i => i.Id)
					.First(),
				() => items.Count());

			first.ShouldBe(2);
			count.ShouldBe(10);

			trace.Texts
				.ShouldHaveSingleItem()
				.ShouldNotContain("JOIN");
		}

		// The check stays out of set-operation branches.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void SetOperationBranchReadsItsValue([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var (rows, _) = db.QueryBatch(
				() => groups
					.Where(g => g.Id == 1)
					.Select(g => new
					{
						g.Id,
						V = items
							.Where(i => i.GroupId == g.Id)
							.OrderBy(i => i.Id)
							.Select(i => i.Value)
							.First(),
					})
					.Concat(groups
						.Where(g => g.Id == 2)
						.Select(g => new
						{
							g.Id,
							V = items
								.Where(i => i.GroupId == g.Id)
								.OrderBy(i => i.Id)
								.Select(i => i.Value)
								.First(),
						}))
					.ToList(),
				() => items.Count());

			var values = rows
				.OrderBy(r => r.Id)
				.Select(r => r.V)
				.ToArray();

			values.ShouldBe(new int?[] { 4, 1 });
		}

		// Where the batch does not combine, KeyedQuery rebuilds the reader of the batch row, the marker included.
		[Test]
		public void MarkerSurvivesKeyedQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			NullableReadUnderTheConfiguredStrategy(context, EagerLoadingStrategy.KeyedQuery);
		}

		// CteUnion rebuilds the parent row from carrier slots, which hold no slot for the marker.
		[ActiveIssue(5823)]
		[Test]
		public void MarkerSurvivesCteUnion([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			NullableReadUnderTheConfiguredStrategy(context, EagerLoadingStrategy.CteUnion);
		}

		void NullableReadUnderTheConfiguredStrategy(string context, EagerLoadingStrategy strategy)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var options = new DataOptions()
				.UseConfiguration(context)
				.UseDefaultEagerLoadingStrategy(strategy);

			using var target = new ProcessingConnection(options);

			var itemTable  = target.GetTable<BatchItem>();
			var groupTable = target.GetTable<BatchGroup>();

			Expression<Func<int?>> empty = () => itemTable
				.Where(i => i.Id > 100)
				.Select(i => i.Value)
				.First();

			Expression<Func<int?>> present = () => itemTable
				.Where(i => i.Id == 3)
				.Select(i => i.Value)
				.First();

			Shouldly.Should.Throw<InvalidOperationException>(() => target.QueryBatch(
				empty,
				() => groupTable
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = itemTable
							.Where(i => i.GroupId == g.Id)
							.ToList(),
					})
					.ToList()));

			var (value, rows) = target.QueryBatch(
				present,
				() => groupTable
					.OrderBy(g => g.Id)
					.Select(g => new
					{
						g.Id,
						Items = itemTable
							.Where(i => i.GroupId == g.Id)
							.ToList(),
					})
					.ToList());

			value.ShouldBeNull();
			rows.Count.ShouldBe(4);
		}

		// A compiled batch loads its collections before it reads the row, so the refusal must come first there too.
		[Test]
		public async Task CompiledQueryBatchRefusesPendingHints(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values] bool oneShot,
			[Values] bool isAsync)
		{
			const string hint = "-- query batch hint";

			var batch = CompiledQuery.Compile((IDataContext db) => db.QueryBatch(
				() => db
					.GetTable<BatchItem>()
					.Count(),
				() => db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.ToList()));

			var batchAsync = CompiledQuery.Compile((IDataContext db) => db.QueryBatchAsync(
				() => db
					.GetTable<BatchItem>()
					.Count(),
				() => db.GetTable<BatchItem>()
					.OrderBy(i => i.Id)
					.ToList(),
				default));

			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var hints = oneShot ? db.NextQueryHints : db.QueryHints;

			trace.Reset();
			hints.Add(hint);

			var error = isAsync
				? await Shouldly.Should.ThrowAsync<LinqToDBException>(() => batchAsync(db))
				: Shouldly.Should.Throw<LinqToDBException>(() => batch(db));

			error.Message.ShouldBe(ErrorHelper.Error_QueryHints_In_QueryBatch);

			trace.Readers.ShouldBe(0);
			trace.Begins.ShouldBe(0);
			hints.ShouldBe([hint]);

			hints.Clear();
		}

		static Expression<Func<List<int>>> NestedFirsts(IQueryable<BatchGroup> groups, IQueryable<BatchItem> items)
		{
			return () => groups
				.OrderBy(g => g.Id)
				.Select(g => items
					.Where(i => i.GroupId == g.Id && i.Id > 8)
					.Select(i => i.Id)
					.First())
				.ToList();
		}

		// The same query outside a batch does not throw: it reads the empty source as the default value, or drops the row.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void NestedEmptyReadOutsideABatchDoesNotThrow([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var outside = NestedFirsts(groups, items)
				.Compile()
				.Invoke();

			// Where the First is no scalar subquery it is an inner join - CROSS APPLY on SQL CE, a ROW_NUMBER join or
			// INNER JOIN LATERAL elsewhere - which drops the row.
			int[] expected = context.IsAnyOf(
				ProviderName.SqlCe,
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllOracle11,
				TestProvName.AllSapHana,
				TestProvName.AllYdb)
				? [9, 10]
				: [0, 9, 10, 0];

			outside.ShouldBe(expected);
		}

		// A batch is new API, so a First nested in one of its parts follows LINQ too.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		[ActiveIssue(6289, Configuration = TestProvName.AllYdb, ErrorTypeName = "Ydb.Sdk.Ado.YdbException", ErrorMessage = "YdbDataReader was closed during transaction execution")]
		public void NestedEmptyReadThrowsInsideTheBatch([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db     = GetDataContext(context);
			using var items  = db.CreateLocalTable(BatchItem.Seed);
			using var groups = db.CreateLocalTable(BatchGroup.Seed);

			var firsts = NestedFirsts(groups, items);

			var empty = Shouldly.Should.Throw<InvalidOperationException>(() => Array.Empty<int>().First());
			var batch = ThrowOfTheBatch(() => db.QueryBatch(() => items.Count(), firsts));

			batch.Message.ShouldBe(empty.Message);
		}

		// An entity Single reads its rows into the batch row, so two rows make two batch rows.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllYdb, ErrorMessage = ErrorHelper.Error_Join_Without_Condition, AlsoWhenParameter = "orDefault", AlsoWhenValue = true)]
		public void TwoRowSingleThrowsAsThePartAlone(
			[DataSources(false, NoFromlessSubquery)] string context,
			[Values] bool orDefault)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<BatchItem?>> part = orDefault
				? () => items
					.Where(i => i.Id < 3)
					.SingleOrDefault()
				: () => items
					.Where(i => i.Id < 3)
					.Single();

			var alone = ThrowOfThePartAlone(part);
			var batch = ThrowOfTheBatch(() => db.QueryBatch(part, () => items.ToList()));

			batch.Message.ShouldBe(alone.Message);
		}

		static async Task Run(QueryBatchBuilder batch, bool isAsync)
		{
			if (isAsync)
				await batch.ExecuteAsync();
			else
				batch.Execute();
		}

		// Fifteen parts, so the tuple nests twice; collections and entities sit past the seventh part. No part is a strict First
		// of a value, which Sybase refuses (StrictValueReadsReadAsThePartAlone). Traced keeps the test out of the baselines.
		[Test]
		public async Task ExplicitBatchReadsEachPart([DataSources(NoFromlessSubquery)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context, Traced(new CommandTrace()));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();

			var count      = batch.Add(() => items.Count());
			var emptyMax   = batch.Add(() => items
				.Where(i => i.Id > 100)
				.Max(i => (int?)i.Id));
			var min        = batch.Add(() => items.Min(i => i.Id));
			var any        = batch.Add(() => items.Any(i => i.Id == 7));
			var constant   = batch.Add(() => 42);
			var sum        = batch.Add(() => items.Sum(i => i.Id));
			var longCount  = batch.Add(() => items.LongCount(i => i.GroupId == 2));
			var page       = batch.Add(() => items
				.OrderBy(i => i.Id)
				.Take(3)
				.ToList());
			var entity     = batch.Add(() => items.First(i => i.Id == 2));
			var array      = batch.Add(() => items
				.Where(i => i.GroupId == 1)
				.OrderBy(i => i.Id)
				.ToArray());
			var nullCount  = batch.Add(() => items.Count(i => i.Value == null));
			var maxGroup   = batch.Add(() => items.Max(i => i.GroupId));
			var values     = batch.Add(() => items
				.Where(i => i.GroupId == 3)
				.OrderBy(i => i.Id)
				.Select(i => i.Id)
				.ToList());
			var entity2    = batch.Add(() => items.First(i => i.Id == 9));
			var last       = batch.Add(() => items
				.Where(i => i.GroupId == 4)
				.OrderBy(i => i.Id)
				.ToList());

			await Run(batch, isAsync);

			var seed = BatchItem.Seed;

			count.Value.ShouldBe(seed.Length);
			emptyMax.Value.ShouldBeNull();
			min.Value.ShouldBe(seed.Min(i => i.Id));
			any.Value.ShouldBeTrue();
			constant.Value.ShouldBe(42);
			sum.Value.ShouldBe(seed.Sum(i => i.Id));
			longCount.Value.ShouldBe(seed.LongCount(i => i.GroupId == 2));
			Ids(page.Value)
				.ShouldBe(PageIds(0, 3));
			entity.Value.Id.ShouldBe(2);
			Ids(array.Value)
				.ShouldBe(Ids(seed.Where(i => i.GroupId == 1)));
			nullCount.Value.ShouldBe(seed.Count(i => i.Value == null));
			maxGroup.Value.ShouldBe(seed.Max(i => i.GroupId));
			values.Value.ShouldBe(Ids(seed.Where(i => i.GroupId == 3)));
			entity2.Value.Id.ShouldBe(9);
			Ids(last.Value)
				.ShouldBe(Ids(seed.Where(i => i.GroupId == 4)));
		}

		// The eighth part is the only value of the nested tuple; one part is a tuple of one. Both are decided before any SQL.
		[Test]
		public void ExplicitBatchOfEightPartsAndOfOne([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();

			var count     = batch.Add(() => items.Count());
			var min       = batch.Add(() => items.Min(i => i.Id));
			var max       = batch.Add(() => items.Max(i => i.Id));
			var sum       = batch.Add(() => items.Sum(i => i.Id));
			var longCount = batch.Add(() => items.LongCount(i => i.GroupId == 2));
			var any       = batch.Add(() => items.Any(i => i.Id == 7));
			var nullCount = batch.Add(() => items.Count(i => i.Value == null));
			var eighth    = batch.Add(() => items
				.OrderBy(i => i.Id)
				.Take(2)
				.ToList());

			batch.Execute();

			var seed = BatchItem.Seed;

			count.Value.ShouldBe(seed.Length);
			min.Value.ShouldBe(1);
			max.Value.ShouldBe(seed.Length);
			sum.Value.ShouldBe(seed.Sum(i => i.Id));
			longCount.Value.ShouldBe(seed.LongCount(i => i.GroupId == 2));
			any.Value.ShouldBeTrue();
			nullCount.Value.ShouldBe(seed.Count(i => i.Value == null));
			Ids(eighth.Value)
				.ShouldBe(PageIds(0, 2));

			var single = db.CreateQueryBatch();
			var rows   = single.Add(() => items
				.OrderBy(i => i.Id)
				.Skip(1)
				.Take(2)
				.ToList());

			single.Execute();

			Ids(rows.Value)
				.ShouldBe(PageIds(1, 2));
		}

		// A remote context runs the parts one by one on the server, where the client counts no readers; Access refuses the
		// FROM-less subquery.
		[Test]
		public void ExplicitBatchSendsOneCommand([DataSources(false, NoFromlessSubquery)] string context, [Values] bool useCombinedCommands)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace, useCombinedCommands));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>>             totalPart = () => items.Count();
			Expression<Func<List<BatchItem>>> pagePart  = () => items
				.OrderBy(i => i.Id)
				.Skip(2)
				.Take(3)
				.ToList();

			trace.Reset();
			db.QueryBatch(totalPart, pagePart);

			var queryBatchReaders = trace.Readers;

			var batch = db.CreateQueryBatch();
			var total = batch.Add(totalPart);
			var page  = batch.Add(pagePart);

			trace.Reset();
			batch.Execute();

			total.Value.ShouldBe(BatchItem.Seed.Length);
			Ids(page.Value)
				.ShouldBe(PageIds(2, 3));
			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : queryBatchReaders);
		}

		sealed class Page
		{
			public int             Total;
			public List<BatchItem> Rows = null!;
		}

		static QueryBatchResult<int> AddTotal(QueryBatchBuilder batch, IQueryable<BatchItem> items)
		{
			return batch.Add(() => items.Count());
		}

		static QueryBatchResult<List<BatchItem>> AddRows(QueryBatchBuilder batch, IQueryable<BatchItem> items, int skip, int take)
		{
			return batch.Add(() => items
				.OrderBy(i => i.Id)
				.Skip(skip)
				.Take(take)
				.ToList());
		}

		// Where a part is added from makes no difference to the query, so one provider shows it.
		[Test]
		public async Task PartsFromSeveralMethods([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();
			var total = AddTotal(batch, items);
			var rows  = AddRows(batch, items, 4, 3);

			await Run(batch, isAsync);

			var result = new Page { Total = total.Value, Rows = rows.Value };

			result.Total.ShouldBe(BatchItem.Seed.Length);
			Ids(result.Rows)
				.ShouldBe(PageIds(4, 3));
		}

		// Equal-shaped parts are told apart while the query is built, before any SQL, so one provider shows it.
		[Test]
		public void SameShapedPartsReadTheirOwnValues([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch  = db.CreateQueryBatch();
			var counts = new List<QueryBatchResult<int>>();

			foreach (var id in new[] { 1, 2, 3 })
				counts.Add(batch.Add(() => items.Count(i => i.Id <= id)));

			Expression<Func<int>>             count = () => items.Count(i => i.GroupId == 2);
			Expression<Func<List<BatchItem>>> rows  = () => items
				.Where(i => i.GroupId == 2)
				.OrderBy(i => i.Id)
				.ToList();

			var count1 = batch.Add(count);
			var count2 = batch.Add(count);
			var rows1  = batch.Add(rows);
			var rows2  = batch.Add(rows);

			batch.Execute();

			counts
				.Select(c => c.Value)
				.ShouldBe([1, 2, 3]);

			var expectedRows = Ids(BatchItem.Seed.Where(i => i.GroupId == 2));

			count1.Value.ShouldBe(expectedRows.Length);
			count2.Value.ShouldBe(expectedRows.Length);
			Ids(rows1.Value)
				.ShouldBe(expectedRows);
			Ids(rows2.Value)
				.ShouldBe(expectedRows);
		}

		// The batch's own state decides this before any command, so one provider shows it.
		[Test]
		public void ValueBeforeExecuteThrows([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			trace.Reset();

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());

			Shouldly.Should
				.Throw<InvalidOperationException>(() => total.Value)
				.Message
				.ShouldBe(ErrorHelper.Error_QueryBatch_NotExecuted);
			trace.Readers.ShouldBe(0);
		}

		// The batch's own state decides this before any command, so one provider shows it.
		[Test]
		public async Task ExecutedBatchRefusesAddAndExecute([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());

			await Run(batch, isAsync);

			total.Value.ShouldBe(BatchItem.Seed.Length);

			Shouldly.Should
				.Throw<InvalidOperationException>(() => batch.Add(() => items.Count()))
				.Message
				.ShouldBe(ErrorHelper.Error_QueryBatch_AlreadyExecuted);

			var again = isAsync
				? await Shouldly.Should.ThrowAsync<InvalidOperationException>(() => batch.ExecuteAsync())
				: Shouldly.Should.Throw<InvalidOperationException>(batch.Execute);

			again.Message.ShouldBe(ErrorHelper.Error_QueryBatch_AlreadyExecuted);
		}

		// An empty batch builds no query, so a pending hint is neither refused nor taken.
		[Test]
		public async Task EmptyBatchSendsNothing([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			const string hint = "-- empty batch hint";

			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			db.NextQueryHints.Add(hint);
			trace.Reset();

			var batch = db.CreateQueryBatch();

			await Run(batch, isAsync);

			trace.Readers.ShouldBe(0);
			trace.Begins.ShouldBe(0);
			db.NextQueryHints.ShouldBe([hint]);
			Shouldly.Should
				.Throw<InvalidOperationException>(() => batch.Add(() => items.Count()))
				.Message
				.ShouldBe(ErrorHelper.Error_QueryBatch_AlreadyExecuted);

			db.NextQueryHints.Clear();
		}

		// Holds the batch's reader until released, so the test can act while ExecuteAsync is in flight.
		sealed class HoldingInterceptor : CommandInterceptor
		{
			public readonly TaskCompletionSource<bool> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
			public readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

			public override async Task<Option<DbDataReader>> ExecuteReaderAsync(CommandEventData eventData, DbCommand command, System.Data.CommandBehavior commandBehavior, Option<DbDataReader> result, CancellationToken cancellationToken)
			{
				Entered.TrySetResult(true);

				await Release.Task.ConfigureAwait(false);

				return result;
			}
		}

		// The batch's own state decides this; the interceptor needs a reader to hold, which SQLite gives.
		[Test]
		public async Task RunningBatchRefusesAddAndExecute([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var interceptor = new HoldingInterceptor();

			db.AddInterceptor(interceptor);

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());

			var running = batch.ExecuteAsync();

			try
			{
				var entered = await Task.WhenAny(interceptor.Entered.Task, Task.Delay(TimeSpan.FromSeconds(30)));

				entered.ShouldBeSameAs(interceptor.Entered.Task);

				Shouldly.Should
					.Throw<InvalidOperationException>(() => batch.Add(() => items.Count()))
					.Message
					.ShouldBe(ErrorHelper.Error_QueryBatch_Running);

				// The refusal comes before any await; a second run would wait on the held reader instead.
				var second = batch.ExecuteAsync();

				second.IsCompleted.ShouldBeTrue();

				var executeError = await Shouldly.Should.ThrowAsync<InvalidOperationException>(() => second);

				executeError.Message.ShouldBe(ErrorHelper.Error_QueryBatch_Running);
				Shouldly.Should
					.Throw<InvalidOperationException>(() => total.Value)
					.Message
					.ShouldBe(ErrorHelper.Error_QueryBatch_NotExecuted);
			}
			finally
			{
				interceptor.Release.TrySetResult(true);
			}

			await running;

			total.Value.ShouldBe(BatchItem.Seed.Length);
		}

		// The refusal comes before any command, so one provider shows that the batch stays retryable.
		[Test]
		public async Task RefusedHintsLeaveTheBatchRetryable([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			const string hint = "-- refused batch hint";

			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());

			db.NextQueryHints.Add(hint);

			var refused = await Shouldly.Should.ThrowAsync<LinqToDBException>(() => batch.ExecuteAsync());

			refused.Message.ShouldBe(ErrorHelper.Error_QueryHints_In_QueryBatch);

			db.NextQueryHints.Clear();

			await batch.ExecuteAsync();

			total.Value.ShouldBe(BatchItem.Seed.Length);
		}

		// The remote case runs the same batch through the server, one statement after another, so Traced keeps the test out
		// of the baselines that would differ from the direct ones.
		[Test]
		public async Task FailedRunLeavesTheBatchRetryable([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool isAsync)
		{
			using var db    = GetDataContext(context, Traced(new CommandTrace()));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int>> firstPart = () => items
				.Where(i => i.Id > 100)
				.Select(i => i.Id)
				.First();

			var batch = db.CreateQueryBatch();
			var first = batch.Add(firstPart);
			var page  = batch.Add(() => items
				.OrderBy(i => i.Id)
				.Take(2)
				.ToList());

			var alone = ThrowOfThePartAlone(firstPart);
			var error = isAsync
				? await Shouldly.Should.ThrowAsync<InvalidOperationException>(() => batch.ExecuteAsync())
				: Shouldly.Should.Throw<InvalidOperationException>(batch.Execute);

			error.Message.ShouldBe(alone.Message);
			Shouldly.Should
				.Throw<InvalidOperationException>(() => page.Value)
				.Message
				.ShouldBe(ErrorHelper.Error_QueryBatch_NotExecuted);

			items.Insert(() => new BatchItem { Id = 101, GroupId = 1 });

			await Run(batch, isAsync);

			first.Value.ShouldBe(101);
			Ids(page.Value)
				.ShouldBe(PageIds(0, 2));
		}

		[Table]
		sealed class BatchLater
		{
			[Column, PrimaryKey] public int Id { get; set; }
		}

		// PostgreSQL aborts a transaction on an error, so a retry there shows that the batch's own read transaction was
		// rolled back.
		[Test]
		public void DatabaseErrorLeavesTheBatchRetryable([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			// A table left behind by an interrupted run on a persistent database would make the first run succeed.
			db.DropTable<BatchLater>(throwExceptionIfNotExists: false);

			var later = db.GetTable<BatchLater>();

			var batch = db.CreateQueryBatch();
			var count = batch.Add(() => later.Count());
			var page  = batch.Add(() => items
				.OrderBy(i => i.Id)
				.Take(2)
				.ToList());

			Shouldly.Should.Throw<Exception>(batch.Execute);

			using var created = db.CreateLocalTable<BatchLater>();

			batch.Execute();

			count.Value.ShouldBe(0);
			Ids(page.Value)
				.ShouldBe(PageIds(0, 2));
		}

		// Without the check SQLite sends the reader or BEGIN first and SQL Server cancels it on the server.
		[Test]
		public async Task CancelledTokenSendsNothing([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());
			var page  = batch.Add(() => items
				.OrderBy(i => i.Id)
				.Take(2)
				.ToList());

			using var cancelled = new CancellationTokenSource();

			cancelled.Cancel();
			trace.Reset();

			await Shouldly.Should.ThrowAsync<OperationCanceledException>(() => batch.ExecuteAsync(cancelled.Token));

			trace.Readers.ShouldBe(0);
			trace.Begins.ShouldBe(0);

			await batch.ExecuteAsync();

			total.Value.ShouldBe(BatchItem.Seed.Length);
			Ids(page.Value)
				.ShouldBe(PageIds(0, 2));
		}

		// Captured values are read while the parameters are built, before any SQL, so one provider shows it.
		[Test]
		public void CapturedValuesAreReadAtExecute([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var take  = 2;
			var batch = db.CreateQueryBatch();
			var page  = batch.Add(() => items
				.OrderBy(i => i.Id)
				.Take(take)
				.ToList());

			take = 4;

			batch.Execute();

			Ids(page.Value)
				.ShouldBe(PageIds(0, 4));
		}

		// Query-cache reuse is decided before any SQL, so one provider shows it.
		[Test, QueryCacheTest]
		public void SameCallSitesReuseTheCachedQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			List<BatchItem> RunBatch(int take)
			{
				var batch = db.CreateQueryBatch();

				batch.Add(() => items.Count());

				var page = batch.Add(() => items
					.OrderBy(i => i.Id)
					.Take(take)
					.ToList());

				batch.Execute();

				return page.Value;
			}

			var first  = RunBatch(2);
			var misses = Query<(int, List<BatchItem>)>.CacheMissCount;
			var second = RunBatch(4);

			Query<(int, List<BatchItem>)>.CacheMissCount.ShouldBe(misses);
			Ids(first)
				.ShouldBe(PageIds(0, 2));
			Ids(second)
				.ShouldBe(PageIds(0, 4));
		}

		// The cached query is keyed by its result type: batches of 2 … 17 parts are 16 other tuple types, so they do not evict
		// the one-part batch. The predicate is this test's own, so the first run is a miss.
		[Test, QueryCacheTest]
		public void PartTypesKeyTheCachedQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			void RunBatch(int parts)
			{
				var batch = db.CreateQueryBatch();

				for (var added = 0; added < parts; added++)
					batch.Add(() => items.Count(i => i.Name != nameof(PartTypesKeyTheCachedQuery)));

				batch.Execute();
			}

			var before = Query<ValueTuple<int>>.CacheMissCount;

			RunBatch(1);

			var afterFirst = Query<ValueTuple<int>>.CacheMissCount;

			for (var parts = 2; parts <= 17; parts++)
				RunBatch(parts);

			var beforeAgain = Query<ValueTuple<int>>.CacheMissCount;

			RunBatch(1);

			(afterFirst - before)
				.ShouldBe(1);
			Query<ValueTuple<int>>.CacheMissCount.ShouldBe(beforeAgain);
		}

		// The refusal comes before any command, so one provider shows it; PendingHintsAreRefused covers DbBatch and remote.
		[Test]
		public void PendingHintsAreRefusedAtExecute([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool oneShot)
		{
			const string hint = "-- explicit batch hint";

			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var hints = oneShot ? db.NextQueryHints : db.QueryHints;

			hints.Add(hint);
			trace.Reset();

			var batch = db.CreateQueryBatch();

			batch.Add(() => items.Count());

			Shouldly.Should
				.Throw<LinqToDBException>(batch.Execute)
				.Message
				.ShouldBe(ErrorHelper.Error_QueryHints_In_QueryBatch);
			trace.Readers.ShouldBe(0);
			hints.ShouldBe([hint]);

			hints.Clear();
		}

		static Task<int> RefusedPartRuns()
		{
			throw new InvalidOperationException("A refused part was recorded.");
		}

		sealed class TaskHolder
		{
			public Task<int> Pending = null!;
		}

		static TaskHolder HoldTask()
		{
			throw new InvalidOperationException("A refused part was evaluated.");
		}

		static class ReadyTasks
		{
			public static readonly Task<int> Three = Task.FromResult(3);
		}

		// Add refuses the part before any query is built, so one provider shows it.
		[Test]
		public void TaskPartIsRefused([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();

			void ShouldRefuse(Action add)
			{
				Shouldly.Should
					.Throw<ArgumentException>(add)
					.Message
					.ShouldStartWith(ErrorHelper.Error_QueryBatch_TaskPart);
			}

			ShouldRefuse(() => batch.Add(() => items.ToListAsync(default)));
			ShouldRefuse(() => batch.Add<object>(() => items.ToListAsync(default)));
			ShouldRefuse(() => batch.Add(() => new { Rows = items.ToListAsync(default) }));
			ShouldRefuse(() => batch.Add(() => new ValueTask<int>(1)));

			// Recorded anyway, this part would fail the run below, so it shows that a refused part is not kept.
			ShouldRefuse(() => batch.Add(() => RefusedPartRuns()));

			// A part whose result is a task reads nothing, even when the task already exists.
			var ready = Task.FromResult(2);

			ShouldRefuse(() => batch.Add(() => ready));
			ShouldRefuse(() => batch.Add<object>(() => ready));

			// A field of a value the part computes may hold a task the part has just started.
			ShouldRefuse(() => batch.Add(() => HoldTask().Pending.Result));

			// A task held by a captured variable, a static field or a constant starts nothing when its result is read.
			var total    = batch.Add(() => items.Count());
			var filtered = batch.Add(() => items
				.Where(i => i.Id <= ready.Result)
				.OrderBy(i => i.Id)
				.ToList());
			var fromStatic = batch.Add(() => items.Count(i => i.Id <= ReadyTasks.Three.Result));
			var constant   = batch.Add(Expression.Lambda<Func<int>>(
				Expression.Property(Expression.Constant(Task.FromResult(4)), nameof(Task<>.Result))));

			trace.Reset();
			batch.Execute();

			total.Value.ShouldBe(BatchItem.Seed.Length);
			Ids(filtered.Value)
				.ShouldBe(PageIds(0, 2));
			fromStatic.Value.ShouldBe(3);
			constant.Value.ShouldBe(4);
			trace.Readers.ShouldBe(1);
		}

		// The strict reads of a value join the batch row to their source, which Sybase refuses as the sibling
		// NullValueReadsAsThePartAlone shows; the whole batch reports it.
		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void StrictValueReadsReadAsThePartAlone([DataSources(false, NoFromlessSubquery)] string context)
		{
			using var db    = GetDataContext(context);
			using var items = db.CreateLocalTable(BatchItem.Seed);

			Expression<Func<int?>>    nullPart   = () => items
				.Where(i => i.Id == 3)
				.Select(i => i.Value)
				.First();
			Expression<Func<string?>> namePart   = () => items
				.Where(i => i.Id == 5)
				.Select(i => i.Name)
				.First();

			var batch = db.CreateQueryBatch();
			var value = batch.Add(nullPart);
			var name  = batch.Add(namePart);
			var row   = batch.Add(() => items
				.Where(i => i.Id == 4)
				.Select(i => new { i.Id, i.Name })
				.First());
			var total = batch.Add(() => items.Count());

			batch.Execute();

			var valueAlone = nullPart
				.Compile()
				.Invoke();
			var nameAlone  = namePart
				.Compile()
				.Invoke();

			value.Value.ShouldBe(valueAlone);
			name.Value.ShouldBe(nameAlone);
			row.Value.ShouldBe(new { Id = 4, Name = (string?)"N4" });
			total.Value.ShouldBe(BatchItem.Seed.Length);
		}

		// The handle throws while the parameters are built, before any reader, so one provider shows it.
		[Test]
		public void HandleReadInsideAPartFails([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());

			batch.Add(() => items
				.Where(i => i.Id > total.Value)
				.ToList());

			trace.Reset();

			Shouldly.Should
				.Throw<InvalidOperationException>(batch.Execute)
				.Message
				.ShouldBe(ErrorHelper.Error_QueryBatch_NotExecuted);
			trace.Readers.ShouldBe(0);
			Shouldly.Should
				.Throw<InvalidOperationException>(() => total.Value)
				.Message
				.ShouldBe(ErrorHelper.Error_QueryBatch_NotExecuted);
		}

		// A queryable part is loaded with the batch, as a collection; SQLite (concat) and SQL Server (DbBatch) combine it.
		[Test]
		public void QueryablePartReadsInsideTheBatch([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			var trace = new CommandTrace();

			using var db    = GetDataContext(context, Traced(trace));
			using var items = db.CreateLocalTable(BatchItem.Seed);

			var batch = db.CreateQueryBatch();
			var total = batch.Add(() => items.Count());
			var rows  = batch.Add(() => items.Where(i => i.Id < 3));

			trace.Reset();
			batch.Execute();

			trace.Readers.ShouldBe(db.UsesCombinedQueryBatch() ? 1 : 2);

			trace.Reset();

			total.Value.ShouldBe(BatchItem.Seed.Length);
			Ids(rows.Value)
				.ShouldBe([1, 2], ignoreOrder: true);
			trace.Readers.ShouldBe(0);
		}
	}
}
