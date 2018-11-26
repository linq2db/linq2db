using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Update
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
			public UpdateRelation Relation;

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

		[Test, Combinatorial]
		public void UpdateTestWhere(
			[DataSources(ProviderName.Access, ProviderName.MySql, TestProvName.MySql57)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var tempTable = db.CreateLocalTable(newData))
			{
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
		}

		[Test, Combinatorial]
		public void UpdateTestjoin(
			[DataSources()]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable(data))
			using (var tempTable = db.CreateLocalTable(newData))
			{
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
		}		

		[Test, Combinatorial]
		public void UpdateTestjoinSkip(
			[IncludeDataSources(
				ProviderName.SqlServer,
				ProviderName.SqlServer2000,
				ProviderName.SqlServer2005,
				ProviderName.SqlServer2008,
				ProviderName.SqlServer2012,
				ProviderName.SqlServer2014,
				ProviderName.PostgreSQL, 
				ProviderName.PostgreSQL92, 
				ProviderName.PostgreSQL93, 
				ProviderName.PostgreSQL95)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable(data))
			using (var tempTable = db.CreateLocalTable(newData))
			{
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
		}		

		[Test, Combinatorial]
		public void UpdateTestjoinSkipTake(
			[DataSources(ProviderName.Access, ProviderName.MySql, TestProvName.MySql57)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			//using (var db = GetDataContext(context))
			//using (var forUpdates = db.CreateLocalTable(data))
			//using (var tempTable = db.CreateLocalTable(newData))

			var db = GetDataContext(context);
			{
				var forUpdates = db.CreateLocalTable(data);
				var tempTable = db.CreateLocalTable(newData);

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
		}		

		[Test, Combinatorial]
		public void UpdateTestjoinTake(
			[DataSources(ProviderName.Access, ProviderName.MySql, TestProvName.MySql57)]
			string context)
		{
			var data = GenerateData();
			var newData = GenerateNewData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable(data))
			using (var tempTable = db.CreateLocalTable(newData))
			{
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
		}		

		[Test, Combinatorial]
		public void UpdateTestAssociation(
			[DataSources()]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

				var affected = forUpdates
					.Where(v => v.Relation.RelatedValue1 == 11)
					.Set(v => v.Value1, v => v.Relation.RelatedValue3)
					.Update();

				Assert.AreEqual(1, affected);

				var updatedValue = forUpdates.Where(v => v.Relation.RelatedValue1 == 11).Select(v => v.Value1).First();

				Assert.AreEqual(13, updatedValue);

			}
		}		

		[Test, Combinatorial]
		public void UpdateTestAssociationAsUpdatable(
			[DataSources()]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

				var query = forUpdates
					.Where(v => v.Relation.RelatedValue1 == 11);

				var updatable = query.AsUpdatable();
				updatable = updatable.Set(v => v.Value1, v => v.Relation.RelatedValue3);

				var affected = updatable.Update();

				Assert.AreEqual(1, affected);

				var updatedValue = forUpdates.Where(v => v.Relation.RelatedValue1 == 11).Select(v => v.Value1).First();

				Assert.AreEqual(13, updatedValue);

			}
		}		

		[Test, Combinatorial]
		public void UpdateTestAssociationSimple(
			[DataSources()]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

				var affected = forUpdates
					.Where(v => v.Relation.RelatedValue1 == 11)
					.Set(v => v.Value1, v => v.Value1 + v.Value2 + v.Value3)
					.Set(v => v.Value2, v => v.Value1 + v.Value2 + v.Value3)
					.Set(v => v.Value3, v => 1)
					.Update();

				Assert.AreEqual(1, affected);

				var updatedValue = forUpdates.Where(v => v.Relation.RelatedValue1 == 11)
					.Select(v => new {v.Value1, v.Value2, v.Value3})
					.First();

				Assert.AreEqual(36, updatedValue.Value1);
				Assert.AreEqual(36, updatedValue.Value2);
				Assert.AreEqual(1,  updatedValue.Value3);
			}
		}		

		[Test, Combinatorial]
		public void UpdateTestAssociationSimpleAsUpdatable(
			[DataSources()]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

				var query = forUpdates
					.Where(v => v.Relation.RelatedValue1 == 11);

				var updatable = query.AsUpdatable();
				updatable = updatable.Set(v => v.Value1, v => v.Value1 + v.Value2 + v.Value3);
				updatable = updatable.Set(v => v.Value2, v => v.Value1 + v.Value2 + v.Value3);
				updatable = updatable.Set(v => v.Value3, v => 1);

				var affected = updatable.Update();

				Assert.AreEqual(1, affected);

				var updatedValue = forUpdates.Where(v => v.Relation.RelatedValue1 == 11)
					.Select(v => new {v.Value1, v.Value2, v.Value3})
					.First();

				Assert.AreEqual(36, updatedValue.Value1);
				Assert.AreEqual(36, updatedValue.Value2);
				Assert.AreEqual(1,  updatedValue.Value3);
			}
		}		




	}
}
