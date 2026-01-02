using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;

using LinqToDB;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Exceptions
{
	[TestFixture]
	public class StackUseTests : TestBase
	{
		sealed class Issue5265Table
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable01
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable02
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable03
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable04
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		sealed class Issue5265SubTable05
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public Issue5265SubTable05? SubTable5 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5265")]
		public void EagerLoadProjection([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var sc  = new ThreadHopsScope(-1);
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue5265Table>();
			using var t1 = db.CreateLocalTable<Issue5265SubTable01>();
			using var t2 = db.CreateLocalTable<Issue5265SubTable02>();
			using var t3 = db.CreateLocalTable<Issue5265SubTable03>();
			using var t4 = db.CreateLocalTable<Issue5265SubTable04>();
			using var t5 = db.CreateLocalTable<Issue5265SubTable05>();

#if DEBUG
			// initial: 710K
			const int LKG_SIZE = 200 * 1024;
#else
			// initial: 390K
			const int LKG_SIZE = 190 * 1024;
#endif
			var thread = new Thread(ThreadBody, LKG_SIZE);
			thread.Start();
			thread.Join();

			void ThreadBody(object? context)
			{
				_ = tb
					.LoadWith(e => e.SubTable3!.SubTable5!.SubTable2!.SubTable4!.SubTable1!.SubTable2!
						.SubTable3!.SubTable3!.SubTable5!.SubTable2!.SubTable4!.SubTable1!.SubTable4)

					.ToArray();
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5265")]
		public void TestStackHopOption([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var sc  = new ThreadHopsScope(1);
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue5265Table>();
			using var t1 = db.CreateLocalTable<Issue5265SubTable01>();
			using var t2 = db.CreateLocalTable<Issue5265SubTable02>();
			using var t3 = db.CreateLocalTable<Issue5265SubTable03>();
			using var t4 = db.CreateLocalTable<Issue5265SubTable04>();
			using var t5 = db.CreateLocalTable<Issue5265SubTable05>();

			// start from small-stack thread to hop fast as otherwise we need to add more associations
			// which make this query really slow
			var thread = new Thread(ThreadBody, 150 * 1024);
			thread.Start();
			thread.Join();

			void ThreadBody(object? context)
			{
				_ = tb
					.LoadWith(e => e.SubTable3!.SubTable5!.SubTable2!.SubTable4!.SubTable1!.SubTable2!
						.SubTable5!.SubTable3!.SubTable3!.SubTable5!.SubTable2!.SubTable4!.SubTable1!.SubTable4)
					.ToArray();
			}
		}

		[Test]
		public void TestPreserveExceptionOnHop()
		{
			using var sc  = new ThreadHopsScope(5);

			var mi = MethodHelper.GetMethodInfo(Call);
			var expr = Expression.Call(mi, Expression.Constant(null, typeof(object)));
			for (var i = 0; i < 30_000; i++)
				expr = Expression.Call(mi, expr);

			Assert.That(() => new TestExpressionVisitor(true).Visit(expr), Throws.InvalidOperationException);
		}

		[Test]
		public void TestExpressionVisitorHops([Values(0, 1, 2, 10)] int hops)
		{
			using var sc  = new ThreadHopsScope(hops);

			var iterations = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
				? 300_000
				: 30_000;

			var mi = MethodHelper.GetMethodInfo(Call);
			var expr = Expression.Call(mi, Expression.Constant(null, typeof(object)));
			for (var i = 0; i < iterations; i++)
				expr = Expression.Call(mi, expr);

			if (hops is 0)
			{
				Assert.That(() => new TestExpressionVisitor().Visit(expr), Throws.InstanceOf<InsufficientExecutionStackException>().And.InnerException.Null);
			}
			else if (hops is 1)
			{
				Assert.That(() => new TestExpressionVisitor().Visit(expr), Throws.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InnerException.Null);
			}
			else if (hops is 2)
			{
				Assert.That(() => new TestExpressionVisitor().Visit(expr), Throws.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InnerException.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InnerException.InnerException.Null);
			}
			else
			{
				new TestExpressionVisitor().Visit(expr);
			}
		}

		static object? Call(object? param) => param;

		sealed class TestExpressionVisitor(bool throwCustom = false) : ExpressionVisitorBase
		{
			private int _counter;

			[return: NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				_counter++;
				if (throwCustom && _counter > 25000)
					throw new InvalidOperationException("Something wrong exception");

				return base.Visit(node);
			}
		}

		[Test]
		public void TestSqlVisitorHops([Values(0, 1, 2, 10)] int hops)
		{
			using var sc  = new ThreadHopsScope(hops);

			var iterations = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
				? 100_000
				: 10_000;

			const string name = "fake";
			var type = new DbDataType(typeof(int));
			var expr = new SqlFunction(type, name, new SqlValue(1));
			for (var i = 0; i < iterations; i++)
				expr = new SqlFunction(type, name, expr);

			if (hops is 0)
			{
				Assert.That(() => new TestSqlVisitor().Visit(expr), Throws.InstanceOf<InsufficientExecutionStackException>().And.InnerException.Null);
			}
			else if (hops is 1)
			{
				Assert.That(() => new TestSqlVisitor().Visit(expr), Throws.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InnerException.Null);
			}
			else if (hops is 2)
			{
				Assert.That(() => new TestSqlVisitor().Visit(expr), Throws.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InnerException.InstanceOf<InsufficientExecutionStackException>()
					.And.InnerException.InnerException.InnerException.Null);
			}
			else
			{
				new TestSqlVisitor().Visit(expr);
			}
		}

		sealed class TestSqlVisitor() : QueryElementVisitor(VisitMode.ReadOnly);
	}
}
