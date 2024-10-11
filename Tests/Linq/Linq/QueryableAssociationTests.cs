using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
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
			public string? OwnerStr { get; set; }

			[Association(QueryExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
			public SomeOtherEntity? Other { get; set; }

			[Association(QueryExpressionMethod = nameof(OtherImpl), CanBeNull = true)]
			public List<SomeOtherEntity> Others { get; set; } = new List<SomeOtherEntity>();

			public SomeOtherEntity?      OtherMapped  { get; set; }
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
			public SomeOtherEntity? OtherFromSql { get; set; }

			private static Expression<Func<SomeEntity, IDataContext, IQueryable<SomeOtherEntity>>> OtherFromSqlImpl()
			{
				return (e, db) => db.FromSql<SomeOtherEntity>($"dbo.fn_SomeFunction({e.Id})").Take(1);
			}

			#region Equality Members

			protected bool Equals(SomeEntity other)
			{
				return Id == other.Id && string.Equals(OwnerStr, other.OwnerStr) && Equals(Other, other.Other);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
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
			public string? StrValue { get; set; }

			protected bool Equals(SomeOtherEntity other)
			{
				return Id == other.Id && string.Equals(StrValue, other.StrValue);
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;
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
			var builder = new FluentMappingBuilder(new MappingSchema());

			builder.Entity<SomeEntity>().Association(e => e.OtherMapped,
				(e, db) => db.GetTable<SomeOtherEntity>().With("NOLOCK").Where(se => se.Id == e.Id));

			builder.Entity<SomeEntity>().Association(e => e.OthersMapped,
				(e, db) => db.GetTable<SomeOtherEntity>().With("NOLOCK").Where(se => se.Id == e.Id));

			builder.Build();

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
						V1 = e.Other!.StrValue + "_B",
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
						V1 = e.Other!.StrValue + "_B",
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

			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable(entities))
			using (db.CreateLocalTable(others))
			{
				CreateFunction(db, "SomeOtherEntity");
				var query = db.GetTable<SomeEntity>().With("NOLOCK")
					.LoadWith(e => e.Other)
					.LoadWith(e => e.Others)
					.LoadWith(e => e.OthersFromSql)
					.Take(2);

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
				}).ToArray();

				AreEqualWithComparer(expected, result);
				DropFunction(db);
			}
		}


		public class SomeTableType
		{
			public int LargeNumberEntityId { get; set; }
			public int Value               { get; set; }
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
			public SomeTableType SomeValue { get; set; } = null!;

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
		[ThrowsForProvider(typeof(LinqException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.Error_Correlated_Subqueries)]
		public void AssociationFromSqlTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable<FewNumberEntity>())
			using (db.CreateLocalTable<LargeNumberEntity>())
			using (db.CreateLocalTable<SomeTableType>("SomeTable"))
			{
				var tasksQuery = db.GetTable<LargeNumberEntity>();

				var filtered = db.GetTable<FewNumberEntity>().Where(x => x.UserId == 123);

				var final = filtered.InnerJoin(tasksQuery, (x, y) => x.Id == y.Id, (x, y) => y);

				var q = final.Select(x => new
				{
					x.Id,
					x.SomeValue.Value
				});

				TestContext.Out.WriteLine(q.ToString());

				var select = q.GetSelectQuery();

				// Ensure that cross apply inlined in query
				Assert.That(select.Select.From.Tables[0].Joins, Has.Count.EqualTo(2));
			}
		}

		public interface ITreeItem
		{
			int Id { get; set; }
			int? ParentId { get; set; }
			IList<TreeItem> Children { get; set; }
			TreeItem? Parent { get; set; }
		}

		[Table("TreeItem")]
		public class TreeItem : ITreeItem
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public int? ParentId { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ParentId))]
			public IList<TreeItem> Children { get; set; } = null!;

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(Id))]
			public TreeItem? Parent { get; set; }
		}

		[Test]
		public void AssociationFromInterfaceInGenericMethod([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context, GetMapping()))
			using (db.CreateLocalTable<TreeItem>())
			{
				var treeItems = db.GetTable<TreeItem>();

				DoGeneric(treeItems);
			}
		}

		void DoGeneric<T>(ITable<T> treeItems) where T: ITreeItem
		{
			var query1 = treeItems
				.Where(x => x.Children.Any());

			var result1 = query1.ToArray();

			var query2 = from t in treeItems
				where t.Parent!.Id > 0
				select t.Children;

			var result2 = query2.ToArray();
		}

		public class Entity
		{
			[Column]
			public int Id { get; set; }

			[Association(QueryExpressionMethod = nameof(Entity2LanguageExpr), CanBeNull = true)]
			public Entity2Language? Entity2Language { get; set; }

			public static Expression<Func<Entity, IDataContext, IQueryable<Entity2Language>>> Entity2LanguageExpr()
			{
				return (e, db) => db
					.GetTable<Entity2Language>()
					.Where(x => x.EntityId == e.Id)
					.Take(1);
			}
		}

		public class Entity2Language
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int EntityId { get; set; }

			[Column]
			public int LanguageId { get; set; }

			[Association(ThisKey = nameof(LanguageId), OtherKey = nameof(QueryableAssociationTests.Language.Id), CanBeNull = false)]
			public Language Language { get; set; } = null!;
		}

		[Test]
		public void SelectAssociations([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new Entity {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Entity2Language {Id = 1, EntityId = 1, LanguageId = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"}
			}))
			{
				var value = db
					.GetTable<Entity>()
					.Select(x => new
					{
						// This works
						EntityId = x.Id,
						x.Entity2Language!.LanguageId,
						// This caused exception
						LanguageName = x.Entity2Language.Language.Name
					})
					.First();

				Assert.Multiple(() =>
				{
					Assert.That(value.EntityId, Is.EqualTo(1));
					Assert.That(value.LanguageId, Is.EqualTo(1));
					Assert.That(value.LanguageName, Is.EqualTo("English"));
				});
			}
		}

		sealed class EntityWithUser1
		{
			[Column]
			public int UserId { get; set; }

			[ExpressionMethod(nameof(BelongsToCurrentUserExpr))]
			public bool BelongsToCurrentUser { get; set; }

			public static Expression<Func<EntityWithUser1, CustomDataConnection, bool>> BelongsToCurrentUserExpr()
			{
				return (e, db) => e.UserId == db.CurrentUserId;
			}
		}

		sealed class EntityWithUser2
		{
			[Column]
			public int UserId { get; set; }

			[ExpressionMethod(nameof(BelongsToCurrentUserExpr))]
			public bool BelongsToCurrentUser { get; set; }

			public static Expression<Func<EntityWithUser2, CustomDataContext, bool>> BelongsToCurrentUserExpr()
			{
				return (e, db) => e.UserId == db.CurrentUserId;
			}
		}

		sealed class EntityWithUser3
		{
			[Column]
			public int UserId { get; set; }

			[ExpressionMethod(nameof(BelongsToCurrentUserExpr))]
			public bool BelongsToCurrentUser { get; set; }

			public static Expression<Func<EntityWithUser3, ICustomContext, bool>> BelongsToCurrentUserExpr()
			{
				return (e, db) => e.UserId == db.CurrentUserId;
			}
		}

		interface ICustomContext : IDataContext
		{
			int CurrentUserId { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2226")]
		public void TestPropertiesFromDataConnection([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2, 3)] int currentUser)
		{
			using (var db = new CustomDataConnection(context))
			using (db.CreateLocalTable(new[]
			{
				new EntityWithUser1 {UserId = 1},
				new EntityWithUser1 {UserId = 2},
				new EntityWithUser1 {UserId = 2},
				new EntityWithUser1 {UserId = 3},
				new EntityWithUser1 {UserId = 3},
				new EntityWithUser1 {UserId = 3},
			}))
			{
				db.CurrentUserId = currentUser;
				var count = db
					.GetTable<EntityWithUser1>()
					.Count(x => x.BelongsToCurrentUser);

				Assert.That(count, Is.EqualTo(currentUser));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2226")]
		public void TestPropertiesFromDataContext([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2, 3)] int currentUser)
		{
			using (var db = new CustomDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new EntityWithUser2 {UserId = 1},
				new EntityWithUser2 {UserId = 2},
				new EntityWithUser2 {UserId = 2},
				new EntityWithUser2 {UserId = 3},
				new EntityWithUser2 {UserId = 3},
				new EntityWithUser2 {UserId = 3},
			}))
			{
				db.CurrentUserId = currentUser;
				var count = db
					.GetTable<EntityWithUser2>()
					.Count(x => x.BelongsToCurrentUser);

				Assert.That(count, Is.EqualTo(currentUser));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2226")]
		public void TestPropertiesFromDataConnectionInterface([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2, 3)] int currentUser)
		{
			using (var db = new CustomDataConnection(context))
			using (db.CreateLocalTable(new[]
			{
				new EntityWithUser3 {UserId = 1},
				new EntityWithUser3 {UserId = 2},
				new EntityWithUser3 {UserId = 2},
				new EntityWithUser3 {UserId = 3},
				new EntityWithUser3 {UserId = 3},
				new EntityWithUser3 {UserId = 3},
			}))
			{
				db.CurrentUserId = currentUser;
				var count = db
					.GetTable<EntityWithUser3>()
					.Count(x => x.BelongsToCurrentUser);

				Assert.That(count, Is.EqualTo(currentUser));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2226")]
		public void TestPropertiesFromDataContextInterface([IncludeDataSources(false, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values(1, 2, 3)] int currentUser)
		{
			using (var db = new CustomDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new EntityWithUser3 {UserId = 1},
				new EntityWithUser3 {UserId = 2},
				new EntityWithUser3 {UserId = 2},
				new EntityWithUser3 {UserId = 3},
				new EntityWithUser3 {UserId = 3},
				new EntityWithUser3 {UserId = 3},
			}))
			{
				db.CurrentUserId = currentUser;
				var count = db
					.GetTable<EntityWithUser3>()
					.Count(x => x.BelongsToCurrentUser);

				Assert.That(count, Is.EqualTo(currentUser));
			}
		}

		sealed class CustomDataConnection : DataConnection, ICustomContext
		{
			public CustomDataConnection(string? configurationString) : base(configurationString)
			{
			}

			public int CurrentUserId { get; set; }
		}

		sealed class CustomDataContext : DataContext, ICustomContext
		{

			public CustomDataContext(string? configurationString) : base(configurationString)
			{
			}

			public int CurrentUserId { get; set; }
		}

		public class UserGroup
		{
			[Column]
			public int Id { get; set; }

			[Association(QueryExpressionMethod = nameof(UsersWithLanguageExpression))]
			public IQueryable<User> UsersWithLanguage(IDataContext db, int languageId)
			{
				return (_usersWithLanguageExpression ??= UsersWithLanguageExpression().CompileExpression())(this, db, languageId);
			}

			[ExpressionMethod(nameof(UsersWithLanguageExpression))]
			public IQueryable<User> UsersWithLanguageEM(IDataContext db, int languageId)
			{
				return (_usersWithLanguageExpression ??= UsersWithLanguageExpression().CompileExpression())(this, db, languageId);
			}

			public static Expression<Func<UserGroup, IDataContext, int, IQueryable<User>>> UsersWithLanguageExpression()
			{
				return (p, db, languageId) => db
					.GetTable<User>()
					.Where(x => x.UserGroupId == p.Id && x.LanguageId == languageId);
			}

			[Association(QueryExpressionMethod = nameof(UsersWithLanguageLikeExpression))]
			public IQueryable<User> UsersWithLanguageLike(IDataContext db, string language)
			{
				return (_usersWithLanguageLikeExpression ??= UsersWithLanguageLikeExpression().CompileExpression())(this, db, language);
			}

			public static Expression<Func<UserGroup, IDataContext, string, IQueryable<User>>> UsersWithLanguageLikeExpression()
			{
				return (p, db, language) => db
					.GetTable<User>()
					.Where(x => x.UserGroupId == p.Id &&
					            x.Language!.Name!.Contains(language.Replace("_", string.Empty)));
			}

			private static Func<UserGroup, IDataContext, string, IQueryable<User>>? _usersWithLanguageLikeExpression;

			[Association(QueryExpressionMethod = nameof(FirstUserWithMultipleParametersExpression), CanBeNull = true)]
			public User? FirstUserWithMultipleParameters(IDataContext db, int parameter1, string parameter2, decimal parameter3)
			{
				return (_firstUserWithMultipleParametersExpression ??=
						FirstUserWithMultipleParametersExpression().CompileExpression()
					)(this, db, parameter1, parameter2, parameter3).FirstOrDefault();
			}


			public static Expression<Func<UserGroup, IDataContext, int, string?, decimal,  IQueryable<User>>> FirstUserWithMultipleParametersExpression()
			{
				return (p,db, _, __, ___) => db
					.GetTable<User>()
					.Where(x => x.UserGroupId == p.Id)
					.Take(1);
			}

			private static Func<UserGroup, IDataContext, int, string?, decimal, IQueryable<User>>? _firstUserWithMultipleParametersExpression;

			private static Func<UserGroup, IDataContext, int, IQueryable<User>>? _usersWithLanguageExpression;


			[Association(QueryExpressionMethod = nameof(FirstUserWithLanguageExpression), CanBeNull = true)]
			public User? FirstUsersWithLanguage(IDataContext db, int languageId)
			{
				return (_firstUserWithLanguageExpression ??= FirstUserWithLanguageExpression().CompileExpression())(this, db, languageId).FirstOrDefault();
			}

			public static Expression<Func<UserGroup, IDataContext, int, IQueryable<User>>> FirstUserWithLanguageExpression()
			{
				return (p, db, languageId) => db
					.GetTable<User>()
					.Where(x => x.UserGroupId == p.Id && x.LanguageId == languageId)
					.Take(1);
			}

			private static Func<UserGroup, IDataContext, int, IQueryable<User>>? _firstUserWithLanguageExpression;
		}

		public class User
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int UserGroupId { get; set; }

			[Association(ThisKey = nameof(UserGroupId), OtherKey = nameof(QueryableAssociationTests.UserGroup.Id), CanBeNull = false)]
			public UserGroup UserGroup { get; set; } = null!;

			[Column]
			public int LanguageId { get; set; }

			[Association(ThisKey = nameof(LanguageId), OtherKey = nameof(QueryableAssociationTests.Language.Id), CanBeNull = true)]
			public Language? Language { get; set; }
		}

		public class Language
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Test]
		public void TestOneToOneAssociation([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new UserGroup {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new User {Id = 1, UserGroupId = 1, LanguageId = 1},
				new User {Id = 2, UserGroupId = 1, LanguageId = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"},
				new Language {Id = 2, Name = "French"}
			}))
			{
				var data = db
					.GetTable<UserGroup>()
					.Select(x => new
					{
						x.Id,
						FirstUserId = x.FirstUsersWithLanguage(db, 1)!.Id,
						LanguageName = x.FirstUsersWithLanguage(db, 1)!.Language!.Name
					})
					.First();

				Assert.Multiple(() =>
				{
					Assert.That(data.FirstUserId, Is.EqualTo(1));
					Assert.That(data.LanguageName, Is.EqualTo("English"));
				});
			}
		}

		[Test]
		public void TestOneToOneAssociationChained([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new UserGroup {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new User {Id = 1, UserGroupId = 1, LanguageId = 1},
				new User {Id = 2, UserGroupId = 1, LanguageId = 1},
				new User {Id = 3, UserGroupId = 1, LanguageId = 2}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"},
				new Language {Id = 2, Name = "French"}
			}))
			{
				var data = db
					.GetTable<UserGroup>()
					.Select(x => new
					{
						x.Id,
						FirstUserId  = x
							.FirstUsersWithLanguage(db, 1)!
							.UserGroup
							.FirstUsersWithLanguage(db, 2)!
							.Id
					})
					.First();

				Assert.That(data.FirstUserId, Is.EqualTo(3));
			}
		}

		[Test]
		public void TestOneToOneAssociationTransformParameter([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new UserGroup {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new User {Id = 1, UserGroupId = 1, LanguageId = 1},
				new User {Id = 2, UserGroupId = 1, LanguageId = 1},
				new User {Id = 3, UserGroupId = 1, LanguageId = 2}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"},
				new Language {Id = 2, Name = "French"}
			}))
			{
				var data = db
					.GetTable<UserGroup>()
					.Select(x => new
					{
						x.Id,
						LanguagesWithEnCount  = x.UsersWithLanguageLike(db, "_En").Count(),
						LanguagesWithLisCount = x.UsersWithLanguageLike(db, "Lis").Count()
					})
					.First();

				Assert.Multiple(() =>
				{
					Assert.That(data.LanguagesWithEnCount, Is.EqualTo(IsCaseSensitiveDB(context) ? 2 : 3));
					Assert.That(data.LanguagesWithLisCount, Is.EqualTo(IsCaseSensitiveDB(context) ? 0 : 2));
				});
			}
		}

		[Test]
		public void TestOneToOneAssociationMultipleParameters([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new UserGroup {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new User {Id = 1, UserGroupId = 1, LanguageId = 1},
				new User {Id = 2, UserGroupId = 1, LanguageId = 1},
				new User {Id = 3, UserGroupId = 1, LanguageId = 2}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"},
				new Language {Id = 2, Name = "French"}
			}))
			{
				var data = db
					.GetTable<UserGroup>()
					.Select(x => new
					{
						x.Id,
						FirstUserId = x.FirstUserWithMultipleParameters(db, default, string.Empty, default)!.Id
					})
					.First();

				Assert.That(data.FirstUserId, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestOneToManyAssociation([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new UserGroup {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new User {Id = 1, UserGroupId = 1, LanguageId = 1},
				new User {Id = 2, UserGroupId = 1, LanguageId = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"},
				new Language {Id = 2, Name = "French"}
			}))
			{
				var data = db
					.GetTable<UserGroup>()
					.Select(x => new
					{
						x.Id,
						EnglishUserCount = x.UsersWithLanguage(db, 1).Count(),
						FrenchhUserCount = x.UsersWithLanguage(db, 2).Count()
					})
					.First();

				Assert.Multiple(() =>
				{
					Assert.That(data.EnglishUserCount, Is.EqualTo(2));
					Assert.That(data.FrenchhUserCount, Is.EqualTo(0));
				});
			}
		}

		[Test]
		public void TestOneToManyAssociationEM([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(new[]
			{
				new UserGroup {Id = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new User {Id = 1, UserGroupId = 1, LanguageId = 1},
				new User {Id = 2, UserGroupId = 1, LanguageId = 1}
			}))
			using (db.CreateLocalTable(new[]
			{
				new Language {Id = 1, Name = "English"},
				new Language {Id = 2, Name = "French"}
			}))
			{
				var data = db
					.GetTable<UserGroup>()
					.Select(x => new
					{
						x.Id,
						EnglishUserCount = x.UsersWithLanguageEM(db, 1).Count(),
						FrenchhUserCount = x.UsersWithLanguageEM(db, 2).Count()
					})
					.First();

				Assert.Multiple(() =>
				{
					Assert.That(data.EnglishUserCount, Is.EqualTo(2));
					Assert.That(data.FrenchhUserCount, Is.EqualTo(0));
				});
			}
		}

		[Table]
		public class PropertyHistory
		{
			[Column] public string? DocumentNo { get; set; }

			[Association(QueryExpressionMethod = nameof(CustomerApplicationImpl), CanBeNull = true)]
			public CustomerApplication? CustomerApplication { get; set; }

			static Expression<Func<PropertyHistory, IDataContext, IQueryable<CustomerApplication>>> CustomerApplicationImpl() =>
			  (e, dc) => dc.GetTable<CustomerApplication>().Where(a => a.Id == Sql.Convert(Sql.Types.Int, e.DocumentNo)).Take(1);
		}

		[Table]
		public class CustomerApplication
		{
			[PrimaryKey] public int Id { get; set; }
		}

		[Test]
		public void Issue3525ConvertInQuery([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var db           = GetDataContext(context);
			using var history      = db.CreateLocalTable<PropertyHistory>();
			using var applications = db.CreateLocalTable<CustomerApplication>();

			history.Select(i =>
				new
				{
					DocNo         = i.DocumentNo,
					ApplicationId = i.CustomerApplication!.Id,
				})
				.ToList();
		}
	}
}
