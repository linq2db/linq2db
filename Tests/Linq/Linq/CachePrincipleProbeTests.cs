using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Probes for the folding rule: a value is folded into the SQL only when it is a function of the query
	// expression itself. Everything else - a clock, the ambient culture, a member of a mutable object - is a
	// parameter and is read on every execution through the same cached query.
	[TestFixture]
	public class CachePrincipleProbeTests : TestBase
	{
		static void Dump(string label, IQueryable<int> q)
		{
			var sql = q.ToSqlQuery();
			TestContext.Out.WriteLine($"PROBE {label}: miss={q.GetCacheMissCount()} params={sql.Parameters.Count} sql={sql.Sql.Replace("\n", " ")}");
		}

		sealed class Counter
		{
			public int Value;
		}

		static readonly Counter _counter = new();

		// A mutable field read through a static readonly object: the object is fixed, its field is not.
		[Test, QueryCacheTest]
		public void MutableFieldOfStaticReadonlyObjectIsParameter([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query() { return db.Person.Where(p => p.ID == _counter.Value).Select(p => p.ID); }

			lock (_counter)
			{
				_counter.Value = 1;
				Query().ToArray().ShouldBe([1]);
				var missCount = Query().GetCacheMissCount();
				Dump("counter#1", Query());

				_counter.Value = 2;
				Query().ToArray().ShouldBe([2]);
				Dump("counter#2", Query());
				Query().GetCacheMissCount().ShouldBe(missCount);
			}
		}

		// The clock behind a static property: never folded, always the current value.
		[Test, QueryCacheTest]
		public void ClockIsParameter([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query() { return db.Types.Where(t => t.ID < DateTime.Now.Millisecond + 100000).Select(t => t.ID); }

			Query().ToArray();
			var missCount = Query().GetCacheMissCount();
			var sql1      = Query().ToSqlQuery();
			Dump("clock#1", Query());

			Thread.Sleep(50);

			Query().ToArray();
			var sql2 = Query().ToSqlQuery();
			Dump("clock#2", Query());

			Query().GetCacheMissCount().ShouldBe(missCount);
			sql1.Parameters.Count.ShouldBe(1);
			sql2.Parameters.Count.ShouldBe(1);
		}

		sealed class ArrayFlagBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqlExtensionBuilder builder)
			{
				builder.ResultExpression = new SqlValue(builder.GetValue<int[]>(0)[0] == 1);
			}
		}

		[Sql.Extension("", BuilderType = typeof(ArrayFlagBuilder), ServerSideOnly = true)]
		static bool ArrayFlag(int[] values) => values[0] == 1;

		static readonly MethodInfo _arrayFlag = typeof(CachePrincipleProbeTests).GetMethod(nameof(ArrayFlag), BindingFlags.NonPublic | BindingFlags.Static)!;

		// One constant array read by an extension builder (shapes the SQL) and indexed elsewhere (a parameter).
		// The parameter's accessor takes the array out of the key; the extension's read must then be compared.
		[Test, QueryCacheTest]
		public void ConstantArrayReadByExtensionAndIndexed([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query(int[] values)
			{
				var p = Expression.Parameter(typeof(Model.Person), "p");
				var c = Expression.Constant(values);

				var body = Expression.AndAlso(
					Expression.Call(_arrayFlag, c),
					Expression.Equal(Expression.PropertyOrField(p, nameof(Model.Person.ID)), Expression.ArrayIndex(c, Expression.Constant(1))));

				return db.Person
					.Where(Expression.Lambda<Func<Model.Person, bool>>(body, p))
					.Select(p => p.ID);
			}

			Query([1, 2]).ToArray().ShouldBe([2]);
			Query([0, 2]).ToArray().ShouldBeEmpty();
			Query([1, 3]).ToArray().ShouldBe([3]);
		}

		sealed class Counting
		{
			static int _next;

			public Counting()
			{
				Value = ++_next;
			}

			public int Value { get; }
		}

		sealed class Holder
		{
			public Holder(int value)
			{
				Value = value;
			}

			public int Value { get; }
		}

		static readonly Holder _holder = new(1);

		// A constructor runs code: new Counting().Value is a run-time input. A get-only auto-property read through
		// a static readonly object is fixed once the object is.
		[Test, QueryCacheTest]
		public void ConstructorWithSideEffectIsParameter([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<int> Query() { return db.Person.Select(p => Sql.AsSql(new Counting().Value)).Take(1); }

			var first  = Query().Single();
			var second = Query().Single();

			second.ShouldBe(first + 1);

			var sql = db.Person.Where(p => p.ID == _holder.Value).Select(p => p.ID).ToSqlQuery();
			sql.Parameters.Count.ShouldBe(0);
		}

		// A comparison that names the current culture reads ambient state: tr-TR's dotted I differs from en-US.
		[Test, QueryCacheTest]
		public void CurrentCultureComparisonIsParameter([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<bool> Query()
			{
				return db.Person.Where(p => p.ID == 1).Select(p => Sql.AsSql("i".Equals("I", StringComparison.CurrentCultureIgnoreCase)));
			}

			var culture = CultureInfo.CurrentCulture;

			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
				Query().Single().ShouldBeTrue();
				var missCount = Query().GetCacheMissCount();

				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
				Query().Single().ShouldBeFalse();
				Query().GetCacheMissCount().ShouldBe(missCount);

				db.Person.Where(p => p.ID == 1).Select(p => Sql.AsSql("i".Equals("I", StringComparison.OrdinalIgnoreCase))).ToSqlQuery().Parameters.Count.ShouldBe(0);
			}
			finally
			{
				CultureInfo.CurrentCulture = culture;
			}
		}

		// Culture-sensitive formatting over literals: the result depends on the ambient culture, not on the
		// expression. It stays a parameter, so a culture change is seen without a rebuild.
		[Test, QueryCacheTest]
		public void CultureSensitiveFormattingIsParameter([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);

			IQueryable<string> Query()
			{
				return db.Person.Where(p => p.ID == 1).Select(p => Sql.AsSql((1234.5).ToString()));
			}

			var culture = CultureInfo.CurrentCulture;

			try
			{
				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
				Query().Single().ShouldBe("1234.5");
				var missCount = Query().GetCacheMissCount();

				CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
				Query().Single().ShouldBe("1234,5");
				Query().GetCacheMissCount().ShouldBe(missCount);

				// An explicit culture makes the value a function of the expression: folded.
				db.Person.Where(p => p.ID == 1).Select(p => Sql.AsSql((1234.5).ToString(CultureInfo.InvariantCulture))).Single().ShouldBe("1234.5");
			}
			finally
			{
				CultureInfo.CurrentCulture = culture;
			}
		}
	}
}
