using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ExpressionsTest : TestBase
	{
		static int Count1(Parent p) { return p.Children.Count(c => c.ChildID > 0); }

		[Test]
		public void MapMember1([DataContexts] string context)
		{
			Expressions.MapMember<Parent,int>(p => Count1(p), p => p.Children.Count(c => c.ChildID > 0));

			using (var db = GetDataContext(context))
				AreEqual(Parent.Select(p => Count1(p)), db.Parent.Select(p => Count1(p)));
		}

		static int Count2(Parent p, int id) { return p.Children.Count(c => c.ChildID > id); }

		[Test]
		public void MapMember2([DataContexts] string context)
		{
			Expressions.MapMember<Parent,int,int>((p,id) => Count2(p, id), (p, id) => p.Children.Count(c => c.ChildID > id));

			using (var db = GetDataContext(context))
				AreEqual(Parent.Select(p => Count2(p, 1)), db.Parent.Select(p => Count2(p, 1)));
		}

		static int Count3(Parent p, int id) { return p.Children.Count(c => c.ChildID > id) + 2; }

		[Test]
		public void MapMember3([DataContexts(ProviderName.SqlCe)] string context)
		{
			Expressions.MapMember<Parent,int,int>((p,id) => Count3(p, id), (p, id) => p.Children.Count(c => c.ChildID > id) + 2);

			var n = 2;

			using (var db = GetDataContext(context))
				AreEqual(Parent.Select(p => Count3(p, n)), db.Parent.Select(p => Count3(p, n)));
		}

		[MethodExpression("Count4Expression")]
		static int Count4(Parent p, int id, int n)
		{
			return (_count4Expression ?? (_count4Expression = Count4Expression().Compile()))(p, id, n);
		}

		static Func<Parent,int,int,int> _count4Expression;

		static Expression<Func<Parent,int,int,int>> Count4Expression()
		{
			return (p, id, n) => p.Children.Count(c => c.ChildID > id) + n;
		}

		[Test]
		public void MethodExpression4([DataContexts] string context)
		{
			var n = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => Count4(p, n, 4)),
					db.Parent.Select(p => Count4(p, n, 4)));
		}

		[MethodExpression("Count5Expression")]
		static int Count5(ITestDataContext db, Parent p, int n)
		{
			return (_count5Expression ?? (_count5Expression = Count5Expression().Compile()))(db, p, n);
		}

		static Func<ITestDataContext,Parent,int,int> _count5Expression;

		static Expression<Func<ITestDataContext,Parent,int,int>> Count5Expression()
		{
			return (db, p, n) => Sql.AsSql(db.Child.Where(c => c.ParentID == p.ParentID).Count() + n);
		}

		[Test]
		public void MethodExpression5([DataContexts(ProviderName.SqlCe, ProviderName.Firebird)] string context)
		{
			var n = 2;

			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count() + n),
					db.Parent.Select(p => Count5(db, p, n)));
		}

		[MethodExpression("Count6Expression")]
		static int Count6(Table<Child> c, Parent p)
		{
			return (_count6Expression ?? (_count6Expression = Count6Expression().Compile()))(c, p);
		}

		static Func<Table<Child>,Parent,int> _count6Expression;

		static Expression<Func<Table<Child>,Parent,int>> Count6Expression()
		{
			return (ch, p) => ch.Where(c => c.ParentID == p.ParentID).Count();
		}

		[Test]
		public void MethodExpression6([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count()),
					db.Parent.Select(p => Count6(db.Child, p)));
		}

		[MethodExpression("Count7Expression")]
		static int Count7(Table<Child> ch, Parent p, int n)
		{
			return (_count7Expression ?? (_count7Expression = Count7Expression().Compile()))(ch, p, n);
		}

		static Func<Table<Child>,Parent,int,int> _count7Expression;

		static Expression<Func<Table<Child>,Parent,int,int>> Count7Expression()
		{
			return (ch, p, n) => Sql.AsSql(ch.Where(c => c.ParentID == p.ParentID).Count() + n);
		}

		[Test]
		public void MethodExpression7([DataContexts(ProviderName.SqlCe, ProviderName.Firebird)] string context)
		{
			var n = 2;

			using (var db = GetDataContext(context))
				AreEqual(
					   Parent.Select(p => Child.Where(c => c.ParentID == p.ParentID).Count() + n),
					db.Parent.Select(p => Count7(db.Child, p, n)));
		}
		[MethodExpression("Expression8")]
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
		public void MethodExpression8([DataContexts] string context)
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
		public void MethodExpression9()
		{
			using (var db = new TestDbManager())
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
					from p in TestDbManager.GetParent9(db, ch)
					where ch.ParentID == p.ParentID
					select ch);
		}

		[Test]
		public void MethodExpression10()
		{
			using (var db = new TestDbManager())
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
	}
}
