using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

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
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable(data))
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

		[Test]
		public void UpdateTestWhere(
			[DataSources(TestProvName.AllMySql, ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
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
		}

		[Test]
		public void UpdateTestJoin(
			[DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
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

		[Test]
		public void UpdateTestJoinSkip(
			[IncludeDataSources(
				TestProvName.AllSqlServer,
				TestProvName.AllPostgreSQL)]
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

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_UpdateWithTopOrderBy)]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllMySql, ErrorMessage = ErrorHelper.MySql.Error_SkipInUpdate)]
		public void UpdateTestJoinSkipTake(
			[DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, ProviderName.SqlCe)]
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

		[Test]
		public void UpdateTestJoinTake(
			[DataSources(TestProvName.AllAccess, TestProvName.AllSqlServer2005, TestProvName.AllMySql, TestProvName.AllClickHouse, ProviderName.SqlCe)]
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

		[Test]
		public void UpdateTestAssociation(
			[DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

				var affected = forUpdates
					.Where(v => v.Relation!.RelatedValue1 == 11)
					.Set(v => v.Value1, v => v.Relation!.RelatedValue3)
					.Update();

				Assert.That(affected, Is.EqualTo(1));

				var updatedValue = forUpdates.Where(v => v.Relation!.RelatedValue1 == 11).Select(v => v.Value1).First();

				Assert.That(updatedValue, Is.EqualTo(13));
			}
		}

		[Test]
		public void UpdateTestAssociationAsUpdatable(
			[DataSources(ProviderName.SqlCe, TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

				var query = forUpdates
					.Where(v => v.Relation!.RelatedValue1 == 11);

				var updatable = query.AsUpdatable();
				updatable = updatable.Set(v => v.Value1, v => v.Relation!.RelatedValue3);

				var affected = updatable.Update();

				Assert.That(affected, Is.EqualTo(1));

				var updatedValue = forUpdates.Where(v => v.Relation!.RelatedValue1 == 11).Select(v => v.Value1).First();

				Assert.That(updatedValue, Is.EqualTo(13));
			}
		}

		[Test]
		public void UpdateTestAssociationSimple(
			[DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

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
		}

		[Test]
		public void UpdateTestAssociationSimpleAsUpdatable(
			[DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)]
			string context)
		{
			var data = GenerateData();
			using (var db = GetDataContext(context))
			using (var forUpdates = db.CreateLocalTable<UpdatedEntities>(data))
			using (var relations = db.CreateLocalTable(GenerateRelationData()))
			{

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

		[ActiveIssue]
		[Obsolete("Remove test after API removed")]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2815")]
		public void Issue2815Test1([DataSources(false)] string context)
		{
			using var db = GetDataConnection(context);
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
			using var db = GetDataConnection(context);
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
			[Column] public int SRC_BIC { get; set; }
			[Column] public int DES_BIC { get; set; }
			[Column] public int IDF { get; set; }
			[Column] public int TREA_CENT { get; set; }
			[Column] public int NOT_HANDLED { get; set; }
			[Column] public TransChannel? TRANS_CHANNEL { get; set; }

			public static readonly Issue2815Table1[] Data =
			[
				new Issue2815Table1() { SRC_BIC = 1, DES_BIC = 1, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 2, DES_BIC = 3, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 4, DES_BIC = 4, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 5, DES_BIC = 6, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 7, DES_BIC = 7, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 8, DES_BIC = 8, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 9, DES_BIC = 9, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 9, DES_BIC = 10, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 11, DES_BIC = 11, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 12, DES_BIC = 12, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 12, DES_BIC = 13, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 13, DES_BIC = 13, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 14, DES_BIC = 14, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = null },
			];

			public static readonly Issue2815Table1[] Expected =
			[
				new Issue2815Table1() { SRC_BIC = 1, DES_BIC = 1, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 2, DES_BIC = 3, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 4, DES_BIC = 4, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 5, DES_BIC = 6, IDF = 5, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 7, DES_BIC = 7, IDF = 4, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 8, DES_BIC = 8, IDF = 0, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL =  TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 9, DES_BIC = 9, IDF = 6, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Tardy },
				new Issue2815Table1() { SRC_BIC = 9, DES_BIC = 10, IDF = 6, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Tardy },
				new Issue2815Table1() { SRC_BIC = 11, DES_BIC = 11, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 12, DES_BIC = 12, IDF = 9, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL = TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 12, DES_BIC = 13, IDF = 1, TREA_CENT = 1, NOT_HANDLED = 1, TRANS_CHANNEL = null },
				new Issue2815Table1() { SRC_BIC = 13, DES_BIC = 13, IDF = 0, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL =  TransChannel.Swift },
				new Issue2815Table1() { SRC_BIC = 14, DES_BIC = 14, IDF = 0, TREA_CENT = 1, NOT_HANDLED = 2, TRANS_CHANNEL =  TransChannel.Swift },
			];
		}

		[Table]
		sealed class Issue2815Table2
		{
			[Column] public int ISO { get; set; }
			[Column] public bool SEPA { get; set; }

			public static readonly Issue2815Table2[] Data =
			[
				new Issue2815Table2() { ISO = 2, SEPA = true },
				new Issue2815Table2() { ISO = 3 },
				new Issue2815Table2() { ISO = 4, SEPA = true },
				new Issue2815Table2() { ISO = 5, SEPA = true },
				new Issue2815Table2() { ISO = 6, SEPA = true },
				new Issue2815Table2() { ISO = 7, SEPA = true },
				new Issue2815Table2() { ISO = 8, SEPA = true },
				new Issue2815Table2() { ISO = 9 },
				new Issue2815Table2() { ISO = 10, SEPA = true },
				new Issue2815Table2() { ISO = 11 },
				new Issue2815Table2() { ISO = 12, SEPA = true },
				new Issue2815Table2() { ISO = 13 },
				new Issue2815Table2() { ISO = 14, SEPA = true },
			];
		}

		[Table]
		sealed class Issue2815Table3
		{
			[Column] public int TreasuryCenter { get; set; }
			[Column] public int BIC { get; set; }
			[Column] public EnumType Sepa { get; set; }
			[Column] public TransChannel Trans_Channel { get; set; }
			[Column] public int Idf { get; set; }

			public static readonly Issue2815Table3[] Data =
			[
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 1, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 1 },
				new Issue2815Table3() { TreasuryCenter = 2, BIC = 2, Sepa = EnumType.SepaCrossBorder, Trans_Channel = TransChannel.Tardy, Idf = 2 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 3, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Swift, Idf = 3 },
				new Issue2815Table3() { TreasuryCenter = 2, BIC = 4, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Tardy, Idf = 4 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 5, Sepa = EnumType.SepaCrossBorder, Trans_Channel = TransChannel.Swift, Idf = 5 },
				new Issue2815Table3() { TreasuryCenter = 2, BIC = 6, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Tardy, Idf = 6 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 7, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 4 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 9, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Tardy, Idf = 6 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 10, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 7 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 11, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Tardy, Idf = 8 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 12, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Swift, Idf = 9 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 13, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Tardy, Idf = 10 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 14, Sepa = EnumType.Foreign, Trans_Channel = TransChannel.Swift, Idf = 11 },
				new Issue2815Table3() { TreasuryCenter = 1, BIC = 15, Sepa = EnumType.Sepa, Trans_Channel = TransChannel.Tardy, Idf = 11 },
			];
		}

		#endregion
	}
}
