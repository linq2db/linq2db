using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Internal.Linq;
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
			[Column(IsPrimaryKey = true)]
			public Guid Id { get; set; }

			[Column(IsPrimaryKey = true)]
			public int Number { get; set; }

			[Column]
			public string? Test { get; set; }
		}

		private object? GetExistingDto(object obj, IQueryable queryable, MappingSchema mappingSchema)
		{
			var type   = obj.GetType();
			var ed     = mappingSchema.GetEntityDescriptor(type);
			var listPk = ed.Columns.Where(c => c.IsPrimaryKey).Select(c => c.MemberName).ToList();

			var         parExp  = Expression.Parameter(typeof(object));
			var         castExp = Expression.Convert(parExp, type);
			Expression? e       = null;
			foreach (var pkName in listPk)
			{
				var par1  = Expression.Property(castExp, pkName);
				var par2  = Expression.Constant(type.GetProperty(pkName)?.GetValue(obj));
				var inner = Expression.Equal(par1, par2);
				e = e == null ? inner : Expression.AndAlso(e, inner);
			}

			var query = Expression.Lambda<Func<BasicDto, bool>>(e!, parExp);

			return queryable.Cast<BasicDto>().FirstOrDefault(query);
		}

		private T? GetExistingDto<T>(IQueryable<T> queryable, T obj)
			where T : BasicDto
		{
			var predicateLambda = BuildSearchPredicate(queryable, obj);

			return queryable.FirstOrDefault(predicateLambda);
		}

		private async Task<T?> GetExistingDtoAsync<T>(IQueryable<T> queryable, T obj, CancellationToken cancellationToken)
			where T : BasicDto
		{
			var predicateLambda = BuildSearchPredicate(queryable, obj);

			return await queryable.FirstOrDefaultAsync(predicateLambda, cancellationToken);
		}

		static Expression<Func<T, bool>> BuildSearchPredicate<T>(IQueryable<T> queryable, T obj) where T : BasicDto
		{
			var dc = Internals.GetDataContext(queryable);
			if (dc == null)
				throw new InvalidOperationException("Unable to get DataContext from queryable.");

			var type   = typeof(T);
			var ed     = dc.MappingSchema.GetEntityDescriptor(type);
			var listPk = ed.Columns.Where(c => c.IsPrimaryKey).Select(c => c.MemberInfo).ToList();

			if (listPk.Count == 0)
				throw new InvalidOperationException("Entity does not have primary keys defined.");

			var parExp = Expression.Parameter(type);

			var objExpr = Expression.Constant(obj);

			var predicate = listPk
				.Select(x => Expression.Equal(Expression.MakeMemberAccess(parExp, x), Expression.MakeMemberAccess(objExpr, x)))
				.Aggregate(Expression.AndAlso);

			var predicateLambda = Expression.Lambda<Func<T, bool>>(predicate, parExp);
			return predicateLambda;
		}

		[Test]
		public void DynamicCreatedQueryFromPks([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<TestDtoWithPks>();

			var dto = new TestDtoWithPks() { Id = Guid.NewGuid(), Number = 5, Test = "aaa" };
			db.Insert(dto);

			var existingDto = GetExistingDto(dto, table, db.MappingSchema);
			Assert.That(existingDto, Is.Not.Null);

		}

		[Test]
		public void FixedQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<TestDtoWithPks>();

			var dto = new TestDtoWithPks() { Id = Guid.NewGuid(), Number = 5, Test = "aaa" };
			var id  = dto.Id;
			var nr  = dto.Number;
			db.Insert(dto);

			var existingDto = ((IQueryable)db.GetTable<TestDtoWithPks>()).Cast<BasicDto>().FirstOrDefault(x => ((TestDtoWithPks)x).Id == id && ((TestDtoWithPks)x).Number == nr);
			Assert.That(existingDto, Is.Not.Null);
		}

		[Test]
		public void GetExistingDtoCall([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<TestDtoWithPks>();

			var dto = new TestDtoWithPks() { Id = Guid.NewGuid(), Number = 5, Test = "aaa" };
			db.Insert(dto);

			var existingDto = GetExistingDto(table, dto);
			Assert.That(existingDto, Is.Not.Null);
		}

		[Test]
		public async Task GetExistingAsyncCall([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus)] string context)
		{
			await using var db    = GetDataContext(context);
			await using var table = db.CreateLocalTable<TestDtoWithPks>();

			var dto = new TestDtoWithPks() { Id = Guid.NewGuid(), Number = 5, Test = "aaa" };
			db.Insert(dto);

			var existingDto = await GetExistingDtoAsync(table, dto, CancellationToken.None);
			Assert.That(existingDto, Is.Not.Null);

		}
	}
}
