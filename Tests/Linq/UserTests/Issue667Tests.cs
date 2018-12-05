using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Tests.Model;
using Tests.Tools;

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
		[Test, DataContextSource]
		public void TestAnd(string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.True<Parent>();
				predicate = predicate.And(p => p.ParentID >= 1);
				predicate = predicate.And(p => p.ParentID <= 4);
				
				var q = db.Parent.Where(predicate); 
				var e = Parent   .Where(predicate.Compile());

				AreEqual(r => new Parent() { ParentID = r.ParentID, Value1 = r.Value1 }, e, q, ComparerBuilder<Parent>.GetEqualityComparer(), src => src.OrderBy(p => p.ParentID));
			}
		}

		[Test, DataContextSource]
		public void TestAndFalse(string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.False<Parent>();
				predicate = predicate.And(p => p.ParentID >= 1);
				predicate = predicate.And(p => p.ParentID <= 4);
				
				var q = db.Parent.Where(predicate); 
				var e = Parent.Where(predicate.Compile()); 

				Assert.AreEqual(e, q);
			}
		}

		[Test, DataContextSource]
		public void TestOrTrue(string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.True<Parent>();
				predicate = predicate.Or(p => p.ParentID >= 1);
				predicate = predicate.Or(p => p.ParentID <= 4);
				
				var q = db.Parent.Where(predicate); 
				var e = Parent   .Where(predicate.Compile());

				AreEqual(r => new Parent() { ParentID = r.ParentID, Value1 = r.Value1 }, e, q, ComparerBuilder<Parent>.GetEqualityComparer(), src => src.OrderBy(p => p.ParentID));
			}
		}

		[Test, DataContextSource]
		public void TestOrFalse(string context)
		{
			using (var db = GetDataContext(context))
			{
				var predicate = PredicateBuilder.False<Parent>();
				predicate = predicate.Or(p => p.ParentID >= 1);
				predicate = predicate.Or(p => p.ParentID <= 4);
				
				var q = db.Parent.Where(predicate); 
				var e = Parent   .Where(predicate.Compile());

				AreEqual(r => new Parent() { ParentID = r.ParentID, Value1 = r.Value1 }, e, q, ComparerBuilder<Parent>.GetEqualityComparer(), src => src.OrderBy(p => p.ParentID));
			}
		}
	}
}
