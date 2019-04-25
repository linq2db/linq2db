using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Tools;

namespace Tests.Playground
{
	[TestFixture]
	public class QueryableAssociationTests : TestBase
	{
		[Table]
		public class SomeEntity
		{
			[Column]
		    public int Id { get; set; }

			[Column]
			public string OwnerStr { get; set; }

			[Association(QueryExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
			public SomeOtherEntity Other { get; set; }

			[Association(QueryExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
			public List<SomeOtherEntity> Others { get; set; } = new List<SomeOtherEntity>();

			public SomeOtherEntity       OtherMapped  { get; set; }
			public List<SomeOtherEntity> OthersMapped { get; set; } = new List<SomeOtherEntity>();

			private static Expression<Func<SomeEntity, IDataContext, IQueryable<SomeOtherEntity>>> OtherImpl()
			{
				return (e, db) => db.GetTable<SomeOtherEntity>().Where(se => se.Id == e.Id)
					.Select(o => new SomeOtherEntity { Id = o.Id, StrValue = o.StrValue + "_A" })
					.Take(1);
			}

			[Association(QueryExpressionMethod = nameof(OthersFromSqlImpl), CanBeNull = true)]
			public List<SomeOtherEntity> OthersFromSql { get; set; } = new List<SomeOtherEntity>();

			private static Expression<Func<SomeEntity, IDataContext, IQueryable<SomeOtherEntity>>> OthersFromSqlImpl()
			{
				return (e, db) => db.FromSql<SomeOtherEntity>($"dbo.fn_SomeFunction({e.Id})");
			}

			[Association(QueryExpressionMethod = nameof(OtherFromSqlImpl), CanBeNull = true)]
			public SomeOtherEntity OtherFromSql { get; set; }

			private static Expression<Func<SomeEntity, IDataContext, IQueryable<SomeOtherEntity>>> OtherFromSqlImpl()
			{
				return (e, db) => db.FromSql<SomeOtherEntity>($"dbo.fn_SomeFunction({e.Id})").Take(1);
			}

			#region Equality Members

			protected bool Equals(SomeEntity other)
			{
				return Id == other.Id && string.Equals(OwnerStr, other.OwnerStr) && Equals(Other, other.Other);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((SomeEntity)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = Id;
					hashCode = (hashCode * 397) ^ (OwnerStr != null ? OwnerStr.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (Other != null ? Other.GetHashCode() : 0);
					return hashCode;
				}
			}

			#endregion
		}

		public class SomeOtherEntity
		{
			[Column]
		    public int Id { get; set; }

			[Column]
			public string StrValue { get; set; }

			protected bool Equals(SomeOtherEntity other)
			{
				return Id == other.Id && string.Equals(StrValue, other.StrValue);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((SomeOtherEntity)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Id * 397) ^ (StrValue != null ? StrValue.GetHashCode() : 0);
				}
			}
		}

		static SomeEntity[] GenerateOwnerEntities()
		{
			return Enumerable.Range(1, 10)
				.Select(i => new SomeEntity
				{
					Id = i,
					OwnerStr = "Own_" + i
				}).ToArray();
		}

		static SomeOtherEntity[] GenerateChildEntities()
		{
			return Enumerable.Range(1, 10)
				.Select(i => new SomeOtherEntity
				{
					Id = i,
					StrValue = "Str_" + i
				}).ToArray();
		}

		static (SomeEntity[], SomeOtherEntity[]) GenerateEntities()
		{
			var entities = GenerateOwnerEntities();
			var others = GenerateChildEntities();

			var pairs = from e in entities
				join o in others on e.Id equals o.Id
				select new { e, o };

			foreach (var pair in pairs)
			{
				var child = new SomeOtherEntity
				{
					Id       = pair.o.Id,
					StrValue = pair.o.StrValue + "_A"
				};

				pair.e.Other       = child;
				pair.e.OtherMapped = child;

				pair.e.Others.Add(child);
				pair.e.OthersMapped.Add(child);

				pair.e.OtherFromSql = pair.o;
				pair.e.OthersFromSql.Add(pair.o);
				pair.e.OthersFromSql.Add(pair.o);
			}

			return (entities, others);
		}

		static MappingSchema GetMapping()
		{
			var builder = new MappingSchema().GetFluentMappingBuilder();

			builder.Entity<SomeEntity>().Association(e => e.OtherMapped,
				(e, db) => db.GetTable<SomeOtherEntity>().With("NOLOCK").Where(se => se.Id == e.Id));

			builder.Entity<SomeEntity>().Association(e => e.OthersMapped,
				(e, db) => db.GetTable<SomeOtherEntity>().With("NOLOCK").Where(se => se.Id == e.Id));

			return builder.MappingSchema;
		}

		[Test]
		public void AssociationProjectionTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (var db = GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				var query = from e in db.GetTable<SomeEntity>()
					select new
					{
						V1 = e.Other.StrValue + "_B",
						V2 = Sql.AsSql(e.Other.StrValue + "_C"),
						e.Other,
						Inner = new
						{
							e.Other.Id,
							e.Other.StrValue
						}
					};

				var expectedQuery = from e in entities
					select new
					{
						V1 = e.Other.StrValue + "_B",
						V2 = e.Other.StrValue + "_C",
						e.Other,
						Inner = new
						{
							e.Other.Id,
							e.Other.StrValue
						}
					};

				var result = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void AssociationObjectTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (var db = GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				var query = from e in db.GetTable<SomeEntity>()
					select new
					{
						e.Other
					};

				var expectedQuery = from e in entities
					select new
					{
						e.Other,
					};

				var result = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result);
			}
		}

		static void CreateFunction(DataConnection dc, string tableName)
		{
			DropFunction(dc);

			dc.Execute($@"CREATE FUNCTION fn_SomeFunction (@id AS INT)
RETURNS TABLE
AS RETURN
  SELECT * FROM [{tableName}] WHERE Id = @id
  UNION ALL
  SELECT * FROM [{tableName}] WHERE Id = @id
"
			);
		}

		static void DropFunction(DataConnection dc)
		{
			dc.Execute(@"
				IF EXISTS (SELECT * FROM sysobjects WHERE id = object_id(N'fn_SomeFunction') AND xtype IN (N'FN', N'IF', N'TF'))
					DROP FUNCTION fn_SomeFunction");
		}

		[Test]
		public void AssociationObjectTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (new AllowMultipleQuery())
			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable("SomeTable", entities))
			using (db.CreateLocalTable(others))
			{
				CreateFunction(db, "SomeOtherEntity");
				var q1 = db.GetTable<SomeEntity>().TableName("SomeTable").With("NOLOCK").Where(x => x.Id == 123 && x.OthersFromSql.Any()).ToArray();
				var q2 = db.GetTable<SomeEntity>().TableName("SomeTable").With("NOLOCK").LoadWith(t => t.OthersFromSql).ToArray();
				DropFunction(db);
			}
		}

		[Test]
		public void AssociationLoadWithTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (var db = GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				var query = db.GetTable<SomeEntity>().LoadWith(e => e.Other).LoadWith(e => e.OtherMapped);

				var result = query.ToArray();
				var expected = entities;

				AreEqual(expected, result);
			}
		}

