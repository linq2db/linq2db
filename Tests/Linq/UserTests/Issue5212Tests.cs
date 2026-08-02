using System;
using System.Linq.Expressions;

using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5212Tests : TestBase
	{
		readonly record struct UnaryOperand(int Value)
		{
			public static UnaryOperand operator -(UnaryOperand value) => new(-value.Value);
		}

		[Test]
		public void MapUnary()
		{
			Expressions.MapUnary<UnaryOperand, UnaryOperand>(value => -value, value => new UnaryOperand(value.Value + 1));

			Expression<Func<UnaryOperand, UnaryOperand>> expression = value => -value;
			var unary  = (UnaryExpression)expression.Body;
			var mapped = Expressions.ConvertUnary(MappingSchema.Default, unary);

			mapped.ShouldNotBeNull();
			mapped.Body.ToString().ShouldContain("Value + 1");
		}
	}
}
