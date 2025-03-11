using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Shouldly;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class TakeSkipTests : TestBase
	{

		static void CheckTakeGlobalParams(IDataContext dc, int additional = 0)
		{
			CheckTakeSkipParams(dc, !LinqToDB.Common.Configuration.Linq.ParameterizeTakeSkip, additional);
		}

		static void CheckTakeSkipParams(IDataContext dc, bool inline, int additional = 0)
		{
			if (!(dc is DataConnection db))
				return;

			var commandInterceptor = new SaveCommandInterceptor();
			db.AddInterceptor(commandInterceptor);

			// check only strong providers
			if (!inline && db.DataProvider.SqlProviderFlags.AcceptsTakeAsParameter && db.DataProvider.SqlProviderFlags.AcceptsTakeAsParameterIfSkip)
				Assert.That(commandInterceptor.Parameters, Has.Length.GreaterThan(additional));
		}

		static void CheckTakeSkipParameterized(IDataContext dc, int additional = 0)
		{
			CheckTakeSkipParams(dc, false, additional);
		}

		[Test]
		public void Take1([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				for (var i = 2; i <= 3; i++)
				{
					Assert.That((from ch in db.Child select ch).Take(i).ToList(), Has.Count.EqualTo(i));
					CheckTakeGlobalParams(db);
				}

				var currentCacheMissCount = db.Child.GetCacheMissCount();

				for (var i = 2; i <= 3; i++)
				{
					Assert.That((from ch in db.Child select ch).Take(i).ToList(), Has.Count.EqualTo(i));
					CheckTakeGlobalParams(db);
				}

				Assert.That(db.Child.GetCacheMissCount(), Is.EqualTo(currentCacheMissCount));
			}
		}

		[Test]
		public async Task Take1Async([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				for (var i = 2; i <= 3; i++)
				{
					Assert.That((from ch in db.Child select ch).Take(i).ToList(), Has.Count.EqualTo(i));
					CheckTakeGlobalParams(db);
				}

				var currentCacheMissCount = db.Child.GetCacheMissCount();

				for (var i = 2; i <= 3; i++)
				{
					Assert.That((await (from ch in db.Child select ch).Take(i).ToListAsync()), Has.Count.EqualTo(i));
					CheckTakeGlobalParams(db);
				}

				Assert.That(db.Child.GetCacheMissCount(), Is.EqualTo(currentCacheMissCount));
			}
		}

		static void TakeParam(ITestDataContext dc, int n)
		{
			dc.Child.Take(() => n).ToList().Count.ShouldBe(n);

			CheckTakeSkipParameterized(dc);
		}

		static async Task TakeParamAsync(ITestDataContext dc, int n)
		{
			(await dc.Child.Take(() => n).ToListAsync()).Count.ShouldBe(n);

			CheckTakeSkipParameterized(dc);
		}

		[Test]
		public void Take2([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				TakeParam(db, 1);
		}

		[Test]
		public async Task Take2Async([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				await TakeParamAsync(db, 1);
		}

		[Test]
		public void Take3([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				(from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch)
					.Take(3)
					.ToList()
					.Count.ShouldBe(3);

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public async Task Take3Async([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				(await (from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch)
					.Take(3)
					.ToListAsync())
					.Count.ShouldBe(3);

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void Take4([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
					(from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch)
						.Take(3)
						.ToList()
						.Count.ShouldBe(3);

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public async Task Take4Async([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				(await (from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch)
					.Take(3)
					.ToListAsync())
					.Count.ShouldBe(3);

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void Take5([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				db.Child
					.Take(3)
					.ToList()
					.Count.ShouldBe(3);

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public async Task Take5Async([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				(await db.Child
					.Take(3)
					.ToListAsync())
					.Count.ShouldBe(3);

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void Take6([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var expected =    Child.OrderBy(c => c.ChildID).Take(3);
				var result   = db.Child.OrderBy(c => c.ChildID).Take(3);
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void Take7([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(db.Child.Take(() => 3).ToList(), Has.Count.EqualTo(3));
			}
		}

		[Test]
		public void Take8([DataSources] string context, [Values] bool withParameters)
		{
			var n = 3;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(db.Child.Take(() => n).ToList(), Has.Count.EqualTo(3));
			}
		}

		[Test]
		public void TakeCount([DataSources(TestProvName.AllSybase)] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					db.Child.Take(5).Count(), Is.EqualTo(Child.Take(5).Count()));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void Skip1([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				AreEqual(Child.OrderBy(_ => _.ChildID).Skip(3), db.Child.OrderBy(_ => _.ChildID).Skip(3));

				var currentCacheMissCount = db.Child.GetCacheMissCount();

				AreEqual(Child.OrderBy(_ => _.ChildID).Skip(4), db.Child.OrderBy(_ => _.ChildID).Skip(4));

				Assert.That(db.Child.GetCacheMissCount(), Is.EqualTo(currentCacheMissCount));
			}
		}

		[Test]
		public void Skip2([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				AreEqual(
					(from ch in    Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID).Skip(3),
					(from ch in db.Child where ch.ChildID > 3 || ch.ChildID < 4 select ch).OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID).Skip(3));
		}

		[Test]
		public void Skip3([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				AreEqual(
					(from ch in    Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID).Skip(3),
					(from ch in db.Child where ch.ChildID >= 0 && ch.ChildID <= 100 select ch).OrderBy(_ => _.ParentID).ThenBy(_ => _.ChildID).Skip(3));
			}
		}

		[Test]
		public void Skip4([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var expected = Child.OrderByDescending(c => c.ChildID).Skip(3);
				var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(3);
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
			}
		}

		[Test]
		public void Skip5([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				AreEqual(
					   Child.OrderByDescending(c => c.ChildID).ThenBy(c => c.ParentID + 1).Skip(3),
					db.Child.OrderByDescending(c => c.ChildID).ThenBy(c => c.ParentID + 1).Skip(3));
			}
		}

		[Test]
		public void Skip6([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				AreEqual(Child.OrderBy(_ => _.ChildID).Skip(3), db.Child.OrderBy(_ => _.ChildID).Skip(() => 3));
			}
		}

		[Test]
		public void Skip7([DataSources] string context, [Values] bool withParameters)
		{
			var n = 3;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				AreEqual(Child.OrderBy(_ => _.ChildID).Skip(n), db.Child.OrderBy(_ => _.ChildID).Skip(() => n));
			}
		}

		[Test]
		public void SkipCount([DataSources(
			TestProvName.AllSybase,
			TestProvName.AllSQLite,
			TestProvName.AllAccess)]
			string context,
			[Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					db.Child.Skip(2).Count(), Is.EqualTo(Child.Skip(2).Count()));
			}
		}

		[Test]
		public void SkipTake1([DataSources] string context, [Values] bool withParameters)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, withParameters);
			execute(context, withParameters);

			void execute(string context, bool withParameters)
			{
				using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				{
					var expected =    Child.OrderByDescending(c => c.ChildID).Skip(2).Take(5);
					var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(2).Take(5);
					Assert.That(result.ToList().SequenceEqual(expected), Is.True);
					CheckTakeGlobalParams(db);
				}
			}
		}

		[Test]
		public void SkipTake2([DataSources] string context, [Values] bool withParameters)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, withParameters);
			execute(context, withParameters);

			void execute(string context, bool withParameters)
			{
				using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				{
					var expected =    Child.OrderByDescending(c => c.ChildID).Take(7).Skip(2);
					var result   = db.Child.OrderByDescending(c => c.ChildID).Take(7).Skip(2);
					Assert.That(result.ToList().SequenceEqual(expected), Is.True);
					CheckTakeGlobalParams(db);
				}
			}
		}

		[Test]
		public void SkipTake3([DataSources] string context, [Values] bool withParameters)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, withParameters);
			execute(context, withParameters);

			void execute(string context, bool withParameters)
			{
				using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				{
					var expected = Child.OrderBy(c => c.ChildID).Skip(1).Take(7).Skip(2);
					var result   = db.Child.OrderBy(c => c.ChildID).Skip(1).Take(7).Skip(2);
					Assert.That(result.ToList().SequenceEqual(expected), Is.True);
					CheckTakeGlobalParams(db);
				}
			}
		}

		[Test]
		public void SkipTake21([DataSources] string context, [Values] bool withParameters)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, withParameters);
			execute(context, withParameters);

			void execute(string context, bool withParameters)
			{
				using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				{
					var skip = 2;
					var take = 5;
					var expected =    Child.OrderByDescending(c => c.ChildID).Skip(skip).Take(take);
					var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(skip).Take(take);
					Assert.That(result.ToList().SequenceEqual(expected), Is.True);
					CheckTakeGlobalParams(db);
				}
			}
		}

		[Test]
		public void SkipTake22([DataSources] string context, [Values] bool withParameters)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, withParameters);
			execute(context, withParameters);

			void execute(string context, bool withParameters)
			{
				using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				{
					var skip = 2;
					var take = 7;
					var expected =    Child.OrderByDescending(c => c.ChildID).Take(take).Skip(skip);
					var result   = db.Child.OrderByDescending(c => c.ChildID).Take(take).Skip(skip);
					Assert.That(result.ToList().SequenceEqual(expected), Is.True);
					CheckTakeGlobalParams(db);
				}
			}
		}

		[Test]
		public void SkipTake23([DataSources] string context, [Values] bool withParameters)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, withParameters);
			execute(context, withParameters);

			void execute(string context, bool withParameters)
			{
				using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				{
					var skip1 = 1;
					var skip2 = 2;
					var take = 7;
					var expected = Child.OrderBy(c => c.ChildID).Skip(skip1).Take(take).Skip(skip2);
					var result   = db.Child.OrderBy(c => c.ChildID).Skip(skip1).Take(take).Skip(skip2);
					Assert.That(result.ToList().SequenceEqual(expected), Is.True);
					CheckTakeGlobalParams(db);
				}
			}
		}

		[Test]
		public void SkipTake31([DataSources(false)] string context, [Values] bool inline)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, inline);
			execute(context, inline);

			void execute(string context, bool inline)
			{
				using (var db = GetDataConnection(context))
				{
					db.InlineParameters = inline;
					var skip = 2;
					var take = 5;
					var expected =    Child.OrderByDescending(c => c.ChildID).Skip(skip).Take(take);
					var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(skip).Take(take);

					Assert.That(result.ToList().SequenceEqual(expected), Is.True);

					CheckTakeSkipParams(db, inline);
				}
			}
		}

		[Test]
		public void SkipTake32([DataSources(false)] string context, [Values] bool inline)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, inline);
			execute(context, inline);

			void execute(string context, bool inline)
			{
				using (var db = GetDataConnection(context))
				{
					db.InlineParameters = inline;
					var skip = 2;
					var take = 7;
					var expected =    Child.OrderByDescending(c => c.ChildID).Take(take).Skip(skip);
					var result   = db.Child.OrderByDescending(c => c.ChildID).Take(take).Skip(skip);

					Assert.That(result.ToList().SequenceEqual(expected), Is.True);

					CheckTakeSkipParams(db, inline);
				}
			}
		}

		[Test]
		public void SkipTake33([DataSources(false)] string context, [Values] bool inline)
		{
			// repeat needed for providers with positional parameters with skip parameter first
			execute(context, inline);
			execute(context, inline);

			void execute(string context, bool inline)
			{
				using (var db = GetDataConnection(context))
				{
					db.InlineParameters = inline;
					var skip1 = 1;
					var skip2 = 2;
					var take = 7;
					var expected = Child.OrderBy(c => c.ChildID).Skip(skip1).Take(take).Skip(skip2);
					var result   = db.Child.OrderBy(c => c.ChildID).Skip(skip1).Take(take).Skip(skip2);

					Assert.That(result.ToList().SequenceEqual(expected), Is.True);

					CheckTakeSkipParams(db, inline);
				}
			}
		}

		[Test]
		public void SkipTake4([DataSources(
			TestProvName.AllSQLite,
			TestProvName.AllSybase,
			TestProvName.AllAccess)]
			string context,
			[Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var expected =    Child.OrderByDescending(c => c.ChildID).Skip(1).Take(7).OrderBy(c => c.ChildID).Skip(2);
				var result   = db.Child.OrderByDescending(c => c.ChildID).Skip(1).Take(7).OrderBy(c => c.ChildID).Skip(2);
				Assert.That(result.ToList().SequenceEqual(expected), Is.True);
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void SkipTake5([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var list = db.Child.Skip(2).Take(5).ToList();
				Assert.That(list, Has.Count.EqualTo(5));
				CheckTakeGlobalParams(db);
			}
		}

		void SkipTake6Internal(ITestDataContext db, bool doSkip)
		{
			var q1 = from g in db.GrandChild select g;

			if (doSkip)
				q1 = q1.Skip(12);
			q1 = q1.Take(3);

			var q2 =
				from c in db.Child
				from p in q1
				where c.ParentID == p.ParentID
				select c;

			var q3 = from g in GrandChild select g;

			if (doSkip)
				q3 = q3.Skip(12);
			q3 = q3.Take(3);

			var q4 =
				from c in Child
				from p in q3
				where c.ParentID == p.ParentID
				select c;

			AreEqual(q4, q2);
		}

		[Test]
		public void SkipTake6([DataSources(
			ProviderName.SqlCe,
			TestProvName.AllSybase,
			TestProvName.AllSQLite,
			TestProvName.AllAccess)]
			string context,
			[Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				SkipTake6Internal(db, false);
				CheckTakeGlobalParams(db);

				SkipTake6Internal(db, true);
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void SkipTakeCount([DataSources(
			ProviderName.SqlCe,
			TestProvName.AllSybase,
			TestProvName.AllSQLite,
			TestProvName.AllAccess)]
			string context,
			[Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					db.Child.Skip(2).Take(5).Count(), Is.EqualTo(Child.Skip(2).Take(5).Count()));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void SkipFirst([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var expected = (from p in Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).Skip(1).First();
				var result = from p in db.GetTable<Parent>() select p;
				result = from p in result where p.ParentID > 1 select p;
				var b = result.OrderBy(_ => _.ParentID).Skip(1).First();

				Assert.That(b, Is.EqualTo(expected));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void ElementAt1([DataSources] string context, [Values(2, 3)] int at, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					(from p in db.Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAt(at), Is.EqualTo((from p in    Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAt(at)));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void ElementAt2([DataSources] string context, [Values] bool withParameters)
		{
			var n = 3;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
				Assert.That(
					(from p in db.Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAt(() => n), Is.EqualTo((from p in    Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAt(n)));
		}

		[Test]
		public async Task ElementAt2Async([DataSources] string context, [Values] bool withParameters)
		{
			var n = 3;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					await (from p in db.Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtAsync(() => n), Is.EqualTo((from p in    Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAt(n)));
				CheckTakeSkipParameterized(db);
			}
		}

		[Test]
		public void ElementAtDefault1([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					(from p in db.Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtOrDefault(3), Is.EqualTo((from p in    Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtOrDefault(3)));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void ElementAtDefault2([DataSources] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That((from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(300000), Is.Null);
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void ElementAtDefault3([DataSources] string context, [Values] bool withParameters)
		{
			var n = 3;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					(from p in db.Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtOrDefault(() => n), Is.EqualTo((from p in    Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtOrDefault(n)));
				CheckTakeSkipParameterized(db);
			}
		}

		[Test]
		public async Task ElementAtDefault3Async([DataSources] string context, [Values] bool withParameters)
		{
			var n = 3;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That(
					await (from p in db.Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtOrDefaultAsync(() => n), Is.EqualTo((from p in    Parent where p.ParentID > 1 select p).OrderBy(_ => _.ParentID).ElementAtOrDefault(n)));
				CheckTakeSkipParameterized(db);
			}
		}

		[Test]
		public void ElementAtDefault4([DataSources] string context, [Values] bool withParameters)
		{
			var n = 300000;
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				Assert.That((from p in db.Parent where p.ParentID > 1 select p).ElementAtOrDefault(() => n), Is.Null);
				CheckTakeSkipParameterized(db);
			}
		}

		[Test]
		public void ElementAtDefault5([DataSources] string context, [Values(2,3)] int idx, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var missCount = db.Person.GetCacheMissCount();
				Assert.That(
					db.Person.OrderBy(p => p.LastName).ElementAtOrDefault(idx), Is.EqualTo(Person.   OrderBy(p => p.LastName).ElementAtOrDefault(idx)));
				CheckTakeGlobalParams(db);

				if (idx == 3)
					Assert.That(missCount, Is.EqualTo(db.Person.GetCacheMissCount()));
			}
		}

		[Test]
		public void TakeWithPercent([IncludeDataSources(true, TestProvName.AllAccess, TestProvName.AllSqlServer)] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var q = db.Person.Take(50, TakeHints.Percent).Select(_ => _);

				Assert.That(q, Is.Not.Empty);

				var sql = q.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("PERCENT"));
				CheckTakeGlobalParams(db);
			}

		}

		[Test]
		public void TakeWithPercent1([IncludeDataSources(TestProvName.AllAccess, TestProvName.AllSqlServer)] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var q = db.Person.Take(() => 50, TakeHints.Percent).Select(_ => _);

				Assert.That(q, Is.Not.Empty);

				var sql = q.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("PERCENT"));
			}

		}

		[Test]
		public void TakeWithTies([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var q = db.Person.OrderBy(_ => _.FirstName).Take(50, TakeHints.WithTies | TakeHints.Percent).Select(_ => _);

				Assert.That(q, Is.Not.Empty);

				var sql = q.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("PERCENT"));
				Assert.That(sql, Does.Contain("WITH"));
				CheckTakeGlobalParams(db);
			}

		}

		[Test]
		public void TakeWithTies2([IncludeDataSources(TestProvName.AllSqlServer)] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var q = db.Person.OrderBy(_ => _.FirstName).Take(() => 50, TakeHints.WithTies | TakeHints.Percent).Select(_ => _);

				Assert.That(q, Is.Not.Empty);

				var sql = q.ToSqlQuery().Sql;
				Assert.That(sql, Does.Contain("PERCENT"));
				Assert.That(sql, Does.Contain("WITH"));
			}

		}

		[Test]
		public void SkipTakeWithTies([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				Assert.Throws<LinqToDBException>(() => db.Person.Skip(1).Take(() => 50, TakeHints.WithTies | TakeHints.Percent).Select(_ => _).ToList());

				Assert.Throws<LinqToDBException>(() => db.Person.Take(() => 50, TakeHints.WithTies | TakeHints.Percent).Skip(1).Select(_ => _).ToList());
			}
		}

		[Test]
		public void TakeWithHintsFails([IncludeDataSources(ProviderName.SqlCe, TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.Throws<LinqToDBException>(() => db.Parent.Take(10, TakeHints.Percent).ToList());
		}

		[Test]
		public void TakeSkipJoin([DataSources(TestProvName.AllSybase)] string context, [Values] bool withParameters)
		{
			// orderby needed to preserve stable test results
			// but access returns wrong number of records if orderby applied to subquery with take
			var orderUnsupported = context.IsAnyOf(TestProvName.AllAccess);

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				var types = db.Types.ToList();

				var q1 = types.Concat(types).Take(15);
				var q2 = db.Types.Concat(db.Types).Take(15);

				if (!orderUnsupported)
				{
					q1 = q1.OrderBy(_ => _.ID);
					q2 = q2.OrderBy(_ => _.ID);
				}

				AreEqual(
					from e in q1
					from p in q1.Where(_ => _.ID == e.ID).DefaultIfEmpty()
					select new {e.ID, p.SmallIntValue},
					from e in q2
					from p in q2.Where(_ => _.ID == e.ID).DefaultIfEmpty()
					select new { e.ID, p.SmallIntValue }
					);
				CheckTakeGlobalParams(db);
			}
		}

		public class Batch
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public string? Value { get; set; }

			[Association(ThisKey = "Id", OtherKey = "BatchId", CanBeNull = false)]
			public List<Confirmation> Confirmations { get; set; } = null!;
		}

		public class Confirmation
		{
			[Column]
			public int BatchId { get; set; }
			[Column]
			public DateTime Date { get; set; }
		}

		[Test]
		public void FirstOrDefaultInSubQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllAccess, TestProvName.AllSqlServer)] string context, [Values] bool withParameters)
		{
			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			{
				using (db.CreateLocalTable(new[]
				{
					new Batch { Id = 1, Value = "V1" },
					new Batch { Id = 2, Value = "V2" },
					new Batch { Id = 3, Value = "V3" }
				}))
				using (db.CreateLocalTable(new[]
				{
					new Confirmation { BatchId = 1, Date = DateTime.Parse("09 Apr 2019 14:30:00 GMT", DateTimeFormatInfo.InvariantInfo) },
					new Confirmation { BatchId = 2, Date = DateTime.Parse("09 Apr 2019 14:30:20 GMT", DateTimeFormatInfo.InvariantInfo) },
					new Confirmation { BatchId = 2, Date = DateTime.Parse("09 Apr 2019 14:30:25 GMT", DateTimeFormatInfo.InvariantInfo) },
					new Confirmation { BatchId = 3, Date = DateTime.Parse("09 Apr 2019 14:30:35 GMT", DateTimeFormatInfo.InvariantInfo) },
				}))
				{

					var query = db.GetTable<Batch>()
							.OrderByDescending(x => x.Id)
							.Select(x => new
							{
								BatchId = x.Id,
								CreationDate = x.Confirmations.FirstOrDefault()!.Date,
								x.Value
							})
							.Take(2)
							.OrderBy(x => x.BatchId);

					var res = query.ToList();

					Assert.That(res, Has.Count.EqualTo(2));
					Assert.Multiple(() =>
					{
						Assert.That(res[0].BatchId, Is.EqualTo(2));
						Assert.That(res[0].Value, Is.EqualTo("V2"));
						Assert.That(res[1].BatchId, Is.EqualTo(3));
						Assert.That(res[1].Value, Is.EqualTo("V3"));
						Assert.That(res[0].CreationDate, Is.EqualTo(DateTime.Parse("09 Apr 2019 14:30:20 GMT", DateTimeFormatInfo.InvariantInfo)));
						Assert.That(res[1].CreationDate, Is.EqualTo(DateTime.Parse("09 Apr 2019 14:30:35 GMT", DateTimeFormatInfo.InvariantInfo)));
					});

					CheckTakeGlobalParams(db);
				}
			}
		}

		sealed class TakeSkipClass
		{
			[Column(DataType = DataType.VarChar, Length = 10)]
			public string? Value { get; set; }

			private bool Equals(TakeSkipClass other)
			{
				return Value == other.Value;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
				return Equals((TakeSkipClass)obj);
			}

			public override int GetHashCode()
			{
				return (Value != null ? Value.GetHashCode() : 0);
			}
		}

		// Sybase, Informix: doesn't support TOP/FIRST in subqueries
		[Test]
		public void GroupTakeAnyTest([DataSources(TestProvName.AllSybase, TestProvName.AllInformix)] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.GroupBy(item => item.Value)
					.Where(group => group.Count() > 1)
					.Select(item => item.Key)
					.Take(1)
					.Any();

				var expected = testData
					.GroupBy(item => item.Value)
					.Where(group => group.Count() > 1)
					.Select(item => item.Key)
					.Take(1)
					.Any();

				Assert.That(actual, Is.EqualTo(expected));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void DistinctTakeTest([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.Distinct()
					.Take(3)
					.ToArray();

				var expected = testData
					.Distinct()
					.Take(3)
					.ToArray();

				AreEqual(expected, actual);
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void OrderByTakeTest([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));
				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTake1([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(3)
					.Take(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(3)
					.Take(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTake2([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(2)
					.Take(3)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(2)
					.Take(3)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTake3([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(1)
					.Take(3)
					.Take(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(1)
					.Take(3)
					.Take(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTake4([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(2)
					.Take(3)
					.Take(1)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(2)
					.Take(3)
					.Take(1)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleSkip1([DataSources] string context)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Skip(1)
					.Skip(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Skip(1)
					.Skip(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}
			}
		}

		[Test]
		public async Task MultipleSkip1Async([DataSources] string context)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = await tempTable
					.OrderBy(t => t.Value)
					.Skip(1)
					.Skip(2)
					.ToArrayAsync();

				var expected = testData
					.OrderBy(t => t.Value)
					.Skip(1)
					.Skip(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}
			}
		}

		[Test]
		public void MultipleSkip2([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "PIPPO" },
				new TakeSkipClass { Value = "PLUTO" },
				new TakeSkipClass { Value = "BOLTO" }
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{
				for (int i = 1; i <= 2; i++)
				{
					var missCount = tempTable.GetCacheMissCount();

					var actual = tempTable
						.OrderBy(t => t.Value)
						.Skip(2)
						.Skip(i)
						.ToArray();

					var expected = testData
						.OrderBy(t => t.Value)
						.Skip(2)
						.Skip(i)
						.ToArray();

					Assert.That(actual, Is.EqualTo(expected));

					if (db is TestDataConnection cn)
					{
						Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
						Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
					}

					if (i == 2)
						Assert.That(missCount, Is.EqualTo(tempTable.GetCacheMissCount()));

				}
			}
		}

		[Test]
		public void MultipleSkip3([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Skip(2)
					.Skip(3)
					.Skip(1)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Skip(2)
					.Skip(3)
					.Skip(1)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}
			}
		}

		[Test]
		public void MultipleSkip4([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Skip(1)
					.Skip(3)
					.Skip(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Skip(1)
					.Skip(3)
					.Skip(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}
			}
		}

		[Test]
		public void MultipleTakeSkip1([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(6)
					.Skip(2)
					.Take(2)
					.Skip(1)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(6)
					.Skip(2)
					.Take(2)
					.Skip(1)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTakeSkip2([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Skip(2)
					.Take(5)
					.Skip(1)
					.Take(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Skip(2)
					.Take(5)
					.Skip(1)
					.Take(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTakeSkip3([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
				new TakeSkipClass { Value = "Value9" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Take(8)
					.Skip(1)
					.Take(4)
					.Skip(1)
					.Take(2)
					.Skip(1)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Take(8)
					.Skip(1)
					.Take(4)
					.Skip(1)
					.Take(2)
					.Skip(1)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void MultipleTakeSkip4([DataSources] string context, [Values] bool withParameters)
		{
			var testData = new[]
			{
				new TakeSkipClass { Value = "Value1" },
				new TakeSkipClass { Value = "Value2" },
				new TakeSkipClass { Value = "Value3" },
				new TakeSkipClass { Value = "Value4" },
				new TakeSkipClass { Value = "Value5" },
				new TakeSkipClass { Value = "Value6" },
				new TakeSkipClass { Value = "Value7" },
				new TakeSkipClass { Value = "Value8" },
				new TakeSkipClass { Value = "Value9" },
			};

			using (var db = GetDataContext(context, o => o.UseParameterizeTakeSkip(withParameters)))
			using (var tempTable = db.CreateLocalTable(testData))
			{

				var actual = tempTable
					.OrderBy(t => t.Value)
					.Skip(1)
					.Take(8)
					.Skip(1)
					.Take(4)
					.Skip(1)
					.Take(2)
					.ToArray();

				var expected = testData
					.OrderBy(t => t.Value)
					.Skip(1)
					.Take(8)
					.Skip(1)
					.Take(4)
					.Skip(1)
					.Take(2)
					.ToArray();

				Assert.That(actual, Is.EqualTo(expected));

				if (db is TestDataConnection cn)
				{
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("iif"));
					Assert.That(cn.LastQuery!.ToLowerInvariant(), Does.Not.Contain("case"));
				}

				CheckTakeGlobalParams(db);
			}
		}

		[Test]
		public void SkipTakeCaching([DataSources] string context, [Values(1, 2)] int skip, [Values(1, 2)] int take)
		{
			using var db = GetDataContext(context);

			var cacheMissCount = db.Parent.GetCacheMissCount();

			var result = db.Parent
				.OrderBy(t => t.Value1)
				.Skip(skip)
				.Take(take)
				.ToArray();

			if (skip > 1 || take > 1)
				db.Parent.GetCacheMissCount().ShouldBe(cacheMissCount);
		}
	}
}
