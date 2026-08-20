using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Expressions;
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

		// Collects every place the mapper actually builds an Address, so the test can assert that no constructor
		// body is emitted more than once. Counting occurrences (rather than distinct nodes) is the point: before
		// the fix the same constructor was embedded at each use site, so Expression.Compile processed it N times.
		sealed class EntityConstructionVisitor : ExpressionVisitor
		{
			public List<Expression> Constructions { get; } = new();

			protected override Expression VisitMemberInit(MemberInitExpression node)
			{
				if (typeof(Address).IsAssignableFrom(node.Type))
					Constructions.Add(node);

				return base.VisitMemberInit(node);
			}

			protected override Expression VisitNew(NewExpression node)
			{
				// bare parameterless New is shared by every MemberInit of the same type — only a parameterized
				// New identifies a distinct construction on its own.
				if (node.Arguments.Count > 0 && typeof(Address).IsAssignableFrom(node.Type))
					Constructions.Add(node);

				return base.VisitNew(node);
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

			var visitor = new EntityConstructionVisitor();

			visitor.Visit(mapperExpression);

			// sanity: the TPH branches are in there at all, otherwise the assertion below is vacuous
			visitor.Constructions.Count.ShouldBeGreaterThan(0);

			// the actual guard: a constructor duplicated across branches must be stored once and reused, so no
			// two occurrences in the mapper may be structurally equal
			visitor.Constructions
				.GroupBy(c => c, ExpressionEqualityComparer.Instance)
				.Where(g => g.Count() > 1)
				.Select(g => $"{g.Key.Type.Name} emitted {g.Count()} times")
				.ToArray()
				.ShouldBeEmpty();
		}
	}
}
