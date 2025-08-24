using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Interceptors;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	/*
	 * Provides basic test coverage for:
	 * - DataContextExtensions APIs
	 * - CommandInfo (implicitly as calls from DataContextExtensions)
	 * 
	 * Test with:
	 * - DataConnection
	 * - DataContext with KeepConnectionAlive true/false
	 * 
	 * DataContextExtensions.SetCommand converage provided implicitly through other DataContextExtensions calls
	 */
	[TestFixture]
	public class DataContextExtensionsTests : TestBase
	{
		sealed class CountOpenInterceptor : ConnectionInterceptor
		{
			private int _sync;
			private int _async;

			public override void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
			{
				_sync++;
			}

			public override Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
			{
				_async++;

				return Task.CompletedTask;
			}

			public void AssertCounters(int expectedSync, int expectedAsync)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(_sync, Is.EqualTo(expectedSync));
					Assert.That(_async, Is.EqualTo(expectedAsync));
				}
			}
		}

		sealed class CountCloseInterceptor : DataContextInterceptor
		{
			private int _sync;
			private int _async;

			public override void OnClosed(DataContextEventData eventData)
			{
				_sync++;
			}

			public override Task OnClosedAsync(DataContextEventData eventData)
			{
				_async++;

				return Task.CompletedTask;
			}

			public void AssertCounters(int expectedSync, int expectedAsync)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(_sync, Is.EqualTo(expectedSync));
					Assert.That(_async, Is.EqualTo(expectedAsync));
				}
			}
		}

		void Test(string context, Action<IDataContext> action)
		{
			// test with DataConnection
			using (var db = GetDataContext(context))
			{
				action(db);
			}

			// test with DataContext
			var open = new CountOpenInterceptor();
			var close = new CountCloseInterceptor();
			using (var db = new DataContext(new DataOptions().UseConfiguration(context).UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(true);

				action(db);

				open.AssertCounters(1, 0);
				close.AssertCounters(0, 0);
			}

			open = new CountOpenInterceptor();
			close = new CountCloseInterceptor();
			using (var db = new DataContext(new DataOptions().UseConfiguration(context).UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(false);

				action(db);

				open.AssertCounters(1, 0);
				close.AssertCounters(1, 0);
			}
		}

		async ValueTask TestAsync(string context, Func<IDataContext, ValueTask> action, bool forceCloseSync = false)
		{
			// test with DataConnection
			using (var db = GetDataContext(context))
			{
				await action(db);
			}

			// test with DataContext
			var open = new CountOpenInterceptor();
			var close = new CountCloseInterceptor();
			using (var db = new DataContext(new DataOptions().UseConfiguration(context).UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(true);

				await action(db);

				open.AssertCounters(0, 1);
				close.AssertCounters(0, 0);
			}

			open = new CountOpenInterceptor();
			close = new CountCloseInterceptor();
			using (var db = new DataContext(new DataOptions().UseConfiguration(context).UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(false);

				await action(db);

				open.AssertCounters(0, 1);

				if (forceCloseSync)
					close.AssertCounters(1, 0);
				else
					close.AssertCounters(0, 1);
			}
		}

		[Test]
		public async ValueTask Query_WithReader([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			Test(context, db =>
			{
				var res = db.Query(r => r.GetInt32(0), sql).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(r => r.GetInt32(0), sql, cancellationToken: default);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async ValueTask Query_WithReader_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			Test(context, db =>
			{
				var res = db.Query(r => r.GetInt32(0), sql, p1, p2).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(r => r.GetInt32(0), sql, p1, p2);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(r => r.GetInt32(0), sql, cancellationToken: default, p1, p2);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask Query_WithReader_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			Test(context, db =>
			{
				var res = db.Query(r => r.GetInt32(0), sql, parameters).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(r => r.GetInt32(0),sql, parameters, cancellationToken: default);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryProc_WithReader_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var input = DataParameter.Int32("input", 1);
			var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var sql = "QueryProcParameters";

			Test(context, db =>
			{
				var res = db.QueryProc(r => r.GetInt32(0), sql, input, output1, output2).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync(r => r.GetInt32(0), sql, input, output1, output2);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync(r => r.GetInt32(0), sql, cancellationToken: default, input, output1, output2);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(4));
				Assert.That(res, Does.Contain(1));
				Assert.That(res, Does.Contain(2));
				Assert.That(res, Does.Contain(3));
				Assert.That(res, Does.Contain(4));
			}
		}

		[Test]
		public async ValueTask QueryProc_WithReader_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 1, output1 = 0, output2 = 0 };
			var sql = "QueryProcParameters";

			Test(context, db =>
			{
				var res = db.QueryProc(r => r.GetInt32(0), sql, parameters).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync(r => r.GetInt32(0), sql, parameters, cancellationToken: default);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(4));
				Assert.That(res, Does.Contain(1));
				Assert.That(res, Does.Contain(2));
				Assert.That(res, Does.Contain(3));
				Assert.That(res, Does.Contain(4));
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithReader([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(r => r.GetInt32(0), sql, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithReader_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(r => r.GetInt32(0), sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(r => r.GetInt32(0), sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithReader_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(r => r.GetInt32(0), sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithReader([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(r => r.GetInt32(0), sql, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithReader_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(r => r.GetInt32(0), sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(r => r.GetInt32(0), sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithReader_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(r => r.GetInt32(0), sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithReader([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable(r => r.GetInt32(0), sql);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 0;
				await foreach (var i in res)
				{
					cnt++;
					Assert.That(i, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.EqualTo(2));
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithReader_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable(r => r.GetInt32(0), sql, p1, p2);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(i, Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithReader_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable(r => r.GetInt32(0), sql, parameters);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(i, Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask QueryForEachAsync_WithReader_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = new List<int>();
				await db.QueryForEachAsync(r => r.GetInt32(0), r => res.Add(r), sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = new List<int>();
				await db.QueryForEachAsync(r => r.GetInt32(0), r => res.Add(r), sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryForEachAsync_WithReader_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = new List<int>();
				await db.QueryForEachAsync(r => r.GetInt32(0), r => res.Add(r), sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask Query([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			Test(context, db =>
			{
				var res = db.Query<int>(sql).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = (await db.QueryAsync<int>(sql, cancellationToken: default)).ToArray();
				AssertResults(res);
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async ValueTask Query_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			Test(context, db =>
			{
				var res = db.Query<int>(sql, p1, p2).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync<int>(sql, p1, p2);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync<int>(sql, cancellationToken: default, p1, p2);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask Query_And_SingleDataParam([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var sql = "SELECT @p1 UNION ALL SELECT @p1 + 2";

			Test(context, db =>
			{
				var res = db.Query<int>(sql, p1).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync<int>(sql, p1, cancellationToken: default);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(3));
				}
			}
		}

		[Test]
		public async ValueTask Query_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			Test(context, db =>
			{
				var res = db.Query<int>(sql, parameters).ToArray();
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync<int>(sql, parameters, cancellationToken: default);
				AssertResults(res.ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask Query_WithTemplate_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 as ID UNION ALL SELECT @p1";

			Test(context, db =>
			{
				var res = db.Query(new { ID = 1 }, sql, p1, p2);
				AssertResults(res.Select(r => r.ID).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(new { ID = 1 }, sql, p1, p2);
				AssertResults(res.Select(r => r.ID).ToArray());
			}, forceCloseSync: true);

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(new { ID = 1 }, sql, cancellationToken: default, p1, p2);
				AssertResults(res.Select(r => r.ID).ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask Query_WithTemplate_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 AS ID UNION ALL SELECT @p1";

			Test(context, db =>
			{
				var res = db.Query(new { ID = 1 }, sql, parameters);
				AssertResults(res.Select(r => r.ID).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryAsync(new { ID = 1 }, sql, parameters, cancellationToken: default);
				AssertResults(res.Select(r => r.ID).ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		sealed class MultipleResultExample
		{
			[ResultSetIndex(2)] public List<Person>? Set3 { get; set; }
			[ResultSetIndex(1)] public IEnumerable<Person>? Set2 { get; set; }
			[ResultSetIndex(0)] public Person[]? Set1 { get; set; }
		}

		[Test]
		public async ValueTask QueryMultiple_With_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var p3 = DataParameter.Int32("p3", 3);
			var sql = "SELECT * FROM Person WHERE PersonID <> @p1; SELECT * FROM Person WHERE PersonID <> @p2; SELECT * FROM Person WHERE PersonID <> @p3";

			Test(context, db =>
			{
				var res = db.QueryMultiple<MultipleResultExample>(sql, p1, p2, p3);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryMultipleAsync<MultipleResultExample>(sql, p1, p2, p3);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryMultipleAsync<MultipleResultExample>(sql, cancellationToken: default, p1, p2, p3);
				AssertResults(res);
			});

			static void AssertResults(MultipleResultExample res)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1, Is.Not.Null);
					Assert.That(res.Set2, Is.Not.Null);
					Assert.That(res.Set3, Is.Not.Null);
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1, Has.Length.EqualTo(3));
					Assert.That(res.Set2.Count(), Is.EqualTo(3));
					Assert.That(res.Set3, Has.Count.EqualTo(3));
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1.Count(r => r.ID == 2), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 3), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 4), Is.EqualTo(1));

					Assert.That(res.Set2.Count(r => r.ID == 1), Is.EqualTo(1));
					Assert.That(res.Set2.Count(r => r.ID == 3), Is.EqualTo(1));
					Assert.That(res.Set2.Count(r => r.ID == 4), Is.EqualTo(1));

					Assert.That(res.Set3.Count(r => r.ID == 1), Is.EqualTo(1));
					Assert.That(res.Set3.Count(r => r.ID == 2), Is.EqualTo(1));
					Assert.That(res.Set3.Count(r => r.ID == 4), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryProc_With_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var input = DataParameter.Int32("input", 1);
			var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var sql = "QueryProcParameters";

			Test(context, db =>
			{
				var res = db.QueryProc<Person>(sql, input, output1, output2);
				AssertResults(res.Select(p => p.ID).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync<Person>(sql, input, output1, output2);
				AssertResults(res.Select(p => p.ID).ToArray());
			}, forceCloseSync: true);

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync<Person>(sql, cancellationToken: default, input, output1, output2);
				AssertResults(res.Select(p => p.ID).ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(4));
				Assert.That(res, Does.Contain(1));
				Assert.That(res, Does.Contain(2));
				Assert.That(res, Does.Contain(3));
				Assert.That(res, Does.Contain(4));
			}
		}

		[Test]
		public async ValueTask QueryProc_With_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 1, output1 = 0, output2 = 0 };
			var sql = "QueryProcParameters";

			Test(context, db =>
			{
				var res = db.QueryProc<Person>(sql, parameters);
				AssertResults(res.Select(p => p.ID).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync<Person>(sql, parameters, cancellationToken: default);
				AssertResults(res.Select(p => p.ID).ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(4));
				Assert.That(res, Does.Contain(1));
				Assert.That(res, Does.Contain(2));
				Assert.That(res, Does.Contain(3));
				Assert.That(res, Does.Contain(4));
			}
		}

		[Test]
		public async ValueTask QueryProc_WithTemplate_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var input = DataParameter.Int32("input", 1);
			var output1 = new DataParameter("output1", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var output2 = new DataParameter("output2", null, DataType.Int32) { Direction = ParameterDirection.Output };
			var sql = "QueryProcParameters";

			Test(context, db =>
			{
				var res = db.QueryProc(new { PersonID = 1 }, sql, input, output1, output2);
				AssertResults(res.Select(p => p.PersonID).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync(new { PersonID = 1 }, sql, input, output1, output2);
				AssertResults(res.Select(p => p.PersonID).ToArray());
			}, forceCloseSync: true);

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync(new { PersonID = 1 }, sql, cancellationToken: default, input, output1, output2);
				AssertResults(res.Select(p => p.PersonID).ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(4));
				Assert.That(res, Does.Contain(1));
				Assert.That(res, Does.Contain(2));
				Assert.That(res, Does.Contain(3));
				Assert.That(res, Does.Contain(4));
			}
		}

		[Test]
		public async ValueTask QueryProc_WithTemplate_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 1, output1 = 0, output2 = 0 };
			var sql = "QueryProcParameters";

			Test(context, db =>
			{
				var res = db.QueryProc(new { PersonID = 1 }, sql, parameters);
				AssertResults(res.Select(p => p.PersonID).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcAsync(new { PersonID = 1 }, sql, parameters, cancellationToken: default);
				AssertResults(res.Select(p => p.PersonID).ToArray());
			}, forceCloseSync: true);

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(4));
				Assert.That(res, Does.Contain(1));
				Assert.That(res, Does.Contain(2));
				Assert.That(res, Does.Contain(3));
				Assert.That(res, Does.Contain(4));
			}
		}

		sealed class MultipleProcResultExample
		{
			[ResultSetIndex(1)] public Doctor[]? Set2 { get; set; }
			[ResultSetIndex(0)] public Person[]? Set1 { get; set; }
		}

		[Test]
		public async ValueTask QueryProcMultiple_With_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("input", 1);
			var p2 = DataParameter.Int32("output1", 0);
			var p3 = DataParameter.Int32("output2", 0);
			var p4 = DataParameter.Int32("output3", 0);
			var sql = "QueryProcMultipleParameters";

			Test(context, db =>
			{
				var res = db.QueryProcMultiple<MultipleProcResultExample>(sql, p1, p2, p3, p4);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcMultipleAsync<MultipleProcResultExample>(sql, p1, p2, p3, p4);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcMultipleAsync<MultipleProcResultExample>(sql, cancellationToken: default, p1, p2, p3, p4);
				AssertResults(res);
			});

			static void AssertResults(MultipleProcResultExample res)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1, Is.Not.Null);
					Assert.That(res.Set2, Is.Not.Null);
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1, Has.Length.EqualTo(4));
					Assert.That(res.Set2.Count(), Is.EqualTo(1));
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1.Count(r => r.ID == 1), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 2), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 3), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 4), Is.EqualTo(1));

					Assert.That(res.Set2.Count(r => r.PersonID == 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryProcMultiple_With_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 2, output1 = 0, output2 = 0, output3 = 0 };
			var sql = "QueryProcMultipleParameters";

			Test(context, db =>
			{
				var res = db.QueryProcMultiple<MultipleProcResultExample>(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryProcMultipleAsync<MultipleProcResultExample>(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(MultipleProcResultExample res)
			{
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1, Is.Not.Null);
					Assert.That(res.Set2, Is.Not.Null);
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1, Has.Length.EqualTo(4));
					Assert.That(res.Set2.Count(), Is.EqualTo(1));
				}

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Set1.Count(r => r.ID == 1), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 2), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 3), Is.EqualTo(1));
					Assert.That(res.Set1.Count(r => r.ID == 4), Is.EqualTo(1));

					Assert.That(res.Set2.Count(r => r.PersonID == 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync<int>(sql, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync<int>(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync<int>(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithDataParmeter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var sql = "SELECT @p1 UNION ALL SELECT @p1 + 2";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync<int>(sql, p1, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(3));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync<int>(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync<int>(sql, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(1));
					Assert.That(res[1], Is.EqualTo(2));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync<int>(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync<int>(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var sql = "SELECT @p1 + 1 UNION ALL SELECT @p1 + 3";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync<int>(sql, p1, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(4));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync<int>(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable<int>(sql);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 0;
				await foreach (var i in res)
				{
					cnt++;
					Assert.That(i, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.EqualTo(2));
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable<int>(sql, p1, p2);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(i, Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var sql = "SELECT @p1 + 1 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable<int>(sql, p1);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(i, Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable<int>(sql, parameters);
				await AssertResults(res);
			});

			static async ValueTask AssertResults(IAsyncEnumerable<int> res)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(i, Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask QueryForEachAsync_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = new List<int>();
				await db.QueryForEachAsync<int>(r => res.Add(r), sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = new List<int>();
				await db.QueryForEachAsync<int>(r => res.Add(r), sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryForEachAsync_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = new List<int>();
				await db.QueryForEachAsync<int>(r => res.Add(r), sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithTemplate_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 AS Id UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(new { Id = 1 }, sql, p1, p2);
				AssertResults(res.Select(r => r.Id).ToList());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(new { Id = 1 }, sql, cancellationToken: default, p1, p2);
				AssertResults(res.Select(r => r.Id).ToList());
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToListAsync_WithTemplate_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 AS Id UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToListAsync(new { Id = 1 }, sql, parameters, cancellationToken: default);
				AssertResults(res.Select(r => r.Id).ToList());
			});

			static void AssertResults(List<int> res)
			{
				Assert.That(res, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithTemplate_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 AS Id UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(new { Id = 1 }, sql, p1, p2);
				AssertResults(res.Select(r => r.Id).ToArray());
			});

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(new { Id = 1 }, sql, cancellationToken: default, p1, p2);
				AssertResults(res.Select(r => r.Id).ToArray());
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToArrayAsync_WithTemplate_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 AS Id UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = await db.QueryToArrayAsync(new { Id = 1 }, sql, parameters, cancellationToken: default);
				AssertResults(res.Select(r => r.Id).ToArray());
			});

			static void AssertResults(int[] res)
			{
				Assert.That(res, Has.Length.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0], Is.EqualTo(2));
					Assert.That(res[1], Is.EqualTo(1));
				}
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithTemplate_And_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p2 AS Id UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable(new { Id = 1 }, sql, p1, p2);
				await AssertResults(res, r => r.Id);
			});

			static async ValueTask AssertResults<T>(IAsyncEnumerable<T> res, Func<T, int> getValue)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(getValue(i), Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask QueryToAsyncEnumerable_WithTemplate_And_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p2 AS Id UNION ALL SELECT @p1";

			await TestAsync(context, async db =>
			{
				var res = db.QueryToAsyncEnumerable(new { Id = 1 }, sql, parameters);
				await AssertResults(res, r => r.Id);
			});

			static async ValueTask AssertResults<T>(IAsyncEnumerable<T> res, Func<T, int> getValue)
			{
				var cnt = 2;
				await foreach (var i in res)
				{
					Assert.That(getValue(i), Is.EqualTo(cnt));
					cnt--;
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		[Test]
		public async ValueTask Execute_NonQuery([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "UPDATE Person SET MiddleName = NULL WHERE MiddleName IS NULL";

			Test(context, db =>
			{
				var res = db.Execute(sql);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync(sql, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(3));
			}
		}

		[Test]
		public async ValueTask Execute_NonQuery_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "UPDATE Person SET MiddleName = NULL WHERE MiddleName IS NULL AND PersonID NOT IN(@p1, @p2)";

			Test(context, db =>
			{
				var res = db.Execute(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(1));
			}
		}

		[Test]
		public async ValueTask Execute_NonQuery_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2, output = 0 };
			var sql = "UPDATE Person SET MiddleName = NULL WHERE MiddleName IS NULL AND PersonID NOT IN(@p1, @p2)";

			Test(context, db =>
			{
				var res = db.Execute(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(1));
			}
		}

		[Test]
		public async ValueTask ExecuteProc_NonQuery_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("input", 2);
			var p2 = DataParameter.Int32("output", 0);
			var sql = "ExecuteProcIntParameters";

			Test(context, db =>
			{
				var res = db.ExecuteProc(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteProcAsync(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteProcAsync(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(1));
			}
		}

		[Test]
		public async ValueTask ExecuteProc_NonQuery_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 1, output = 0 };
			var sql = "ExecuteProcIntParameters";

			Test(context, db =>
			{
				var res = db.ExecuteProc(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteProcAsync(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(1));
			}
		}

		[Test]
		public async ValueTask Execute_Scalar([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 5";

			Test(context, db =>
			{
				var res = db.Execute<int>(sql);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync<int>(sql, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(5));
			}
		}

		[Test]
		public async ValueTask Execute_Scalar_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var p2 = DataParameter.Int32("p2", 2);
			var sql = "SELECT @p1 + @p2";

			Test(context, db =>
			{
				var res = db.Execute<int>(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync<int>(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync<int>(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(3));
			}
		}

		[Test]
		public async ValueTask Execute_Scalar_WithDataParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 2);
			var sql = "SELECT @p1 + 3";

			Test(context, db =>
			{
				var res = db.Execute<int>(sql, p1);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync<int>(sql, p1, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(5));
			}
		}

		[Test]
		public async ValueTask Execute_Scalar_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p1 + @p2";

			Test(context, db =>
			{
				var res = db.Execute<int>(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteAsync<int>(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(int res)
			{
				Assert.That(res, Is.EqualTo(3));
			}
		}

		[Test]
		public async ValueTask ExecuteProc_Scalar_WithDataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("input", 2);
			var p2 = DataParameter.Int32("output", 2);
			var sql = "ExecuteProcStringParameters";

			Test(context, db =>
			{
				var res = db.ExecuteProc<string>(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteProcAsync<string>(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteProcAsync<string>(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(string res)
			{
				Assert.That(res, Is.EqualTo("издрасте"));
			}
		}

		[Test]
		public async ValueTask ExecuteProc_Scalar_WithObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 1, output = 0 };
			var sql = "ExecuteProcStringParameters";

			Test(context, db =>
			{
				var res = db.ExecuteProc<string>(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				var res = await db.ExecuteProcAsync<string>(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(string res)
			{
				Assert.That(res, Is.EqualTo("издрасте"));
			}
		}

		[Test]
		public async ValueTask ExecuteReader([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var sql = "SELECT 1 UNION ALL SELECT 2";

			Test(context, db =>
			{
				using var res = db.ExecuteReader(sql);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = (await db.ExecuteReaderAsync(sql, cancellationToken: default));
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetInt32(0);

					Assert.That(v, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.EqualTo(2));
			}
		}

		[Test]
		public async ValueTask ExecuteReader_With_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 2);
			var p2 = DataParameter.Int32("p2", 1);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			Test(context, db =>
			{
				using var res = db.ExecuteReader(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderAsync(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderAsync(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetInt32(0);

					Assert.That(v, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.EqualTo(2));
			}
		}

		[Test]
		public async ValueTask ExecuteReader_With_SingleDataParam([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 1);
			var sql = "SELECT @p1 UNION ALL SELECT @p1 + 1";

			Test(context, db =>
			{
				using var res = db.ExecuteReader(sql, p1);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderAsync(sql, p1, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetInt32(0);

					Assert.That(v, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.EqualTo(2));
			}
		}

		[Test]
		public async ValueTask ExecuteReader_With_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { p1 = 1, p2 = 2 };
			var sql = "SELECT @p1 UNION ALL SELECT @p2";

			Test(context, db =>
			{
				using var res = db.ExecuteReader(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderAsync(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetInt32(0);

					Assert.That(v, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.EqualTo(2));
			}
		}

		[Test]
		public async ValueTask ExecuteReader_With_DataParams_AndBehavior([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("p1", 2);
			var p2 = DataParameter.Int32("p2", 1);
			var sql = "SELECT @p2 UNION ALL SELECT @p1";

			Test(context, db =>
			{
				using var res = db.ExecuteReader(sql, CommandType.Text, CommandBehavior.SchemaOnly, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderAsync(sql, CommandType.Text, CommandBehavior.SchemaOnly, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderAsync(sql, CommandType.Text, CommandBehavior.SchemaOnly, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetInt32(0);

					Assert.That(v, Is.EqualTo(cnt));
				}

				Assert.That(cnt, Is.Zero);
			}
		}

		/// <summary>
		/// 
		/// 
		/// </summary>
		/// 

		[Test]
		public async ValueTask ExecuteReaderProc_With_DataParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var p1 = DataParameter.Int32("input", 2);
			var p2 = DataParameter.Int32("output", 0);
			var sql = "ExecuteProcStringParameters";

			Test(context, db =>
			{
				using var res = db.ExecuteReaderProc(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderProcAsync(sql, p1, p2);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderProcAsync(sql, cancellationToken: default, p1, p2);
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetString(0);

					Assert.That(v, Is.EqualTo("издрасте"));
				}

				Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public async ValueTask ExecuteReaderProc_With_ObjectParams([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			var parameters = new { input = 1, output = 0 };
			var sql = "ExecuteProcStringParameters";

			Test(context, db =>
			{
				using var res = db.ExecuteReaderProc(sql, parameters);
				AssertResults(res);
			});

			await TestAsync(context, async db =>
			{
				await using var res = await db.ExecuteReaderProcAsync(sql, parameters, cancellationToken: default);
				AssertResults(res);
			});

			static void AssertResults(DataReaderAsync res)
			{
				var cnt = 0;
				while (res.Reader!.Read())
				{
					cnt++;
					var v = res.Reader.GetString(0);

					Assert.That(v, Is.EqualTo("издрасте"));
				}

				Assert.That(cnt, Is.EqualTo(1));
			}
		}

		sealed class BulkCopyTable
		{
			[PrimaryKey] public int Id { get; set; }
			[PrimaryKey] public int Value { get; set; }

			public static readonly BulkCopyTable[] Data =
			[
				new () { Id = 1, Value = 10 },
				new () { Id = 2, Value = 20 },
			];

			public static async IAsyncEnumerable<BulkCopyTable> AsyncEnumerableData()
			{
				yield return new BulkCopyTable() { Id = 1, Value = 10 };
				await Task.CompletedTask;
				yield return new BulkCopyTable() { Id = 2, Value = 20 };
			}
		}

		[Test]
		public async ValueTask BulkCopy_Enumerable([DataSources(false)] string context, [Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType type)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<BulkCopyTable>();

			TestBulkCopy(tb, context, db =>
			{
				db.BulkCopy(new BulkCopyOptions().WithBulkCopyType(type), BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				db.BulkCopy(1, BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				db.BulkCopy(BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				db.GetTable<BulkCopyTable>().BulkCopy(new BulkCopyOptions().WithBulkCopyType(type), BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				db.GetTable<BulkCopyTable>().BulkCopy(1, BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				db.GetTable<BulkCopyTable>().BulkCopy(BulkCopyTable.Data);
			});

			// provider doesn't have async BulkCopy API
			var forceSync = type == BulkCopyType.ProviderSpecific
				&& context.IsAnyOf(TestProvName.AllOracle);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(type), BulkCopyTable.Data);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.BulkCopyAsync(1, BulkCopyTable.Data);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.BulkCopyAsync(BulkCopyTable.Data);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(type), BulkCopyTable.Data);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(1, BulkCopyTable.Data);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(BulkCopyTable.Data);
			}, forceSync: forceSync);
		}

		[Test]
		public async ValueTask BulkCopy_AsyncEnumerable([DataSources(false)] string context, [Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType type)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<BulkCopyTable>();

			// provider doesn't have async BulkCopy API
			var forceSync = type == BulkCopyType.ProviderSpecific
				&& context.IsAnyOf(TestProvName.AllOracle);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(type), BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.BulkCopyAsync(1, BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.BulkCopyAsync(BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(type), BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(1, BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, forceSync: forceSync);

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(type));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, forceSync: forceSync);
		}

		[Test]
		public async ValueTask BulkCopy_Enumerable_OracleModes([IncludeDataSources(TestProvName.AllOracle)] string context, [Values] AlternativeBulkCopy type)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<BulkCopyTable>();

			TestBulkCopy(tb, context, db =>
			{
				db.BulkCopy(new BulkCopyOptions().WithBulkCopyType(BulkCopyType.MultipleRows), BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				db.BulkCopy(1, BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				db.BulkCopy(BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				db.GetTable<BulkCopyTable>().BulkCopy(new BulkCopyOptions().WithBulkCopyType(BulkCopyType.MultipleRows), BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				db.GetTable<BulkCopyTable>().BulkCopy(1, BulkCopyTable.Data);
			});

			TestBulkCopy(tb, context, db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				db.GetTable<BulkCopyTable>().BulkCopy(BulkCopyTable.Data);
			});

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(BulkCopyType.MultipleRows), BulkCopyTable.Data);
			});

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.BulkCopyAsync(1, BulkCopyTable.Data);
			});

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.BulkCopyAsync(BulkCopyTable.Data);
			});

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(BulkCopyType.MultipleRows), BulkCopyTable.Data);
			});

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(1, BulkCopyTable.Data);
			});

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(BulkCopyTable.Data);
			});
		}

		[Test]
		public async ValueTask BulkCopy_AsyncEnumerable_OracleModes([IncludeDataSources(TestProvName.AllOracle)] string context, [Values] AlternativeBulkCopy type)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<BulkCopyTable>();

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(BulkCopyType.MultipleRows), BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, o => o.UseOracle(o => o with { AlternativeBulkCopy = type }));

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.BulkCopyAsync(1, BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, o => o.UseOracle(o => o with { AlternativeBulkCopy = type }));

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.BulkCopyAsync(BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, o => o.UseOracle(o => o with { AlternativeBulkCopy = type }));

			await TestBulkCopyAsync(tb, context, async db =>
			{
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(new BulkCopyOptions().WithBulkCopyType(BulkCopyType.MultipleRows), BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, o => o.UseOracle(o => o with { AlternativeBulkCopy = type }));

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(1, BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, o => o.UseOracle(o => o with { AlternativeBulkCopy = type }));

			await TestBulkCopyAsync(tb, context, async db =>
			{
				using var _ = db.UseBulkCopyOptions(o => o.WithBulkCopyType(BulkCopyType.MultipleRows));
				await db.GetTable<BulkCopyTable>().BulkCopyAsync(BulkCopyTable.AsyncEnumerableData(), cancellationToken: default);
			}, o => o.UseOracle(o => o with { AlternativeBulkCopy = type }));
		}

		void TestBulkCopy(ITable<BulkCopyTable> table, string context, Action<IDataContext> action, Func<DataOptions, DataOptions>? customOptions = null)
		{
			var options = new DataOptions().UseConfiguration(context);
			if (customOptions != null)
				options = customOptions(options);

			// test with DataConnection
			using (var db = new DataConnection(options))
			{
				action(db);
			}

			AssertResults(table);

			// test with DataContext
			var open = new CountOpenInterceptor();
			var close = new CountCloseInterceptor();
			using (var db = new DataContext(options.UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(true);

				action(db);

				open.AssertCounters(1, 0);
				close.AssertCounters(0, 0);
			}

			AssertResults(table);

			open = new CountOpenInterceptor();
			close = new CountCloseInterceptor();
			using (var db = new DataContext(options.UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(false);

				action(db);

				open.AssertCounters(1, 0);
				close.AssertCounters(1, 0);
			}

			AssertResults(table);

			static void AssertResults(ITable<BulkCopyTable> table)
			{
				var res = table.OrderBy(r => r.Id).ToArray();
				table.Delete();

				Assert.That(res, Has.Length.EqualTo(2));

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].Id, Is.EqualTo(1));
					Assert.That(res[0].Value, Is.EqualTo(10));
					Assert.That(res[1].Id, Is.EqualTo(2));
					Assert.That(res[1].Value, Is.EqualTo(20));
				}
			}
		}

		async ValueTask TestBulkCopyAsync(ITable<BulkCopyTable> table, string context, Func<IDataContext, ValueTask> action, Func<DataOptions, DataOptions>? customOptions = null, bool forceSync = false)
		{
			var options = new DataOptions().UseConfiguration(context);
			if (customOptions != null)
				options = customOptions(options);

			// test with DataConnection
			using (var db = new DataConnection(options))
			{
				await action(db);
			}

			AssertResults(table);

			// test with DataContext
			var open = new CountOpenInterceptor();
			var close = new CountCloseInterceptor();
			using (var db = new DataContext(options.UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(true);

				await action(db);

				if (forceSync)
					open.AssertCounters(1, 0);
				else
					open.AssertCounters(0, 1);
				close.AssertCounters(0, 0);
			}

			AssertResults(table);

			open = new CountOpenInterceptor();
			close = new CountCloseInterceptor();
			using (var db = new DataContext(options.UseInterceptors(open, close)))
			{
				db.SetKeepConnectionAlive(false);

				await action(db);

				if (forceSync)
				{
					open.AssertCounters(1, 0);
					close.AssertCounters(1, 0);
				}
				else
				{
					open.AssertCounters(0, 1);
					close.AssertCounters(0, 1);
				}
			}

			AssertResults(table);

			static void AssertResults(ITable<BulkCopyTable> table)
			{
				var res = table.OrderBy(r => r.Id).ToArray();
				table.Delete();

				Assert.That(res, Has.Length.EqualTo(2));

				using (Assert.EnterMultipleScope())
				{
					Assert.That(res[0].Id, Is.EqualTo(1));
					Assert.That(res[0].Value, Is.EqualTo(10));
					Assert.That(res[1].Id, Is.EqualTo(2));
					Assert.That(res[1].Value, Is.EqualTo(20));
				}
			}
		}

		[Table]
		sealed class RetrieveIdentityTable
		{
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public async ValueTask RetrieveIdentity([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<RetrieveIdentityTable>();

			Test(context, db =>
			{
				var records = new RetrieveIdentityTable[]
				{
					new(),
					new(),
				};

				records.RetrieveIdentity(db, useIdentity: true);

				using (Assert.EnterMultipleScope())
				{
					Assert.That(records[0].Id, Is.EqualTo(2));
					Assert.That(records[1].Id, Is.EqualTo(3));
				}
			});

			await TestAsync(context, async db =>
			{
				var records = new RetrieveIdentityTable[]
				{
					new(),
					new(),
				};

				await records.RetrieveIdentityAsync(db, useIdentity: true, cancellationToken: default);

				using (Assert.EnterMultipleScope())
				{
					Assert.That(records[0].Id, Is.EqualTo(2));
					Assert.That(records[1].Id, Is.EqualTo(3));
				}
			});
		}
	}
}
