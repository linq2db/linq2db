using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common;

[TestFixture]
public class ExpressionEqualityComparerTests
{
	[Test(Description = "https://github.com/linq2db/linq2db/issues/6458")]
	public void BlockLambdaComparesParametersByPosition([Values(1, 2)] int depth, [Values] bool swapParameters)
	{
		var x     = Expression.Parameter(typeof(int), "x");
		var y     = Expression.Parameter(typeof(int), "y");
		var left  = Expression.Parameter(typeof(int), "left");
		var right = Expression.Parameter(typeof(int), "right");

		Expression firstBody  = Expression.Subtract(x, y);
		Expression secondBody = swapParameters ? Expression.Subtract(right, left) : Expression.Subtract(left, right);

		for (var i = 0; i < depth; i++)
		{
			firstBody  = Expression.Block(firstBody);
			secondBody = Expression.Block(secondBody);
		}

		var first  = Expression.Lambda<Func<int, int, int>>(firstBody, x, y);
		var second = Expression.Lambda<Func<int, int, int>>(secondBody, left, right);

		ExpressionEqualityComparer.Instance.Equals(first, second)
			.ShouldBe(!swapParameters);
		ExpressionEqualityComparer.Instance.Equals(second, first)
			.ShouldBe(!swapParameters);
	}
}
