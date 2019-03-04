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
		public void ToNullableTest([SQLiteDataSources(true)] string context)
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

		[Test]
		public void GroupByWithCast([DataSources(ProviderName.MySql, TestProvName.MySql57)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.GroupBy(_ => new
					{
						group = _.ID,
						key = new
						{
							// cast
							id = (int?)_.Patient.PersonID,
							value = _.Patient.Diagnosis
						}
					})
					.Having(auto16031 => auto16031.Key.group == 1)
					.Select(_ => new
					{
						x = new
						{
							@value = _.Key.key.@value,
							id = _.Key.key.id
						},
						y = _.Average(auto16033 => auto16033.ID)
					})
					.OrderByDescending(_ => _.x.value)
					.Take(1000);

				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(1, result[0].y);
				Assert.IsNull(result[0].x.value);
				Assert.IsNull(result[0].x.id);
			}
		}

		[Test]
		public void GroupByWithTwoCasts([DataSources(ProviderName.MySql, TestProvName.MySql57)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.GroupBy(_ => new
					{
						group = _.ID,
						key = new
						{
							// cast
							id = (int?)_.Patient.PersonID,
							value = _.Patient.Diagnosis
						}
					})
					.Having(auto16031 => auto16031.Key.group == 1)
					.Select(_ => new
					{
						x = new
						{
							@value = _.Key.key.@value,
							// cast int? to int?
							id = (int?)_.Key.key.id
						},
						y = _.Average(auto16033 => auto16033.ID)
					})
					.OrderByDescending(_ => _.x.value)
					.Take(1000);

				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(1, result[0].y);
				Assert.IsNull(result[0].x.value);
				Assert.IsNull(result[0].x.id);
			}
		}

		[Test]
		public void GroupByWithToNullable([DataSources(ProviderName.MySql, TestProvName.MySql57)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person
					.GroupBy(_ => new
					{
						group = _.ID,
						key = new
						{
							id = Sql.ToNullable(_.Patient.PersonID),
							value = _.Patient.Diagnosis
						}
					})
					.Having(auto16031 => auto16031.Key.group == 1)
					.Select(_ => new
					{
						x = new
						{
							@value = _.Key.key.@value,
							id = _.Key.key.id
						},
						y = _.Average(auto16033 => auto16033.ID)
					})
					.OrderByDescending(_ => _.x.value)
					.Take(1000);

				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(1, result[0].y);
				Assert.IsNull(result[0].x.value);
				Assert.IsNull(result[0].x.id);
			}
		}
	}
}
