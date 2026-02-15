using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5336Tests : TestBase
	{
		public class BasicDto
		{
		}

		[Table]
		public class TestDtoWithPks : BasicDto
		{
			[Column(IsPrimaryKey = true)] public Guid Id { get; set; }
			[Column(IsPrimaryKey = true)] public int Number { get; set; }
			[Column] public string? Test { get; set; }
		}

		private object? GetExistingDto(object obj, IQueryable queryable, MappingSchema mappingSchema)
		{
			var type = obj.GetType();
			var ed = mappingSchema.GetEntityDescriptor(type);
			var listPk = ed.Columns.Where(c => c.IsPrimaryKey).Select(c => c.MemberName).ToList();

			var parExp = Expression.Parameter(typeof(object));
			var castExp = Expression.Convert(parExp, type);
			Expression? e = null;
			foreach (var pkName in listPk)
			{
				var par1 = Expression.Property(castExp, pkName);
				var par2 = Expression.Constant(type.GetProperty(pkName)?.GetValue(obj));
				var inner = Expression.Equal(par1, par2);
				e = e == null ? inner : Expression.And(e, inner);
			}

			var query = Expression.Lambda<Func<BasicDto, bool>>(e!, parExp);

			return queryable.Cast<BasicDto>().FirstOrDefault(query);
		}

		[Test]
		public void DynamicCreatedQueryFromPks([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TestDtoWithPks>())
			{
				var dto = new TestDtoWithPks() { Id = Guid.NewGuid(), Number = 5, Test = "aaa" };
				db.Insert(dto);
				var existingDto = GetExistingDto(dto, db.GetTable<TestDtoWithPks>(), db.MappingSchema);
				Assert.That(existingDto, Is.Not.Null);
			}
		}

		[Test]
		public void FixedQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<TestDtoWithPks>())
			{
				var dto = new TestDtoWithPks() { Id = Guid.NewGuid(), Number = 5, Test = "aaa" };
				var id = dto.Id;
				var nr = dto.Number;
				db.Insert(dto);
				var existingDto = ((IQueryable) db.GetTable<TestDtoWithPks>()).Cast<BasicDto>().FirstOrDefault(x => ((TestDtoWithPks)x).Id == id && ((TestDtoWithPks)x).Number == nr);
				Assert.That(existingDto, Is.Not.Null);
			}
		}
	}
}
