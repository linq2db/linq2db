using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Shouldly;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

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
			Expressions.MapMember((Enum e, Enum e2) => e.HasFlag(e2),
				(t, flag) => (Sql.ConvertTo<int>.From(t) & Sql.ConvertTo<int>.From(flag)) != 0);
		}

		[Test]
		public void MapOperator([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = from p in db.Parent
					where p.ParentID >> 1 > 0
					select p;

				var expected = from p in Parent
					where p.ParentID >> 1 > 0
					select p;

				AreEqual(expected, query);
			}
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
			[Column] public int       Id    { get; set; }
			[Column] public int       Value { get; set; }
			[Column] public FlagsEnum Flags { get; set; }
		}

		[Test]
		public void MapHasFlag([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values (FlagsEnum.Flag1, FlagsEnum.Flag3)] FlagsEnum flag)
		{
			var data = Enumerable.Range(1, 10).Select(i => new MappingTestClass
				{
					Id = i,
					Value = i * 10,
					Flags = (FlagsEnum)(i & (int)FlagsEnum.All)
				})
				.ToArray();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query = from t in table
					where t.Flags.HasFlag(flag)
					select t;

				var expected = from t in data
					where t.Flags.HasFlag(flag)
					select t;

				AreEqualWithComparer(expected, query);
			}
		}

		static int Count1(Parent p) { return p.Children.Count(c => c.ChildID > 0); }

		[Test]
		public void MapMember1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			Expressions.MapMember<Parent,int>(p => Count1(p), p => p.Children.Count(c => c.ChildID > 0));

			using (var db = GetDataContext(context))
				AreEqual(Parent.Select(Count1), db.Parent.Select(p => Count1(p)));
		}

		static int Count2(Parent p, int id) { return p.Children.Count(c => c.ChildID > id); }

		[Test]
		public void MapMember2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			Expressions.MapMember<Parent,int,int>((p,id) => Count2(p, id), (p, id) => p.Children.Count(c => c.ChildID > id));

			using (var db = GetDataContext(context))
				AreEqual(Parent.Select(p => Count2(p, 1)), db.Parent.Select(p => Count2(p, 1)));
		}

		static int Count3(Parent p, int id) { return p.Children.Count(c => c.ChildID > id) + 2; }

		[Test]
		public void MapMember3([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			Expressions.MapMember<Parent,int,int>((p,id) => Count3(p, id), (p, id) => p.Children.Count(c => c.ChildID > id) + 2);

			var n = 2;

			using (var db = GetDataContext(context))
				AreEqual(Parent.Select(p => Count3(p, n)), db.Parent.Select(p => Count3(p, n)));
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
		public void MethodExpression4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			var n = 3;

			using (var db = GetDataContext(context))
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
		public void MethodExpression5([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context, [Values(1, 2) ]int n)
		{
			using (var db = GetDataContext(context))
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
		public void MethodExpression6([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
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
		public void MethodExpression7([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context)
		{
			var n = 2;

			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count() + n),
					db.Parent.Select(p => Count7(db.Child, p, n)));
		}

		[ExpressionMethod(nameof(Expression8))]
		static IQueryable<Parent> GetParent(ITestDataContext db, Child ch)
		{
			throw new InvalidOperationException();
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
			using (var db = GetDataContext(context))
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
			using (var db = GetDataConnection(context))
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
			using (var db = GetDataConnection(context))
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
			throw new InvalidOperationException();
		}

		static Expression<Func<T,bool>> GetBoolExpression1<T>()
			where T : class
		{
			return obj => obj != null;
		}

		[Test]
		public void TestGenerics1()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from ch in db.Child
					where GetBool1(ch.Parent)
					select ch;

				var _ = q.ToList();
			}
		}

		[ExpressionMethod("GetBoolExpression2_{0}")]
		static bool GetBool2<T>(T obj)
		{
			throw new InvalidOperationException();
		}

		static Expression<Func<Parent,bool>> GetBoolExpression2_Parent()
		{
			return obj => obj != null;
		}

		[Test]
		public void TestGenerics2()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from ch in db.Child
					where GetBool2(ch.Parent)
					select ch;

				var _ = q.ToList();
			}
		}

		sealed class TestClass<T>
		{
			[ExpressionMethod(nameof(GetBoolExpression3))]
			public static bool GetBool3(Parent? obj)
			{
				throw new InvalidOperationException();
			}

			static Expression<Func<Parent?,bool>> GetBoolExpression3()
			{
				return obj => obj != null;
			}
		}

		[Test]
		public void TestGenerics3()
		{
			using (var db = new TestDataConnection())
			{
				var q =
					from ch in db.Child
					where TestClass<int>.GetBool3(ch.Parent)
					select ch;

				q.ToList();
			}
		}

		[ExpressionMethod(nameof(AssociationExpression))]
		static IEnumerable<GrandChild> GrandChildren(Parent p)
		{
			throw new InvalidOperationException();
		}

		static Expression<Func<Parent,IEnumerable<GrandChild>>> AssociationExpression()
		{
			return parent => parent.Children.SelectMany(gc => gc.GrandChildren);
		}

		[Test]
		public void AssociationMethodExpression([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in Parent
					select p.Children.SelectMany(gc => gc.GrandChildren).Count()
					,
					from p in db.Parent
					select GrandChildren(p).Count());
		}

		[Test]
		public async Task AssociationMethodExpressionAsync([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test]
		public void ParameterlessExpression()
		{
			using (var db = new TestDataConnection())
			{
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
			using (var db = GetDataContext(context))
			{
				var _ = MyWhere(db.Parent, p => p.ParentID == 1).ToList();
			}
		}

		[Test]
		public void PredicateExpressionTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var _ = (
					from c in db.Child
					from p in MyWhere(db.Parent, p => p.ParentID == c.ParentID)
					select p
				).ToList();
			}
		}

		[Test]
		public void LeftJoinTest1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var _ = db.Child.LeftJoin(db.Parent, c => c.ParentID, p => p.ParentID).ToList();
			}
		}

		[Test]
		public void LeftJoinTest2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var _ = (
					from g in db.GrandChild
					where db.Child.LeftJoin(db.Parent, c => c.ParentID, p => p.ParentID).Any(t => t.Outer.ChildID == g.ChildID)
					select g
				).ToList();
			}
		}

		[Test]
		public void ToLowerInvariantTest([DataSources] string context)
		{
			Expressions.MapMember((string s) => s.ToLowerInvariant(), s => s.ToLower());

			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Doctor.Where(p => p.Taxonomy.ToLowerInvariant() == "psychiatry").Select(p => p.Taxonomy.ToLower()),
					db.Doctor.Where(p => p.Taxonomy.ToLowerInvariant() == "psychiatry").Select(p => p.Taxonomy.ToLower()));
			}
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
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.SelectMany(p => p.Children.SelectMany(c => c.GrandChildren)),
					db.Parent.SelectMany(p => p.GrandChildren2));
			}
		}

		[Test]
		public void AssociationTest2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					   Parent.SelectMany(p => p.Children.Where(c => c.ChildID == 22).SelectMany(c => c.GrandChildren)),
					db.Parent.SelectMany(p => p.GrandChildrenByID(22)));
			}
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
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => Wrap<IList<int>>(new int[] { 1, 2, 3 }).Contains(p.ID)).ToList();
			}
		}

		[Test]
		public void ExpressionCompilerNoCrash([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => Wrap<int[]>(new int[] { 1, 2, 3 }).Contains(p.ID)).ToList();
			}
		}

		[Test]
		public void CompareWithNullCheck1([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL == NULL
				Assert.That(db.Parent
					.Any(p => p.ParentID == 2 && p.Value1 == Noop(FirstIfNullOrSecondAsNumber(null, "-1"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck2([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL == NULL
				Assert.That(db.Parent
					.Any(p => p.ParentID == 2 && Noop(FirstIfNullOrSecondAsNumber(null, "-1")) == p.Value1), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck3([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 3 == 3
				Assert.That(db.Parent
					.Any(p => p.ParentID == 3 && p.Value1 == Noop(FirstIfNullOrSecondAsNumber("", "3"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck4([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 3 == 3
				Assert.That(db.Parent
					.Any(p => p.ParentID == 3 && Noop(FirstIfNullOrSecondAsNumber("", "3")) == p.Value1), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck5([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 3 != NULL
				Assert.That(db.Parent
					.Any(p => p.ParentID == 3 && p.Value1 != Noop(FirstIfNullOrSecondAsNumber(null, "-1"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck6([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL != 3
				Assert.That(db.Parent
					.Any(p => p.ParentID == 3 && Noop(FirstIfNullOrSecondAsNumber(null, "-1")) != p.Value1), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck7([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL != 4
				Assert.That(db.Parent
					.Any(p => p.ParentID == 2 && p.Value1 != Noop(FirstIfNullOrSecondAsNumber("4", "4"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck8([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 4 != NULL
				Assert.That(db.Parent
					.Any(p => p.ParentID == 2 && Noop(FirstIfNullOrSecondAsNumber("4", "4")) != p.Value1), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck9([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 5 != 6
				Assert.That(db.Parent
					.Any(p => p.ParentID == 5 && p.Value1 != Noop(FirstIfNullOrSecondAsNumber("not5", "6"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck10([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 6 != 5
				Assert.That(db.Parent
					.Any(p => p.ParentID == 5 && Noop(FirstIfNullOrSecondAsNumber("not5", "6")) != p.Value1), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck21([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL == NULL
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 1 && p.intDataType == Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "-1"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck22([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL == NULL
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 1 && Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "-1")) == p.intDataType), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck23([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 7777777 == 7777777
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 2 && p.intDataType == Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "7777777"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck24([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 7777777 == 7777777
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 2 && Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "7777777")) == p.intDataType), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck25([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 7777777 != NULL
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 2 && p.intDataType != Noop(FirstIfNullOrSecondAsNumber(p.char20DataType, "1"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck26([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// NULL != 7777777
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 2 && Noop(FirstIfNullOrSecondAsNumber(p.char20DataType, "1")) != p.intDataType), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck27([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 7777777 != 1
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 2 && p.intDataType != Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "1"))), Is.True);
			}
		}

		[Test]
		public void CompareWithNullCheck28([IncludeDataSources(true, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				// 1 != 7777777
				Assert.That(db.GetTable<AllTypes>()
					.Any(p => p.ID == 2 && Noop(FirstIfNullOrSecondAsNumber(p.varcharDataType, "1")) != p.intDataType), Is.True);
			}
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
			throw new InvalidOperationException();
		}

		[ExpressionMethod(nameof(Func2Expr))]
		private static int? FirstIfNullOrSecondAsNumber(string? value, string intValue)
		{
			throw new InvalidOperationException();
		}

		private static Expression<Func<string, string, int?>> Func2Expr()
		{
			return (value, intValue) => Func3(value, intValue);
		}

		[Sql.Expression("CASE WHEN {0} IS NULL THEN NULL ELSE CAST({1} AS INT) END", ServerSideOnly = true)]
		private static int? Func3(string value, string intValue)
		{
			throw new InvalidOperationException();
		}

		#region issue 2688
		[Test]
		public void NullableNullValueTest1([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => p.ID != GetTernaryExpressionValue1(null)).ToList();
			}
		}

		[Test]
		public void NullableNullValueTest2([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => p.ID != GetTernaryExpressionValue2(null)).ToList();
			}
		}

		[Test]
		public void NullableNullValueTest3([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => p.ID != GetTernaryExpressionValue3(null)).ToList();
			}
		}

		[Test]
		public void NullableNullValueTest4([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => p.ID != GetTernaryExpressionValue4(null)).ToList();
			}
		}

		[Test]
		public void NullableNullValueTest5([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(p => p.ID != GetTernaryExpressionValue5(null)).ToList();
			}
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue1Expr))]
		private static int? GetTernaryExpressionValue1(int? value)
		{
			throw new InvalidOperationException();
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue1Expr()
		{
			// null.Value
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction(value.Value, int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue2Expr))]
		private static int? GetTernaryExpressionValue2(int? value)
		{
			throw new InvalidOperationException();
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue2Expr()
		{
			// (int)null
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction((int)value, int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue3Expr))]
		private static int? GetTernaryExpressionValue3(int? value)
		{
			throw new InvalidOperationException();
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue3Expr()
		{
			// null.GetValueOrDefault()
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction(value.GetValueOrDefault(), int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue4Expr))]
		private static int? GetTernaryExpressionValue4(int? value)
		{
			throw new InvalidOperationException();
		}

		private static Expression<Func<int?, int?>> GetTernaryExpressionValue4Expr()
		{
			// null.GetValueOrDefault(0)
			return value => value == null ? null : (int?)GetTernaryExpressionValueFunction(value.GetValueOrDefault(0), int.MaxValue);
		}

		[ExpressionMethod(nameof(GetTernaryExpressionValue5Expr))]
		private static int? GetTernaryExpressionValue5(int? value)
		{
			throw new InvalidOperationException();
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
			throw new InvalidOperationException();
		}
		#endregion

		#region issue 2431
		[Table]
		sealed class Issue2431Table
		{
			[Column] public int Id;
			[Column(DataType = DataType.NVarChar)] public JsonType? Json;

			public static readonly Issue2431Table[] Data = new []
			{
				new Issue2431Table() { Id = 1 },
				new Issue2431Table() { Id = 2 },
				new Issue2431Table() { Id = 3 }
			};

			public sealed class JsonType
			{
				public string? Text;
			}
		}

		[Test]
		public void Issue2431Test([IncludeDataSources(true, TestProvName.AllPostgreSQL93Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(Issue2431Table.Data))
			{
				db.GetTable<Issue2431Table>().Where(r => JsonExtractPathText(r.Json, json => json!.Text) == "test" ? true : false).ToList();
			}
		}

		[ExpressionMethod(nameof(JsonExtractPathExpression))]
		private static TJsonProp JsonExtractPathText<TColumn, TJsonProp>(
			TColumn field,
			Expression<Func<TColumn, TJsonProp>> path)
			=> throw new InvalidOperationException();

		private static Expression<Func<TColumn, Expression<Func<TColumn, TJsonProp>>, TJsonProp>>
			JsonExtractPathExpression<TColumn, TJsonProp>()
		{
			return (column, jsonProp) => JsonExtractPathText<TColumn, TJsonProp>(column, Sql.Expr<string>(JsonPath(jsonProp)));
		}

		[Sql.Expression("{0}::json #>> {1}", ServerSideOnly = true, IsPredicate = true)]
		private static TJsonProp JsonExtractPathText<TColumn, TJsonProp>(TColumn left, string right)
			=> throw new InvalidOperationException();

		private static string JsonPath<TColumn, TJsonProp>(Expression<Func<TColumn, TJsonProp>> extractor) => "'{json, text}'";
		#endregion

		#region issue 2434
		[Table]
		sealed class Issue2434Table
		{
			[Column] public int     Id;
			[Column] public string? FirstName;
			[Column] public string? LastName;

			[ExpressionMethod(nameof(FullNameExpr), IsColumn = true)]
			public string? FullName { get; set; }

			private static Expression<Func<Issue2434Table, string>> FullNameExpr() => t => $"{t.FirstName} {t.LastName}";
		}

		[Test]
		public void Issue2434Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var tb = db.CreateLocalTable<Issue2434Table>())
			{
				tb.OrderBy(x => x.FullName).ToArray();
			}
		}
		#endregion

		#region issue 3472
		[Table]
		public class Issue3472TableDC
		{
			[Column] public int Id { get; set; }

			[ExpressionMethod(nameof(PersonsCountExpr), IsColumn = true)]
			public int PersonsCount { get; set; }

			private static Expression<Func<Issue3472TableDC, DataConnection, int>> PersonsCountExpr() => (r, db) => db.GetTable<Person>().Where(p => p.ID == r.Id).Count();
		}

		[Table]
		public class Issue3472TableDCTX
		{
			[Column] public int Id { get; set; }

			[ExpressionMethod(nameof(PersonsCountExpr), IsColumn = true)]
			public int PersonsCount { get; set; }

			private static Expression<Func<Issue3472TableDCTX, DataContext, int>> PersonsCountExpr() => (r, db) => db.GetTable<Person>().Where(p => p.ID == r.Id).Count();
		}

		[Test]
		public void Issue3472Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			if (db is DataConnection)
			{
				using var tb = db.CreateLocalTable(new[] { new Issue3472TableDC() { Id = 1 } });
				tb.ToArray();
			}
			else
			{
				using var tb = db.CreateLocalTable(new[] { new Issue3472TableDCTX() { Id = 1 } });
				tb.ToArray();
			}
		}
		#endregion

		#region Null check generated

		[Test]
		public void TestNullCheckInExpressionLeft([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Any(p => p.ID == Function2(Function1Left(null)));
			}
		}

		[Test]
		public void TestNullCheckInExpressionRight([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Any(p => p.ID == Function2(Function1Right(null)));
			}
		}

		[Test]
		public void TestNullCheckInExpressionUsingFieldLeft([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => p.Value1 == null).Any(p => p.ParentID == Function2(Function1Left(p.Value1)));
			}
		}

		[Test]
		public void TestNullCheckInExpressionUsingFieldRight([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => p.Value1 == null).Any(p => p.ParentID == Function2(Function1Right(p.Value1)));
			}
		}

		[Sql.Expression("{0}", ServerSideOnly = true, IsNullable = Sql.IsNullableType.SameAsFirstParameter)]
		private static int? Function2(int? Value) => throw new InvalidOperationException();

		[ExpressionMethod(nameof(Function1LeftExpr))]
		private static int? Function1Left(int? value) => throw new InvalidOperationException();

		[ExpressionMethod(nameof(Function1RightExpr))]
		private static int? Function1Right(int? value) => throw new InvalidOperationException();

		[Sql.Expression("CAST(N'SHOULD NOT BE CALLED' AS INT)", ServerSideOnly = true)]
		private static int Fail(int value) => throw new InvalidOperationException();

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
		[Test(Description = "Tests regression introduced in 3.5.2")]
		public void ComparisonTest1([DataSources(ProviderName.SqlCe, TestProvName.AllClickHouse)] string context, [Values(1, 2)] int iteration)
		{
			using (var db = GetDataContext(context))
			{
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
		}

		[Test(Description = "Tests regression introduced in 3.5.2")]
		public void ComparisonTest2([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var left  = GetQuery(db, 0);
				var right = GetQuery(db, 2);

				Assert.That(
					db.Person.Where(_ =>
					left.Where(rec => !right.Select(r2 => r2.PersonID).Contains(rec.PersonID)).Count() == 0
					&&
					right.Where(rec => !left.Select(r2 => r2.PersonID).Contains(rec.PersonID)).Count() == 0)
					.Any(), Is.False);
			}
		}

		private static IQueryable<Patient> GetQuery(ITestDataContext db, int? personId)
		{
			return db.Patient.Where(_ => _.PersonID == personId);
		}

		#endregion

		#region Issue 4613
		[Table]
		class Issue4613Service
		{
			[Column] public int IdContract { get; set; }
		}

		[Table]
		class Issue4613Contract
		{
			[Column] public int Id { get; set; }
		}

		class Issue4613ServiceProjection
		{
			public int IdContract { get; set; }
		}

		static class Issue4613Expressions
		{
			[ExpressionMethod(nameof(ToServiceProjectionExpr))]
			public static Issue4613ServiceProjection ToServiceProjection(Issue4613Service serv)
				=> throw new NotImplementedException();

			static Expression<Func<Issue4613Service, Issue4613ServiceProjection>> ToServiceProjectionExpr()
				=> (serv) => new Issue4613ServiceProjection { IdContract = serv.IdContract };
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4613")]
		public void Issue4613Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue4613Service>();
			using var t2 = db.CreateLocalTable<Issue4613Contract>();

			var query = (
				from servProj in (
				from serv in db.GetTable<Issue4613Service>()
				select Issue4613Expressions.ToServiceProjection(serv))
				join contract in db.GetTable<Issue4613Contract>() on servProj.IdContract equals contract.Id
				select new
				{
					Contract = contract,
					Service = servProj
				});

			query.ToList();
		}

		#endregion
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
