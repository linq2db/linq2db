using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Generators
{
	public partial class ExpressionMethodGeneratorTests
	{
		public class PersonDto
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;
		}

		public partial class Person
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;

			[GenerateExpressionMethod(MethodName = "Test1")]
			public PersonDto PersonDto1 =>
				new PersonDto { Id = Id, Name = Name, };

			[GenerateExpressionMethod(MethodName = "Test2")]
			public PersonDto PersonDto2
			{
				get => new PersonDto { Id = this.Id, Name = this.Name, };
			}

			[GenerateExpressionMethod(MethodName = "Test3")]
			public PersonDto PersonDto3
			{
				get
				{
					return new PersonDto { Id = Id, Name = Name, };
				}
			}
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoReturn(Person person)
		{
			return new PersonDto
			{
				Id = person.Id,
				Name = person.Name,
			};
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoArrow(Person person)
			=> new PersonDto
			{
				Id = person.Id,
				Name = person.Name
			};

		[GenerateExpressionMethod]
		public static PersonDto ToDtoComplex(int id, string firstName, string lastName, TaskStatus status)
			=> new PersonDto
			{
				Id = id + (int)status,
				Name = firstName + lastName,
			};

		[Test]
		public void VerifyGeneratedExpressionsAreCorrect()
		{
			Expression<Func<Person, PersonDto>> expr = p => new PersonDto { Id = p.Id, Name = p.Name, };
			var comparer = ExpressionEqualityComparer.Instance;

			Assert.True(comparer.Equals(expr, Person.Test1()));
			Assert.True(comparer.Equals(expr, Person.Test2()));
			Assert.True(comparer.Equals(expr, Person.Test3()));
			Assert.True(comparer.Equals(expr, __ToDtoArrowExpression()));
			Assert.True(comparer.Equals(expr, __ToDtoReturnExpression()));

			Expression<Func<int, string, string, TaskStatus, PersonDto>> expr2 = (int id, string firstName, string lastName, TaskStatus status)
				=> new PersonDto
				{
					Id = id + (int)status,
					Name = firstName + lastName,
				};
			Assert.True(comparer.Equals(expr2, __ToDtoComplexExpression()));
		}
	}
}
