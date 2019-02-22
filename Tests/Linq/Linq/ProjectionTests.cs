using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ProjectionTests : TestBase
	{
		[Table]
		public class SomeEntity
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public int? OtherId { get; set; }

			[Association(ThisKey = "OtherId", OtherKey = "Id", CanBeNull = true)]
			public SomeOtherEntity Other { get; set; }

			[ExpressionMethod(nameof(GetThroughIsActual), IsColumn = false)]
			public bool? ThroughIsActual { get; set; }

			private static Expression<Func<SomeEntity, bool?>> GetThroughIsActual()
			{
				return t => Sql.ToNullable(t.Other.IsActual);
			}
		}

		[Table]
		public class SomeOtherEntity
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public string Name { get; set; }
			[Column]
			public bool IsActual { get; set; }
		}

		[Test]
		public void AssociationTest([SQLiteDataSources(true)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SomeEntity>(new[]{new SomeEntity{Id = 1, OtherId = 3} }))
			using (db.CreateLocalTable<SomeOtherEntity>(new[]{new SomeOtherEntity{Id = 2, IsActual = true} }))
			{
				var query = db.GetTable<SomeEntity>()
					.Select(t => new { t.Id, t.OtherId, t.ThroughIsActual });

				var result = query.First();

				Assert.That(result.ThroughIsActual, Is.Null);
			}
		}

		[Test]
		public void ToNullableTes([SQLiteDataSources(true)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SomeEntity>(new[]{new SomeEntity{Id = 1, OtherId = 3} }))
			using (db.CreateLocalTable<SomeOtherEntity>(new[]{new SomeOtherEntity{Id = 2, IsActual = true} }))
			{
				var query = from t in db.GetTable<SomeEntity>()
					from t2 in db.GetTable<SomeOtherEntity>().LeftJoin(t2 => t2.Id == t.OtherId)
					select new { t.Id, t.OtherId, IsActual = Sql.ToNullable(t2.IsActual) };

				var result = query.First();

				Assert.That(result.IsActual, Is.Null);
			}
		}
	}
}
