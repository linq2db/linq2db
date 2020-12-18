using System;
using System.Collections.Generic;
using System.Linq;
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

		public class Person
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoReturn(Person person)
		{
			return new PersonDto
			{
				Id = person.Id,
				Name = person.Name
			};
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoArrow(Person person) 
			=> new PersonDto
			{
				Id = person.Id,
				Name = person.Name
			};

		[Test]
		public void VerifyExpressionMethodGeneratedOnReturn()
		{
			var baseMethod = typeof(ExpressionMethodGeneratorTests).GetMethod(nameof(ToDtoReturn));

			var expressionMethodAttributes = baseMethod!.GetCustomAttributes(typeof(ExpressionMethodAttribute), false);
			Assert.IsTrue(expressionMethodAttributes.Any());

			var methodName = (expressionMethodAttributes[0] as ExpressionMethodAttribute)!.MethodName;
			Assert.IsTrue(typeof(ExpressionMethodGeneratorTests).GetMethods().Any(m => m.Name == methodName));
		}

		[Test]
		public void VerifyExpressionMethodGeneratedOnArrow()
		{
			var baseMethod = typeof(ExpressionMethodGeneratorTests).GetMethod(nameof(ToDtoArrow));

			var expressionMethodAttributes = baseMethod!.GetCustomAttributes(typeof(ExpressionMethodAttribute), false);
			Assert.IsTrue(expressionMethodAttributes.Any());

			var methodName = (expressionMethodAttributes[0] as ExpressionMethodAttribute)!.MethodName;
			Assert.IsTrue(typeof(ExpressionMethodGeneratorTests).GetMethods().Any(m => m.Name == methodName));
		}
	}
}
