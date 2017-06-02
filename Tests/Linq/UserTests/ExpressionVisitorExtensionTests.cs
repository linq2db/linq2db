using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using NUnit.Framework;

namespace Tests.UserTests
{
	public class ExpressionVisitorExtensionTests: TestBase
	{
		class TestExpressionVisitor : ExpressionVisitor
		{
			private readonly IEnumerable<Expression> _enumerable;
			private readonly IEnumerator<Expression> _enumerator;

			public TestExpressionVisitor(IEnumerable<Expression> enumerable)
			{
				_enumerable = enumerable;
				_enumerator = _enumerable.GetEnumerator();
			}

			public override Expression Visit(Expression node)
			{
				if (node != null)
				{
					if (!_enumerator.MoveNext())
						throw new Exception("Enumrator should return entities");
					Assert.AreEqual(node, _enumerator.Current);
				}
				return base.Visit(node);
			}
		}

		[Test]
		public static void Test1()
		{
			var list = new[] {1, 4, 5};
			var list2 = new[] {1.0, 4, 5};

			var zz = from l1 in list.AsQueryable()
				join l2 in list2.AsQueryable() on l1 equals l2
				where (double)l1 == l2
				select new
				{
					l1,
					l2
				};

			new TestExpressionVisitor(zz.Expression.EnumerateParentFirst()).Visit(zz.Expression);
		}
	}
}