		[Test]
		public void AssociationOneToManyTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (var db = GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				var query = from e in db.GetTable<SomeEntity>()
					from o in e.Others
					select new
					{
						o,
						e.Id
					};

				var expectedQuery = from e in entities
					from o in e.Others
					select new
					{
						o,
						e.Id
					};

				var result = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void AssociationOneToManyTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (var db = GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				var query = from e in db.GetTable<SomeEntity>()
					from o in e.Others
					select o;

				var expectedQuery = from e in entities
					from o in e.Others
					select o;

				var result = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void AssociationOneToManyTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (var db = GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				var query = from e in db.GetTable<SomeEntity>()
					from o in e.Others
					select o.StrValue;

				var expectedQuery = from e in entities
					from o in e.Others
					select o.StrValue;

				var result = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void AssociationOneToManyLazy([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (new AllowMultipleQuery())
			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				CreateFunction(db, "SomeOtherEntity");
				var query = db.GetTable<SomeEntity>().With("NOLOCK").LoadWith(e => e.Other).LoadWith(e => e.Others).LoadWith(e => e.OthersFromSql).Take(2);

				var expectedQuery = entities.Take(2);

				var result = query.ToArray();
				var expected = expectedQuery.ToArray();

				AreEqual(expected, result);
				DropFunction(db);
			}
		}

		[Test]
		public void AssociationOneToManyLazyProjection([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			var (entities, others) = GenerateEntities();

			using (new AllowMultipleQuery())
			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				CreateFunction(db, "SomeOtherEntity");
				var query = db.GetTable<SomeEntity>().With("NOLOCK").Take(2).Select(e => new
				{
					e.Id,
					Others        = e.Others.ToArray(),
					Other         = e.Other,
					OthersFromSql = e.OthersFromSql.ToArray(),
					OtherFromSql  = e.OtherFromSql
				});

				var result = query.ToArray();

				var expected = entities.Take(2).Select(e => new
				{
					e.Id,
					Others        = e.Others.ToArray(),
					Other         = e.Other,
					OthersFromSql = e.OthersFromSql.ToArray(),
					OtherFromSql  = e.OtherFromSql
				});

				AreEqualWithComparer(expected, result);
				DropFunction(db);
			}
		}


		public class SomeTableType
		{
			public int Value { get; set; }
		}

		[Table("FewNumberEntity")]
		public class FewNumberEntity
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public int UserId { get; set; }
		}

		[Table("LargeNumberEntity")]
		public class LargeNumberEntity
		{
			[Column]
			public int Id { get; set; }

			[Association(QueryExpressionMethod = nameof(GetSomeValue), CanBeNull = false)]
			public SomeTableType SomeValue { get; set; }

			private static Expression<Func<LargeNumberEntity, IDataContext, IQueryable<SomeTableType>>> GetSomeValue()
			{
				return (t, db) => db.FromSql<SomeTableType>($@"SELECT 
	COUNT(*) as [Value] 
FROM 
	[dbo].[SomeTable] [st] WITH(NOLOCK) 
WHERE 
	[st].[LargeNumberEntityId]={t.Id}");
			}
		}

		[Test]
		public void AssociationFromSqlTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable<FewNumberEntity>())
			using (db.CreateLocalTable<LargeNumberEntity>())
			{
				var tasksQuery = db.GetTable<LargeNumberEntity>();

				var filtered = db.GetTable<FewNumberEntity>().Where(x => x.UserId == 123);

				var final = filtered.InnerJoin(tasksQuery, (x, y) => x.Id == y.Id, (x, y) => y);

				var q = final.Select(x => new
				{
					x.Id,
					x.SomeValue.Value
				});

				var select = q.GetSelectQuery();

				// Ensure that cross apply inlined in query
				Assert.AreEqual(2, select.Select.From.Tables[0].Joins.Count);

				Console.WriteLine(q.ToString());
			}
		}

	}
}
