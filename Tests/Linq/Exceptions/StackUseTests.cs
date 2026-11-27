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

using Shouldly;

namespace Tests.Exceptions
{
	// mutates the global Configuration.TranslationThreadMaxHopCount (via ThreadHopsScope)
	[TestFixture, NonParallelizable]
	public class StackUseTests : TestBase
	{
		sealed class AssociationChainRoot
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode05? SubTable5 { get; set; }
		}

		sealed class AssociationChainNode01
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode05? SubTable5 { get; set; }
		}

		sealed class AssociationChainNode02
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode05? SubTable5 { get; set; }
		}

		sealed class AssociationChainNode03
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode05? SubTable5 { get; set; }
		}

		sealed class AssociationChainNode04
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode05? SubTable5 { get; set; }
		}

		sealed class AssociationChainNode05
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int FK { get; set; }
			public int Field { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode01? SubTable1 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode02? SubTable2 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode03? SubTable3 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode04? SubTable4 { get; set; }
			[Association(ThisKey = nameof(Id), OtherKey = "FK")] public AssociationChainNode05? SubTable5 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5265")]
		public void EagerLoadProjection([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var sc  = new ThreadHopsScope(-1);
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<AssociationChainRoot>();
			using var t1 = db.CreateLocalTable<AssociationChainNode01>();
			using var t2 = db.CreateLocalTable<AssociationChainNode02>();
			using var t3 = db.CreateLocalTable<AssociationChainNode03>();
			using var t4 = db.CreateLocalTable<AssociationChainNode04>();
			using var t5 = db.CreateLocalTable<AssociationChainNode05>();

			// initial stack use: 710K under DEBUG, 390K under RELEASE; both tuned down to the same
			// last-known-good size, so the configuration split is no longer needed here
			const int LKG_SIZE = 200 * 1024;
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
			using var tb = db.CreateLocalTable<AssociationChainRoot>();
			using var t1 = db.CreateLocalTable<AssociationChainNode01>();
			using var t2 = db.CreateLocalTable<AssociationChainNode02>();
			using var t3 = db.CreateLocalTable<AssociationChainNode03>();
			using var t4 = db.CreateLocalTable<AssociationChainNode04>();
			using var t5 = db.CreateLocalTable<AssociationChainNode05>();

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

			var iterations = OperatingSystem.IsLinux()
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

			var iterations = OperatingSystem.IsLinux()
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

		// access needs more stack  as it is in-process provider and needs to use our stack
		[Test(Description = "https://github.com/linq2db/linq2db/issues/3674")]
		public void LargeOrFilterInSmallStackThread([DataSources(false, TestProvName.AllNativeAccess)] string context)
		{
			using var sc = new ThreadHopsScope(-1);
			using var db = GetDataContext((string)context!);
			using var tb = db.CreateLocalTable<LargeOrFilterEntity>();

			// 512 Kb used by IIS and MacOS runtime
			// we will use less to detect regressions earlier
			// e.g. now it required 130Kb (release) / 190Kb (debug) of memory
			// Note that stack use could depend on provider, so we test all of them
			// UPDATE: after fixes of binary expression aggregation implementation it works with 70K limit (debug, net9.0)

			var size = 80 * 1024;

			// octonica provider needs some extra stack space
			if (context.IsAnyOf(ProviderName.ClickHouseOctonica))
				size += 10 * 1024;

			var thread = new Thread(RunLargeOrFilterQuery, size);
			thread.Start(tb);
			thread.Join();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3674")]
		public void LargeOrFilterInCurrentThread([DataSources(false)] string context)
		{
			using var sc = new ThreadHopsScope(-1);
			using var db = GetDataContext((string)context!);
			using var tb = db.CreateLocalTable<LargeOrFilterEntity>();

			RunLargeOrFilterQuery(tb);
		}

		void RunLargeOrFilterQuery(object? context)
		{
			var entity = ((ITable<LargeOrFilterEntity>)context!).FirstOrDefault(p => p.Code == "42" && (
						p.DIM_Company == null || p.DIM_Company == ""
						|| p.DIM_Company == "1" || p.DIM_Company == "2" || p.DIM_Company == "3" || p.DIM_Company == "4" || p.DIM_Company == "5" || p.DIM_Company == "6" || p.DIM_Company == "7" || p.DIM_Company == "8" || p.DIM_Company == "9" || p.DIM_Company == "0"
					) && (
						p.DIM_Branch == null || p.DIM_Branch == ""
						|| p.DIM_Branch == "1" || p.DIM_Branch == "2" || p.DIM_Branch == "3" || p.DIM_Branch == "4" || p.DIM_Branch == "5" || p.DIM_Branch == "6" || p.DIM_Branch == "7" || p.DIM_Branch == "8" || p.DIM_Branch == "9" || p.DIM_Branch == "0"
					) && (
						p.DIM_Location == null || p.DIM_Location == ""
						|| p.DIM_Location == "1" || p.DIM_Location == "2" || p.DIM_Location == "3" || p.DIM_Location == "4" || p.DIM_Location == "5" || p.DIM_Location == "6" || p.DIM_Location == "7" || p.DIM_Location == "8" || p.DIM_Location == "9" || p.DIM_Location == "0"
					) && (
						p.DIM_MSegment == null || p.DIM_MSegment == ""
						|| p.DIM_MSegment == "1" || p.DIM_MSegment == "2" || p.DIM_MSegment == "3" || p.DIM_MSegment == "4" || p.DIM_MSegment == "5" || p.DIM_MSegment == "6" || p.DIM_MSegment == "7" || p.DIM_MSegment == "8" || p.DIM_MSegment == "9" || p.DIM_MSegment == "0"
					) && (
						p.DIM_Make == null || p.DIM_Make == ""
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
						|| p.DIM_Make == "1" || p.DIM_Make == "2" || p.DIM_Make == "3" || p.DIM_Make == "4" || p.DIM_Make == "5" || p.DIM_Make == "6" || p.DIM_Make == "7" || p.DIM_Make == "8" || p.DIM_Make == "9" || p.DIM_Make == "0"
					)
				);
		}

		[Table("LargeOrFilterEntity")]
		sealed class LargeOrFilterEntity
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(Length = 30, CanBeNull = false)] public string Code { get; set; } = null!;
			[Column(Length = 30)] public string? DIM_Company { get; set; }
			[Column(Length = 30)] public string? DIM_Branch { get; set; }
			[Column(Length = 30)] public string? DIM_Location { get; set; }
			[Column(Length = 30)] public string? DIM_MSegment { get; set; }
			[Column(Length = 30)] public string? DIM_Make { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/447")]
		public void LargeOrPredicateDoesNotOverflowStack([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

			var array = Enumerable.Range(0, 3000).ToArray();

			// Build "where" conditions
			var param = Expression.Parameter(typeof(Model.Child));
			Expression<Func<Model.Child, bool>>? predicate = null;

			for (var i = 0; i < array.Length; i++)
			{
				var id = array[i];

				var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
					(Expression.Equal(
						Expression.Convert(Expression.PropertyOrField(param, "ChildID"), typeof(int)),
						Expression.Constant(id)
					), param);

				predicate = predicate != null ? Or(predicate, filterExpression) : filterExpression;
			}

			result = result.Where(predicate!);

			// StackOverflowException cannot be handled and will terminate process
			_ = result.ToSqlQuery().Sql;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/447"), QueryCacheTest]
		public void LargeOrPredicateIsServedFromQueryCache([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

			var array = Enumerable.Range(0, 3000).ToArray();

			// Build "where" conditions
			var param = Expression.Parameter(typeof(Model.Child));
			Expression<Func<Model.Child, bool>>? predicate1 = null;
			Expression<Func<Model.Child, bool>>? predicate2 = null;

			for (var i = 0; i < array.Length; i++)
			{
				var id = array[i];

				var filterExpression = Expression.Lambda<Func<Model.Child, bool>>
					(Expression.Equal(
						Expression.Convert(Expression.PropertyOrField(param, "ChildID"), typeof(int)),
						Expression.Constant(id)
					), param);

				predicate1 = predicate1 != null ? Or(predicate1, filterExpression) : filterExpression;
				predicate2 = predicate2 != null ? Or(predicate2, filterExpression) : filterExpression;
			}

			var result1 = result.Where(predicate1!);
			var result2 = result.Where(predicate2!);

			// StackOverflowException cannot be handled and will terminate process
			_ = result1.ToSqlQuery().Sql;

			var cacheMiss = result1.GetCacheMissCount();

			// from cache
			_ = result2.ToSqlQuery().Sql;

			result1.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/447"), QueryCacheTest]
		public void LargeOrPredicateInSelectManyIsServedFromQueryCache([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Child.Where(c => c.ChildID > 1 || c.ChildID > 0);

			var array = Enumerable.Range(0, 3000).ToArray();

			// Build "where" conditions
			var                                  param      = Expression.Parameter(typeof(Model.Child));
			Expression<Func<Model.Child, bool>>? predicate1 = null;
			Expression<Func<Model.Child, bool>>? predicate2 = null;

			for (var i = 0; i < array.Length; i++)
			{
				var id = array[i];

				var filterExpression = Expression.Lambda<Func<Model.Child, bool>>(
					Expression.Equal(
						Expression.Convert(Expression.PropertyOrField(param, "ChildID"), typeof(int)),
						Expression.Constant(id)
					),
					param
				);

				predicate1 = predicate1 != null ? Or(predicate1, filterExpression) : filterExpression;
				predicate2 = predicate2 != null ? Or(predicate2, filterExpression) : filterExpression;
			}

			var result1 = result.Where(predicate1!);

			var combined1 =
					from r in result1
					from r1 in result1
					select r1;

			var result2 = result.Where(predicate2!);

			var combined2 =
					from r in result2
					from r1 in result2
					select r1;

			// StackOverflowException cannot be handled and will terminate process
			_ = combined1.ToSqlQuery().Sql;

			var cacheMiss = combined1.GetCacheMissCount();

			// from cache
			_ = combined2.ToSqlQuery().Sql;

			combined2.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		private static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke(expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>(Expression.Or(expr1.Body, invokedExpr), expr1.Parameters);
		}
	}
}
