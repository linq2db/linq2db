using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ExpressionsTests : TestBase
	{
		[Sql.Expression("{0} << {1}", Precedence = Precedence.Primary)]
		[Sql.Expression(ProviderName.ClickHouse, "bitShiftLeft({0}, {1})", Precedence = Precedence.Primary)]
		private static long Shl(long v, int s) => v << s;

		[Sql.Expression("{0} >> {1}", Precedence = Precedence.Primary)]
		[Sql.Expression(ProviderName.ClickHouse, "bitShiftRight({0}, {1})", Precedence = Precedence.Primary)]
		private static long Shr(long v, int s) => v >> s;

		static ExpressionsTests()
		{
			Expressions.MapBinary((long v, int s) => v << s, (v, s) => Shl(v, s));
			Expressions.MapBinary((long v, int s) => v >> s, (v, s) => Shr(v, s));
			Expressions.MapBinary((int  v, int s) => v << s, (v, s) => Shl(v, s));
			Expressions.MapBinary((int  v, int s) => v >> s, (v, s) => Shr(v, s));
		}

		[Test]
		public void MapOperator([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var query = from p in db.Parent
						where p.ParentID >> 1 > 0
						select p;

			var expected = from p in Parent
						   where p.ParentID >> 1 > 0
						   select p;

			AreEqual(expected, query);
		}

		[Flags]
		public enum FlagsEnum
		{
			None = 0,

			Flag1 = 0x1,
			Flag2 = 0x2,
			Flag3 = 0x4,

			All = Flag1 | Flag2 | Flag3
		}

		[Table]
		sealed class MappingTestClass
		{
			[PrimaryKey] public int   Id    { get; set; }
			[Column] public int       Value { get; set; }
			[Column] public FlagsEnum Flags { get; set; }
		}

		[Test]
		public void MapHasFlag([DataSources] string context, [Values(FlagsEnum.Flag1, FlagsEnum.Flag3, FlagsEnum.All)] FlagsEnum flag)
		{
			var data = Enumerable.Range(1, 10).Select(i => new MappingTestClass
				{
					Id = i,
					Value = i * 10,
					Flags = (FlagsEnum)(i & (int)FlagsEnum.All)
				})
				.ToArray();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query = from t in table
						where t.Flags.HasFlag(flag)
						select t;

			var expected = from t in data
						   where t.Flags.HasFlag(flag)
						   select t;

			AreEqualWithComparer(expected, query);
		}

		[Flags]
		public enum StringMappedFlagsEnum
		{
			[MapValue("F1")] Flag1 = 0x1,
			[MapValue("F2")] Flag2 = 0x2,
			[MapValue("F3")] Flag3 = 0x4,
		}

		[Table]
		sealed class StringMappedFlagsTable
		{
			[Column] public int                   Id    { get; set; }
			[Column(DataType = DataType.NVarChar, Length = 10)]
			public StringMappedFlagsEnum          Flags { get; set; }
		}

		[Test]
		public void HasFlag_OnStringMappedEnum_FailsToTranslate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<StringMappedFlagsTable>();

			var query = from t in table
						where t.Flags.HasFlag(StringMappedFlagsEnum.Flag1)
						select t;

			Assert.That(() => query.ToArray(), Throws.TypeOf<LinqToDBException>());
		}

		[Flags]
		public enum ConverterMappedFlagsEnum
		{
			Flag1 = 0x1,
			Flag2 = 0x2,
			Flag3 = 0x4,
		}

		sealed class FlagsEnumToStringConverter : ValueConverter<ConverterMappedFlagsEnum, string>
		{
			public FlagsEnumToStringConverter() : base(
				v => v.ToString(),
				s => Enum.Parse<ConverterMappedFlagsEnum>(s),
				handlesNulls: true)
			{
			}
		}

		[Table]
		sealed class ConverterMappedFlagsTable
		{
			[Column] public int Id { get; set; }

			[Column(DataType = DataType.NVarChar, Length = 30)]
			[ValueConverter(ConverterType = typeof(FlagsEnumToStringConverter))]
			public ConverterMappedFlagsEnum Flags { get; set; }
		}

		[Test]
		public void HasFlag_OnConverterMappedEnum_FailsToTranslate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<ConverterMappedFlagsTable>();

			var query = from t in table
						where t.Flags.HasFlag(ConverterMappedFlagsEnum.Flag1)
						select t;

			Assert.That(() => query.ToArray(), Throws.TypeOf<LinqToDBException>());
		}

		[Test]
		public void HasFlag_OnConverterMappedEnum_ByColumn_FailsToTranslate([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<ConverterMappedFlagsTable>();

			var value = ConverterMappedFlagsEnum.Flag1;

			var query = from t in table
						where value.HasFlag(t.Flags)
						select t;

			Assert.That(() => query.ToArray(), Throws.TypeOf<LinqToDBException>());
		}

		static int Count1(Parent p) { return p.Children.Count(c => c.ChildID > 0); }

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void MapMember1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			Expressions.MapMember<Parent,int>(nameof(MapMember1), p => Count1(p), p => p.Children.Count(c => c.ChildID > 0));

			using var db = GetDataContext(context, new MappingSchema(nameof(MapMember1)));
			AreEqual(Parent.Select(Count1), db.Parent.Select(p => Count1(p)));
		}

		static int Count2(Parent p, int id) { return p.Children.Count(c => c.ChildID > id); }

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void MapMember2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			Expressions.MapMember<Parent,int,int>(nameof(MapMember2), (p,id) => Count2(p, id), (p, id) => p.Children.Count(c => c.ChildID > id));

			using var db = GetDataContext(context, new MappingSchema(nameof(MapMember2)));
			AreEqual(Parent.Select(p => Count2(p, 1)), db.Parent.Select(p => Count2(p, 1)));
		}

		static int Count3(Parent p, int id) { return p.Children.Count(c => c.ChildID > id) + 2; }

		[Test]
		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		public void MapMember3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			Expressions.MapMember<Parent,int,int>(nameof(MapMember3), (p,id) => Count3(p, id), (p, id) => p.Children.Count(c => c.ChildID > id) + 2);

			var n = 2;

			using var db = GetDataContext(context, new MappingSchema(nameof(MapMember3)));
			AreEqual(Parent.Select(p => Count3(p, n)), db.Parent.Select(p => Count3(p, n)));
		}

		class MapMemberBaseCalculator
		{
			protected readonly int Offset = 100;

			public long Calc(long value) => value + Offset;
		}

		sealed class MapMemberDerivedCalculator : MapMemberBaseCalculator
		{
			public int Calc(int value) => value + Offset;
		}

		[Test]
		public void MapMemberOnBaseOverloadWithWiderParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			Expressions.MapMember<MapMemberBaseCalculator,long,long>(nameof(MapMemberOnBaseOverloadWithWiderParameter), (c, v) => c.Calc(v), (c, v) => v + 100);

			var calculator = new MapMemberDerivedCalculator();

			using var db = GetDataContext(context, new MappingSchema(nameof(MapMemberOnBaseOverloadWithWiderParameter)));

			var ids = db.Parent
				.Where(p => calculator.Calc(p.ParentID) == 101)
				.Select(p => p.ParentID)
				.ToArray();

			ids.ShouldBe([1]);
		}

		abstract class SqlExpressionBaseCalculator
		{
			[Sql.Expression("{0} + 100", ServerSideOnly = true)]
			public static long Calc(long value) => throw new ServerSideOnlyException(nameof(Calc));
		}

		sealed class SqlExpressionDerivedCalculator : SqlExpressionBaseCalculator
		{
			public static int Calc(int value) => throw new ServerSideOnlyException(nameof(Calc));
		}

		[Test]
		public void SqlExpressionOnBaseOverloadWithWiderParameter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			var ids = db.Parent
				.Where(p => SqlExpressionDerivedCalculator.Calc(p.ParentID) == 101)
				.Select(p => p.ParentID)
				.ToArray();

			ids.ShouldBe([1]);
		}

		[ExpressionMethod(nameof(Count4Expression))]
		static int Count4(Parent p, int id, int n)
		{
			return (_count4Expression ??= Count4Expression().CompileExpression())(p, id, n);
		}

		static Func<Parent,int,int,int>? _count4Expression;

		static Expression<Func<Parent,int,int,int>> Count4Expression()
		{
			return (p, id, n) => p.Children.Count(c => c.ChildID > id) + n;
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void MethodExpression4([DataSources] string context)
		{
			var n = 3;

			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(p => Count4(p, n, 4)),
				db.Parent.Select(p => Count4(p, n, 4)));
		}

		[ExpressionMethod(nameof(Count5Expression))]
		static int Count5(ITestDataContext db, Parent p, int n)
		{
			return (_count5Expression ??= Count5Expression().CompileExpression())(db, p, n);
		}

		static Func<ITestDataContext,Parent,int,int>? _count5Expression;

		static Expression<Func<ITestDataContext,Parent,int,int>> Count5Expression()
		{
			return (db, p, n) => Sql.AsSql(db.Child.Where(c => c.ParentID == p.ParentID).Count() + n);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void MethodExpression5([DataSources(ProviderName.SqlCe)] string context, [Values(1, 2) ]int n)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count() + n),
				db.Parent.Select(p => Count5(db, p, n)));
		}

		[ExpressionMethod(nameof(Count6Expression))]
		static int Count6(ITable<Child> c, Parent p)
		{
			return (_count6Expression ??= Count6Expression().CompileExpression())(c, p);
		}

		static Func<ITable<Child>,Parent,int>? _count6Expression;

		static Expression<Func<ITable<Child>,Parent,int>> Count6Expression()
		{
			return (ch, p) => ch.Where(c => c.ParentID == p.ParentID).Count();
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void MethodExpression6([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count()),
				db.Parent.Select(p => Count6(db.Child, p)));
		}

		[ExpressionMethod(nameof(Count7Expression))]
		static int Count7(ITable<Child> ch, Parent p, int n)
		{
			return (_count7Expression ??= Count7Expression().CompileExpression())(ch, p, n);
		}

		static Func<ITable<Child>,Parent,int,int>? _count7Expression;

		static Expression<Func<ITable<Child>,Parent,int,int>> Count7Expression()
		{
			return (ch, p, n) => Sql.AsSql(ch.Where(c => c.ParentID == p.ParentID).Count() + n);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void MethodExpression7([DataSources(ProviderName.SqlCe)] string context)
		{
			var n = 2;

			using var db = GetDataContext(context);
			AreEqual(
				   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count() + n),
				db.Parent.Select(p => Count7(db.Child, p, n)));
		}

		[ExpressionMethod(nameof(Expression8))]
		static IQueryable<Parent> GetParent(ITestDataContext db, Child ch)
		{
			throw new ServerSideOnlyException(nameof(GetParent));
		}

		static Expression<Func<ITestDataContext, Child, IQueryable<Parent>>> Expression8()
		{
			return (db, ch) =>
				from p in db.Parent
				where p.ParentID == (int)Math.Floor(ch.ChildID / 10.0)
				select p;
		}

		[Test]
		public void MethodExpression8([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				from p in
					from p in Parent
					where p.ParentID == ch.ChildID / 10
					select p
				where ch.ParentID == p.ParentID
				select ch
				,
				from ch in db.Child
				from p in GetParent(db, ch)
				where ch.ParentID == p.ParentID
				select ch);
		}

		[Test]
		public void MethodExpression9([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from ch in Child
				from p in
					from p in Parent
					where p.ParentID == ch.ChildID / 10
					select p
				where ch.ParentID == p.ParentID
				select ch
				,
				from ch in db.Child
				from p in TestDataConnection.GetParent9(db, ch)
				where ch.ParentID == p.ParentID
				select ch);
		}

		[Test]
		public void MethodExpression10([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			AreEqual(
				from ch in Child
				from p in
					from p in Parent
					where p.ParentID == ch.ChildID / 10
					select p
				where ch.ParentID == p.ParentID
				select ch
				,
				from ch in db.Child
				from p in db.GetParent10(ch)
				where ch.ParentID == p.ParentID
				select ch);
		}

		[ExpressionMethod("GetBoolExpression1")]
		static bool GetBool1<T>(T obj)
		{
			throw new ServerSideOnlyException(nameof(GetBool1));
		}

		static Expression<Func<T,bool>> GetBoolExpression1<T>()
			where T : class
		{
			return obj => obj != null;
		}

		[Test]
		public void TestGenerics1()
		{
			using var db = new TestDataConnection();
			var q =
					from ch in db.Child
					where GetBool1(ch.Parent)
					select ch;

			var _ = q.ToList();
		}

		[ExpressionMethod("GetBoolExpression2_{0}")]
		static bool GetBool2<T>(T obj)
		{
			throw new ServerSideOnlyException(nameof(GetBool2));
		}

		static Expression<Func<Parent,bool>> GetBoolExpression2_Parent()
		{
			return obj => obj != null;
		}

		[Test]
		public void TestGenerics2()
		{
			using var db = new TestDataConnection();
			var q =
					from ch in db.Child
					where GetBool2(ch.Parent)
					select ch;

			var _ = q.ToList();
		}

		sealed class TestClass<T>
		{
			[ExpressionMethod(nameof(GetBoolExpression3))]
			public static bool GetBool3(Parent? obj)
			{
				throw new ServerSideOnlyException(nameof(GetBool3));
			}

			static Expression<Func<Parent?,bool>> GetBoolExpression3()
			{
				return obj => obj != null;
			}
		}

		[Test]
		public void TestGenerics3()
		{
			using var db = new TestDataConnection();
			var q =
					from ch in db.Child
					where TestClass<int>.GetBool3(ch.Parent)
					select ch;

			q.ToList();
		}

		[ExpressionMethod(nameof(AssociationExpression))]
		static IEnumerable<GrandChild> GrandChildren(Parent p)
		{
			throw new ServerSideOnlyException(nameof(GrandChildren));
		}

		static Expression<Func<Parent,IEnumerable<GrandChild>>> AssociationExpression()
		{
			return parent => parent.Children.SelectMany(gc => gc.GrandChildren);
		}

		[Test]
		[ThrowsRequiresCorrelatedSubquery]
		public void AssociationMethodExpression([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in Parent
				select p.Children.SelectMany(gc => gc.GrandChildren).Count()
				,
				from p in db.Parent
				select GrandChildren(p).Count());
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test]
		public async Task AssociationMethodExpressionAsync([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var _ = await db.Parent.ToListAsync();

			AreEqual(
				from p in Parent
				select p.Children.SelectMany(gc => gc.GrandChildren).Count()
				,
				await (
					from p in db.Parent
					select GrandChildren(p).Count()
				).ToListAsync());
		}

		[Test]
		public void ParameterlessExpression()
		{
			using var db = new TestDataConnection();
			var parameter = Expression.Parameter(typeof(Parent));
			var selector  = Expression.Lambda(parameter, parameter);
			var table     = db.Parent;
			var exp       = Expression.Call(
					typeof(Queryable),
					"Select",
					new [] { typeof(Parent), typeof(Parent) },
					table.Expression,
					selector);

			var res = table.Provider.CreateQuery<Parent>(exp);

			foreach (var parent in res)
			{
			}
		}

//		[ExpressionMethod("AssociationExpression")]
//		static IEnumerable<GrandChild> GrandChildren(Parent p)
//		{
//			throw new InvalidOperationException();
//		}
//
//		static Expression<Func<Parent,IEnumerable<GrandChild>>> AssociationExpression()
//		{
//			return parent => parent.Children.SelectMany(gc => gc.GrandChildren);
//		}

		[ExpressionMethod(nameof(MyWhereImpl))]
		static IQueryable<TSource> MyWhere<TSource>(IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate)
		{
			return source.Where(predicate);
		}

		static Expression<Func<IQueryable<TSource>,Expression<Func<TSource,bool>>,IQueryable<TSource>>> MyWhereImpl<TSource>()
		{
			return (source, predicate) => source.Where(predicate);
		}

		[Test]
		public void PredicateExpressionTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var _ = MyWhere(db.Parent, p => p.ParentID == 1).ToList();
		}

		[Test]
		public void PredicateExpressionTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var _ = (
					from c in db.Child
					from p in MyWhere(db.Parent, p => p.ParentID == c.ParentID)
					select p
				).ToList();
		}

		[Test]
		public void LeftJoinTest1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var _ = db.Child.LeftJoin(db.Parent, c => c.ParentID, p => p.ParentID).ToList();
		}

		[ThrowsRequiresCorrelatedSubquery(simple: true)]
		[Test]
		public void LeftJoinTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var _ = (
					from g in db.GrandChild
					where db.Child.LeftJoin(db.Parent, c => c.ParentID, p => p.ParentID).Any(t => t.Outer.ChildID == g.ChildID)
					select g
				).ToList();
		}

		[Test]
		public void ToLowerInvariantTest([DataSources] string context)
		{
			Expressions.MapMember(nameof(ToLowerInvariantTest), (string s) => s.ToLowerInvariant(), s => s.ToLower());

			using var db = GetDataContext(context, new MappingSchema(nameof(ToLowerInvariantTest)));
			AreEqual(
				   Doctor.Where(p => p.Taxonomy.ToLowerInvariant() == "psychiatry").Select(p => p.Taxonomy.ToLower()),
				db.Doctor.Where(p => p.Taxonomy.ToLowerInvariant() == "psychiatry").Select(p => p.Taxonomy.ToLower()));
		}

		/*
		[Test, DataContextSource]
		public void LeftJoinTest3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from g in db.GrandChild
					where db.Child.LeftJoin(db.Parent, c => c.ParentID, p => p.ParentID, (c,p) => c).Any(t => t.ChildID == g.ChildID)
					select g
				).ToList();
			}
		}
		*/

		[Test]
		public void AssociationTest([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.SelectMany(p => p.Children.SelectMany(c => c.GrandChildren)),
				db.Parent.SelectMany(p => p.GrandChildren2));
		}

		[Test]
		public void AssociationTest2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				   Parent.SelectMany(p => p.Children.Where(c => c.ChildID == 22).SelectMany(c => c.GrandChildren)),
				db.Parent.SelectMany(p => p.GrandChildrenByID(22)));
		}

		[ExpressionMethod(nameof(WrapExpression))]
		private static T Wrap<T>(T value)
		{
			return value;
		}

		private static Expression<Func<T, T>> WrapExpression<T>()
		{
			return value => value;
		}

		[Test(Description = "InvalidOperationException : Code supposed to be unreachable")]
		public void ExpressionCompilerCrash([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => Wrap<IList<int>>(new int[] { 1, 2, 3 }).Contains(p.ID)).ToList();
		}

		[Test]
		public void ExpressionCompilerNoCrash([DataSources] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => Wrap<int[]>(new int[] { 1, 2, 3 }).Contains(p.ID)).ToList();
		}

		[Test]
		public void CompareWithNullCheck1([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// NULL == NULL
			Assert.That(db.Parent
				.Any(p => p.ParentID == 2 && p.Value1 == Noop(FirstIfNullOrSecondAsNumber(null, "-1"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck2([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// NULL == NULL
			Assert.That(db.Parent
				.Any(p => p.ParentID == 2 && Noop(FirstIfNullOrSecondAsNumber(null, "-1")) == p.Value1), Is.True);
		}

		[Test]
		public void CompareWithNullCheck3([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 3 == 3
			Assert.That(db.Parent
				.Any(p => p.ParentID == 3 && p.Value1 == Noop(FirstIfNullOrSecondAsNumber("", "3"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck4([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 3 == 3
			Assert.That(db.Parent
				.Any(p => p.ParentID == 3 && Noop(FirstIfNullOrSecondAsNumber("", "3")) == p.Value1), Is.True);
		}

		[Test]
		public void CompareWithNullCheck5([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 3 != NULL
			Assert.That(db.Parent
				.Any(p => p.ParentID == 3 && p.Value1 != Noop(FirstIfNullOrSecondAsNumber(null, "-1"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck6([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// NULL != 3
			Assert.That(db.Parent
				.Any(p => p.ParentID == 3 && Noop(FirstIfNullOrSecondAsNumber(null, "-1")) != p.Value1), Is.True);
		}

		[Test]
		public void CompareWithNullCheck7([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// NULL != 4
			Assert.That(db.Parent
				.Any(p => p.ParentID == 2 && p.Value1 != Noop(FirstIfNullOrSecondAsNumber("4", "4"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck8([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 4 != NULL
			Assert.That(db.Parent
				.Any(p => p.ParentID == 2 && Noop(FirstIfNullOrSecondAsNumber("4", "4")) != p.Value1), Is.True);
		}

		[Test]
		public void CompareWithNullCheck9([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 5 != 6
			Assert.That(db.Parent
				.Any(p => p.ParentID == 5 && p.Value1 != Noop(FirstIfNullOrSecondAsNumber("not5", "6"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck10([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 6 != 5
			Assert.That(db.Parent
				.Any(p => p.ParentID == 5 && Noop(FirstIfNullOrSecondAsNumber("not5", "6")) != p.Value1), Is.True);
		}

		[Test]
		public void CompareWithNullCheck21([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			// NULL == NULL
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 1 && p.intDataType == Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "-1"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck22([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			// NULL == NULL
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 1 && Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "-1")) == p.intDataType), Is.True);
		}

		[Test]
		public void CompareWithNullCheck23([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 7777777 == 7777777
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 2 && p.intDataType == Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "7777777"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck24([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 7777777 == 7777777
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 2 && Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "7777777")) == p.intDataType), Is.True);
		}

		[Test]
		public void CompareWithNullCheck25([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 7777777 != NULL
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 2 && p.intDataType != Noop(FirstIfNullOrSecondAsNumber(p.char20DataType, "1"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck26([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// NULL != 7777777
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 2 && Noop(FirstIfNullOrSecondAsNumber(p.char20DataType, "1")) != p.intDataType), Is.True);
		}

		[Test]
		public void CompareWithNullCheck27([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 7777777 != 1
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 2 && p.intDataType != Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "1"))), Is.True);
		}

		[Test]
		public void CompareWithNullCheck28([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			// 1 != 7777777
			Assert.That(db.GetTable<AllTypes>()
				.Any(p => p.ID == 2 && Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "1")) != p.intDataType), Is.True);
		}

		[LinqToDB.Mapping.Table("AllTypes")]
		sealed class AllTypes
		{
			[LinqToDB.Mapping.Column] public int     ID              { get; set; }
			[LinqToDB.Mapping.Column] public int?    intDataType     { get; set; }
			[LinqToDB.Mapping.Column] public string? varcharDataType { get; set; }
			[LinqToDB.Mapping.Column] public string? char20DataType  { get; set; }
		}

		[Sql.Expression("COALESCE({0}, {0})", ServerSideOnly = true)]
		private static int? Noop(int? value)
		{
			throw new ServerSideOnlyException(nameof(Noop));
		}

		[ExpressionMethod(nameof(Func2Expr))]
		private static int? FirstIfNullOrSecondAsNumber(string? value, string intValue)
		{
			throw new ServerSideOnlyException(nameof(FirstIfNullOrSecondAsNumber));
		}

		private static Expression<Func<string, string, int?>> Func2Expr()
		{
			return (value, intValue) => Func3(value, intValue);
		}

		[Sql.Expression("CASE WHEN {0} IS NULL THEN NULL ELSE CAST({1} AS INT) END", ServerSideOnly = true)]
		private static int? Func3(string value, string intValue)
		{
			throw new ServerSideOnlyException(nameof(Func3));
		}

		[Test]
		public void NullableNullValueTest1([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => p.ID != GetTernaryExpressionValue1(null)).ToList();
		}

		[Test]
		public void NullableNullValueTest2([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => p.ID != GetTernaryExpressionValue2(null)).ToList();
		}

		[Test]
		public void NullableNullValueTest3([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => p.ID != GetTernaryExpressionValue3(null)).ToList();
		}

		[Test]
		public void NullableNullValueTest4([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => p.ID != GetTernaryExpressionValue4(null)).ToList();
		}

		[Test]
		public void NullableNullValueTest5([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Where(p => p.ID != GetTernaryExpressionValue5(null)).ToList();
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue1Expr))]
		private static int? GetTernaryExpressionValue1(int? value)
		{
			throw new ServerSideOnlyException(nameof(GetTernaryExpressionValue1));
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue1Expr()
		{
			// null.Value
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction(value.Value, int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue2Expr))]
		private static int? GetTernaryExpressionValue2(int? value)
		{
			throw new ServerSideOnlyException(nameof(GetTernaryExpressionValue2));
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue2Expr()
		{
			// (int)null
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction((int)value, int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue3Expr))]
		private static int? GetTernaryExpressionValue3(int? value)
		{
			throw new ServerSideOnlyException(nameof(GetTernaryExpressionValue3));
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue3Expr()
		{
			// null.GetValueOrDefault()
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction(value.GetValueOrDefault(), int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue4Expr))]
		private static int? GetTernaryExpressionValue4(int? value)
		{
			throw new ServerSideOnlyException(nameof(GetTernaryExpressionValue4));
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue4Expr()
		{
			// null.GetValueOrDefault(0)
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction(value.GetValueOrDefault(0), int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue5Expr))]
		private static int? GetTernaryExpressionValue5(int? value)
		{
			throw new ServerSideOnlyException(nameof(GetTernaryExpressionValue5));
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue5Expr()
		{
			// this actually works
			// null.HasValue
			return value => value.HasValue ? null : (int?)GetTernaryExpressionValueFunction(1, int.MaxValue);
		}

		[Sql.Function("COALESCE", ServerSideOnly = true)]
		private static int GetTernaryExpressionValueFunction(int value, int defaultValue)
		{
			throw new ServerSideOnlyException(nameof(GetTernaryExpressionValueFunction));
		}

		[Table]
		sealed class JsonColumnTable
		{
			[Column] public int Id;
			[Column(DataType = DataType.NVarChar)] public JsonType? Json;

			public static readonly JsonColumnTable[] Data = new []
			{
				new JsonColumnTable() { Id = 1 },
				new JsonColumnTable() { Id = 2 },
				new JsonColumnTable() { Id = 3 }
			};

			public sealed class JsonType
			{
				public string? Text;
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2431")]
		public void ExpressionMethodGenericJsonPathInConditional([IncludeDataSources(true, TestProvName.AllPostgreSQL93Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(JsonColumnTable.Data);
			db.GetTable<JsonColumnTable>().Where(r => JsonExtractPathText(r.Json, json => json!.Text) == "test" ? true : false).ToList();
		}

		[ExpressionMethod(nameof(JsonExtractPathExpression))]
		private static TJsonProp JsonExtractPathText<TColumn, TJsonProp>(
			TColumn field,
			Expression<Func<TColumn, TJsonProp>> path)
			=> throw new ServerSideOnlyException(nameof(JsonExtractPathText));

		private static Expression<Func<TColumn, Expression<Func<TColumn, TJsonProp>>, TJsonProp>>
			JsonExtractPathExpression<TColumn, TJsonProp>()
		{
			return (column, jsonProp) => JsonExtractPathText<TColumn, TJsonProp>(column, Sql.Expr<string>(JsonPath(jsonProp)));
		}

		[Sql.Expression("{0}::json #>> {1}", ServerSideOnly = true, IsPredicate = true)]
		private static TJsonProp JsonExtractPathText<TColumn, TJsonProp>(TColumn left, string right)
			=> throw new ServerSideOnlyException(nameof(JsonExtractPathText));

		private static string JsonPath<TColumn, TJsonProp>(Expression<Func<TColumn, TJsonProp>> extractor) => "'{json, text}'";

		[Table]
		sealed class FullNameColumnTable
		{
			[PrimaryKey] public int     Id;
			[Column] public string? FirstName;
			[Column] public string? LastName;

			[ExpressionMethod(nameof(FullNameExpr), IsColumn = true)]
			public string? FullName { get; set; }

			private static Expression<Func<FullNameColumnTable, string>> FullNameExpr() => t => $"{t.FirstName} {t.LastName}";
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2434")]
		public void OrderByExpressionMethodColumn([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<FullNameColumnTable>();
			tb.OrderBy(x => x.FullName).ToArray();
		}

		[Table]
		public class PersonsCountDcTable
		{
			[PrimaryKey] public int Id { get; set; }

			[ExpressionMethod(nameof(PersonsCountExpr), IsColumn = true)]
			public int PersonsCount { get; set; }

			private static Expression<Func<PersonsCountDcTable, DataConnection, int>> PersonsCountExpr() => (r, db) => db.GetTable<Person>().Where(p => p.ID == r.Id).Count();
		}

		[Table(nameof(PersonsCountDcTable))]
		public class PersonsCountDcTxTable
		{
			[PrimaryKey] public int Id { get; set; }

			[ExpressionMethod(nameof(PersonsCountExpr), IsColumn = true)]
			public int PersonsCount { get; set; }

			private static Expression<Func<PersonsCountDcTxTable, DataContext, int>> PersonsCountExpr() => (r, db) => db.GetTable<Person>().Where(p => p.ID == r.Id).Count();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3472")]
		[ThrowsRequiresCorrelatedSubquery]
		public void ExpressionMethodColumnWithContextParameter([DataSources] string context)
		{
			using var db = GetDataContext(context);
			if (db is DataConnection)
			{
				using var tb = db.CreateLocalTable(new[] { new PersonsCountDcTable() { Id = 1 } });
				tb.ToArray();
			}
			else
			{
				using var tb = db.CreateLocalTable(new[] { new PersonsCountDcTxTable() { Id = 1 } });
				tb.ToArray();
			}
		}

		#region Null check generated

		[Test]
		public void TestNullCheckInExpressionLeft([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Any(p => p.ID == Function2(Function1Left(null)));
		}

		[Test]
		public void TestNullCheckInExpressionRight([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			db.Person.Any(p => p.ID == Function2(Function1Right(null)));
		}

		[Test]
		public void TestNullCheckInExpressionUsingFieldLeft([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			db.Parent.Where(p => p.Value1 == null).Any(p => p.ParentID == Function2(Function1Left(p.Value1)));
		}

		[Test]
		public void TestNullCheckInExpressionUsingFieldRight([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			db.Parent.Where(p => p.Value1 == null).Any(p => p.ParentID == Function2(Function1Right(p.Value1)));
		}

		[Sql.Expression("{0}", ServerSideOnly = true, IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		private static int? Function2(int? Value) => throw new ServerSideOnlyException(nameof(Function2));

		[ExpressionMethod(nameof(Function1LeftExpr))]
		private static int? Function1Left(int? value) => throw new ServerSideOnlyException(nameof(Function1Left));

		[ExpressionMethod(nameof(Function1RightExpr))]
		private static int? Function1Right(int? value) => throw new ServerSideOnlyException(nameof(Function1Right));

		[Sql.Expression("CAST(N'SHOULD NOT BE CALLED' AS INT)", ServerSideOnly = true)]
		private static int Fail(int value) => throw new ServerSideOnlyException(nameof(Fail));

		private static Expression<Func<int?, int?>> Function1LeftExpr()
		{
			return value => value == null ? null : Fail(value.Value);
		}

		private static Expression<Func<int?, int?>> Function1RightExpr()
		{
			return value => value != null ? Fail(value.Value) : null;
		}

		#endregion

		#region Regression: query comparison
		[Test(Description = "Tests regression introduced in 3.5.2"), QueryCacheTest]
		public void ComparisonTest1([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);
			var left  = GetQuery(db, 0);
			var right = GetQuery(db, 2);

			var cacheMiss = db.Patient.GetCacheMissCount();

			Assert.That(
				db.Person.Where(_ =>
				left.Where(rec => !right.Select(r2 => r2.PersonID).Contains(rec.PersonID)).Select(_ => Sql.Ext.Count(_.PersonID, Sql.AggregateModifier.None).ToValue()).Single() == 0
				&&
				right.Where(rec => !left.Select(r2 => r2.PersonID).Contains(rec.PersonID)).Select(_ => Sql.Ext.Count(_.PersonID, Sql.AggregateModifier.None).ToValue()).Single() == 0)
				.Any(), Is.False);

			if (iteration > 1)
				db.Patient.GetCacheMissCount().ShouldBe(cacheMiss);
		}

		[Test(Description = "Tests regression introduced in 3.5.2")]
		public void ComparisonTest2([DataSources(TestProvName.AllNativeAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var left  = GetQuery(db, 0);
			var right = GetQuery(db, 2);

			Assert.That(
				db.Person.Where(_ =>
				left.Where(rec => !right.Select(r2 => r2.PersonID).Contains(rec.PersonID)).Count() == 0
				&&
				right.Where(rec => !left.Select(r2 => r2.PersonID).Contains(rec.PersonID)).Count() == 0)
				.Any(), Is.False);
		}

		private static IQueryable<Patient> GetQuery(ITestDataContext db, int? personId)
		{
			return db.Patient.Where(_ => _.PersonID == personId);
		}

		#endregion

		[Table]
		class ProjectionServiceTable
		{
			[PrimaryKey] public int IdContract { get; set; }
		}

		[Table]
		class ProjectionContractTable
		{
			[PrimaryKey] public int Id { get; set; }
		}

		class ServiceProjection
		{
			public int IdContract { get; set; }
		}

		static class ServiceProjectionExpressions
		{
			[ExpressionMethod(nameof(ToServiceProjectionExpr))]
			public static ServiceProjection ToServiceProjection(ProjectionServiceTable serv)
				=> throw new ServerSideOnlyException(nameof(ToServiceProjection));

			static Expression<Func<ProjectionServiceTable, ServiceProjection>> ToServiceProjectionExpr()
				=> (serv) => new ServiceProjection { IdContract = serv.IdContract };
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4613")]
		public void ExpressionMethodProjectionInJoin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<ProjectionServiceTable>();
			using var t2 = db.CreateLocalTable<ProjectionContractTable>();

			var query = (
				from servProj in (
				from serv in db.GetTable<ProjectionServiceTable>()
				select ServiceProjectionExpressions.ToServiceProjection(serv))
				join contract in db.GetTable<ProjectionContractTable>() on servProj.IdContract equals contract.Id
				select new
				{
					Contract = contract,
					Service = servProj
				});

			query.ToList();
		}

		static class ProtectedEntityFilterModel
		{
			public interface IBaseEntity;

			public interface IEntityWithPermissions : IBaseEntity
			{
				int PermissionObjectId { get; }
			}

			[Table]
			public sealed class User : IBaseEntity
			{
				[Column(SkipOnUpdate = true)] public int Id { get; set; }
				// Comment it and query generation works
				[Column] public string? Username { get; set; }
			}

			[Table]
			public sealed class Permission : IBaseEntity
			{
				[Column(SkipOnUpdate = true)] public int Id { get; set; }
				[Column] public int ObjectId { get; set; }
			}

			[Table, ProtectedEntity]
			public sealed class Account : IEntityWithPermissions
			{
				[Column] public int UserId { get; set; }

				[Association(CanBeNull = false, ThisKey = nameof(UserId), OtherKey = nameof(User.Id))]
				public User User { get; } = null!;

				[ExpressionMethod(nameof(PermissionObjectIdExpression))]
				public int PermissionObjectId => PermissionObjectIdExpression().Compile()(this);

				static Expression<Func<Account, int>> PermissionObjectIdExpression() => x => x.UserId;
			}

			public class ProtectedEntityAttribute()
				: EntityFilterAttribute(typeof(ProtectedEntityAttribute), nameof(Filter))
			{
				private static IQueryable<T> Filter<T>(IQueryable<T> q, IDataContext dbCtx)
					where T : IEntityWithPermissions
					=> q.Where(x => dbCtx
							.GetTable<Permission>()
							.Select(y => y.ObjectId)
							.Contains(x.PermissionObjectId));
			}

			[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
			public class EntityFilterAttribute(Type providerType, string propertyName) : Attribute
			{
				public Type ProviderType { get; } = providerType;
				public string PropertyName { get; } = propertyName;
			}

			internal static class EntityFilterHelper
			{
				public static Func<IQueryable<TEntity>, IDataContext, IQueryable<TEntity>>? GetEntityFilter<TEntity>()
				{
					var filters = FindAllFilterAttributes(typeof(TEntity))
						.Select(GetLambda<TEntity, IDataContext>)
						.ToArray();

					return filters.Any()
						? (q, dbCtx) => filters.Aggregate(q, (currQ, nextFunc) => nextFunc(currQ, dbCtx))
						: null;
				}

				private static List<EntityFilterAttribute> FindAllFilterAttributes(Type entityType)
				{
					var relevantTypes = new HashSet<Type>();
					var processQueue = new Queue<Type>();
					processQueue.Enqueue(entityType);

					while (processQueue.Count > 0)
					{
						var processType = processQueue.Dequeue();

						if (processType.BaseType != null)
						{
							processQueue.Enqueue(processType.BaseType);
						}

						foreach (var iFace in processType.GetInterfaces())
						{
							processQueue.Enqueue(iFace);
						}

						relevantTypes.Add(processType);
					}

					return relevantTypes
						.SelectMany(x => x.GetAttributes<EntityFilterAttribute>(true))
						.ToList();
				}

				private static Func<IQueryable<TEntity>, TContext, IQueryable<TEntity>> GetLambda<TEntity, TContext>(
					EntityFilterAttribute entityFilter
				)
				{
					var methodInfo = entityFilter.ProviderType
						.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						.FirstOrDefault(m => m.Name == entityFilter.PropertyName && m.IsGenericMethodDefinition)
						?? throw new ArgumentException($"Method '{entityFilter.PropertyName}' not found in type '{entityFilter.ProviderType.FullName}' or is not a generic method definition.");

					if (methodInfo.IsGenericMethod)
					{
						methodInfo = methodInfo.MakeGenericMethod(typeof(TEntity));
					}

					var qParam = Expression.Parameter(typeof(IQueryable<TEntity>), "q");
					var dbCtxParam = Expression.Parameter(typeof(TContext), "dbCtx");

					var methodCall = Expression.Call(null, methodInfo, qParam, dbCtxParam);

					var lambda = Expression.Lambda<Func<IQueryable<TEntity>, TContext, IQueryable<TEntity>>>(
						methodCall,
						qParam,
						dbCtxParam);

					return lambda.Compile();
				}
			}

			public static class ModelBuilderExtensions
			{
				public static void ProcessEntity<TEntity>(FluentMappingBuilder builder)
				{
					var filter = EntityFilterHelper.GetEntityFilter<TEntity>();
					if (filter != null)
					{
						builder.Entity<TEntity>().HasQueryFilter(filter);
					}
				}
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5040")]
		public void QueryFilterOverInterfaceExpressionMethodProperty([IncludeDataSources(true, ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			var fb = new FluentMappingBuilder(ms);
			ProtectedEntityFilterModel.ModelBuilderExtensions.ProcessEntity<ProtectedEntityFilterModel.Account>(fb);
			fb.Build();

			using var db = GetDataContext(context, o => o.UseMappingSchema(ms));
			using var t1 = db.CreateLocalTable<ProtectedEntityFilterModel.Account>();
			using var t2 = db.CreateLocalTable<ProtectedEntityFilterModel.Permission>();
			using var t3 = db.CreateLocalTable<ProtectedEntityFilterModel.User>();

			t1.ToList();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1700")]
		public void ExpressionMethodSubqueryUnderOuterApply([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllSQLite)] string context)
		{
			var groupId = 5;

			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<OuterApplyItem>();
			using var t2 = db.CreateLocalTable<OuterApplyItemType>();
			using var t3 = db.CreateLocalTable<OuterApplyAppType>();
			using var t4 = db.CreateLocalTable<OuterApplyAppSubType>();

			var items     = db.GetTable<OuterApplyItem>().AsQueryable();
			var itemTypes = db.GetTable<OuterApplyItemType>().AsQueryable();
			var types     = db.GetTable<OuterApplyAppType>().AsQueryable();
			var subTypes  = db.GetTable<OuterApplyAppSubType>().AsQueryable();

			var data = (
				from item in items.Where(i => i.GroupId == groupId)
				let itemSubTypeDescription = SubTypeSummary(itemTypes, types, subTypes, item)
				select new { item.ItemId, Description1 = item.Description, Description2 = itemSubTypeDescription.Description });

			var all_items = data.ToList();
		}

		[Table]
		class OuterApplyItemType
		{
			[Column] public int AppTypeId { get; set; }
			[Column] public int ItemId { get; set; }
		}

		[Table]
		class OuterApplyItem
		{
			[Column] public int GroupId { get; set; }
			[Column] public int ItemId { get; set; }
			[Column] public string? Description { get; set; }
		}

		[Table]
		class OuterApplyAppType
		{
			[Column] public int AppTypeId { get; set; }
			[Column] public DateTime CreatedDate { get; set; }
		}

		[Table]
		class OuterApplyAppSubType
		{
			[Column] public int AppTypeId { get; set; }
			[Column] public int AppSubTypeId { get; set; }
			[Column] public string? Description { get; set; }
			[Column] public DateTime CreatedDate { get; set; }
		}

		[ExpressionMethod(nameof(SubTypeSummaryImpl))]
		static OuterApplySubTypeSummary SubTypeSummary(IQueryable<OuterApplyItemType> itemTypes, IQueryable<OuterApplyAppType> types, IQueryable<OuterApplyAppSubType> subTypes, OuterApplyItem item)
		{
			throw new ServerSideOnlyException(nameof(SubTypeSummary));
		}

		public class OuterApplySubTypeSummary
		{
			public int AppSubTypeId { get; set; }
			public string? Description { get; set; }
			public DateTime MaxSubtypeCreatedDate { get; set; }
			public DateTime MaxTypeCreatedDate { get; set; }
			public int MaxTypeId { get; set; }
			public int CountDistinctTypeId { get; set; }
			public int CountDistinctSubTypeId { get; set; }
		}

		static Expression<Func<IQueryable<OuterApplyItemType>, IQueryable<OuterApplyAppType>, IQueryable<OuterApplyAppSubType>, OuterApplyItem, OuterApplySubTypeSummary?>> SubTypeSummaryImpl()
		{
			return (itemTypes, types, subTypes, item) => (
					from sub in
						from itemtype in itemTypes
						from type in types.LeftJoin(t => t.AppTypeId == itemtype.AppTypeId)
						from subtype in subTypes.LeftJoin(u => u.AppTypeId == type.AppTypeId)
						where itemtype.ItemId == item.ItemId
							  && type.AppTypeId == itemtype.AppTypeId
							  && subtype.AppTypeId == type.AppTypeId
						select new
						{
							subtype.Description,
							subtype.AppSubTypeId,
							subtypeCreatedDate = subtype.CreatedDate,
							typeCreatedDate = type.CreatedDate,
							type.AppTypeId
						}
					group sub by new { sub.Description, sub.AppSubTypeId }
					into grpby
					select new OuterApplySubTypeSummary
					{
						AppSubTypeId = grpby.Key.AppSubTypeId,
						Description = grpby.Key.Description,
						MaxSubtypeCreatedDate = grpby.Max(i => i.subtypeCreatedDate),
						MaxTypeCreatedDate = grpby.Max(i => i.typeCreatedDate),
						MaxTypeId = grpby.Max(i => i.AppTypeId),
						CountDistinctTypeId = grpby.CountExt(i => i.AppTypeId, Sql.AggregateModifier.Distinct),
						CountDistinctSubTypeId = grpby.CountExt(i => i.AppSubTypeId, Sql.AggregateModifier.Distinct)
					}
				)
				.OrderByDescending(ord1 => ord1.CountDistinctTypeId)
				.ThenByDescending(ord2 => ord2.MaxSubtypeCreatedDate)
				.ThenByDescending(ord3 => ord3.MaxTypeCreatedDate)
				.ThenByDescending(ord4 => ord4.MaxTypeId)
				.FirstOrDefault();
		}

		public class ExpressionMethodCar
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;
		}

		public static class ExpressionMethodCarExtensions
		{
			[ExpressionMethod(nameof(FilterBySpecialStringExpression))]
			public static bool FilterBySpecialString(ExpressionMethodCar car, bool? isSpecial = false)
			{
				return !isSpecial.HasValue || (isSpecial.Value && car.Name == "Special") || (!isSpecial.Value && car.Name != "Special");
			}

			public static Expression<Func<ExpressionMethodCar, bool?, bool>> FilterBySpecialStringExpression()
			{
				return (x, isSpecial) => isSpecial == null || (isSpecial.Value && x.Name == "Special") || (!isSpecial.Value && x.Name != "Special");
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2800")]
		public void ExpressionMethodWithNullableBooleanParameter([DataSources] string context)
		{
			var fluentMappingBuilder = new FluentMappingBuilder(new MappingSchema());

			var carBuilder = fluentMappingBuilder.Entity<ExpressionMethodCar>();
			carBuilder.Property(x => x.Id).IsPrimaryKey();
			carBuilder.Property(x => x.Name).HasLength(50);

			fluentMappingBuilder.Build();

			var records = new ExpressionMethodCar[]
			{
				new ExpressionMethodCar { Id = 1, Name = "Special" },
				new ExpressionMethodCar { Id = 2, Name = "NoSpecial" }
			};

			using var db = GetDataContext(context, fluentMappingBuilder.MappingSchema);
			using var carTable = db.CreateLocalTable<ExpressionMethodCar>(records);
			var isSpecial = (bool?)null;
			AssertQuery(carTable.Where(x => ExpressionMethodCarExtensions.FilterBySpecialString(x, isSpecial)));
			isSpecial = false;
			AssertQuery(carTable.Where(x => ExpressionMethodCarExtensions.FilterBySpecialString(x, isSpecial)));
			isSpecial = true;
			AssertQuery(carTable.Where(x => ExpressionMethodCarExtensions.FilterBySpecialString(x, isSpecial)));

			AssertQuery(carTable.Where(x => ExpressionMethodCarExtensions.FilterBySpecialString(x, null)));
			AssertQuery(carTable.Where(x => ExpressionMethodCarExtensions.FilterBySpecialString(x, false)));
			AssertQuery(carTable.Where(x => ExpressionMethodCarExtensions.FilterBySpecialString(x, true)));
		}

		static class SettingNameIds
		{
			public const string UserNumberMaxValueId = "UserNumberMaxValue";
		}

		class NumberFormatUser
		{
			public int    Id         { get; set; }
			public int    HospitalId { get; set; }
			public string Name       { get; set; } = null!;
			public int    UserNumber { get; set; }

			[Association(ThisKey = nameof(HospitalId), OtherKey = nameof(NumberFormatHospital.Id), CanBeNull = true)]
			public NumberFormatHospital Hospital { get; set; } = null!;
		}

		public class NumberFormatHospital
		{
			public int    Id   { get; set; }
			public string Name { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(NumberFormatHospitalSetting.HospitalId), CanBeNull = true)]
			public List<NumberFormatHospitalSetting> Settings { get; set; } = null!;

			[ExpressionMethod(nameof(GetSettingValueImpl))]
			public string? GetSettingValue(string settingId) => throw new ServerSideOnlyException(nameof(GetSettingValue));

			static Expression<Func<NumberFormatHospital, string, string?>> GetSettingValueImpl()
			{
				return (h, settingId) => h.Settings
					.Where(sv => sv.SettingId == settingId)
					.Select(sv => sv.SettingValue)
					.SingleOrDefault();
			}

			[ExpressionMethod(nameof(GetUserNumberMaxValueImpl))]
			public int GetUserNumberMaxValue() => throw new ServerSideOnlyException(nameof(GetUserNumberMaxValue));

			static Expression<Func<NumberFormatHospital, int>> GetUserNumberMaxValueImpl()
			{
				return h => Sql.ConvertTo<int?>.From(h.GetSettingValue(SettingNameIds.UserNumberMaxValueId)) ?? 9000;
			}
		}

		[ExpressionMethod(nameof(FormatUserFromUserNumberImpl))]
		static string FormatUserNumber(NumberFormatUser user)
		{
			throw new ServerSideOnlyException(nameof(FormatUserNumber));
		}

		static Expression<Func<NumberFormatUser, string>> FormatUserFromUserNumberImpl()
		{
			return u => u.UserNumber >= 0 && u.UserNumber.ToString().Length < u.Hospital.GetUserNumberMaxValue().ToString().Length
				? u.UserNumber.ToString().PadLeft(u.Hospital.GetUserNumberMaxValue().ToString().Length, '0')
				: u.UserNumber.ToString();
		}

		public class NumberFormatHospitalSetting
		{
			public int    Id           { get; set; }
			public int    HospitalId   { get; set; }
			public string SettingId    { get; set; } = null!;
			public string SettingValue { get; set; } = null!;

			[Association(ThisKey = nameof(HospitalId), OtherKey = nameof(NumberFormatHospital.Id), CanBeNull = true)]
			public NumberFormatHospital Hospital { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5283")]
		public void ExpressionMethodOverNestedAssociationInProjectionFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var userTable = db.CreateLocalTable(new[]
			{
				new NumberFormatUser { Id = 1, HospitalId = 1, Name = "User 1", UserNumber = 1 },
				new NumberFormatUser { Id = 2, HospitalId = 1, Name = "User 2", UserNumber = 2 },
				new NumberFormatUser { Id = 3, HospitalId = 1, Name = "User 3", UserNumber = 3 },
				new NumberFormatUser { Id = 4, HospitalId = 2, Name = "User 4", UserNumber = 9001 },
				new NumberFormatUser { Id = 5, HospitalId = 2, Name = "User 5", UserNumber = 9002 },
				new NumberFormatUser { Id = 6, HospitalId = 2, Name = "User 6", UserNumber = 9003 }
			});
			using var hospitalTable = db.CreateLocalTable(new[]
			{
				new NumberFormatHospital { Id = 1, Name = "Hospital 1" },
				new NumberFormatHospital { Id = 2, Name = "Hospital 2" }
			});
			using var hospitalSettingTable = db.CreateLocalTable(new[]
			{
				new NumberFormatHospitalSetting { Id = 1, HospitalId = 1, SettingId = SettingNameIds.UserNumberMaxValueId, SettingValue = "100" },
				new NumberFormatHospitalSetting { Id = 2, HospitalId = 2, SettingId = SettingNameIds.UserNumberMaxValueId, SettingValue = "10000" }
			});

			var searchLexems = new[] { "001", "002", "003", "9001", "9002", "9003" };

			var query =
				from user in db.GetTable<NumberFormatUser>()
				select new
				{
					Data = new
					{
						UserNumberFormatted = FormatUserNumber(user)
					}
				};

			query = query.Where(u => searchLexems.Any(l => l == u.Data.UserNumberFormatted));

			var result = query.ToList();

			result.Count.ShouldBe(3);
		}

		[Table(Name = "Person")]
		public class PersonWithDoctorAssociation //: Person
		{
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID"), Identity, PrimaryKey]
			public int ID;
			[NotNull] public string FirstName { get; set; } = null!;
			[NotNull] public string LastName = null!;
			[Nullable] public string? MiddleName;

			[Association(ThisKey = nameof(ID), OtherKey = nameof(Model.Doctor.PersonID), CanBeNull = true)]
			public Doctor? Doctor { get; set; }
		}

		public class PersonWithDoctorDto
		{
			public int    Id;
			public string Name = null!;

			public DoctorTaxonomyDto? Doc;
		}

		public class DoctorTaxonomyDto
		{
			public int    PersonId;
			public string Taxonomy = null!;
		}

		[ExpressionMethod("MapPersonToDtoExpr")]
		private static PersonWithDoctorDto MapToDto(PersonWithDoctorAssociation person)
		{
			return MapPersonToDtoExpr().CompileExpression()(person);
		}

		[ExpressionMethod("MapDoctorToDtoExpr")]
		private static DoctorTaxonomyDto MapToDto(Doctor doctor)
		{
			return MapDoctorToDtoExpr().CompileExpression()(doctor);
		}

		private static Expression<Func<PersonWithDoctorAssociation, PersonWithDoctorDto>> MapPersonToDtoExpr()
		{
			return x => new PersonWithDoctorDto
			{

				Id   = x.ID,
				Name = x.FirstName,
				Doc  = x.Doctor != null ? MapToDto(x.Doctor) : null
			};
		}

		private static Expression<Func<Doctor, DoctorTaxonomyDto>> MapDoctorToDtoExpr()
		{
			return x => new DoctorTaxonomyDto
			{
				PersonId = x.PersonID,
				Taxonomy = x.Taxonomy
			};
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/376")]
		public void ExpressionMethodMapsAssociationToDto([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var l = db
					.GetTable<PersonWithDoctorAssociation>()
					.Where(_ => _.Doctor!.Taxonomy.Length >= 0 || _.Doctor.Taxonomy == null)
					.Select(_ => MapToDto(_)).ToList();

			Assert.That(l, Is.Not.Empty);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(l.Where(_ => _.Doc == null), Is.Not.Empty);
				Assert.That(l.Where(_ => _.Doc != null), Is.Not.Empty);
			}
		}
	}

	#region Extensions
	static class ExpressionTestExtensions
	{
		public sealed class LeftJoinInfo<TOuter,TInner>
		{
			public TOuter Outer = default!;
			public TInner Inner = default!;
		}

		[ExpressionMethod(nameof(LeftJoinImpl))]
		public static IQueryable<LeftJoinInfo<TOuter,TInner>> LeftJoin<TOuter, TInner, TKey>(
			this IQueryable<TOuter> outer,
			IEnumerable<TInner> inner,
			Expression<Func<TOuter, TKey>> outerKeySelector,
			Expression<Func<TInner, TKey>> innerKeySelector)
		{
			return outer
				.GroupJoin(inner, outerKeySelector, innerKeySelector, (o, gr) => new { o, gr })
				.SelectMany(t => t.gr.DefaultIfEmpty(), (o,i) => new LeftJoinInfo<TOuter,TInner> { Outer = o.o, Inner = i! });
		}

		static Expression<Func<
			IQueryable<TOuter>,
			IEnumerable<TInner>,
			Expression<Func<TOuter,TKey>>,
			Expression<Func<TInner,TKey>>,
			IQueryable<LeftJoinInfo<TOuter,TInner>>>>
			LeftJoinImpl<TOuter, TInner, TKey>()
		{
			return (outer,inner,outerKeySelector,innerKeySelector) => outer
				.GroupJoin(inner, outerKeySelector, innerKeySelector, (o, gr) => new { o, gr })
				.SelectMany(t => t.gr.DefaultIfEmpty(), (o,i) => new LeftJoinInfo<TOuter,TInner> { Outer = o.o, Inner = i! });
		}

		/*
		[ExpressionMethod("LeftJoinImpl1")]
		public static IQueryable<TResult> LeftJoin<TOuter,TInner,TKey,TResult>(
			this IQueryable<TOuter> outer,
			IEnumerable<TInner> inner,
			Expression<Func<TOuter,TKey>> outerKeySelector,
			Expression<Func<TInner,TKey>> innerKeySelector,
			Expression<Func<TOuter,TInner,TResult>> resultSelector)
		{
			return outer
				.GroupJoin(inner, outerKeySelector, innerKeySelector, (o, gr) => new { o, gr })
				.SelectMany(t => t.gr.DefaultIfEmpty(), (o,i) => o, resultSelector);
		}

		static Expression<Func<
			IQueryable<TOuter>,
			IEnumerable<TInner>,
			Expression<Func<TOuter,TKey>>,
			Expression<Func<TInner,TKey>>,
			Expression<Func<TOuter,TInner, TResult>>,
			IQueryable<TResult>>>
			LeftJoinImpl1<TOuter,TInner,TKey,TResult>()
		{
			return (outer,inner,outerKeySelector,innerKeySelector,resultSelector) => outer
				.GroupJoin(inner, outerKeySelector, innerKeySelector, (o, gr) => new { o, gr })
				.SelectMany(t => t.gr.DefaultIfEmpty(), (o,i) => new { o.o, i })
				.Select(resultSelector);
		}
		*/
	}
	#endregion
}
