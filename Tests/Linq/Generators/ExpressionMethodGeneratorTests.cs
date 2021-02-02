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
	[TestFixture]
	public partial class ExpressionMethodGeneratorTests : TestBase
	{
		public class PersonDto
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;
		}

		public partial class ExprPerson
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

			public static ExprPerson[] GenerateTestData() =>
				Enumerable.Range(0, 20)
					.Select(i => new ExprPerson { Id = i, Name = $"Person{i}", })
					.ToArray();
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoReturn(ExprPerson person)
		{
			return new PersonDto
			{
				Id = person.Id,
				Name = person.Name,
			};
		}

		[GenerateExpressionMethod]
		public static PersonDto ToDtoArrow(ExprPerson person)
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

		[GenerateExpressionMethod]
		public static PersonDto ToDtoComplex(Tuple<int, string> tuple)
			=> new PersonDto
			{
				Id = tuple.Item1,
				Name = tuple.Item2,
			};

		[Test]
		public void VerifyGeneratedExpressions()
		{
			Expression<Func<ExprPerson, PersonDto>> expr = p => new PersonDto { Id = p.Id, Name = p.Name, };
			var comparer = ExpressionEqualityComparer.Instance;

			Assert.True(comparer.Equals(expr, ExprPerson.Test1()));
			Assert.True(comparer.Equals(expr, ExprPerson.Test2()));
			Assert.True(comparer.Equals(expr, ExprPerson.Test3()));
			Assert.True(comparer.Equals(expr, __Expression_ToDtoArrow_ExprPerson()));
			Assert.True(comparer.Equals(expr, __Expression_ToDtoReturn_ExprPerson()));

			Expression<Func<int, string, string, TaskStatus, PersonDto>> expr2 =
				(int id, string firstName, string lastName, TaskStatus status)
				=> new PersonDto
				{
					Id = id + (int)status,
					Name = firstName + lastName,
				};
			Assert.True(comparer.Equals(expr2, __Expression_ToDtoComplex_int_string_string_TaskStatus()));

			Expression<Func<Tuple<int, string>, PersonDto>> expr3 =
				(Tuple<int, string> tuple)
				=> new PersonDto
				{
					Id = tuple.Item1,
					Name = tuple.Item2,
				};
			Assert.True(comparer.Equals(expr3, __Expression_ToDtoComplex_Tuple_int_string_()));
		}

		[Test]
		public void VerifyUseInQuery([DataSources] string context)
		{
			var testData = ExprPerson.GenerateTestData();
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(testData))
			{
				Assert.AreEqual(
					testData.Length,
					testData.Select(p => p.PersonDto1).ToArray().Length);
			}
		}
	}
}
