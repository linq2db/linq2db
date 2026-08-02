using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	/// <summary>
	/// <see href="https://github.com/linq2db/linq2db/issues/5719"/>
	/// </summary>
	[TestFixture]
	public class Issue5719Tests : TestBase
	{
		[Table("Issue5719Address")]
		[InheritanceMapping(Code = null, Type = typeof(Address), IsDefault = true)]
		[InheritanceMapping(Code = "R", Type = typeof(Root))]
		[InheritanceMapping(Code = "S1", Type = typeof(Sub1))]
		[InheritanceMapping(Code = "S2", Type = typeof(Sub2))]
		[InheritanceMapping(Code = "S3", Type = typeof(Sub3))]
		class Address
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(IsDiscriminator = true)] public string? Kind { get; set; }
			[Column] public int? ReferenceId { get; set; }
			[Column] public int? Reference2Id { get; set; }
			[Column] public string? Value1 { get; set; }
			[Column] public string? Value2 { get; set; }
			[Column] public string? Value3 { get; set; }
			[Column] public string? Value4 { get; set; }
			[Column] public string? Value5 { get; set; }

			[Association(ThisKey = nameof(ReferenceId), OtherKey = nameof(Id))]
			public Address? Reference { get; set; }

			[Association(ThisKey = nameof(Reference2Id), OtherKey = nameof(Id))]
			public Address? Reference2 { get; set; }
		}

		sealed class Root : Address
		{
		}

		sealed class Sub1 : Address
		{
		}

		sealed class Sub2 : Address
		{
		}

		sealed class Sub3 : Address
		{
		}

		sealed class ConstructorFactoryVisitor : ExpressionVisitor
		{
			public int Count { get; private set; }

			protected override Expression VisitInvocation(InvocationExpression node)
			{
				var invoke = node.Expression.Type.GetMethod(nameof(Action.Invoke));

				if (invoke?.GetParameters().Length == 0 && typeof(Address).IsAssignableFrom(invoke.ReturnType))
					Count++;

				return base.VisitInvocation(node);
			}
		}

		[Test]
		public void ReusesDuplicateTphConstructors([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			Expression? mapperExpression = null;

			using var db = GetDataContext(
				context,
				options => options
					.UseTraceMapperExpression(true)
					.UseTracing(info =>
					{
						if (info.TraceInfoStep == TraceInfoStep.MapperCreated)
							mapperExpression = info.MapperExpression;
					}));
			using var table = db.CreateLocalTable<Address>();

			db.Insert(new Root { Id = 1, ReferenceId = 2, Reference2Id = 3 });
			db.Insert(new Address { Id = 2 });
			db.Insert(new Sub1 { Id = 3 });

			var result = db.GetTable<Root>()
				.LoadWith(root => root.Reference)
				.LoadWith(root => root.Reference2)
				.Single();

			result.Reference.ShouldNotBeNull();
			result.Reference.Id.ShouldBe(2);
			result.Reference2.ShouldNotBeNull();
			result.Reference2.Id.ShouldBe(3);

			mapperExpression.ShouldNotBeNull();

			var visitor = new ConstructorFactoryVisitor();

			visitor.Visit(mapperExpression);
			visitor.Count.ShouldBeGreaterThan(0);
		}
	}
}
