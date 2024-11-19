using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Common;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	public static class PredicateBuilder
	{
		public static Expression<Func<T, bool>> True<T> ()  { return f => true;  }
		public static Expression<Func<T, bool>> False<T> () { return f => false; }

		public static Expression<Func<T, bool>> Or<T> (this Expression<Func<T, bool>> expr1,
			Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke (expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>
				(Expression.OrElse (expr1.Body, invokedExpr), expr1.Parameters);
		}

		public static Expression<Func<T, bool>> And<T> (this Expression<Func<T, bool>> expr1,
			Expression<Func<T, bool>> expr2)
		{
			var invokedExpr = Expression.Invoke (expr2, expr1.Parameters);
			return Expression.Lambda<Func<T, bool>>
				(Expression.AndAlso (expr1.Body, invokedExpr), expr1.Parameters);
		}
	}

	[TestFixture]
	public class Issue667Tests: TestBase
	{
		[Test]
		public void TestAnd([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.True<Parent>();
				predicate = predicate.And(p => p.ParentID >= 1);
				predicate = predicate.And(p => p.ParentID <= 4);

				var q = db.Parent.Where(predicate);
				var e = Parent.Where(predicate.CompileExpression());

				AreEqual(r => new Parent() { ParentID = r.ParentID, Value1 = r.Value1 }, e, q, ComparerBuilder.GetEqualityComparer<Parent>(), src => src.OrderBy(p => p.ParentID));
			}
		}

		[Test]
		public void TestAndFalse([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.False<Parent>();
				predicate = predicate.And(p => p.ParentID >= 1);
				predicate = predicate.And(p => p.ParentID <= 4);

				var q = db.Parent.Where(predicate);
				var e = Parent.Where(predicate.CompileExpression());

				Assert.That(q, Is.EqualTo(e));
			}
		}

		[Test]
		public void TestOrTrue([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.True<Parent>();
				predicate = predicate.Or(p => p.ParentID >= 1);
				predicate = predicate.Or(p => p.ParentID <= 4);

				var q = db.Parent.Where(predicate);
				var e = Parent.Where(predicate.CompileExpression());

				AreEqual(r => new Parent() { ParentID = r.ParentID, Value1 = r.Value1 }, e, q, ComparerBuilder.GetEqualityComparer<Parent>(), src => src.OrderBy(p => p.ParentID));
			}
		}

		[Test]
		public void TestOrFalse([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.False<Parent>();
				predicate = predicate.Or(p => p.ParentID >= 1);
				predicate = predicate.Or(p => p.ParentID <= 4);

				var q = db.Parent.Where(predicate);
				var e = Parent.Where(predicate.CompileExpression());

				AreEqual(r => new Parent() { ParentID = r.ParentID, Value1 = r.Value1 }, e, q, ComparerBuilder.GetEqualityComparer<Parent>(), src => src.OrderBy(p => p.ParentID));
			}
		}
	}
}
