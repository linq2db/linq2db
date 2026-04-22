using System;
using System.Linq;
#if NET7_0_OR_GREATER
using System.Text.RegularExpressions;
#endif

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	[TestFixture]
	public class UpdateFromTests : TestBase
	{
		[Table]
		public partial class UpdatedEntities
		{
			[PrimaryKey, NotNull] public int id { get; set; }
			[Column] public int Value1 { get; set; }
			[Column] public int Value2 { get; set; }
			[Column] public int Value3 { get; set; }

			[Column] public int? RelationId { get; set; }

			[Association(ThisKey = "RelationId", OtherKey = "id")]
			public UpdateRelation? Relation;

		}

		[Table]
		public class UpdateRelation
		{
			[PrimaryKey, NotNull] public int id { get; set; }
			[Column] public int RelatedValue1 { get; set; }
			[Column] public int RelatedValue2 { get; set; }
			[Column] public int RelatedValue3 { get; set; }
		}

		[Table]
		public partial class NewEntities
		{
			[PrimaryKey, NotNull] public int id { get; set; }
			[Column] public int Value1 { get; set; }
			[Column] public int Value2 { get; set; }
			[Column] public int Value3 { get; set; }
		}

		private UpdatedEntities[] GenerateData()
		{
			return new UpdatedEntities[]
			{
				new UpdatedEntities {id = 0, Value1 = 01, Value2 = 01, Value3 = 03, RelationId = 0},
				new UpdatedEntities {id = 1, Value1 = 11, Value2 = 12, Value3 = 13, RelationId = 1},
				new UpdatedEntities {id = 2, Value1 = 21, Value2 = 22, Value3 = 23, RelationId = 2},
				new UpdatedEntities {id = 3, Value1 = 31, Value2 = 32, Value3 = 33, RelationId = 3},
			};
		}

		private UpdateRelation[] GenerateRelationData()
		{
			return new UpdateRelation[]
			{
				new UpdateRelation {id = 0, RelatedValue1 = 01, RelatedValue2 = 02, RelatedValue3 = 03},
				new UpdateRelation {id = 1, RelatedValue1 = 11, RelatedValue2 = 12, RelatedValue3 = 13},
				new UpdateRelation {id = 2, RelatedValue1 = 21, RelatedValue2 = 22, RelatedValue3 = 23},
				new UpdateRelation {id = 3, RelatedValue1 = 31, RelatedValue2 = 32, RelatedValue3 = 33},
			};
		}

		private NewEntities[] GenerateNewData()
		{
			return new NewEntities[]
			{
				new NewEntities {id = 0, Value1 = 0, Value2 = 0, Value3 = 0},
				new NewEntities {id = 1, Value1 = 1, Value2 = 1, Value3 = 1},
				new NewEntities {id = 2, Value1 = 2, Value2 = 2, Value3 = 2},
				new NewEntities {id = 3, Value1 = 3, Value2 = 3, Value3 = 3},
			};
		}

		[Obsolete("Remove test after API removed")]
		[Test]
		public void UpdateTestWhereOld(
			[DataSources(TestProvName.AllMySql, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var tempTable = db.CreateLocalTable(newData);
			var someId = 100;

			var recordsToUpdate =
					from c in forUpdates
					from t in tempTable
					where t.id == c.id && t.id != someId
					select new {c, t};

			var int1 = 11;
			var int2 = 22;
			var int3 = 33;

			recordsToUpdate.Update(forUpdates, v => new UpdatedEntities()
			{
				Value1 = v.c.Value1 * v.t.Value1 * int1,
				Value2 = v.c.Value2 * v.t.Value2 * int2,
				Value3 = v.c.Value3 * v.t.Value3 * int3,
			});

			var actual = forUpdates.Select(v => new
			{
				Id = v.id,
				Value1 = v.Value1,
				Value2 = v.Value2,
				Value3 = v.Value3,
			});

			var expected = data.Join(newData, c => c.id, t => t.id, (c, t) => new { c, t })
					.Select(v => new
					{
						Id = v.c.id,
						Value1 = v.c.Value1 * v.t.Value1 * int1,
						Value2 = v.c.Value2 * v.t.Value2 * int2,
						Value3 = v.c.Value3 * v.t.Value3 * int3,
					});

			AreEqual(expected, actual);
		}

		[Test]
		public void UpdateTestWhere(
			[DataSources(TestProvName.AllMySql, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var tempTable = db.CreateLocalTable(newData);
			var someId = 100;

			var recordsToUpdate =
					from c in forUpdates
					from t in tempTable
					where t.id == c.id && t.id != someId
					select new {c, t};

			var int1 = 11;
			var int2 = 22;
			var int3 = 33;

			recordsToUpdate.Update(q => q.c, v => new UpdatedEntities()
			{
				Value1 = v.c.Value1 * v.t.Value1 * int1,
				Value2 = v.c.Value2 * v.t.Value2 * int2,
				Value3 = v.c.Value3 * v.t.Value3 * int3,
			});

			var actual = forUpdates.Select(v => new
			{
				Id = v.id,
				Value1 = v.Value1,
				Value2 = v.Value2,
				Value3 = v.Value3,
			});

			var expected = data.Join(newData, c => c.id, t => t.id, (c, t) => new { c, t })
					.Select(v => new
					{
						Id = v.c.id,
						Value1 = v.c.Value1 * v.t.Value1 * int1,
						Value2 = v.c.Value2 * v.t.Value2 * int2,
						Value3 = v.c.Value3 * v.t.Value3 * int3,
					});

			AreEqual(expected, actual);
		}

		[Test]
		public void UpdateTestJoin(
			[DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var tempTable = db.CreateLocalTable(newData);
			var someId = 100;

			var recordsToUpdate =
					from c in forUpdates
					from t in tempTable.InnerJoin(t => t.id == c.id)
					where t.id != someId
					select new {c, t};

			var int1 = 11;
			var int2 = 22;
			var int3 = 33;

			recordsToUpdate
				.Set(v => v.c.Value1, v => v.c.Value1 * v.t.Value1 * int1)
				.Set(v => v.c.Value2, v => v.c.Value2 * v.t.Value2 * int2)
				.Set(v => v.c.Value3, v => v.c.Value3 * v.t.Value3 * int3)
				.Update();

			var actual = forUpdates.Select(v => new
			{
				Id = v.id,
				Value1 = v.Value1,
				Value2 = v.Value2,
				Value3 = v.Value3,
			});

			var expected = data.Join(newData, c => c.id, t => t.id, (c, t) => new { c, t })
					.Select(v => new
					{
						Id = v.c.id,
						Value1 = v.c.Value1 * v.t.Value1 * int1,
						Value2 = v.c.Value2 * v.t.Value2 * int2,
						Value3 = v.c.Value3 * v.t.Value3 * int3,
					});

			AreEqual(expected, actual);
		}

		[Test]
		public void UpdateTestJoinSkip(
			[IncludeDataSources(
				TestProvName.AllSqlServer,
				TestProvName.AllPostgreSQL)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var tempTable = db.CreateLocalTable(newData);
			var someId = 100;

			var recordsToUpdate =
					from c in forUpdates
					from t in tempTable.InnerJoin(t => t.id == c.id)
					where t.id != someId
					select new {c, t};

			var int1 = 11;
			var int2 = 22;
			var int3 = 33;

			recordsToUpdate.OrderBy(v => v.c.id).Skip(2)
				.Set(v => v.c.Value1, v => v.c.Value1 * v.t.Value1 * int1)
				.Set(v => v.c.Value2, v => v.c.Value2 * v.t.Value2 * int2)
				.Set(v => v.c.Value3, v => v.c.Value3 * v.t.Value3 * int3)
				.Update();

			var actual = forUpdates.Select(v => new
			{
				Id = v.id,
				Value1 = v.Value1,
				Value2 = v.Value2,
				Value3 = v.Value3,
			});

			var expected = data.Join(newData, c => c.id, t => t.id, (c, t) => new { c, t })
					.Select(v => new
					{
						Id = v.c.id,
						Value1 = v.c.id > 1 ? v.c.Value1 * v.t.Value1 * int1 : v.c.Value1,
						Value2 = v.c.id > 1 ? v.c.Value2 * v.t.Value2 * int2 : v.c.Value2,
						Value3 = v.c.id > 1 ? v.c.Value3 * v.t.Value3 * int3 : v.c.Value3,
					});

			AreEqual(expected, actual);
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_UpdateWithTopOrderBy)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql, ErrorMessage = ErrorHelper.MySql.Error_SkipInUpdate)]
		public void UpdateTestJoinSkipTake(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, ProviderName.SqlCe)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var tempTable = db.CreateLocalTable(newData);
			var someId = 100;

			var recordsToUpdate =
					from c in forUpdates
					from t in tempTable.InnerJoin(t => t.id == c.id)
					where t.id != someId
					select new {c, t};

			var int1 = 11;
			var int2 = 22;
			var int3 = 33;

			recordsToUpdate.OrderBy(v => v.c.id).Skip(1).Take(2)
				.Set(v => v.c.Value1, v => v.c.Value1 * v.t.Value1 * int1)
				.Set(v => v.c.Value2, v => v.c.Value2 * v.t.Value2 * int2)
				.Set(v => v.c.Value3, v => v.c.Value3 * v.t.Value3 * int3)
				.Update();

			var actual = forUpdates.Select(v => new
			{
				Id = v.id,
				Value1 = v.Value1,
				Value2 = v.Value2,
				Value3 = v.Value3,
			});

			var expected = data.Join(newData, c => c.id, t => t.id, (c, t) => new { c, t })
					.Select(v => new
					{
						Id = v.c.id,
						Value1 = v.c.id > 0 && v.c.id < 3 ? v.c.Value1 * v.t.Value1 * int1 : v.c.Value1,
						Value2 = v.c.id > 0 && v.c.id < 3 ? v.c.Value2 * v.t.Value2 * int2 : v.c.Value2,
						Value3 = v.c.id > 0 && v.c.id < 3 ? v.c.Value3 * v.t.Value3 * int3 : v.c.Value3,
					});

			AreEqual(expected, actual);
		}

		[Test]
		public void UpdateTestJoinTake(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2005, TestProvName.AllMySql, TestProvName.AllClickHouse, ProviderName.SqlCe)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var tempTable = db.CreateLocalTable(newData);
			var someId = 100;

			var recordsToUpdate =
					from c in forUpdates
					from t in tempTable.InnerJoin(t => t.id == c.id)
					where t.id != someId
					select new {c, t};

			var int1 = 11;
			var int2 = 22;
			var int3 = 33;

			recordsToUpdate.Take(2)
				.Set(v => v.c.Value1, v => v.c.Value1 * v.t.Value1 * int1)
				.Set(v => v.c.Value2, v => v.c.Value2 * v.t.Value2 * int2)
				.Set(v => v.c.Value3, v => v.c.Value3 * v.t.Value3 * int3)
				.Update();

			var actual = forUpdates.Select(v => new
			{
				Id = v.id,
				Value1 = v.Value1,
				Value2 = v.Value2,
				Value3 = v.Value3,
			});

			var expected = data.Join(newData, c => c.id, t => t.id, (c, t) => new { c, t })
					.Select(v => new
					{
						Id = v.c.id,
						Value1 = v.c.id < 2 ? v.c.Value1 * v.t.Value1 * int1 : v.c.Value1,
						Value2 = v.c.id < 2 ? v.c.Value2 * v.t.Value2 * int2 : v.c.Value2,
						Value3 = v.c.id < 2 ? v.c.Value3 * v.t.Value3 * int3 : v.c.Value3,
					});

			AreEqual(expected, actual);
		}

		[Test]
		public void UpdateTestAssociation(
			[DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable(data);
			using var relations = db.CreateLocalTable(GenerateRelationData());

			var affected = forUpdates
					.Where(v => v.Relation!.RelatedValue1 == 11)
					.Set(v => v.Value1, v => v.Relation!.RelatedValue3)
					.Update();

			Assert.That(affected, Is.EqualTo(1));

			var updatedValue = forUpdates.Where(v => v.Relation!.RelatedValue1 == 11).Select(v => v.Value1).First();

			Assert.That(updatedValue, Is.EqualTo(13));
		}

		[Test]
		public void UpdateTestAssociationAsUpdatable(
			[DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable<UpdatedEntities>(data);
			using var relations = db.CreateLocalTable(GenerateRelationData());

			var query = forUpdates
					.Where(v => v.Relation!.RelatedValue1 == 11);

			var updatable = query.AsUpdatable();
			updatable = updatable.Set(v => v.Value1, v => v.Relation!.RelatedValue3);

			var affected = updatable.Update();

			Assert.That(affected, Is.EqualTo(1));

			var updatedValue = forUpdates.Where(v => v.Relation!.RelatedValue1 == 11).Select(v => v.Value1).First();

			Assert.That(updatedValue, Is.EqualTo(13));
		}

		[Test]
		public void UpdateTestAssociationSimple(
			[DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable<UpdatedEntities>(data);
			using var relations = db.CreateLocalTable(GenerateRelationData());

			var affected = forUpdates
					.Where(v => v.Relation!.RelatedValue1 == 11)
					.Set(v => v.Value1, v => v.Value1 + v.Value2 + v.Value3)
					.Set(v => v.Value2, v => v.Value1 + v.Value2 + v.Value3)
					.Set(v => v.Value3, v => 1)
					.Update();

			Assert.That(affected, Is.EqualTo(1));

			var updatedValue = forUpdates.Where(v => v.Relation!.RelatedValue1 == 11)
					.Select(v => new {v.Value1, v.Value2, v.Value3})
					.First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(updatedValue.Value1, Is.EqualTo(36));
				Assert.That(updatedValue.Value2, Is.EqualTo(36));
				Assert.That(updatedValue.Value3, Is.EqualTo(1));
			}
		}

		[Test]
		public void UpdateTestAssociationSimpleAsUpdatable(
			[DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using var db = GetDataContext(context);
			using var forUpdates = db.CreateLocalTable<UpdatedEntities>(data);
			using var relations = db.CreateLocalTable(GenerateRelationData());

			var query = forUpdates
					.Where(v => v.Relation!.RelatedValue1 == 11);

			var updatable = query.AsUpdatable();
			updatable = updatable.Set(v => v.Value1, v => v.Value1 + v.Value2 + v.Value3);
			updatable = updatable.Set(v => v.Value2, v => v.Value1 + v.Value2 + v.Value3);
			updatable = updatable.Set(v => v.Value3, v => 1);

			var affected = updatable.Update();

			Assert.That(affected, Is.EqualTo(1));

			var updatedValue = forUpdates.Where(v => v.Relation!.RelatedValue1 == 11)
					.Select(v => new {v.Value1, v.Value2, v.Value3})
					.First();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(updatedValue.Value1, Is.EqualTo(36));
				Assert.That(updatedValue.Value2, Is.EqualTo(36));
				Assert.That(updatedValue.Value3, Is.EqualTo(1));
			}
		}

		sealed class ParentTable
		{
			[PrimaryKey] public int Id { get; set; }
			public int Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildTable.ParentId))]
			public ChildTable[] Children { get; set; } = null!;
		}

		sealed class ChildTable
		{
			[PrimaryKey] public int Id { get; set; }
			public int? ParentId { get; set; }
			public int Value { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(ParentTable.Id), CanBeNull = true)]
			public ParentTable? Parent { get; set; }
		}

		[Test]
		public void UpdateParentTableFromChild(
			[DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			using var db = GetDataContext(context);

			using var parents = db.CreateLocalTable(
			[
				new ParentTable { Id = 1, Value = 1 },
				new ParentTable { Id = 2, Value = 2 },
				new ParentTable { Id = 3, Value = 3 }
			]);

			using var children = db.CreateLocalTable(
			[
				new ChildTable { Id = 1, ParentId = 1, Value = 1 },
				new ChildTable { Id = 2, ParentId = 2, Value = 2 },
				new ChildTable { Id = 3, ParentId = 3, Value = 3 }
			]);

			var query = 
				from c in children
				where c.Parent!.Id == 2
				select c.Parent;

			var updated  = query
				.Set(p => p.Value, p => p.Value * 10)
				.Update();

			Assert.That(updated, Is.EqualTo(1));

			var parentRecord = parents.First(p => p.Id == 2);
			Assert.That(parentRecord.Value, Is.EqualTo(20));
		}

		sealed class InsertFromWithConstantsTable
		{
			[PrimaryKey]
			public int Id { get; set; }
			public int? Value { get; set; }
			public string? Value1 { get; set; }
			public string? Value2 { get; set; }
			public string? Value3 { get; set; }
			public string? Value4 { get; set; }
		}

		[Test(Description = "Tests that client/duplicate columns not removed (v6.2.0 regression)")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedUpdate)]
		public void UpdateFromWithDuplicateSubqueryColumn_SingleOrDefault([DataSources(TestProvName.AllSqlCe, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<InsertFromWithConstantsTable>();

			var id1 = 1;

			tb
				.Select(r => new
				{
					r,
					Value1 = tb.Where(r => r.Id == id1).Select(r => r.Value3).SingleOrDefault(),
					Value2 = "string 1",
				})
				.Update(
					x => x.r,
					x => new InsertFromWithConstantsTable()
					{
						Value1 = x.Value1,
						Value2 = x.Value1,
						Value3 = x.Value2,
						Value4 = x.Value2,
					});
		}

		[Test(Description = "Tests that client/duplicate columns not removed (v6.2.0 regression)")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedUpdate)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_JoinToDerivedTableWithTakeInvalid)]
		public void UpdateFromWithDuplicateSubqueryColumn_FirstOrDefault([DataSources(TestProvName.AllSqlCe, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<InsertFromWithConstantsTable>();

			var id1 = 1;

			tb
				.Select(r => new
				{
					r,
					Value1 = tb.Where(r => r.Id == id1).Select(r => r.Value3).FirstOrDefault(),
					Value2 = "string 1",
				})
				.Update(
					x => x.r,
					x => new InsertFromWithConstantsTable()
					{
						Value1 = x.Value1,
						Value2 = x.Value1,
						Value3 = x.Value2,
						Value4 = x.Value2,
					});
		}

		[Table]
		sealed class UpdateSubquerySourceTable
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public string? FirstName { get; set; }
			[Column] public string? LastName { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5413")]
		public void UpdateFromSubqueryRowShouldBeOptimized([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL, TestProvName.AllInformix)] string context)
		{
			using var db = GetDataContext(context);

			using var _ = new DeletePerson(db);

			using var sourceTable = db.CreateLocalTable(
			[
				new UpdateSubquerySourceTable { Id = 1, FirstName = "FirstTooth", LastName = "FirstFairy" },
				new UpdateSubquerySourceTable { Id = 2, FirstName = "SecondTooth", LastName = "SecondFairy" },
				new UpdateSubquerySourceTable { Id = 3, FirstName = "ThirdTooth", LastName = "ThirdFairy" }
			]);

			var affectedCount = sourceTable
				.Where(x => x.Id == 1)
				.Set(
					x => Sql.Row(x.FirstName, x.LastName),
					x => (
						from s in db.SelectQuery(() => 1)
						from canChange in sourceTable.Where(t => t.Id == x.Id + 1).DefaultIfEmpty()
						select Sql.Row(
							canChange != null ? canChange.FirstName! : x.FirstName,
							canChange != null ? canChange.LastName!  : x.LastName
						)
					).Single()
				)
				.Update();

			affectedCount.ShouldBe(1);

			var records = sourceTable.OrderBy(x => x.Id).ToList();

			records[0].FirstName.ShouldBe("SecondTooth");
			records[0].LastName.ShouldBe("SecondFairy");

			records[1].FirstName.ShouldBe("SecondTooth");
			records[1].LastName.ShouldBe("SecondFairy");

			records[2].FirstName.ShouldBe("ThirdTooth");
			records[2].LastName.ShouldBe("ThirdFairy");
		}

		[Test(Description = "Row-setter with mixed literal + correlated values (regression for ProcessUpdateItemsWithRows column/value pairing)")]
		public void UpdateFromSubqueryRowMixedIndependentAndDependent([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracle, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);

			using var sourceTable = db.CreateLocalTable(
			[
				new UpdateSubquerySourceTable { Id = 1, FirstName = "FirstTooth",  LastName  = "FirstFairy"  },
				new UpdateSubquerySourceTable { Id = 2, FirstName = "SecondTooth", LastName  = "SecondFairy" },
				new UpdateSubquerySourceTable { Id = 3, FirstName = "ThirdTooth",  LastName  = "ThirdFairy"  }
			]);

			// (FirstName, LastName) = ("literal", (subquery on next row).LastName)
			// FirstName side is independent of other tables; LastName side depends on a
			// correlated subquery. Exercises the independent/dependent split in
			// ProcessUpdateItemsWithRows — earlier code emitted (col, col) = (col, col)
			// for the independent slot, dropping the literal value.
			var affectedCount = sourceTable
				.Where(x => x.Id == 1)
				.Set(
					x => Sql.Row(x.FirstName, x.LastName),
					x => Sql.Row(
						(string?)"literalFirst",
						sourceTable.Where(t => t.Id == x.Id + 1).Select(t => t.LastName).First()))
				.Update();

			affectedCount.ShouldBe(1);

			var records = sourceTable.OrderBy(x => x.Id).ToList();

			records[0].FirstName.ShouldBe("literalFirst");
			records[0].LastName .ShouldBe("SecondFairy");

			records[1].FirstName.ShouldBe("SecondTooth");
			records[1].LastName .ShouldBe("SecondFairy");

			records[2].FirstName.ShouldBe("ThirdTooth");
			records[2].LastName .ShouldBe("ThirdFairy");
		}

#if NET7_0_OR_GREATER
// net7.0 for Regex.Count support, this doesn't need to be tested on all frameworks anyway.
// Annoyingly trying to use Regex.Matches(..).Count suggests using Regex.Count,
// and those warnings are treated as errors :(

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5413")]
		public void UpdateFromSubqueryRowShouldRemainSimple([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using var db = GetDataContext(context);
			using var table1 = db.CreateLocalTable<NewEntities>();
			using var table2 = db.CreateLocalTable<UpdatedEntities>();
			using var table3 = db.CreateLocalTable<UpdateRelation>();

			table1
				.Where(u1 => u1.id == 7)
				.Set(
					u1 => Sql.Row(u1.Value1, u1.Value2),
					u1 => (
						from n2 in table2
						from n3 in table3.InnerJoin(n3 => n2.RelationId == n3.id)
						where n3.RelatedValue3 < 1000 && n2.id == u1.id
						select Sql.Row(n2.Value1, n3.RelatedValue2))
						.Single()
				)
				.Update();

			// Query above should look something like:
			// 		update NewEntities
			// 		set (value1, value2) = (
			// 				select n2.value1, n3.relatedValue2 
			// 				from UpdatedEntities n2
			// 				join UpdateRelation n3 on n2.relationId = n3.id
			//      		where n3.relatedValue3 < 1000 and n2.id = NewEntities.id)
			// 		where id = 7
			// Starting with linq2db v6, row queries are optimized by transforming into UPDATE..FROM 
			// optimizing the query and then transforming back to UPDATE ROW 
			// for providers without UPDATE..FROM support (i.e., Oracle).
			// This test validates that those transformations don't complexify the request 
			// by leaking some EXISTS in outer WHERE or unnecessary `FROM NewEntities` in subquery.
			Regex.Count(LastQuery!, "\"NewEntities\"\\s").ShouldBe(1);
			Regex.Count(LastQuery!, "\"UpdatedEntities\"\\s").ShouldBe(1);
			Regex.Count(LastQuery!, "\"UpdateRelation\"\\s").ShouldBe(1);
			LastQuery!.ShouldNotContain("EXISTS");
		}	

#endif

		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedUpdate)]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5413")]
		public void UpdateFromSubqueryShouldBeOptimized([DataSources(TestProvName.AllSqlCe)] string context)
		{
			using var db = GetDataContext(context);

			using var _ = new DeletePerson(db);

			using var sourceTable = db.CreateLocalTable(
			[
				new UpdateSubquerySourceTable { Id = 1, FirstName = "FirstTooth", LastName  = "FirstFairy" },
				new UpdateSubquerySourceTable { Id = 2, FirstName = "SecondTooth", LastName = "SecondFairy" },
				new UpdateSubquerySourceTable { Id = 3, FirstName = "ThirdTooth", LastName  = "ThirdFairy" }
			]);

			var updateQuery =
				from x in sourceTable
				where x.Id == 1
				from canChange in sourceTable.Where(t => t.Id == x.Id + 1).DefaultIfEmpty()
				select new
				{
					record = x,
					NewValues = new
					{
						NewFirstName = canChange != null ? canChange.FirstName! : x.FirstName,
						NewLastName  = canChange != null ? canChange.LastName! : x.LastName
					}
				};

			var affectedCount = updateQuery
				.Set(x => x.record.FirstName, x => x.NewValues.NewFirstName)
				.Set(x => x.record.LastName,  x => x.NewValues.NewLastName)
				.Update();

			affectedCount.ShouldBe(1);

			var records = sourceTable.OrderBy(x => x.Id).ToList();

			records[0].FirstName.ShouldBe("SecondTooth");
			records[0].LastName.ShouldBe("SecondFairy");

			records[1].FirstName.ShouldBe("SecondTooth");
			records[1].LastName.ShouldBe("SecondFairy");

			records[2].FirstName.ShouldBe("ThirdTooth");
			records[2].LastName.ShouldBe("ThirdFairy");
		}

		[Obsolete("Remove test after API removed")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2330")]
		public void Issue2330TestOld([DataSources(TestProvName.AllClickHouse, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q = from w in db.Parent
					join b in db.Child on w.ParentID equals b.ParentID
					where b.ChildID == (from b2 in db.Child select b2.ParentID).Max()
						// to avoid actual update
						&& b.ChildID == -1
					select new { w, b };

			q.Update(db.Parent, obj => new Model.Parent()
			{
				Value1 = obj.b.ChildID
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2330")]
		public void Issue2330Test([DataSources(TestProvName.AllClickHouse, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataContext(context);

			var q = from w in db.Parent
					join b in db.Child on w.ParentID equals b.ParentID
					where b.ChildID == (from b2 in db.Child select b2.ParentID).Max()
						// to avoid actual update
						&& b.ChildID == -1
					select new { w, b };

			q.Update(q => q.w, obj => new Model.Parent()
			{
				Value1 = obj.b.ChildID
			});
		}

		#region Issue 2815

		[ActiveIssue(Configurations = [ TestProvName.AllSqlServer, TestProvName.AllSQLite, ProviderName.SqlCe, TestProvName.AllPostgreSQL, TestProvName.AllOracle11, TestProvName.AllMySql, TestProvName.AllClickHouse, TestProvName.AllAccess ])]
		[Obsolete("Remove test after API removed")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2815")]
		public void Issue2815Test1([DataSources(false)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<Issue2815Table1>();
			using var t2 = db.CreateLocalTable<Issue2815Table2>();
			using var t3 = db.CreateLocalTable<Issue2815Table3>();

			var query = (from ext in t1
						 from source in t2.LeftJoin(c => c.ISO == ext.SRC_BIC)
						 from destination in t2.LeftJoin(c => c.ISO == ext.DES_BIC)
						 let sepa = source.SEPA && destination.SEPA
							? source.ISO == destination.ISO
								? EnumType.Sepa
								: EnumType.SepaCrossBorder
							: EnumType.Foreign
						 from channel in t3.LeftJoin(c => c.TreasuryCenter == ext.TREA_CENT
							&& c.BIC == ext.SRC_BIC
							&& c.Sepa == sepa)
						 where ext.NOT_HANDLED == 2 && ext.TRANS_CHANNEL == null
						 select channel);

			query.Update(t1, x => new Issue2815Table1()
			{
				TRANS_CHANNEL = ((TransChannel?)x.Trans_Channel) ?? TransChannel.Swift,
				IDF = x.Idf
			});
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2815")]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllClickHouse, ErrorMessage = ErrorHelper.ClickHouse.Error_CorrelatedUpdate)]
		public void Issue2815Test2([DataSources(false, ProviderName.SqlCe, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Issue2815Table1.Data);
			using var t2 = db.CreateLocalTable(Issue2815Table2.Data);
			using var t3 = db.CreateLocalTable(Issue2815Table3.Data);

			var query = (from ext in t1
						 from source in t2.LeftJoin(c => c.ISO == ext.SRC_BIC)
						 from destination in t2.LeftJoin(c => c.ISO == ext.DES_BIC)
						 let sepa = source.SEPA && destination.SEPA
							? source.ISO == destination.ISO
								? EnumType.Sepa
								: EnumType.SepaCrossBorder
							: EnumType.Foreign
						 from channel in t3.LeftJoin(c => c.TreasuryCenter == ext.TREA_CENT
							&& c.BIC == ext.SRC_BIC
							&& c.Sepa == sepa)
						 where ext.NOT_HANDLED == 2 && ext.TRANS_CHANNEL == null
						 select new {channel, ext });

			var cnt = query.Update(q => q.ext, x => new Issue2815Table1()
			{
				TRANS_CHANNEL = ((TransChannel?)x.channel.Trans_Channel) ?? TransChannel.Swift,
				IDF = (int?)x.channel.Idf ?? 0
			});

			Assert.That(cnt, Is.EqualTo(8));

			var res = t1.OrderBy(r => r.SRC_BIC).ThenBy(r => r.DES_BIC).ToArray();

			AreEqual(res, Issue2815Table1.Expected, ComparerBuilder.GetEqualityComparer<Issue2815Table1>());
		}

		enum EnumType
		{
			Sepa,
			SepaCrossBorder,
			Foreign
		}

		enum TransChannel
		{
			Tardy,
			Swift,

		}

		[Table]
		sealed class Issue2815Table1
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int SRC_BIC { get; set; }
			[Column] public int DES_BIC { get; set; }
			[Column] public int IDF { get; set; }
			[Column] public int TREA_CENT { get; set; }
			[Column] public int NOT_HANDLED { get; set; }
			[Column] public TransChannel? TRANS_CHANNEL { get; set; }

			public static readonly Issue2815Table1[] Data =
			[
				new Issue2815Table1() { Id = 1, SRC_BIC = 1, DES_BIC = 1, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 2, SRC_BIC = 2, DES_BIC = 3, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 3, SRC_BIC = 4, DES_BIC = 4, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 4, SRC_BIC = 5, DES_BIC = 6, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 5, SRC_BIC = 7, DES_BIC = 7, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 6, SRC_BIC = 8, DES_BIC = 8, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 7, SRC_BIC = 9, DES_BIC = 9, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 8, SRC_BIC = 9, DES_BIC = 10, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 9, SRC_BIC = 11, DES_BIC = 11, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 10, SRC_BIC = 12, DES_BIC = 12, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 11, SRC_BIC = 12, DES_BIC = 13, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 12, SRC_BIC = 13, DES_BIC = 13, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 13, SRC_BIC = 14, DES_BIC = 14, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
			];

			public static readonly Issue2815Table1[] Expected =
			[
				new Issue2815Table1() { Id = 1, SRC_BIC = 1, DES_BIC = 1, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 2, SRC_BIC = 2, DES_BIC = 3, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 3, SRC_BIC = 4, DES_BIC = 4, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 4, SRC_BIC = 5, DES_BIC = 6, IDF = 5, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 5, SRC_BIC = 7, DES_BIC = 7, IDF = 4, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 6, SRC_BIC = 8, DES_BIC = 8, IDF = 0, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL =  TransChannel.Swift },
				new Issue2815Table1() { Id = 7, SRC_BIC = 9, DES_BIC = 9, IDF = 6, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Tardy },
				new Issue2815Table1() { Id = 8, SRC_BIC = 9, DES_BIC = 10, IDF = 6, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Tardy },
				new Issue2815Table1() { Id = 9, SRC_BIC = 11, DES_BIC = 11, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 10, SRC_BIC = 12, DES_BIC = 12, IDF = 9, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { Id = 11, SRC_BIC = 12, DES_BIC = 13, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { Id = 12, SRC_BIC = 13, DES_BIC = 13, IDF = 0, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL =  TransChannel.Swift },
				new Issue2815Table1() { Id = 13, SRC_BIC = 14, DES_BIC = 14, IDF = 0, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL =  TransChannel.Swift },
			];
		}

		[Table]
		sealed class Issue2815Table2
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int ISO { get; set; }
			[Column] public bool SEPA { get; set; }

			public static readonly Issue2815Table2[] Data =
			[
				new Issue2815Table2() { Id = 1, ISO = 2, SEPA = true },
				new Issue2815Table2() { Id = 2, ISO = 3 },
				new Issue2815Table2() { Id = 3, ISO = 4, SEPA = true },
				new Issue2815Table2() { Id = 4, ISO = 5, SEPA = true },
				new Issue2815Table2() { Id = 5, ISO = 6, SEPA = true },
				new Issue2815Table2() { Id = 6, ISO = 7, SEPA = true },
				new Issue2815Table2() { Id = 7, ISO = 8, SEPA = true },
				new Issue2815Table2() { Id = 8, ISO = 9 },
				new Issue2815Table2() { Id = 9, ISO = 10, SEPA = true },
				new Issue2815Table2() { Id = 10, ISO = 11 },
				new Issue2815Table2() { Id = 11, ISO = 12, SEPA = true },
				new Issue2815Table2() { Id = 12, ISO = 13 },
				new Issue2815Table2() { Id = 13, ISO = 14, SEPA = true },
			];
		}

		[Table]
		sealed class Issue2815Table3
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public int TreasuryCenter { get; set; }
			[Column] public int BIC { get; set; }
			[Column] public EnumType Sepa { get; set; }
			[Column] public TransChannel Trans_Channel { get; set; }
			[Column] public int Idf { get; set; }

			public static readonly Issue2815Table3[] Data =
			[
				new Issue2815Table3() { Id = 1, TreasuryCenter = 1, BIC = 1, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 1 },
				new Issue2815Table3() { Id = 2, TreasuryCenter = 2, BIC = 2, Sepa = EnumType.SepaCrossBorder, Trans_Channel = TransChannel.Tardy, Idf = 2 },
				new Issue2815Table3() { Id = 3, TreasuryCenter = 1, BIC = 3, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Swift, Idf = 3 },
				new Issue2815Table3() { Id = 4, TreasuryCenter = 2, BIC = 4, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Tardy, Idf = 4 },
				new Issue2815Table3() { Id = 5, TreasuryCenter = 1, BIC = 5, Sepa = EnumType.SepaCrossBorder, Trans_Channel = TransChannel.Swift, Idf = 5 },
				new Issue2815Table3() { Id = 6, TreasuryCenter = 2, BIC = 6, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Tardy, Idf = 6 },
				new Issue2815Table3() { Id = 7, TreasuryCenter = 1, BIC = 7, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 4 },
				new Issue2815Table3() { Id = 8, TreasuryCenter = 1, BIC = 9, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Tardy, Idf = 6 },
				new Issue2815Table3() { Id = 9, TreasuryCenter = 1, BIC = 10, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 7 },
				new Issue2815Table3() { Id = 10, TreasuryCenter = 1, BIC = 11, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Tardy, Idf = 8 },
				new Issue2815Table3() { Id = 11, TreasuryCenter = 1, BIC = 12, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 9 },
				new Issue2815Table3() { Id = 12, TreasuryCenter = 1, BIC = 13, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Tardy, Idf = 10 },
				new Issue2815Table3() { Id = 13, TreasuryCenter = 1, BIC = 14, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Swift, Idf = 11 },
				new Issue2815Table3() { Id = 14, TreasuryCenter = 1, BIC = 15, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Tardy, Idf = 11 },
			];
		}

		#endregion
	}
}
