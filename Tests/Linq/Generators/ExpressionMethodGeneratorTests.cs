using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LinqToDB;
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

			[GenerateExpressionMethod(MethodName = "PersonDtoExpression")]
			public PersonDto PersonDto1 =>
				new PersonDto { Id = Id, Name = Name, };

			[GenerateExpressionMethod]
			public PersonDto PersonDto2
			{
				get => new PersonDto { Id = this.Id, Name = this.Name, };
			}

			[GenerateExpressionMethod]
			public PersonDto PersonDto3
			{
				get
				{
					return new PersonDto { Id = Id , Name = Name, };
				}
			}
		}

		[GenerateExpressionMethod(MethodName = "asdf")]
		public static PersonDto ToDtoReturn(Person person)
		{
			return new PersonDto
			{
				Id = person.Id,
				Name = person.Name,
			};
		}

		[Test]
		public void VerifyExpressionMethodGeneratedOnReturn()
		{
			var baseMethod = typeof(ExpressionMethodGeneratorTests).GetMethod(nameof(ToDtoReturn));

			var expressionMethodAttributes = baseMethod!.GetCustomAttributes(typeof(ExpressionMethodAttribute), false);
			Assert.IsTrue(expressionMethodAttributes.Any());

			var methodName = (expressionMethodAttributes[0] as ExpressionMethodAttribute)!.MethodName;
			Assert.IsTrue(typeof(ExpressionMethodGeneratorTests)
				.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
				.Any(m => m.Name == methodName));
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoArrow(Person person)
			=> new PersonDto
			{
				Id = person.Id,
				Name = person.Name
			};

		[Test]
		public void VerifyExpressionMethodGeneratedOnArrow()
		{
			var baseMethod = typeof(ExpressionMethodGeneratorTests).GetMethod(nameof(ToDtoArrow));

			var expressionMethodAttributes = baseMethod!.GetCustomAttributes(typeof(ExpressionMethodAttribute), false);
			Assert.IsTrue(expressionMethodAttributes.Any());

			var methodName = (expressionMethodAttributes[0] as ExpressionMethodAttribute)!.MethodName;
			Assert.IsTrue(typeof(ExpressionMethodGeneratorTests)
				.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
				.Any(m => m.Name == methodName));
		}


		[GenerateExpressionMethod]
		public static PersonDto ToDtoComplex(int id, string firstName, string lastName, TaskStatus status)
			=> new PersonDto
			{
				Id = id + (int)status,
				Name = firstName + lastName,
			};

		[Test]
		public void VerifyExpressionMethodGeneratedOnComplex()
		{
			var baseMethod = typeof(ExpressionMethodGeneratorTests).GetMethod(nameof(ToDtoComplex));

			var expressionMethodAttributes = baseMethod!.GetCustomAttributes(typeof(ExpressionMethodAttribute), false);
			Assert.IsTrue(expressionMethodAttributes.Any());

			var methodName = (expressionMethodAttributes[0] as ExpressionMethodAttribute)!.MethodName;
			Assert.IsTrue(typeof(ExpressionMethodGeneratorTests)
				.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
				.Any(m => m.Name == methodName));
		}
	}
}
